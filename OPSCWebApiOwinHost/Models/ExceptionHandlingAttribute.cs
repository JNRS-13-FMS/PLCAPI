using log4net;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;

namespace JNRSWebApiOwinHost.Models
{
    public class ExceptionHandlingAttribute : ExceptionFilterAttribute
    {
        protected readonly ILog log = log4net.LogManager.GetLogger("JNRSLogger");
        public override void OnException(HttpActionExecutedContext context)
        {
            //Log Critical errors
            //Debug.WriteLine(context.Exception);
            log.Error(context.Exception);
            log.Error("-----------------------------------------------------------");
            Console.WriteLine(context.Exception);

            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("An error occurred, please try again or contact the administrator."),
                ReasonPhrase = "Critical Exception"
            });
        }
    }
}
