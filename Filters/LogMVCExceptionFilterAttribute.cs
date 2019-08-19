using SawyerSight.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc.Filters;
using System.Web.Mvc;


namespace SawyerSight.Web.Filters
{
    public class LogMVCExceptionFilterAttribute:  FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                ExceptionLogger.LogError(filterContext.Exception,"Controller Method error");
                filterContext.ExceptionHandled = false;
            }
        }
    }

}