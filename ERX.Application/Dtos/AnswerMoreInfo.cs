/*
 * 版本： 4.0
 * 日期：2017/8/7 10:50:43
 * 
 * 描述：实体类
 * 
 */

using System;
using System.Collections.Generic;

namespace ERX.Services.Dtos.AnswerInfo
{
    /// <summary>
    /// 实体类 AccountBaseInfoDto  (属性说明自动提取数据库字段的描述信息)
    /// </summary>
    public class AnswerMoreInfo
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int QuestionID{ get; set; }       
        
        public string Answer { get; set; }
        public string CreatedAt { get; set; }
        public string QuestionTitle { get; set; }
        public int QuestionSequence { get; set; }
        public int QuestionType { get; set; }
    }
}