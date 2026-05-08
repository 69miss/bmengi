using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dobo.Appl.Utility;
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

public static class ObjectExtensions
{
    // AOT 安全的核心特性，防止属性被编译器剪裁掉
    public static void Assign<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this T target, T source)
    {
        if (source == null || target == null) return;

        var props = typeof(T).GetProperties();

        foreach (var p in props)
        {
            // 确保属性既可以读取，又可以写入
            if (p.CanRead && p.CanWrite)
            {
                // 从 source 读取值
                var val = p.GetValue(source);
                // 赋值给 target
                p.SetValue(target, val);
            }
        }
    }
    public static void Each<T>(this IEnumerable<T> values, Action<T> action) {
        foreach (var item in values)
        {
            action(item);
        }
    }

}

