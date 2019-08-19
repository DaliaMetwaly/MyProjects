using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Models.ETL
{
    public class InputVariables
    {
        public string password { get; set; }
        public string DBName { get; set; }
        public string ExportFolder { get; set; }
        public string SchemaName { get; set; }
        public string username { get; set; }
        public string serverName { get; set; }
    }

    public class TablesInfo
    {
        public string Status { get; set; }
        public string TableName { get; set; }
        public int TableExported { get; set; }
    }

    public class ExecutionManifest
    {
        public string ExecutionID { get; set; }
        public InputVariables InputVariables { get; set; }
        public string ExportEngine { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public List<TablesInfo> TablesInfo { get; set; }
        public DateTime EndTime { get; set; }
        public string ScriptVersion { get; set; }
    }
}