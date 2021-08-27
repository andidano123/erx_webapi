using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGFApi
{
    public class LoginUser
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// 密钥
        /// </summary>
        public string EncryptKey { get; set; }
    }
}