using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility
{
    public class FastSerialize
    {
       static Lazy<FastSerialize> instance =new Lazy<FastSerialize>(() => new FastSerialize());
        public static FastSerialize Instance {
            get { return instance.Value; }
        }
        public string BySeparator(IEnumerable<IConvertible> arr, char separator = '|', char? warp1 = null, char? warp2 = null)
        {

            return warp1 + string.Join(separator, arr) + warp2;
        }
        public T[] FromSeparator<T>(string str, char separator = '|', char? warp1 = null, char? warp2 = null) where T : IConvertible
        {
            if (string.IsNullOrEmpty(str))
                return null;
            str = warp1.HasValue ? str.TrimStart(warp1.Value) : str;
            str = warp2.HasValue ? str.TrimEnd(warp2.Value) : str;
            var strArr = str.Split(separator);
            var ty = typeof(T);
            //if (ty == typeof(int))
            //    return strArr.Select(p => (T)(object)((IConvertible)p).ToInt32(null)).ToArray();
            return strArr.Select(p => (T)Convert.ChangeType(p, ty)).ToArray();
        }
        public T Deserialize<T>(string str){
            return JsonSerializer.Deserialize<T>(str);
        }
        public string Serialize<T>(T obj )
        {
            return JsonSerializer.Serialize<T>(obj);
        }
        public string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj);
        }
    }
}
