using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class ORG: ETLGeneric
    {
        public string ORG_ID { get; set; }
        public string ORG_NAME { get; set; }
        public int ReportDataSetID { get; set; }
    }
}