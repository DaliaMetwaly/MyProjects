using System;
using System.Configuration;
using System.IdentityModel.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Owin;
using SawyerSight.Web.Models;
using SawyerSight.Web.Helpers;
using SawyerSight.DAL.Repositories;
using System.Linq;

namespace SawyerSight.Web
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private static string aadInstance = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        public static readonly string Authority = aadInstance + tenantId;

        // This is the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
        string graphResourceId = "https://graph.windows.net";

        public void ConfigureAuth(IAppBuilder app)
        {
           
            app.Use((context, next) => { context.Request.Scheme = "https"; return next(); });

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = Authority,
                    PostLogoutRedirectUri = postLogoutRedirectUri,

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
                        AuthorizationCodeReceived = (context) =>
                        {
                            try
                            {
                                var code = context.Code;
                                ClientCredential credential = new ClientCredential(clientId, appKey);
                                string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
                                AuthenticationContext authContext = new AuthenticationContext(Authority, new ADALTokenCache(signedInUserID));
                                AuthenticationResult result = authContext.AcquireTokenByAuthorizationCodeAsync(
                                    code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, graphResourceId).Result;

                                var userService = (IUserDataService)UnityConfig.DefaultContainer.Resolve(typeof(IUserDataService), null, null);
                                string upn = result.UserInfo.DisplayableId;

                                if (userService.GetUser(upn) == null)
                                {
                                    userService.AddUser(new SawyerSight.Models.DAL.User()
                                    {
                                        UPN = upn,
                                        DisplayName = upn,
                                        ClientID = signedInUserID
                                    });                                    
                                }                                

                                var roles = userService.GetRoles(upn).ToList();

                                if (roles.Any())
                                { 
                                    roles.ForEach(roleName => context.AuthenticationTicket.Identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName)));
                                }
                            }
                            catch (Exception ex)
                            {
                                ExceptionLogger.LogError(ex, "AuthorizationCodeReceived failed.");
                            }
                            return Task.FromResult(0);
                        }
                    }
                });
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
