using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Visibility = System.Windows.Visibility;

namespace GB_NewCadPlus_III.Helpers
{
    /// <summary>
    /// 将属性定义的 data_type 字符串映射到 Visibility，用于在 XAML 中根据类型选择控件显示。
    /// 用法示例：
    /// - ConverterParameter="string"    -> 当 DataType == "string" 时 Visible，否则 Collapsed
    /// - ConverterParameter="number"    -> 当 DataType == "number" 时 Visible，否则 Collapsed
    /// - ConverterParameter="date"      -> 当 DataType == "date" 时 Visible，否则 Collapsed
    /// - ConverterParameter="any"       -> 始终 Visible（回退）
    /// - ConverterParameter 可以是逗号分隔的多个类型，例如 "string,number"
    /// </summary>
    public class DataTypeToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 将 dataType 映射为 Visibility
        /// value: 期望为 string（如 "string","number","date","json","bool"）
        /// parameter: 期望显示的类型（字符串），支持逗号分隔或 "any"
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string dataType = (value as string)?.Trim().ToLowerInvariant() ?? string.Empty;
                string param = (parameter as string)?.Trim().ToLowerInvariant() ?? string.Empty;

                // 参数为 "any" 表示总是可见（fallback）
                if (param == "any" || string.IsNullOrEmpty(param))
                    return Visibility.Visible;

                // 支持多个期望类型，形如 "string,number"
                var wanted = param.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.Trim())
                                  .Where(s => !string.IsNullOrEmpty(s))
                                  .ToArray();

                // 如果没有解析到任何期望类型，默认 Visible
                if (wanted.Length == 0)
                    return Visibility.Visible;

                // 当 dataType 与任何 wanted 匹配时 Visible，否则 Collapsed
                if (wanted.Contains(dataType))
                    return Visibility.Visible;

                return Visibility.Collapsed;
            }
            catch
            {
                // 任何异常都返回 Collapsed 以避免影响 UI 行为
                return Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 不支持反向转换
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("DataTypeToVisibilityConverter 不支持 ConvertBack");
        }
    }
}
