using GB_NewCadPlus_III.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Clipboard = System.Windows.Clipboard;
using Path = System.IO.Path;
using MessageBox = System.Windows.MessageBox;
using DataTable = Autodesk.AutoCAD.DatabaseServices.DataTable;

namespace GB_NewCadPlus_III.Views
{
    /// <summary>
    /// 导入确认窗口交互逻辑
    /// </summary>
    public partial class ImportConfirmWindow : Window
    {
        private readonly ImportEntityDto _dto;// 导入的实体数据
        private readonly WpfMainWindow _mainWindow; // 引用主窗口以访问方法
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dto">导入实体数据传输对象</param>
        /// <param name="mainWindow">主窗口</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ImportConfirmWindow(ImportEntityDto dto, WpfMainWindow mainWindow)
        {
            InitializeComponent();
            _dto = dto ?? throw new ArgumentNullException(nameof(dto));// 参数不能为空 主窗口引用
            // 初始化控件 绑定数据源引用 
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));// 参数不能为空
            // 加载预览图片 窗口加载时绑定数据
            this.Loaded += (s, e) =>
            {
                // 加载预览图片 加载预览图
                LoadPreviewImage();
                // 绑定数据源 绑定属性到数据网格 绑定属性到数据网格
                BindPropertiesToGrid();
            };

            BtnConfirm.Click += BtnConfirm_Click;//确认按钮点击事件
            BtnCancel.Click += (s, e) => this.DialogResult = false;//取消按钮点击事件
            BtnPastePreview.Click += BtnPastePreview_Click;//粘贴预览按钮点击事件
            BtnExportTemplate.Click += BtnExportTemplate_Click;//导出模板按钮点击事件
        }
        /// <summary>
        /// 绑定属性
        /// </summary>
        private void LoadPreviewImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(_dto.PreviewImagePath) && File.Exists(_dto.PreviewImagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_dto.PreviewImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // 确保文件句柄被释放
                    bitmap.EndInit();
                    bitmap.Freeze(); // 跨线程访问需要
                    PreviewImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"加载预览图失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 绑定属性到数据网格
        /// </summary>
        private void BindPropertiesToGrid()
        {
            // 使用主窗口已有的方法来准备数据
            var displayData = _mainWindow.PrepareFileDisplayData(_dto.FileStorage, _dto.FileAttribute);
            PropertiesGrid.ItemsSource = displayData;
        }
        /// <summary>
        /// 粘贴预览剪贴板图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPastePreview_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                var imageSource = Clipboard.GetImage();//获取剪贴板中的图片
                PreviewImage.Source = imageSource;//设置预览图片源更新预览图控件的图片源

                // 将新预览图保存到临时文件，并更新DTO
                try
                {
                    //创建临时目录 创建临时文件夹
                    string tempDir = Path.Combine(Path.GetTempPath(), "GB_NewCadPlus_III_Previews");
                    // 创建临时文件夹  如果不存在
                    Directory.CreateDirectory(tempDir);
                    // 创建临时文件 生成唯一文件名 生成唯一的预览图文件名
                    string newPreviewPath = Path.Combine(tempDir, $"preview_clipboard_{Guid.NewGuid()}.png");
                    // 创建图片源 保存图片到文件
                    var encoder = new PngBitmapEncoder();
                    // 创建图片帧
                    encoder.Frames.Add(BitmapFrame.Create(imageSource));
                    // 保存图片保存到文件流
                    using (var fs = new FileStream(newPreviewPath, FileMode.Create))
                    {
                        // 保存图片
                        encoder.Save(fs);
                    }
                    // 保存图片路径 更新DTO的预览图路径
                    _dto.PreviewImagePath = newPreviewPath;
                    LogManager.Instance.LogInfo($"预览图已从剪贴板更新并保存到: {newPreviewPath}");
                }
                catch (Exception ex)
                {
                    LogManager.Instance.LogError($"保存剪贴板预览图失败: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("剪贴板中没有图片。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        /// <summary>
        /// 粘贴剪贴板图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnExportTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 从UI读取当前值
                UpdateDtoFromGrid();

                // 创建一个只包含当前图元信息的DataTable
                var dt = _mainWindow.CreateTemplateDataTable();
                dt.Rows.Clear(); // 清空模板里的示例行
                DataRow newRow = dt.NewRow();

                // 填充FileStorage
                foreach (PropertyInfo prop in typeof(FileStorage).GetProperties())
                {
                    if (dt.Columns.Contains(prop.Name))
                    {
                        newRow[prop.Name] = prop.GetValue(_dto.FileStorage) ?? DBNull.Value;
                    }
                }
                // 填充FileAttribute
                foreach (PropertyInfo prop in typeof(FileAttribute).GetProperties())
                {
                    if (dt.Columns.Contains(prop.Name))
                    {
                        newRow[prop.Name] = prop.GetValue(_dto.FileAttribute) ?? DBNull.Value;
                    }
                }
                dt.Rows.Add(newRow);

                // 导出
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    FileName = $"图元_{_dto.FileStorage.DisplayName}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (_mainWindow.ExportDataTableToExcel(dt, saveFileDialog.FileName))
                    {
                        MessageBox.Show("模板导出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出模板失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 确认按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 从UI更新DTO
                UpdateDtoFromGrid(); // 确保先保存用户在表格中修改的属性

                // 2. 提示用户是否关闭当前文件
                var result = MessageBox.Show("是否关闭当前文件？\n关闭后可正常上传，不关闭可能导致文件被占用。", "关闭文件提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                        if (doc != null)
                        {
                            doc.CloseAndSave(doc.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"关闭文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    var giveUp = MessageBox.Show("是否放弃本次识别并退出？", "放弃识别", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (giveUp == MessageBoxResult.Yes)
                    {
                        this.DialogResult = false;
                        return;
                    }
                    return;
                }

                // 3. 再次从UI更新DTO，确保所有更改都已保存
                UpdateDtoFromGrid();

                // 4. 执行导入
                _mainWindow.SetSelectedFileForImport(_dto);
                await _mainWindow.UploadFileAndSaveToDatabase(_dto);

                // 5. 关闭窗口
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = false;
            }
        }

        /// <summary>
        /// 从UI的DataGrid中读取修改后的值，并更新回DTO对象
        /// </summary>
        private void UpdateDtoFromGrid()
        {
            var items = PropertiesGrid.ItemsSource as List<CategoryPropertyEditModel>;
            if (items == null) return;

            foreach (var item in items)
            {
                // 使用主窗口的 SetFileAttributeProperty 方法来解析和设置属性
                _mainWindow.SetFileAttributeProperty(_dto.FileAttribute, item.PropertyName1, item.PropertyValue1);
                _mainWindow.SetFileAttributeProperty(_dto.FileAttribute, item.PropertyName2, item.PropertyValue2);
                // 采集文件信息（完善：支持FileStorage字段）
                _mainWindow.SetFileStorageProperty(_dto.FileStorage, item.PropertyName1, item.PropertyValue1);
                _mainWindow.SetFileStorageProperty(_dto.FileStorage, item.PropertyName2, item.PropertyValue2);
            }
        }

    }
}