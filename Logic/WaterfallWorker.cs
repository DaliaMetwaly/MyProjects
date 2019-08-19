using Newtonsoft.Json;
using OfficeOpenXml;
using SawyerSight.Models.DAL;
using SawyerSight.Web.DAL;
using SawyerSight.Web.Models.ETL.Costpoint;
using SawyerSight.Web.Models.ViewModel;
using SawyerSight.Web.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Logic
{

    public class WaterfallWorker
    {
        private readonly IStageDataService _etlService;
        private readonly IMigrationDataService _migrationService;
        private readonly WaterfallContext _waterfallContext;

        public List<TrialBalanceQuery> GeneratedQueries;
        public Dictionary<string, List<string>> ProcessedFiscalYears;
        public WaterfallWorker(WaterfallContext waterfallContext)
        {
            _etlService = (IStageDataService)UnityConfig.DefaultContainer.Resolve(typeof(IStageDataService), null, null);
            _migrationService = (IMigrationDataService)UnityConfig.DefaultContainer.Resolve(typeof(IMigrationDataService), null, null);
            _waterfallContext = waterfallContext;

        }

        public WaterfallWorker(IStageDataService etlService, IMigrationDataService migrationService, WaterfallContext waterfallContext)
        {
            _etlService = etlService;
            _migrationService = migrationService;
        }

        public string MigrateETLToLiveDataAndGenerateWaterfall()
        {
            try
            {
                int itemsCount = 8;
                int itemsProgress = 1;

                /* */

                SignalRProcessor.SendImportUpdate("Processing Selected Organizations", itemsProgress++, itemsCount);
                var organizations = _etlService.GetOrganizations(_waterfallContext.ReportDataSetID);
                var filteredOrganizations = ProcessSelectedOrganizations(organizations, _waterfallContext.SelectedOrganizations);
                _migrationService.ImportOrganizations(filteredOrganizations, _waterfallContext.ReportDataSetID);
                SignalRProcessor.SendImportUpdate("Processing Selected Projects", itemsProgress++, itemsCount);
                var projects = _etlService.GetETLProjects(_waterfallContext.ReportDataSetID);
                var filteredProjects = ProcessSelectedProjects(projects, _waterfallContext.SelectedProjects).Distinct().ToList();
                _migrationService.ImportProjects(filteredProjects, _waterfallContext.ReportDataSetID);
                SignalRProcessor.SendImportUpdate("Processing Customers from CUST Table", itemsProgress++, itemsCount);
                var allCustomers = _etlService.GetAllCustomers(_waterfallContext.ReportDataSetID);
                _migrationService.ImportCustomers(allCustomers, _waterfallContext.ReportDataSetID);
                SignalRProcessor.SendImportUpdate("Processing Projects Levels", itemsProgress++, itemsCount);
                _migrationService.CrossMapProjects(_waterfallContext.ReportDataSetID, _waterfallContext.SelectedProjectsLevel);


                SignalRProcessor.SendImportUpdate("Processing Selected Trial Balance Accounts", itemsProgress++, itemsCount);
                var allTrialBalances = new List<string>();
                allTrialBalances.AddRange(_waterfallContext.RevenueAccounts.Revenue1Nodes);
                allTrialBalances.AddRange(_waterfallContext.RevenueAccounts.Revenue2Nodes);
                allTrialBalances.AddRange(_waterfallContext.RevenueAccounts.Revenue3Nodes);
                allTrialBalances.AddRange(_waterfallContext.RevenueAccounts.Revenue4Nodes);

                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs1Nodes);
                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs2Nodes);
                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs3Nodes);
                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs4Nodes);
                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs5Nodes);
                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs6Nodes);
                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs7Nodes);
                allTrialBalances.AddRange(_waterfallContext.CostsAccounts.Costs8Nodes);
               

                SignalRProcessor.SendImportUpdate("Processing Accounts from ACCT Table", itemsProgress++, itemsCount);
                var allAccounts = _etlService.GetAllAcct(_waterfallContext.ReportDataSetID, allTrialBalances);
                _migrationService.ImportAccounts(allAccounts, _waterfallContext.ReportDataSetID);

                var trialAccounts = _etlService.GetAllTrialBalanceTransactionAmounts(filteredProjects.Select(x => x.PROJ_ID).ToList(), allTrialBalances, _waterfallContext.ReportDataSetID);
                SignalRProcessor.SendImportUpdate("Processing Amounts", itemsProgress++, itemsCount);
                _migrationService.ImportGlPostSum(trialAccounts, _waterfallContext.ReportDataSetID);
                 
                
                //------------------------------------ Finished importing data ---------------------------------------------------

                var yearsAndMonths = ProcessFiscalYears(_waterfallContext.SelectedFiscalYears);
                ProcessedFiscalYears = yearsAndMonths;
                var trialBalanceQueries = GenerateSUMQueriesForCategoriesAndYears(yearsAndMonths);
                GeneratedQueries = trialBalanceQueries;
                _waterfallContext.SelectedDemographics.RemoveAll(x => string.IsNullOrWhiteSpace(x));
                string demographicsSelect = string.Join(",", _waterfallContext.SelectedDemographics);

            

                string totalQuery = $@"
                                       IF OBJECT_ID('tempdb..#TempTrialBalanceSummary', 'U') IS NOT NULL 
                                       DROP TABLE #TempTrialBalanceSummary;
                                       
                                       IF OBJECT_ID('tempdb..#TempWaterfallData', 'U') IS NOT NULL 
                                       DROP TABLE #TempWaterfallData

                                       Declare	@dynamicCol            NVARCHAR(max),
	                                       		@dynamicColNull        NVARCHAR(max),
	                                       		@endResult             NVARCHAR(max),
	                                       		@waterfallColumns      NVARCHAR(max),
	                                       		@waterfallColumnsSum   NVARCHAR(max) ,
	                                       		@waterfallTempTableSql NVARCHAR(max),
                                                @tBTieOut              NVARCHAR(max),
                                                @emptyRow              NVARCHAR(max)
                                       
                                       ------------------------------------Trial Balance Summary----------------------------------------
                                       CREATE TABLE #TempTrialBalanceSummary (
                                       ACCT_ID nvarchar(100),
                                       BalanceValue decimal(18,8),
                                       CategoryName VARCHAR(30) NOT NULL,
                                       AccountName VARCHAR(30) NOT NULL,
                                       
                                       );

                                       INSERT INTO #TempTrialBalanceSummary (ACCT_ID, BalanceValue, CategoryName,AccountName)
                                       { string.Join("UNION ALL", GenerateTrialBalanceSummaryQueries(yearsAndMonths).Select(x => x.Query).ToList())}

                                       -------------------------------------------------------------------------------------------------


                                       ------------------------------------WaterFall Data-----------------------------------------------
                                       CREATE TABLE #TempWaterfallData ( TempColumn INT );

                                        SET  @WaterfallColumns =   
                                       ' PROJ_ID						nvarchar(50)		NULL'+
	                                   ',PROJ_NAME						nvarchar(50)		NULL'+
							           ',PRIME_CONTR_ID					nvarchar(50)		NULL'+  
							           ',PROJ_START_DT					smalldatetime		NULL'+
							           ',PROJ_END_DT					smalldatetime		NULL'+  
							           ',CUST_NAME						nvarchar(50)		NULL'+
							           ',PRIME_CONTR_FL					nvarchar(1)		    NULL'+ 
                                       ',['+'{string.Join(",[", trialBalanceQueries.Select(x => x.CategoryName+'_'+x.Year+"] decimal(18,8)		NULL ").ToList())}';

                                      
                                       SET @waterfallColumnsSum=  
                                       '-2      AS RowID					'+
	                                   ','' '' AS PROJ_ID				'+
	                                   ','' '' AS PROJ_NAME				'+
							           ','' '' AS PRIME_CONTR_ID		'+  
							           ',NULL  AS PROJ_START_DT		    '+
							           ',NULL  AS PROJ_END_DT			'+  
							           ','' ''  AS CUST_NAME			'+
							           ',''Subtotal per Waterfall'' AS PRIME_CONTR_FL	'+ 
                                       ',SUM(['+'{string.Join(",SUM([", trialBalanceQueries.Select(x => x.CategoryName + '_' + x.Year + "])").ToList())}';

                                       SET @tBTieOut=  
                                       '0      AS RowID					'+
	                                   ',''TB Tie-Out'' AS PROJ_ID				'+
	                                   ','' '' AS PROJ_NAME				'+
							           ','' '' AS PRIME_CONTR_ID		'+  
							           ',NULL  AS PROJ_START_DT		    '+
							           ',NULL  AS PROJ_END_DT			'+  
							           ','' ''  AS CUST_NAME			'+
							           ','' '' AS PRIME_CONTR_FL	'+ 
                                       ',NULL AS ['+'{string.Join(",NULL AS [", trialBalanceQueries.Select(x => x.CategoryName + '_' + x.Year + "]").ToList())}';

                                       SET @emptyRow=  
                                       '-1      AS RowID					'+
	                                   ','' '' AS PROJ_ID				'+
	                                   ','' '' AS PROJ_NAME				'+
							           ','' '' AS PRIME_CONTR_ID		'+  
							           ',NULL  AS PROJ_START_DT		    '+
							           ',NULL  AS PROJ_END_DT			'+  
							           ','' ''  AS CUST_NAME			'+
							           ','' '' AS PRIME_CONTR_FL	'+ 
                                       ',NULL AS ['+'{string.Join(",NULL AS [", trialBalanceQueries.Select(x => x.CategoryName + '_' + x.Year + "]").ToList())}';

                                       SET @WaterfallTempTableSql='ALTER TABLE #TempWaterfallData ADD '+@WaterfallColumns;	

                                       EXEC(@WaterfallTempTableSql)	

	                                   ALTER TABLE #TempWaterfallData DROP COLUMN TempColumn;

                                       INSERT INTO #TempWaterfallData
                                       Select  PROJ_ID, {demographicsSelect}," + string.Join(",", trialBalanceQueries.Select(x => x.Query).ToList()) + $@" 
                                       FROM SawyerSight.PROJ pr
                                       JOIN SawyerSight.CUST c on pr.CUST_ID=c.CUST_ID
                                       WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
                                       AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                       AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}

                                      SELECT @dynamicColNull=STUFF((SELECT  ', '+'ISNULL('+QUOTENAME(CategoryName),','+'''0'''+') As '+QUOTENAME(CategoryName) FROM #TempTrialBalanceSummary group by CategoryName  FOR XML PATH ('')),1,2,'')
                                      SELECT @dynamicCol=STUFF((SELECT  ', '+QUOTENAME(CategoryName) FROM #TempTrialBalanceSummary group by CategoryName FOR XML PATH ('')),1,2,'')                                        	
                                      
                                       -------------------------------------------------------------------------------------------------


                                       ------------------------------------Union WaterFall Data AND Trial Balance Summary---------------
                                       SET    @endResult=
                                         'SELECT * 
                                          FROM( 
                                      			SELECT NULL AS RowID, * from #TempWaterfallData
                                      			
                                      			UNION
                                      			
                                      			SELECT '+@waterfallColumnsSum+' from #TempWaterfallData
                                      			UNION 
                                                SELECT '+ @emptyRow +'
                                                UNION
                                                SELECT '+ @tBTieOut +'
                                      			UNION  
                                      			
                                      			SELECT ROW_NUMBER() OVER (ORDER BY ACCT_ID ) AS RowID,  CASE WHEN TRIM([ACCT_ID])='''' OR [ACCT_ID] IS NULL THEN ''EMPTY'' ELSE [ACCT_ID] END  AS [PROJ_ID],[AccountName]  AS [PROJ_NAME],'' ''  AS [PRIME_CONTR_ID],NULL  AS [PROJ_START_DT],NULL  AS [PROJ_END_DT], '' '' AS [CUST_NAME] ,'' '' AS [PRIME_CONTR_FL] , '+@DynamicColNull+' From
                                      			(   
                                      			SELECT * from #TempTrialBalanceSummary GROUP BY ROLLUP(BalanceValue,ACCT_ID,CategoryName,AccountName)
                                      			)
                                      			AS Src
                                      			PIVOT
                                      			(
                                      			SUM(BalanceValue) FOR [CategoryName] IN ('+@DynamicCol+')
                                      			)AS Pvt 
                                      		) AS EndResult 
                                      		
                                      		WHERE EndResult.[PROJ_ID] <> ''EMPTY'' AND EndResult.[PROJ_NAME] IS NOT NULL  
                                      		Order By EndResult.RowID'	
                                      
                                     EXEC(@endResult)
                                     ";
                
                SignalRProcessor.SendImportUpdate("Generating Waterfall Report", itemsProgress++, itemsCount);

                var report = _migrationService.GenerateWaterfall(totalQuery);
                DataRow dr = report.NewRow();
                for (int i = 0; i < report.Columns.Count - 1; i++)
                {
                    report.Columns[i].AllowDBNull = true;

                }
                
                report.Rows.InsertAt(dr, 0);
                dr = report.NewRow();
                report.Rows.InsertAt(dr, 0);
                dr = report.NewRow();
                report.Rows.InsertAt(dr, 0);
                var reportJSON = JsonConvert.SerializeObject(report);
                _migrationService.SaveWaterfall(_waterfallContext.ClientUniqueID, _waterfallContext.ReportDataSetID, reportJSON);

                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\App_Data", _waterfallContext.ReportDataSetID.ToString() + ".xlsx");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                report.Columns.RemoveAt(0);// remove rowid from excel which responsible for sorting excel

                
                using (ExcelPackage pck = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Accounts");
                    ws.Cells["A1"].LoadFromDataTable(report, true);
                    pck.Save();
                }

                SignalRProcessor.SendImportUpdate("Generating Waterfall Report Finished. You will be redirected to the Excel Preview.", itemsCount, itemsCount);

                return totalQuery;
            }

            catch (Exception ex)
            {
                return ex.Message + Environment.NewLine + ex.StackTrace;
            }


        }

        private List<ITreeItemModel> ProcessSelectedOrganizations(IEnumerable<ITreeItemModel> fullList, TreeListChecks checks)
        {
            List<ITreeItemModel> result = new List<ITreeItemModel>();

            //if All Active are selected, add them
            if (checks.SelectAllActive)
            {
                result.AddRange(fullList.Where(x => x.ActiveFlag == "Y").ToList());
            }

            //if All Inactive are selected, add them
            if (checks.SelectAllInactive)
            {
                result.AddRange(fullList.Where(x => x.ActiveFlag == "N").ToList());
            }

            //Add All that start with
            foreach (var el in checks.CheckedNodes)
            {
                result.AddRange(fullList.Where(x => x.Id.StartsWith(el)).ToList());
            }
            foreach (var el in checks.VisibleUncheckedNodes)
            {
                //if there are checked children of the element, remove the parent only
                if (checks.CheckedNodes.Where(x => x.StartsWith(el)).Count() > 0)
                {
                    result.RemoveAll(x => x.Id == el);
                }
                //otherwise, remove parent and all children
                else
                {
                    result.RemoveAll(x => x.Id.StartsWith(el));
                }
            }

            return result.Distinct().ToList();
        }

        private List<PROJ> ProcessSelectedProjects(IEnumerable<PROJ> fullList, TreeListChecks checks)
        {
            List<PROJ> result = new List<PROJ>();

            //if All Active are selected, add them
            if (checks.SelectAllActive)
            {
                result.AddRange(fullList.Where(x => x.ACTIVE_FL == "Y").ToList());
            }

            //if All Inactive are selected, add them
            if (checks.SelectAllInactive)
            {
                result.AddRange(fullList.Where(x => x.ACTIVE_FL == "N").ToList());
            }

            //Add All that start with
            foreach (var el in checks.CheckedNodes)
            {
                result.AddRange(fullList.Where(x => x.PROJ_ID.StartsWith(el)).ToList());
            }
            foreach (var el in checks.VisibleUncheckedNodes)
            {
                //if there are checked children of the element, remove the parent only
                if (checks.CheckedNodes.Where(x => x.StartsWith(el)).Count() > 0)
                {
                    result.RemoveAll(x => x.PROJ_ID == el);
                }
                //otherwise, remove parent and all children
                else
                {
                    result.RemoveAll(x => x.PROJ_ID.StartsWith(el));
                }
            }

            return result.Distinct().ToList();
        }

        private Dictionary<string, List<string>> ProcessFiscalYears(List<string> selectedYears)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            var yearsOnly = selectedYears.Where(x => !x.Contains("/")).Distinct().ToList();
            yearsOnly.Sort();
            //there is a whole year selected, process it
            if (yearsOnly.Count > 0)
            {
                foreach (var year in yearsOnly)
                {
                    result.Add(year, selectedYears.Where(x => x.Contains("/") && x.StartsWith(year)).Select(y => y.Substring(y.IndexOf('/') + 1)).ToList());
                }
            }
            else
            {
                yearsOnly = selectedYears.Select(x => x.Split('/')[0]).Distinct().ToList();
                foreach (var year in yearsOnly)
                {
                    result.Add(year, selectedYears.Where(x => x.Contains("/") && x.StartsWith(year)).Select(y => y.Substring(y.IndexOf('/') + 1)).ToList());
                }
            }

            return result;
        }

        private List<TrialBalanceQuery> GenerateSUMQueriesForCategoriesAndYears(Dictionary<string, List<string>> years)
        {
            List<TrialBalanceQuery> result = new List<TrialBalanceQuery>();
            List<string> allRevenues = new List<string>();
            List<string> allCosts = new List<string>();

            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue1Name) && _waterfallContext.RevenueAccounts.Revenue1Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(true, _waterfallContext.RevenueAccounts.Revenue1Name, _waterfallContext.RevenueAccounts.Revenue1Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue1Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue2Name) && _waterfallContext.RevenueAccounts.Revenue2Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(true, _waterfallContext.RevenueAccounts.Revenue2Name, _waterfallContext.RevenueAccounts.Revenue2Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue2Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue3Name) && _waterfallContext.RevenueAccounts.Revenue3Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(true, _waterfallContext.RevenueAccounts.Revenue3Name, _waterfallContext.RevenueAccounts.Revenue3Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue3Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue4Name) && _waterfallContext.RevenueAccounts.Revenue4Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(true, _waterfallContext.RevenueAccounts.Revenue4Name, _waterfallContext.RevenueAccounts.Revenue4Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue4Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs1Name) && _waterfallContext.CostsAccounts.Costs1Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs1Name, _waterfallContext.CostsAccounts.Costs1Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs1Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs2Name) && _waterfallContext.CostsAccounts.Costs2Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs2Name, _waterfallContext.CostsAccounts.Costs2Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs2Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs3Name) && _waterfallContext.CostsAccounts.Costs3Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs3Name, _waterfallContext.CostsAccounts.Costs3Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs3Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs4Name) && _waterfallContext.CostsAccounts.Costs4Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs4Name, _waterfallContext.CostsAccounts.Costs4Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs4Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs5Name) && _waterfallContext.CostsAccounts.Costs5Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs5Name, _waterfallContext.CostsAccounts.Costs5Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs5Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs6Name) && _waterfallContext.CostsAccounts.Costs6Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs6Name, _waterfallContext.CostsAccounts.Costs6Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs6Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs7Name) && _waterfallContext.CostsAccounts.Costs7Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs7Name, _waterfallContext.CostsAccounts.Costs7Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs7Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs8Name) && _waterfallContext.CostsAccounts.Costs8Nodes.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, _waterfallContext.CostsAccounts.Costs8Name, _waterfallContext.CostsAccounts.Costs8Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs8Nodes);
            }

            if (allRevenues.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(true, "TotalRevenue", allRevenues, years));
            }

            if (allCosts.Count > 0)
            {
                result.AddRange(GenerateSingleSUMQuery(false, "TotalCosts", allCosts, years));
            }

            if (allRevenues.Count > 0 && allCosts.Count > 0)
            {
                foreach (var year in years)
                {
                    if (year.Value.Count < 12 && year.Value.Count > 0)
                    {

                        result.Add(new TrialBalanceQuery()
                        {
                            CategoryName = "Gross",
                            IsRevenue = false,
                            Year = year.Key,
                            YearName = $"{year} ({string.Join(",", year.Value)})",
                            Query = $@"(SELECT
                                        (SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT (SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allCosts.Select(x => "'" + x + "'").ToList())}))
                                        ) AS Gross_{year.Key}"
                        });
                    }
                    else
                    {
                        result.Add(new TrialBalanceQuery()
                        {
                            CategoryName = "Gross",
                            IsRevenue = false,
                            Year = year.Key,
                            YearName = $"{year}",
                            Query = $@"(SELECT
                                        (SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT (SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allCosts.Select(x => "'" + x + "'").ToList())}))
                                        ) AS Gross_{year.Key}"
                        });
                    }
                }

                foreach (var year in years)
                {
                    if (year.Value.Count < 12 && year.Value.Count > 0)
                    {

                        result.Add(new TrialBalanceQuery()
                        {
                            CategoryName = "Margin",
                            IsRevenue = false,
                            Year = year.Key,
                            YearName = $"{year} ({string.Join(",", year.Value)})",
                            Query = $@"(SELECT ((SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        /NULLIF(
                                        (SELECT
                                        (SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT (SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allCosts.Select(x => "'" + x + "'").ToList())}))
                                        ),0))) AS Margin_{year.Key}"
                        });
                    }
                    else
                    {
                        result.Add(new TrialBalanceQuery()
                        {
                            CategoryName = "Margin",
                            IsRevenue = false,
                            Year = year.Key,
                            YearName = $"{year}",
                            Query = $@"(SELECT ((SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        /NULLIF(
                                        (SELECT
                                        (SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT (SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allCosts.Select(x => "'" + x + "'").ToList())}))
                                        ),0))) AS Margin_{year.Key}"
                        });
                    }
                }
            }

            return result;
        }

        private List<TrialBalanceQuery> GenerateSingleSUMQuery(bool IsRevenue, string categoryName, List<string> categoryAccounts, Dictionary<string, List<string>> years)
        {
            List<TrialBalanceQuery> result = new List<TrialBalanceQuery>();

            foreach (var year in years)
            {
                if (year.Value.Count < 12 && year.Value.Count > 0)
                {
                    result.Add(new TrialBalanceQuery()
                    {
                        CategoryName = categoryName,
                        IsRevenue = true,
                        Year = year.Key,
                        YearName = $"{year} ({string.Join(",", year.Value)})",
                        Query = IsRevenue?$@"(SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})) AS [{categoryName}_{year.Key}]":
                                        $@"(SELECT (SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})) AS [{categoryName}_{year.Key}]"

                    });
                }
                else
                {
                    result.Add(new TrialBalanceQuery()
                    {
                        CategoryName = categoryName,
                        IsRevenue = true,
                        Year = year.Key,
                        YearName = $"{year}",
                        Query = IsRevenue?$@"(SELECT (-1*SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl 
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})) AS [{categoryName}_{year.Key}]":
                                        $@"(SELECT (SUM(AMT)/1000) FROM SawyerSight.GL_POST_SUM gl 
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})) AS [{categoryName}_{year.Key}]"

                    });
                }
            }

            return result;
        }

        //Trial Balance Summary Methods
        private List<TrialBalanceQuery> GenerateTrialBalanceSummaryQueries(Dictionary<string, List<string>> years)
        {
            List<TrialBalanceQuery> result = new List<TrialBalanceQuery>();
            List<string> allRevenues = new List<string>();
            List<string> allCosts = new List<string>();

            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue1Name) && _waterfallContext.RevenueAccounts.Revenue1Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(true, _waterfallContext.RevenueAccounts.Revenue1Name, _waterfallContext.RevenueAccounts.Revenue1Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue1Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue2Name) && _waterfallContext.RevenueAccounts.Revenue2Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(true, _waterfallContext.RevenueAccounts.Revenue2Name, _waterfallContext.RevenueAccounts.Revenue2Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue2Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue3Name) && _waterfallContext.RevenueAccounts.Revenue3Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(true, _waterfallContext.RevenueAccounts.Revenue3Name, _waterfallContext.RevenueAccounts.Revenue3Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue3Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.RevenueAccounts.Revenue4Name) && _waterfallContext.RevenueAccounts.Revenue4Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(true, _waterfallContext.RevenueAccounts.Revenue4Name, _waterfallContext.RevenueAccounts.Revenue4Nodes, years));
                allRevenues.AddRange(_waterfallContext.RevenueAccounts.Revenue4Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs1Name) && _waterfallContext.CostsAccounts.Costs1Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs1Name, _waterfallContext.CostsAccounts.Costs1Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs1Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs2Name) && _waterfallContext.CostsAccounts.Costs2Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs2Name, _waterfallContext.CostsAccounts.Costs2Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs2Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs3Name) && _waterfallContext.CostsAccounts.Costs3Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs3Name, _waterfallContext.CostsAccounts.Costs3Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs3Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs4Name) && _waterfallContext.CostsAccounts.Costs4Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs4Name, _waterfallContext.CostsAccounts.Costs4Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs4Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs5Name) && _waterfallContext.CostsAccounts.Costs5Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs5Name, _waterfallContext.CostsAccounts.Costs5Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs5Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs6Name) && _waterfallContext.CostsAccounts.Costs6Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs6Name, _waterfallContext.CostsAccounts.Costs6Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs6Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs7Name) && _waterfallContext.CostsAccounts.Costs7Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs7Name, _waterfallContext.CostsAccounts.Costs7Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs7Nodes);
            }
            if (!string.IsNullOrWhiteSpace(_waterfallContext.CostsAccounts.Costs8Name) && _waterfallContext.CostsAccounts.Costs8Nodes.Count > 0)
            {
                result.AddRange(GenerateTrialBalanceSummaryQuery(false, _waterfallContext.CostsAccounts.Costs8Name, _waterfallContext.CostsAccounts.Costs8Nodes, years));
                allCosts.AddRange(_waterfallContext.CostsAccounts.Costs8Nodes);
            }

            if (allRevenues.Count > 0)
            {
                foreach (var year in years)
                {
                    result.Add(new TrialBalanceQuery()
                    {
                        CategoryName = "TotalRevenue",
                        IsRevenue = true,
                        Year = year.Key,
                        YearName = $"{year} ({string.Join(",", year.Value)})",
                        Query = $@"(SELECT '   ' AS ACCT_ID,0 AS BalanceValue , 'TotalRevenue_{year.Key}',' ')"

                    });

                }

            }

           

            if (allCosts.Count > 0)
            {
                foreach (var year in years)
                {
                    result.Add(new TrialBalanceQuery()
                    {
                        CategoryName = "TotalCosts",
                        IsRevenue = false,
                        Year = year.Key,
                        YearName = $"{year} ({string.Join(",", year.Value)})",
                        Query = $@"(SELECT  '   ' AS ACCT_ID, 0 AS BalanceValue , 'TotalCosts_{year.Key}',' ')"

                    });

                }

            }

            if (allRevenues.Count > 0 && allCosts.Count > 0)
            {
                foreach (var year in years)
                {                    
                        result.Add(new TrialBalanceQuery()
                        {
                            CategoryName = "Gross Profit",
                            IsRevenue = false,
                            Year = year.Key,
                            YearName = $"{year} ({string.Join(",", year.Value)})",
                            Query = $@"(SELECT  '   ' AS ACCT_ID, 0 AS BalanceValue , 'Gross_{year.Key}',' ')"
                        });                    
                }

                foreach (var year in years)
                {
                   

                        result.Add(new TrialBalanceQuery()
                        {
                            CategoryName = "Gross Profit",
                            IsRevenue = false,
                            Year = year.Key,
                            YearName = $"{year} ({string.Join(",", year.Value)})",
                            Query = $@"(SELECT '   ' AS ACCT_ID, 0 AS BalanceValue , 'Margin_{year.Key}',' ')"
                        });
                    
                }
            }






            return result;
        }


        private List<TrialBalanceQuery> GenerateTrialBalanceSummaryQuery(bool IsRevenue, string categoryName, List<string> categoryAccounts, Dictionary<string, List<string>> years)
        {
            List<TrialBalanceQuery> result = new List<TrialBalanceQuery>();

            foreach (var year in years)
            {
                if (year.Value.Count < 12 && year.Value.Count > 0)
                {
                    result.Add(new TrialBalanceQuery()
                    {
                        CategoryName = categoryName,
                        IsRevenue = true,
                        Year = year.Key,
                        YearName = $"{year} ({string.Join(",", year.Value)})",
                        Query = IsRevenue ?  $@"(SELECT ACCT_ID, (-1*SUM(AMT)/1000),'{categoryName}_{year.Key}',(SELECT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) FROM SawyerSight.GL_POST_SUM gl
                                              WHERE Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											  WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID)
                                              AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID)  
                                              
                                              UNION ALL 

										      (SELECT '   ' AS ACCT_ID,0 AS BalanceValue,'{categoryName}_{year.Key}',' ' 										 
										      WHERE NOT Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											   WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID))" :


                                             $@"(SELECT  ACCT_ID, (SUM(AMT)/1000),'{categoryName}_{year.Key}',(SELECT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) FROM SawyerSight.GL_POST_SUM gl
                                              WHERE Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											   WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID)
                                              AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID) 
                                             
                                              UNION ALL 

										     (SELECT '   ' AS ACCT_ID,0 AS BalanceValue,'{categoryName}_{year.Key}',' '									 
										      WHERE NOT Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											   WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID))"

                    });
                }
                else
                {
                    result.Add(new TrialBalanceQuery()
                    {
                        CategoryName = categoryName,
                        IsRevenue = true,
                        Year = year.Key,
                        YearName = $"{year}",
                        Query = IsRevenue ?     $@"(SELECT ACCT_ID, (-1*SUM(AMT)/1000),'{categoryName}_{year.Key}',(SELECT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) FROM SawyerSight.GL_POST_SUM gl 
                                                WHERE Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											    WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}' 
                                                AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID)                                       
                                                AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}' 
                                                AND gl.ACCT_ID IN ({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID) 
                                               
                                                UNION ALL 

										        (SELECT '   ' AS ACCT_ID,0 AS BalanceValue,'{categoryName}_{year.Key}',' '										 
										        WHERE NOT Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											    WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}' 
                                                AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID))" :

                                                $@"(SELECT ACCT_ID,(SUM(AMT)/1000),'{categoryName}_{year.Key}',(SELECT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) FROM SawyerSight.GL_POST_SUM gl 
                                                WHERE Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											    WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}' 
                                                AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID)                                         
                                                AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID}
                                                AND gl.FY_CD='{year.Key}' 
                                                AND gl.ACCT_ID IN ({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})  group by ACCT_ID)
                                                
                                                UNION ALL 

										       (SELECT '   ' AS ACCT_ID,0 AS BalanceValue,'{categoryName}_{year.Key}',' '									 
										        WHERE NOT Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											    WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}' 
                                                                     AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID))"

                    });
                }
            }

            return result;
        }

    }
}