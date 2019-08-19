using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace SawyerSight.Web
{
    public partial class Startup
    {
        public void SetupLogger()
        {
            Log.Logger = new LoggerConfiguration()
               .ReadFrom.AppSettings()
               .CreateLogger();
        }
    }
}