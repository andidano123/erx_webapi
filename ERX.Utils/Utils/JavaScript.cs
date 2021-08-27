using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ERX.Utils
{
    /// <summary>
    /// 向页面注册JS、添加标签
    /// </summary>
    public class JavaScript
    {

        #region 注册js脚本到页面
        /// <summary>
        /// 注册js脚本到页面
        /// </summary>
        /// <param name="page">当前页面对象</param>
        /// <param name="js">要输出的js代码</param>
        /// <param name="beforeOnload">是否要让输出的脚本放置在页面底部</param>
        public static void RegJs(System.Web.UI.Page page,string js,bool bottom)
        {
            if(bottom)
                page.ClientScript.RegisterStartupScript(page.GetType(),new Random().Next(1000,9999).ToString(),"<script type=\"text/javascript\" language=\"javascript\">" + js + "</script>");
            else
                page.ClientScript.RegisterClientScriptBlock(page.GetType(),new Random().Next(1000,9999).ToString(),"<script type=\"text/javascript\" language=\"javascript\">" + js + "</script>");
        }
        #endregion

        #region 注册一段脚本包含到页面上
        /// <summary>
        /// 注册一段脚本包含到页面上
        /// </summary>
        /// <param name="url">js文件路径</param>
        public static void RegJs(System.Web.UI.Page page,string url)
        {
            Dictionary<string,string> dic = new Dictionary<string,string>();
            dic.Add("type","text/javascript");
            dic.Add("src",url);
            System.Web.UI.HtmlControls.HtmlGenericControl js = CreateGenericControl("script",dic);
            page.Header.Controls.Add(js);
        }
        #endregion

        #region 注册一段脚本包含到页面底部
        /// <summary>
        /// 注册一段脚本包含到页面底部
        /// </summary>
        /// <param name="page">页面</param>
        /// <param name="url">脚本路径</param>
        public static void RegJsToBottom(System.Web.UI.Page page,string url)
        {
            page.ClientScript.RegisterStartupScript(page.GetType(),new Random().Next().ToString(),"<script src='" + url + "' type='text/javascript'></script>");
        }
        #endregion

        #region 弹出网页警告框，返回或者不返回上一页
        /// <summary>
        /// 弹出警告框，返回或者不返回上一页
        /// </summary>
        /// <param name="message">提示信息</param>
        /// <param name="IsBack">是否返回上一页</param>
        protected static void Alert(string message,bool IsBack)
        {
            if(IsBack)
            {
                HttpContext.Current.Response.Write("<script language='javascript'>alert('" + message.Replace("'",@"\'") + "');history.back();</script>");
            }
            else
            {
                HttpContext.Current.Response.Write("<script language='javascript'>alert('" + message.Replace("'",@"\'") + "');</script>");
            }
        }

        #endregion

        #region 弹出网页警告框，并跳转到指定页

        /// <summary>
        /// 弹出网页警告框，并跳转到指定页
        /// </summary>
        /// <param name="message">提示信息</param>
        /// <param name="url">指定跳转的URL</param>
        protected static void Alert(string message,string url)
        {
            HttpContext.Current.Response.Write("<script language='javascript'>alert('" + message.Replace("'",@"\'") + "');location.href='" + url + "';</script>");
        }

        #endregion

        #region 创建一个HtmlGenericControl类型的标签
        /// <summary>
        /// 创建一个HtmlGenericControl类型的标签
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <param name="dic">属性列表</param>
        /// <returns></returns>
        public static System.Web.UI.HtmlControls.HtmlGenericControl CreateGenericControl(string tagName,IDictionary<string,string> dic)
        {
            System.Web.UI.HtmlControls.HtmlGenericControl obj = new System.Web.UI.HtmlControls.HtmlGenericControl();
            obj.TagName = tagName;
            foreach(KeyValuePair<string,string> kvp in dic)
            {
                obj.Attributes.Add(kvp.Key,kvp.Value);
            }
            return obj;
        }
        #endregion

        #region 创建一个页面头部css引用标签
        /// <summary>
        /// 创建一个页面头部css引用标签
        /// </summary>
        /// <param name="cssUrl">外部css文件路径</param>
        public static System.Web.UI.HtmlControls.HtmlLink CreateCssInclude(string cssUrl)
        {
            System.Web.UI.HtmlControls.HtmlLink AllCss = new System.Web.UI.HtmlControls.HtmlLink();
            AllCss.Href = cssUrl;
            AllCss.Attributes.Add("type","text/css");
            AllCss.Attributes.Add("rel","Stylesheet");
            return AllCss;
        }
        #endregion

        #region 创建一个meta标签
        /// <summary>
        /// 创建一个meta标签
        /// </summary>
        /// <param name="dic">标签属性列表</param>
        public static System.Web.UI.HtmlControls.HtmlMeta CreateMeta(IDictionary<string,string> dic)
        {
            System.Web.UI.HtmlControls.HtmlMeta meta = new System.Web.UI.HtmlControls.HtmlMeta();
            foreach(KeyValuePair<string,string> kvp in dic)
            {
                meta.Attributes.Add(kvp.Key,kvp.Value);
            }
            return meta;
        }
        #endregion
    }
}
