﻿using ERX.Utils;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ERX.Services.Helpers.Db;
namespace ERX.Services.Helpers.Message
{
  public class MessageHelper
  {
    private MessageHelper()
    {
    }

    public static Message GetMessage(List<DbParameter> prams)
    {
      return new Message(TypeParse.StrToInt(prams[prams.Count - 1].Value, -1), prams[prams.Count - 2].Value.ToString());
    }

    public static Message GetMessage(
      DbHelper database,
      string procName,
      List<DbParameter> prams)
    {
      database.RunProc(procName, prams);
      return MessageHelper.GetMessage(prams);
    }

    public static Message GetMessageForDataSet(
      DbHelper database,
      string procName,
      List<DbParameter> prams)
    {
      DataSet ds = (DataSet) null;
      database.RunProc(procName, prams, out ds);
      Message message = MessageHelper.GetMessage(prams);
      if (message.MessageID == 0)
        message.AddEntity((object) ds);
      return message;
    }

    public static Message GetMessageForObject<T>(
      DbHelper database,
      string procName,
      List<DbParameter> prams)
    {
      DataSet ds = (DataSet) null;
      database.RunProc(procName, prams, out ds);
      Message message = MessageHelper.GetMessage(prams);
      if (message.MessageID == 0)
        message.AddEntity((object) DataHelper.ConvertRowToObject<T>(ds.Tables[0].Rows[0]));
      return message;
    }

    public static Message GetMessageForObjectList<T>(
      DbHelper database,
      string procName,
      List<DbParameter> prams)
    {
      DataSet ds = (DataSet) null;
      database.RunProc(procName, prams, out ds);
      Message message = MessageHelper.GetMessage(prams);
      if (message.MessageID == 0)
        message.AddEntity((object) DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]));
      return message;
    }

    public static T GetObject<T>(DbHelper database, string procName, List<DbParameter> prams)
    {
      return database.RunProcObject<T>(procName, prams);
    }

    public static IList<T> GetObjectList<T>(
      DbHelper database,
      string procName,
      List<DbParameter> prams)
    {
      return database.RunProcObjectList<T>(procName, prams);
    }
  }
}
