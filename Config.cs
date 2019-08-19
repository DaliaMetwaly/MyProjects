using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SawyerSight.Web
{
    public class Config
    {
        private Config()
        {
            DefaultConnectionString = ConfigurationManager.ConnectionStrings["SawyerSightDBConnection"].ConnectionString;
            ETLConnectionString = ConfigurationManager.ConnectionStrings["SawyerSightETLDBConnection"].ConnectionString;            
            UploadFolderPath = ConfigurationManager.AppSettings["UploadFolderPath"];
        }

        public static Config Current => instance.Value;
        private static Lazy<Config> instance = new Lazy<Config>(() => new Config());

        public string DefaultConnectionString { get; private set; }
        public string ETLConnectionString { get; private set; }        
        public string UploadFolderPath { get; private set; }
    }
}