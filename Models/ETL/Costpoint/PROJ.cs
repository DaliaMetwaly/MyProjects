using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class PROJ: ETLGeneric
    {
        public string PROJ_ID { get; set; }
        public string ORG_ID { get; set; }
        public string PROJ_NAME { get; set;}
        public string ACTIVE_FL { get; set; }
        public string PRIME_CONTR_ID { get; set; }
        public DateTime PROJ_START_DT { get; set; }
        public DateTime PROJ_END_DT { get; set; }
        public string CUST_ID { get; set; }
        public int LVL_NO { get; set; }
        public string PRIME_CONTR_FL { get; set; }
        public decimal PROJ_V_TOT_AMT { get; set; }
        public decimal PROJ_F_TOT_AMT { get; set; }
        public string PROJ_TYPE_DC { get; set; }
        public string SUB_CONTR_FL { get; set; }
        public int ReportDataSetID { get; set; }
    }
}