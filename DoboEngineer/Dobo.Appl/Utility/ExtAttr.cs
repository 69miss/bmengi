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
    public static bool TryParse<T>(this object value, out T result)
    {
        if (TryParse(value, typeof(T), out object objResult))
        {
            result = (T)objResult;
            return true;
        }
        result = default(T);
        return false;
    }

    /// <summary>
    ///  针对IConvertible进行转换
    /// </summary>
    /// <param name="value">需要转换的原始值</param>
    /// <param name="targetType">目标类型 </param>
    /// <param name="result">转换结果</param>
    /// <returns>是否转换成功</returns>
    public static bool TryParse(this object value, Type targetType, out object result)
    {
        result = null;

        if (value == null || value == DBNull.Value || targetType == null)
            return false;
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        TypeCode targetTypeCode = Type.GetTypeCode(underlyingType);
        if (value.GetType() == underlyingType)
        {
            result = value;
            return true;
        }
        if (value is string str)
        {
            switch (targetTypeCode)
            {
                case TypeCode.Boolean: if (bool.TryParse(str, out bool b)) { result = b; return true; } break;
                case TypeCode.Char: if (char.TryParse(str, out char c)) { result = c; return true; } break;
                case TypeCode.SByte: if (sbyte.TryParse(str, out sbyte sb)) { result = sb; return true; } break;
                case TypeCode.Byte: if (byte.TryParse(str, out byte b8)) { result = b8; return true; } break;
                case TypeCode.Int16: if (short.TryParse(str, out short i16)) { result = i16; return true; } break;
                case TypeCode.UInt16: if (ushort.TryParse(str, out ushort u16)) { result = u16; return true; } break;
                case TypeCode.Int32: if (int.TryParse(str, out int i32)) { result = i32; return true; } break;
                case TypeCode.UInt32: if (uint.TryParse(str, out uint u32)) { result = u32; return true; } break;
                case TypeCode.Int64: if (long.TryParse(str, out long i64)) { result = i64; return true; } break;
                case TypeCode.UInt64: if (ulong.TryParse(str, out ulong u64)) { result = u64; return true; } break;
                case TypeCode.Single: if (float.TryParse(str, out float f)) { result = f; return true; } break;
                case TypeCode.Double: if (double.TryParse(str, out double d)) { result = d; return true; } break;
                case TypeCode.Decimal: if (decimal.TryParse(str, out decimal dec)) { result = dec; return true; } break;
                case TypeCode.DateTime: if (DateTime.TryParse(str, out DateTime dt)) { result = dt; return true; } break;
                case TypeCode.String: result = str; return true;
            }
            return false;
        }
        if (value is IConvertible convertible)
        {
            try
            {
                switch (targetTypeCode)
                {
                    case TypeCode.Boolean: result = convertible.ToBoolean(null); return true;
                    case TypeCode.Char: result = convertible.ToChar(null); return true;
                    case TypeCode.SByte: result = convertible.ToSByte(null); return true;
                    case TypeCode.Byte: result = convertible.ToByte(null); return true;
                    case TypeCode.Int16: result = convertible.ToInt16(null); return true;
                    case TypeCode.UInt16: result = convertible.ToUInt16(null); return true;
                    case TypeCode.Int32: result = convertible.ToInt32(null); return true;
                    case TypeCode.UInt32: result = convertible.ToUInt32(null); return true;
                    case TypeCode.Int64: result = convertible.ToInt64(null); return true;
                    case TypeCode.UInt64: result = convertible.ToUInt64(null); return true;
                    case TypeCode.Single: result = convertible.ToSingle(null); return true;
                    case TypeCode.Double: result = convertible.ToDouble(null); return true;
                    case TypeCode.Decimal: result = convertible.ToDecimal(null); return true;
                    case TypeCode.DateTime: result = convertible.ToDateTime(null); return true;
                    case TypeCode.String: result = convertible.ToString(null); return true;
                }
            }
            catch (OverflowException) { return false; }
            catch (InvalidCastException) { return false; }
        }
        return false;
    }
}

