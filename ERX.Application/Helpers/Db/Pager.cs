using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERX.Services.Providers
{
    /// <summary>
    /// 获取分页参数
    /// </summary>
    public class Pager
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
        public Pager()
        {
        }

        public Pager(int page, int limit)
        {
            this.Page = page;
            this.Limit = limit;
        }
    }
    public class Pager<T>: Pager
    {
        public Pager()
        {
        }
        public Pager(int page, int limit,int count, IList<T> data):base(page,limit)
        {
            this.count = count;
            externalData1 = 0;
            externalData2 = 0;
            externalData3 = 0;

            this.data = data;
        }
        /// <summary>
        /// 记录总数
        /// </summary>
        /// 
        public int count { get; set; }
        public int externalData1 { get; set; }
        public int externalData2 { get; set; }
        public int externalData3 { get; set; }

        /// <summary>
        /// 页数据
        /// </summary>
        /// 
        public IList<T> data { get; set; }
        /// <summary>
        /// 解析提示文本
        /// </summary>
        /// 
        public string msg { get; set; } = "";
        /// <summary>
        /// 解析接口状态
        /// </summary>
        /// 
        public int code { get; set; } = 0;

    }
}
