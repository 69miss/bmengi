using Avalonia.Data;
using Avalonia.Data.Converters;
using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace PumpsSystem.AvaloniaUI;

/// <summary>
/// Bool 转字符串转换器
/// </summary>
public class CommConverter : IValueConverter
{

    /// <summary>
    /// Bool → 字符串（正向转换）
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var paramArr = FastSerialize.Instance.FromSeparator<string>(parameter + "");
        var valueStr = value + "";
        if (value is IConvertible intVal&& (targetType == typeof(bool)|| Nullable.GetUnderlyingType(targetType) == typeof(bool)))
        {
                var trueVal = paramArr.Length > 0 ? paramArr[0] : "1";
                var falseVal = paramArr.Length > 1 ? paramArr[1] : "0";
                return !string.IsNullOrEmpty(trueVal) ? trueVal.Equals(valueStr) : !falseVal.Equals(valueStr);
        }
        return BindingOperations.DoNothing;
    }

    /// <summary>
    /// 字符串 → Bool（反向转换，如需要双向绑定才实现）
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;
        var paramArr = FastSerialize.Instance.FromSeparator<string>(parameter + "");
        var valueStr = value + "";


        if (value is bool boolVal && (targetType == typeof(int)|| targetType == typeof(short)))
        {
            if (boolVal)
                return paramArr.Length > 0 && int.TryParse(paramArr[0], out int trueVal) ? trueVal : BindingOperations.DoNothing; 
            return paramArr.Length > 1 && int.TryParse(paramArr[1], out int falseVal) ? falseVal : BindingOperations.DoNothing;
        }
        return BindingOperations.DoNothing;
    }
}

