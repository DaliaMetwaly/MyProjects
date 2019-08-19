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

    public class ETLToLive
    {
        private readonly IStageDataService _etlService;
        private readonly IMigrationDataService _migrationService;
        private readonly WaterfallContext _waterfallContext;
        
        public ETLToLive(WaterfallContext waterfallContext)
        {
            _etlService = (IStageDataService)UnityConfig.DefaultContainer.Resolve(typeof(IStageDataService), null, null);
            _migrationService = (IMigrationDataService)UnityConfig.DefaultContainer.Resolve(typeof(IMigrationDataService), null, null);
            _waterfallContext = waterfallContext;

        }

        public ETLToLive(IStageDataService etlService, IMigrationDataService migrationService, WaterfallContext waterfallContext)
        {
            _etlService = etlService;
            _migrationService = migrationService;
        }

        public void Export()
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
        

    }
}