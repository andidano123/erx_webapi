
namespace ERX.Services.Helpers.Pager
{
  public class PagerParameters
  {
    private int m_cacherSize;
    private string[] m_fields;
    private int m_pageIndex;
    private int m_pageSize;
    private string m_tableName;
    private string m_where;
    private string m_order;
    private string m_procName;

    public int CacherSize
    {
      get
      {
        return this.m_cacherSize;
      }
      set
      {
        this.m_cacherSize = value;
      }
    }

    public string[] Fields
    {
      get
      {
        return this.m_fields;
      }
      set
      {
        this.m_fields = value;
      }
    }

    public int PageIndex
    {
      get
      {
        return this.m_pageIndex;
      }
      set
      {
        this.m_pageIndex = value;
      }
    }

    public int PageSize
    {
      get
      {
        return this.m_pageSize;
      }
      set
      {
        this.m_pageSize = value;
      }
    }

    public string TableName
    {
      get
      {
        return this.m_tableName;
      }
      set
      {
        this.m_tableName = value;
      }
    }

    public string Where
    {
      get
      {
        return this.m_where;
      }
      set
      {
        this.m_where = value;
      }
    }

    public string Order
    {
      get
      {
        return this.m_order;
      }
      set
      {
        this.m_order = value;
      }
    }

    public string ProcName
    {
      get
      {
        return this.m_procName;
      }
      set
      {
        this.m_procName = value;
      }
    }

    public PagerParameters()
    {
      this.m_order = "";
      this.m_pageIndex = 1;
      this.m_pageSize = 20;
      this.m_cacherSize = 0;
      this.m_where = "";
      this.m_tableName = "";
      this.m_fields = (string[]) null;
      this.m_procName = "WEB_PageView_New";
    }

    public PagerParameters(
      string tableName,
      string where,
      string order,
      int pageIndex,
      int pageSize,
      string[] fields = null,
      string procName = "WEB_PageView_New",
      int cacheSize = 0)
    {
      this.m_order = order;
      this.m_pageIndex = pageIndex;
      this.m_pageSize = pageSize;
      this.m_cacherSize = cacheSize;
      this.m_where = where;
      this.m_tableName = tableName;
      this.m_fields = fields;
      this.m_procName = procName;
    }
  }
}
