using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Logic
{
    public class ETLToLiveMigrationHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }
    }
}