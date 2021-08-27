
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace ERX.Services.Helpers.Db
{
  public class SqlServerProvider : IDbProvider
  {
    public object ConvertToLocalDbType(Type t)
    {
      switch (t.ToString())
      {
        case "System.Boolean":
          return (object) SqlDbType.Bit;
        case "System.Byte":
          return (object) SqlDbType.TinyInt;
        case "System.Byte[]":
          return (object) SqlDbType.Image;
        case "System.DateTime":
          return (object) SqlDbType.DateTime;
        case "System.Decimal":
          return (object) SqlDbType.Decimal;
        case "System.Double":
          return (object) SqlDbType.Float;
        case "System.Guid":
          return (object) SqlDbType.UniqueIdentifier;
        case "System.Int16":
          return (object) SqlDbType.SmallInt;
        case "System.Int32":
          return (object) SqlDbType.Int;
        case "System.Int64":
          return (object) SqlDbType.BigInt;
        case "System.Object":
          return (object) SqlDbType.Variant;
        case "System.Single":
          return (object) SqlDbType.Float;
        case "System.String":
          return (object) SqlDbType.NVarChar;
        case "System.TimeSpan":
          return (object) SqlDbType.Time;
        default:
          return (object) SqlDbType.Int;
      }
    }

    public string ConvertToLocalDbTypeString(Type netType)
    {
      switch (netType.ToString())
      {
        case "System.Boolean":
          return "bit";
        case "System.Byte":
          return "tinyint";
        case "System.Byte[]":
          return "image";
        case "System.DateTime":
          return "datetime";
        case "System.Decimal":
          return "decimal";
        case "System.Double":
          return "float";
        case "System.Guid":
          return "uniqueidentifier";
        case "System.Int16":
          return "smallint";
        case "System.Int32":
          return "int";
        case "System.Int64":
          return "bigint";
        case "System.Object":
          return "sql_variant";
        case "System.Single":
          return "float";
        case "System.String":
          return "nvarchar";
        case "System.TimeSpan":
          return "time";
        default:
          return (string) null;
      }
    }

    public void DeriveParameters(IDbCommand cmd)
    {
      if (!(cmd is SqlCommand))
        return;
      SqlCommandBuilder.DeriveParameters(cmd as SqlCommand);
    }

    public string GetLastIdSql()
    {
      return "SELECT SCOPE_IDENTITY()";
    }

    public DbProviderFactory Instance()
    {
      return (DbProviderFactory) SqlClientFactory.Instance;
    }

    public bool IsBackupDatabase()
    {
      return true;
    }

    public bool IsCompactDatabase()
    {
      return true;
    }

    public bool IsDbOptimize()
    {
      return true;
    }

    public bool IsFullTextSearchEnabled()
    {
      return true;
    }

    public bool IsShrinkData()
    {
      return true;
    }

    public bool IsStoreProc()
    {
      return true;
    }

    public DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction)
    {
      Type paraType = (Type) null;
      if (paraValue != null)
        paraType = paraValue.GetType();
      return this.MakeParam(paraName, paraValue, direction, paraType, (string) null);
    }

    public DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction,
      Type paraType,
      string sourceColumn)
    {
      return this.MakeParam(paraName, paraValue, direction, paraType, sourceColumn, 0);
    }

    public DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction,
      Type paraType,
      string sourceColumn,
      int size)
    {
      SqlParameter sqlParameter1 = new SqlParameter();
      sqlParameter1.ParameterName = this.ParameterPrefix + paraName;
      SqlParameter sqlParameter2 = sqlParameter1;
      if (paraType != null)
        sqlParameter2.SqlDbType = (SqlDbType) this.ConvertToLocalDbType(paraType);
      sqlParameter2.Value = paraValue;
      if (sqlParameter2.Value == null)
        sqlParameter2.Value = (object) DBNull.Value;
      sqlParameter2.Direction = direction;
      if (direction != ParameterDirection.Output || paraValue != null)
        sqlParameter2.Value = paraValue;
      if (direction == ParameterDirection.Output)
        sqlParameter2.Size = size;
      if (sourceColumn != null)
        sqlParameter2.SourceColumn = sourceColumn;
      return (DbParameter) sqlParameter2;
    }

    public string ParameterPrefix
    {
      get
      {
        return "@";
      }
    }
  }
}
