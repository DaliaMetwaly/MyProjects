using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Serilog;
using Unity;

[assembly: OwinStartupAttribute(typeof(SawyerSight.Web.Startup))]
namespace SawyerSight.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            SetupLogger();

            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}
