using Dapper;
using SawyerSight.Models.DAL;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;
using SawyerSight.Web.Models.ETL.Costpoint;

namespace SawyerSight.Web.DAL.Infrastructure
{
    public static class DataTableExtensions
    {
        public static void SetColumnsOrder(this DataTable table, params String[] columnNames)
        {
            int columnIndex = 0;
            foreach (var columnName in columnNames)
            {
                table.Columns[columnName].SetOrdinal(columnIndex);
                columnIndex++;
            }
        }
    }
    class StageDataService : DataService, IStageDataService
    {        
        public StageDataService()
            : base(Config.Current.ETLConnectionString)
        {            
        }

        public IEnumerable<Project> GetProjects(int reportDataSetID)
        {
            string sql =
                $@"SELECT 
                [PROJ_ID] as Id,
                [PROJ_NAME] as Name,
                '' as ParentId,				
                ACTIVE_FL as ActiveFlag, 
                ORG_ID as OrgId,
                LVL_NO as Level,
                ReportDataSetID
                FROM [Costpoint].[PROJ] 
                WHERE ReportDataSetID=@reportDataSetID
                ORDER BY LVL_NO ASC, PROJ_ID ASC";

            return Query<Project>(sql, new { reportDataSetID }).ToList();
        }

        public IEnumerable<Organization> GetOrganizations(int reportDataSetID)
        {
            const string sql =
                @"SELECT [ORG_ID] as Id,
                [ORG_NAME] as Name,
                '' as ParentID,				
                ACTIVE_FL as ActiveFlag, 
                ReportDataSetID
                FROM [Costpoint].[ORG] 
                WHERE ReportDataSetID=@reportDataSetID
                ORDER BY LVL_NO ASC, ORG_ID ASC;";

            return Query<Organization>(sql, new { reportDataSetID }).ToList();
        }

        public IEnumerable<FiscalYear> GetFiscalYears(int reportDataSetID)
        {
            const string sql =
                @"SELECT distinct '' as yearCode,
                 '' as ParentYearCode,
                 years.FY_DESC as Description,
                 years.S_STATUS_CD as Status,
                 years.ReportDataSetID, years.FY_CD as yearPart,PD_NO as monthPart
                  FROM [Costpoint].[GL_POST_SUM] months
                  JOIN [Costpoint].[FY] years on months.FY_CD=years.FY_CD and years.ReportDataSetID=months.ReportDataSetID
                WHERE months.ReportDataSetID=@reportDataSetID
				Order BY yearPart DESC, monthPart ASC;";
            return Query<FiscalYear>(sql, new { reportDataSetID }).ToList();
        }

        public IEnumerable<Account> GetAllAccounts(int reportDataSetID)
        {
            const string sql =
               @"SELECT [ACCT_ID] as Id,
                [ACCT_NAME] as Name,
                '' as ParentID,				
                ACTIVE_FL as ActiveFlag, 
                0 as IsActualAccount,
                ReportDataSetID
                FROM [Costpoint].[ACCT] 
                WHERE ReportDataSetID=@reportDataSetID
                ORDER BY LVL_NO ASC, ACCT_ID ASC;";

            return Query<Account>(sql, new { reportDataSetID }).ToList();
        }

        public IEnumerable<Account> GetRevenueAccounts(int reportDataSetID)
        {
            const string sql =
               @"SELECT DISTINCT acct.[ACCT_ID] as Id,
                acct.[ACCT_NAME] as Name,
                '' as ParentID,				
                acct.ACTIVE_FL as ActiveFlag, 
                1 as IsActualAccount,
                acct.ReportDataSetID
                FROM Costpoint.FS_LN fs
                JOIN Costpoint.FS_LN_ACCT fsacct ON fs.FS_LN_KEY=fsacct.FS_LN_KEY
                JOIN Costpoint.ACCT acct ON acct.ACCT_ID=fsacct.ACCT_ID
                WHERE fs.FS_CD = 'INCSTM' AND fsacct.FS_CD = 'INCSTM' AND [FS_MAJOR_NO] = '1' AND [FS_GRP_NO] = '1'
                AND fs.ReportDataSetID=@reportDataSetID AND fsacct.ReportDataSetID=@reportDataSetID AND acct.ReportDataSetID=@reportDataSetID;";

            return Query<Account>(sql, new { reportDataSetID }).ToList();
        }

        public IEnumerable<Account> GetCostsAccounts(int reportDataSetID)
        {
            const string sql =
               @"SELECT DISTINCT acct.[ACCT_ID] as Id,
                acct.[ACCT_NAME] as Name,
                '' as ParentID,				
                acct.ACTIVE_FL as ActiveFlag, 
                1 as IsActualAccount,
                acct.ReportDataSetID
                FROM Costpoint.FS_LN fs
                JOIN Costpoint.FS_LN_ACCT fsacct ON fs.FS_LN_KEY=fsacct.FS_LN_KEY
                JOIN Costpoint.ACCT acct ON acct.ACCT_ID=fsacct.ACCT_ID
                WHERE fs.FS_CD = 'INCSTM' AND fsacct.FS_CD = 'INCSTM' AND [FS_MAJOR_NO] = '2'
                AND fs.ReportDataSetID=@reportDataSetID AND fsacct.ReportDataSetID=@reportDataSetID AND acct.ReportDataSetID=@reportDataSetID;";

            return Query<Account>(sql, new { reportDataSetID }).ToList();
        }

