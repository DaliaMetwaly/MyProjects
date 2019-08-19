using SawyerSight.DAL.Repositories;
using SawyerSight.Web.Models.ViewModel;
using System;
using System.Web.Mvc;

namespace SawyerSight.Web.Models
{
    public class AppSessionContext : IDisposable
    {
        private Controller controller;
        private readonly IClientDataService _clientService;

        public AppSessionContext(Controller controller, IClientDataService clientService)
        {
            this.controller = controller;
            _clientService = clientService;
        }

        public WaterfallContext Waterfall
        {

            get
            {
                var session = controller.Session;
                if (session == null)
                {
                    throw new NullReferenceException("Session is null.");
                }

                const string key = "WaterfallContext";
                if (session[key] == null)
                {
                    session[key] = new WaterfallContext();
                }

                return (WaterfallContext)session[key];
            }
            set
            {
                var session = controller.Session;
                if (session == null)
                {
                    throw new NullReferenceException("Session is null.");
                }

                const string key = "WaterfallContext";
                session[key] = value;
            }
        }

        public bool SaveWaterfall()
        {
            var contextText = Newtonsoft.Json.JsonConvert.SerializeObject(this.Waterfall);
            _clientService.SaveWaterfallContext(Waterfall.ClientUniqueID, Waterfall.ReportDataSetID, contextText);
            return true;
        }

        public WaterfallContext LoadWaterfall(int clientID, int reportDatasetID)
        {
            string contextText = _clientService.GetWaterfallContext(clientID);

            if (!string.IsNullOrWhiteSpace(contextText))
            {
                Waterfall = Newtonsoft.Json.JsonConvert.DeserializeObject<WaterfallContext>(contextText);
            }
            return Waterfall;
        }

        public WaterfallContext LoadWaterfall(int reportDatasetID)
        {
            string contextText = _clientService.GetWaterfallContextByReportDataSetID(reportDatasetID);

            if (!string.IsNullOrWhiteSpace(contextText))
            {
                Waterfall = Newtonsoft.Json.JsonConvert.DeserializeObject<WaterfallContext>(contextText);
            }
            return Waterfall;
        }

        public void Dispose()
        {

        }
    }
}