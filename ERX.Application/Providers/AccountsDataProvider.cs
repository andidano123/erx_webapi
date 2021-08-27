using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ERX.Services.Providers.Interfaces;
using ERX.Services.Helpers.Db;
using ERX.Services.Helpers.Message;
using ERX.Services.Helpers.Pager;
using ERX.Services.Dtos;
using System.Text;
using ERX.Services.Dtos.AccountInfo;
using ERX.Services.Dtos.QuestionInfo;
using ERX.Services.Dtos.QuestionCategoryInfo;
using ERX.Services.Dtos.AnswerInfo;

namespace ERX.Services.Providers
{
    /// <summary>
    /// 帐号库数据层
    /// </summary>
    public class AccountsDataProvider : BaseDataProvider, IAccountsDataProvider
    {
        #region 构造方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public AccountsDataProvider(string connString)
            : base(connString)
        {

        }

        #endregion 构造方法

        #region EAX
        
        public DbResult<AccountInfo> GetUserInfo(int nUserID)
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT * FROM AccountInfo WITH(NOLOCK) WHERE UserID={0}", nUserID);
                var dbResult = new DbResult<AccountInfo>();
                dbResult.Data = Database.ExecuteObject<AccountInfo>(sqlQuery);
                if (dbResult.Data != null)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "User is not exists!";
                }
                return dbResult;
            });
        }
        public DbResult<AccountInfo> GetUserInfo(string email)
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT * FROM AccountInfo WITH(NOLOCK) WHERE UserEmail='{0}'", email);
                var dbResult = new DbResult<AccountInfo>();
                dbResult.Data = Database.ExecuteObject<AccountInfo>(sqlQuery);
                if (dbResult.Data != null)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "User is not exists!";
                }
                return dbResult;
            });
        }
        
       

        public DbResult<int> AddNewAccount(string email)
        {
            return this.ExecAction(() =>
            {
                var dbResult = new DbResult<int>();
                string sqlQuery = string.Format(
                    @"SET NOCOUNT ON;INSERT INTO AccountInfo(UserEmail, AnswerStatus) VALUES('{0}', 0);Select @@IDENTITY ", email);
                dbResult.Data = Database.Sql(sqlQuery).ExecuteScalar<int>();
                
                if (dbResult.Data == 0)
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "failed!";
                }
                else
                {
                    dbResult.IsSuccess = true;
                }
                return dbResult;
            });
        }
        public DbResult<int> AddNewAnswer(int userid, int questionid, int question_sequence, int question_type, string answer)
        {
            return this.ExecAction(() =>
            {
                var dbResult = new DbResult<int>();
                string sqlQuery = string.Format(
                    @"INSERT INTO AnswerInfo(UserID, QuestionID, Answer, QuestionSequence, QuestionType) VALUES({0}, {1}, '{2}',{3},{4}) ", userid, questionid, answer, question_sequence, question_type);
                dbResult.Data = Database.ExecuteObject<int>(sqlQuery);
                if (dbResult.Data == 0)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "failed!";
                }
                return dbResult;
            });
        }


        public DbResult<QuestionInfo> GetQuestion(int questionid)
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT A.*, B.Title As CategoryTitle, C.Type as QuestionType, C.Content as TypeContent FROM QuestionInfo as A, QuestionCategoryInfo as B, QuestionTypeInfo as C WHERE A.ID='{0}' and B.ID=A.CategoryID and C.ID=A.TypeID", questionid);
                var dbResult = new DbResult<QuestionInfo>();
                dbResult.Data = Database.ExecuteObject<QuestionInfo>(sqlQuery);
                if (dbResult.Data != null)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "Question is not exists!";
                }
                return dbResult;
            });
        }

        public DbResult<int> GetLastAnsweredQuestionSequence(int userid)
        {
            return this.ExecAction(() =>
            {            
                string sqlQuery = string.Format(
                    @"SELECT TOP 1 QuestionSequence FROM AnswerInfo WITH(NOLOCK) WHERE UserID='{0}' order by CreatedAt DESC", userid);
                var dbResult = new DbResult<int>();
                dbResult.Data = Database.Sql(sqlQuery).ExecuteScalar<int>();
                if (dbResult.Data > 0)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "Question is not exists!";
                }
                return dbResult;
            });
        }

        public DbResult<QuestionInfo> GetNextSequenceQuestion(int sequence)
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(                    
                    @"SELECT TOP 1 A.*, B.Title as CategoryTitle, C.Type as QuestionType, C.Content As TypeContent FROM QuestionInfo as A, QuestionCategoryInfo as B, QuestionTypeInfo as C WHERE A.Sequence > {0} and B.ID = A.CategoryID and C.ID = A.TypeID ORDER BY A.Sequence ASC", sequence);            
            var dbResult = new DbResult<QuestionInfo>();
                dbResult.Data = Database.ExecuteObject<QuestionInfo>(sqlQuery);
                if (dbResult.Data != null)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "Question is not exists!";
                }
                return dbResult;

            });
        }
        public DbResult<int> GetQuestionsTotalCount()
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT COUNT(ID) FROM QuestionInfo WITH(NOLOCK)");
                var dbResult = new DbResult<int>();
                dbResult.Data = Database.Sql(sqlQuery).ExecuteScalar<int>();                
                return dbResult;
            });
        }
        public DbResult<int> GetQuestionsAnswerdCount(int userid)
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT COUNT(ID) FROM AnswerInfo WITH(NOLOCK) WHERE UserID={0}", userid);
                var dbResult = new DbResult<int>();
                dbResult.Data = Database.Sql(sqlQuery).ExecuteScalar<int>();                
                return dbResult;
            });
        }


        public DbResult<QuestionCategoryInfo> GetQuestionCategory(int categoryid)
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT * FROM QuestionCategoryInfo WITH(NOLOCK) WHERE ID='{0}'", categoryid);
                var dbResult = new DbResult<QuestionCategoryInfo>();
                dbResult.Data = Database.ExecuteObject<QuestionCategoryInfo>(sqlQuery);
                if (dbResult.Data != null)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "QuestionCategory is not exists!";
                }
                return dbResult;
            });
        }

        public DbResult<IList<AnswerMoreInfo>> GetAnswerListByCategoryID(int userid, int categoryid)
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT A.Answer, A.QuestionSequence, A.QuestionType, B.Title as QuestionTitle FROM AnswerInfo as A, QuestionInfo as B WHERE A.UserID={0} and A.QuestionID=B.ID and B.CategoryID={1} ORDER BY B.Sequence ASC", userid, categoryid);
                return Database.Sql(sqlQuery).QueryList<AnswerMoreInfo>();
            });
        }
        public DbResult<IList<QuestionCategoryInfo>> GetQuestionCategoryList()
        {
            return this.ExecAction(() =>
            {
                string sqlQuery = string.Format(
                    @"SELECT * FROM QuestionCategoryInfo ORDER BY Sequence ASC");
                return Database.Sql(sqlQuery).QueryList<QuestionCategoryInfo>();
            });
        }
        public DbResult<int> UpdateAccountInfo(int userid, int answer_status)
        {
            return this.ExecAction(() =>
            {
                var dbResult = new DbResult<int>();
                string sqlQuery = string.Format(
                    @"UPDATE AccountInfo SET AnswerStatus={0}, FinishAnswerAt='{1}' WHERE UserID={2}", answer_status, DateTime.Now,  userid);
                dbResult.Data = Database.ExecuteObject<int>(sqlQuery);
                if (dbResult.Data == 0)
                {
                    dbResult.IsSuccess = true;
                }
                else
                {
                    dbResult.IsSuccess = false;
                    dbResult.ErrorMessage = "failed!";
                }
                return dbResult;
            });
        }
        #endregion
    }
}