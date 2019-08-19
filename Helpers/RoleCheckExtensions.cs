using SawyerSight.Web.Controllers;
using SawyerSight.Web.DAL;
using System;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace System.Web.WebPages
{
    public static class RoleCheckExtensions
    {
        public static bool IsAdmin(this WebPageRenderingBase webPage)
        {
            return CurrentUser.IsAdmin;
        }
    }   
}

namespace SawyerSight.Web.DAL
{
    public static class CurrentUser
    {
        public static string Upn()
        {
            var claimsIdentity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var emailClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if(emailClaim != null && !string.IsNullOrWhiteSpace(emailClaim.Value))
                {
                    return emailClaim.Value.Trim();
                }
            }

            return HttpContext.Current.User.Identity.Name;
        }

        public static bool IsInRole(string roleName)
        {
            var claimsIdentity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                return claimsIdentity.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value.Equals(roleName, StringComparison.InvariantCultureIgnoreCase));
            }

            return false;
        }

        public static bool IsAdmin
        {
            get
            {
                return IsInRole(RoleType.Admin);
            }
        }
    }
}