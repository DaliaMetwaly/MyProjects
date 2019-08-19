using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web;
using Dapper;
using SawyerSight.Models.DAL;
using SawyerSight.Web.Models.ETL.Costpoint;

namespace SawyerSight.Web.DAL
{
    public class MigrationDataService :DataService, IMigrationDataService
    {
        public MigrationDataService()
           : base(Config.Current.DefaultConnectionString)
        {
        }
        public void ImportOrganizations(IEnumerable<ITreeItemModel> list, int reportDataSetID)
        {
            var dt = ListToDataTable<ITreeItemModel>(list);
            string sql = $"DELETE FROM [SawyerSight].[ORG] WHERE ReportDataSetID=@reportDataSetID;" +
                $" INSERT INTO [SawyerSight].[ORG] ([ORG_ID],[ORG_NAME],[ReportDataSetID]) SELECT [Id],[Name],[ReportDataSetID] FROM @tvp";
            Execute(sql, new {@reportDataSetID=reportDataSetID, @tvp = dt.AsTableValuedParameter("SawyerSight.ITreeItemModelTableType") });
        }

        public void ImportProjects(IEnumerable<PROJ> list, int reportDataSetID)
        {
            var dt = ListToDataTable<PROJ>(list);
            string sql = $"DELETE FROM [SawyerSight].[PROJ] WHERE ReportDataSetID=@reportDataSetID;" +
                $@"INSERT INTO [SawyerSight].[PROJ]
                       ([PROJ_ID]
                       ,[ORG_ID]
                       ,[PROJ_NAME]
                       ,[ACTIVE_FL]
                       ,[PRIME_CONTR_ID]
                       ,[PROJ_START_DT]
                       ,[PROJ_END_DT]
                       ,[CUST_ID]
                       ,[PRIME_CONTR_FL]
                       ,[PROJ_V_TOT_AMT]
                       ,[PROJ_F_TOT_AMT]
                       ,[LVL_NO]       
                       ,[PROJ_TYPE_DC]
                       ,[SUB_CONTR_FL]
                       ,[ReportDataSetID]) 
                SELECT [PROJ_ID]
                       ,[ORG_ID]
                       ,[PROJ_NAME]
                       ,[ACTIVE_FL]
                       ,[PRIME_CONTR_ID]
                       ,[PROJ_START_DT]
                       ,[PROJ_END_DT]
                       ,[CUST_ID]
                       ,[PRIME_CONTR_FL]
                       ,[PROJ_V_TOT_AMT]
                       ,[PROJ_F_TOT_AMT]
                       ,[LVL_NO]
                       ,[PROJ_TYPE_DC]
                       ,[SUB_CONTR_FL]
                       ,[ReportDataSetID] FROM @tvp";
            Execute(sql, new { @reportDataSetID = reportDataSetID, @tvp = dt.AsTableValuedParameter("SawyerSight.PROJTableType") });
        }

        public void CrossMapProjects(int reportDataSetID, int level)
        {
            string sql = $"DELETE FROM [SawyerSight].[PROJ_Cross_Mappings] WHERE ReportDataSetID=@reportDataSetID;" +
                                $@"INSERT INTO [SawyerSight].[PROJ_Cross_Mappings]
                                       ([PARENT]
                                       ,[CHILD]
                                       ,[PARENTLEVEL]
                                       ,[CHILDLEVEL]
                                       ,[ReportDataSetID])
                                 select pout.PROJ_ID as PARENT, pin.PROJ_ID as CHILD, pout.LVL_NO as PARENTLEVEL, pin.LVL_NO AS CHILDLEVEL, pout.ReportDataSetID
                            from sawyersight.PROJ pout
                            join sawyersight.proj pin on pin.PROJ_ID like (pout.PROJ_ID+'%') 
                            WHERE pout.ReportDataSetID=@reportDataSetID AND pin.ReportDataSetID=@reportDataSetID AND pout.LVL_NO={level}";
            Execute(sql, new { @reportDataSetID = reportDataSetID });
        }

