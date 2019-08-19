using SawyerSight.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace SawyerSight.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            UnityConfig.RegisterComponents();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            FilterConfig.RegisterGlobalAPIFilters(GlobalConfiguration.Configuration.Filters);            
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Environment.SetEnvironmentVariable("BASEDIR", AppDomain.CurrentDomain.BaseDirectory);
        }

        protected void Application_Error()
        {
            // Exception exception = Server.GetLastError();
            //Response.Clear();

            //HttpException httpException = exception as HttpException;
            //if (httpException != null)
            //{
            //    RouteData routeData = new RouteData();
            //    routeData.Values.Add("controller", "Error");
            //    if (httpException.GetHttpCode() == 404)
            //    {

            //        return;
            //    }

            //    ExceptionLogger.LogError(exception, "Application HTTP Error");
            //    routeData.Values.Add("action", "Index");
            //    routeData.Values.Add("error", exception);
            //    Response.RedirectToRoute(routeData);
            //}
            //Server.ClearError();
        }

        void Session_Start(object sender, EventArgs e)
        {
            // place holder to solve endless loop issue
        }
    }
}
