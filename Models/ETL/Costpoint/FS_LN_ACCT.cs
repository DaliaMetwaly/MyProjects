using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class FS_LN_ACCT: ETLGeneric
    {
        public string FS_CD { get; set; }
        public int FS_LN_KEY { get; set; }
        public string ACCT_ID { get; set; }
        public int ReportDataSetID { get; set; }
    }
}