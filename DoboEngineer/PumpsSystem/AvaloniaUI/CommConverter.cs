using Avalonia.Data.Converters;
using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
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
        if (value is int intVal)
        {
            if (targetType == typeof(bool))
            {
                
            }

        }
        return "";
    }

        /// <summary>
        /// 字符串 → Bool（反向转换，如需要双向绑定才实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            string strValue = value.ToString().Trim();
            // 根据文本反向转换为 bool（示例："启用"→true，其他→false）
            return strValue == "启用" || strValue == "是" || strValue == "√";
        }
    }

