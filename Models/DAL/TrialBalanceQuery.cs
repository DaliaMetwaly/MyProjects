using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Models.DAL
{
    public class TrialBalanceQuery
    {
        public string CategoryName{ get; set; }
        public string Year { get; set; }
        public string YearName { get; set; }
        public string Query { get; set; }
        public bool IsRevenue { get; set; }
    }
}