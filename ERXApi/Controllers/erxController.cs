using Google.Authenticator;
using ERX.Service;
using ERX.Services.Dtos;
using ERX.Services.Dtos.AccountInfo;
using ERX.Services.Helpers.Message;
using ERX.Services.Providers.Interfaces;
using ERX.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using ERX.Services.Dtos.QuestionInfo;
using ERX.Services.Dtos.QuestionCategoryInfo;
using ERX.Services.Dtos.AnswerInfo;
using System.Net.Http.Headers;

namespace ERXApi.Controllers
{
    public class erxController : BaseApiController
    {        
        [HttpPost]
        public IHttpActionResult check_account([FromBody]dynamic input)
        {
            CheckAccountResponseModel res = new CheckAccountResponseModel();
            res.Code = 200;
            res.Message = "Success";

            string email = input.email;

            var result = ServiceManage<IAccountsDataProvider>.Instance.GetUserInfo(email);
            int last_question_seq = 0;
            // Already Exists, Continue or Show result
            if (result.IsSuccess)
            {
                //Finish answering
                if (result.Data.AnswerStatus == 1)
                {                   
                    res.NextStatus = 1;
                    res.AccountData = result.Data;
                }
                else
                {
                    res.NextStatus = 0;
                    var last_result = ServiceManage<IAccountsDataProvider>.Instance.GetLastAnsweredQuestionSequence(result.Data.UserID);
                    last_question_seq = Convert.ToInt32(last_result.Data);
                }
                res.UserID = result.Data.UserID;
            }
            else
            //Not Exist, then start to answer
            {
                res.NextStatus = 0;
                last_question_seq = 0;
                var new_userid = ServiceManage<IAccountsDataProvider>.Instance.AddNewAccount(email);
                if(new_userid.IsSuccess)
                    res.UserID = new_userid.Data;
                else
                {
                    res.Code = 500;
                    res.Message = "Server Error";
                }
            }
            if(res.NextStatus == 0 && res.Code == 200)
            {
                var question_result = ServiceManage<IAccountsDataProvider>.Instance.GetNextSequenceQuestion(last_question_seq);
                //It must return with correct question id
                if (!question_result.IsSuccess)
                {
                    res.Code = 500;
                    res.Message = "Server Error";
                }
                else
                {
                    // The question to answer next.
                    res.QuestionData = question_result.Data;
                }
            }
            return Json(res);
        }


