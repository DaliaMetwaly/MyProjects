using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class FY: ETLGeneric
    {
        public string FY_CD { get; set; }
        public int FY_SEQ_NO { get; set; }
        public string S_STATUS_CD { get; set; }
        public string FY_DESC { get; set; }
        public string S_CLOSE_ACT_TGT_CD { get; set; }
        public int ReportDataSetID { get; set; }
    }
}