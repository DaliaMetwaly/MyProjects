using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class FS_LN: ETLGeneric
    {
        public string FS_CD { get; set; }
        public int FS_LN_KEY { get; set; }
        public int FS_MAJOR_NO { get; set; }
        public int FS_GRP_NO { get; set; }
        public int FS_LN_NO { get; set; }
        public string FS_MAJOR_DESC { get; set; }
        public string FS_GRP_DESC { get; set; }
        public string FS_LN_DESC { get; set; }
        public int ReportDataSetID { get; set; }
    }
}