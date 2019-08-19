using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;
namespace SawyerSight.Web.Controllers
{
    public class ErrorController : Controller
    {
        [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)] // will disable caching
        public ActionResult Unauthorized()
        {
            ViewBag.Title = "Error";            
            return View();
        }
       
    }
}