
using ERX.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace ERX.Services.Helpers.Db
{
  public class DbHelper
  {
    private static string m_querydetail = "";
    private object lockHelper = new object();
    protected string m_connectionstring = (string) null;
    private DbProviderFactory m_factory = (DbProviderFactory) null;
    private Hashtable m_paramcache = Hashtable.Synchronized(new Hashtable());
    private IDbProvider m_provider = (IDbProvider) null;
    private int m_querycount = 0;

    protected internal string ConnectionString
    {
      get
      {
        return this.m_connectionstring;
      }
      set
      {
        this.m_connectionstring = value;
      }
    }

    public DbProviderFactory Factory
    {
      get
      {
        if (this.m_factory == null)
          this.m_factory = this.Provider.Instance();
        return this.m_factory;
      }
    }

    public IDbProvider Provider
    {
      get
      {
        if (this.m_provider == null)
        {
          lock (this.lockHelper)
          {
            if (this.m_provider == null)
            {
              try
              {
                this.m_provider = (IDbProvider) Activator.CreateInstance(Type.GetType("ERX.Services.Helpers.Db.SqlServerProvider, ERX.Services", false, true));
              }
              catch
              {
                new Terminator().Throw("SqlServerProvider 数据库访问器创建失败！");
              }
            }
          }
        }
        return this.m_provider;
      }
    }

    public int QueryCount
    {
      get
      {
        return this.m_querycount;
      }
      set
      {
        this.m_querycount = value;
      }
    }

    public static string QueryDetail
    {
      get
      {
        return DbHelper.m_querydetail;
      }
      set
      {
        DbHelper.m_querydetail = value;
      }
    }

    public DbHelper(string connString)
    {
      this.BuildConnection(connString);
    }

    public void BuildConnection(string connectionString)
    {
      if (string.IsNullOrEmpty(connectionString))
        new Terminator().Throw("请检查数据库连接信息，当前数据库连接信息为空。");
      this.m_connectionstring = connectionString;
      this.m_querycount = 0;
    }

    private void AssignParameterValues(DbParameter[] commandParameters, DataRow dataRow)
    {
      if (commandParameters == null || dataRow == null)
        return;
      int num = 0;
      foreach (DbParameter commandParameter in commandParameters)
      {
        if (commandParameter.ParameterName == null || commandParameter.ParameterName.Length <= 1)
          new Terminator().Throw(string.Format("请提供参数{0}一个有效的名称{1}.", (object) num, (object) commandParameter.ParameterName));
        if (dataRow.Table.Columns.IndexOf(commandParameter.ParameterName.Substring(1)) != -1)
          commandParameter.Value = dataRow[commandParameter.ParameterName.Substring(1)];
        ++num;
      }
    }

    private void AssignParameterValues(DbParameter[] commandParameters, object[] parameterValues)
    {
      if (commandParameters == null || parameterValues == null)
        return;
      if (commandParameters.Length != parameterValues.Length)
        new Terminator().Throw("参数值个数与参数不匹配。");
      int index = 0;
      for (int length = commandParameters.Length; index < length; ++index)
      {
        if (parameterValues[index] is IDbDataParameter)
        {
          IDbDataParameter parameterValue = (IDbDataParameter) parameterValues[index];
          commandParameters[index].Value = parameterValue.Value != null ? parameterValue.Value : (object) DBNull.Value;
        }
        else
          commandParameters[index].Value = parameterValues[index] != null ? parameterValues[index] : (object) DBNull.Value;
      }
    }

    private void AttachParameters(DbCommand command, DbParameter[] commandParameters)
    {
      if (command == null)
        throw new ArgumentNullException(nameof (command));
      if (commandParameters == null)
        return;
      foreach (DbParameter commandParameter in commandParameters)
      {
        if (commandParameter != null)
        {
          if ((commandParameter.Direction == ParameterDirection.InputOutput || commandParameter.Direction == ParameterDirection.Input) && commandParameter.Value == null)
            commandParameter.Value = (object) DBNull.Value;
          command.Parameters.Add((object) commandParameter);
        }
      }
    }

    public void CacheParameterSet(string commandText, params DbParameter[] commandParameters)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (commandText == null || commandText.Length == 0)
        throw new ArgumentNullException(nameof (commandText));
      this.m_paramcache[(object) (this.ConnectionString + ":" + commandText)] = (object) commandParameters;
    }

    private DbParameter[] CloneParameters(DbParameter[] originalParameters)
    {
      DbParameter[] dbParameterArray = new DbParameter[originalParameters.Length];
      int index = 0;
      for (int length = originalParameters.Length; index < length; ++index)
        dbParameterArray[index] = (DbParameter) ((ICloneable) originalParameters[index]).Clone();
      return dbParameterArray;
    }

    public DbCommand CreateCommand(
      DbConnection connection,
      string spName,
      params string[] sourceColumns)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      DbCommand command = this.Factory.CreateCommand();
      command.CommandText = spName;
      command.Connection = connection;
      command.CommandType = CommandType.StoredProcedure;
      if (sourceColumns != null && (uint) sourceColumns.Length > 0U)
      {
        DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
        for (int index = 0; index < sourceColumns.Length; ++index)
          spParameterSet[index].SourceColumn = sourceColumns[index];
        this.AttachParameters(command, spParameterSet);
      }
      return command;
    }

    private DbParameter[] DiscoverSpParameterSet(
      DbConnection connection,
      string spName,
      bool includeReturnValueParameter)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      DbCommand command = connection.CreateCommand();
      command.CommandText = spName;
      command.CommandType = CommandType.StoredProcedure;
      connection.Open();
      this.Provider.DeriveParameters((IDbCommand) command);
      connection.Close();
      if (!includeReturnValueParameter)
        command.Parameters.RemoveAt(0);
      DbParameter[] dbParameterArray = new DbParameter[command.Parameters.Count];
      command.Parameters.CopyTo((Array) dbParameterArray, 0);
      foreach (DbParameter dbParameter in dbParameterArray)
        dbParameter.Value = (object) DBNull.Value;
      return dbParameterArray;
    }

    public void ExecuteCommandWithSplitter(string commandText)
    {
      this.ExecuteCommandWithSplitter(commandText, "\r\nGO\r\n");
    }

    public void ExecuteCommandWithSplitter(string commandText, string splitter)
    {
      int startIndex = 0;
      do
      {
        int num = commandText.IndexOf(splitter, startIndex);
        int length = (num > startIndex ? num : commandText.Length) - startIndex;
        string commandText1 = commandText.Substring(startIndex, length);
        if (commandText1.Trim().Length > 0)
          this.ExecuteNonQuery(CommandType.Text, commandText1);
        if (num != -1)
          startIndex = num + splitter.Length;
        else
          goto label_5;
      }
      while (startIndex < commandText.Length);
      goto label_6;
label_5:
      return;
label_6:;
    }

    public DataSet ExecuteDataset(string commandText)
    {
      return this.ExecuteDataset(CommandType.Text, commandText, (DbParameter[]) null);
    }

    public DataSet ExecuteDataset(CommandType commandType, string commandText)
    {
      return this.ExecuteDataset(commandType, commandText, (DbParameter[]) null);
    }

    public DataSet ExecuteDataset(
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        return this.ExecuteDataset(connection, commandType, commandText, commandParameters);
      }
    }

    public DataSet ExecuteDataset(
      DbConnection connection,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteDataset(connection, commandType, commandText, (DbParameter[]) null);
    }

    public DataSet ExecuteDataset(
      DbConnection connection,
      string spName,
      params object[] parameterValues)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteDataset(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteDataset(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public DataSet ExecuteDataset(
      DbConnection connection,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, connection, (DbTransaction) null, commandType, commandText, commandParameters, out mustCloseConnection);
      using (DbDataAdapter dataAdapter = this.Factory.CreateDataAdapter())
      {
        dataAdapter.SelectCommand = command;
        DataSet dataSet = new DataSet();
        DateTime now1 = DateTime.Now;
        dataAdapter.Fill(dataSet);
        DateTime now2 = DateTime.Now;
        DbHelper.m_querydetail += DbHelper.GetQueryDetail(command.CommandText, now1, now2, commandParameters);
        ++this.m_querycount;
        command.Parameters.Clear();
        if (mustCloseConnection)
          connection.Close();
        return dataSet;
      }
    }

    public DataSet ExecuteDataset(
      DbTransaction transaction,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteDataset(transaction, commandType, commandText, (DbParameter[]) null);
    }

    public DataSet ExecuteDataset(
      DbTransaction transaction,
      string spName,
      params object[] parameterValues)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteDataset(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteDataset(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public DataSet ExecuteDataset(
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
      using (DbDataAdapter dataAdapter = this.Factory.CreateDataAdapter())
      {
        dataAdapter.SelectCommand = command;
        DataSet dataSet = new DataSet();
        dataAdapter.Fill(dataSet);
        command.Parameters.Clear();
        return dataSet;
      }
    }

    public DataSet ExecuteDatasetTypedParams(string spName, DataRow dataRow)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteDataset(CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteDataset(CommandType.StoredProcedure, spName, spParameterSet);
    }

    public DataSet ExecuteDatasetTypedParams(
      DbConnection connection,
      string spName,
      DataRow dataRow)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteDataset(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteDataset(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public DataSet ExecuteDatasetTypedParams(
      DbTransaction transaction,
      string spName,
      DataRow dataRow)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteDataset(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteDataset(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public int ExecuteNonQuery(string commandText)
    {
      return this.ExecuteNonQuery(CommandType.Text, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(CommandType commandType, string commandText)
    {
      return this.ExecuteNonQuery(commandType, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        return this.ExecuteNonQuery(connection, commandType, commandText, commandParameters);
      }
    }

    public int ExecuteNonQuery(
      DbConnection connection,
      string spName,
      params object[] parameterValues)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public int ExecuteNonQuery(
      DbConnection connection,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteNonQuery(connection, commandType, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(
      DbConnection connection,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, connection, (DbTransaction) null, commandType, commandText, commandParameters, out mustCloseConnection);
      DateTime now1 = DateTime.Now;
      int num = command.ExecuteNonQuery();
      DateTime now2 = DateTime.Now;
      DbHelper.m_querydetail += DbHelper.GetQueryDetail(command.CommandText, now1, now2, commandParameters);
      ++this.m_querycount;
      command.Parameters.Clear();
      if (mustCloseConnection)
        connection.Close();
      return num;
    }

    public int ExecuteNonQuery(
      DbTransaction transaction,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteNonQuery(transaction, commandType, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(
      DbTransaction transaction,
      string spName,
      params object[] parameterValues)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public int ExecuteNonQuery(
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
      int num = command.ExecuteNonQuery();
      command.Parameters.Clear();
      return num;
    }

    public int ExecuteNonQuery(out int id, CommandType commandType, string commandText)
    {
      return this.ExecuteNonQuery(out id, commandType, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(
      out int id,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        return this.ExecuteNonQuery(out id, connection, commandType, commandText, commandParameters);
      }
    }

    public int ExecuteNonQuery(out int id, string commandText)
    {
      return this.ExecuteNonQuery(out id, CommandType.Text, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(
      out int id,
      DbConnection connection,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteNonQuery(out id, connection, commandType, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(
      out int id,
      DbTransaction transaction,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteNonQuery(out id, transaction, commandType, commandText, (DbParameter[]) null);
    }

    public int ExecuteNonQuery(
      out int id,
      DbConnection connection,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (this.Provider.GetLastIdSql().Trim() == "")
        throw new ArgumentNullException("GetLastIdSql is \"\"");
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, connection, (DbTransaction) null, commandType, commandText, commandParameters, out mustCloseConnection);
      int num = command.ExecuteNonQuery();
      command.Parameters.Clear();
      command.CommandType = CommandType.Text;
      command.CommandText = this.Provider.GetLastIdSql();
      id = int.Parse(command.ExecuteScalar().ToString());
      DateTime now1 = DateTime.Now;
      id = int.Parse(command.ExecuteScalar().ToString());
      DateTime now2 = DateTime.Now;
      DbHelper.m_querydetail += DbHelper.GetQueryDetail(command.CommandText, now1, now2, commandParameters);
      ++this.m_querycount;
      if (mustCloseConnection)
        connection.Close();
      return num;
    }

    public int ExecuteNonQuery(
      out int id,
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
      int num = command.ExecuteNonQuery();
      command.Parameters.Clear();
      command.CommandType = CommandType.Text;
      command.CommandText = this.Provider.GetLastIdSql();
      id = int.Parse(command.ExecuteScalar().ToString());
      return num;
    }

    public int ExecuteNonQueryTypedParams(string spName, DataRow dataRow)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteNonQuery(CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteNonQuery(CommandType.StoredProcedure, spName, spParameterSet);
    }

    public int ExecuteNonQueryTypedParams(DbConnection connection, string spName, DataRow dataRow)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public int ExecuteNonQueryTypedParams(
      DbTransaction transaction,
      string spName,
      DataRow dataRow)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public T ExecuteObject<T>(string commandText)
    {
      DataSet ds = this.ExecuteDataset(commandText);
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
      return default (T);
    }

    public T ExecuteObject<T>(string commandText, List<DbParameter> prams)
    {
      DataSet ds = this.ExecuteDataset(CommandType.Text, commandText, prams.ToArray());
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
      return default (T);
    }

    public IList<T> ExecuteObjectList<T>(string commandText)
    {
      DataSet ds = this.ExecuteDataset(commandText);
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
      return (IList<T>) null;
    }

    public IList<T> ExecuteObjectList<T>(string commandText, List<DbParameter> prams)
    {
      DataSet ds = this.ExecuteDataset(CommandType.Text, commandText, prams.ToArray());
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
      return (IList<T>) null;
    }

    public DbDataReader ExecuteReader(CommandType commandType, string commandText)
    {
      return this.ExecuteReader(commandType, commandText, (DbParameter[]) null);
    }

    public DbDataReader ExecuteReader(string spName, params object[] parameterValues)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues != null && (uint) parameterValues.Length > 0U)
      {
        DbParameter[] spParameterSet = this.GetSpParameterSet(spName);
        this.AssignParameterValues(spParameterSet, parameterValues);
        return this.ExecuteReader(this.ConnectionString, (object) CommandType.StoredProcedure, (object) spName, (object) spParameterSet);
      }
      return this.ExecuteReader(this.ConnectionString, (object) CommandType.StoredProcedure, (object) spName);
    }

    public DbDataReader ExecuteReader(
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      DbConnection connection = (DbConnection) null;
      DbDataReader dbDataReader;
      try
      {
        connection = this.Factory.CreateConnection();
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        dbDataReader = this.ExecuteReader(connection, (DbTransaction) null, commandType, commandText, commandParameters, DbHelper.DbConnectionOwnership.Internal);
      }
      catch
      {
        if (connection != null)
          connection.Close();
        throw;
      }
      return dbDataReader;
    }

    public DbDataReader ExecuteReader(
      DbConnection connection,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteReader(connection, commandType, commandText, (DbParameter[]) null);
    }

    public DbDataReader ExecuteReader(
      DbConnection connection,
      string spName,
      params object[] parameterValues)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteReader(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteReader(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public DbDataReader ExecuteReader(
      DbTransaction transaction,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteReader(transaction, commandType, commandText, (DbParameter[]) null);
    }

    public DbDataReader ExecuteReader(
      DbTransaction transaction,
      string spName,
      params object[] parameterValues)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteReader(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteReader(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public DbDataReader ExecuteReader(
      DbConnection connection,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      return this.ExecuteReader(connection, (DbTransaction) null, commandType, commandText, commandParameters, DbHelper.DbConnectionOwnership.External);
    }

    public DbDataReader ExecuteReader(
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      return this.ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, DbHelper.DbConnectionOwnership.External);
    }

    private DbDataReader ExecuteReader(
      DbConnection connection,
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      DbParameter[] commandParameters,
      DbHelper.DbConnectionOwnership connectionOwnership)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      bool mustCloseConnection = false;
      DbCommand command = this.Factory.CreateCommand();
      DbDataReader dbDataReader1;
      try
      {
        this.PrepareCommand(command, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
        DateTime now1 = DateTime.Now;
        DbDataReader dbDataReader2 = connectionOwnership != DbHelper.DbConnectionOwnership.External ? command.ExecuteReader(CommandBehavior.CloseConnection) : command.ExecuteReader();
        DateTime now2 = DateTime.Now;
        DbHelper.m_querydetail += DbHelper.GetQueryDetail(command.CommandText, now1, now2, commandParameters);
        ++this.m_querycount;
        bool flag = true;
        foreach (DbParameter parameter in command.Parameters)
        {
          if (parameter.Direction != ParameterDirection.Input)
            flag = false;
        }
        if (flag)
          command.Parameters.Clear();
        dbDataReader1 = dbDataReader2;
      }
      catch
      {
        if (mustCloseConnection)
          connection.Close();
        throw;
      }
      return dbDataReader1;
    }

    public DbDataReader ExecuteReaderTypedParams(string spName, DataRow dataRow)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow != null && (uint) dataRow.ItemArray.Length > 0U)
      {
        DbParameter[] spParameterSet = this.GetSpParameterSet(spName);
        this.AssignParameterValues(spParameterSet, dataRow);
        return this.ExecuteReader(this.ConnectionString, (object) CommandType.StoredProcedure, (object) spName, (object) spParameterSet);
      }
      return this.ExecuteReader(this.ConnectionString, (object) CommandType.StoredProcedure, (object) spName);
    }

    public DbDataReader ExecuteReaderTypedParams(
      DbConnection connection,
      string spName,
      DataRow dataRow)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteReader(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteReader(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public DbDataReader ExecuteReaderTypedParams(
      DbTransaction transaction,
      string spName,
      DataRow dataRow)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteReader(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteReader(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public object ExecuteScalar(CommandType commandType, string commandText)
    {
      return this.ExecuteScalar(commandType, commandText, (DbParameter[]) null);
    }

    public object ExecuteScalar(
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        return this.ExecuteScalar(connection, commandType, commandText, commandParameters);
      }
    }

    public object ExecuteScalar(
      DbConnection connection,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteScalar(connection, commandType, commandText, (DbParameter[]) null);
    }

    public object ExecuteScalar(
      DbConnection connection,
      string spName,
      params object[] parameterValues)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteScalar(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteScalar(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public object ExecuteScalar(
      DbTransaction transaction,
      CommandType commandType,
      string commandText)
    {
      return this.ExecuteScalar(transaction, commandType, commandText, (DbParameter[]) null);
    }

    public object ExecuteScalar(
      DbTransaction transaction,
      string spName,
      params object[] parameterValues)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues == null || (uint) parameterValues.Length <= 0U)
        return this.ExecuteScalar(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, parameterValues);
      return this.ExecuteScalar(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public object ExecuteScalar(
      DbConnection connection,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, connection, (DbTransaction) null, commandType, commandText, commandParameters, out mustCloseConnection);
      object obj = command.ExecuteScalar();
      command.Parameters.Clear();
      if (mustCloseConnection)
        connection.Close();
      return obj;
    }

    public object ExecuteScalar(
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, transaction.Connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
      DateTime now1 = DateTime.Now;
      object obj = command.ExecuteScalar();
      DateTime now2 = DateTime.Now;
      DbHelper.m_querydetail += DbHelper.GetQueryDetail(command.CommandText, now1, now2, commandParameters);
      ++this.m_querycount;
      command.Parameters.Clear();
      return obj;
    }

    public string ExecuteScalarToStr(CommandType commandType, string commandText)
    {
      object obj = this.ExecuteScalar(commandType, commandText);
      if (obj == null)
        return "";
      return obj.ToString();
    }

    public string ExecuteScalarToStr(
      CommandType commandType,
      string commandText,
      params DbParameter[] commandParameters)
    {
      object obj = this.ExecuteScalar(commandType, commandText, commandParameters);
      if (obj == null)
        return "";
      return obj.ToString();
    }

    public object ExecuteScalarTypedParams(string spName, DataRow dataRow)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteScalar(CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteScalar(CommandType.StoredProcedure, spName, spParameterSet);
    }

    public object ExecuteScalarTypedParams(DbConnection connection, string spName, DataRow dataRow)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteScalar(connection, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteScalar(connection, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public object ExecuteScalarTypedParams(
      DbTransaction transaction,
      string spName,
      DataRow dataRow)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (dataRow == null || (uint) dataRow.ItemArray.Length <= 0U)
        return this.ExecuteScalar(transaction, CommandType.StoredProcedure, spName);
      DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
      this.AssignParameterValues(spParameterSet, dataRow);
      return this.ExecuteScalar(transaction, CommandType.StoredProcedure, spName, spParameterSet);
    }

    public void FillDataset(
      CommandType commandType,
      string commandText,
      DataSet dataSet,
      string[] tableNames)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (dataSet == null)
        throw new ArgumentNullException(nameof (dataSet));
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        this.FillDataset(connection, commandType, commandText, dataSet, tableNames);
      }
    }

    public void FillDataset(
      string spName,
      DataSet dataSet,
      string[] tableNames,
      params object[] parameterValues)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (dataSet == null)
        throw new ArgumentNullException(nameof (dataSet));
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        this.FillDataset(connection, spName, dataSet, tableNames, parameterValues);
      }
    }

    public void FillDataset(
      CommandType commandType,
      string commandText,
      DataSet dataSet,
      string[] tableNames,
      params DbParameter[] commandParameters)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (dataSet == null)
        throw new ArgumentNullException(nameof (dataSet));
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        connection.Open();
        this.FillDataset(connection, commandType, commandText, dataSet, tableNames, commandParameters);
      }
    }

    public void FillDataset(
      DbConnection connection,
      CommandType commandType,
      string commandText,
      DataSet dataSet,
      string[] tableNames)
    {
      this.FillDataset(connection, commandType, commandText, dataSet, tableNames, (DbParameter[]) null);
    }

    public void FillDataset(
      DbConnection connection,
      string spName,
      DataSet dataSet,
      string[] tableNames,
      params object[] parameterValues)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (dataSet == null)
        throw new ArgumentNullException(nameof (dataSet));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues != null && (uint) parameterValues.Length > 0U)
      {
        DbParameter[] spParameterSet = this.GetSpParameterSet(connection, spName);
        this.AssignParameterValues(spParameterSet, parameterValues);
        this.FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames, spParameterSet);
      }
      else
        this.FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames);
    }

    public void FillDataset(
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      DataSet dataSet,
      string[] tableNames)
    {
      this.FillDataset(transaction, commandType, commandText, dataSet, tableNames, (DbParameter[]) null);
    }

    public void FillDataset(
      DbTransaction transaction,
      string spName,
      DataSet dataSet,
      string[] tableNames,
      params object[] parameterValues)
    {
      if (transaction == null)
        throw new ArgumentNullException(nameof (transaction));
      if (transaction != null && transaction.Connection == null)
        throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
      if (dataSet == null)
        throw new ArgumentNullException(nameof (dataSet));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      if (parameterValues != null && (uint) parameterValues.Length > 0U)
      {
        DbParameter[] spParameterSet = this.GetSpParameterSet(transaction.Connection, spName);
        this.AssignParameterValues(spParameterSet, parameterValues);
        this.FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, spParameterSet);
      }
      else
        this.FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames);
    }

    public void FillDataset(
      DbConnection connection,
      CommandType commandType,
      string commandText,
      DataSet dataSet,
      string[] tableNames,
      params DbParameter[] commandParameters)
    {
      this.FillDataset(connection, (DbTransaction) null, commandType, commandText, dataSet, tableNames, commandParameters);
    }

    public void FillDataset(
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      DataSet dataSet,
      string[] tableNames,
      params DbParameter[] commandParameters)
    {
      this.FillDataset(transaction.Connection, transaction, commandType, commandText, dataSet, tableNames, commandParameters);
    }

    private void FillDataset(
      DbConnection connection,
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      DataSet dataSet,
      string[] tableNames,
      params DbParameter[] commandParameters)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (dataSet == null)
        throw new ArgumentNullException(nameof (dataSet));
      DbCommand command = this.Factory.CreateCommand();
      bool mustCloseConnection = false;
      this.PrepareCommand(command, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);
      using (DbDataAdapter dataAdapter = this.Factory.CreateDataAdapter())
      {
        dataAdapter.SelectCommand = command;
        if (tableNames != null && (uint) tableNames.Length > 0U)
        {
          string sourceTable = "Table";
          for (int index = 0; index < tableNames.Length; ++index)
          {
            if (tableNames[index] == null || tableNames[index].Length == 0)
              throw new ArgumentException("The tableNames parameter must contain a list of tables, a value was provided as null or empty string.", nameof (tableNames));
            dataAdapter.TableMappings.Add(sourceTable, tableNames[index]);
            sourceTable += (index + 1).ToString();
          }
        }
        dataAdapter.Fill(dataSet);
        command.Parameters.Clear();
      }
      if (!mustCloseConnection)
        return;
      connection.Close();
    }

    public DbParameter[] GetCachedParameterSet(string commandText)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (commandText == null || commandText.Length == 0)
        throw new ArgumentNullException(nameof (commandText));
      DbParameter[] originalParameters = this.m_paramcache[(object) (this.ConnectionString + ":" + commandText)] as DbParameter[];
      if (originalParameters == null)
        return (DbParameter[]) null;
      return this.CloneParameters(originalParameters);
    }

    public DataTable GetEmptyTable(string tableName)
    {
      return this.ExecuteDataset(string.Format("SELECT * FROM {0} WHERE 1=0", (object) tableName)).Tables[0];
    }

    private static string GetQueryDetail(
      string commandText,
      DateTime dtStart,
      DateTime dtEnd,
      DbParameter[] cmdParams)
    {
      string str1 = "<tr style=\"background: rgb(255, 255, 255) none repeat scroll 0%; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial;\">";
      string str2 = "";
      string str3 = "";
      string str4 = "";
      string str5 = "";
      if (cmdParams != null && (uint) cmdParams.Length > 0U)
      {
        foreach (DbParameter cmdParam in cmdParams)
        {
          if (cmdParam != null)
          {
            str2 = str2 + "<td>" + cmdParam.ParameterName + "</td>";
            str3 = str3 + "<td>" + cmdParam.DbType.ToString() + "</td>";
            str4 = str4 + "<td>" + cmdParam.Value.ToString() + "</td>";
          }
        }
        str5 = string.Format("<table width=\"100%\" cellspacing=\"1\" cellpadding=\"0\" style=\"background: rgb(255, 255, 255) none repeat scroll 0%; margin-top: 5px; font-size: 12px; display: block; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial;\">{0}{1}</tr>{0}{2}</tr>{0}{3}</tr></table>", (object) str1, (object) str2, (object) str3, (object) str4);
      }
      return string.Format("<center><div style=\"border: 1px solid black; margin: 2px; padding: 1em; text-align: left; width: 96%; clear: both;\"><div style=\"font-size: 12px; float: right; width: 100px; margin-bottom: 5px;\"><b>TIME:</b> {0}</div><span style=\"font-size: 12px;\">{1}{2}</span></div><br /></center>", (object) (dtEnd.Subtract(dtStart).TotalMilliseconds / 1000.0), (object) commandText, (object) str5);
    }

    public DbParameter[] GetSpParameterSet(string spName)
    {
      return this.GetSpParameterSet(spName, false);
    }

    public DbParameter[] GetSpParameterSet(
      string spName,
      bool includeReturnValueParameter)
    {
      if (this.ConnectionString == null || this.ConnectionString.Length == 0)
        throw new ArgumentNullException("ConnectionString");
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      using (DbConnection connection = this.Factory.CreateConnection())
      {
        connection.ConnectionString = this.ConnectionString;
        return this.GetSpParameterSetInternal(connection, spName, includeReturnValueParameter);
      }
    }

    internal DbParameter[] GetSpParameterSet(DbConnection connection, string spName)
    {
      return this.GetSpParameterSet(connection, spName, false);
    }

    internal DbParameter[] GetSpParameterSet(
      DbConnection connection,
      string spName,
      bool includeReturnValueParameter)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      using (DbConnection connection1 = (DbConnection) ((ICloneable) connection).Clone())
        return this.GetSpParameterSetInternal(connection1, spName, includeReturnValueParameter);
    }

    private DbParameter[] GetSpParameterSetInternal(
      DbConnection connection,
      string spName,
      bool includeReturnValueParameter)
    {
      if (connection == null)
        throw new ArgumentNullException(nameof (connection));
      if (spName == null || spName.Length == 0)
        throw new ArgumentNullException(nameof (spName));
      string str = connection.ConnectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : "");
      DbParameter[] originalParameters = this.m_paramcache[(object) str] as DbParameter[];
      if (originalParameters == null)
      {
        DbParameter[] dbParameterArray = this.DiscoverSpParameterSet(connection, spName, includeReturnValueParameter);
        this.m_paramcache[(object) str] = (object) dbParameterArray;
        originalParameters = dbParameterArray;
      }
      return this.CloneParameters(originalParameters);
    }

    public DbParameter MakeInParam(string paraName, object paraValue)
    {
      return this.MakeParam(paraName, paraValue, ParameterDirection.Input);
    }

    public DbParameter MakeOutParam(string paraName, Type paraType)
    {
      return this.MakeParam(paraName, (object) null, ParameterDirection.Output, paraType, "");
    }

    public DbParameter MakeOutParam(string paraName, Type paraType, int size)
    {
      return this.MakeParam(paraName, (object) null, ParameterDirection.Output, paraType, "", size);
    }

    public DbParameter MakeOutParam(
      string paraName,
      object paraValue,
      Type paraType,
      int size)
    {
      return this.MakeParam(paraName, paraValue, ParameterDirection.Output, paraType, "", size);
    }

    public DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction)
    {
      return this.Provider.MakeParam(paraName, paraValue, direction);
    }

    public DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction,
      Type paraType,
      string sourceColumn)
    {
      return this.Provider.MakeParam(paraName, paraValue, direction, paraType, sourceColumn);
    }

    public DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction,
      Type paraType,
      string sourceColumn,
      int size)
    {
      return this.Provider.MakeParam(paraName, paraValue, direction, paraType, sourceColumn, size);
    }

    public DbParameter MakeReturnParam()
    {
      return this.MakeReturnParam("ReturnValue");
    }

    public DbParameter MakeReturnParam(string paraName)
    {
      return this.MakeParam(paraName, (object) 0, ParameterDirection.ReturnValue);
    }

    private void PrepareCommand(
      DbCommand command,
      DbConnection connection,
      DbTransaction transaction,
      CommandType commandType,
      string commandText,
      DbParameter[] commandParameters,
      out bool mustCloseConnection)
    {
      if (command == null)
        throw new ArgumentNullException(nameof (command));
      if (commandText == null || commandText.Length == 0)
        throw new ArgumentNullException(nameof (commandText));
      if (connection.State != ConnectionState.Open)
      {
        mustCloseConnection = true;
        connection.Open();
      }
      else
        mustCloseConnection = false;
      command.Connection = connection;
      command.CommandText = commandText;
      if (transaction != null)
      {
        if (transaction.Connection == null)
          throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.", nameof (transaction));
        command.Transaction = transaction;
      }
      command.CommandType = commandType;
      if (commandParameters == null)
        return;
      this.AttachParameters(command, commandParameters);
    }

    public void ResetDbProvider()
    {
      this.m_connectionstring = (string) null;
      this.m_factory = (DbProviderFactory) null;
      this.m_provider = (IDbProvider) null;
    }

    public int RunProc(string procName)
    {
      return this.ExecuteNonQuery(CommandType.StoredProcedure, procName, (DbParameter[]) null);
    }

    public void RunProc(string procName, out DbDataReader reader)
    {
      reader = this.ExecuteReader(CommandType.StoredProcedure, procName, (DbParameter[]) null);
    }

    public void RunProc(string procName, out DataSet ds)
    {
      ds = this.ExecuteDataset(CommandType.StoredProcedure, procName, (DbParameter[]) null);
    }

    public void RunProc(string procName, out object obj)
    {
      obj = this.ExecuteScalar(CommandType.StoredProcedure, procName, (DbParameter[]) null);
    }

    public int RunProc(string procName, List<DbParameter> prams)
    {
      prams.Add(this.MakeReturnParam());
      return this.ExecuteNonQuery(CommandType.StoredProcedure, procName, prams.ToArray());
    }

    public void RunProc(string procName, List<DbParameter> prams, out DbDataReader reader)
    {
      prams.Add(this.MakeReturnParam());
      reader = this.ExecuteReader(CommandType.StoredProcedure, procName, prams.ToArray());
    }

    public void RunProc(string procName, List<DbParameter> prams, out DataSet ds)
    {
      prams.Add(this.MakeReturnParam());
      ds = this.ExecuteDataset(CommandType.StoredProcedure, procName, prams.ToArray());
    }

    public void RunProc(string procName, List<DbParameter> prams, out object obj)
    {
      prams.Add(this.MakeReturnParam());
      obj = this.ExecuteScalar(CommandType.StoredProcedure, procName, prams.ToArray());
    }

    public T RunProcObject<T>(string procName)
    {
      DataSet ds = (DataSet) null;
      this.RunProc(procName, out ds);
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
      return default (T);
    }

    public T RunProcObject<T>(string procName, List<DbParameter> prams)
    {
      DataSet ds = (DataSet) null;
      this.RunProc(procName, prams, out ds);
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]);
      return default (T);
    }

    public IList<T> RunProcObjectList<T>(string procName)
    {
      DataSet ds = (DataSet) null;
      this.RunProc(procName, out ds);
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
      return (IList<T>) null;
    }

    public IList<T> RunProcObjectList<T>(string procName, List<DbParameter> prams)
    {
      DataSet ds = (DataSet) null;
      this.RunProc(procName, prams, out ds);
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
      return (IList<T>) null;
    }

    public void UpdateByDataSet(DataSet dataSet, string tableName)
    {
      DbDataAdapter dataAdapter = this.Factory.CreateDataAdapter();
      dataAdapter.SelectCommand.CommandText = string.Format("Select * from {0} ORDER BY DayID DESC", (object) tableName);
      this.Factory.CreateCommandBuilder().DataAdapter.SelectCommand.Connection = this.Factory.CreateConnection();
      DataSet dataSet1 = new DataSet();
      dataAdapter.Fill(dataSet1);
      dataSet1.Tables[0].Rows[0][1] = (object) "107";
      dataAdapter.Update(dataSet1);
    }

    public void UpdateDataSet(DataSet dataSet, string tableName)
    {
      string str = string.Format("Select * from {0} where 1=0", (object) tableName);
      DbCommandBuilder commandBuilder = this.Factory.CreateCommandBuilder();
      commandBuilder.DataAdapter = this.Factory.CreateDataAdapter();
      commandBuilder.DataAdapter.SelectCommand = this.Factory.CreateCommand();
      commandBuilder.DataAdapter.DeleteCommand = this.Factory.CreateCommand();
      commandBuilder.DataAdapter.InsertCommand = this.Factory.CreateCommand();
      commandBuilder.DataAdapter.UpdateCommand = this.Factory.CreateCommand();
      commandBuilder.DataAdapter.SelectCommand.CommandText = str;
      commandBuilder.DataAdapter.SelectCommand.Connection = this.Factory.CreateConnection();
      commandBuilder.DataAdapter.DeleteCommand.Connection = this.Factory.CreateConnection();
      commandBuilder.DataAdapter.InsertCommand.Connection = this.Factory.CreateConnection();
      commandBuilder.DataAdapter.UpdateCommand.Connection = this.Factory.CreateConnection();
      commandBuilder.DataAdapter.SelectCommand.Connection.ConnectionString = this.ConnectionString;
      commandBuilder.DataAdapter.DeleteCommand.Connection.ConnectionString = this.ConnectionString;
      commandBuilder.DataAdapter.InsertCommand.Connection.ConnectionString = this.ConnectionString;
      commandBuilder.DataAdapter.UpdateCommand.Connection.ConnectionString = this.ConnectionString;
      this.UpdateDataSet(commandBuilder.GetInsertCommand(), commandBuilder.GetDeleteCommand(), commandBuilder.GetUpdateCommand(), dataSet, tableName);
    }

    public void UpdateDataSet(
      DbCommand insertCommand,
      DbCommand deleteCommand,
      DbCommand updateCommand,
      DataSet dataSet,
      string tableName)
    {
      if (insertCommand == null)
        throw new ArgumentNullException(nameof (insertCommand));
      if (deleteCommand == null)
        throw new ArgumentNullException(nameof (deleteCommand));
      if (updateCommand == null)
        throw new ArgumentNullException(nameof (updateCommand));
      if (tableName == null || tableName.Length == 0)
        throw new ArgumentNullException(nameof (tableName));
      using (DbDataAdapter dataAdapter = this.Factory.CreateDataAdapter())
      {
        dataAdapter.UpdateCommand = updateCommand;
        dataAdapter.InsertCommand = insertCommand;
        dataAdapter.DeleteCommand = deleteCommand;
        dataAdapter.Update(dataSet, tableName);
        dataSet.AcceptChanges();
      }
    }

    private enum DbConnectionOwnership
    {
      Internal,
      External,
    }
    /// <summary>
    /// 开启一个事务
    /// </summary>
    /// <returns></returns>
    public DbTransaction BeginTransaction()
    {
        var conn = new SqlConnection(this.ConnectionString);
        conn.Open();
        return conn.BeginTransaction();
    }
    }
}
