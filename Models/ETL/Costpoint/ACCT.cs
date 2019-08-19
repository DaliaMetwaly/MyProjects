using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class ACCT:ETLGeneric
    {
        public string ACCT_ID { get; set; }
        public string ACCT_NAME { get; set; }
        public int ReportDataSetID { get; set; }
    }
}