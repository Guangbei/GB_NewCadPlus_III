using Dapper;
using GB_NewCadPlus_III;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// AttributeEditorControl：动态展示并编辑 EAV 属性（file_attribute_values）
    /// 注意：此类命名空间必须与 XAML 中的 x:Class 完全一致（XAML 中为 GB_NewCadPlus_III.AttributeEditorControl）
    /// </summary>
    public partial class AttributeEditorControl : System.Windows.Controls.UserControl
    {
        // 外部注入的数据库管理器（必须设置）
        public DatabaseManager DatabaseManager { get; set; }

        // 要显示的文件ID 与 分类ID（外部设置）
        public int FileId { get; set; }
        public int CategoryId { get; set; }

        // 绑定到 ItemsControl 的字段集合
        private ObservableCollection<AttributeFieldViewModel> _fields = new ObservableCollection<AttributeFieldViewModel>();

        public AttributeEditorControl()
        {
            InitializeComponent();

            // 绑定 ItemsControl 的数据源（FieldsItemsControl 在 XAML 中定义，x:Name="FieldsItemsControl"）
            FieldsItemsControl.ItemsSource = _fields;

            // 注册一个很小的本地转换器资源（在没有 App 资源时避免 XAML 报错）
            if (!this.Resources.Contains("DataTypeToVisibilityConverter"))
            {
                this.Resources.Add("DataTypeToVisibilityConverter", new Helpers.DataTypeToVisibilityConverter());
            }
        }

        /// <summary>
        /// 异步加载：从模板/属性定义 + 已有属性值 构建字段清单并渲染
        /// </summary>
        public async Task LoadAsync()
        {
            if (DatabaseManager == null)
                throw new InvalidOperationException("请先设置 DatabaseManager 属性。");

            _fields.Clear();

            // 1) 获取分类相关模板（可能为空）
            List<AttributeTemplate> templates = null;
            try
            {
                templates = await DatabaseManager.GetAttributeTemplatesForCategoryAsync(CategoryId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载模板失败: {ex.Message}");
                // 继续尝试直接读取属性定义（回退）
            }

            // 2) 准备要显示的 attribute_id 列表（优先模板项）
            var attributeIds = new List<int>();
            if (templates != null && templates.Count > 0)
            {
                foreach (var t in templates)
                {
                    foreach (var item in t.Items)
                    {
                        if (!attributeIds.Contains(item.AttributeId))
                            attributeIds.Add(item.AttributeId);
                    }
                }
            }

            // 如果模板为空或未定义项 -> 作为回退，加载部分常见属性（示例：material/model/diameter）
            if (attributeIds.Count == 0)
            {
                using var conn = DatabaseManager.GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                var defs = await conn.QueryAsync<int>("SELECT id FROM attribute_definitions WHERE is_core_field = 0 LIMIT 20").ConfigureAwait(false);
                attributeIds.AddRange(defs);
            }

            // 3) 加载属性定义详情（按 id）
            var attributeDefs = new List<DatabaseManager.AttributeDefinition>();
            using (var conn = DatabaseManager.GetConnection())
            {
                await conn.OpenAsync().ConfigureAwait(false);
                const string getDefsSql = @"
                    SELECT id AS Id, key_name AS KeyName, display_name AS DisplayName, data_type AS DataType, unit AS Unit
                    FROM attribute_definitions
                    WHERE id IN @Ids";
                attributeDefs = (await conn.QueryAsync<DatabaseManager.AttributeDefinition>(getDefsSql, new { Ids = attributeIds }).ConfigureAwait(false)).AsList();
            }

            // 4) 加载目标文件已存在的 EAV 值
            var existingValues = new List<FileAttributeValue>();
            try
            {
                existingValues = await DatabaseManager.GetFileAttributeValuesAsync(FileId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载已存在属性值失败: {ex.Message}");
            }

            // 5) 合并生成字段 viewmodel 集合（保留模板排序：attributeIds 顺序）
            foreach (var aid in attributeIds)
            {
                var def = attributeDefs.FirstOrDefault(d => d.Id == aid);
                if (def == null) continue;

                var fv = existingValues.FirstOrDefault(v => v.AttributeId == aid);

                var vm = new AttributeFieldViewModel
                {
                    AttributeId = def.Id,
                    KeyName = def.KeyName,
                    DisplayName = string.IsNullOrWhiteSpace(def.DisplayName) ? def.KeyName : def.DisplayName,
                    DataType = def.DataType ?? "string",
                    Unit = def.Unit,
                    // 初始化现有值（优先 number/date then string/json）
                    ValueNumber = fv?.ValueNumber,
                    ValueDate = fv?.ValueDate,
                    ValueString = fv?.ValueString ?? fv?.ValueJson
                };

                _fields.Add(vm);
            }
        }

        /// <summary>
        /// 把界面字段保存到 file_attribute_values（根据类型写入相应列）
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            if (DatabaseManager == null)
                throw new InvalidOperationException("请先设置 DatabaseManager 属性。");

            try
            {
                foreach (var f in _fields)
                {
                    var fav = new FileAttributeValue
                    {
                        FileId = FileId,
                        AttributeId = f.AttributeId,
                        ValueString = null,
                        ValueNumber = null,
                        ValueDate = null,
                        ValueJson = null
                    };

                    switch ((f.DataType ?? "string").ToLowerInvariant())
                    {
                        case "number":
                            // 允许用户以字符串形式输入数值，尝试解析
                            if (double.TryParse(f.ValueNumberString, out double num))
                                fav.ValueNumber = num;
                            else if (f.ValueNumber.HasValue)
                                fav.ValueNumber = f.ValueNumber.Value;
                            else
                                fav.ValueNumber = null;
                            break;
                        case "date":
                            if (f.ValueDate.HasValue)
                                fav.ValueDate = f.ValueDate.Value;
                            else
                                fav.ValueDate = null;
                            break;
                        case "json":
                            fav.ValueJson = f.ValueString;
                            break;
                        default:
                            // 默认字符串
                            fav.ValueString = f.ValueString;
                            break;
                    }

                    // 调用 DatabaseManager 保存（upsert）
                    await DatabaseManager.SaveFileAttributeValueAsync(fav).ConfigureAwait(false);
                }

                MessageBox.Show("属性保存完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"保存属性失败: {ex.Message}");
                MessageBox.Show($"保存属性失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 重新加载按钮事件
        /// </summary>
        private async void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存按钮事件
        /// </summary>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// 字段 ViewModel：用于绑定 UI
    /// </summary>
    public class AttributeFieldViewModel : INotifyPropertyChanged
    {
        public int AttributeId { get; set; }
        public string KeyName { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; } = "string"; // string/number/date/json
        public string Unit { get; set; }

        // 文本表示的值（用于字符串与 JSON）
        private string _valueString;
        public string ValueString
        {
            get => _valueString;
            set { _valueString = value; OnPropertyChanged(nameof(ValueString)); }
        }

        // 数值形式（双向：也支持 ValueNumberString 文本输入解析）
        public double? ValueNumber { get; set; }

        private string _valueNumberString;
        public string ValueNumberString
        {
            get => _valueNumberString ?? (ValueNumber?.ToString() ?? "");
            set { _valueNumberString = value; OnPropertyChanged(nameof(ValueNumberString)); }
        }

        // 日期形式
        private DateTime? _valueDate;
        public DateTime? ValueDate
        {
            get => _valueDate;
            set { _valueDate = value; OnPropertyChanged(nameof(ValueDate)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
