using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Models.DAL
{
    public class User
    {
        public string UPN { get; set; }
        public string DisplayName { get; set; }
        public string ClientID { get; set; }
    }
}