        [HttpPost]
        public IHttpActionResult check_answer([FromBody]dynamic input)
        {
            CheckAnswerResponseModel res = new CheckAnswerResponseModel();
            res.Code = 200;
            res.Message = "Success";

            
            int userid = input.userid;
            string answer = input.answer;   // if question type is multiple choice, then answer is jsonarry, and otherwise it is text answer.
            int qid = input.qid;            //question id

            var account_result = ServiceManage<IAccountsDataProvider>.Instance.GetUserInfo(userid);            
            int last_question_seq = 0;
            
            //incorrect situation
            if ((account_result.IsSuccess && account_result.Data.AnswerStatus == 1 )
                || (!account_result.IsSuccess) || qid == 0 || userid == 0)
            {
                res.Code = 500;
                res.Message = "Wrong Form Data";
                return Json(res);
            }

            var answer_count_result = ServiceManage<IAccountsDataProvider>.Instance.GetQuestionsAnswerdCount(userid);
            var question_count_result = ServiceManage<IAccountsDataProvider>.Instance.GetQuestionsTotalCount();
            //incorrect situation
            if (answer_count_result.Data >= question_count_result.Data)
            {
                res.Code = 500;
                res.Message = "Wrong Form Data";
                return Json(res);
            }
           

            var question_info_result = ServiceManage<IAccountsDataProvider>.Instance.GetQuestion(qid);
            QuestionInfo question_info = question_info_result.Data;

            var new_answer_result = ServiceManage<IAccountsDataProvider>.Instance.AddNewAnswer(userid, qid, question_info.Sequence, question_info.QuestionType, answer);
            if (!new_answer_result.IsSuccess)
            {
                res.Code = 500;
                res.Message = "Wrong Form Data";
                return Json(res);
            }

            //The question has non-allowed anwer.
            bool finished = false;
            if(question_info.NotAllowed != "")
            {
                //string[] non_allowed_list = question_info.NotAllowed.Split(',');
                JArray non_allowd = JArray.Parse(question_info.NotAllowed);
                if(question_info.QuestionType == 3)
                {
                    JArray answer_array = JArray.Parse(answer);
                    foreach (var jObject in non_allowd)
                    {
                        string baned_answer = jObject.ToString();
                        foreach (var item in answer_array)
                        {
                            if (baned_answer == item.ToString())
                            {
                                // Not Allowed Answer
                                finished = true;
                                break;
                            }
                        }
                    }
                }else if(question_info.QuestionType == 2)
                {
                    foreach (var jObject in non_allowd)
                    {
                        if(jObject.ToString() == answer)
                        {
                            // Not Allowed Answer
                            finished = true;
                            break;
                        }
                    }

                }
            }
            if (answer_count_result.Data + 1 == question_count_result.Data)
                finished = true;

            if (finished)
            {
                var update_result = ServiceManage<IAccountsDataProvider>.Instance.UpdateAccountInfo(userid, 1);
                res.NextStatus = 1;                
                res.AccountData = account_result.Data;
                res.AccountData.AnswerStatus = 1;
                res.AccountData.FinishAnswerAt = DateTime.Now.ToString();
            }
            else
            {
                //if not finished, then select next question
                var last_result = ServiceManage<IAccountsDataProvider>.Instance.GetLastAnsweredQuestionSequence(account_result.Data.UserID);
                last_question_seq = Convert.ToInt32(last_result.Data);
                var question_result = ServiceManage<IAccountsDataProvider>.Instance.GetNextSequenceQuestion(last_question_seq);
                if (!question_result.IsSuccess)
                {
                    res.Code = 500;
                    res.Message = "Server Error";
                }
                else
                {
                    // The question to answer next.
                    res.QuestionData = question_result.Data;
                }
            }
            return Json(res);
        }
        [HttpPost]
        public IHttpActionResult get_answer_list([FromBody]dynamic input)
        {
            int userid = input.userid;
            AnswerListResponseModel res = new AnswerListResponseModel();
            res.Code = 200;
            res.Message = "Success";

            var result = ServiceManage<IAccountsDataProvider>.Instance.GetQuestionCategoryList();
            res.CategoryList = new List<QuestionCategoryMoreInfo>();

            if (result.Data != null)
            {
                for(int i = 0; i< result.Data.Count; i++)
                {
                    QuestionCategoryMoreInfo info = new QuestionCategoryMoreInfo();
                    info.Sequence = result.Data[i].Sequence;
                    info.ID = result.Data[i].ID;
                    info.Title = result.Data[i].Title;                    
                    var answer_result = ServiceManage<IAccountsDataProvider>.Instance.GetAnswerListByCategoryID(userid, info.ID);                    
                    if(answer_result.Data != null)
                    {
                        info.AnswerList = new List<AnswerMoreInfo>();
                        for (int j = 0; j < answer_result.Data.Count; j++)
                        {
                            info.AnswerList.Add(answer_result.Data[j]);
                        }                        
                    }
                    res.CategoryList.Add(info);
                }
            }
            return Json(res);
        }

        [HttpGet]
        public HttpResponseMessage download_csv(int userid)
        {

            StringBuilder sb = new StringBuilder();

            var result = ServiceManage<IAccountsDataProvider>.Instance.GetQuestionCategoryList();
            IEnumerable<QuestionCategoryMoreInfo> categoryList = new List<QuestionCategoryMoreInfo>();

            if (result.Data != null)
            {
                for (int i = 0; i < result.Data.Count; i++)
                {
                    
                    var answer_result = ServiceManage<IAccountsDataProvider>.Instance.GetAnswerListByCategoryID(userid, result.Data[i].ID);
                    if (answer_result.Data != null)
                    {
                        sb.AppendFormat(
                           "{0}{1}",
                           result.Data[i].Title,
                           Environment.NewLine);
                        for (int j = 0; j < answer_result.Data.Count; j++)
                        {
                            if(answer_result.Data[j].QuestionType == 3)
                            {
                                JArray temp = JArray.Parse(answer_result.Data[j].Answer);
                                string answer_str = "";
                                int c = 0;
                                foreach (var item in temp)
                                {
                                    answer_str = item.ToString();
                                    c++;
                                    if (c != temp.Count)
                                        answer_str += ",";
                                }
                                sb.AppendFormat("{0}.  {1}:   {2}{3}",
                                   answer_result.Data[j].QuestionSequence,
                                   answer_result.Data[j].QuestionTitle,
                                   answer_str,
                                   Environment.NewLine);
                            }
                            else
                            {
                                sb.AppendFormat(
                               "{0}.  {1}:   {2}{3}",
                               answer_result.Data[j].QuestionSequence,
                               answer_result.Data[j].QuestionTitle,
                               answer_result.Data[j].Answer,
                               Environment.NewLine);
                            }
                            
                        }
                        sb.AppendFormat(
                          "{0}",
                          Environment.NewLine);
                    }
                   
                }
            }

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(sb.ToString());
            writer.Flush();
            stream.Position = 0;

            HttpResponseMessage csv = new HttpResponseMessage(HttpStatusCode.OK);
            csv.Content = new StreamContent(stream);
            csv.Content.Headers.ContentType =
                new MediaTypeHeaderValue("text/csv");
            csv.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "Download.csv" };
            return csv;
        }

    }
}
