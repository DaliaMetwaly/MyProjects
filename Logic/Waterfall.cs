using Newtonsoft.Json;
using OfficeOpenXml;
using SawyerSight.Models.DAL;
using SawyerSight.Web.DAL;
using SawyerSight.Web.Helpers;
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
    public class Waterfall
    {
        private readonly IStageDataService _etlService;
        private readonly IMigrationDataService _migrationService;
        private readonly WaterfallContext _waterfallContext;
        int itemsCount;
        int itemsProgress;

        public List<TrialBalanceQuery> GeneratedQueries;
        public Dictionary<string, List<string>> ProcessedFiscalYears;
        public Waterfall(WaterfallContext waterfallContext)
        {
            _etlService = (IStageDataService)UnityConfig.DefaultContainer.Resolve(typeof(IStageDataService), null, null);
            _migrationService = (IMigrationDataService)UnityConfig.DefaultContainer.Resolve(typeof(IMigrationDataService), null, null);
            _waterfallContext = waterfallContext;
            itemsCount = 8;
            itemsProgress = 1;
        }

        public Waterfall(IStageDataService etlService, IMigrationDataService migrationService, WaterfallContext waterfallContext)
        {
            _etlService = etlService;
            _migrationService = migrationService;
        }
        //---------------------Generate Waterfall---------------------------------
        public List<DataTable> Generate()
        {
                List<DataTable> reports = new List<DataTable>();
                var yearsAndMonths = ProcessFiscalYears(_waterfallContext.SelectedFiscalYears);
                ProcessedFiscalYears = yearsAndMonths;
                var trialBalanceQueries = GenerateSUMQueriesForCategoriesAndYears(yearsAndMonths);
                GeneratedQueries = trialBalanceQueries;
                _waterfallContext.SelectedDemographics.RemoveAll(x => string.IsNullOrWhiteSpace(x));
           
                string demographicsSelect = string.Join(",", _waterfallContext.SelectedDemographics)+"," ;

                var waterfallQuery = 
                $@"
                    Select  PROJ_ID AS [Project Id], {demographicsSelect}" + string.Join(",", trialBalanceQueries.Select(x => x.Query).ToList()) + $@" 
                    FROM SawyerSight.PROJ pr
                    JOIN SawyerSight.CUST c on pr.CUST_ID=c.CUST_ID
                    WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
                    AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                    AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
                 ";

            var trialBalanceQuery =
                $@"SELECT * FROM (
                    { string.Join("UNION ALL", GenerateTrialBalanceSummaryQueries(yearsAndMonths).Select(x => x.Query).ToList())}
                  ) AS Temp Order By ACCT_ID";
            SignalRProcessor.SendImportUpdate("Generating Waterfall Report", itemsProgress++, itemsCount);
           
            reports.Add(_migrationService.GenerateWaterfall(waterfallQuery));
            reports.Add(_migrationService.GenerateWaterfall(trialBalanceQuery));


            //pass years data table to customize column names
            DataTable yearsDataTable = new DataTable();
            //add columns to table
            for (int i = 0; i < ProcessedFiscalYears.Keys.Count(); i++)
            {
                yearsDataTable.Columns.Add(ProcessedFiscalYears.Keys.ElementAt(i));
            }
           
            for (int j = 0; j < 12; j++)
            {
                 
                DataRow newRow = yearsDataTable.Rows.Add();
                foreach (var key in ProcessedFiscalYears.Keys)
                {
                    if (ProcessedFiscalYears[key].Count > j)
                        newRow[key] = ProcessedFiscalYears[key][j];
                }
            }

            reports.Add(yearsDataTable);
            SignalRProcessor.SendImportUpdate("Generating Waterfall Report Finished. You will be redirected to the Excel Preview.", itemsCount, itemsCount);
            return reports;
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
                                        (SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT ISNULL((SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
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
                                        (SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT ISNULL((SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
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
                            Query = $@"ISNULL((SELECT ((SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        /NULLIF(
                                        (SELECT
                                        (SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT ISNULL((SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allCosts.Select(x => "'" + x + "'").ToList())}))
                                        ),0))),0) AS Margin_{year.Key}"
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
                            Query = $@"ISNULL((SELECT ((SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        /NULLIF(
                                        (SELECT
                                        (SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allRevenues.Select(x => "'" + x + "'").ToList())}))
                                        -
                                        (SELECT ISNULL((SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", allCosts.Select(x => "'" + x + "'").ToList())}))
                                        ),0))),0) AS Margin_{year.Key}"
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
                        Query = IsRevenue ? $@"(SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})) AS [{categoryName}_{year.Key}]" :
                                        $@"(SELECT ISNULL((SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl
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
                        Query = IsRevenue ? $@"(SELECT ISNULL((-1*SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl 
                                        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} AND gl.FY_CD='{year.Key}' AND gl.PROJ_ID IN (SELECT CHILD FROM SawyerSight.PROJ_Cross_Mappings WHERE PARENT = pr.PROJ_ID AND ReportDataSetID = {_waterfallContext.ReportDataSetID}) AND gl.ACCT_ID IN ({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})) AS [{categoryName}_{year.Key}]" :
                                        $@"(SELECT ISNULL((SUM(AMT)/1000),0) FROM SawyerSight.GL_POST_SUM gl 
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
                        Query = IsRevenue ? $@"(SELECT ACCT_ID,(SELECT DISTINCT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) AS ACCT_NAME, ISNULL((-1*SUM(AMT)/1000),0) AS BalanceValue,'{categoryName}_{year.Key}' AS AccountType FROM SawyerSight.GL_POST_SUM gl
                                              WHERE Exists
                                                      (
                                                            SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											                WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                            AND gl.FY_CD='{year.Key}' 
                                                            AND gl.PROJ_ID IN 
											                (
											                   SELECT CHILD 
											                	FROM  SawyerSight.PROJ pr  
											                	JOIN SawyerSight.CUST c 
											                	on pr.CUST_ID=c.CUST_ID
											                	AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
											                	JOIN SawyerSight.PROJ_Cross_Mappings   
											                	ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
											                	WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
											                	AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                                AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                            )
                                                            AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                                            AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID
                                                       )
                                              AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND gl.PROJ_ID IN 
											 (
											    SELECT CHILD 
												FROM  SawyerSight.PROJ pr  
												JOIN SawyerSight.CUST c 
												on pr.CUST_ID=c.CUST_ID
												AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												JOIN SawyerSight.PROJ_Cross_Mappings   
												ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                             )
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID)  
                                              
                                              UNION ALL 

										      (SELECT '   ' AS ACCT_ID,' ' AS ACCT_NAME,0 AS BalanceValue,'{categoryName}_{year.Key}' AS AccountType 										 
										      WHERE NOT Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											  WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}'
                                              AND gl.PROJ_ID IN 
											   (
											    SELECT CHILD 
												FROM  SawyerSight.PROJ pr  
												JOIN SawyerSight.CUST c 
												on pr.CUST_ID=c.CUST_ID
												AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												JOIN SawyerSight.PROJ_Cross_Mappings   
												ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                )
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID))" :


                                             $@"(SELECT  ACCT_ID,(SELECT DISTINCT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) AS ACCT_NAME, ISNULL((SUM(AMT)/1000),0) AS BalanceValue ,'{categoryName}_{year.Key}' AS AccountType FROM SawyerSight.GL_POST_SUM gl
                                              WHERE Exists
                                                        (
                                                         SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											             WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                         AND gl.FY_CD='{year.Key}' 
                                                         AND gl.PROJ_ID IN 
											               (
											               SELECT CHILD 
											            	FROM  SawyerSight.PROJ pr  
											            	JOIN SawyerSight.CUST c 
											            	on pr.CUST_ID=c.CUST_ID
											            	AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
											            	JOIN SawyerSight.PROJ_Cross_Mappings   
											            	ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
											            	WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
											            	AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                            AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                           )
                                                         AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                                         AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID

                                                         )
                                              AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}' 
                                              AND gl.PROJ_ID IN 
											               (
											               SELECT CHILD 
											            	FROM  SawyerSight.PROJ pr  
											            	JOIN SawyerSight.CUST c 
											            	on pr.CUST_ID=c.CUST_ID
											            	AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
											            	JOIN SawyerSight.PROJ_Cross_Mappings   
											            	ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
											            	WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
											            	AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                            AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                           )
                                              AND PD_NO IN(0,{ string.Join(",", year.Value.ToArray()) }) 
                                              AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID) 
                                             
                                              UNION ALL 

										     (SELECT '   ' AS ACCT_ID,' ' AS ACCT_NAME,0 AS BalanceValue,'{categoryName}_{year.Key}' AS AccountType									 
										      WHERE NOT Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											  WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                              AND gl.FY_CD='{year.Key}'
                                              AND gl.PROJ_ID IN 
											   (
											    SELECT CHILD 
												FROM  SawyerSight.PROJ pr  
												JOIN SawyerSight.CUST c 
												on pr.CUST_ID=c.CUST_ID
												AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												JOIN SawyerSight.PROJ_Cross_Mappings   
												ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                )
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
                        Query = IsRevenue ? $@"(SELECT ACCT_ID,(SELECT DISTINCT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) AS ACCT_NAME, ISNULL((-1*SUM(AMT)/1000),0) AS BalanceValue,'{categoryName}_{year.Key}' AS AccountType FROM SawyerSight.GL_POST_SUM gl 
                                                WHERE Exists
                                                      (
                                                        SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											            WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                        AND gl.FY_CD='{year.Key}'
                                                        AND gl.PROJ_ID IN 
											            (
											            SELECT CHILD 
												        FROM  SawyerSight.PROJ pr  
												        JOIN SawyerSight.CUST c 
												        on pr.CUST_ID=c.CUST_ID
												        AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												        JOIN SawyerSight.PROJ_Cross_Mappings   
												        ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												        WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												        AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        )
                                                        AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID
                                                       )                                       
                                                AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}' 
                                                AND gl.PROJ_ID IN 
											            (
											            SELECT CHILD 
												        FROM  SawyerSight.PROJ pr  
												        JOIN SawyerSight.CUST c 
												        on pr.CUST_ID=c.CUST_ID
												        AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												        JOIN SawyerSight.PROJ_Cross_Mappings   
												        ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												        WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												        AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        )
                                                AND gl.ACCT_ID IN ({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID) 
                                               
                                                UNION ALL 

										        (SELECT '   ' AS ACCT_ID,' ' AS ACCT_NAME,0 AS BalanceValue,'{categoryName}_{year.Key}'	AS AccountType									 
										        WHERE NOT Exists
                                                  (
                                                SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											    WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}'
                                                AND gl.PROJ_ID IN 
											       (
											            SELECT CHILD 
												        FROM  SawyerSight.PROJ pr  
												        JOIN SawyerSight.CUST c 
												        on pr.CUST_ID=c.CUST_ID
												        AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												        JOIN SawyerSight.PROJ_Cross_Mappings   
												        ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												        WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												        AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                   )
                                                AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID
                                                  )
                                                )" :

                                                $@"(SELECT ACCT_ID,(SELECT DISTINCT [ACCT_NAME] FROM [SawyerSight].[ACCT] WHERE [ACCT_ID]=gl.ACCT_ID) AS ACCT_NAME,ISNULL((SUM(AMT)/1000),0) AS BalanceValue,'{categoryName}_{year.Key}' AS AccountType FROM SawyerSight.GL_POST_SUM gl 
                                                WHERE Exists
                                                (
                                                    SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											        WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                    AND gl.FY_CD='{year.Key}'
                                                    AND gl.PROJ_ID IN 
											         (
											            SELECT CHILD 
												        FROM  SawyerSight.PROJ pr  
												        JOIN SawyerSight.CUST c 
												        on pr.CUST_ID=c.CUST_ID
												        AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												        JOIN SawyerSight.PROJ_Cross_Mappings   
												        ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												        WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												        AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                     )
                                                    AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID

                                                )                                         
                                                AND gl.ReportDataSetID={_waterfallContext.ReportDataSetID}
                                                AND gl.FY_CD='{year.Key}'
                                                AND gl.PROJ_ID IN 
											         (
											            SELECT CHILD 
												        FROM  SawyerSight.PROJ pr  
												        JOIN SawyerSight.CUST c 
												        on pr.CUST_ID=c.CUST_ID
												        AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												        JOIN SawyerSight.PROJ_Cross_Mappings   
												        ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												        WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												        AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                     )
                                                AND gl.ACCT_ID IN ({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())})  group by ACCT_ID)
                                                
                                                UNION ALL 

										       (SELECT '   ' AS ACCT_ID,' ' AS ACCT_NAME,0 AS BalanceValue,'{categoryName}_{year.Key}' AS AccountType									 
										        WHERE NOT Exists(SELECT ACCT_ID FROM SawyerSight.GL_POST_SUM gl 
											    WHERE gl.ReportDataSetID={_waterfallContext.ReportDataSetID} 
                                                AND gl.FY_CD='{year.Key}'
                                                     AND gl.PROJ_ID IN 
											         (
											         SELECT CHILD 
												        FROM  SawyerSight.PROJ pr  
												        JOIN SawyerSight.CUST c 
												        on pr.CUST_ID=c.CUST_ID
												        AND c.ReportDataSetID={_waterfallContext.ReportDataSetID}
												        JOIN SawyerSight.PROJ_Cross_Mappings   
												        ON pr.PROJ_ID = PROJ_Cross_Mappings.PARENT 
												        WHERE LVL_NO = {_waterfallContext.SelectedProjectsLevel} 
												        AND pr.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                        AND PROJ_Cross_Mappings.ReportDataSetID = {_waterfallContext.ReportDataSetID}
                                                     )
                                                AND gl.ACCT_ID IN({string.Join(",", categoryAccounts.Select(x => "'" + x + "'").ToList())}) group by ACCT_ID))"

                    });
                }
            }
            return result;
        }     
    }
}