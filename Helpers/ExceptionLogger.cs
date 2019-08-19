using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Helpers
{
    public static class ExceptionLogger
    {
        public static void LogError(Exception ex, string errorSource)
        {
            Log.Error(ex, errorSource);
        }
    }
}