using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApiThrottle;

namespace ERXApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.Filters.Add(new GenericAuthorizeAttribute());
            // Web API routes
            config.MapHttpAttributeRoutes();

            //var cors = new EnableCorsAttribute("https://we.hgfgood.com,https://good.hgfgood.com,https://we.hgf618.com,https://good.hgf618.com,https://hgf618.com,https://www.hgf618.com,http://localhost:3000,https://my.hgf618.com,https://yes.hgf618.com,http://my.hgf618.com,http://yes.hgf618.com,http://we.hgfgood.com,http://good.hgfgood.com", "*", "*");
            var cors = new EnableCorsAttribute("https://we.hgf818.com,https://good.hgf818.com,https://zgzg.hgf618.com,https://nbnb.hgf818.com,https://good.hgf618.com,https://hgf618.com,https://www.hgf618.com,http://localhost:3000,https://my.hgf618.com,https://yes.hgf618.com,http://my.hgf618.com,http://yes.hgf618.com,http://we.hgf818.com,http://good.hgf818.com", "*", "*");
            config.EnableCors(cors);

            //config.EnableCors();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            
            //config.MessageHandlers.Add(new DlmThrottlingHandler()
            //{
            //    Policy = new ThrottlePolicy(perSecond: 50000, perMinute: 5000000, perHour: 500000000)
            //    {
            //        IpThrottling = false,
            //        ClientThrottling = true,
            //        EndpointThrottling = true
            //    },
            //    Repository = new MemoryCacheRepository()
            //});
        }
    }
}
