using SawyerSight.Web.Helpers;
using System.IO;
using System.Web.Http.Filters;

namespace SawyerSight.Web.Filters
{
    public class LogWebAPIExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnException(actionExecutedContext);

            string rawRequest;
            using (var stream = new StreamReader(actionExecutedContext.Request.Content.ReadAsStreamAsync().Result))
            {
                stream.BaseStream.Position = 0;
                rawRequest = stream.ReadToEnd();
            }
            actionExecutedContext.Exception.Data.Add("Context", rawRequest);
            ExceptionLogger.LogError(actionExecutedContext.Exception,"Web API Method Exception");
        }
    }
}