using SawyerSight.Models.DAL;
using SawyerSight.Models.ViewModel;
using SawyerSight.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Helpers
{
    public static class ViewModelBuilder
    {
        public static List<OrganizationVM> BuildOrganizationsTree(IEnumerable<Organization> organizations)
        {
            List<OrganizationVM> result = new List<OrganizationVM>();
            var rootOrganizations = organizations.Where(x => x.Id == x.ParentId).ToList();
            if (rootOrganizations.Count == 0) return result;

            foreach(var org in rootOrganizations)
            {
                var orgVM = new OrganizationVM()
                {
                    OrganizationID = org.Id,
                    OrganizationName = org.Name,
                    ParentID = org.ParentId,
                    ActiveFlag = org.ActiveFlag,
                    ReportDataSetID = org.ReportDataSetID
                };
                orgVM.hasChildren = BuildChildren(orgVM, organizations).Count > 0 ? true : false ;
                result.Add(orgVM);
            }
            return result;
        }

        private static List<OrganizationVM> BuildChildren(OrganizationVM parent, IEnumerable<Organization> organizations)
        {
            List<OrganizationVM> result = new List<OrganizationVM>();
            //this is the leaf
            if (organizations.Where(x => x.ParentId == parent.OrganizationID && x.Id != parent.OrganizationID).Count() == 0)
            {
                return new List<OrganizationVM>();
            }
            else
            {
                var children = organizations.Where(x => x.ParentId == parent.OrganizationID && x.Id != parent.OrganizationID).ToList();
                foreach(var child in children)
                {
                    var childVM = new OrganizationVM()
                    {
                        OrganizationID = child.Id,
                        OrganizationName = child.Name,
                        ParentID = child.ParentId,
                        ActiveFlag = child.ActiveFlag,
                        ReportDataSetID=child.ReportDataSetID
                    };
                    childVM.hasChildren=BuildChildren(childVM, organizations).Count > 0 ? true : false;
                    result.Add(childVM);
                }
            }
            return result;
        }

        public static List<Demographics> GetDemographics(AppSessionContext appSession)
        {
            return new List<Demographics>()
            {
                new Demographics() { Id = "PROJ_NAME", Name = "Contract Name", Checked=appSession.Waterfall.SelectedDemographics.Contains("PROJ_NAME")?"checked":""},
                new Demographics() { Id = "PRIME_CONTR_ID", Name = "Contract Number", Checked=appSession.Waterfall.SelectedDemographics.Contains("PRIME_CONTR_ID")?"checked":"" },
                new Demographics() { Id = "PROJ_START_DT", Name = "Start Date", Checked=appSession.Waterfall.SelectedDemographics.Contains("PROJ_START_DT")?"checked":"" },
                new Demographics() { Id = "PROJ_END_DT", Name = "End Date", Checked=appSession.Waterfall.SelectedDemographics.Contains("PROJ_END_DT")?"checked":"" },
                new Demographics() { Id = "CUST_NAME", Name = "End Customer", Checked=appSession.Waterfall.SelectedDemographics.Contains("CUST_NAME")?"checked":"" },
                new Demographics() { Id = "PRIME_CONTR_FL", Name = "Prime / Sub", Checked=appSession.Waterfall.SelectedDemographics.Contains("PRIME_CONTR_FL")?"checked":"" },
                new Demographics() { Id = "PROJ_TYPE_DC", Name = "Type", Checked=appSession.Waterfall.SelectedDemographics.Contains("PROJ_TYPE_DC")?"checked":"" },
                new Demographics() { Id = "PROJ_V_TOT_AMT", Name = "Total Contract Value", Checked=appSession.Waterfall.SelectedDemographics.Contains("PROJ_V_TOT_AMT")?"checked":"" },
                new Demographics() { Id = "", Name = "Total Backlog", Checked=appSession.Waterfall.SelectedDemographics.Contains("Total Backlog")?"checked":"" },
                new Demographics() { Id = "PROJ_F_TOT_AMT", Name = "Funded Contract Value", Checked=appSession.Waterfall.SelectedDemographics.Contains("PROJ_F_TOT_AMT")?"checked":"" },
                new Demographics() { Id = "", Name = "Funded Backlog", Checked=appSession.Waterfall.SelectedDemographics.Contains("Funded Backlog")?"checked":"" }
            };
        }
    }
}