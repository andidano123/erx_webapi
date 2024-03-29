﻿
using System;
using System.Data;
using System.Data.Common;

namespace ERX.Services.Helpers.Db
{
  public interface IDbProvider
  {
    object ConvertToLocalDbType(Type t);

    string ConvertToLocalDbTypeString(Type netType);

    void DeriveParameters(IDbCommand cmd);

    string GetLastIdSql();

    DbProviderFactory Instance();

    bool IsBackupDatabase();

    bool IsCompactDatabase();

    bool IsDbOptimize();

    bool IsFullTextSearchEnabled();

    bool IsShrinkData();

    bool IsStoreProc();

    DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction);

    DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction,
      Type paraType,
      string sourceColumn);

    DbParameter MakeParam(
      string paraName,
      object paraValue,
      ParameterDirection direction,
      Type paraType,
      string sourceColumn,
      int size);

    string ParameterPrefix { get; }
  }
}
