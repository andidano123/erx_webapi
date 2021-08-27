using ERX.Services.Dtos.AccountInfo;
using ERX.Services.Dtos.QuestionInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ERXApi
{
    public class CheckAccountResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        
        public int UserID { get; set; }
        public int NextStatus { get; set; }     // 1: Finished, 0:New or Answering
        public AccountInfo AccountData { get; set; }
        public QuestionInfo QuestionData { get; set; }
    }
}