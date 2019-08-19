using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class GL_POST_SUM: ETLGeneric
    {
        public string ACCT_ID { get; set;}
        public string ORG_ID { get; set; }
        public string PROJ_ID { get; set; }
        public string FY_CD { get; set; }
        public decimal AMT { get; set; }
        public int ReportDataSetID { get; set; }
    }
}