using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SawyerSight.Web.Controllers
{
    public static class RoleType
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public class SecureAttribute: AuthorizeAttribute
    {       
        public SecureAttribute(params string[] roles)
        {
            Roles = roles ?? new [] { RoleType.User };
        }               

        protected new string[] Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {            
            bool isAuthorized = base.AuthorizeCore(httpContext);
            if (!isAuthorized)
            {
                return false;
            }

            var claimsIdentity = HttpContext.Current.User.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {                
                return Roles.Any(roleName => claimsIdentity.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)));
            }

            return false;
        }

        protected override void HandleUnauthorizedRequest(System.Web.Mvc.AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Error", action = "Unauthorized" }));
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}