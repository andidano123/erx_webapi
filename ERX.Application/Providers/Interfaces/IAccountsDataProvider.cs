using ERX.Services.Dtos;
using ERX.Services.Dtos.AccountInfo;
using ERX.Services.Dtos.AnswerInfo;
using ERX.Services.Dtos.QuestionCategoryInfo;
using ERX.Services.Dtos.QuestionInfo;
using ERX.Services.Helpers.Message;
using System.Collections.Generic;
using System.Data;

namespace ERX.Services.Providers.Interfaces
{
    /// <summary>
    /// 帐号库数据层接口
    /// </summary>
    public interface IAccountsDataProvider
    {
        DbResult<AccountInfo> GetUserInfo(int nUserID);
        DbResult<AccountInfo> GetUserInfo(string email);
        DbResult<int> AddNewAccount(string email);
        DbResult<int> AddNewAnswer(int userid, int questionid, int question_sequence, int question_type, string answer);
        DbResult<QuestionInfo> GetQuestion(int questionid);
        DbResult<int> GetLastAnsweredQuestionSequence(int userid);
        DbResult<QuestionInfo> GetNextSequenceQuestion(int sequence);
        DbResult<int> GetQuestionsTotalCount();
        DbResult<int> GetQuestionsAnswerdCount(int userid);
        DbResult<QuestionCategoryInfo> GetQuestionCategory(int categoryid);        
        DbResult<int> UpdateAccountInfo(int userid, int answer_status);
        DbResult<IList<AnswerMoreInfo>> GetAnswerListByCategoryID(int userid, int categoryid);
        DbResult<IList<QuestionCategoryInfo>> GetQuestionCategoryList();


    }
}