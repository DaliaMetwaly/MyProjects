using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using SawyerSight.DAL.Repositories;
using SawyerSight.Graph;
using SawyerSight.Graph.Models.FolderItems;
using SawyerSight.Graph.Models.FolderPermissions;
using SawyerSight.Graph.Models.FoldersExplorer;
using SawyerSight.Graph.Models.InvitationResponseNonUser;
using SawyerSight.Graph.Models.InvitationResponseUser;
using SawyerSight.Models;
using SawyerSight.Models.DAL;
using SawyerSight.Web.DAL;
using SawyerSight.Web.Filters;
using SawyerSight.Web.Helpers;
using SawyerSight.Web.Logic;
using SawyerSight.Web.Models;
using SawyerSight.Web.Models.DAL;
using SawyerSight.Web.Models.ViewModel;
using SawyerSight.Web.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Link = SawyerSight.Graph.Models.FolderPermissions.Link;


namespace SawyerSight.Web.Controllers
{
    [LogMVCExceptionFilter]
    [MessageCleanupFilter]
    [Secure(RoleType.Admin, RoleType.User)]
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            return RedirectToAction("SelectClient", "Waterfall");
        }
        
      
    }
}