/*
 * 版本： 4.0
 * 日期：2017/8/7 10:50:43
 * 
 * 描述：实体类
 * 
 */

using System;
using System.Collections.Generic;

namespace ERX.Services.Dtos.QuestionTypeInfo
{
    /// <summary>
    /// 实体类 AccountBaseInfoDto  (属性说明自动提取数据库字段的描述信息)
    /// </summary>
    public class QuestionTypeInfo
    {
        public int ID { get; set; }
        public string Title{ get; set; }
        public int Type { get; set; }
        public string Content { get; set; }
    }
}