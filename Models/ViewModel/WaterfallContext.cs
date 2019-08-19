using SawyerSight.Models;
using SawyerSight.Models.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ViewModel
{
    public class WaterfallContext
    {
        public int ClientUniqueID { get; set; }
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public int ReportDataSetID { get; set; }
        public string ProjectName { get; set; }
        public string ClientArchiveURL { get; set; }
        public int ProjectsMaxLevel { get; set; }
        public int SelectedProjectsLevel { get; set; }
        public TreeListChecks SelectedOrganizations { get; set; }
        public TreeListChecks SelectedProjects { get; set; }
        public List<string> SelectedFiscalYears { get; set; }
        public List<string> SelectedDemographics { get; set; }
        public SelectedRevenueAccounts RevenueAccounts { get; set; }
        public SelectedCostsAccounts CostsAccounts { get; set; }

        public WaterfallContext()
        {
            ClientUniqueID = 0;
            ClientID = "";
            ClientName = "";
            ReportDataSetID = 0;
            ProjectName = "";
            ProjectsMaxLevel = 0;
            SelectedProjectsLevel = 0;
            SelectedOrganizations = new TreeListChecks();
            SelectedProjects = new TreeListChecks();
            SelectedFiscalYears = new List<string>();
            SelectedDemographics = new List<string>();
            RevenueAccounts = new SelectedRevenueAccounts();
            CostsAccounts = new SelectedCostsAccounts();
        }
    }

    public class TreeListChecks
    {
        public List<string> CheckedNodes { get; set; }
        public List<string> VisibleUncheckedNodes { get; set; }
        public bool SelectAllActive { get; set; }
        public bool SelectAllInactive { get; set; }

        public TreeListChecks()
        {
            CheckedNodes = new List<string>();
            VisibleUncheckedNodes = new List<string>();
            SelectAllActive = false;
            SelectAllInactive = false;
        }
    }

    public class AccountCategories
    {
        public string CategoryName { get; set; }
        public int AccountType { get; set; }
        public List<string> SelectedAccounts { get; set; }

        public AccountCategories()
        {
            SelectedAccounts = new List<string>();
        }
    }

    public static class Processor
    {
        public static List<Project> ProcessSelectedOrganizationsIntoProjects(IEnumerable<Project> fullList, TreeListChecks checks)
        {
            List<Project> result = new List<Project>();

            //if All Active are selected, add them
            if (checks.SelectAllActive)
            {
                result.AddRange(fullList.Where(x => x.ActiveFlag == "Y").ToList());
            }

            //if All Inactive are selected, add them
            if(checks.SelectAllInactive)
            {
                result.AddRange(fullList.Where(x => x.ActiveFlag == "N").ToList());
            }

            //Add All that start with
            foreach(var el in checks.CheckedNodes)
            {
                result.AddRange(fullList.Where(x => x.OrgId.StartsWith(el)).ToList());
            }
            foreach(var el in checks.VisibleUncheckedNodes)
            {
                //if there are checked children of the element, remove the parent only
                if(checks.CheckedNodes.Where(x=>x.StartsWith(el)).Count()>0)
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
    }

}