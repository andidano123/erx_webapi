/*
 * 版本： 4.0
 * 日期：2017/8/7 10:50:43
 * 
 * 描述：实体类
 * 
 */

using System;
using System.Collections.Generic;

namespace ERX.Services.Dtos.QuestionInfo
{
    /// <summary>
    /// 实体类 AccountBaseInfoDto  (属性说明自动提取数据库字段的描述信息)
    /// </summary>
    public class QuestionInfo
    {
        public int ID { get; set; }
        public string Title{ get; set; }
        public int CategoryID{ get; set; }
        public int TypeID { get; set; }
        public int Sequence { get; set; }
        public string NotAllowed { get; set; }
        public string CreatedAt { get; set; }
        public string CategoryTitle { get; set; }
        public int QuestionType { get; set; }
        public string TypeContent { get; set; }
    }
}