using ERX.Services.Helpers.Db;
using ERX.Services.Providers.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERX.Service
{
    public class ServiceManage<T>
    {
        private static IDictionary<Type, T> dic = new Dictionary<Type, T>();
        private static readonly object _lock = new object();
        public static T Instance
        {
            get
            {
                Type type = typeof(T);
                if (!dic.TryGetValue(type, out T t))
                {
                    lock (_lock)
                    {
                        if (!dic.TryGetValue(type, out t))
                        {
                            t = ClassFactory.GetDataProvider<T>();
                            dic[type] = t;
                        }
                    }
                }
                return t;
            }
        }
    }
}
