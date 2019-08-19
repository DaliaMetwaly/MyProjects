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
using SawyerSight.Web.Models.ViewModel;
using SawyerSight.Web.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Link = SawyerSight.Graph.Models.FolderPermissions.Link;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using Application = Microsoft.Office.Interop.Excel.Application;
using OfficeOpenXml;

namespace SawyerSight.Web.Controllers
{
    [LogMVCExceptionFilter]
    [MessageCleanupFilter]
    [Secure(RoleType.Admin, RoleType.User)]
    public class WaterfallController : Controller
    {
        private readonly IClientDataService clientService;
        private readonly IStageDataService stageService;

        public WaterfallController(IClientDataService clientService, IStageDataService stageService)
        {
            this.clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            this.stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
        }


        public ActionResult Index(int? reportDataSetId)
        {
            int reportDataSetID;
            if (reportDataSetId is null && Session["reportDataSetId"] != null)
            {
                    reportDataSetID = (int)Session["reportDataSetId"];
            }
            else
            {
                reportDataSetID = (int)reportDataSetId;
            }
            
            //load report dataset context
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            appSession.LoadWaterfall(reportDataSetID);
            //check if no Dataset for this client
            if (clientService.checkIfClientExportData(appSession.Waterfall.ReportDataSetID) <=0)
            {
                ExportData();
            }
            //Customize Demographics Columns
            var demographics = SawyerSight.Web.Helpers.ViewModelBuilder.GetDemographics(new AppSessionContext(this, clientService));
            appSession.Waterfall.SelectedDemographics = appSession.Waterfall.SelectedDemographics.Where(x => x.IndexOf("AS") != -1).Count() == 0 ? appSession.Waterfall.SelectedDemographics : appSession.Waterfall.SelectedDemographics.Select(x => x.Substring(0, x.IndexOf("AS") - 1)).ToList();
            for (int i = 0; i < appSession.Waterfall.SelectedDemographics.Count(); i++)
            {
                if(demographics.Exists(x=>x.Id==appSession.Waterfall.SelectedDemographics[i]))
                {
                    var columnName = (String.IsNullOrEmpty(appSession.Waterfall.SelectedDemographics[i]) ? "' '" : appSession.Waterfall.SelectedDemographics[i]);
                    var aliasName = demographics.Where(x => x.Id == appSession.Waterfall.SelectedDemographics[i]).Count() <= 1 ?
                                    demographics.Where(x => x.Id == appSession.Waterfall.SelectedDemographics[i]).First().Name : demographics.Where(x => x.Id == appSession.Waterfall.SelectedDemographics[i]).ToList().ElementAt(appSession.Waterfall.SelectedDemographics.Where(x => x.ToString().Contains("' ' AS")).Count()).Name;
                    appSession.Waterfall.SelectedDemographics[i] = columnName + " AS [" + aliasName  + "]";
                }    
                else if (demographics.Exists(x => x.Name == appSession.Waterfall.SelectedDemographics[i]))
                {
                    appSession.Waterfall.SelectedDemographics[i] = demographics.Where(x => x.Name == appSession.Waterfall.SelectedDemographics[i]).First().Id + " AS [" + appSession.Waterfall.SelectedDemographics[i] + "]";
                }
            }
            //generate waterfall
            var waterfall = new Waterfall(appSession.Waterfall);
            var resultQuery = waterfall.Generate();
            //render spreadsheet
            return View(resultQuery);
        }
        public ActionResult WaterfallReport()
        {
            //generate waterfall
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            var demographics = SawyerSight.Web.Helpers.ViewModelBuilder.GetDemographics(new AppSessionContext(this, clientService));
            appSession.Waterfall.SelectedDemographics=appSession.Waterfall.SelectedDemographics.Where(x=>x.IndexOf("AS")!=-1).Count()==0? appSession.Waterfall.SelectedDemographics: appSession.Waterfall.SelectedDemographics.Select(x=>x.Substring(0,x.IndexOf("AS")-1)).ToList();
            for (int i=0;i< appSession.Waterfall.SelectedDemographics.Count();i++)
            {
                appSession.Waterfall.SelectedDemographics[i]= appSession.Waterfall.SelectedDemographics[i]+" AS ["+ demographics.Where(x => x.Id == appSession.Waterfall.SelectedDemographics[i]).First().Name + "]";
            }
            //ExportData();
            var waterfall = new Waterfall(appSession.Waterfall);
            var resultQuery = waterfall.Generate();
            
            //var etlToLive = new ETLToLive(appSession.Waterfall);
            //etlToLive.Export();
            //render spreadsheet
            return View(resultQuery);
        }

        public void ExportData()
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            var etlToLive = new ETLToLive(appSession.Waterfall);
            etlToLive.Export();
            
        }


        public ActionResult CreateClient()
        {
            ViewBag.Message = "Create a New Client";
            ViewBag.SelectedPage = "CreateClient";
            return View(new Client());
        }

