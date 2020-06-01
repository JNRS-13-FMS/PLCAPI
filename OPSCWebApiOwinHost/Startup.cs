using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Tracing;
using Owin;
using JNRSWebApiOwinHost.Models;
using System.Net;
using JNRSWebApiOwinHost.Filter;

namespace JNRSWebApiOwinHost
{
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            //HttpListener listener = (HttpListener)appBuilder.Properties["System.Net.HttpListener"];
            //listener.AuthenticationSchemes =  AuthenticationSchemes.Anonymous;
            log4net.Config.XmlConfigurator.Configure();
            //config.Services.Replace(typeof(ITraceWriter), new DynamicTrace());
            config.Filters.Add(new ExceptionHandlingAttribute());

            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            //config.Services.Replace(typeof(ITraceWriter), new DynamicTrace());
            //config.Filters.Add(new ExceptionHandlingAttribute());
            //config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            //appBuilder.MapSignalR();
            //appBuilder.UseStaticFiles();

            appBuilder.UseWebApi(config);
        }
    }
}
