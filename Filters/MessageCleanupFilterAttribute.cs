using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SawyerSight.Web.Filters
{
    public class MessageCleanupFilterAttribute:FilterAttribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //filterContext.Controller.TempData["ErrorMessage"] = null;
            //filterContext.Controller.ViewBag.SuccessMessage = null;
            //filterContext.Controller.ViewBag.GeneralMessage = null;
        }
    }
    
}