        [HttpPost]
        public ActionResult CreateClient(Client newClient)
        {
            var existingClient = clientService.GetAll().Where(x => x.ClientID == newClient.ClientID || x.ClientName == newClient.ClientName).FirstOrDefault();
            if (existingClient != null)
            {
                TempData["ErrorMessage"] = "Client with the same Name or ID already exists";
                return View(newClient);// return the posted client data when show error message

            }

            //-------------------------

            //set ContactAddress2 not required 
            if (newClient.ContactAddress2 == null) { newClient.ContactAddress2 = String.Empty; }
            //---------------------------
            clientService.Add(newClient);
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            appSession.Waterfall.ClientID = newClient.ClientID;
            appSession.Waterfall.ClientName = newClient.ClientName;
            GraphServiceClientForNonUser graph = new GraphServiceClientForNonUser();
            graph.CreateFolder(newClient.ClientID);

            TempData["NewClient"] = newClient.ClientID;
            TempData["SuccessMessage "] = "Client Successfully Added";
            return RedirectToAction("SelectClient");
        }

        public ActionResult SelectClient()
        {
            var clients = clientService.GetAll();
            ViewBag.Title = "Select an Existing Data Set";
            ViewBag.SelectedPage = "SelectClient";
            return View(clients);
        }

        [HttpPost]
        public ActionResult SelectClient(string client)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            WaterfallContext wfcontext = new WaterfallContext();
            var clientInfo = client.Split('|');
            if (clientInfo != null && clientInfo.Length == 3)
            {
                appSession.Waterfall.ClientUniqueID = int.Parse(clientInfo[0]);
                appSession.Waterfall.ClientID = clientInfo[1];
                appSession.Waterfall.ClientName = clientInfo[2];
                appSession.LoadWaterfall(appSession.Waterfall.ClientUniqueID, appSession.Waterfall.ReportDataSetID);
                appSession.Waterfall.ClientUniqueID = int.Parse(clientInfo[0]);
                appSession.Waterfall.ClientID = clientInfo[1];
                appSession.Waterfall.ClientName = clientInfo[2];
                appSession.SaveWaterfall();
            }
            else
            {
                var firstClient = clientService.GetAll().First();

                appSession.Waterfall.ClientUniqueID = firstClient.UniqueID;
                appSession.Waterfall.ClientID = firstClient.ClientID;
                appSession.Waterfall.ClientName = firstClient.ClientName;
                appSession.LoadWaterfall(appSession.Waterfall.ClientUniqueID, appSession.Waterfall.ReportDataSetID);
                appSession.Waterfall.ClientUniqueID = firstClient.UniqueID;
                appSession.Waterfall.ClientID = firstClient.ClientID;
                appSession.Waterfall.ClientName = firstClient.ClientName;
                appSession.SaveWaterfall();
            }


