using SawyerSight.Web.Filters;
using System.Web.Mvc;

namespace SawyerSight.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new LogMVCExceptionFilterAttribute());
            filters.Add(new MessageCleanupFilterAttribute());
        }

        public static void RegisterGlobalAPIFilters(System.Web.Http.Filters.HttpFilterCollection filters)
        {
            filters.Add(new LogWebAPIExceptionFilterAttribute());
        }        
    }
}
