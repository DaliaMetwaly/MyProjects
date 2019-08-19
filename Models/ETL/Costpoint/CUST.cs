﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL.Costpoint
{
    public class CUST:ETLGeneric
    {
        public string CUST_ID { get; set; }
        public string CUST_NAME { get; set; }
        public int ReportDataSetID { get; set; }
    }
}