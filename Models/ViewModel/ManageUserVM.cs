using SawyerSight.Models.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.Mvc;
using System.ComponentModel;

namespace SawyerSight.Web.Models.DAL
{
    public class ManageUserVM
    {        
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid e-mail adress")]
        [Required(ErrorMessage = "User name is required")]   
        public string UPN { get; set; }
        public string DisplayName { get; set; }
        [UIHint("UserRoles")]
        public SawyerSight.Web.Models.DAL.Roles UserRoles { get; set; } 
        [UIHint("UserClients")]
        public IEnumerable<Client> UserClients { get; set; }
    }
}