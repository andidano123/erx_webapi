/*
 * 版本： 4.0
 * 日期：2017/8/7 10:50:43
 * 
 * 描述：实体类
 * 
 */

using ERX.Services.Dtos.AnswerInfo;
using System;
using System.Collections.Generic;

namespace ERX.Services.Dtos.QuestionCategoryInfo
{
    /// <summary>
    /// 实体类 AccountBaseInfoDto  (属性说明自动提取数据库字段的描述信息)
    /// </summary>
    public class QuestionCategoryInfo
    {
        public int ID { get; set; }
        public string Title{ get; set; }
        public int Sequence{ get; set; }       
    }
    public class QuestionCategoryMoreInfo:QuestionCategoryInfo
    {
        public List<AnswerMoreInfo> AnswerList{ get; set; }
    }
}