        public void InsertTable(string tableName, DataTable table, int reportDataSetID)
        {            
            //first get column orders from the table type
            string columnsSql = $"SELECT c.name FROM sys.table_types tt INNER JOIN sys.columns c ON c.object_id = tt.type_table_object_id WHERE tt.name = '{tableName + "TableType"}'";
            var columns = Query<string>(columnsSql).ToArray();

            table.SetColumnsOrder(columns);

            List<string> columnNames = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columnNames.Add(column.ColumnName);
            }
            string sql = $"DELETE FROM [Costpoint].[{tableName}] WHERE ReportDataSetID=@reportDataSetID; INSERT INTO [Costpoint].[{tableName}] SELECT * FROM @tvp";
            
            //this code is for debugging only, and should always remain commented, unless the execution is locally
            //DebugOnlyExecute(columns,tableName, table, reportDataSetID);

            Execute(sql, new { @reportDataSetID = reportDataSetID, @tvp = table.AsTableValuedParameter("Costpoint." + tableName + "TableType") });

        }

        private void DebugOnlyExecute(string[] columns,string tableName, DataTable table, int reportDataSetID)
        {
            if (tableName != "PROJ")
            {
                return;
            }

            List<string> errors = new List<string>();
            int rowNr = 0;
            var factory = System.Data.Common.DbProviderFactories.GetFactory("System.Data.SqlClient");
            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = Config.Current.DefaultConnectionString;

                foreach (DataRow r in table.Rows)
                {
                    rowNr++;
                    try
                    {                        
                        var dtRow = new DataTable();

                        foreach (var column in columns)
                        {
                            dtRow.Columns.Add(new DataColumn(column));
                        }
                        //DataTable dtRow = new DataTable();
                        dtRow.Rows.Add(r.ItemArray);
                        string sql = $"DELETE FROM [Costpoint].[{tableName}] WHERE ReportDataSetID=@reportDataSetID; INSERT INTO [Costpoint].[{tableName}] SELECT * FROM @tvp";

                        Execute(sql, new { @reportDataSetID = reportDataSetID, @tvp = dtRow.AsTableValuedParameter("Costpoint." + tableName + "TableType") });

                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("truncated"))
                        {
                            string err = "";
                            foreach(var column in columns)
                            {
                                err += column + " " + r[column].ToString().Length.ToString() + "\n";
                            }
                            errors.Add(rowNr + ": " + err);
                        }
                        else
                        {
                            errors.Add(rowNr + ": " + ex.StackTrace);
                        }
                    }
                }
            }
            System.IO.File.WriteAllText("D:\\tableErrors.txt", string.Join(Environment.NewLine, errors));
        }

        public IEnumerable<PROJ> GetETLProjects(int reportDataSetID)
        {
            string sql =
                $@"SELECT *
                FROM [Costpoint].[PROJ] 
                WHERE ReportDataSetID=@reportDataSetID";

            return Query<PROJ>(sql, new { reportDataSetID }).ToList();
        }

        public IEnumerable<GlPostSum> GetAllTrialBalanceTransactionAmounts(IEnumerable<string> selectedProjects, IEnumerable<string> selectedAccounts, int reportDataSetID)
        {
            string sql = $@"SELECT ACCT_ID AS AccountID, PROJ_ID AS ProjectID, ORG_ID AS OrganizationID, FY_CD AS FiscalYear, PD_NO as FiscalMonth, AMT AS Amount, ReportDataSetID
                            FROM [Costpoint].[GL_POST_SUM] WHERE PROJ_ID IN (SELECT * FROM @ProjTVP) AND ACCT_ID IN (SELECT * FROM @AcctTVP) AND ReportDataSetID={reportDataSetID}";
            return Query<GlPostSum>(sql, new { @ProjTVP = ListStringToDataTable(selectedProjects).AsTableValuedParameter("[Costpoint].[StringTableType]"),
                                                @AcctTVP = ListStringToDataTable(selectedAccounts).AsTableValuedParameter("[Costpoint].[StringTableType]")});
        }

        private DataTable ListStringToDataTable(IEnumerable<string> list)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID");
            foreach(var el in list)
            {
                var row = dt.NewRow();
                row[0] = el;
                dt.Rows.Add(row);
            }
           
            return dt;
        }

        public IEnumerable<CUST> GetAllCustomers(int reportDataSetID)
        {
            string sql =
                $@"SELECT CUST_ID, CUST_NAME, ReportDataSetID
                FROM [Costpoint].[CUST] 
                WHERE ReportDataSetID=@reportDataSetID";

            return Query<CUST>(sql, new { reportDataSetID }).ToList();
        }
        public IEnumerable<ACCT> GetAllAcct(int reportDataSetID, IEnumerable<String>ACCTID)
        {
            string sql =
                $@"SELECT ACCT_ID, ACCT_NAME, ReportDataSetID
                FROM [Costpoint].[ACCT] 
                WHERE ReportDataSetID=@reportDataSetID AND ACCT_ID IN ('{string.Join("','", ACCTID)}')";

            return Query<ACCT>(sql, new { reportDataSetID }).ToList();
        }
    }
}