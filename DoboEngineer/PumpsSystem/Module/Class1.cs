using Jint.Native;
using PumpsSystem.Pump2;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PumpsSystem.Module;
public class IDataItemPropConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return  typeof(IDataItemProp).IsAssignableFrom(typeToConvert);
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
public class IConvertibleConverter : JsonConverter<object>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IConvertible).IsAssignableFrom(typeToConvert);
    }
    // 反序列化：根据JSON类型创建对应的IConvertible实现
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int intVal)) return intVal;
                if (reader.TryGetInt64(out var longVal)) return longVal;
                if (reader.TryGetSingle(out var floatVal)) return floatVal;
                if (reader.TryGetDouble(out double doubleVal)) return doubleVal;
                break;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean();
            case JsonTokenType.Null:
                return null;
        }
        return JsonSerializer.Deserialize(ref reader, typeToConvert, options);
    }

    // 序列化：根据IConvertible的实际类型写入JSON
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {

        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
         JsonSerializer.Serialize(writer, value, options);
        //if (value is IConvertible baseVal)
        //switch (baseVal.GetTypeCode())
        //    {
        //        case TypeCode.Int32:
        //            writer.WriteNumberValue((int)value);
        //            break;
        //        case TypeCode.Double:
        //            writer.WriteNumberValue((double)value);
        //            break;
        //        case TypeCode.String:
        //            writer.WriteStringValue((string)value);
        //            break;
        //        case TypeCode.Boolean:
        //            writer.WriteBooleanValue((bool)value);
        //            break;
        //        default:
        //            throw new NotSupportedException($"类型 {value.GetType()} 不支持转换为JSON");
        //    }
    }
}