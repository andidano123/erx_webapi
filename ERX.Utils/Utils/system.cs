using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;

namespace Game.Web
{
    public class system
    {
        /// <summary>
        /// 打印字符串
        /// </summary>
        /// <param name="msg"></param>
        public static void print(string msg)
        {
            HttpContext.Current.Response.Write(msg);
        }
        public static void printEx(string msg)
        {
            HttpContext.Current.Response.Write(msg);
            HttpContext.Current.Response.End();
        }
        /// <summary>
        /// 过滤文本框前后空格符
        /// </summary>
        /// <param name="textBoxId"></param>
        /// <returns></returns>
        public static string replaceTextBox(TextBox textBoxId)
        {
            return textBoxId.Text.ToString().Trim();
        }
        public static void LiteralValue(Literal LiteralId, string Str)
        {
            LiteralId.Text = Str;
        }

        /// <summary>
        /// 获取Dataset 中的Field 字段数据值
        /// </summary>
        /// <param name="Ds">DataSet 名称</param>
        /// <param name="Field">字段同名称</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string dsField(DataSet Ds, string Field)
        {
            string Result = "";

            if (dsCount(Ds))
            {
                Result = Ds.Tables[0].Rows[0][Field].ToString();
            }

            return Result;
        }
        /// <summary>
        /// 获取DataSet 中 Index 索引的值
        /// </summary>
        /// <param name="Ds">DataSet 名称</param>
        /// <param name="Index">索引数</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string dsIndex(DataSet Ds, int Index)
        {
            string Result = "";

            if (dsCount(Ds))
            {
                Result = Ds.Tables[0].Rows[0][Index].ToString();
            }

            return Result;
        }
        /// <summary>
        /// 判断Ds 中是否有记录
        /// </summary>
        /// <param name="Ds"></param>
        /// <returns></returns>
        public static bool dsCount(DataSet Ds)
        {
            bool Result = false;

            if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
            {
                Result = true;
            }

            return Result;
        }
        /// <summary>
        /// TextBox 赋值
        /// </summary>
        /// <param name="TextBoxId">TextBox 名称</param>
        /// <param name="Str">TextBox 所赋的值</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static void TextBoxValue(TextBox TextBoxId, string Str)
        {
            TextBoxId.Text = Str;
        }


        /// <summary>
        /// 返回DropDownList的index值 Value值
        /// </summary>
        /// <param name="DropDownList1"></param>
        /// <param name="str">Value值</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static DropDownList dropDownList_Index(DropDownList DropDownList1, string Str)
        {
            int i = 0;
            for (i = 0; i <= Convert.ToInt32(DropDownList1.Items.Count - 1); i++)
            {
                if (DropDownList1.Items[i].Value == Str)
                {
                    DropDownList1.SelectedIndex = i;
                }
            }
            return DropDownList1;
        }
        /// <summary>
        /// 控制状态
        /// </summary>
        public enum ControlStatus
        {
            None, 黑名单, 白名单
        }
        /// <summary>
        /// 控制类型
        /// </summary>
        public enum ControlType
        {
            None, 时间控制, 金币变更控制
        }
        public static string Replace_TextBox(TextBox TextBoxId)
        {
            return TextBoxId.Text.ToString().Trim().Replace("'", "''");
        }
        public static string RndNumber()
        {
            Random Rnd = new Random();
            int Result = Rnd.Next(0, 99999);    //  

            return DateTime.Now.ToString("yyyyMMddHHmmss") + Result;     //.Replace(":", "").Replace("/", "").Replace(" ", "") 
        }
        public static string PostData(string Url, string Parameter)
        {
            //Parameter ：aa=1&b=22&c=3
            string Result = null;

            try
            {
                //转换数据
                byte[] postDataBytes = Encoding.UTF8.GetBytes(Parameter);   //GetEncoding("gb2312")
                WebRequest req = WebRequest.Create(Url);
                //HttpWebRequest req = HttpWebRequest.Create(Url) as HttpWebRequest;
                //req.UseDefaultCredentials = true;
                //req.ServicePoint.ConnectionLimit = 1000;
                //req.ServicePoint.Expect100Continue = false;

                //ServicePoint


                req.Method = "POST";
                //req.Accept = "application/json";
                req.ContentType = "application/x-www-form-urlencoded";     //"application/json";       // 
                int DataLength = postDataBytes.Length;
                req.ContentLength = DataLength;

                //提交数据
                Stream reqStream = req.GetRequestStream();

                reqStream.Write(postDataBytes, 0, DataLength);
                reqStream.Close();

                //获取返回值
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);

                Result = sr.ReadToEnd();

                sr.Close();
                resp.Close();
                //req.Abort();
            }
            catch (Exception ex)
            {
                string Memo = "Url：" + Url + "，Parameter：" + Parameter + BrMark() + ex.Message + BrMark() + ex.StackTrace;
                //ErrorAdd("PostData 请求异常", Memo);

            }

            return Result;
        }
        public static string BrMark()
        {
            return "</br>";
        }

    }
}