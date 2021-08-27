using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Security;

namespace ERXApi
{
    public class GenericAuthorizeAttribute: AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            base.HandleUnauthorizedRequest(actionContext);
            var response = actionContext.Response = actionContext.Response ?? new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.Unauthorized;
        }
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            // Don't care token for test project.
            return true;
            //try
            //{
            //    //从head中获取token
            //    //if (actionContext.Request.Headers.TryGetValues("token", out IEnumerable<string> values))
            //    if (actionContext.Request.Headers.Authorization.ToString() != "")
            //    {
            //        //var token = actionContext.Request.Headers.Authorization.ToString();
                    
            //        //var userJson = RedisCache.Instance.Get(token);
            //        //if (string.IsNullOrWhiteSpace(userJson) && token != "lkjtsbeal123kjabbl1ldksbbelsds")
            //        //{
            //        //    return false;
            //        //}
            //        //else
            //        //{
                        
            //        //}
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}
            //catch
            //{
            //    return false;
            //}
        }
    }
}