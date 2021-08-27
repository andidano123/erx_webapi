
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using ERX.Services.Helpers.Db;
namespace ERX.Services.Helpers.Pager
{
  public class PagerManager
  {
    private DbHelper m_dbHelper;
    private IDictionary<int, PagerSet> m_fixedCacher;
    private PagerParameters m_prams;

    public PagerManager(DbHelper dbHelper)
    {
      this.m_dbHelper = dbHelper;
    }

    public PagerManager(string connectionString)
    {
      this.m_dbHelper = new DbHelper(connectionString);
    }

    public PagerManager(PagerParameters prams, DbHelper dbHelper)
    {
      this.m_prams = prams;
      this.m_dbHelper = dbHelper;
    }

    public PagerManager(PagerParameters prams, string connectionString)
    {
      this.m_prams = prams;
      this.m_dbHelper = new DbHelper(connectionString);
      if (prams.CacherSize <= 0)
        return;
      this.m_fixedCacher = (IDictionary<int, PagerSet>) new Dictionary<int, PagerSet>(prams.CacherSize);
    }

    private void CacheObject(int index, PagerSet pagerSet)
    {
      if (this.m_fixedCacher != null)
      {
        this.m_fixedCacher.Add(index, pagerSet);
      }
      else
      {
        if (this.m_prams.CacherSize <= 0)
          return;
        this.m_fixedCacher = (IDictionary<int, PagerSet>) new Dictionary<int, PagerSet>(this.m_prams.CacherSize);
        this.m_fixedCacher.Add(index, pagerSet);
      }
    }

    private PagerSet GetCachedObject(int index)
    {
      if (this.m_fixedCacher == null || !this.m_fixedCacher.ContainsKey(index))
        return (PagerSet) null;
      return this.m_fixedCacher[index];
    }

    protected string GetFieldString(string[] fields)
    {
      if (fields == null)
        return "*";
      StringBuilder stringBuilder = new StringBuilder();
      for (int index = 0; index < fields.Length; ++index)
        stringBuilder.AppendFormat("{0},", (object) fields[index]);
      string str = stringBuilder.ToString();
      return str == null || str.Length <= 0 ? "*" : str.Substring(0, str.Length - 1);
    }

    public PagerSet GetPagerSet()
    {
      return this.GetPagerSet(this.m_prams);
    }

    public PagerSet GetPagerSet(PagerParameters pramsPager)
    {
      if (this.m_prams == null)
        this.m_prams = pramsPager;
      if (pramsPager.PageIndex < 0)
        return (PagerSet) null;
      List<DbParameter> prams = new List<DbParameter>()
      {
        this.m_dbHelper.MakeInParam("TableName", (object) pramsPager.TableName),
        this.m_dbHelper.MakeInParam("ReturnFields", (object) this.GetFieldString(pramsPager.Fields)),
        this.m_dbHelper.MakeInParam("PageSize", (object) pramsPager.PageSize),
        this.m_dbHelper.MakeInParam("PageIndex", (object) pramsPager.PageIndex),
        this.m_dbHelper.MakeInParam("Where", (object) pramsPager.Where),
        this.m_dbHelper.MakeInParam("Order", (object) pramsPager.Order),
        this.m_dbHelper.MakeOutParam("PageCount", typeof (int)),
        this.m_dbHelper.MakeOutParam("RecordCount", typeof (int))
      };
      DataSet ds = new DataSet();
      this.m_dbHelper.RunProc(pramsPager.ProcName, prams, out ds);
      return new PagerSet(pramsPager.PageIndex, pramsPager.PageSize, Convert.ToInt32(prams[prams.Count - 3].Value), Convert.ToInt32(prams[prams.Count - 2].Value), ds)
      {
        PageSet = {
          DataSetName = "PagerSet_" + pramsPager.TableName
        }
      };
    }
  }
}