        public void ImportGlPostSum(IEnumerable<GlPostSum> list, int reportDataSetID)
        {
            var dt = ListToDataTable<GlPostSum>(list);
            string sql = $@"DELETE FROM [SawyerSight].[GL_POST_SUM] WHERE ReportDataSetID=@reportDataSetID;
                            INSERT INTO [SawyerSight].[GL_POST_SUM]
                               ([ACCT_ID]
                               ,[PROJ_ID]
                               ,[ORG_ID]
                               ,[FY_CD]
                               ,[PD_NO]
                               ,[AMT]
                               ,[ReportDataSetID])
                    SELECT AccountID, ProjectID, OrganizationID, FiscalYear, FiscalMonth, Amount, ReportDataSetID
                               FROM @tvp";
            Execute(sql, new { @reportDataSetID = reportDataSetID, @tvp = dt.AsTableValuedParameter("SawyerSight.GLPOSTSUMTableType") });
            //this code is for debugging only, and should always remain commented, unless the execution is locally
        }

        public void ImportCustomers(IEnumerable<CUST> list, int reportDataSetID)
        {
            var dt = ListToDataTable<CUST>(list);
            string sql = $"DELETE FROM [SawyerSight].[CUST] WHERE ReportDataSetID=@reportDataSetID;" +
                $@"INSERT INTO [SawyerSight].[CUST]
                               ([CUST_ID]
                               ,[CUST_NAME]
                               ,[ReportDataSetID])
                    SELECT CUST_ID, CUST_NAME, ReportDataSetID
                               FROM @tvp";
            Execute(sql, new { @reportDataSetID = reportDataSetID, @tvp = dt.AsTableValuedParameter("SawyerSight.CUSTTableType") });
        }

        public DataTable GenerateWaterfall(string query)
        {
            return DataTableExecute(query);
        }

        public void SaveWaterfall(int clientID, int reportDataSetID, string waterfallText)
        {
            string sql = $"DELETE FROM [SawyerSight].[WaterfallData] WHERE ReportDataSetID=@reportDataSetID;" +
               $@"INSERT INTO [SawyerSight].[WaterfallData]
                       ([ClientID]
                       ,[ReportDataSetID]
                       ,[WaterfallData])
                 VALUES
                       (@clientID
                       ,@reportDataSetID
                       ,@WaterfallData)";
            Execute(sql, new { @clientID=clientID, @reportDataSetID = reportDataSetID, @waterfallData=waterfallText});
        }

        public static DataTable ListToDataTable<T>(IEnumerable<T> data)
        {
            DataTable table = new DataTable();

            //special handling for value types and string
            if (typeof(T).IsValueType || typeof(T).Equals(typeof(string)))
            {

                DataColumn dc = new DataColumn("Value");
                table.Columns.Add(dc);
                foreach (T item in data)
                {
                    DataRow dr = table.NewRow();
                    dr[0] = item;
                    table.Rows.Add(dr);
                }
            }
            else
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
                foreach (PropertyDescriptor prop in properties)
                {
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
                foreach (T item in data)
                {
                    DataRow row = table.NewRow();
                    foreach (PropertyDescriptor prop in properties)
                    {
                        try
                        {
                            row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                        }
                        catch (Exception ex)
                        {
                            row[prop.Name] = DBNull.Value;
                        }
                    }
                    table.Rows.Add(row);
                }
            }
            return table;
        }
        public void ImportAccounts(IEnumerable<ACCT> accounts, int reportDataSetID)
        {
            var dt = ListToDataTable<ACCT>(accounts);
            string sql = $"DELETE FROM [SawyerSight].[ACCT] WHERE ReportDataSetID=@reportDataSetID;" +
                $@"INSERT INTO [SawyerSight].[ACCT] ([ACCT_ID],[ACCT_NAME],[ReportDataSetID])SELECT [ACCT_ID],[ACCT_NAME],[ReportDataSetID] FROM @tvp ";
            Execute(sql, new { @reportDataSetID = reportDataSetID, @tvp = dt.AsTableValuedParameter("SawyerSight.ACCTTableType") });
        }

        
    }
}