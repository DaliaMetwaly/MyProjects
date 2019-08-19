using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Models.DAL
{
    public class GlPostSum
    {
        public string AccountID { get; set;}
        public string ProjectID { get; set; }
        public string OrganizationID { get; set; }
        public string FiscalYear { get; set; }
        public int FiscalMonth { get; set; }
        public double Amount { get; set; }
        public int ReportDataSetID { get; set; }
    }
}