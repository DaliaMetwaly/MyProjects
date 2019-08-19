using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using SawyerSight.DAL.Repositories;
using SawyerSight.Web.Filters;
using SawyerSight.Web.Models.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SawyerSight.Web.Controllers
{
    [LogMVCExceptionFilter]
    [MessageCleanupFilter]
    [Secure(RoleType.Admin)]
    public class AdminController : Controller
    {
        private readonly IClientDataService clientService;
        private readonly IUserDataService userService;

        public AdminController(IClientDataService clientService, IUserDataService userService)
        {
            this.clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public ActionResult ManageUsers()
        {
            ViewBag.Title = "Manage Users";
            ViewBag.SelectedPage = "ManageUsers";
            PopulateRoles();
            PopulateClients();
            return View();
        }

        public ActionResult GetManageUsers([DataSourceRequest]DataSourceRequest request)
        {
            return Json(userService.GetAllManageUsers().ToDataSourceResult(request));
        }

        [HttpPost]
        public ActionResult SaveManageUsers([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")]IEnumerable<ManageUserVM> users)
        {
            if (users != null && ModelState.IsValid)
            {
                foreach (var user in users)
                {
                    userService.UpdateManageUsers(user);
                }
            }

            return Json(users.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        public ActionResult DeleteUser([DataSourceRequest] DataSourceRequest request,
           [Bind(Prefix = "models")]IEnumerable<ManageUserVM> users)
        {
            foreach (var user in users)
            {
                userService.DeleteUser(user.UPN);
            }

            return Json(users.ToDataSourceResult(request, ModelState));
        }

        [HttpPost]
        public ActionResult AddUser([DataSourceRequest] DataSourceRequest request,
           [Bind(Prefix = "models")]IEnumerable<ManageUserVM> users)
        {
            var results = new List<ManageUserVM>();

            if (users != null && ModelState.IsValid)
            {
                users.Where(x => x.UserClients.Any(y => y.ClientName == null)).Select(c => { c.UserClients = null; return c; }).ToList();
                foreach (var user in users)
                {
                    user.DisplayName = user.UPN;                   
                    userService.AddUser(user);
                    results.Add(user);
                }
            }

            return Json(results.ToDataSourceResult(request, ModelState));
        }

        private void PopulateRoles()
        {
            var roles = userService.GetAllRoles();
            ViewData["roles"] = roles;
            ViewData["defaultRole"] = roles.ElementAt(1);
        }

        private void PopulateClients()
        {
            var clients = clientService.GetAll();
            ViewData["clients"] = clients;
            ViewData["defaultClient"] = clients.First();
        }

       
    }
}