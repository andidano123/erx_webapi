using ERX.Services.Helpers.Db;
using ERX.Services.Helpers.Message;
using ERX.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ERX.Services.Providers
{
    public static class DbHelperExtensions
    {
        /// <summary>
        /// 执行一条SQL语句
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static SqlCommand Sql(this DbHelper db,string sql)
        {
            return new SqlCommand(db, sql);
        }
        /// <summary>
        /// 执行一条SQL语句
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static SqlCommand Sql(this DbHelper db, SqlBuilder sql)
        {
            return new SqlCommand(db, sql.ToString());
        }
        /// <summary>
        /// 执行一条SQL语句
        /// </summary>
        /// <param name="db"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static SpCommand Sp(this DbHelper db, string sql)
        {
            return new SpCommand(db, sql);
        }

        /// <summary>
        /// 拼接SQL条件
        /// 不要在条件前面加where或者and
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static SqlCommand AddWhere(this SqlCommand command, string condition, string paraName, object paraValue)
        {
            return command.AddWhere(condition).AddInputParameter(paraName,paraValue);
        }

        /// <summary>
        /// 拼接SQL条件
        /// 不要在条件前面加where或者and
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static SqlCommand AddWhere(this SqlCommand command, string condition, IDictionary<string, object> paras)
        {
            return command.AddWhere(condition).AddInputParameter(paras);
        }

        /// <summary>
        /// 拼接SQL条件
        /// 不要在条件前面加where或者and
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static SqlCommand AddWhere<TClass>(this SqlCommand command, string condition, TClass inputParameters)
            where TClass : class
        {
            return command.AddWhere(condition).AddInputParameter(inputParameters);
        }

        /// <summary>
        /// 添加参数
        /// 不支持in的参数化
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static TCommand AddParameter<TCommand>(this TCommand command, DbParameter parameter) 
            where TCommand: DataCommand
        {
            command.Parameters.Add(parameter);
            return command;
        }
        /// <summary>
        /// 添加参数
        /// 不支持in的参数化
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static TCommand AddParameter<TCommand>(this TCommand command, IList<DbParameter> parameters) 
            where TCommand : DataCommand
        {
            command.Parameters.AddRange(parameters);
            return command;
        }
        /// <summary>
        /// 添加输入参数
        /// 支持in的参数化
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static TCommand AddInputParameter<TCommand>(this TCommand command, string paraName, object paraValue)
            where TCommand : DataCommand
        {
            if (paraValue is System.Collections.ICollection)
            {
                var valList = paraValue as System.Collections.ICollection;
                var keys = new List<string>();
                foreach (var item in valList)
                {
                    var key = $"{paraName}_{keys.Count}";
                    var val = item;
                    var para = command.Database.MakeInParam(key, val);
                    command.Parameters.Add(para);
                    keys.Add("@" + key);
                }

                var str = string.Join(",", keys);
                command.Sql = command.Sql.Replace($"@{paraName}", str);
            }
            else
            {
                var p = command.Database.MakeInParam(paraName, paraValue);
                command.Parameters.Add(p);
            }
            return command;
        }

        /// <summary>
        /// 添加输出参数
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static TCommand AddOutputParameter<TCommand>(this TCommand command, string paraName, Type outType)
            where TCommand : SpCommand
        {
            var para = command.Database.MakeOutParam(paraName, outType,500);
            command.Parameters.Add(para);
            return command;
        }

        /// <summary>
        /// 添加输出参数
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static TCommand AddOutputParameter<TCommand>(this TCommand command, IDictionary<string, Type> paras)
            where TCommand : SpCommand
        {
            foreach (var item in paras)
            {
                var p = command.Database.MakeOutParam(item.Key, item.Value,500);
                command.Parameters.Add(p);
            }
            return command;
        }

        /// <summary>
        /// 添加输入参数
        /// 支持in的参数化
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static TCommand AddInputParameter<TCommand>(this TCommand command, IDictionary<string, object> paras) 
            where TCommand : DataCommand
        {
            foreach (var obj in paras)
            {
                if (obj.Value is System.Collections.ICollection)
                {
                    var valList = obj.Value as System.Collections.ICollection;
                    var keys = new List<string>();
                    foreach (var item in valList)
                    {
                        var key = $"{obj.Key}_{keys.Count}";
                        var val = item;
                        var para = command.Database.MakeInParam(key, val);
                        command.Parameters.Add(para);
                        keys.Add("@" + key);
                    }

                    var str = string.Join(",", keys);
                    command.Sql = command.Sql.Replace($"@{obj.Key}", str);
                }
                else
                {
                    var p = command.Database.MakeInParam(obj.Key, obj.Value);
                    command.Parameters.Add(p);
                }
            }
            return command;
        }

        /// <summary>
        /// 添加输入参数
        /// 支持in的参数化
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static TCommand AddInputParameter<TCommand, TClass>(this TCommand command, TClass inputParameters) 
            where TCommand : DataCommand 
            where TClass:class
        {
            if (inputParameters != null)
            {
                command = GetParameters(command, inputParameters, true);
            }
            return command;
        }

        /// <summary>
        /// 添加排序
        /// 只有返回list的时候有效
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static SqlCommand OrderBy(this SqlCommand command, string orderBy)
        {
            command.OrderBy = orderBy;
            return command;
        }

        /// <summary>
        /// 返回一个list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static IList<T> QueryList<T>(this DataCommand command)
            where T : class
        {
            var sql = command.Sql;
            if (command is SqlCommand)
            {
                sql += $" order by {(command as SqlCommand).OrderBy} ";
            }
            var ds = command.Database.ExecuteDataset(command.Type, command.Sql, command.Parameters.ToArray());
            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
            return null;
        }

        /// <summary>
        /// 返回一个list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static IList<T> QueryList<T, TReturn>(this SpCommand command, out TReturn retVal)
            where T : class
            where TReturn:struct
        {
            var sql = command.Sql;
            command.Parameters.Add(command.Database.MakeReturnParam());
            var ds = command.Database.ExecuteDataset(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<TReturn>();
            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
            return null;
        }

        /// <summary>
        /// 返回一个list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static IList<T> QueryList<T, TReturn>(this SpCommand command, out TReturn retVal, out dynamic outputParameters)
            where T : class
            where TReturn : struct
        {
            var sql = command.Sql;
            command.Parameters.Add(command.Database.MakeReturnParam());
            var ds = command.Database.ExecuteDataset(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<TReturn>();
            outputParameters = GetOutputParameters(command.Parameters);
            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
            return null;
        }
        /// <summary>
        /// 返回一个list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static IList<T> QueryList<T>(this SpCommand command, out dynamic outputParameters)
            where T : class
        {
            var sql = command.Sql;

            var ds = command.Database.ExecuteDataset(command.Type, command.Sql, command.Parameters.ToArray());
            outputParameters = GetOutputParameters(command.Parameters);
            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
            return null;
        }

        /// <summary>
        /// 返回一个Layui的数据页
        /// 必须要有order by
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="pageIndex">从1开始</param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static Pager<T> QueryPager<T>(this SqlCommand command, int pageIndex, int pageSize) where T : class
        {
            var m = command.Sql.Replace("\r\n", " ").IndexOf(" from ", StringComparison.OrdinalIgnoreCase);
            var countSql = "select count(*) " + command.Sql.Substring(m);
            return command.QueryPager<T>(countSql, pageIndex, pageSize);
        }
        /// <summary>
        /// 返回一个Layui的数据页
        /// 必须要有order by
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="countSql">当不能正确分割SQL时候，可以传入count的sql</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static Pager<T> QueryPager<T>(this SqlCommand command, string countSql, int pageIndex, int pageSize) where T : class
        {
            var sql = command.Sql;
            var skip = (pageIndex - 1) * pageSize;
            if (!string.IsNullOrWhiteSpace(command.OrderBy))
            {
                sql += $" order by {command.OrderBy} ";
            }
            sql += $" OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            int count = command.Database.Sql(countSql).AddParameter(command.Parameters).ExecuteScalar<int>();
            var list = command.Database.Sql(sql).AddParameter(command.Parameters).QueryList<T>();
            return new Pager<T>(pageIndex, pageSize,count, list);
        }
        /// <summary>
        /// 返回一个实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T QuerySingle<T>(this DataCommand command) where T : class
        {
            DataSet ds = command.Database.ExecuteDataset(command.Type,command.Sql,command.Parameters.ToArray());
            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
            return default(T);
        }

        /// <summary>
        /// 返回一个实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T QuerySingle<T,TReturn>(this SpCommand command, out TReturn retVal) 
            where T : class
            where TReturn:struct
        {
            DataSet ds = command.Database.ExecuteDataset(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<TReturn>();

            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
            return default(T);
        }

        /// <summary>
        /// 返回一个实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T QuerySingle<T, TReturn>(this SpCommand command, out TReturn retVal, out dynamic outputParameters) 
            where T : class
            where TReturn : struct
        {
            DataSet ds = command.Database.ExecuteDataset(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<TReturn>();
            outputParameters = GetOutputParameters(command.Parameters);

            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
            return default(T);
        }
        /// <summary>
        /// 返回一个实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T QuerySingle<T>(this SpCommand command, out dynamic outputParameters)
            where T : class
        {
            DataSet ds = command.Database.ExecuteDataset(command.Type, command.Sql, command.Parameters.ToArray());
            outputParameters = GetOutputParameters(command.Parameters);

            if (Validate.CheckedDataSet(ds))
                return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
            return default(T);
        }

        /// <summary>
        /// 返回一个单值
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T ExecuteScalar<T>(this DataCommand command) where T : struct
        {
            object result = command.Database.ExecuteScalar(command.Type, command.Sql, command.Parameters.ToArray());
            if (result == null)
            {
                return default(T);
            }
            else
            {
                return result.To<T>();
            }
        }

        /// <summary>
        /// 返回一个单值
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T ExecuteScalar<T,TReturn>(this SpCommand command, out TReturn retVal)
            where T : struct
            where TReturn:struct
        {
            object result = command.Database.ExecuteScalar(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<TReturn>();
            if (result == null)
            {
                return default(T);
            }
            else
            {
                return result.To<T>();
            }
        }

        /// <summary>
        /// 返回一个单值
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T ExecuteScalar<T,TReturn>(this SpCommand command,out TReturn retVal, out dynamic outputParameters) 
            where T : struct
            where TReturn : struct
        {
            object result = command.Database.ExecuteScalar(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<TReturn>();
            outputParameters = GetOutputParameters(command.Parameters);
            if (result == null)
            {
                return default(T);
            }
            else
            {
                return result.To<T>();
            }
        }

        /// <summary>
        /// 返回一个单值
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static T ExecuteScalar<T>(this SpCommand command, out dynamic outputParameters)
            where T : struct
        {
            object result = command.Database.ExecuteScalar(command.Type, command.Sql, command.Parameters.ToArray());
            outputParameters = GetOutputParameters(command.Parameters);
            if (result == null)
            {
                return default(T);
            }
            else
            {
                return result.To<T>();
            }
        }

        /// <summary> 
        /// 执行SQL，返回受影响的行数
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(this DataCommand command)
        {
            return command.Database.ExecuteNonQuery(command.Type, command.Sql, command.Parameters.ToArray());
        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="command"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery<T>(this SpCommand command, out T retVal) where T : struct
        {

            command.Parameters.Add(command.Database.MakeReturnParam());
            var num = command.Database.ExecuteNonQuery(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<T>();

            return num;

        }

        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="command"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery<T>(this SpCommand command, out T retVal, out dynamic outputParameters) where T : struct
        {

            command.Parameters.Add(command.Database.MakeReturnParam());
            var num = command.Database.ExecuteNonQuery(command.Type, command.Sql, command.Parameters.ToArray());
            //获取return值
            retVal = command.Parameters.Where(p => p.Direction == ParameterDirection.ReturnValue).First().Value.To<T>();

            outputParameters = GetOutputParameters(command.Parameters);
            return num;
        }
        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="command"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(this SpCommand command, out dynamic outputParameters) 
        {

            command.Parameters.Add(command.Database.MakeReturnParam());
            var num = command.Database.ExecuteNonQuery(command.Type, command.Sql, command.Parameters.ToArray());

            outputParameters = GetOutputParameters(command.Parameters);
            return num;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T To<T>(this object obj)
        {
            var type = typeof(T);
            if (type.IsEnum)
            {
                throw new InvalidCastException("不支持到Enum的转换");
            }
            else if ((type.IsValueType && !type.IsEnum) || type == typeof(string))
            {
                return (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
            }
            else if (type.IsClass || type.IsInterface)
            {
                return (T)obj;
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        private static TCommand GetParameters<TCommand>(TCommand command, object parameters, bool isInput = true)
            where TCommand:DataCommand
        {
            if (parameters is IEnumerable<DbParameter>)
            {
                command.Parameters.AddRange(parameters as IEnumerable<DbParameter>);
            }
            else
            {
                var properties = parameters.GetType().GetProperties();
                foreach (PropertyInfo properyty in properties)
                {
                    var value = properyty.GetValue(parameters, null);
                    if (properyty.PropertyType.IsEnum)
                        value = Convert.ToInt64(value);
                    if (value == null || value.Equals(DateTime.MinValue)) value = DBNull.Value;
                    if (isInput)
                    {
                        //参数类似是数组类型，判定为in参数
                        if (value is System.Collections.ICollection)
                        {
                            var valList = value as System.Collections.ICollection;
                            var keys = new List<string>();
                            foreach(var item in valList)
                            {
                                var key = $"{properyty.Name}_{keys.Count}";
                                var val = item;
                                var para = command.Database.MakeInParam(key, val);
                                command.Parameters.Add(para);
                                keys.Add("@" + key);
                            }

                            var str = string.Join(",", keys);
                            command.Sql = command.Sql.Replace($"@{properyty.Name}", str);
                        }
                        else
                        {
                            var para = command.Database.MakeInParam($"{properyty.Name}", value);
                            command.Parameters.Add(para);
                        }
                    }
                    else
                    {
                        var para = command.Database.MakeOutParam($"{properyty.Name}", properyty.PropertyType, 500);
                        command.Parameters.Add(para);
                    }
                }
            }
            return command;
        }
        private static dynamic GetOutputParameters(List<DbParameter> parameters)
        {
            dynamic outParameters = new ExpandoObject();

            parameters.Where(p => p.Direction == ParameterDirection.Output).ToList().ForEach(item=>
            {
                if(item.Value == DBNull.Value)
                {
                    throw new Exception($"返回参数{item.ParameterName}的值不能为空");
                }
                (outParameters as IDictionary<string, object>).Add(item.ParameterName.Substring(1), item.Value);
            });
            return outParameters;
        }
    }

    public class DataCommand
    {
        public CommandType Type { get; protected set; }
        public DbHelper Database { get; private set; }
        /// <summary>
        /// SQL语句
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public List<DbParameter> Parameters { get; set; }

        public DataCommand(DbHelper db, string sql) : this(db, CommandType.Text, sql)
        {
        }
        public DataCommand(DbHelper db, CommandType commandType, string sql)
        {
            this.Type = commandType;
            this.Database = db;
            this.Sql = sql;
            this.Parameters = new List<DbParameter>();
        }
    }

    public class SqlCommand: DataCommand
    {
        /// <summary>
        /// 排序参数
        /// </summary>
        public string OrderBy { get; set; }
        private bool IsWhere = true;
        public SqlCommand(DbHelper db, string sql) :base(db, CommandType.Text,sql)
        {

        }
        /// <summary>
        /// 拼接SQL条件
        /// 不要在条件前面加where或者and
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public SqlCommand AddWhere(string condition)
        {
            if(IsWhere)
            {
                this.Sql = $"{this.Sql} where {condition} ";
                IsWhere = false;
            }
            else
            {
                this.Sql = $"{this.Sql} and {condition} ";
            }
            return this;
        }
    }

    public class SpCommand : DataCommand
    {
        public SpCommand(DbHelper db, string sql) : base(db, CommandType.StoredProcedure, sql)
        {

        }
    }

    public class SqlBuilder
    {
        private StringBuilder sql = new StringBuilder();
        public bool IsWhere = false;
        public IDictionary<string, object> Parameters { get; private set; } = new Dictionary<string, object>();

        public SqlBuilder(string sqlText)
        {
            sql.Append(sqlText);
        }

        public SqlBuilder AddSql(string str)
        {
            sql.Append($" {str}");
            return this;
        }

        public SqlBuilder AddSql(string str, string paraName, object paraVal)
        {
            Parameters.Add(paraName, paraVal);
            return AddSql(str);
        }
        /// <summary>
        /// 添加where条件，解决拼where还是and得问题
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public SqlBuilder AddWhere(string str)
        {
            if(IsWhere)
            {
                sql.Append($" and {str}");
            }
            else
            {
                sql.Append($" where {str}");
            }
            IsWhere = true;
            return this;
        }

        public SqlBuilder AddWhere(string str,string paraName,object paraVal)
        {
            Parameters.Add(paraName, paraVal);
            return AddWhere(str);
        }

        public override string ToString()
        {
            return sql.ToString();
        }
    }
}
