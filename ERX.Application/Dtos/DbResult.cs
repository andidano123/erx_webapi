using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERX.Services.Dtos
{
    public class DbResult
    {
        public bool IsSuccess { get; set; } = true;
        [JsonIgnore]
        public Exception ErrorException { get; set; }
        public string ErrorMessage { get; set; } = "操作成功";
    }

    public class DbResult<T>: DbResult
    {
        public T Data { get; set; }
    }
}