            return RedirectToAction("FolderInvitations");
        }

        private Graph.Models.FolderItems.Value GetLatestFile(GraphServiceClientForNonUser graph)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            var allFolders = JsonConvert.DeserializeObject<FoldersExplorer>(graph.GetDriveItems());
            var clientFolder = allFolders.value.Where(x => x.name == appSession.Waterfall.ClientID).FirstOrDefault();
            var dbInvitations = new List<OneDriveInvitedUser>();
            List<string> avoidedInvitations = new List<string>() { "Team Site Owners", "Team Site Visitors", "Team Site Members", "Company Administrator", "SharePoint Service Administrator" };
            if (clientFolder != null)
            {
                var allInvitations = JsonConvert.DeserializeObject<FolderPermissions>(graph.GetFolderInvitations(clientFolder.id));
                var invitations = allInvitations.value.Where(x => !avoidedInvitations.Contains(x.grantedTo?.user?.displayName)).ToList();
                dbInvitations = clientService.GetAllOneDriveShares(appSession.Waterfall.ClientID).ToList();

                var allFiles = JsonConvert.DeserializeObject<FolderItems>(graph.GetFolderItems(clientFolder.id));

                var latestFile = allFiles.value.Where(x => x.lastModifiedDateTime == allFiles.value.Max(y => y.lastModifiedDateTime) && x.downloadUrl != null
                && (x.name.ToLower().Contains(".zip") || x.name.ToLower().Contains(".xlsx"))
                ).FirstOrDefault();
                return latestFile;
            }

            return null;
        }

        public ActionResult FolderInvitations()
        {
            ViewBag.SelectedPage = "SelectClient";

            AppSessionContext appSession = new AppSessionContext(this, clientService);
            GraphServiceClientForNonUser graph = new GraphServiceClientForNonUser();
            var allFolders = JsonConvert.DeserializeObject<FoldersExplorer>(graph.GetDriveItems());
            var clientFolder = allFolders.value.Where(x => x.name == appSession.Waterfall.ClientID).FirstOrDefault();
            var dbInvitations = new List<OneDriveInvitedUser>();
            List<string> avoidedInvitations = new List<string>() { "Team Site Owners", "Team Site Visitors", "Team Site Members", "Company Administrator", "SharePoint Service Administrator" };
            if (clientFolder != null)
            {
                Session["clientFolder"] = clientFolder.id;
                var allInvitations = JsonConvert.DeserializeObject<FolderPermissions>(graph.GetFolderInvitations(clientFolder.id));
                var invitations = allInvitations.value.Where(x => !avoidedInvitations.Contains(x.grantedTo?.user?.displayName)).ToList();
                dbInvitations = clientService.GetAllOneDriveShares(appSession.Waterfall.ClientID).ToList();

                var allFiles = JsonConvert.DeserializeObject<FolderItems>(graph.GetFolderItems(clientFolder.id));
                TempData["FolderItems"] = allFiles;
                
                var latestFile = allFiles.value.Where(x => x.lastModifiedDateTime == allFiles.value.Max(y => y.lastModifiedDateTime) && x.downloadUrl != null 
                && (x.name.ToLower().Contains(".zip")|| x.name.ToLower().Contains(".xlsx"))
                ).FirstOrDefault();
                TempData["LatestFile"] = latestFile;
                var shareLink = graph.CreateShareLink(clientFolder.id);
                TempData["ShareLink"] = JsonConvert.DeserializeObject<Link>((JsonConvert.DeserializeObject<Dictionary<string, object>>(shareLink)).Last().Value.ToString());
                foreach (var invite in dbInvitations)
                {
                    if (string.IsNullOrWhiteSpace(invite.ShareID))
                    {
                        invite.ShareID = invitations.Where(x => x.link.webUrl == invite.ShareLink).FirstOrDefault().id;
                        clientService.UpdateOneDriveShareID(invite.ShareLink, invite.ShareID);
                    }
                }
            }
            return View(dbInvitations);
        }

        [HttpPost]
        public ActionResult SendInvitation(string txtInvitationEmail, string txtInvitationText, string hiddenClientFolder, string hiddenProjectName)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            GraphServiceClientForNonUser graph = new GraphServiceClientForNonUser();
            var response = graph.SendSharingInvitation(hiddenClientFolder, txtInvitationText, txtInvitationEmail);
            var NonUser = JsonConvert.DeserializeObject<InvitationResponseNonUser>(response);
            if (NonUser.value[0].link != null)
            {
                clientService.AddNewOneDriveShare(appSession.Waterfall.ClientUniqueID, appSession.Waterfall.ClientID, txtInvitationEmail, txtInvitationText, NonUser.value[0].link.webUrl, "");
            }
            else
            {
                var user = JsonConvert.DeserializeObject<InvitationResponseUser>(response);
                clientService.AddNewOneDriveShare(appSession.Waterfall.ClientUniqueID, appSession.Waterfall.ClientID, txtInvitationEmail, txtInvitationText, "", user.value[0].id);
            }
            Session["ProjectName"] = hiddenProjectName != null ? hiddenProjectName : "";
            return RedirectToAction("FolderInvitations");
        }

        [HttpPost]
        public JsonResult DeleteShareInvitation(string ShareID, string FolderID)
        {
            GraphServiceClientForNonUser graph = new GraphServiceClientForNonUser();
            graph.DeleteSharingInvitation(FolderID, ShareID);
            clientService.DeleteOneDriveShare(ShareID);
            return Json("Success");

        }

        [HttpPost]
        public ActionResult LoadETLData(string archiveURL, string inputProjectName)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            appSession.Waterfall.ProjectName = inputProjectName;
            appSession.Waterfall.ClientArchiveURL = archiveURL;
            appSession.SaveWaterfall();
            ViewBag.SelectedPage = "DBDiagnostics";
            return View();
        }

        public JsonResult ExtractAndImportClientArchive()
        {

            int itemsProgress = 1;
            int itemsCount = 34;

            SignalRProcessor.SendImportUpdate("Downloading Archive from One Drive", itemsProgress++, itemsCount);

            AppSessionContext appSession = new AppSessionContext(this, clientService);
            var webClient = new WebClient();

            GraphServiceClientForNonUser graph = new GraphServiceClientForNonUser();        
            var latestFile = GetLatestFile(graph);

            string fileExtension = Path.GetExtension(latestFile.name);
            var excelDir =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "App_Data" + "\\" + appSession.Waterfall.ClientID + ".xlsx");
            byte[] archiveBytes = null;
            if (fileExtension == ".xlsx")
            {
                webClient.DownloadFile(appSession.Waterfall.ClientArchiveURL,excelDir );
            }
            else
            {
                 archiveBytes = webClient.DownloadData(appSession.Waterfall.ClientArchiveURL);
            }
               
            SignalRProcessor.SendImportUpdate("Archive downloaded. Proceeding with extraction", itemsProgress++, itemsCount);

            var existingReportDS = clientService.GetReportDataSetID(appSession.Waterfall.ClientUniqueID);
            if (existingReportDS < 1)
            {
                existingReportDS = clientService.AddNewReportDataSet(appSession.Waterfall.ClientUniqueID);
            }
            appSession.Waterfall.ReportDataSetID = existingReportDS;
            appSession.SaveWaterfall();

            ETLWorker wrk = new ETLWorker(itemsProgress);
            if (fileExtension == ".xlsx")
            {             
                ExcelToCSVCoversion(excelDir, appSession.Waterfall.ClientID);

                if (wrk.LoadClientData(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "App_Data", appSession.Waterfall.ClientID), existingReportDS))
                {
                    SignalRProcessor.SendImportUpdate($"Archive Processed Successfully. Redirecting to Organizations Page", itemsCount, itemsCount);
                    return Json("", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json("FAILURE", JsonRequestBehavior.AllowGet);
                }


            }
            else
            {
                if (wrk.LoadClientData(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "\\App_Data", appSession.Waterfall.ClientID), existingReportDS, archiveBytes))
                {
                    SignalRProcessor.SendImportUpdate($"Archive Processed Successfully. Redirecting to Organizations Page", itemsCount, itemsCount);

                    return Json("", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json("FAILURE", JsonRequestBehavior.AllowGet);
                }
            }



           
           
        }

        public ActionResult SelectOrganizations()
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            if (appSession.Waterfall.ClientUniqueID == 0)
            {
                return RedirectToAction("SelectClient");
            }
            ViewBag.ProjectName = appSession.Waterfall.ProjectName;
            ViewBag.SelectedPage = "WaterfallParameters";
            return View();
        }

        [HttpPost]
        public ActionResult SelectOrganizations(List<string> chxOrgs, string hiddenActionLoggerUnChecked, string hiddenActionLoggerChecked, string selectAllActive, string selectAllInactive)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            appSession.Waterfall.SelectedOrganizations = new TreeListChecks()
            {
                CheckedNodes = hiddenActionLoggerChecked.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                SelectAllActive = selectAllActive.ToLower() == "true",
                SelectAllInactive = selectAllInactive.ToLower() == "true",
                VisibleUncheckedNodes = hiddenActionLoggerUnChecked.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
            appSession.Waterfall.ProjectName = Request.Form["inputProjectName"];
            if (appSession.Waterfall.SelectedProjects == null)
            {
                appSession.Waterfall.SelectedProjects = new TreeListChecks();
            }
            appSession.SaveWaterfall();
            return RedirectToAction("SelectProjects");
        }

        [HttpGet]
        public ActionResult SelectProjects()
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            if (appSession.Waterfall.ClientUniqueID == 0)
            {
                return RedirectToAction("SelectClient");
            }
            ViewBag.SelectedPage = "WaterfallParameters";
            ViewBag.ProjectName = appSession.Waterfall.ProjectName;
            return View();
        }

        [HttpPost]
        public ActionResult SelectProjects(List<string> chxProj, string hiddenActionLoggerUnChecked, string hiddenActionLoggerChecked, string selectAllActive, string selectAllInactive, string projectLevel)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            appSession.Waterfall.SelectedProjects = new TreeListChecks()
            {
                CheckedNodes = hiddenActionLoggerChecked.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                SelectAllActive = selectAllActive.ToLower() == "true",
                SelectAllInactive = selectAllInactive.ToLower() == "true",
                VisibleUncheckedNodes = hiddenActionLoggerUnChecked.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
            appSession.Waterfall.SelectedProjectsLevel = int.Parse(projectLevel);
            if (appSession.Waterfall.SelectedFiscalYears == null)
            {
                appSession.Waterfall.SelectedFiscalYears = new List<string>();
            }
            appSession.SaveWaterfall();
            return RedirectToAction("SelectFiscalYears");
        }

        public ActionResult SelectFiscalYears()
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            ViewBag.ProjectName = appSession.Waterfall.ProjectName;
            ViewBag.SelectedPage = "WaterfallParameters";
            return View();
        }

        [HttpPost]
        public ActionResult SelectFiscalYears(List<string> chxFiscal)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            appSession.Waterfall.SelectedFiscalYears = chxFiscal;
            if (appSession.Waterfall.SelectedDemographics == null)
            {
                appSession.Waterfall.SelectedDemographics = new List<string>();
            }
            appSession.SaveWaterfall();
            return RedirectToAction("SelectDemographics");
        }

        public ActionResult SelectDemographics()
        {
            ViewBag.Title = "Demographic Options";
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            if (appSession.Waterfall.SelectedDemographics == null)
            {
                appSession.Waterfall.SelectedDemographics = new List<string>();
            }
            ViewBag.SelectedPage = "WaterfallParameters";
            return View(SawyerSight.Web.Helpers.ViewModelBuilder.GetDemographics(appSession));
        }

        [HttpPost]
        public ActionResult SelectDemographics(List<string> chxDemographics)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            appSession.Waterfall.SelectedDemographics = chxDemographics;
            if (appSession.Waterfall.RevenueAccounts == null)
            {
                appSession.Waterfall.RevenueAccounts = new SelectedRevenueAccounts();
            }
            appSession.SaveWaterfall();
            return RedirectToAction("TrialBalanceRevenue");
        }

        public ActionResult TrialBalanceRevenue()
        {
            ViewBag.Title = "Revenue Categories";
            ViewBag.SelectedPage = "TrialBalance";
            return View();
        }

        [HttpPost]
        public JsonResult TrialBalanceRevenue(List<string> SelectedAccounts, string Revenue1Name, List<string> Revenue1Nodes, string Revenue2Name, List<string> Revenue2Nodes, string Revenue3Name, List<string> Revenue3Nodes, string Revenue4Name, List<string> Revenue4Nodes)
        {
            if (SelectedAccounts != null)
            {
                SelectedRevenueAccounts revenues = new SelectedRevenueAccounts()
                {
                    SelectedAccounts = SelectedAccounts,
                    Revenue1Name = Revenue1Name,
                    Revenue1Nodes = Revenue1Nodes != null ? Revenue1Nodes : new List<string>(),
                    Revenue2Name = Revenue2Name,
                    Revenue2Nodes = Revenue2Nodes != null ? Revenue2Nodes : new List<string>(),
                    Revenue3Name = Revenue3Name,
                    Revenue3Nodes = Revenue3Nodes != null ? Revenue3Nodes : new List<string>(),
                    Revenue4Name = Revenue4Name,
                    Revenue4Nodes = Revenue4Nodes != null ? Revenue4Nodes : new List<string>()
                };
                AppSessionContext appSession = new AppSessionContext(this, clientService);
                appSession.Waterfall.RevenueAccounts = revenues;
                appSession.SaveWaterfall();
            }
            return Json(new { result = "Success" });
        }

        public ActionResult TrialBalanceCosts()
        {
            ViewBag.Title = "Costs Categories";
            ViewBag.SelectedPage = "TrialBalance";
            return View();
        }

        [HttpPost]
        public JsonResult TrialBalanceCosts(List<string> SelectedAccounts, string Cost1Name, List<string> Cost1Nodes, string Cost2Name, List<string> Cost2Nodes, string Cost3Name, List<string> Cost3Nodes, string Cost4Name, List<string> Cost4Nodes,
                                                string Cost5Name, List<string> Cost5Nodes, string Cost6Name, List<string> Cost6Nodes, string Cost7Name, List<string> Cost7Nodes, string Cost8Name, List<string> Cost8Nodes)
        {
            if (SelectedAccounts != null)
            {
                SelectedCostsAccounts costs = new SelectedCostsAccounts()
                {
                    SelectedAccounts = SelectedAccounts,
                    Costs1Name = Cost1Name,
                    Costs1Nodes = Cost1Nodes != null ? Cost1Nodes : new List<string>(),
                    Costs2Name = Cost2Name,
                    Costs2Nodes = Cost2Nodes != null ? Cost2Nodes : new List<string>(),
                    Costs3Name = Cost3Name,
                    Costs3Nodes = Cost3Nodes != null ? Cost3Nodes : new List<string>(),
                    Costs4Name = Cost4Name,
                    Costs4Nodes = Cost4Nodes != null ? Cost4Nodes : new List<string>(),
                    Costs5Name = Cost5Name,
                    Costs5Nodes = Cost5Nodes != null ? Cost5Nodes : new List<string>(),
                    Costs6Name = Cost6Name,
                    Costs6Nodes = Cost6Nodes != null ? Cost6Nodes : new List<string>(),
                    Costs7Name = Cost7Name,
                    Costs7Nodes = Cost7Nodes != null ? Cost7Nodes : new List<string>(),
                    Costs8Name = Cost8Name,
                    Costs8Nodes = Cost8Nodes != null ? Cost8Nodes : new List<string>(),
                };
                AppSessionContext appSession = new AppSessionContext(this, clientService);
                appSession.Waterfall.CostsAccounts = costs;
                appSession.SaveWaterfall();
            }
            return Json(new { result = "Success" });
        }

        [HttpPost]
        public ActionResult TrialBalanceAccounts(string txtContractRevenue)
        {
            return RedirectToAction("GenerateWaterfall");
        }

        public ActionResult GenerateWaterfall()
        {
            ViewBag.SelectedPage = "GenerateWaterfall";
            return View();
        }

        [HttpPost]
        public ActionResult GenerateWaterfall(string projectName)
        {
            //Replace the new report with new one
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            Session["reportDataSetId"] = appSession.Waterfall.ReportDataSetID;
            return RedirectToAction("Index");
            
            //return RedirectToAction("WaterfallProcessor");
        }


        //public ActionResult WaterfallProcessor()
        //{
        //    return View();
        //}

        //public JsonResult GenerateWaterfallReport()
        //{
        //    AppSessionContext appSession = new AppSessionContext(this, clientService);
        //    var worker = new WaterfallWorker(appSession.Waterfall);
        //    var resultQuery = worker.MigrateETLToLiveDataAndGenerateWaterfall();
        //    Session["fiscalYears"] = worker.ProcessedFiscalYears;
        //    Session["WaterfallQueries"] = worker.GeneratedQueries;
        //    Session["WaterfallQuery"] = resultQuery.Split('|')[0];

        //    return Json("", JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult GetGeneratedWaterfall()
        //{
        //    AppSessionContext appSession = new AppSessionContext(this, clientService);
        //    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "App_Data", appSession.Waterfall.ReportDataSetID.ToString() + ".xlsx");
        //    var workbook = Telerik.Web.Spreadsheet.Workbook.Load(path);
        //    var years = (Dictionary<string, List<string>>)Session["fiscalYears"];

        //    var currentSheet = workbook.Sheets[0];

        //    var headerCells = currentSheet.Rows[0].Cells;

         
        //    foreach (var r in currentSheet.Rows)
        //    {
        //        foreach (var c in r.Cells)
        //        {

        //            c.TextAlign = "center";
        //            if (c.Value is double)
        //            {
        //                c.Format = "###,###,##0";
        //            }

        //        }
        //    }

        //    //find if there is PROJ Start Date, and apply the format to that cell
        //    var projStartDateIndex = -1;
        //    projStartDateIndex = currentSheet.Rows[0].Cells.Where(x => x.Value.ToString() == "PROJ_START_DT").FirstOrDefault() != null ? currentSheet.Rows[0].Cells.Where(x => x.Value.ToString() == "PROJ_START_DT").First().Index.Value : -1;
        //    currentSheet.Rows[0].Index = 3;
        //    if (projStartDateIndex > 0)
        //    {
        //        foreach (var row in currentSheet.Rows)
        //        {                    
        //                row.Cells[projStartDateIndex].Format = "yyyy/mm/dd";                     
        //        }
        //    }

        //    //find if there is PROJ End Date, and apply the format to that cell
            
        //    var projEndDateIndex = -1;
        //    projEndDateIndex = currentSheet.Rows[0].Cells.Where(x => x.Value.ToString() == "PROJ_END_DT").FirstOrDefault() != null ? currentSheet.Rows[0].Cells.Where(x => x.Value.ToString() == "PROJ_END_DT").First().Index.Value : -1;
        //    currentSheet.Rows[0].Index = 3;
        //    if (projEndDateIndex > 0)
        //    {
        //        foreach (var row in currentSheet.Rows)
        //        {
        //            row.Cells[projEndDateIndex].Format = "yyyy/mm/dd";
        //        }
        //    }


        //    currentSheet.Rows.Insert(0, new Telerik.Web.Spreadsheet.Row() { Cells = new List<Telerik.Web.Spreadsheet.Cell>(), Index = 0 });
        //    currentSheet.Rows.Insert(1, new Telerik.Web.Spreadsheet.Row() { Cells = new List<Telerik.Web.Spreadsheet.Cell>(), Index = 1 });
        //    currentSheet.Rows.Insert(2, new Telerik.Web.Spreadsheet.Row() { Cells = new List<Telerik.Web.Spreadsheet.Cell>(), Index = 2 });
        //    var demographics = SawyerSight.Web.Helpers.ViewModelBuilder.GetDemographics(appSession);

        //    int cellOffsetCounter = 1;

        //    currentSheet.Rows[3].Cells[0].Bold = true;
        //    currentSheet.Rows[3].Cells[0].TextAlign = "center";

        //    foreach (var c in currentSheet.Rows[3].Cells)
        //    {
        //        c.BorderBottom = new Telerik.Web.Spreadsheet.BorderStyle()
        //        {
        //            Size = 5,
        //            Color = "#d9e1f2"
        //        };
        //    }

        //    foreach (var demographic in appSession.Waterfall.SelectedDemographics)
        //    {
        //        var cd = demographics.Where(x => x.Id == demographic).First();
        //        currentSheet.Rows[2].Cells.Add(new Telerik.Web.Spreadsheet.Cell() { Value = "", Index = cellOffsetCounter, Bold = true, TextAlign = "center" });
        //        currentSheet.Rows[3].Cells[cellOffsetCounter].Value = cd.Name;
        //        currentSheet.Rows[3].Cells[cellOffsetCounter].Bold = true;
        //        currentSheet.Rows[3].Cells[cellOffsetCounter].TextAlign = "center";
        //        cellOffsetCounter++;
        //    }


        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.RevenueAccounts.Revenue1Name) && appSession.Waterfall.RevenueAccounts.Revenue1Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.RevenueAccounts.Revenue1Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.RevenueAccounts.Revenue2Name) && appSession.Waterfall.RevenueAccounts.Revenue2Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.RevenueAccounts.Revenue2Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.RevenueAccounts.Revenue3Name) && appSession.Waterfall.RevenueAccounts.Revenue3Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.RevenueAccounts.Revenue3Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.RevenueAccounts.Revenue4Name) && appSession.Waterfall.RevenueAccounts.Revenue4Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.RevenueAccounts.Revenue4Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs1Name) && appSession.Waterfall.CostsAccounts.Costs1Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs1Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs2Name) && appSession.Waterfall.CostsAccounts.Costs2Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs2Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs3Name) && appSession.Waterfall.CostsAccounts.Costs3Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs3Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs4Name) && appSession.Waterfall.CostsAccounts.Costs4Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs4Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs5Name) && appSession.Waterfall.CostsAccounts.Costs5Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs5Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs6Name) && appSession.Waterfall.CostsAccounts.Costs6Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs6Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs7Name) && appSession.Waterfall.CostsAccounts.Costs7Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs7Name);
        //    }

        //    if (!string.IsNullOrWhiteSpace(appSession.Waterfall.CostsAccounts.Costs8Name) && appSession.Waterfall.CostsAccounts.Costs8Nodes.Count > 0)
        //    {
        //        cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, appSession.Waterfall.CostsAccounts.Costs8Name);
        //    }

        //    cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, "Total Revenue");

        //    cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, "Total Costs");


        //    cellOffsetCounter = SpreadsheetLayoutHelper.FormatWaterFiscalYearHeader(currentSheet, cellOffsetCounter, years, "Gross Profit");


        //    int revStartTotalMargin = cellOffsetCounter;
        //    foreach (var year in years)
        //    {
        //        currentSheet.Rows[2].Cells.Add(new Telerik.Web.Spreadsheet.Cell() { Value = "Gross Margin", Index = cellOffsetCounter, Bold = true, TextAlign = "center", BorderBottom = new Telerik.Web.Spreadsheet.BorderStyle() { Color = "black", Size = 2 } });
        //        if (year.Value.Count < 12)
        //        {
        //            currentSheet.Rows[3].Cells[cellOffsetCounter].Value = "YTD" + SawyerSight.Web.Helpers.SpreadsheetLayoutHelper.GetMonthNameByMonthNumber(year.Value.Select(x => int.Parse(x)).ToList().Max()) + year.Key.ToString().Substring(2);
        //        }
        //        else
        //        {
        //            currentSheet.Rows[3].Cells[cellOffsetCounter].Value = "FY" + year.Key.ToString().Substring(2);
        //        }
        //        currentSheet.Rows[3].Cells[cellOffsetCounter].Bold = true;
        //        currentSheet.Rows[3].Cells[cellOffsetCounter].TextAlign = "center";


        //        foreach (var r in currentSheet.Rows)
        //        {
        //            if (r.Cells.Where(x => x.Index == cellOffsetCounter).Count() > 0)
        //            {
        //                if (r.Cells.Where(x => x.Index == cellOffsetCounter).First().Value is double)
        //                {
        //                    var v = (double)r.Cells.Where(x => x.Index == cellOffsetCounter).First().Value;

        //                    r.Cells.Where(x => x.Index == cellOffsetCounter).First().Value = v.ToString("P", CultureInfo.InvariantCulture);
        //                }

        //            }
        //        }

        //        cellOffsetCounter++;
        //    }
        //    var startLetterTotalMargin = SawyerSight.Web.Helpers.SpreadsheetLayoutHelper.GetExcelColumnName(revStartTotalMargin + 1);
        //    var endLetterTotalMargin = SawyerSight.Web.Helpers.SpreadsheetLayoutHelper.GetExcelColumnName(cellOffsetCounter);
        //    currentSheet.AddMergedCells($"{startLetterTotalMargin}3:{endLetterTotalMargin}3");
            
        //    currentSheet.FrozenColumns = 3;
        //    currentSheet.FrozenRows = 4;

        //    //Uses Newtonsoft.Json internally to serialize fields correctly.
        //    return Content(workbook.ToJson(), Telerik.Web.Spreadsheet.MimeTypes.JSON);
        //}

        #region TreeDataLoaders

        public JsonResult ProjectsTree_Read([DataSourceRequest] DataSourceRequest request, int? id)
        {
            using (AppSessionContext appSession = new AppSessionContext(this, clientService))
            {
                List<Project> allProjects = stageService.GetProjects(appSession.Waterfall.ReportDataSetID).ToList();
                var projects = Processor.ProcessSelectedOrganizationsIntoProjects(allProjects, appSession.Waterfall.SelectedOrganizations);
                var tree = new TreeCollectionLazyLoading<Project, string>(projects, proj => proj.Id, proj => proj.ParentId);
                var ProjectMaxLevel = projects.Count > 0 ? projects.Select(x => x.Level).Max() : 0;
                appSession.Waterfall.ProjectsMaxLevel = ProjectMaxLevel;
                return Json(tree.AsDataSource(request, id), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult OrganizationsTree_Read([DataSourceRequest] DataSourceRequest request, int? id)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            List<Organization> organizations = stageService.GetOrganizations(appSession.Waterfall.ReportDataSetID).ToList();
            var tree = new TreeCollectionLazyLoading<Organization, string>(organizations, org => org.Id, org => org.ParentId);
            return Json(tree.AsDataSource(request, id), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetProjectMaxLevel()
        {
            using (AppSessionContext appSession = new AppSessionContext(this, clientService))
            {
                return Json(appSession.Waterfall.ProjectsMaxLevel, JsonRequestBehavior.AllowGet);
            }

        }

        public JsonResult FiscalYearsTree_Read([DataSourceRequest] DataSourceRequest request)
        {

            AppSessionContext appSession = new AppSessionContext(this, clientService);
            List<FiscalYear> fysWithMonths = stageService.GetFiscalYears(appSession.Waterfall.ReportDataSetID).ToList();

            var tree = new TreeCollection<FiscalYear, string>(fysWithMonths, org => org.YearCode, org => org.ParentId);

            return Json(tree.AsDataSource(request), JsonRequestBehavior.AllowGet);
        }

        public JsonResult TrialBalanceAccountsTree_Read([DataSourceRequest] DataSourceRequest request, string[] selectedAccounts)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);

            if (selectedAccounts == null)
            {
                selectedAccounts = new string[] { "-1" };
            }

            List<Account> allAccounts = stageService.GetAllAccounts(appSession.Waterfall.ReportDataSetID).ToList();
            allAccounts = allAccounts.Where(x => selectedAccounts.Contains(x.Id.Substring(0, 1))).OrderBy(y => y.Id).ToList();
            return Json(allAccounts, JsonRequestBehavior.AllowGet);
        }

        public JsonResult FilterTrialBalanceAccounts_ID([DataSourceRequest] DataSourceRequest request)
        {
            AppSessionContext appSession = new AppSessionContext(this, clientService);
            List<string> alreadyAdded = new List<string>();

            List<Account> allAccounts = stageService.GetAllAccounts(appSession.Waterfall.ReportDataSetID).ToList();
            var startsWith = allAccounts.Select(x => new AccountStartWith() { Id = x.Id.Substring(0, 1), NumAccounts = allAccounts.Where(y => y.Id.StartsWith(x.Id.Substring(0, 1))).Count() }).GroupBy(x => x.Id).Select(y => y.First()).ToList();
            return Json(startsWith, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// This method generates a list of accounts that is required for proper generation of the 
        /// tree structure for the UI. As we don't have the proper structure in the revenues or costs table,
        /// we use all of the accounts, and keep adding parents of the actual revenue or costs accounts until we get to the root
        /// </summary>
        /// <param name="allAccounts"></param>
        /// <param name="revenuesCosts"></param>
        /// <returns></returns>
        private List<Account> BuildAccountsParentsTree(List<Account> allAccounts, List<Account> revenuesCosts)
        {
            Dictionary<string, Account> result = new Dictionary<string, Account>();

            result.AddRange(revenuesCosts.Select(x => new KeyValuePair<string, Account>(x.Id, x)));

            bool noMore = false;

            while (!noMore)
            {
                Dictionary<string, Account> toAdd = new Dictionary<string, Account>();
                noMore = true;
                foreach (var item in result)
                {
                    if (item.Value.ParentId != null)
                    {
                        if (!result.ContainsKey(item.Value.ParentId))
                        {
                            var itemToAdd = allAccounts.Where(x => x.Id == item.Value.ParentId).FirstOrDefault();
                            if (itemToAdd != null && !toAdd.ContainsKey(itemToAdd.Id))
                                toAdd.Add(itemToAdd.Id, itemToAdd);
                        }
                    }
                }
                result.AddRange(toAdd);
                noMore = toAdd.Count == 0;
            }

            return result.Values.ToList();
        }

        #endregion

        #region   ExcelToCSVCoversion

        void ExcelToCSVCoversion(string sourceFile,string clientID)
        {
            var tableList=new List<string>();
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "App_Data\\"+ clientID + "\\Extracted");

            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            Directory.CreateDirectory(dir);

            try
            {              
                var excel = new ExcelPackage(new System.IO.FileInfo(sourceFile));
                foreach (var sheet in excel.Workbook.Worksheets)
                {
                    using (var writer = new StreamWriter(Path.Combine(dir, sheet.Name + ".csv")))
                    {
                        using (var csv = new CsvHelper.CsvWriter(writer))
                        {
                            csv.Configuration.HasHeaderRecord = true;
                            int counter = 0;
                            int sheetColumnCount = sheet.Dimension.Columns;
                            foreach (var cell in sheet.Cells)
                            {
                                var cellVal = cell.Value;
                                    if(cellVal== null)
                                     {
                                         csv.WriteField(null);
                                     }
                                    else
                                     {
                                         if (cellVal.ToString() == " ")
                                         {
                                             cellVal = 0;
                                         }
                                         if (cell.Style.Numberformat.Format.Contains("mmm") && double.TryParse(cellVal.ToString(), out double dateAsDouble))
                                         {
                                             csv.WriteField(DateTime.FromOADate((double)cellVal).ToString("yyyy-MM-dd"));
                                         }

                                         else
                                         {
                                             csv.WriteField(cellVal);
                                         }
                                     }
                                  
                                
                                
                                if (++counter % sheetColumnCount == 0)
                                {
                                    csv.NextRecord();
                                }
                            }
                        }
                    }
                }



                var excelConfigDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "ExcelConfig");

                //Copy table.txt file
                string destinationTablesFile = Path.Combine(dir, "Tables.txt");
                string sourceTablesFile = Path.Combine(excelConfigDir, "Tables.txt");
                System.IO.File.Copy(sourceTablesFile, destinationTablesFile, true);


                //Copy executionLog file                
                string executionLogFileSource = Path.Combine(excelConfigDir, "executionLog.log.txt");//done like this to avoid Git suppresion
                string executionLogFileDestination = Path.Combine(dir, "executionLog.log");
                System.IO.File.Copy(executionLogFileSource, executionLogFileDestination, true);                

            }

            finally
            {
               
                //Delete Excel file
                if (System.IO.File.Exists(sourceFile))
                {
                    System.IO.File.Delete(sourceFile);
                }               
            }
        }
        #endregion

    }
}