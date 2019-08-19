using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Models.ViewModel
{
    public class OrganizationVM
    {
        public string OrganizationID { get; set; }
        public string OrganizationName { get; set; }
        public string ParentID { get; set; }
        public string ActiveFlag { get; set; }
        public int ReportDataSetID { get; set; }
        public bool hasChildren { get; set; }
        //public List<OrganizationVM> subOrganizations { get; set; }
    }
}