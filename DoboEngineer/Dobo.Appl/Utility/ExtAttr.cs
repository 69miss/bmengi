using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility
{
    public static class ExtAttr
    {
       static ConditionalWeakTable<object, Dictionary<string, object>> ext = new ConditionalWeakTable<object, Dictionary<string, object>>();
        public static void Ext(this object obj, string name, object value)
        {
            var dic = ext.GetOrCreateValue(obj);
            dic[name] = value;
        }
        public static object? Ext(this object obj, string name)
        {
            if (ext.TryGetValue(obj, out var dic))
            {
                if (dic.TryGetValue(name, out var val))
                {
                    return val;
                }
            }
            return null;
        }
        public static T? Ext<T>(this object obj, string name) where T : class
        {
            var val = obj.Ext(name);
            return val is T tval ? tval : null;
        }
        public static void ExtClear(object obj) {
            ext.Remove(obj);
        }
        public static IDictionary<string, object> Ext(this object obj)
        {
            return ext.GetOrCreateValue(obj);
        }
        public static IEnumerable<KeyValuePair<object, Dictionary<string, object>>> GetSourceTable() {
            return ext;
        }
    }
}
