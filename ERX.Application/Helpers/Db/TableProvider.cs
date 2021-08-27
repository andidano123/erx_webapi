
using ERX.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ERX.Services.Helpers.Db
{
  public class TableProvider : BaseDataProvider, ITableProvider
  {
    private string m_tableName;

    public string TableName
    {
      get
      {
        return this.m_tableName;
      }
    }

    public TableProvider(DbHelper database, string tableName)
      : base(database)
    {
      this.m_tableName = "";
      this.m_tableName = tableName;
    }

    public TableProvider(string connectionString, string tableName)
      : base(connectionString)
    {
      this.m_tableName = "";
      this.m_tableName = tableName;
    }

    public void BatchCommitData(DataSet dataSet, string[][] columnMapArray)
    {
      this.BatchCommitData(dataSet.Tables[0], columnMapArray);
    }

    public void BatchCommitData(DataTable table, string[][] columnMapArray)
    {
      using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(this.Database.ConnectionString))
      {
        sqlBulkCopy.DestinationTableName = this.TableName;
        foreach (string[] columnMap in columnMapArray)
          sqlBulkCopy.ColumnMappings.Add(columnMap[0], columnMap[1]);
        sqlBulkCopy.WriteToServer(table);
        sqlBulkCopy.Close();
      }
    }

    public void CommitData(DataTable dt)
    {
      this.Database.UpdateDataSet(this.ConstructDataSet(dt), this.TableName);
    }

    private DataSet ConstructDataSet(DataTable dt)
    {
      if (dt.DataSet != null)
        return dt.DataSet;
      return new DataSet() { Tables = { dt } };
    }

    public void Delete(string where)
    {
      this.Database.ExecuteNonQuery(string.Format("DELETE FROM {0} {1}", (object) this.TableName, (object) where));
    }

    public DataSet Get(string where)
    {
      return this.Database.ExecuteDataset(string.Format("SELECT * FROM {0} {1}", (object) this.TableName, (object) where));
    }

    public DataTable GetEmptyTable()
    {
      DataTable emptyTable = this.Database.GetEmptyTable(this.TableName);
      emptyTable.TableName = this.TableName;
      return emptyTable;
    }

    public DataRow NewRow()
    {
      DataTable emptyTable = this.GetEmptyTable();
      DataRow dataRow = emptyTable.NewRow();
      for (int index = 0; index < emptyTable.Columns.Count; ++index)
        dataRow[index] = (object) DBNull.Value;
      return dataRow;
    }

    public T GetObject<T>(string where)
    {
      DataRow one = this.GetOne(where);
      if (one == null)
        return default (T);
      return DataHelper.ConvertRowToObject<T>(one);
    }

    public IList<T> GetObjectList<T>(string where)
    {
      DataSet ds = this.Get(where);
      if (Validate.CheckedDataSet(ds))
        return DataHelper.ConvertDataTableToObjects<T>(ds.Tables[0]);
      return (IList<T>) null;
    }

    public DataRow GetOne(string where)
    {
      DataSet dataSet = this.Get(where);
      if (dataSet.Tables[0].Rows.Count > 0)
        return dataSet.Tables[0].Rows[0];
      return (DataRow) null;
    }

    public int GetRecordsCount(string where)
    {
      if (where == null)
        where = "";
      return int.Parse(this.Database.ExecuteScalarToStr(CommandType.Text, string.Format("SELECT COUNT(*) FROM {0} {1}", (object) this.TableName, (object) where)));
    }

    public void Insert(DataRow row)
    {
      DataTable emptyTable = this.GetEmptyTable();
      try
      {
        DataRow row1 = emptyTable.NewRow();
        for (int index = 0; index < emptyTable.Columns.Count; ++index)
          row1[index] = row[index];
        emptyTable.Rows.Add(row1);
        this.CommitData(emptyTable);
      }
      catch
      {
        throw;
      }
      finally
      {
        emptyTable.Rows.Clear();
        emptyTable.AcceptChanges();
      }
    }
  }
}
