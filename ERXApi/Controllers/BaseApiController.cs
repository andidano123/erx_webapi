using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Cors;
using System.Web.Http.Results;

namespace ERXApi.Controllers
{
    //[Authorize]
    //[EnableCors(origins: "http://47.243.36.207", headers: "*", methods: "*")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]

    public abstract class BaseApiController : ApiController
    {
        protected override JsonResult<T> Json<T>(T content, JsonSerializerSettings serializerSettings, Encoding encoding)
        {
            serializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            return base.Json<T>(content, serializerSettings, encoding);
        }

        /// <summary>
        /// Author:codeo.cn
        /// </summary>
        /// <returns></returns>
        protected string GetIPAddress()
        {
            string ipval = string.Empty;

            if (string.IsNullOrWhiteSpace(ipval))
            {
                ipval = HttpContext.Current.Request["ip"];
            }
            if (string.IsNullOrEmpty(ipval))
            {
                ipval = HttpContext.Current.Request.ServerVariables["HTTP_X_REAL_IP"];
            }
            if (string.IsNullOrEmpty(ipval))
            {
                ipval = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            }
            if (string.IsNullOrEmpty(ipval))
            {
                ipval = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }
            if (string.IsNullOrEmpty(ipval))
            {
                ipval = HttpContext.Current.Request.UserHostAddress;
            }
            if (!string.IsNullOrEmpty(ipval) && IsIPAddress(ipval))
            {
                if (ipval.IndexOf(",") > -1)
                {
                    var ipadd = ipval.Split(',');
                    ipval = ipadd[0];
                }
            }
            //局域网IP
            if (string.Compare(ipval, "::1") == 0)
            {
                ipval = "127.0.0.1";
            }

            return ipval;
        }
        /// <summary>
        /// Author:codeo.cn  
        /// </summary>
        /// <param name="strIp">待判断的IP地址</param>
        /// <returns>true or false</returns>
        private bool IsIPAddress(string strIp)
        {
            if (strIp == null || strIp == string.Empty || strIp.Length < 7 || strIp.Length > 15)
            {
                return false;
            }

            string strRegformat = @"^d{1,3}[.]d{1,3}[.]d{1,3}[.]d{1,3}___FCKpd___0quot";

            Regex regex = new Regex(strRegformat, RegexOptions.IgnoreCase);

            return regex.IsMatch(strIp);
        }

        public bool IsEmailAddress(string strEmail)
        {
            string strRegformat = @"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*";
            Regex regex = new Regex(strRegformat, RegexOptions.IgnoreCase);
            return regex.IsMatch(strEmail);
        }
    }
}
