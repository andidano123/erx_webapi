using ERX.Services.Dtos;
using ERX.Services.Helpers.Pager;
using System;

namespace ERX.Services.Helpers.Db
{
    public abstract class BaseDataProvider
    {
        private string m_connectionString;
        private DbHelper m_database;
        private PagerManager m_pagerHelper;

        protected internal string ConnectionString
        {
            get
            {
                return this.m_connectionString;
            }
        }

        protected internal DbHelper Database
        {
            get
            {
                return this.m_database;
            }
        }

        protected internal PagerManager PagerHelper
        {
            get
            {
                return this.m_pagerHelper;
            }
        }

        protected internal BaseDataProvider()
        {
        }

        protected internal BaseDataProvider(DbHelper database)
        {
            this.m_database = database;
            this.m_connectionString = database.ConnectionString;
            this.m_pagerHelper = new PagerManager(this.m_database);
        }

        protected internal BaseDataProvider(string connectionString)
        {
            this.m_connectionString = connectionString;
            this.m_database = new DbHelper(connectionString);
            this.m_pagerHelper = new PagerManager(this.m_database);
        }

        protected virtual PagerSet GetPagerSet(PagerParameters prams)
        {
            return this.PagerHelper.GetPagerSet(prams);
        }

        protected virtual ITableProvider GetTableProvider(string tableName)
        {
            return (ITableProvider)new TableProvider(this.Database, tableName);
        }

        public DbResult<T> ExecAction<T>(Func<T> func)
        {
            var message = new Dtos.DbResult<T>();
            try
            {
                message.IsSuccess = true;
                if (string.IsNullOrWhiteSpace(message.ErrorMessage) && message.IsSuccess)
                {
                    message.ErrorMessage = "操作成功";
                }
                message.Data = func();
            }
            catch (Exception e)
            {
                message.IsSuccess = false;
                message.ErrorMessage = e.Message;
                message.ErrorException = e;
            }
            return message;
        }

        public DbResult<T> ExecAction<T>(Func<DbResult<T>> func)
        {
            var message = new Dtos.DbResult<T>();
            try
            {
                message = func();
            }
            catch (Exception e)
            {
                message = new Dtos.DbResult<T>()
                {
                    IsSuccess = false,
                    ErrorException = e,
                    ErrorMessage = e.Message
                };
            }
            return message;
        }


        /// <summary>
        /// 事务执行一个DB操作
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Dtos.DbResult ExecTran(Action<DbHelper> action)
        {
            var message = new Dtos.DbResult()
            {
                IsSuccess = true,
                ErrorMessage="操作成功"
            };
            if (action != null)
            {
                var dbContext = new DbHelper(this.ConnectionString);
                var tran = dbContext.BeginTransaction();
                try
                {
                    action(dbContext);
                    tran.Commit();
                }
                catch(Exception e)
                {
                    tran.Rollback();
                    dbContext.ResetDbProvider();
                    message.IsSuccess = false;
                    message.ErrorException = e;
                    message.ErrorMessage = e.Message;
                }
            }
            return message;
        }
    }
}
