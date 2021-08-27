using ERX.Services.Dtos.AccountInfo;
using ERX.Services.Dtos.QuestionCategoryInfo;
using ERX.Services.Dtos.QuestionInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERXApi
{
    public class AnswerListResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public List<QuestionCategoryMoreInfo> CategoryList { get; set; }

    }

}