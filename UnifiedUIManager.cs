using System;
using System.Linq;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 统一界面管理器 - 管理WPF和WinForm界面的统一访问
    /// </summary>
    public static class UnifiedUIManager
    {
        private static FormMain _winFormInstance;
        private static WpfMainWindow _wpfInstance;

        /// <summary>
        /// 设置WinForm实例
        /// </summary>
        public static void SetWinFormInstance(FormMain instance)
        {
            _winFormInstance = instance;
        }

        /// <summary>
        /// 设置WPF实例
        /// </summary>
        public static void SetWpfInstance(WpfMainWindow instance)
        {
            _wpfInstance = instance;
        }

        /// <summary>
        /// 获取TextBox的值（自动从当前活动界面获取）
        /// </summary>
        public static string GetTextBoxValue(string textBoxName, string defaultValue = "")
        {
            // 优先从WPF界面获取
            if (_wpfInstance != null)
            {
                string wpfValue = GetWpfTextBoxValue(textBoxName);
                if (wpfValue != null)
                    return string.IsNullOrEmpty(wpfValue) ? defaultValue : wpfValue;
            }

            // 如果WPF界面没有或为空，从WinForm界面获取
            if (_winFormInstance != null)
            {
                string winFormValue = GetWinFormTextBoxValue(textBoxName);
                if (winFormValue != null)
                    return string.IsNullOrEmpty(winFormValue) ? defaultValue : winFormValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// 获取WPF界面TextBox值
        /// </summary>
        private static string GetWpfTextBoxValue(string textBoxName)
        {
            try
            {
                if (_wpfInstance == null) return null;

                // 使用反射获取WPF控件
                var field = _wpfInstance.GetType().GetField(textBoxName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    var textBox = field.GetValue(_wpfInstance) as TextBox;
                    if (textBox != null)
                    {
                        return TextBoxValueHelper.GetTextBoxValue(textBox);
                    }
                }

                // 尝试使用FindName方法
                var textBoxByName = _wpfInstance.FindName(textBoxName) as TextBox;
                if (textBoxByName != null)
                {
                    return TextBoxValueHelper.GetTextBoxValue(textBoxByName);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取WinForm界面TextBox值
        /// </summary>
        private static string GetWinFormTextBoxValue(string textBoxName)
        {
            try
            {
                if (_winFormInstance == null) return null;

                // 使用反射获取WinForm控件
                var field = _winFormInstance.GetType().GetField(textBoxName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    var textBox = field.GetValue(_winFormInstance) as TextBox;
                    if (textBox != null)
                    {
                        return string.IsNullOrEmpty(textBox.Text) ? textBox.Text : textBox.Text.Trim();
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置TextBox的值
        /// </summary>
        public static void SetTextBoxValue(string textBoxName, string value)
        {
            // 同时设置两个界面的值
            SetWpfTextBoxValue(textBoxName, value);
            SetWinFormTextBoxValue(textBoxName, value);
        }

        /// <summary>
        /// 设置WPF界面TextBox值
        /// </summary>
        private static void SetWpfTextBoxValue(string textBoxName, string value)
        {
            try
            {
                if (_wpfInstance == null) return;

                var field = _wpfInstance.GetType().GetField(textBoxName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    var textBox = field.GetValue(_wpfInstance) as TextBox;
                    if (textBox != null)
                    {
                        textBox.Text = value;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 设置WinForm界面TextBox值
        /// </summary>
        private static void SetWinFormTextBoxValue(string textBoxName, string value)
        {
            try
            {
                if (_winFormInstance == null) return;

                var field = _winFormInstance.GetType().GetField(textBoxName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null)
                {
                    var textBox = field.GetValue(_winFormInstance) as TextBox;
                    if (textBox != null)
                    {
                        textBox.Text = value;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
        }
        /// <summary>
        /// TextBox值获取帮助类
        /// </summary>

    }
    public static class TextBoxValueHelper
    {
        /// <summary>
        /// 获取TextBox的值，如果为空则返回Tag属性值作为默认值
        /// </summary>
        public static string GetTextBoxValue(TextBox textBox)
        {
            if (textBox == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(textBox.Text))
                return textBox.Text.Trim();

            return textBox.Tag?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 获取TextBox的数值
        /// </summary>
        public static double GetNumericValueOrDefault(TextBox textBox, double defaultValue = 0)
        {
            string value = GetTextBoxValue(textBox);
            if (double.TryParse(value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取TextBox的整数值
        /// </summary>
        public static int GetIntTextBoxValue(TextBox textBox, int defaultValue = 0)
        {
            string value = GetTextBoxValue(textBox);
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 从统一界面管理器获取值
        /// </summary>
        public static string GetUnifiedValue(string textBoxName, string defaultValue = "")
        {
            return UnifiedUIManager.GetTextBoxValue(textBoxName, defaultValue);
        }
    }
}
