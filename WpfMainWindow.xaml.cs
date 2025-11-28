using Microsoft.VisualBasic;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.Style;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static TextBoxValueHelper;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Border = System.Windows.Controls.Border;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using DataTable = System.Data.DataTable;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Panel = System.Windows.Controls.Panel;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using TabControl = System.Windows.Controls.TabControl;
using TextBox = System.Windows.Controls.TextBox;
using TreeView = System.Windows.Controls.TreeView;
using UserControl = System.Windows.Controls.UserControl;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// WpfMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WpfMainWindow : UserControl
    {
        #region  私有字段和属性

        #region 服务器端字段和属性
        private string _serverIP = "localhost";
        private int _serverPort = 3306;
        private string _databaseName = "cad_sw_library";
        private string _username = "root";
        private string _password = "root";
        private string _storagePath = "D:\\GB_Tools\\Cad_Sw_Library";
        private bool _useDPath = true;
        private bool _autoSync = true;
        private int _syncInterval = 30;
        ServerSyncManager _serverSyncManager;
        #endregion
        private string _selectedFilePath; // 选中的文件路径
        private string _selectedPreviewImagePath; // 选中的预览图片路径
        private FileStorage _currentFileStorage; // 当前文件存储信息
        private FileAttribute _currentFileAttribute; // 当前文件属性信息
                                                     // 添加预览图片缓存相关字段和方法
        private readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();
        private readonly string _previewCachePath;


        /// <summary>
        /// 在WpfMainWindow类中添加以下字段和属性
        /// </summary>
        private ManagementOperationType _currentOperation = ManagementOperationType.None;

        /// <summary>
        /// 创建结构树节点
        /// </summary>
        private List<CategoryTreeNode> _categoryTreeNodes = new List<CategoryTreeNode>();

        /// <summary>
        /// 添加数据库管理器
        /// </summary>
        private DatabaseManager _databaseManager;

        /// <summary>
        /// 在WpfMainWindow类中添加字段
        /// </summary>
        private CategoryTreeNode _selectedCategoryNode; // 在分类架构树的当前选中的分类节点

        /// <summary>
        /// 添加字段
        /// </summary>
        private FileManager _fileManager;

        /// <summary>
        /// 添加枚举类型
        /// </summary>
        public enum ManagementOperationType
        {
            None,
            AddCategory,
            AddSubcategory
        }

        /// <summary>
        /// 数据库连接字符串（应该从配置文件读取）
        /// </summary>
        private string _connectionString = "Server=localhost;Database=cad_sw_library;Uid=root;Pwd=root;";

        /// <summary>
        /// 是否使用数据库模式
        /// </summary>
        private bool _useDatabaseMode = true;

        /// <summary>
        /// 排序顺序
        /// </summary>
        private int _sort_order = 0;

        /// <summary>
        /// 当前选中的数据库类型（CAD或SW）
        /// </summary>
        private string _currentDatabaseType = "";

        /// <summary>
        /// 当前选中的节点类型（分类、子分类、图元）
        /// </summary>
        private string _currentNodeType = "";

        /// <summary>
        /// 当前选中的节点ID
        /// </summary>
        private int _currentNodeId = 0;

        /// <summary>
        /// CAD文件存储路径
        /// </summary>
        private string _cadStoragePath = "";

        /// <summary>
        /// SW文件存储路径
        /// </summary>
        private string _swStoragePath = "";

        /// <summary>
        /// 当前选中的节点对象（用于修改操作）
        /// </summary>
        private object? _currentSelectedNode = null;

        /// <summary>
        /// 用于显示分类树的TreeView控件
        /// </summary>
        private System.Windows.Controls.TreeView _categoryTreeView;

        /// <summary>
        /// 添加预览图片显示的Viewbox引用
        /// </summary>
        private Viewbox previewViewbox;

        /// <summary>
        /// 拿到本app的local的路径，并创建GB_CADPLUS文件夹
        /// </summary>
        public static string AppPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GB_CADPLUS");

        /// <summary>
        /// 文件路径与名称  resourcesFile
        /// </summary>
        public static string? filePathAndName = null;

        /// <summary>
        /// 引用文件referenceFile文件夹  
        /// </summary>
        public static string referenceFile = System.IO.Path.Combine(AppPath, "ReferenceFile");

        /// <summary>
        /// 添加一个字典来跟踪哪些TabItem已经加载过
        /// </summary>
        private Dictionary<string, bool> loadedTabItems = new Dictionary<string, bool>();

        #endregion

        /// <summary>
        /// WpfMainWindow主界面
        /// </summary>
        public WpfMainWindow()
        {
            InitializeComponent();//初始化界面
            UnifiedUIManager.SetWpfInstance(this); // 注册到统一管理器
            LogManager.Instance.LogInfo("WPF实例已注册到UnifiedUIManager"); // 调试输出，确认注册成功
            NewTjLayer();//初始化图层
            Loaded += WpfMainWindow_Loaded;//加载按钮
            // 初始化预览图片缓存路径
            _previewCachePath = Path.Combine(AppPath, "PreviewCache");
            if (!Directory.Exists(_previewCachePath))
            {
                Directory.CreateDirectory(_previewCachePath);
            }
        }

        /// <summary>
        /// 窗口初始化时运行加载项
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void WpfMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取预览Viewbox的引用
                previewViewbox = FindVisualChild<Viewbox>(this, "预览Viewbox") ??
                                 FindVisualChild<Viewbox>(this, "Viewbox");
                // 直接通过名称查找TabControl
                var mainTabControl = FindVisualChild<TabControl>(this, "MainTabControl");
                if (mainTabControl != null)
                {
                    mainTabControl.SelectionChanged += TabControl_SelectionChanged; // 绑定TabControl事件
                    LogManager.Instance.LogInfo("TabControl事件绑定成功");//测试
                }
                else
                {
                    LogManager.Instance.LogWarning("未找到名称为MainTabControl的控件");
                }
                //初始化数据库 第一步
                ReinitializeDatabase();

                // 添加右键菜单到分类树
                if (CategoryTreeView != null)
                {
                    AddContextMenuToTreeView(CategoryTreeView);
                    LogManager.Instance.LogInfo("分类树右键菜单添加成功");
                }
                else
                {
                    LogManager.Instance.LogWarning("未找到CategoryTreeView控件");
                }

                // 查找PropertiesDataGrid控件
                PropertiesDataGrid = FindVisualChild<DataGrid>(this, "PropertiesDataGrid");
                Load();
                LogManager.Instance.LogInfo("=== WPF主窗口加载完成 ===");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogWarning($"窗口加载时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新初始化数据库连接
        /// </summary>
        private async void ReinitializeDatabase()
        {
            try
            {
                // 停止同步
                _serverSyncManager?.StopSync();

                // 获取新的连接字符串
                string newConnectionString = $"Server={_serverIP};Port={_serverPort};Database={_databaseName};Uid={_username};Pwd={_password};";

                // 更新连接字符串
                _connectionString = newConnectionString;

                // 重新初始化数据库管理器
                _databaseManager = new DatabaseManager(_connectionString);

                // 重新初始化文件管理器
                _fileManager = new FileManager(_databaseManager);

                // 重新初始化同步管理器
                _serverSyncManager = new ServerSyncManager(_databaseManager, _fileManager);

                // 如果启用自动同步，开始同步
                if (_autoSync)
                {
                    _serverSyncManager.StartSync(_syncInterval);
                }

                // 刷新分类树 第二步
                await RefreshCategoryTreeAsync();

                LogManager.Instance.LogInfo("数据库连接已重新初始化");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"重新初始化数据库时出错: {ex.Message}");
                MessageBox.Show($"重新初始化数据库失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加端口输入验证（可选）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_Set_ServicePort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 只允许输入数字
            e.Handled = !IsTextAllowed(e.Text);
        }

        /// <summary>
        /// 初始化分类属性编辑网格
        /// </summary>
        private void InitializeCategoryPropertyGrid()
        {
            var initialRows = new List<CategoryPropertyEditModel>
                {
                    new CategoryPropertyEditModel(),
                    new CategoryPropertyEditModel(),
                    new CategoryPropertyEditModel()
                };

            CategoryPropertiesDataGrid.ItemsSource = initialRows;
            LogManager.Instance.LogInfo("初始化分类属性编辑网格成功:InitializeCategoryPropertyGrid()");
        }

        /// <summary>
        /// 获取默认预览图片
        /// </summary>
        private BitmapImage GetDefaultPreviewImage()
        {
            try
            {
                // 首先尝试从资源加载默认图片
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/GB_NewCadPlus_III;component/Resources/default_preview.png");
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载默认预览图片失败: {ex.Message}");

                // 如果资源图片不存在，创建一个纯色图片
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("pack://application:,,,/GB_NewCadPlus_III;component/Resources/no_preview.png");
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    // 如果都失败了，创建一个空白图片
                    return CreatePlaceholderImage();
                }
            }
        }

        /// <summary>
        /// 创建占位符图片
        /// </summary>
        private BitmapImage CreatePlaceholderImage()
        {
            try
            {
                // 创建一个简单的占位符图片
                var bitmap = new RenderTargetBitmap(80, 60, 96, 96, PixelFormats.Pbgra32);

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // 绘制灰色背景
                    drawingContext.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Gray, 1), new System.Windows.Rect(0, 0, 80, 60));

                    // 绘制"No Preview"文本
                    var text = new FormattedText(
                        "无预览",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection,
                        new Typeface("Arial"),
                        12,
                        Brushes.Gray);

                    drawingContext.DrawText(text, new Point(20, 20));
                }

                bitmap.Render(drawingVisual);

                // 转换为BitmapImage
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;

                    var result = new BitmapImage();
                    result.BeginInit();
                    result.StreamSource = stream;
                    result.CacheOption = BitmapCacheOption.OnLoad;
                    result.EndInit();
                    result.Freeze();
                    return result;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"创建占位符图片失败: {ex.Message}");

                // 最后的备选方案：返回空的BitmapImage
                return new BitmapImage();
            }
        }

        /// <summary>
        /// 清理无效的图片缓存
        /// </summary>
        private void CleanupInvalidImageCache()
        {
            try
            {
                var invalidKeys = new List<string>();

                foreach (var kvp in _imageCache)
                {
                    try
                    {
                        // 检查图片是否仍然有效
                        if (kvp.Value == null || kvp.Value.Width <= 0 || kvp.Value.Height <= 0)
                        {
                            invalidKeys.Add(kvp.Key);
                        }
                    }
                    catch
                    {
                        invalidKeys.Add(kvp.Key);
                    }
                }

                // 移除无效的缓存项
                foreach (string key in invalidKeys)
                {
                    _imageCache.Remove(key);
                }

                LogManager.Instance.LogInfo($"清理了 {invalidKeys.Count} 个无效的图片缓存项");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"清理图片缓存时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从服务器获取预览图片并缓存
        /// </summary>
        private async Task<BitmapImage> GetPreviewImageAsync(FileStorage fileStorage)
        {
            try
            {
                // 检查文件存储对象是否有效
                if (fileStorage == null)
                {
                    LogManager.Instance.LogInfo("文件存储对象为空");
                    return GetDefaultPreviewImage();
                }

                // 检查内存缓存
                if (_imageCache.ContainsKey(fileStorage.FilePath ?? fileStorage.Id.ToString()))
                {
                    return _imageCache[fileStorage.FilePath ?? fileStorage.Id.ToString()];
                }

                // 检查是否有预览图片路径
                string previewImagePath = fileStorage.PreviewImagePath ?? fileStorage.FilePath;
                if (string.IsNullOrEmpty(previewImagePath))
                {
                    LogManager.Instance.LogInfo("预览图片路径为空");
                    return GetDefaultPreviewImage();
                }

                // 检查本地缓存文件
                string cacheFileName = $"{fileStorage.Id}_{Path.GetFileName(previewImagePath)}.png";
                string cacheFilePath = Path.Combine(_previewCachePath, cacheFileName);

                // 如果本地缓存存在且有效，直接加载
                if (File.Exists(cacheFilePath))
                {
                    try
                    {
                        var bitmap = LoadImageFromFile(cacheFilePath);
                        if (bitmap != null)
                        {
                            // 添加到内存缓存
                            _imageCache[fileStorage.FilePath ?? fileStorage.Id.ToString()] = bitmap;
                            return bitmap;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.LogInfo($"从本地缓存加载图片失败: {ex.Message}");
                        // 删除损坏的缓存文件
                        try { File.Delete(cacheFilePath); } catch { }
                    }
                }

                // 尝试从原始路径加载图片
                if (File.Exists(previewImagePath))
                {
                    try
                    {
                        var bitmap = LoadImageFromFile(previewImagePath);
                        if (bitmap != null)
                        {
                            // 保存到本地缓存
                            try
                            {
                                using (var fileStream = File.Create(cacheFilePath))
                                {
                                    var encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                                    encoder.Save(fileStream);
                                }
                            }
                            catch (Exception cacheEx)
                            {
                                LogManager.Instance.LogInfo($"保存到缓存失败: {cacheEx.Message}");
                            }

                            // 添加到内存缓存
                            _imageCache[fileStorage.FilePath ?? fileStorage.Id.ToString()] = bitmap;
                            return bitmap;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.LogInfo($"从原始路径加载图片失败: {ex.Message}");
                    }
                }

                // 如果所有方法都失败，返回默认图片
                return GetDefaultPreviewImage();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取预览图片时出错: {ex.Message}");
                return GetDefaultPreviewImage();
            }
        }

        /// <summary>
        /// 从文件加载图片（带错误处理）
        /// </summary>
        private BitmapImage LoadImageFromFile(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                    return null;

                // 获取文件扩展名
                string extension = Path.GetExtension(imagePath)?.ToLower();

                // 根据文件类型使用不同的加载方法
                switch (extension)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".bmp":
                    case ".gif":
                    case ".tif":
                    case ".tiff":
                        return LoadStandardImage(imagePath);
                    default:
                        // 尝试使用默认方式加载
                        return LoadStandardImage(imagePath);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载图片文件失败 {imagePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 加载标准图片格式
        /// </summary>
        private BitmapImage LoadStandardImage(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结以提高性能
                return bitmap;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载标准图片失败 {imagePath}: {ex.Message}");

                // 尝试使用流方式加载
                try
                {
                    using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
                catch (Exception streamEx)
                {
                    LogManager.Instance.LogInfo($"流方式加载图片也失败 {imagePath}: {streamEx.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// 在窗口关闭时清理缓存
        /// </summary>
        /// <param name="e"></param>
        protected void OnClosing(CancelEventArgs e)
        {
            try
            {
                // 停止同步
                _serverSyncManager?.StopSync();

                // 清理图片缓存
                CleanupInvalidImageCache();

                OnClosing(e);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"关闭窗口时清理缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加手动清理缓存按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 清理缓存按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                CleanupInvalidImageCache();
                MessageBox.Show("缓存已清理", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清理缓存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #region 图元tabItem

        /// <summary>
        /// TabControl选择改变事件
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                LogManager.Instance.LogInfo("TabControl选择改变事件触发");

                // 获取当前选中的TabItem
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
                {
                    string header = selectedTab.Header.ToString().Trim();
                    LogManager.Instance.LogInfo($"选中的TabItem: {header}");

                    // 处理主分类TabItem（工艺、建筑、结构等）
                    if (header == "工艺" || header == "建筑" || header == "结构" ||
                        header == "电气" || header == "给排水" || header == "暖通" ||
                        header == "自控" || header == "总图" || header == "公共图")
                    {
                        LogManager.Instance.LogInfo($"处理主分类TabItem: {header}");
                        LoadButtonsForMainCategoryTab(selectedTab, header);

                        // 特殊处理工艺分类
                        if (header == "工艺")
                        {
                            LoadConditionButtons();
                        }
                    }
                    // 处理嵌套的TabItem（图元集、图层管理等）
                    else if (header.Contains("图元集") || header.Contains("图层管理"))
                    {
                        LogManager.Instance.LogInfo($"处理嵌套TabItem: {header}");
                        TabItem parentTabItem = FindParentTabItem(selectedTab);
                        if (parentTabItem != null)
                        {
                            string parentHeader = parentTabItem.Header.ToString().Trim();
                            LogManager.Instance.LogInfo($"父级TabItem: {parentHeader}");
                            LoadButtonsForMainCategoryTab(parentTabItem, parentHeader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"处理TabControl选择改变时出错: {ex.Message}");
                LogManager.Instance.LogError($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 为指定的主分类TabItem加载按钮
        /// </summary>
        private void LoadButtonsForMainCategoryTab(TabItem tabItem, string categoryName)
        {
            try
            {
                LogManager.Instance.LogInfo($"开始为分类 {categoryName} 加载按钮");

                // 查找对应的面板
                WrapPanel panel = GetPanelByFolderName(categoryName);
                if (panel == null)
                {
                    LogManager.Instance.LogInfo($"未找到 {categoryName} 对应的面板");
                    return;
                }

                // 清空面板内容
                panel.Children.Clear();

                // 检查数据库是否可用
                if (_databaseManager != null && _databaseManager.IsDatabaseAvailable)
                {
                    LogManager.Instance.LogInfo($"使用数据库模式加载 {categoryName}");
                    LoadButtonsFromDatabaseForCategory(categoryName, panel);
                }
                else
                {
                    LogManager.Instance.LogInfo($"使用Resources文件夹模式加载 {categoryName}");
                    LoadButtonsFromResources(categoryName, panel);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"为分类 {categoryName} 加载按钮时出错: {ex.Message}");
                LogManager.Instance.LogInfo($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 从数据库为指定分类加载按钮
        /// </summary>
        private async void LoadButtonsFromDatabaseForCategory(string categoryName, WrapPanel panel)
        {
            try
            {
                LogManager.Instance.LogInfo($"=== 开始从数据库加载分类 {categoryName} ===");

                if (_databaseManager == null)
                {
                    LogManager.Instance.LogInfo("数据库管理器为空");
                    LoadButtonsFromResources(categoryName, panel);
                    return;
                }

                if (!_databaseManager.IsDatabaseAvailable)
                {
                    LogManager.Instance.LogInfo("数据库连接不可用");
                    LoadButtonsFromResources(categoryName, panel);
                    return;
                }

                // 获取主分类
                var category = await _databaseManager.GetCadCategoryByNameAsync(categoryName);
                if (category == null)
                {
                    LogManager.Instance.LogInfo($"数据库中未找到分类: {categoryName}");
                    LoadButtonsFromResources(categoryName, panel);
                    return;
                }

                LogManager.Instance.LogInfo($"找到主分类: {category.DisplayName} (ID: {category.Id})");

                // 获取子分类
                var subcategories = await _databaseManager.GetCadSubcategoriesByCategoryIdAsync(category.Id);
                LogManager.Instance.LogInfo($"找到 {subcategories.Count} 个子分类");

                // 清空面板
                panel.Children.Clear();

                if (subcategories.Count == 0)
                {
                    // 没有子分类，直接加载该分类下的文件
                    await LoadFilesDirectlyForCategory(category, panel);
                }
                else
                {
                    // 有子分类，按子分类组织文件
                    await LoadFilesBySubcategories(category, subcategories, panel);
                }

                LogManager.Instance.LogInfo($"=== 完成加载分类 {categoryName} ===");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"从数据库加载分类 {categoryName} 时出错: {ex.Message}");
                LogManager.Instance.LogInfo($"堆栈跟踪: {ex.StackTrace}");
                LoadButtonsFromResources(categoryName, panel);
            }
        }

        /// <summary>
        /// 直接为分类加载文件（无子分类情况）
        /// </summary>
        private async Task LoadFilesDirectlyForCategory(CadCategory category, WrapPanel panel)
        {
            try
            {
                LogManager.Instance.LogInfo($"直接加载分类 {category.DisplayName} 下的文件");

                // 获取该分类下的所有文件
                var files = await _databaseManager.GetFilesByCategoryIdAsync(category.Id, "main");
                LogManager.Instance.LogInfo($"在分类 {category.DisplayName} 中找到 {files.Count} 个文件");

                if (files.Count > 0)
                {
                    // 按显示名称排序
                    var sortedFiles = files.OrderBy(f => f.DisplayName).ToList();

                    // 创建文件显示区域
                    CreateFileButtonsForPanel(sortedFiles, panel, category.DisplayName);
                }
                else
                {
                    ShowNoFilesMessage(panel, "暂无文件");
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"直接加载分类文件时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 按子分类加载文件
        /// </summary>
        private async Task LoadFilesBySubcategories(CadCategory category,
            List<CadSubcategory> subcategories, WrapPanel panel)
        {
            try
            {
                LogManager.Instance.LogInfo($"按子分类加载分类 {category.DisplayName} 下的文件");

                // 定义背景色
                List<System.Windows.Media.Color> backgroundColors = new List<System.Windows.Media.Color>
                {
                    Colors.FloralWhite, Colors.Azure, Colors.FloralWhite, Colors.Azure
                };

                int colorIndex = 0;

                // 遍历子分类
                foreach (var subcategory in subcategories.OrderBy(s => s.SortOrder))
                {
                    LogManager.Instance.LogInfo($"处理子分类: {subcategory.DisplayName} (ID: {subcategory.Id})");

                    // 获取子分类下的文件
                    var files = await _databaseManager.GetFilesByCategoryIdAsync(subcategory.Id, "sub");
                    LogManager.Instance.LogInfo($"在子分类 {subcategory.DisplayName} 中找到 {files.Count} 个文件");

                    // 创建子分类区域
                    Border sectionBorder = CreateSubcategorySection(
                        subcategory.DisplayName,
                        backgroundColors[colorIndex % backgroundColors.Count]);

                    StackPanel sectionPanel = sectionBorder.Child as StackPanel;

                    if (files.Count > 0)
                    {
                        // 按显示名称排序
                        var sortedFiles = files.OrderBy(f => f.DisplayName).ToList();
                        CreateFileButtonsForPanel(sortedFiles, sectionPanel, subcategory.DisplayName);
                    }
                    else
                    {
                        ShowNoFilesMessage(sectionPanel, "暂无文件");
                    }

                    panel.Children.Add(sectionBorder);
                    colorIndex++;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"按子分类加载文件时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建子分类区域
        /// </summary>
        private Border CreateSubcategorySection(string title, System.Windows.Media.Color backgroundColor)
        {
            Border sectionBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 2, 0, 2),
                Width = 300,
                Background = new SolidColorBrush(backgroundColor),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };

            StackPanel sectionPanel = new StackPanel
            {
                Margin = new Thickness(3)
            };

            TextBlock sectionHeader = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2),
                Foreground = new SolidColorBrush(Colors.DarkBlue)
            };

            sectionPanel.Children.Add(sectionHeader);
            sectionBorder.Child = sectionPanel;

            return sectionBorder;
        }

        /// <summary>
        /// 找到分类文件后为面板创建文件按钮
        /// </summary>
        private void CreateFileButtonsForPanel(List<FileStorage> files, Panel targetPanel, string sectionName)
        {
            try
            {
                LogManager.Instance.LogInfo($"为 {sectionName} 创建 {files.Count} 个文件按钮");

                // 按3列分组
                int columns = 3;
                for (int i = 0; i < files.Count; i += columns)
                {
                    StackPanel rowPanel = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 2)
                    };

                    // 创建一行按钮（最多3个）
                    for (int j = 0; j < columns && (i + j) < files.Count; j++)
                    {
                        var file = files[i + j];
                        Button btn = CreateFileButton(file);
                        rowPanel.Children.Add(btn);
                    }

                    targetPanel.Children.Add(rowPanel);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"创建文件按钮时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建文件按钮
        /// </summary>
        private Button CreateFileButton(FileStorage file)
        {
            // 仅显示最后一个下划线后的名称，例如 "DQTJ_EQUIP_潮湿插座" -> "潮湿插座"
            string buttonText = file?.DisplayName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(buttonText))
            {
                int lastUnderscore = buttonText.LastIndexOf('_');
                if (lastUnderscore >= 0 && lastUnderscore + 1 < buttonText.Length)
                {
                    buttonText = buttonText.Substring(lastUnderscore + 1).Trim();
                }
                else
                {
                    buttonText = buttonText.Trim();
                }
            }
            Button btn = new Button
            {
                Content = buttonText,
                Width = 88,
                Height = 22,
                Margin = new Thickness(0, 0, 5, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Tag = new ButtonTagCommandInfo
                {
                    Type = "FileStorage",
                    ButtonName = buttonText,
                    fileStorage = file
                }
            };

            btn.Click += DynamicButton_Click;
            return btn;
        }

        /// <summary>
        /// 显示无文件消息
        /// </summary>
        private void ShowNoFilesMessage(Panel panel, string message)
        {
            TextBlock noFilesText = new TextBlock
            {
                Text = message,
                FontSize = 12,
                Margin = new Thickness(5, 0, 0, 3),
                Foreground = new SolidColorBrush(Colors.Gray)
            };
            panel.Children.Add(noFilesText);
        }

        /// <summary>
        /// 查找TabItem的父级TabItem
        /// </summary>
        /// <param Name="tabItem"></param>
        /// <returns></returns>
        private TabItem FindParentTabItem(TabItem tabItem)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(tabItem);//获取父级
            while (parent != null)
            {
                if (parent is TabItem parentTabItem)
                {
                    return parentTabItem;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// 通过文件夹名称获取对应的面板引用
        /// </summary>
        /// <param Name="folderName"></param>
        /// <returns></returns>
        private WrapPanel GetPanelByFolderName(string folderName)
        {
            LogManager.Instance.LogInfo($"查找面板: {folderName}");

            WrapPanel panel = null;

            switch (folderName)
            {
                case "公共图":
                    panel = PublicButtonsPanel;
                    break;
                case "工艺":
                    panel = CraftButtonsPanel;
                    break;
                case "建筑":
                    panel = ArchitectureButtonsPanel;
                    break;
                case "结构":
                    panel = StructureButtonsPanel;
                    break;
                case "电气":
                    panel = ElectricalButtonsPanel;
                    break;
                case "给排水":
                    panel = PlumbingButtonsPanel;
                    break;
                case "暖通":
                    panel = HVACButtonsPanel;
                    break;
                case "自控":
                    panel = ControlButtonsPanel;
                    break;
                case "总图":
                    panel = GeneralButtonsPanel;
                    break;
            }

            LogManager.Instance.LogInfo($"面板查找结果: {panel != null}");
            return panel;
        }

        /// <summary>
        /// 从数据库加载按钮（新方法）
        /// </summary>
        /// <param Name="folderName">分类名称</param>
        /// <param Name="panel">目标面板</param>
        private async Task LoadButtonsFromDatabase(string folderName, WrapPanel panel)
        {
            try
            {
                if (_databaseManager == null)
                {
                    LogManager.Instance.LogInfo("数据库管理器未初始化");
                    return;
                }

                LogManager.Instance.LogInfo($"开始从数据库加载分类 {folderName} 的按钮");

                // 从数据库获取主分类信息
                var category = await _databaseManager.GetCadCategoryByNameAsync(folderName);
                if (category == null)
                {
                    LogManager.Instance.LogInfo($"未找到分类: {folderName}");
                    return;
                }

                // 获取该分类下的所有子分类
                var subcategories = await _databaseManager.GetCadSubcategoriesByCategoryIdAsync(category.Id);
                LogManager.Instance.LogInfo($"找到 {subcategories.Count} 个子分类");

                // 定义背景色列表，用于区分不同区域
                List<System.Windows.Media.Color> backgroundColors = new List<System.Windows.Media.Color>
                {
                    Colors.FloralWhite,
                    Colors.Azure,
                    Colors.FloralWhite,
                    Colors.Azure,
                    Colors.FloralWhite,
                    Colors.Azure,
                    Colors.FloralWhite,
                };

                int colorIndex = 0;

                // 遍历所有子分类
                foreach (var subcategory in subcategories)
                {
                    LogManager.Instance.LogInfo($"处理子分类: {subcategory.DisplayName}");

                    // 为每个子分类创建一个带边框和背景色的区域
                    Border sectionBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Gray),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(5),
                        Margin = new Thickness(0, 2, 0, 2),
                        Width = 300,
                        Background = new SolidColorBrush(backgroundColors[colorIndex % backgroundColors.Count]),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    };

                    // 创建区域内容的StackPanel
                    StackPanel sectionPanel = new StackPanel
                    {
                        Margin = new Thickness(3)
                    };

                    // 添加区域标题
                    TextBlock sectionHeader = new TextBlock
                    {
                        Text = subcategory.DisplayName,
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 2),
                        Foreground = new SolidColorBrush(Colors.DarkBlue)
                    };
                    sectionPanel.Children.Add(sectionHeader);

                    // 从数据库获取该子分类下的所有图元文件
                    var graphics = await _databaseManager.GetFileStorageBySubcategoryIdAsync(subcategory.Id);
                    LogManager.Instance.LogInfo($"在 {subcategory.DisplayName} 中找到 {graphics.Count} 个图元文件");

                    if (graphics.Count > 0)
                    {
                        // 按显示名称排序
                        graphics.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));

                        // 按3列分组处理
                        int columns = 3;
                        for (int i = 0; i < graphics.Count; i += columns)
                        {
                            // 创建水平StackPanel用于放置一行按钮
                            StackPanel rowPanel = new StackPanel
                            {
                                Orientation = System.Windows.Controls.Orientation.Horizontal,
                                Margin = new Thickness(0, 0, 0, 2)
                            };

                            // 添加该行的按钮（最多3个）
                            for (int j = 0; j < columns && (i + j) < graphics.Count; j++)
                            {
                                var graphic = graphics[i + j];

                                // 检查是否是预定义的按钮
                                //var commandInfo = ButtonCommandMapper.GetCommandInfo(graphic.DisplayName);
                                string buttonName = graphic.DisplayName;
                                // 创建按钮
                                Button btn = new Button
                                {
                                    Content = graphic.DisplayName,
                                    Width = 88,
                                    Height = 22,
                                    Margin = new Thickness(0, 0, 5, 0),
                                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                                    Tag = graphic // 存储完整的图元信息
                                };

                                // 检查是否是预定义的按钮
                                if (UnifiedCommandManager.IsPredefinedCommand(buttonName))
                                {
                                    // 如果是预定义按钮
                                    btn.Tag = new ButtonTagCommandInfo
                                    {
                                        Type = "Predefined",
                                        ButtonName = buttonName,
                                        fileStorage = graphic
                                    };
                                    btn.Click += PredefinedButton_Click;
                                }
                                else
                                {
                                    // 如果是普通图元按钮，存储图元信息
                                    btn.Tag = new ButtonTagCommandInfo
                                    {
                                        Type = "FileStorage",
                                        ButtonName = buttonName,
                                        fileStorage = graphic
                                    };
                                    btn.Click += DynamicButton_Click;
                                }
                                // 添加按钮到行面板
                                rowPanel.Children.Add(btn);
                            }

                            // 添加行面板到区域面板
                            sectionPanel.Children.Add(rowPanel);
                        }
                    }
                    else
                    {
                        // 如果该子分类没有文件，显示提示信息
                        TextBlock noFilesText = new TextBlock
                        {
                            Text = "暂无文件",
                            FontSize = 12,
                            Margin = new Thickness(5, 0, 0, 3),
                            Foreground = new SolidColorBrush(Colors.Gray)
                        };
                        sectionPanel.Children.Add(noFilesText);
                    }

                    // 将区域面板添加到边框中
                    sectionBorder.Child = sectionPanel;

                    // 将边框添加到主面板
                    panel.Children.Add(sectionBorder);

                    // 切换到下一个背景色
                    colorIndex++;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"从数据库加载按钮时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从Resources文件夹加载按钮（支持命令映射）
        /// </summary>
        /// <param Name="folderName">文件夹名称</param>
        /// <param Name="panel">目标面板</param>
        private void LoadButtonsFromResources(string folderName, WrapPanel panel)
        {
            try
            {
                // 显示调试信息
                string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                LogManager.Instance.LogInfo($"应用程序路径: {appPath}");//显示调试信息没找到资源文件夹

                string resourcePath = System.IO.Path.Combine(appPath, "Resources", folderName);//返回本程序的资源文件夹路径；
                LogManager.Instance.LogInfo($"资源文件夹路径: {resourcePath}");//显示调试信息没找到资源文件夹
                LogManager.Instance.LogInfo($"资源文件夹是否存在: {System.IO.Directory.Exists(resourcePath)}");//显示调试信息没找到资源文件夹


                // 定义背景色列表，用于区分不同区域
                List<System.Windows.Media.Color> backgroundColors = new List<System.Windows.Media.Color>
                {
                    Colors.FloralWhite,
                    Colors.Azure,
                    Colors.FloralWhite,
                    Colors.Azure,
                    Colors.FloralWhite,
                    Colors.Azure,
                    Colors.FloralWhite,
                };

                int colorIndex = 0;

                // 检查一级文件夹是否存在
                if (System.IO.Directory.Exists(resourcePath))
                {
                    // 获取所有二级文件夹
                    string[] subDirectories = System.IO.Directory.GetDirectories(resourcePath);
                    LogManager.Instance.LogInfo($"找到 {subDirectories.Length} 个二级文件夹");

                    // 遍历所有二级文件夹
                    foreach (string subDir in subDirectories)
                    {
                        string subDirName = System.IO.Path.GetFileName(subDir);
                        LogManager.Instance.LogInfo($"处理二级文件夹: {subDirName}");

                        // 为每个二级文件夹创建一个带边框和背景色的区域
                        Border sectionBorder = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.Gray),//边框颜色
                            BorderThickness = new Thickness(1),//边框宽度
                            CornerRadius = new CornerRadius(5),//圆角
                            Margin = new Thickness(0, 2, 0, 3),//间隔
                            Width = 282,
                            Background = new SolidColorBrush(backgroundColors[colorIndex % backgroundColors.Count]),//背景色
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Left  // 左对齐
                        };

                        // 创建区域内容的StackPanel
                        StackPanel sectionPanel = new StackPanel
                        {
                            Margin = new Thickness(5)//间隔
                        };

                        // 添加区域标题
                        TextBlock sectionHeader = new TextBlock
                        {
                            Text = subDirName,
                            FontSize = 12,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 5, 0, 5),
                            Foreground = new SolidColorBrush(Colors.DarkBlue)
                        };
                        sectionPanel.Children.Add(sectionHeader);//区域标题

                        // 获取该二级文件夹下的所有dwg文件
                        string[] files = System.IO.Directory.GetFiles(subDir, "*.dwg");
                        LogManager.Instance.LogInfo($"在 {subDirName} 中找到 {files.Length} 个dwg文件");//显示文件数量

                        if (files.Length > 0) //创建行面板
                        {
                            // 过滤并处理文件名
                            var buttonInfoList = new List<Tuple<string, string>>(); // (按钮名称, 完整文件路径)
                                                                                    // 遍历所有dwg文件
                            foreach (string file in files)
                            {
                                //调试文件
                                LogManager.Instance.LogInfo($"处理文件: {file}");
                                // 获取不带扩展名的文件名
                                string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(file);
                                // 去除_前的字符，获取按钮显示名称
                                string buttonName = fileNameWithoutExt;
                                if (fileNameWithoutExt.Contains("_"))
                                {
                                    // 去除_前的字符
                                    buttonName = fileNameWithoutExt.Substring(fileNameWithoutExt.IndexOf("_") + 1);
                                }
                                // 只保留中文字符，去除所有符号与英文字母
                                string chineseOnlyName = ExtractChineseCharacters(buttonName);
                                // 如果提取后没有中文字符，则使用原名称
                                if (string.IsNullOrEmpty(chineseOnlyName))
                                {
                                    chineseOnlyName = buttonName;
                                }
                                // 添加到列表 (按钮名称, 完整文件路径)
                                buttonInfoList.Add(new Tuple<string, string>(chineseOnlyName, file));
                            }

                            // 按钮名称排序
                            buttonInfoList.Sort((x, y) => x.Item1.CompareTo(y.Item1));

                            // 按3列分组处理
                            int columns = 3;
                            for (int i = 0; i < buttonInfoList.Count; i += columns)//3列
                            {
                                // 创建水平StackPanel用于放置一行按钮
                                StackPanel rowPanel = new StackPanel
                                {
                                    Orientation = System.Windows.Controls.Orientation.Horizontal,//水平
                                    Margin = new Thickness(0, 0, 0, 5) // 每行底部间隔5
                                };

                                // 添加该行的按钮（最多3个）
                                for (int j = 0; j < columns && (i + j) < buttonInfoList.Count; j++)
                                {
                                    var buttonInfo = buttonInfoList[i + j];//按钮信息
                                    string buttonName = buttonInfo.Item1;//按钮名称
                                    string fullPath = buttonInfo.Item2;//完整文件路径

                                    Button btn = new Button
                                    {
                                        Content = buttonName,//按钮内容
                                        Width = 88,//按钮宽度
                                        Height = 20,//按钮高度
                                        FontSize = 12,
                                        FontFamily = new System.Windows.Media.FontFamily("微软雅黑"),
                                        Margin = new Thickness(0, 0, 3, 0), // 按钮右侧间隔5
                                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,//水平居左
                                        VerticalAlignment = System.Windows.VerticalAlignment.Top,//垂直居上
                                        Tag = fullPath // 将完整路径存储在Tag属性中
                                    };
                                    btn.FontWeight = FontWeights.Normal;
                                    // 检查是否是预定义的按钮
                                    if (UnifiedCommandManager.IsPredefinedCommand(buttonName))
                                    {
                                        // 如果是预定义按钮
                                        btn.Tag = new ButtonTagCommandInfo
                                        {
                                            Type = "Predefined",
                                            ButtonName = buttonName,
                                            FilePath = fullPath
                                        };
                                        btn.Click += PredefinedButton_Click;
                                    }
                                    else
                                    {
                                        // 如果是普通图元按钮，存储文件路径
                                        btn.Tag = new ButtonTagCommandInfo
                                        {
                                            Type = "File",
                                            ButtonName = buttonName,
                                            FilePath = fullPath
                                        };
                                    }

                                    // 添加按钮到行面板
                                    rowPanel.Children.Add(btn);
                                }
                                // 添加行面板到区域面板
                                sectionPanel.Children.Add(rowPanel);
                            }
                        }
                        else
                        {
                            // 如果该文件夹没有文件，显示提示信息
                            TextBlock noFilesText = new TextBlock
                            {
                                Text = "暂无文件",
                                FontSize = 12,
                                Margin = new Thickness(5, 0, 0, 5),
                                Foreground = new SolidColorBrush(Colors.Gray)
                            };
                            sectionPanel.Children.Add(noFilesText);
                        }

                        // 将区域面板添加到边框中
                        sectionBorder.Child = sectionPanel;

                        // 将边框添加到主面板
                        panel.Children.Add(sectionBorder);

                        // 切换到下一个背景色
                        colorIndex++;
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show($"找不到资源文件夹: {resourcePath}\n请检查Resources文件夹中的'{folderName}'文件夹是否存在");
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                System.Windows.MessageBox.Show($"加载按钮时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 加载条件图元按钮
        /// </summary>
        private async void LoadConditionButtons()
        {
            try
            {
                LogManager.Instance.LogInfo("开始加载条件图元按钮...");

                // 清空现有按钮
                ClearConditionButtons();

                // 加载各专业条件按钮
                await LoadSpecializedConditionButtons("电气", 电气条件按钮面板);
                await LoadSpecializedConditionButtons("给排水", 给排水条件按钮面板);
                await LoadSpecializedConditionButtons("自控", 自控条件按钮面板);
                await LoadSpecializedConditionButtons("建筑", 结构条件按钮面板);
                await LoadSpecializedConditionButtons("结构", 结构条件按钮面板);
                await LoadSpecializedConditionButtons("暖通", 暖通条件按钮面板);

                LogManager.Instance.LogInfo("条件图元按钮加载完成");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载条件图元按钮时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"加载条件图元按钮时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载指定专业的条件按钮
        /// </summary>
        private async Task LoadSpecializedConditionButtons(string 专业名称, WrapPanel targetPanel)
        {
            try
            {
                LogManager.Instance.LogInfo($"开始加载{专业名称}条件按钮...");

                if (targetPanel == null)
                {
                    LogManager.Instance.LogInfo($"目标面板 {专业名称} 为空");
                    return;
                }

                // 从数据库或资源文件夹中获取指定专业的条件文件
                var conditionFiles = await GetConditionFilesForSpecialty(专业名称);
                LogManager.Instance.LogInfo($"找到 {conditionFiles.Count} 个{专业名称}条件文件");

                if (conditionFiles.Count == 0)
                {
                    // 添加"暂无文件"提示
                    AddNoFilesLabel(targetPanel, $"暂无{专业名称}条件文件");
                    return;
                }

                // 按3列排列按钮
                int columns = 3;
                for (int i = 0; i < conditionFiles.Count; i += columns)
                {
                    StackPanel rowPanel = new StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    for (int j = 0; j < columns && (i + j) < conditionFiles.Count; j++)
                    {
                        var file = conditionFiles[i + j];
                        Button btn = CreateConditionButton(file);//创建条件按钮
                        rowPanel.Children.Add(btn);
                    }

                    targetPanel.Children.Add(rowPanel);
                }

                LogManager.Instance.LogInfo($"{专业名称}条件按钮加载完成");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载{专业名称}条件按钮时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取指定专业的条件文件
        /// </summary>
        private async Task<List<ConditionFileInfo>> GetConditionFilesForSpecialty(string specialtyName)
        {
            var conditionFiles = new List<ConditionFileInfo>();

            try
            {
                // 这里根据您的实际数据源来实现
                // 可以是从数据库、资源文件夹或其他地方获取

                // 示例实现（您需要根据实际情况修改）：
                if (_databaseManager != null)
                {
                    // 从数据库获取条件文件
                    // conditionFiles = await _databaseManager.GetConditionFilesBySpecialtyAsync(specialtyName);
                }
                else
                {
                    // 从资源文件夹获取条件文件
                    conditionFiles = GetConditionFilesFromResources(specialtyName);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取{specialtyName}条件文件时出错: {ex.Message}");
            }

            return conditionFiles;
        }

        /// <summary>
        /// 从资源文件夹获取条件文件
        /// </summary>
        private List<ConditionFileInfo> GetConditionFilesFromResources(string specialtyName)
        {
            var conditionFiles = new List<ConditionFileInfo>();

            try
            {
                // 根据专业名称确定资源路径
                string resourcePath = $"pack://application:,,,/Resources/Conditions/{specialtyName}/";

                // 这里需要根据您的实际资源结构来实现
                // 示例数据：
                switch (specialtyName)
                {
                    case "电气":
                        conditionFiles.AddRange(new[]
                        {
                    new ConditionFileInfo { Name = "电气条件1", DisplayName = "电气条件1", FilePath = $"{resourcePath}电气条件1.dwg" },
                    new ConditionFileInfo { Name = "电气条件2", DisplayName = "电气条件2", FilePath = $"{resourcePath}电气条件2.dwg" },
                    new ConditionFileInfo { Name = "电气条件3", DisplayName = "电气条件3", FilePath = $"{resourcePath}电气条件3.dwg" }
                });
                        break;

                    case "自控":
                        conditionFiles.AddRange(new[]
                        {
                    new ConditionFileInfo { Name = "自控条件1", DisplayName = "自控条件1", FilePath = $"{resourcePath}自控条件1.dwg" },
                    new ConditionFileInfo { Name = "自控条件2", DisplayName = "自控条件2", FilePath = $"{resourcePath}自控条件2.dwg" }
                });
                        break;

                    case "给排水":
                        conditionFiles.AddRange(new[]
                        {
                    new ConditionFileInfo { Name = "给排水条件1", DisplayName = "给排水条件1", FilePath = $"{resourcePath}给排水条件1.dwg" },
                    new ConditionFileInfo { Name = "给排水条件2", DisplayName = "给排水条件2", FilePath = $"{resourcePath}给排水条件2.dwg" },
                    new ConditionFileInfo { Name = "给排水条件3", DisplayName = "给排水条件3", FilePath = $"{resourcePath}给排水条件3.dwg" }
                });
                        break;

                    case "暖通":
                        conditionFiles.AddRange(new[]
                        {
                    new ConditionFileInfo { Name = "暖通条件1", DisplayName = "暖通条件1", FilePath = $"{resourcePath}暖通条件1.dwg" }
                });
                        break;

                    case "结构":
                        conditionFiles.AddRange(new[]
                        {
                    new ConditionFileInfo { Name = "结构条件1", DisplayName = "结构条件1", FilePath = $"{resourcePath}结构条件1.dwg" },
                    new ConditionFileInfo { Name = "结构条件2", DisplayName = "结构条件2", FilePath = $"{resourcePath}结构条件2.dwg" }
                });
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"从资源获取{specialtyName}条件文件时出错: {ex.Message}");
            }

            return conditionFiles;
        }

        /// <summary>
        /// 创建条件按钮
        /// </summary>
        private Button CreateConditionButton(ConditionFileInfo fileInfo)
        {
            // 仅显示最后一个下划线后的名称，例如 "DQTJ_EQUIP_潮湿插座" -> "潮湿插座"
            string buttonText = fileInfo?.DisplayName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(buttonText))
            {
                int lastUnderscore = buttonText.LastIndexOf('_');
                if (lastUnderscore >= 0 && lastUnderscore + 1 < buttonText.Length)
                {
                    buttonText = buttonText.Substring(lastUnderscore + 1).Trim();
                }
                else
                {
                    buttonText = buttonText.Trim();
                }
            }
            Button btn = new Button
            {
                Content = buttonText,
                Width = 85,
                Height = 20,
                Margin = new Thickness(5, 1, 1, 1),
                Tag = fileInfo, // 存储文件信息
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontWeight = FontWeights.Normal
            };

            // 应用统一的按钮样式
            btn.Style = (Style)FindResource("ButtonStyle"); // 如果您有自定义按钮样式

            // 添加点击事件
            btn.Click += ConditionButton_Click;

            return btn;
        }

        /// <summary>
        /// 条件按钮点击事件
        /// </summary>
        private void ConditionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is ConditionFileInfo fileInfo)
                {
                    LogManager.Instance.LogInfo($"点击条件按钮: {fileInfo.DisplayName}");

                    // 执行条件插入操作
                    ExecuteConditionInsert(fileInfo);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"执行条件插入时出错: {ex.Message}");
                MessageBox.Show($"执行条件插入时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 执行条件插入操作
        /// </summary>
        private void ExecuteConditionInsert(ConditionFileInfo fileInfo)
        {
            try
            {
                // 设置全局变量
                VariableDictionary.btnFileName = fileInfo.Name;
                VariableDictionary.btnBlockLayer = "TJ(条件图元)";
                VariableDictionary.layerColorIndex = 7; // 默认颜色

                // 执行插入命令
                Env.Document.SendStringToExecute("GB_InsertBlock ", false, false, false);

                LogManager.Instance.LogInfo($"成功插入条件: {fileInfo.DisplayName}");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"插入条件失败: {ex.Message}");
                MessageBox.Show($"插入条件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空条件按钮
        /// </summary>
        private void ClearConditionButtons()
        {
            if (电气条件按钮面板 != null) 电气条件按钮面板.Children.Clear();
            if (自控条件按钮面板 != null) 自控条件按钮面板.Children.Clear();
            if (给排水条件按钮面板 != null) 给排水条件按钮面板.Children.Clear();
            if (暖通条件按钮面板 != null) 暖通条件按钮面板.Children.Clear();
            if (结构条件按钮面板 != null) 结构条件按钮面板.Children.Clear();
        }

        /// <summary>
        /// 添加"暂无文件"提示
        /// </summary>
        private void AddNoFilesLabel(WrapPanel panel, string message)
        {
            TextBlock noFilesText = new TextBlock
            {
                Text = message,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(10, 5, 0, 5)
            };
            panel.Children.Add(noFilesText);
        }

        /// <summary>
        /// 条件文件信息类
        /// </summary>
        public class ConditionFileInfo
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string FilePath { get; set; }
            public string Specialty { get; set; } // 专业类别
            public DateTime CreatedTime { get; set; }
        }

        /// <summary>
        /// 提取字符串中的中文字符，去除所有符号与英文字母
        /// </summary>
        /// <param Name="input">输入字符串</param>
        /// <returns>只包含中文字符的字符串</returns>
        private string ExtractChineseCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder result = new StringBuilder();

            foreach (char c in input)
            {
                // 判断是否为中文字符（基本汉字范围）
                if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    result.Append(c);
                }
                // 也可以包含中文标点符号等扩展区域
                else if (c >= 0x3400 && c <= 0x4DBF) // 扩展A
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }


        /// <summary>
        /// 动态生成按钮的统一点击事件处理
        /// </summary>
        /// <param Name="sender">事件发送者</param>
        /// <param Name="e">路由事件参数</param>
        private void DynamicButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查发送者是否为按钮
                if (sender is Button btn)
                {
                    FileStorage fileStorage = null;

                    if (_useDatabaseMode && btn.Tag is ButtonTagCommandInfo tagInfo)
                    {
                        // 数据库模式：处理数据库图元
                        fileStorage = tagInfo.fileStorage;
                        LogManager.Instance.LogInfo($"点击了数据库图元按钮: {tagInfo.ButtonName}");

                        // 显示预览图片
                        ShowFilePreview(fileStorage);

                        // 显示文件详细属性（使用PropertiesDataGrid）
                        DisplayFilePropertiesInDataGridAsync(fileStorage);

                        // 执行原有操作
                        ExecuteDynamicButtonActionFromDatabase(fileStorage);
                    }
                    else if (!_useDatabaseMode && btn.Tag is string filePath)
                    {
                        // Resources模式：处理文件路径
                        LogManager.Instance.LogInfo($"点击了Resources图元按钮: {filePath}");

                        // 从文件路径提取按钮名称
                        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        string buttonName = fileNameWithoutExt;
                        if (fileNameWithoutExt.Contains("_"))
                        {
                            buttonName = fileNameWithoutExt.Substring(fileNameWithoutExt.IndexOf("_") + 1);
                        }

                        // 显示预览图片
                        ShowPreviewImage(filePath, buttonName);

                        // 清空属性显示
                        ClearFilePropertiesInDataGrid();

                        // 执行原有操作
                        ExecuteDynamicButtonActionFromResources(buttonName, filePath);
                    }
                    else if (btn.Tag is FileStorage directFileStorage)
                    {
                        // 直接的FileStorage对象
                        fileStorage = directFileStorage;
                        LogManager.Instance.LogInfo($"点击了文件按钮: {fileStorage.DisplayName}");

                        // 显示预览图片
                        ShowFilePreview(fileStorage);

                        // 显示文件详细属性（使用PropertiesDataGrid）
                        _ = DisplayFilePropertiesInDataGridAsync(fileStorage);

                        // 执行操作
                        ExecuteDynamicButtonActionFromDatabase(fileStorage);
                    }
                    else
                    {
                        LogManager.Instance.LogWarning("按钮点击事件处理失败：无法识别的数据类型");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"处理按钮点击事件时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"处理按钮点击事件时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 在DataGrid中显示文件属性（用于CAD图元界面）
        /// </summary>
        private async Task DisplayFilePropertiesInDataGridAsync(FileStorage fileStorage)
        {
            try
            {
                LogManager.Instance.LogInfo($"在PropertiesDataGrid中显示文件 {fileStorage.DisplayName} 的属性");

                if (PropertiesDataGrid == null)
                {
                    LogManager.Instance.LogWarning("PropertiesDataGrid控件为空");
                    return;
                }

                if (_databaseManager == null)
                {
                    LogManager.Instance.LogWarning("数据库管理器为空");
                    PropertiesDataGrid.ItemsSource = null;
                    return;
                }

                // 获取文件属性
                var fileAttribute = await _databaseManager.GetFileAttributeByGraphicIdAsync(fileStorage.Id);

                // 准备显示数据
                //var displayData = PrepareFileDisplayDataForDataGrid(fileStorage, fileAttribute);
                var displayData = PrepareFileDisplayData(fileStorage, fileAttribute);
                PropertiesDataGrid.ItemsSource = displayData;

                LogManager.Instance.LogInfo("文件属性在PropertiesDataGrid中显示完成");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"在PropertiesDataGrid中显示文件属性时出错: {ex.Message}");
                if (PropertiesDataGrid != null)
                {
                    PropertiesDataGrid.ItemsSource = null;
                }
                MessageBox.Show($"显示文件属性时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示文件预览图片
        /// </summary>
        private async void ShowFilePreview(FileStorage fileStorage)
        {
            try
            {
                if (预览 == null)
                {
                    LogManager.Instance.LogWarning("预览图片控件为空");
                    return;
                }

                // 清空现有预览
                预览.Source = null;

                if (fileStorage == null)
                {
                    LogManager.Instance.LogWarning("文件存储对象为空");
                    return;
                }

                LogManager.Instance.LogInfo($"显示文件预览: {fileStorage.DisplayName}");

                // 获取预览图片
                var previewImage = await GetPreviewImageAsync(fileStorage);

                if (previewImage != null)
                {
                    预览.Source = previewImage;
                    LogManager.Instance.LogInfo("预览图片显示成功");
                }
                else
                {
                    LogManager.Instance.LogWarning("无法加载预览图片");
                    // 显示默认图片或提示
                    预览.Source = GetDefaultPreviewImage();
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"显示文件预览时出错: {ex.Message}");
                // 显示错误图片
                预览.Source = GetDefaultPreviewImage();
            }
        }

        /// <summary>
        /// 显示文件详细信息和属性
        /// </summary>
        private async void ShowFileDetails(FileStorage fileStorage)
        {
            try
            {
                LogManager.Instance.LogInfo($"显示文件详细信息: {fileStorage.DisplayName}");

                if (fileStorage == null)
                {
                    LogManager.Instance.LogWarning("文件存储对象为空");
                    return;
                }

                // 显示基本信息（如果存在相应的控件）
                // 注意：这里需要在XAML中添加显示文件信息的控件
                // 暂时只处理属性显示

                // 显示属性信息
                await DisplayFilePropertiesAsync(fileStorage);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"显示文件详细信息时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示文件属性
        /// </summary>
        private async Task DisplayFilePropertiesAsync(FileStorage fileStorage)
        {
            try
            {
                LogManager.Instance.LogInfo($"显示文件属性: {fileStorage.DisplayName}");

                if (_databaseManager == null)
                {
                    LogManager.Instance.LogWarning("数据库管理器为空");
                    return;
                }

                // 获取文件属性
                var fileAttribute = await _databaseManager.GetFileAttributeByGraphicIdAsync(fileStorage.Id);

                // 准备显示数据
                var displayData = PrepareFileDisplayData(fileStorage, fileAttribute);

                // 更新显示（CAD图元界面）
                if (PropertiesDataGrid != null)
                {
                    PropertiesDataGrid.ItemsSource = displayData;
                }

                LogManager.Instance.LogInfo("文件属性显示完成");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"显示文件属性时出错: {ex.Message}");
            }
        }



        /// <summary>
        /// 执行Resources图元按钮点击后的操作
        /// </summary>
        /// <param Name="buttonName">按钮名称</param>
        /// <param Name="filePath">文件路径</param>
        private void ExecuteDynamicButtonActionFromResources(string buttonName, string filePath)
        {
            try
            {
                // 1. 显示预览图
                ShowPreviewImage(filePath, buttonName);

                // 2. 调用AutoCAD命令
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.SendStringToExecute($"DBTextLabel\n", true, false, false);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"执行Resources按钮操作时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"执行Resources按钮操作时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行数据库图元按钮点击后的操作
        /// </summary>
        /// <param Name="cadGraphic">图元信息</param>
        private async void ExecuteDynamicButtonActionFromDatabase(FileStorage fileStorage)
        {
            try
            {
                // 1. 显示预览图
                //ShowPreviewImageFromDatabase(fileStorage);

                // 2. 获取图元属性
                var graphicAttribute = await _databaseManager.GetFileAttributeByGraphicIdAsync(fileStorage.Id);

                // 3. 设置相关变量
                SetRelatedVariablesFromDatabase(fileStorage, graphicAttribute);

                // 4. 调用AutoCAD命令插入块
                InsertBlockToAutoCADFromDatabase(fileStorage, graphicAttribute);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"执行数据库按钮操作时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"执行数据库按钮操作时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从数据库信息显示预览图
        /// </summary>
        /// <param Name="cadBlock">图元信息</param>
        private void ShowPreviewImageFromDatabase(FileStorage fileStorage)
        {
            try
            {
                // 如果没有预览Viewbox，直接返回
                if (previewViewbox == null) return;

                // 清空现有的预览内容
                previewViewbox.Child = null;

                // 检查预览图路径是否存在
                if (!string.IsNullOrEmpty(fileStorage.PreviewImagePath) && System.IO.File.Exists(fileStorage.PreviewImagePath))
                {
                    // 创建Image控件显示预览图
                    System.Windows.Controls.Image previewImage = new System.Windows.Controls.Image
                    {
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(5)
                    };

                    // 创建BitmapImage并加载预览图文件
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fileStorage.PreviewImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // 设置图片源
                    previewImage.Source = bitmap;

                    // 将图片添加到Viewbox中
                    previewViewbox.Child = previewImage;
                }
                else
                {
                    // 如果没有找到预览图，显示提示文字
                    TextBlock noPreviewText = new TextBlock
                    {
                        Text = "无预览图",
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(Colors.Gray)
                    };

                    previewViewbox.Child = noPreviewText;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"显示预览图时出错: {ex.Message}");
            }
        }



        /// <summary>
        /// 添加文件时从数据库信息设置相关变量
        /// </summary>
        /// <param Name="cadBlock">图元信息</param>
        /// <param Name="blockAttribute">图元属性</param>
        private void SetRelatedVariablesFromDatabase(FileStorage fileStorage, FileAttribute cadGraphicAttribute)
        {
            try
            {
                // 根据图元属性设置变量
                if (cadGraphicAttribute != null)
                {
                    VariableDictionary.entityRotateAngle = (double)(cadGraphicAttribute.Angle ?? 0);
                    VariableDictionary.btnFileName = fileStorage.FileName;
                    VariableDictionary.btnBlockLayer = fileStorage.LayerName ?? "TJ()";
                    VariableDictionary.layerColorIndex = fileStorage.ColorIndex ?? 0;

                    // 设置其他属性
                    VariableDictionary.textbox_S_Width = cadGraphicAttribute.Width?.ToString();
                    VariableDictionary.textbox_S_Height = cadGraphicAttribute.Height?.ToString();
                    VariableDictionary.textBox_S_CirDiameter = (double?)cadGraphicAttribute.Length;
                }
                else
                {
                    // 默认值
                    VariableDictionary.entityRotateAngle = 0;
                    VariableDictionary.btnFileName = fileStorage.FileName;
                    VariableDictionary.btnBlockLayer = "TJ()";
                    VariableDictionary.layerColorIndex = 0;
                }

                LogManager.Instance.LogInfo($"已设置变量: btnFileName={VariableDictionary.btnFileName}, btnBlockLayer={VariableDictionary.btnBlockLayer}");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"设置变量时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从数据库信息在AutoCAD中插入块
        /// </summary>
        /// <param Name="cadBlock">图元信息</param>
        /// <param Name="blockAttribute">图元属性</param>
        private void InsertBlockToAutoCADFromDatabase(FileStorage fileStorage, FileAttribute cadGraphicAttribute)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    Editor ed = doc.Editor;

                    // 使用SendStringToExecute发送命令
                    string command = $"_INSERT_BLOCK \"{fileStorage.FilePath}\" \"{fileStorage.DisplayName}\"\n";
                    doc.SendStringToExecute(command, true, false, false);

                    LogManager.Instance.LogInfo($"已发送插入命令: {fileStorage.DisplayName}");
                }
                else
                {
                    LogManager.Instance.LogInfo("未找到活动的AutoCAD文档");
                    System.Windows.MessageBox.Show("未找到活动的AutoCAD文档");
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                LogManager.Instance.LogInfo($"AutoCAD命令执行错误: {ex.Message}");
                System.Windows.MessageBox.Show($"AutoCAD命令执行错误: {ex.Message}\n错误代码: {ex.ErrorStatus}");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"插入块时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"插入块时出错: {ex.Message}");
            }
        }


        /// <summary>
        /// 显示预览图片
        /// </summary>
        /// <param Name="dwgFilePath">dwg文件路径</param>
        /// <param Name="buttonName">按钮名称</param>
        private void ShowPreviewImage(string dwgFilePath, string buttonName)
        {
            try
            {
                // 如果没有预览Viewbox，直接返回
                if (previewViewbox == null) return;

                // 清空现有的预览内容
                previewViewbox.Child = null;

                // 获取文件所在的文件夹路径
                string folderPath = System.IO.Path.GetDirectoryName(dwgFilePath);

                // 构造png文件路径 (与dwg文件同名)
                string pngFilePath = System.IO.Path.Combine(folderPath,
                    System.IO.Path.GetFileNameWithoutExtension(dwgFilePath) + ".png");

                // 检查png文件是否存在
                if (System.IO.File.Exists(pngFilePath))
                {
                    // 创建Image控件显示预览图
                    System.Windows.Controls.Image previewImage = new System.Windows.Controls.Image
                    {
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(5)
                    };

                    // 创建BitmapImage并加载png文件
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(pngFilePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // 设置图片源
                    previewImage.Source = bitmap;

                    // 将图片添加到Viewbox中
                    previewViewbox.Child = previewImage;
                }
                else
                {
                    // 如果没有找到png文件，显示提示文字
                    TextBlock noPreviewText = new TextBlock
                    {
                        Text = "无预览图",
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(Colors.Gray)
                    };

                    previewViewbox.Child = noPreviewText;
                }
            }
            catch (Exception ex)
            {
                // 处理预览图加载异常
                System.Windows.MessageBox.Show($"加载预览图时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找可视化树中的子元素
        /// </summary>
        /// <typeparam Name="T">要查找的元素类型</typeparam>
        /// <param Name="parent">父元素</param>
        /// <param Name="childName">子元素名称</param>
        /// <returns>找到的子元素或null</returns>
        private T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // 遍历所有子元素
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 检查是否是目标类型
                if (child != null && child is T typedChild)
                {
                    // 检查名称是否匹配
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {
                        return typedChild;
                    }
                }

                // 递归查找子元素
                var childOfChild = FindVisualChild<T>(child, childName);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }

        /// <summary>
        /// 初始化条件图图层
        /// </summary>
        public static void NewTjLayer()
        {
            while (true)
            {
                foreach (var item in VariableDictionary.GGtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.GtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.AtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.StjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.PtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.NtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.EtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.ZKtjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                foreach (var item in VariableDictionary.tjtBtn)
                {
                    if (!VariableDictionary.allTjtLayer.Contains(item))
                        VariableDictionary.allTjtLayer.Add(item);
                }
                break;
            }
        }

        /// <summary>
        /// 读取本地设置路径下的配置文件
        /// </summary>
        private void Load()
        {
            string[]? lines = null;
            try
            {
                lines = System.IO.File.ReadAllLines(GetPath.referenceFile);//按每一行为一个DWG文件读进来； 
                GetPath.ListDwgFile.AddRange(lines);//把本程序下添加的文件都显示在列表里；
            }
            catch
            {
            }
        }

        /// <summary>
        /// 保存添加的图库文件与写入配置文件中
        /// </summary>
        public void Save()
        {
            try
            {
                using (var sr = new StreamWriter(GetPath.referenceFile)) //useing调用后主动释放文件
                {
                    foreach (var item in GetPath.ListDwgFile)
                    {
                        sr.WriteLine(item);
                    }
                }
            }
            catch (System.Exception)
            {
            }
        }

        #endregion

        #region 方向按钮事件处理方法...

        private void 上_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("上");
            command?.Invoke();
        }

        private void 右上_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("右上");
            command?.Invoke();
        }

        private void 右_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("右");
            command?.Invoke();
        }

        private void 右下_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("右下");
            command?.Invoke();

        }

        private void 下_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("下");
            command?.Invoke();
        }

        private void 左下_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("左下");
            command?.Invoke();
        }

        private void 左_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("左");
            command?.Invoke();
        }

        private void 左上_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("左上");
            command?.Invoke();
        }
        #endregion

        #region 功能区处理方法...

        private void 查找_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 功能1_Btn_Click(object sender, RoutedEventArgs e)
        {
            Env.Document.SendStringToExecute("pu ", false, false, false);
        }

        private void 功能2_Btn_Click(object sender, RoutedEventArgs e)
        {
            Env.Document.SendStringToExecute("audit\n y ", false, false, false);
        }

        private void 功能3_Btn_Click(object sender, RoutedEventArgs e)
        {
            Env.Document.SendStringToExecute("DRAWINGRECOVERY ", false, false, false);
        }
        #endregion

        #region CAD\SW 管理员数据库操作

        #region CAD\SW 分类树

        #region 架构树新方法

        /// <summary>
        /// 初始化架构树
        /// </summary>
        /// <returns></returns>
        private async Task InitializeCategoryTreeAsync()
        {
            try
            {
                await LoadCategoryTreeAsync();
                DisplayCategoryTree();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"初始化架构树失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载架构树数据
        /// </summary>
        /// <returns></returns>
        private async Task LoadCategoryTreeAsync()
        {
            try
            {
                _categoryTreeNodes.Clear();

                // 获取所有分类和子分类
                var categories = await _databaseManager.GetAllCadCategoriesAsync();
                var subcategories = await _databaseManager.GetAllCadSubcategoriesAsync();

                // 构建树结构
                BuildCategoryTree(categories, subcategories);

            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载架构树数据失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 构建分类树结构
        /// </summary>
        /// <param name="categories"></param>
        /// <param name="subcategories"></param>
        private void BuildCategoryTree(List<CadCategory> categories, List<CadSubcategory> subcategories)
        {
            // 清空现有节点
            _categoryTreeNodes.Clear();

            // 1. 创建主分类节点
            var mainCategoryNodes = categories
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryTreeNode(c.Id, c.Name, c.DisplayName, 0, 0, c))
                .ToList();

            // 2. 创建子分类节点字典，便于快速查找
            var subcategoryDict = subcategories
                .ToDictionary(s => s.Id, s => new CategoryTreeNode(
                    s.Id, s.Name, s.DisplayName, s.Level, s.ParentId, s));

            // 3. 构建父子关系
            BuildTreeRelationships(mainCategoryNodes, subcategoryDict);

            // 4. 将根节点添加到树节点列表
            _categoryTreeNodes.AddRange(mainCategoryNodes);

        }

        /// <summary>
        /// 构建树的父子关系
        /// </summary>
        /// <param name="mainNodes"></param>
        /// <param name="subcategoryDict"></param>
        private void BuildTreeRelationships(List<CategoryTreeNode> mainNodes, Dictionary<int, CategoryTreeNode> subcategoryDict)
        {
            // 创建所有节点的查找字典
            var allNodesDict = new Dictionary<int, CategoryTreeNode>();

            // 添加主分类节点
            foreach (var node in mainNodes)
            {
                allNodesDict[node.Id] = node;
            }

            // 添加子分类节点
            foreach (var kvp in subcategoryDict)
            {
                allNodesDict[kvp.Key] = kvp.Value;
            }

            // 建立父子关系
            foreach (var node in subcategoryDict.Values)
            {
                if (allNodesDict.ContainsKey(node.ParentId))
                {
                    var parentNode = allNodesDict[node.ParentId];
                    parentNode.Children.Add(node);
                }
                else if (node.Level == 1)
                {
                    // 二级子分类，父级是主分类
                    var mainCategoryNode = mainNodes.FirstOrDefault(n => n.Id == node.ParentId);
                    if (mainCategoryNode != null)
                    {
                        mainCategoryNode.Children.Add(node);
                    }
                }
            }

            // 对所有节点的子节点按排序序号排序
            foreach (var node in allNodesDict.Values)
            {
                node.Children = node.Children.OrderBy(child => GetSortOrder(child)).ToList();
            }
        }

        /// <summary>
        /// 获取节点的排序序号
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private int GetSortOrder(CategoryTreeNode node)
        {
            if (node.Data is CadCategory category)
                return category.SortOrder;
            else if (node.Data is CadSubcategory subcategory)
                return subcategory.SortOrder;
            return 0;
        }

        /// <summary>
        /// 显示架构树
        /// </summary>
        private void DisplayCategoryTree()
        {
            try
            {
                if (CategoryTreeView != null)
                {
                    CategoryTreeView.ItemsSource = null;
                    CategoryTreeView.ItemsSource = _categoryTreeNodes;
                    // 添加TreeView的选择事件
                    //CategoryTreeView.SelectedItemChanged += CategoryTreeView_SelectedItemChanged;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"显示架构树失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取TreeViewItem的辅助方法（增强版）
        /// </summary>
        /// <param name="container"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private TreeViewItem GetTreeViewItem(ItemsControl container, object item)
        {
            if (container == null) return null;

            // 首先尝试直接获取
            var directlyFound = container.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (directlyFound != null)
                return directlyFound;

            // 如果直接获取失败，遍历所有子项
            if (container.Items != null)
            {
                foreach (var containerItem in container.Items)
                {
                    var treeViewItem = container.ItemContainerGenerator.ContainerFromItem(containerItem) as TreeViewItem;
                    if (treeViewItem != null)
                    {
                        if (treeViewItem.DataContext == item)
                        {
                            return treeViewItem;
                        }

                        // 递归查找子项
                        var child = GetTreeViewItem(treeViewItem, item);
                        if (child != null)
                        {
                            return child;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 调整DataGrid行高以适应换行文本
        /// </summary>
        private void AdjustDataGridRowHeight()
        {
            try
            {
                if (StroageFileDataGrid != null)
                {
                    // 设置行高为自动调整
                    StroageFileDataGrid.RowHeight = Double.NaN; // 自动行高

                    // 或者设置一个最小行高
                    // StroageFileDataGrid.MinRowHeight = 60;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"调整DataGrid行高时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 在DataGrid数据源改变时调整行高
        /// </summary>
        private void StroageFileDataGrid_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            try
            {
                // 延迟调整行高，确保数据已加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AdjustDataGridRowHeight();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"DataGrid数据更新时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 在DataGrid加载完成后调整行高
        /// </summary>
        private void StroageFileDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                AdjustDataGridRowHeight();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"DataGrid加载时出错: {ex.Message}");
            }
        }


        /// <summary>
        /// 添加文件名处理方法
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private string FormatFileNameForDisplay(string fileName, int maxLength = 50)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            if (fileName.Length <= maxLength)
                return fileName;

            // 截断过长的文件名并添加省略号
            return fileName.Substring(0, maxLength - 3) + "...";
        }


        /// <summary>
        /// 递归展开所有子节点
        /// </summary>
        /// <param name="item"></param>
        private void ExpandAllChildren(TreeViewItem item)
        {
            if (item == null) return;

            item.IsExpanded = true;
            foreach (var child in item.Items)
            {
                var childItem = GetTreeViewItem(item, child);
                if (childItem != null)
                {
                    ExpandAllChildren(childItem);
                }
            }
        }

        /// <summary>
        /// 架构树选中项改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CategoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is CategoryTreeNode selectedNode)
                {
                    _selectedCategoryNode = selectedNode;
                    //LogManager.Instance.LogInfo($"选中分类节点: {selectedNode.DisplayText} (ID: {selectedNode.Id}, Level: {selectedNode.Level})");
                    LogManager.Instance.LogInfo($"选中分类节点: {selectedNode.DisplayText} (ID: {selectedNode.Id}, Level: {selectedNode.Level})");
                    // 根据选中的节点类型显示相应的属性编辑界面
                    DisplayNodePropertiesForEditing(selectedNode);

                    // 加载该分类下的文件
                    await LoadFilesForCategoryAsync(selectedNode);
                }
                else
                {
                    LogManager.Instance.LogInfo("选中的节点为空或类型不正确");
                    StroageFileDataGrid.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"处理架构树选中项改变失败: {ex.Message}");
                MessageBox.Show($"处理分类选择失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示节点属性用于编辑
        /// </summary>
        /// <param name="node"></param>
        private void DisplayNodePropertiesForEditing(CategoryTreeNode node)
        {
            try
            {
                var propertyRows = new List<CategoryPropertyEditModel>();

                if (node.Level == 0 && node.Data is CadCategory category)
                {
                    // 主分类
                    propertyRows.Add(new CategoryPropertyEditModel
                    {
                        PropertyName1 = "ID",
                        PropertyValue1 = category.Id.ToString(),
                        PropertyName2 = "名称",
                        PropertyValue2 = category.Name
                    });
                    propertyRows.Add(new CategoryPropertyEditModel
                    {
                        PropertyName1 = "显示名称",
                        PropertyValue1 = category.DisplayName,
                        PropertyName2 = "排序序号",
                        PropertyValue2 = category.SortOrder.ToString()
                    });
                    propertyRows.Add(new CategoryPropertyEditModel
                    {
                        PropertyName1 = "子分类数",
                        PropertyValue1 = GetSubcategoryCount(category).ToString(),
                        PropertyName2 = "",
                        PropertyValue2 = ""
                    });
                }
                else if (node.Data is CadSubcategory subcategory)
                {
                    // 子分类
                    propertyRows.Add(new CategoryPropertyEditModel
                    {
                        PropertyName1 = "ID",
                        PropertyValue1 = subcategory.Id.ToString(),
                        PropertyName2 = "父ID",
                        PropertyValue2 = subcategory.ParentId.ToString()
                    });
                    propertyRows.Add(new CategoryPropertyEditModel
                    {
                        PropertyName1 = "名称",
                        PropertyValue1 = subcategory.Name,
                        PropertyName2 = "显示名称",
                        PropertyValue2 = subcategory.DisplayName
                    });
                    propertyRows.Add(new CategoryPropertyEditModel
                    {
                        PropertyName1 = "排序序号",
                        PropertyValue1 = subcategory.SortOrder.ToString(),
                        PropertyName2 = "层级",
                        PropertyValue2 = subcategory.Level.ToString()
                    });
                    propertyRows.Add(new CategoryPropertyEditModel
                    {
                        PropertyName1 = "子分类数",
                        PropertyValue1 = GetSubcategoryCount(subcategory).ToString(),
                        PropertyName2 = "",
                        PropertyValue2 = ""
                    });
                }

                // 添加空行用于编辑
                propertyRows.Add(new CategoryPropertyEditModel());
                propertyRows.Add(new CategoryPropertyEditModel());

                CategoryPropertiesDataGrid.ItemsSource = propertyRows;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"显示节点属性失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取分类数量
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private int GetSubcategoryCount(CadCategory category)
        {
            if (string.IsNullOrEmpty(category.SubcategoryIds))
                return 0;

            return category.SubcategoryIds.Split(',').Length;
        }

        /// <summary>
        /// 获取子分类数量
        /// </summary>
        /// <param name="subcategory"></param>
        /// <returns></returns>
        private int GetSubcategoryCount(CadSubcategory subcategory)
        {
            if (string.IsNullOrEmpty(subcategory.SubcategoryIds))
                return 0;

            return subcategory.SubcategoryIds.Split(',').Length;
        }

        /// <summary>
        /// 初始化子分类属性编辑界面
        /// </summary>
        /// <param name="parentNode"></param>
        private void InitializeSubcategoryPropertiesForEditing(CategoryTreeNode parentNode)
        {
            var subcategoryProperties = new List<CategoryPropertyEditModel>
            {
                new CategoryPropertyEditModel { PropertyName1 = "父分类ID", PropertyValue1 = parentNode.Id.ToString(), PropertyName2 = "名称", PropertyValue2 = "" },
                new CategoryPropertyEditModel { PropertyName1 = "显示名称", PropertyValue1 = "", PropertyName2 = "排序序号", PropertyValue2 = "自动生成" } // 留空，表示自动生成
            };

            // 添加参考信息
            subcategoryProperties.Add(new CategoryPropertyEditModel
            {
                PropertyName1 = "父级名称",
                PropertyValue1 = parentNode.DisplayText,
                PropertyName2 = "",
                PropertyValue2 = ""
            });

            // 添加空行用于用户输入
            subcategoryProperties.Add(new CategoryPropertyEditModel());
            subcategoryProperties.Add(new CategoryPropertyEditModel());

            CategoryPropertiesDataGrid.ItemsSource = subcategoryProperties;
        }

        /// <summary>
        /// 初始化主分类属性编辑界面
        /// </summary>
        private void InitializeCategoryPropertiesForCategory()
        {
            var categoryProperties = new List<CategoryPropertyEditModel>
            {
                new CategoryPropertyEditModel { PropertyName1 = "名称", PropertyValue1 = "", PropertyName2 = "显示名称", PropertyValue2 = "" },
                new CategoryPropertyEditModel { PropertyName1 = "排序序号", PropertyValue1 = "自动生成", PropertyName2 = "", PropertyValue2 = "" } // 留空，表示自动生成
            };

            // 添加空行用于用户输入
            categoryProperties.Add(new CategoryPropertyEditModel());
            categoryProperties.Add(new CategoryPropertyEditModel());

            CategoryPropertiesDataGrid.ItemsSource = categoryProperties;
        }

        /// <summary>
        /// 应用分类属性（返回bool值）
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ApplyCategoryPropertiesAsync()
        {
            try
            {
                var properties = CategoryPropertiesDataGrid.ItemsSource as List<CategoryPropertyEditModel>;
                if (properties == null || properties.Count == 0)
                {
                    throw new Exception("没有可应用的属性");
                }

                // 解析属性数据
                var (name, displayName, sortOrder) = ParseCategoryProperties(properties);

                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception("分类名称不能为空");
                }

                // 自动生成排序序号（如果未提供或为0）
                if (sortOrder <= 0)
                {
                    sortOrder = await _databaseManager.GetMaxCadCategorySortOrderAsync() + 1;
                }

                // 生成主分类ID
                int categoryId = await CategoryIdGenerator.GenerateMainCategoryIdAsync(_databaseManager);

                // 创建分类对象
                var category = new CadCategory
                {
                    Id = categoryId,
                    Name = name,
                    DisplayName = displayName ?? name,
                    SortOrder = sortOrder,
                    SubcategoryIds = "", // 新建时为空
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 添加到数据库
                int result = await _databaseManager.AddCadCategoryAsync(category);

                // 验证数据库操作是否成功
                return result > 0;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"应用分类属性时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 应用子分类属性（返回bool值）
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ApplySubcategoryPropertiesAsync()
        {
            try
            {
                var properties = CategoryPropertiesDataGrid.ItemsSource as List<CategoryPropertyEditModel>;
                if (properties == null || properties.Count == 0)
                {
                    throw new Exception("没有可应用的属性");
                }

                // 解析属性数据
                var (parentId, name, displayName, sortOrder) = ParseSubcategoryProperties(properties);

                if (parentId <= 0)
                {
                    throw new Exception("父分类ID必须大于0");
                }

                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception("子分类名称不能为空");
                }

                // 自动生成排序序号（如果未提供或为0）
                if (sortOrder <= 0)
                {
                    sortOrder = await _databaseManager.GetMaxCadSubcategorySortOrderAsync(parentId) + 1;
                }

                // 生成子分类ID
                int subcategoryId = await CategoryIdGenerator.GenerateSubcategoryIdAsync(_databaseManager, parentId);

                // 确定层级
                int level = await DetermineCategoryLevelAsync(parentId);

                // 创建子分类对象
                var subcategory = new CadSubcategory
                {
                    Id = subcategoryId,
                    ParentId = parentId,
                    Name = name,
                    DisplayName = displayName ?? name,
                    SortOrder = sortOrder,
                    Level = level,
                    SubcategoryIds = "", // 新建时为空
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 添加到数据库
                int result = await _databaseManager.AddCadSubcategoryAsync(subcategory);

                if (result > 0)
                {
                    // 更新父级的子分类列表
                    await _databaseManager.UpdateParentSubcategoryListAsync(parentId, subcategoryId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"应用子分类属性时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确定分类层级
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        private async Task<int> DetermineCategoryLevelAsync(int parentId)
        {
            if (parentId < 10000)
            {
                // 父级是主分类（4位），这是二级子分类
                return 1;
            }
            else
            {
                // 父级是子分类，需要确定是几级子分类
                var parentSubcategory = await _databaseManager.GetCadSubcategoryByIdAsync(parentId);
                if (parentSubcategory != null)
                {
                    return parentSubcategory.Level + 1;
                }
                else
                {
                    // 默认为二级分类
                    return 1;
                }
            }
        }

        /// <summary>
        /// 解析分类属性数据
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private (string Name, string DisplayName, int SortOrder) ParseCategoryProperties(List<CategoryPropertyEditModel> properties)
        {
            string name = "";
            string displayName = "";
            int sortOrder = 0; // 默认排序序号

            foreach (var property in properties)
            {
                // 处理第一列
                ProcessCategoryProperty(property.PropertyName1, property.PropertyValue1, ref name, ref displayName, ref sortOrder);

                // 处理第二列
                ProcessCategoryProperty(property.PropertyName2, property.PropertyValue2, ref name, ref displayName, ref sortOrder);
            }

            return (name, displayName, sortOrder);
        }

        /// <summary>
        /// 处理单个分类属性
        /// </summary>
        /// <param name="propertyName">分类名</param>
        /// <param name="propertyValue">分类名对应的值</param>
        /// <param name="name">返回的名称</param>
        /// <param name="displayName">返回的显示名称</param>
        /// <param name="sortOrder">返回的排列顺序</param>
        private void ProcessCategoryProperty(string propertyName, string propertyValue, ref string name, ref string displayName, ref int sortOrder)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyValue))
                return;

            switch (propertyName.ToLower().Trim())
            {
                case "名称":
                case "name":
                    name = propertyValue.Trim();
                    break;
                case "显示名称":
                case "displayname":
                case "显示名":
                    displayName = propertyValue.Trim();
                    break;
                case "排序序号":
                case "sortorder":
                    // 排序序号现在是可选的，如果提供了就使用，否则自动生成
                    if (int.TryParse(propertyValue.Trim(), out int sort))
                        sortOrder = sort;
                    break;
            }
        }

        /// <summary>
        /// 解析子分类属性数据
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private (int ParentId, string Name, string DisplayName, int SortOrder) ParseSubcategoryProperties(List<CategoryPropertyEditModel> properties)
        {
            int parentId = 0;
            string name = "";
            string displayName = "";
            int sortOrder = 0; // 默认排序序号

            foreach (var property in properties)
            {
                // 处理第一列
                ProcessSubcategoryProperty(property.PropertyName1, property.PropertyValue1, ref parentId, ref name, ref displayName, ref sortOrder);

                // 处理第二列
                ProcessSubcategoryProperty(property.PropertyName2, property.PropertyValue2, ref parentId, ref name, ref displayName, ref sortOrder);
            }

            return (parentId, name, displayName, sortOrder);
        }

        /// <summary>
        /// 处理单个子分类属性
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <param name="parentId"></param>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        /// <param name="sortOrder"></param>
        private void ProcessSubcategoryProperty(string propertyName, string propertyValue, ref int parentId, ref string name, ref string displayName, ref int sortOrder)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyValue))
                return;

            switch (propertyName.ToLower().Trim())
            {
                case "父分类id":
                case "parentid":
                case "父id":
                    if (int.TryParse(propertyValue.Trim(), out int pid))
                        parentId = pid;
                    break;
                case "名称":
                case "name":
                    name = propertyValue.Trim();
                    break;
                case "显示名称":
                case "displayname":
                case "显示名":
                    displayName = propertyValue.Trim();
                    break;
                case "排序序号":
                case "sortorder":
                    // 排序序号现在是可选的
                    if (int.TryParse(propertyValue.Trim(), out int sort))
                        sortOrder = sort;
                    break;
            }
        }

        /// <summary>
        /// 加载分类下的文件
        /// </summary>
        /// <param name="categoryNode"></param>
        /// <returns></returns>
        private async Task LoadFilesForCategoryAsync(CategoryTreeNode categoryNode)
        {
            try
            {
                if (_databaseManager == null)
                {
                    LogManager.Instance.LogInfo("数据库管理器未初始化");
                    return;
                }

                List<FileStorage> files = new List<FileStorage>();

                //LogManager.Instance.LogInfo($"开始加载分类 {categoryNode.Id} ({categoryNode.DisplayText}) 的文件");
                LogManager.Instance.LogInfo($"开始加载分类 {categoryNode.Id} ({categoryNode.DisplayText}) 的文件");

                if (categoryNode.Level == 0 && categoryNode.Data is CadCategory category)
                {
                    // 主分类
                    LogManager.Instance.LogInfo($"加载主分类 {category.Name} (ID: {category.Id}) 的文件");
                    files = await _databaseManager.GetFilesByCategoryIdAsync(category.Id, "main");
                }
                else if (categoryNode.Data is CadSubcategory subcategory)
                {
                    // 子分类

                    LogManager.Instance.LogInfo($"加载子分类 {subcategory.Name} (ID: {subcategory.Id}) 的文件");
                    files = await _databaseManager.GetFilesByCategoryIdAsync(subcategory.Id, "sub");
                }
                else
                {
                    LogManager.Instance.LogInfo("未知的节点类型");
                    return;
                }

                LogManager.Instance.LogInfo($"从数据库查询到 {files.Count} 个文件");

                // 调试输出文件信息
                DebugFileData(files);

                // 确保在UI线程更新DataGrid
                Dispatcher.Invoke(() =>
                {
                    StroageFileDataGrid.ItemsSource = files;
                    LogManager.Instance.LogInfo($"DataGrid已更新，显示 {files.Count} 个文件");
                });

                // 如果没有文件，显示提示
                if (files.Count == 0)
                {
                    LogManager.Instance.LogInfo($"分类 '{categoryNode.DisplayText}' 下没有文件");
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载文件列表失败: {ex.Message}");
                LogManager.Instance.LogInfo($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 添加调试方法来检查加载的文件数据
        /// </summary>
        /// <param name="files"></param>
        private void DebugFileData(List<FileStorage> files)
        {
            LogManager.Instance.LogInfo($"=== 文件数据调试信息 ===");
            LogManager.Instance.LogInfo($"文件总数: {files.Count}");

            foreach (var file in files)
            {
                LogManager.Instance.LogInfo($"文件ID: {file.Id}");
                LogManager.Instance.LogInfo($"  名称: {file.DisplayName ?? file.FileName}");
                LogManager.Instance.LogInfo($"  路径: {file.FilePath}");
                LogManager.Instance.LogInfo($"  预览图: {file.PreviewImagePath}");
                LogManager.Instance.LogInfo($"  分类ID: {file.CategoryId}");
                LogManager.Instance.LogInfo($"  分类类型: {file.CategoryType}");
                LogManager.Instance.LogInfo("---");
            }
        }

        /// <summary>
        /// 文件列表双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StroageFileDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (StroageFileDataGrid.SelectedItem is FileStorage selectedFile)
                {
                    // 显示选中文件的详细信息
                    DisplayFileStorageInfo(selectedFile);

                    // 显示预览图片
                    var previewBitmap = await GetPreviewImageAsync(selectedFile);

                    System.Diagnostics.Debug.WriteLine($"选中文件: {selectedFile.DisplayName}\n文件ID: {selectedFile.Id}",
                        "文件信息", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理文件选择失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// 添加预览图片加载事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PreviewImage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var image = sender as Image;
                if (image?.Tag is FileStorage fileStorage)
                {
                    try
                    {
                        // 异步加载图片
                        var bitmap = await GetPreviewImageAsync(fileStorage);

                        // 在UI线程更新图片
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (image != null)
                            {
                                image.Source = bitmap;

                                // 隐藏加载文本
                                var parentGrid = image.Parent as Grid;
                                if (parentGrid != null)
                                {
                                    var loadingText = parentGrid.Children.OfType<TextBlock>().FirstOrDefault();
                                    if (loadingText != null)
                                    {
                                        loadingText.Visibility = Visibility;
                                    }
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.LogInfo($"设置图片源时出错: {ex.Message}");

                        // 显示错误信息
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (image != null)
                            {
                                image.Source = GetDefaultPreviewImage();

                                // 显示错误文本
                                var parentGrid = image.Parent as Grid;
                                if (parentGrid != null)
                                {
                                    var loadingText = parentGrid.Children.OfType<TextBlock>().FirstOrDefault();
                                    if (loadingText != null)
                                    {
                                        loadingText.Text = "加载失败";
                                        loadingText.Foreground = new SolidColorBrush(Colors.Red);
                                    }
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"预览图片加载事件处理失败: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 架构树节点类
        /// </summary>
        public class CategoryTreeNode
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public int Level { get; set; } // 0=主分类, 1=二级子分类, 2=三级子分类...
            public int ParentId { get; set; }
            public object Data { get; set; } // 存储原始数据对象
            public List<CategoryTreeNode> Children { get; set; } = new List<CategoryTreeNode>();
            public string DisplayText { get; set; }
            //public string DisplayText => string.IsNullOrEmpty(DisplayName) ? Name : DisplayName;

            public CategoryTreeNode(int id, string name, string displayName, int level, int parentId, object data)
            {
                Id = id;
                Name = name;
                DisplayText = displayName;
                Level = level;
                ParentId = parentId;
                Data = data;
                Children = new List<CategoryTreeNode>();
            }
        }

        /// <summary>
        /// 通过Row索引查找Grid
        /// </summary>
        private Grid FindGridByRow(DependencyObject parent, int targetRow)
        {
            LogManager.Instance.LogInfo($"开始查找Row={targetRow}的Grid");

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 检查是否是Grid且有Grid.Row属性
                if (child is Grid grid)
                {
                    var row = Grid.GetRow(grid);
                    LogManager.Instance.LogInfo($"找到Grid，Row={row}");
                    if (row == targetRow)
                    {
                        LogManager.Instance.LogInfo($"找到目标Grid，Row={targetRow}");
                        return grid;
                    }
                }

                // 递归查找子元素
                var result = FindGridByRow(child, targetRow);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 加载CAD子分类（递归加载多级子分类）
        /// </summary>
        private async Task LoadCadSubcategoriesAsync(int parentId, TreeViewItem parentItem, int level)
        {
            try
            {
                var subcategories = await _databaseManager.GetCadSubcategoriesByParentIdAsync(parentId);// 获取指定父级ID的子分类
                foreach (var subcategory in subcategories)// 遍历子分类
                {
                    // 创建子分类节点
                    string indent = new string(' ', level * 2); // 根据层级添加缩进
                    TreeViewItem subcategoryItem = new TreeViewItem// 创建子分类节点
                    {
                        Header = $"{indent}{subcategory.DisplayName}",// 显示子分类名称
                        Tag = new { Type = "Subcategory", Id = subcategory.Id, Object = subcategory }// 设置Tag属性
                    };
                    await LoadCadGraphicsAsync(subcategory.Id, subcategoryItem); // 加载图元
                    await LoadCadSubcategoriesAsync(subcategory.Id, subcategoryItem, level + 1); // 递归加载子子分类
                    parentItem.Items.Add(subcategoryItem);// 添加子分类节点到父分类节点
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载CAD子分类时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载CAD图元
        /// </summary>
        private async Task LoadCadGraphicsAsync(int subcategoryId, TreeViewItem parentItem)
        {
            try
            {
                var files = await _databaseManager.GetFileStorageBySubcategoryIdAsync(subcategoryId);
                foreach (var file in files)
                {
                    TreeViewItem fileItem = new TreeViewItem
                    {
                        Header = $"    {file.LayerName}",
                        Tag = new { Type = "Graphic", Id = file.Id, Object = file }
                    };
                    parentItem.Items.Add(fileItem);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载CAD图元时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载SW子分类（递归加载多级子分类）
        /// </summary>
        private async Task LoadSwSubcategoriesAsync(int parentId, TreeViewItem parentItem, int level)
        {
            try
            {
                var subcategories = await _databaseManager.GetSwSubcategoriesByParentIdAsync(parentId);
                foreach (var subcategory in subcategories)
                {
                    // 创建子分类节点
                    string indent = new string(' ', level * 2); // 根据层级添加缩进
                    TreeViewItem subcategoryItem = new TreeViewItem
                    {
                        Header = $"{indent}{subcategory.DisplayName}",
                        Tag = new { Type = "Subcategory", Id = subcategory.Id, Object = subcategory }
                    };

                    // 加载图元
                    await LoadSwGraphicsAsync(subcategory.Id, subcategoryItem);

                    // 递归加载子子分类
                    await LoadSwSubcategoriesAsync(subcategory.Id, subcategoryItem, level + 1);

                    parentItem.Items.Add(subcategoryItem);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载SW子分类时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载SW图元
        /// </summary>
        private async Task LoadSwGraphicsAsync(int subcategoryId, TreeViewItem parentItem)
        {
            try
            {
                var graphics = await _databaseManager.GetSwGraphicsBySubcategoryIdAsync(subcategoryId);
                foreach (var graphic in graphics)
                {
                    TreeViewItem graphicItem = new TreeViewItem
                    {
                        Header = $"    {graphic.FileName}",
                        Tag = new { Type = "Graphic", Id = graphic.Id, Object = graphic }
                    };
                    parentItem.Items.Add(graphicItem);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"加载SW图元时出错: {ex.Message}");
            }
        }


        #endregion

        #region 树节点选中与右键操作


        /// <summary>
        /// 为TreeView添加右键菜单
        /// </summary>
        private void AddContextMenuToTreeView(System.Windows.Controls.TreeView treeView)
        {
            try
            {
                var contextMenu = new System.Windows.Controls.ContextMenu();

                // 新建分类菜单项
                var newItem = new System.Windows.Controls.MenuItem { Header = "新建分类" };
                newItem.Click += NewCategory_MenuItem_Click;
                contextMenu.Items.Add(newItem);

                // 添加子分类菜单项
                var addSubItem = new System.Windows.Controls.MenuItem { Header = "添加子分类" };
                addSubItem.Click += AddSubcategory_MenuItem_Click;
                contextMenu.Items.Add(addSubItem);

                // 修改菜单项
                var editItem = new System.Windows.Controls.MenuItem { Header = "修改" };
                editItem.Click += Edit_MenuItem_Click;
                contextMenu.Items.Add(editItem);

                // 删除菜单项
                var deleteItem = new System.Windows.Controls.MenuItem { Header = "删除" };
                deleteItem.Click += Delete_MenuItem_Click;
                contextMenu.Items.Add(deleteItem);

                // 刷新菜单项
                var RefreshItem = new System.Windows.Controls.MenuItem { Header = "刷新" };
                deleteItem.Click += 刷新文件列表按钮_Click;
                contextMenu.Items.Add(RefreshItem);

                treeView.ContextMenu = contextMenu;

                LogManager.Instance.LogInfo("右键菜单添加成功");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"添加右键菜单时出错: {ex.Message}");
                MessageBox.Show($"添加右键菜单失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除主分类
        /// </summary>
        /// <param name="categoryNode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> DeleteMainCategoryAsync(CategoryTreeNode categoryNode)
        {
            try
            {
                // 检查是否有子分类
                if (categoryNode.Children.Count > 0)
                {
                    if (MessageBox.Show("该主分类下还有子分类，确定要全部删除吗？",
                                      "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return false;
                    }
                }

                // 从数据库删除主分类
                int result = await _databaseManager.DeleteCadCategoryAsync(categoryNode.Id);
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"删除主分类失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除子分类
        /// </summary>
        /// <param name="subcategoryNode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> DeleteSubcategoryAsync(CategoryTreeNode subcategoryNode)
        {
            try
            {
                // 检查是否有子分类
                if (subcategoryNode.Children.Count > 0)
                {
                    if (MessageBox.Show("该子分类下还有子分类，确定要全部删除吗？",
                                      "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    {
                        return false;
                    }
                }

                // 从数据库删除子分类
                int result = await _databaseManager.DeleteCadSubcategoryAsync(subcategoryNode.Id);
                if (result > 0)
                {
                    // 更新父级的子分类列表
                    await UpdateParentSubcategoryListAfterDeleteAsync(subcategoryNode.ParentId, subcategoryNode.Id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"删除子分类失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除后更新父级子分类列表
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="deletedSubcategoryId"></param>
        /// <returns></returns>
        private async Task UpdateParentSubcategoryListAfterDeleteAsync(int parentId, int deletedSubcategoryId)
        {
            try
            {
                // 获取父级记录
                string currentSubcategoryIds = "";
                if (parentId >= 10000)
                {
                    // 父级是子分类
                    var parentSubcategory = await _databaseManager.GetCadSubcategoryByIdAsync(parentId);
                    currentSubcategoryIds = parentSubcategory?.SubcategoryIds ?? "";
                }
                else
                {
                    // 父级是主分类
                    var categories = await _databaseManager.GetAllCadCategoriesAsync();
                    var parentCategory = categories.FirstOrDefault(c => c.Id == parentId);
                    currentSubcategoryIds = parentCategory?.SubcategoryIds ?? "";
                }

                // 更新子分类列表（移除已删除的ID）
                if (!string.IsNullOrEmpty(currentSubcategoryIds))
                {
                    var ids = currentSubcategoryIds.Split(',').Select(id => id.Trim()).ToList();
                    ids.Remove(deletedSubcategoryId.ToString());
                    string newSubcategoryIds = string.Join(",", ids);

                    // 更新数据库
                    await _databaseManager.UpdateParentSubcategoryListAsync(parentId, newSubcategoryIds);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新父级子分类列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新分类属性（编辑功能）
        /// </summary>
        /// <returns></returns>
        private async Task<bool> UpdateCategoryPropertiesAsync()
        {
            try
            {
                if (_selectedCategoryNode == null)
                    throw new Exception("没有选中的分类");

                var properties = CategoryPropertiesDataGrid.ItemsSource as List<CategoryPropertyEditModel>;
                if (properties == null || properties.Count == 0)
                    throw new Exception("没有可更新的属性");

                // 解析更新的属性
                var (name, displayName, sortOrder) = ParseUpdatedCategoryProperties(properties);

                if (string.IsNullOrEmpty(name))
                    throw new Exception("分类名称不能为空");

                // 根据节点类型更新相应的记录
                if (_selectedCategoryNode.Level == 0 && _selectedCategoryNode.Data is CadCategory category)
                {
                    // 更新主分类
                    category.Name = name;
                    category.DisplayName = displayName ?? name;
                    category.SortOrder = sortOrder;
                    category.UpdatedAt = DateTime.Now;

                    int result = await _databaseManager.UpdateCadCategoryAsync(category);
                    return result > 0;
                }
                else if (_selectedCategoryNode.Data is CadSubcategory subcategory)
                {
                    // 更新子分类
                    subcategory.Name = name;
                    subcategory.DisplayName = displayName ?? name;
                    subcategory.SortOrder = sortOrder;
                    subcategory.UpdatedAt = DateTime.Now;

                    int result = await _databaseManager.UpdateCadSubcategoryAsync(subcategory);
                    return result > 0;
                }
                else
                {
                    throw new Exception("不支持的分类类型");
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新分类属性时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 解析更新的分类属性
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private (string Name, string DisplayName, int SortOrder) ParseUpdatedCategoryProperties(List<CategoryPropertyEditModel> properties)
        {
            string name = "";
            string displayName = "";
            int sortOrder = 0;

            foreach (var property in properties)
            {
                ProcessCategoryProperty(property.PropertyName1, property.PropertyValue1, ref name, ref displayName, ref sortOrder);
                ProcessCategoryProperty(property.PropertyName2, property.PropertyValue2, ref name, ref displayName, ref sortOrder);
            }

            return (name, displayName, sortOrder);
        }

        /// <summary>
        /// 添加手动刷新文件列表的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void 刷新文件列表按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCategoryNode != null)
                {
                    LogManager.Instance.LogInfo("手动刷新文件列表");
                    await LoadFilesForCategoryAsync(_selectedCategoryNode);
                    MessageBox.Show("文件列表已刷新", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("请先选择一个分类", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新文件列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        #endregion

        #region 按键点击操作

        /// <summary>
        /// 按钮信息类，用于管理按钮与命令的关联
        /// </summary>
        public class ButtonTagCommandInfo
        {
            /// <summary>
            /// cad图元
            /// </summary>
            public FileStorage fileStorage { get; set; }
            /// <summary>
            /// 命令信息
            /// </summary>
            public ButtonTagCommandInfo CommandInfo { get; set; }
            /// <summary>
            /// 按钮类型
            /// </summary>
            public string Type { get; set; }
            /// <summary>
            /// 按钮名称
            /// </summary>
            public string ButtonName { get; set; }
            /// <summary>
            /// 输入名称
            /// </summary>
            public string BtnInputText { get; set; }
            /// <summary>
            /// 旋转角度
            /// </summary>
            public double RotateAngle { get; set; }
            /// <summary>
            /// 层颜色索引
            /// </summary>
            public int LayerColorIndex { get; set; }
            /// <summary>
            /// 文件名（不包含路径和扩展名）
            /// </summary>
            public string FileName { get; set; }
            /// <summary>
            /// 块名称（从文件名中提取）
            /// </summary>
            public string BlockName { get; set; }
            /// <summary>
            /// 文件完整路径
            /// </summary>
            public string FilePath { get; set; }
            /// <summary>
            /// 对应的Command方法名
            /// </summary>
            public string CommandMethodName { get; set; }
            /// <summary>
            /// 按钮显示文本
            /// </summary>
            public string DisplayText { get; set; }
            /// <summary>
            /// 按钮类型（自定义按钮、工艺按钮等）
            /// </summary>
            public string ButtonType { get; set; }
            /// <summary>
            /// 相关参数
            /// </summary>
            public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
            /// <summary>
            /// 所属分类（二级文件夹名称）
            /// </summary>
            public string Category { get; set; }
            /// <summary>
            /// 其他Object对象
            /// </summary>
            public object Object { get; set; } // 用于存储其他对象
        }

        /// <summary>
        /// 预定义按钮点击事件处理
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void PredefinedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is ButtonTagCommandInfo tagInfo)
                {
                    LogManager.Instance.LogInfo($"点击预定义按钮: {tagInfo.ButtonName}");

                    // 通过统一管理器获取并执行对应的命令
                    var command = UnifiedCommandManager.GetCommand(tagInfo.ButtonName);
                    if (command != null)
                    {
                        try
                        {
                            command.Invoke();
                            LogManager.Instance.LogInfo($"成功执行按钮命令: {tagInfo.ButtonName}");
                        }
                        catch (Exception invokeEx)
                        {
                            LogManager.Instance.LogInfo($"执行按钮命令时出错: {invokeEx.Message}");
                            System.Windows.MessageBox.Show($"执行命令 '{tagInfo.ButtonName}' 时出错: {invokeEx.Message}");
                        }
                    }
                    else
                    {
                        LogManager.Instance.LogInfo($"未找到按钮 '{tagInfo.ButtonName}' 对应的命令");
                        System.Windows.MessageBox.Show($"未找到按钮 '{tagInfo.ButtonName}' 对应的命令");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"处理预定义按钮点击事件时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"处理按钮点击事件时出错: {ex.Message}");
            }
        }

        #region 共用图按键处理方法...

        private void 共用条件_Btn_Click(object sender, RoutedEventArgs e)
        {
            //MigrateResourcesToDatabaseAsync();//数据迁移
        }

        private void 所有条件开关_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 设备开关_Btn_Click(object sender, RoutedEventArgs e)
        {

        }


        #endregion

        #region 工艺按键处理方法...
        private void 纯化水_Btn_Clic(object sender, RoutedEventArgs e)
        {
            /// 获取按钮的命令
            var command = UnifiedCommandManager.GetCommand("纯化水");
            command?.Invoke();//执行命令
        }

        private void 纯蒸汽_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("纯蒸汽");
            command?.Invoke();
        }

        private void 注射用水_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("注射用水");
            command?.Invoke();
        }

        private void 凝结回水_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("凝结回水");
            command?.Invoke();
        }

        private void 氧气_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("氧气");
            command?.Invoke();
        }

        private void 氮气_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("氮气");
            command?.Invoke();
        }

        private void 二氧化碳_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("二氧化碳");
            command?.Invoke();
        }

        private void 无菌压缩空气_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("无菌压缩空气");
            command?.Invoke();
        }

        private void 仪表压缩空气_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("仪表压缩空气");
            command?.Invoke();
        }

        private void 低压蒸汽_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("低压蒸汽");
            command?.Invoke();
        }

        private void 低温循环上水_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("低温循环上水");
            command?.Invoke();
        }

        private void 常温循环上水_Btn_Click(object sender, RoutedEventArgs e)
        {

            var command = UnifiedCommandManager.GetCommand("常温循环上水");
            command?.Invoke();
        }

        private void 设备表导入_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 设备表导出_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 区域开关_Btn_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region 管理员按键处理方法...

        /// <summary>
        /// 加载CAD数据库按钮点击事件
        /// </summary>
        private async void LoadCadDatabase_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 设置当前数据库类型
                _currentDatabaseType = "CAD";
                LogManager.Instance.LogInfo("设置数据库类型为: " + _currentDatabaseType);
                LogManager.Instance.LogInfo("=== 开始加载CAD数据库 ===");
                if (!_useDatabaseMode || _databaseManager == null || !_databaseManager.IsDatabaseAvailable)
                {
                    System.Windows.MessageBox.Show("数据库不可用，请检查数据库连接配置");
                    return;
                }
                _cadStoragePath = await _databaseManager.GetConfigValueAsync("cad_storage_path");  // 获取CAD存储路径
                if (string.IsNullOrEmpty(_cadStoragePath))
                {
                    _cadStoragePath = System.IO.Path.Combine(AppPath, "CadFiles");
                }
                System.IO.Directory.CreateDirectory(_cadStoragePath); // 确保存储路径存在

                // 加载并显示CAD分类树
                await InitializeCategoryTreeAsync();

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载CAD数据库时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载SW数据库按钮点击事件
        /// </summary>
        private async void LoadSwDatabase_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_databaseManager == null)
                {
                    System.Windows.MessageBox.Show("数据库未初始化");
                    return;
                }

                // 设置当前数据库类型
                _currentDatabaseType = "SW";

                // 获取SW存储路径
                _swStoragePath = await _databaseManager.GetConfigValueAsync("sw_storage_path");
                if (string.IsNullOrEmpty(_swStoragePath))
                {
                    _swStoragePath = System.IO.Path.Combine(AppPath, "SwFiles");
                }

                // 确保存储路径存在
                System.IO.Directory.CreateDirectory(_swStoragePath);

                // 加载并显示SW分类树
                //await LoadAndDisplayCategoryTreeAsync();

                System.Windows.MessageBox.Show("SW数据库加载成功");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载SW数据库时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 新建分类菜单项点击事件
        /// </summary>
        private async void NewCategory_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentDatabaseType))
                {
                    System.Windows.MessageBox.Show("请先加载数据库");
                    return;
                }
                if (_currentDatabaseType == "CAD")
                {
                    _currentOperation = ManagementOperationType.AddCategory;
                    InitializeCategoryPropertiesForCategory();//初始化新建分类界面
                    _selectedCategoryNode = null; // 清除选中节点，表示添加主分类

                    //ShowNewCategoryTips();// 显示提示信息
                    LogManager.Instance.LogInfo("初始化新建主分类界面");
                }
                else if (_currentDatabaseType == "SW")
                {
                    _currentOperation = ManagementOperationType.AddCategory; // 创建新的SW分类
                    InitializeCategoryPropertiesForCategory();
                }

                MessageBox.Show("请在表格中填写分类属性，然后点击'应用属性'按钮", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                //System.Windows.MessageBox.Show("分类创建成功");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"新建分类时出错: {ex.Message}");
                MessageBox.Show($"初始化分类添加失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加子分类菜单项点击事件
        /// </summary>
        private async void AddSubcategory_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentDatabaseType))
                {
                    System.Windows.MessageBox.Show("请先加载数据库");
                    return;
                }

                if (_currentDatabaseType == "CAD")
                {
                    if (_selectedCategoryNode != null)
                    {
                        _currentOperation = ManagementOperationType.AddSubcategory;
                        // 初始化子分类属性编辑界面，预填父分类ID
                        InitializeSubcategoryPropertiesForEditing(_selectedCategoryNode);
                        // 显示提示信息
                        //ShowNewSubcategoryTips(_selectedCategoryNode);

                        LogManager.Instance.LogInfo($"初始化添加子分类界面，父节点: {_selectedCategoryNode.DisplayText}");
                    }
                    else
                    {
                        MessageBox.Show("请先选择一个分类或子分类", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加子分类时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 修改菜单项点击事件
        /// </summary>
        private async void Edit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentNodeId <= 0)
            {
                System.Windows.MessageBox.Show("请先选择一个项目");
                return;
            }
            try
            {
                if (_selectedCategoryNode != null)
                {
                    // 显示当前选中节点的属性用于编辑
                    DisplayNodePropertiesForEditing(_selectedCategoryNode);
                    _currentOperation = ManagementOperationType.None; // 设置为编辑模式

                    LogManager.Instance.LogInfo($"初始化编辑分类界面: {_selectedCategoryNode.DisplayText}");
                }
                else
                {
                    MessageBox.Show("请先选择一个分类或子分类", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化编辑分类失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        /// <summary>
        /// 删除菜单项点击事件
        /// </summary>
        private async void Delete_MenuItem_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (_selectedCategoryNode != null)
                {
                    string nodeName = _selectedCategoryNode.DisplayText;

                    if (MessageBox.Show($"确定要删除分类 '{nodeName}' 吗？\n注意：删除操作不可恢复！",
                                      "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await DeleteCategoryNodeAsync(_selectedCategoryNode);
                    }
                }
                else
                {
                    MessageBox.Show("请先选择一个分类或子分类", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除分类失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除分类节点方法
        /// </summary>
        /// <param name="nodeToDelete"></param>
        /// <returns></returns>
        private async Task DeleteCategoryNodeAsync(CategoryTreeNode nodeToDelete)
        {
            try
            {
                if (nodeToDelete == null) return;

                bool success = false;

                // 根据节点层级执行不同的删除操作
                if (nodeToDelete.Level == 0)
                {
                    // 删除主分类
                    success = await DeleteMainCategoryAsync(nodeToDelete);
                }
                else
                {
                    // 删除子分类
                    success = await DeleteSubcategoryAsync(nodeToDelete);
                }

                if (success)
                {
                    // 刷新架构树
                    await RefreshCategoryTreeAsync();
                    _selectedCategoryNode = null; // 清除选中节点
                    InitializeCategoryPropertyGrid(); // 清空属性编辑区

                    MessageBox.Show("分类删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception("删除操作失败");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除分类失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新架构树按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void 刷新架构树按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshCategoryTreeAsync();
                MessageBox.Show("架构树刷新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新架构树失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 展开所有按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 展开所有按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CategoryTreeView != null && CategoryTreeView.Items != null)
                {
                    foreach (CategoryTreeNode node in CategoryTreeView.Items)
                    {
                        var treeViewItem = GetTreeViewItem(CategoryTreeView, node);
                        if (treeViewItem != null)
                        {
                            treeViewItem.IsExpanded = true;
                            ExpandAllChildren(treeViewItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"展开所有节点失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 折叠所有按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 折叠所有按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CategoryTreeView != null && CategoryTreeView.Items != null)
                {
                    foreach (CategoryTreeNode node in CategoryTreeView.Items)
                    {
                        var treeViewItem = GetTreeViewItem(CategoryTreeView, node);
                        if (treeViewItem != null)
                        {
                            treeViewItem.IsExpanded = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"折叠所有节点失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 选择文件按钮点击事件
        /// </summary>
        private async void SelectFile_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedCategoryNode == null)
                {
                    MessageBox.Show("请先在架构树中选择一个分类", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 初始化文件上传界面
                InitializeFileUploadInterface();
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择要上传的文件",
                    Filter = "所有文件 (*.*)|*.*|DWG文件 (*.dwg)|*.dwg|图片文件 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|文档文件 (*.pdf;*.doc;*.docx)|*.pdf;*.doc;*.docx",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _selectedFilePath = openFileDialog.FileName;

                    // 显示文件信息
                    DisplayFileInfo(_selectedFilePath);

                    // 初始化属性编辑界面
                    AddFileInitializeFilePropertiesGrid();
                }
                MessageBox.Show("文件已上传到服务器，可以编辑属性后点击'完成添加'", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加预览图按钮点击事件
        /// </summary>
        private async void SelectViewImage_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择预览图片",
                    Filter = "图片文件 (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _selectedPreviewImagePath = openFileDialog.FileName;

                    // 显示预览图片
                    DisplayPreviewImage(_selectedPreviewImagePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"选择预览图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除图元按钮点击事件
        /// </summary>
        private async void DeleteGraphic_Btn_Click(object sender, RoutedEventArgs e)
        {
            var selected = StroageFileDataGrid.SelectedItem as FileStorage;// 获取选中的行 获取选中的图元
            if (selected == null)
            {
                MessageBox.Show("未选中要删除的图元。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"确定删除图元：{selected.DisplayName ?? selected.FileName} ?\n该操作将删除所有关联数据且不可恢复。",
                "删除确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            DeleteGraphic_Btn.IsEnabled = false;

            try
            {
                if (_databaseManager == null || !_databaseManager.IsDatabaseAvailable)
                {
                    MessageBox.Show("数据库不可用，无法删除。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool ok = await _databaseManager.DeleteCadGraphicCascadeAsync(selected.Id, physicalDelete: true);
                if (ok)
                {
                    // 尝试从 ItemsSource 移除并刷新
                    var src = StroageFileDataGrid.ItemsSource;

                    if (src is IList<FileStorage> list)
                    {
                        list.Remove(selected);
                    }
                    else if (src is System.Collections.IList nonGenericList)
                    {
                        nonGenericList.Remove(selected);
                    }
                    else if (src is System.Windows.Data.CollectionView view && view.SourceCollection is System.Collections.IList viewList)
                    {
                        viewList.Remove(selected);
                        view.Refresh();
                    }
                    else
                    {
                        // 退化策略：直接刷新当前分类文件列表
                        await RefreshFilesForCurrentCategoryAsync();
                    }

                    StroageFileDataGrid.Items.Refresh();
                    CategoryPropertiesDataGrid.ItemsSource = null;

                    MessageBox.Show("删除成功。", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("删除失败，请查看日志。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除过程中发生错误：{ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DeleteGraphic_Btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// 还原初始值按钮点击事件
        /// </summary>
        private void ResetToInitial_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentOperation = ManagementOperationType.None;
                InitializeCategoryPropertyGrid();
                ClearFileUploadInterface();
                MessageBox.Show("操作已取消", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取消操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 应用属性按钮点击事件
        /// </summary>
        private async void ApplyProperties_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool success = false;

                switch (_currentOperation)
                {
                    case ManagementOperationType.AddCategory:
                        success = await ApplyCategoryPropertiesAsync();
                        break;
                    case ManagementOperationType.AddSubcategory:
                        success = await ApplySubcategoryPropertiesAsync();
                        break;
                    case ManagementOperationType.None:
                        // 如果没有明确的操作类型，可能是编辑操作
                        if (_selectedCategoryNode != null)
                        {
                            success = await UpdateCategoryPropertiesAsync();
                        }
                        else
                        {
                            MessageBox.Show("请先选择要操作的分类或子分类", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        break;
                    default:
                        MessageBox.Show("未知操作类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                }

                if (success)
                {
                    // 重置操作状态
                    _currentOperation = ManagementOperationType.None;
                    InitializeCategoryPropertyGrid();

                    // 刷新架构树显示
                    await RefreshCategoryTreeAsync();

                    MessageBox.Show("操作成功，架构树已更新", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("操作失败，请检查输入数据", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加文件按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFile_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 执行文件上传
                UploadFileAndSaveToDatabase();

                LoadFilesForCategoryAsync(_selectedCategoryNode);// 重新加载文件列表
            }
            catch (Exception ex)
            {
                MessageBox.Show($"完成添加失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 文件处理

        /// <summary>
        /// 辅助方法
        /// </summary>
        private void InitializeFileUploadInterface()
        {
            // 清空所有输入框
            file_Path.Text = "";
            File_Name.Text = "";
            File_Size.Text = "";
            view_File_Path.Text = "";
            ViewImage.Source = null;

            // 清空属性编辑网格
            CategoryPropertiesDataGrid.ItemsSource = null;

            // 重置字段
            _selectedFilePath = null;
            _selectedPreviewImagePath = null;
            _currentFileStorage = null;
            _currentFileAttribute = null;
        }

        /// <summary>
        /// 显示文件信息
        /// </summary>
        /// <param name="filePath"></param>
        private void DisplayFileInfo(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                // 显示文件信息
                file_Path.Text = filePath;
                File_Name.Text = fileInfo.Name;
                File_Size.Text = $"{fileInfo.Length / 1024.0:F2} KB";
                File_Type.Text = fileInfo.Extension.ToLower();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示文件信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加显示文件存储信息的方法
        /// </summary>
        /// <param name="fileStorage"></param>
        private void DisplayFileStorageInfo(FileStorage fileStorage)
        {
            try
            {
                // 显示文件信息
                file_Path.Text = fileStorage.FilePath ?? "";
                File_Name.Text = fileStorage.DisplayName ?? fileStorage.FileName ?? "";
                File_Name.Text = FormatFileNameForDisplay(fileStorage.DisplayName ?? fileStorage.FileName ?? "");
                File_Size.Text = fileStorage.FileSize > 0 ? $"{fileStorage.FileSize / 1024.0:F2} KB" : "";
                File_Type.Text = fileStorage.FileType ?? "";
                view_File_Path.Text = fileStorage.PreviewImagePath ?? "无预览图片";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示文件信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示预览图片
        /// </summary>
        /// <param name="imagePath"></param>
        private void DisplayPreviewImage(string imagePath)
        {
            try
            {
                view_File_Path.Text = imagePath;

                // 显示预览图片
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ViewImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示预览图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化属性编辑网格
        /// </summary>
        private void AddFileInitializeFilePropertiesGrid()
        {
            try
            {
                var properties = new List<CategoryPropertyEditModel>
                  {
                  // 文件存储表(cad_file_storage)相关属性
                  new CategoryPropertyEditModel { PropertyName1 = "显示名称", PropertyValue1 = Path.GetFileNameWithoutExtension(_selectedFilePath), PropertyName2 = "元素块名", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "层名", PropertyValue1 = "TJ(工艺专业GY)", PropertyName2 = "颜色索引", PropertyValue2 = "40" },
                  new CategoryPropertyEditModel { PropertyName1 = "描述", PropertyValue1 = "", PropertyName2 = "版本", PropertyValue2 = "1" },
                  new CategoryPropertyEditModel { PropertyName1 = "是否公开", PropertyValue1 = "是", PropertyName2 = "创建者", PropertyValue2 = Environment.UserName },
                  
                  // 文件属性表(cad_file_attributes)相关属性
                  new CategoryPropertyEditModel { PropertyName1 = "长度", PropertyValue1 = "", PropertyName2 = "宽度", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "高度", PropertyValue1 = "", PropertyName2 = "角度", PropertyValue2 = "0" },
                  new CategoryPropertyEditModel { PropertyName1 = "基点X", PropertyValue1 = "0", PropertyName2 = "基点Y", PropertyValue2 = "0" },
                  new CategoryPropertyEditModel { PropertyName1 = "基点Z", PropertyValue1 = "0", PropertyName2 = "介质", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "规格", PropertyValue1 = "", PropertyName2 = "材质", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "标准编号", PropertyValue1 = "", PropertyName2 = "功率", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "容积", PropertyValue1 = "", PropertyName2 = "压力", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "温度", PropertyValue1 = "", PropertyName2 = "直径", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "外径", PropertyValue1 = "", PropertyName2 = "内径", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "厚度", PropertyValue1 = "", PropertyName2 = "重量", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "型号", PropertyValue1 = "", PropertyName2 = "备注", PropertyValue2 = "" },
                  
                  // 文件标签表(file_tags)相关属性（可以添加多个标签）
                  new CategoryPropertyEditModel { PropertyName1 = "标签1", PropertyValue1 = "", PropertyName2 = "标签2", PropertyValue2 = "" },
                  new CategoryPropertyEditModel { PropertyName1 = "标签3", PropertyValue1 = "", PropertyName2 = "", PropertyValue2 = "" }
                  };

                CategoryPropertiesDataGrid.ItemsSource = properties;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化属性编辑失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 初始化文件属性编辑网格
        /// </summary>
        private void InitializeFilePropertiesGrid()
        {
            try
            {
                var initialRows = new List<CategoryPropertyEditModel>
        {
            new CategoryPropertyEditModel(),
            new CategoryPropertyEditModel(),
            new CategoryPropertyEditModel()
        };

                CategoryPropertiesDataGrid.ItemsSource = initialRows;
                LogManager.Instance.LogInfo("初始化文件属性编辑网格成功:InitializeFilePropertiesGrid()");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"初始化文件属性编辑网格失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 上传文件并保存到数据库
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task UploadFileAndSaveToDatabase()
        {
            List<string> uploadedFiles = new List<string>(); // 记录已上传的文件路径，用于回滚
            FileStorage savedFileStorage = null; // 记录已保存的文件记录
            FileAttribute savedFileAttribute = null; // 记录已保存的属性记录
            bool transactionSuccess = false;

            try
            {
                if (string.IsNullOrEmpty(_selectedFilePath) || _selectedCategoryNode == null)
                {
                    throw new Exception("文件路径或分类节点为空");
                }

                if (_fileManager == null)
                {
                    throw new Exception("文件管理器未初始化");
                }

                // 1. 获取文件信息
                var fileInfo = new FileInfo(_selectedFilePath);
                string fileName = fileInfo.Name;
                string displayName = Path.GetFileNameWithoutExtension(fileName);
                string description = $"上传文件: {fileName}";
                var fileStorage = new FileStorage();
                // 2. 使用FileManager上传主文件到服务器指定路径
                using (var fileStream = File.OpenRead(_selectedFilePath))
                {
                    fileStorage = await _fileManager.UploadFileAsync(_databaseManager,
                        _selectedCategoryNode.Id,
                        _selectedCategoryNode.Level == 0 ? "main" : "sub",
                        fileName,
                        fileStream,
                        description,
                        Environment.UserName
                    );

                    // 保存上传后的文件信息
                    _currentFileStorage = fileStorage;
                    savedFileStorage = fileStorage;
                    uploadedFiles.Add(fileStorage.FilePath); // 记录已上传的文件路径
                }

                // 3. 如果有预览图片，上传预览图片
                string previewStoredPath = null;
                if (!string.IsNullOrEmpty(_selectedPreviewImagePath) && File.Exists(_selectedPreviewImagePath))
                {
                    var previewInfo = new FileInfo(_selectedPreviewImagePath);
                    string previewFileName = $"{Path.GetFileNameWithoutExtension(_selectedPreviewImagePath)}_preview{previewInfo.Extension}";

                    using (var previewStream = File.OpenRead(_selectedPreviewImagePath))
                    {
                        // 生成预览文件存储路径
                        string previewStoredName = $"{Guid.NewGuid()}{previewInfo.Extension}";
                        previewStoredPath = Path.Combine(
                            Path.GetDirectoryName(_currentFileStorage.FilePath),
                            previewStoredName);

                        // 复制预览图片到同一目录
                        File.Copy(_selectedPreviewImagePath, previewStoredPath, true);

                        _currentFileStorage.PreviewImageName = previewStoredName;
                        _currentFileStorage.PreviewImagePath = previewStoredPath;
                        uploadedFiles.Add(previewStoredPath); // 记录预览文件路径
                    }
                }

                // 4. 创建文件属性对象
                _currentFileAttribute = new FileAttribute
                {
                    //FileStorageId = _currentFileStorage.Id,
                    FileName = _currentFileStorage.FileName,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 5. 从属性编辑网格中获取属性值
                var properties = CategoryPropertiesDataGrid.ItemsSource as List<CategoryPropertyEditModel>;
                if (properties != null)
                {
                    foreach (var property in properties)
                    {
                        SetFileAttributeProperty(_currentFileAttribute, property.PropertyName1, property.PropertyValue1);
                        SetFileAttributeProperty(_currentFileAttribute, property.PropertyName2, property.PropertyValue2);
                    }
                }
                if (_currentFileAttribute.FileName == null)
                {
                    MessageBox.Show("请填写文件名称", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 6. 保存文件属性到数据库
                int attributeResult = await _databaseManager.AddFileAttributeAsync(_currentFileAttribute);
                if (attributeResult <= 0)
                {
                    LogManager.Instance.LogInfo("保存文件属性失败");

                }
                else
                {
                    LogManager.Instance.LogInfo("保存文件属性到数据库:成功");
                }
                //获取文件属性ID
                _currentFileAttribute = await _databaseManager.GetFileAttributeAsync(_currentFileStorage.DisplayName);
                if (_currentFileAttribute == null || _currentFileAttribute.Id == null)
                {
                    LogManager.Instance.LogInfo("获取文件属性ID失败");
                    // 发生异常，需要回滚操作
                    await FileManager.RollbackFileUpload(_databaseManager, uploadedFiles, _currentFileStorage, _currentFileAttribute);
                    return;
                }
                _currentFileStorage.FileAttributeId = _currentFileAttribute.Id;

                //新加文件到数据库中
                var fileResult = await _databaseManager.AddFileStorageAsync(_currentFileStorage);
                if (fileResult == 0)
                {
                    LogManager.Instance.LogInfo("保存文件记录到数据库:失败");
                    // 发生异常，需要回滚操作
                    await FileManager.RollbackFileUpload(_databaseManager, uploadedFiles, _currentFileStorage, _currentFileAttribute);
                    return;
                }
                else
                {
                    LogManager.Instance.LogInfo("保存文件记录到数据库:成功");
                }
                ;
                _currentFileStorage = await _databaseManager.GetFileStorageAsync(_currentFileStorage.FileHash);//获取文件的基本信息
                _currentFileAttribute.FileStorageId = _currentFileStorage.Id;//文件属性ID

                await _databaseManager.UpdateFileAttributeAsync(_currentFileAttribute);//更新文件属性
                // 8. 处理标签信息
                await ProcessFileTags(_currentFileStorage.Id, properties);

                // 9. 更新分类统计
                var updateBool = await _databaseManager.UpdateCategoryStatisticsAsync(
                    _currentFileStorage.CategoryId,
                    _currentFileStorage.CategoryType);

                // 如果所有操作都成功，标记事务成功
                transactionSuccess = true;
                // 11. 刷新分类树和界面显示
                // 替换为：
                //await RefreshCurrentCategoryFilesAsync();
                await RefreshCurrentCategoryDisplayAsync(_selectedCategoryNode);
                MessageBox.Show($"文件已成功上传并保存到服务器指定路径\n文件路径: {_currentFileStorage.FilePath}",
                    "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // 发生异常，需要回滚操作
                await FileManager.RollbackFileUpload(_databaseManager, uploadedFiles, _currentFileStorage, _currentFileAttribute);
                throw new Exception($"文件上传和数据库保存失败: {ex.Message}", ex);
            }
            finally
            {
                // 如果事务失败，执行回滚
                if (!transactionSuccess)
                {
                    await FileManager.RollbackFileUpload(_databaseManager, uploadedFiles, _currentFileStorage, _currentFileAttribute);
                }
            }
        }

        /// <summary>
        /// 设置文件属性
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        private void SetFileAttributeProperty(FileAttribute attribute, string propertyName, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyValue))
                return;
            bool boolValue = false;
            if (propertyValue == "是") boolValue = true;
            try
            {
                switch (propertyName.ToLower())
                {
                    case "长度":
                        if (decimal.TryParse(propertyValue, out decimal length))
                            attribute.Length = length;
                        break;
                    case "宽度":
                        if (decimal.TryParse(propertyValue, out decimal width))
                            attribute.Width = width;
                        break;
                    case "高度":
                        if (decimal.TryParse(propertyValue, out decimal height))
                            attribute.Height = height;
                        break;
                    case "角度":
                        if (decimal.TryParse(propertyValue, out decimal angle))
                            attribute.Angle = angle;
                        break;
                    case "基点x":
                        if (decimal.TryParse(propertyValue, out decimal baseX))
                            attribute.BasePointX = baseX;
                        break;
                    case "基点y":
                        if (decimal.TryParse(propertyValue, out decimal baseY))
                            attribute.BasePointY = baseY;
                        break;
                    case "基点z":
                        if (decimal.TryParse(propertyValue, out decimal baseZ))
                            attribute.BasePointZ = baseZ;
                        break;
                    case "介质":
                        attribute.MediumName = propertyValue;
                        break;
                    case "规格":
                        attribute.Specifications = propertyValue;
                        break;
                    case "材质":
                        attribute.Material = propertyValue;
                        break;
                    case "标准编号":
                        attribute.StandardNumber = propertyValue;
                        break;
                    case "功率":
                        attribute.Power = propertyValue;
                        break;
                    case "容积":
                        attribute.Volume = propertyValue;
                        break;
                    case "压力":
                        attribute.Pressure = propertyValue;
                        break;
                    case "温度":
                        attribute.Temperature = propertyValue;
                        break;
                    case "直径":
                        attribute.Diameter = propertyValue;
                        break;
                    case "外径":
                        attribute.OuterDiameter = propertyValue;
                        break;
                    case "内径":
                        attribute.InnerDiameter = propertyValue;
                        break;
                    case "厚度":
                        attribute.Thickness = propertyValue;
                        break;
                    case "重量":
                        attribute.Weight = propertyValue;
                        break;
                    case "型号":
                        attribute.Model = propertyValue;
                        break;
                    case "备注":
                        attribute.Remarks = propertyValue;
                        break;
                    case "名称":
                        attribute.FileName = propertyValue;
                        break;
                    case "元素块名":
                        _currentFileStorage.ElementBlockName = propertyValue;
                        break;
                    case "层名":
                        _currentFileStorage.LayerName = propertyValue;
                        break;
                    case "颜色索引":
                        _currentFileStorage.ColorIndex = Convert.ToInt32(propertyValue);
                        break;
                    case "是否公开":
                        _currentFileStorage.IsPublic = Convert.ToInt32(propertyValue);
                        break;
                    case "描述":
                        _currentFileStorage.Description = propertyValue;
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"设置属性 {propertyName} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理文件标签
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private async Task ProcessFileTags(int fileId, List<CategoryPropertyEditModel> properties)
        {
            try
            {
                // 查找标签属性并添加到数据库
                foreach (var property in properties)
                {
                    // 处理标签1
                    if (property.PropertyName1?.StartsWith("标签") == true && !string.IsNullOrEmpty(property.PropertyValue1))
                    {
                        var tag = new FileTag
                        {
                            FileId = fileId,
                            TagName = property.PropertyValue1,
                            CreatedAt = DateTime.Now
                        };
                        // 这里需要在DatabaseManager中添加添加标签的方法
                        var addFileTagBool = await _databaseManager.AddFileTagAsync(tag);
                        if (addFileTagBool)
                        {
                            LogManager.Instance.LogInfo($"添加标签 {tag.TagName} 成功");
                        }
                        
                    }

                    // 处理标签2
                    if (property.PropertyName2?.StartsWith("标签") == true && !string.IsNullOrEmpty(property.PropertyValue2))
                    {
                        var tag = new FileTag
                        {
                            FileId = fileId,
                            TagName = property.PropertyValue2,
                            CreatedAt = DateTime.Now
                        };
                        var addFileTagBool = await _databaseManager.AddFileTagAsync(tag);
                        if (addFileTagBool)
                        {
                            LogManager.Instance.LogInfo($"添加标签 {tag.TagName} 成功");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"处理文件标签时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空文件上传界面
        /// </summary>
        private void ClearFileUploadInterface()
        {
            // 清空所有输入框和显示
            file_Path.Text = "";
            File_Name.Text = "";
            File_Size.Text = "";
            File_Type.Text = "";
            view_File_Path.Text = "";
            ViewImage.Source = null;

            // 清空属性编辑网格
            CategoryPropertiesDataGrid.ItemsSource = null;

            // 重置字段
            _selectedFilePath = null;
            _selectedPreviewImagePath = null;
            _currentFileStorage = null;
            _currentFileAttribute = null;
            _selectedCategoryNode = null;
        }

        /// <summary>
        /// 添加文件选择相关字段
        /// </summary>
        private FileStorage _selectedFileStorage;
        private FileAttribute _selectedFileAttribute;

        /// <summary>
        /// DataGrid选中事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void StroageFileDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                if (StroageFileDataGrid.SelectedItem is FileStorage selectedFile)
                {
                    LogManager.Instance.LogInfo($"选中文件: {selectedFile.DisplayName} (ID: {selectedFile.Id})");
                    _selectedFileStorage = selectedFile;

                    // 显示文件基本信息
                    DisplayFileBasicInfo(selectedFile);
                    // 加载并显示文件属性
                    await LoadAndDisplayFileAttributesAsync(selectedFile);

                    // 加载预览图片
                    var previewImage = await GetPreviewImageAsync(selectedFile);
                    // 预览图片会在PreviewImage_Loaded事件中处理
                    // 初始化属性编辑界面
                    //InitializeFilePropertiesGrid();
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"处理文件选择时出错: {ex.Message}");
                MessageBox.Show($"处理文件选择时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示文件基本信息
        /// </summary>
        /// <param name="fileStorage"></param>
        private void DisplayFileBasicInfo(FileStorage fileStorage)
        {
            try
            {
                file_Path.Text = fileStorage.FilePath ?? "";
                File_Name.Text = fileStorage.DisplayName ?? fileStorage.FileName ?? "";
                File_Size.Text = fileStorage.FileSize > 0 ? $"{fileStorage.FileSize / 1024.0:F2} KB" : "";
                File_Type.Text = fileStorage.FileType ?? "";
                view_File_Path.Text = fileStorage.PreviewImagePath ?? "无预览图片";
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"显示文件基本信息时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载并显示文件属性（修改现有方法）
        /// </summary>
        /// <param name="fileStorage">数据库储存的文件</param>
        /// <returns></returns>
        private async Task LoadAndDisplayFileAttributesAsync(FileStorage fileStorage)
        {
            try
            {
                LogManager.Instance.LogInfo($"开始加载文件 {fileStorage.DisplayName} 的属性");

                if (_databaseManager == null)
                {
                    LogManager.Instance.LogWarning("数据库管理器为空");
                    return;
                }

                // 获取文件属性
                var fileAttribute = await _databaseManager.GetFileAttributeByGraphicIdAsync(fileStorage.Id);
                _selectedFileAttribute = fileAttribute;

                // 准备显示数据
                var displayData = PrepareFileDisplayData(fileStorage, fileAttribute);
                CategoryPropertiesDataGrid.ItemsSource = displayData;

                LogManager.Instance.LogInfo("文件属性加载完成");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"加载文件属性时出错: {ex.Message}");
                MessageBox.Show($"加载文件属性时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 准备文件显示数据
        /// </summary>
        /// <param name="fileStorage"></param>
        /// <param name="fileAttribute"></param>
        /// <returns></returns>
        private List<CategoryPropertyEditModel> PrepareFileDisplayData(FileStorage fileStorage, FileAttribute fileAttribute)
        {
            var propertyRows = new List<CategoryPropertyEditModel>();

            try
            {
                LogManager.Instance.LogDebug("准备文件显示数据");

                // 收集所有属性
                var allProperties = new List<KeyValuePair<string, string>>();

                // 添加FileStorage属性
                if (fileStorage != null)
                {
                    AddObjectProperties(allProperties, fileStorage, "文件信息");
                }

                // 添加FileAttribute属性
                if (fileAttribute != null)
                {
                    AddObjectProperties(allProperties, fileAttribute, "属性信息");
                }

                // 转换为两列显示格式
                for (int i = 0; i < allProperties.Count; i += 2)
                {
                    var row = new CategoryPropertyEditModel();

                    // 第一列
                    var prop1 = allProperties[i];
                    row.PropertyName1 = GetPropertyDisplayName(prop1.Key);
                    row.PropertyValue1 = prop1.Value ?? "";

                    // 第二列（如果有）
                    if (i + 1 < allProperties.Count)
                    {
                        var prop2 = allProperties[i + 1];
                        row.PropertyName2 = GetPropertyDisplayName(prop2.Key);
                        row.PropertyValue2 = prop2.Value ?? "";
                    }

                    propertyRows.Add(row);
                }

                // 确保至少有几行空行用于编辑
                while (propertyRows.Count < 5)
                {
                    propertyRows.Add(new CategoryPropertyEditModel());
                }

                LogManager.Instance.LogDebug($"准备完成 {propertyRows.Count} 行属性数据");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"准备文件显示数据时出错: {ex.Message}");
            }

            return propertyRows;
        }

        /// <summary>
        /// 添加对象属性到列表
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="obj"></param>
        /// <param name="category"></param>
        private void AddObjectProperties(List<KeyValuePair<string, string>> properties, object obj, string category)
        {
            try
            {
                if (obj == null) return;

                var objectType = obj.GetType();
                var objectProperties = objectType.GetProperties();

                foreach (var prop in objectProperties)
                {
                    try
                    {
                        // 跳过一些不需要显示的属性
                        if (ShouldSkipProperty(prop.Name))
                            continue;

                        var value = prop.GetValue(obj);
                        string displayValue = value?.ToString() ?? "";

                        // 特殊处理某些属性
                        if (prop.Name == "FileSize" && value is long fileSize)
                        {
                            displayValue = FormatFileSize(fileSize);
                        }
                        else if (prop.Name.EndsWith("At") && value is DateTime dateTime)
                        {
                            displayValue = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else if (prop.Name == "IsActive" || prop.Name == "IsPublic" || prop.Name == "IsPreview")
                        {
                            displayValue = (value?.ToString() == "True") ? "是" : "否";
                        }

                        properties.Add(new KeyValuePair<string, string>($"{category}.{prop.Name}", displayValue));
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.LogDebug($"获取属性 {prop.Name} 值时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"添加对象属性时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否应该跳过属性
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private bool ShouldSkipProperty(string propertyName)
        {
            var skipProperties = new[] { "FileData", "PreviewImageData" }; // 跳过二进制数据
            return skipProperties.Contains(propertyName);
        }

        /// <summary>
        /// 获取属性显示名称
        /// </summary>
        /// <param name="fullPropertyName"></param>
        /// <returns></returns>
        private string GetPropertyDisplayName(string fullPropertyName)
        {
            try
            {
                // 分离分类和属性名
                if (fullPropertyName.Contains("."))
                {
                    var parts = fullPropertyName.Split('.');
                    var category = parts[0];
                    var propertyName = parts[1];

                    // 获取映射名称
                    if (_propertyDisplayNameMap.TryGetValue(propertyName, out string displayName))
                    {
                        return displayName;
                    }
                }
                else
                {
                    // 直接属性名
                    if (_propertyDisplayNameMap.TryGetValue(fullPropertyName, out string displayName))
                    {
                        return displayName;
                    }
                }

                // 如果没有映射，返回原始名称
                return fullPropertyName.Contains(".") ? fullPropertyName.Split('.')[1] : fullPropertyName;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogDebug($"获取属性显示名称时出错: {ex.Message}");
                return fullPropertyName;
            }
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        private string FormatFileSize(long fileSize)
        {
            try
            {
                if (fileSize < 1024)
                    return $"{fileSize} B";
                else if (fileSize < 1024 * 1024)
                    return $"{fileSize / 1024.0:F2} KB";
                else if (fileSize < 1024 * 1024 * 1024)
                    return $"{fileSize / (1024.0 * 1024.0):F2} MB";
                else
                    return $"{fileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
            catch
            {
                return fileSize.ToString();
            }
        }

        /// <summary>
        /// 清空DataGrid中的文件属性显示
        /// </summary>
        private void ClearFilePropertiesInDataGrid()
        {
            try
            {
                if (PropertiesDataGrid != null)
                {
                    PropertiesDataGrid.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"清空PropertiesDataGrid时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新选中文件
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSelectedFileAsync()
        {
            try
            {
                if (_selectedFileStorage == null || _databaseManager == null)
                    return;

                bool fileUpdated = false;
                bool previewUpdated = false;

                // 更新文件
                if (!string.IsNullOrEmpty(file_Path.Text) && File.Exists(file_Path.Text))
                {
                    // 复制新文件到存储位置
                    string newStoredFileName = $"{Guid.NewGuid()}{Path.GetExtension(file_Path.Text)}";
                    string newStoredFilePath = Path.Combine(
                        Path.GetDirectoryName(_selectedFileStorage.FilePath),
                        newStoredFileName);

                    File.Copy(file_Path.Text, newStoredFilePath, true);

                    // 更新数据库记录
                    _selectedFileStorage.FilePath = newStoredFilePath;
                    _selectedFileStorage.FileName = Path.GetFileName(file_Path.Text);
                    _selectedFileStorage.FileSize = new FileInfo(newStoredFilePath).Length;
                    _selectedFileStorage.Version += 1; // 增加版本号
                    _selectedFileStorage.UpdatedAt = DateTime.Now;

                    fileUpdated = true;
                }

                // 更新预览图片
                if (!string.IsNullOrEmpty(view_File_Path.Text) && File.Exists(view_File_Path.Text))
                {
                    // 复制新预览图片到存储位置
                    string newPreviewFileName = $"{Guid.NewGuid()}{Path.GetExtension(view_File_Path.Text)}";
                    string newPreviewFilePath = Path.Combine(
                        Path.GetDirectoryName(_selectedFileStorage.PreviewImagePath ?? _selectedFileStorage.FilePath),
                        newPreviewFileName);

                    File.Copy(view_File_Path.Text, newPreviewFilePath, true);

                    // 更新数据库记录
                    _selectedFileStorage.PreviewImagePath = newPreviewFilePath;
                    _selectedFileStorage.PreviewImageName = newPreviewFileName;

                    previewUpdated = true;
                }

                // 保存到数据库
                if (fileUpdated || previewUpdated)
                {
                    await _databaseManager.UpdateFileStorageAsync(_selectedFileStorage);
                }

                // 清空输入框
                //new_File_Path.Text = "";
                //new_Preview_Path.Text = "";
                //version_Description.Text = "";

                LogManager.Instance.LogInfo($"文件更新完成: 文件={fileUpdated}, 预览={previewUpdated}");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新文件时出错: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 更新文件属性
        /// </summary>
        /// <param name="fileAttribute"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        private void UpdateFileAttributeProperty(FileAttribute fileAttribute, string propertyName, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyName) || fileAttribute == null)
                return;

            try
            {
                var property = fileAttribute.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    // 根据属性类型进行转换
                    if (property.PropertyType == typeof(string))
                    {
                        property.SetValue(fileAttribute, propertyValue);
                    }
                    else if (property.PropertyType == typeof(int?) || property.PropertyType == typeof(int))
                    {
                        if (int.TryParse(propertyValue, out int intValue))
                        {
                            property.SetValue(fileAttribute, intValue);
                        }
                    }
                    else if (property.PropertyType == typeof(double?) || property.PropertyType == typeof(double))
                    {
                        if (double.TryParse(propertyValue, out double doubleValue))
                        {
                            property.SetValue(fileAttribute, doubleValue);
                        }
                    }
                    else if (property.PropertyType == typeof(DateTime?) || property.PropertyType == typeof(DateTime))
                    {
                        if (DateTime.TryParse(propertyValue, out DateTime dateTimeValue))
                        {
                            property.SetValue(fileAttribute, dateTimeValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新属性 {propertyName} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 直接刷新当前选中分类的文件显示
        /// </summary>
        private async Task RefreshCurrentCategoryFilesAsync()
        {
            try
            {
                if (_selectedCategoryNode == null)
                    return;

                // 获取当前选中的TabItem
                if (MainTabControl?.SelectedItem is TabItem selectedTabItem)
                {
                    string header = selectedTabItem.Header.ToString().Trim();

                    // 查找对应的面板
                    WrapPanel panel = GetPanelByFolderName(header);
                    if (panel != null)
                    {
                        // 重新加载按钮
                        await LoadButtonsFromDatabase(header, panel);
                        LogManager.Instance.LogInfo($"已刷新 {header} 分类的文件显示");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"刷新当前分类文件显示时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新当前分类的显示
        /// </summary>
        /// <param name="categoryNode">分类节点</param>
        private async Task RefreshCurrentCategoryDisplayAsync(CategoryTreeNode categoryNode)
        {
            try
            {
                // 根据当前选中的分类节点，刷新对应的界面显示
                if (categoryNode.Level == 0 && categoryNode.Data is CadCategory)
                {
                    // 主分类，刷新主分类下的内容显示
                    await RefreshMainCategoryDisplayAsync(categoryNode);
                }
                else if (categoryNode.Data is CadSubcategory)
                {
                    // 子分类，刷新子分类下的内容显示
                    await RefreshSubcategoryDisplayAsync(categoryNode);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"刷新当前分类显示时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新主分类显示
        /// </summary>
        /// <param name="categoryNode">主分类节点</param>
        private async Task RefreshMainCategoryDisplayAsync(CategoryTreeNode categoryNode)
        {
            try
            {
                // 这里可以根据需要刷新主分类的显示
                // 例如：刷新主分类下的文件列表等
                LogManager.Instance.LogInfo($"刷新主分类显示: {categoryNode.DisplayText}");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"刷新主分类显示时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新子分类显示
        /// </summary>
        /// <param name="categoryNode">子分类节点</param>
        private async Task RefreshSubcategoryDisplayAsync(CategoryTreeNode categoryNode)
        {
            try
            {
                // 刷新子分类下的文件显示
                await RefreshSubcategoryFilesDisplayAsync(categoryNode);
                LogManager.Instance.LogInfo($"刷新子分类显示: {categoryNode.DisplayText}");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"刷新子分类显示时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新子分类文件显示
        /// </summary>
        /// <param name="subcategoryNode">子分类节点</param>
        private async Task RefreshSubcategoryFilesDisplayAsync(CategoryTreeNode subcategoryNode)
        {
            try
            {
                // 根据当前TabItem刷新对应的文件显示
                // 这里需要根据当前选中的TabItem来确定刷新哪个面板
                string currentTabHeader = GetCurrentSelectedTabHeader();

                if (!string.IsNullOrEmpty(currentTabHeader))
                {
                    // 找到对应的面板并刷新
                    WrapPanel targetPanel = GetPanelByFolderName(currentTabHeader);
                    if (targetPanel != null)
                    {
                        // 重新加载该分类下的按钮
                        await LoadButtonsFromDatabase(currentTabHeader, targetPanel);
                        LogManager.Instance.LogInfo($"刷新了 {currentTabHeader} 面板的文件显示");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"刷新子分类文件显示时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新架构树显示（修正版）
        /// </summary>
        /// <returns></returns>
        private async Task RefreshCategoryTreeAsync()
        {
            try
            {
                // 重新加载分类和子分类数据
                await LoadCategoryTreeAsync();

                // 更新UI显示
                DisplayCategoryTree();

                // 展开当前选中的节点
                if (_selectedCategoryNode != null && CategoryTreeView != null)
                {
                    ExpandTreeNodeToSelectedNode(_selectedCategoryNode);
                }

                LogManager.Instance.LogInfo("架构树刷新完成");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"刷新架构树时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 展开树节点到选中的节点
        /// </summary>
        /// <param name="selectedNode">选中的节点</param>
        private void ExpandTreeNodeToSelectedNode(CategoryTreeNode selectedNode)
        {
            try
            {
                // 这里可以实现展开树节点到指定节点的逻辑
                // 例如：展开父节点，选中指定节点等
                if (CategoryTreeView != null && CategoryTreeView.Items.Count > 0)
                {
                    // 可以通过遍历TreeViewItem来展开到指定节点
                    // 这里简化处理，实际可以根据需要完善
                    CategoryTreeView.UpdateLayout();
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"展开树节点时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前选中的TabItem标题
        /// </summary>
        /// <returns>TabItem标题</returns>
        private string GetCurrentSelectedTabHeader()
        {
            try
            {
                // 根据当前选中的分类节点确定对应的TabItem
                // 这里需要根据您的具体界面结构来实现
                if (_selectedCategoryNode != null)
                {
                    // 可以根据分类节点的名称来确定对应的TabItem
                    // 例如：如果分类节点名称为"工艺"，则对应的TabItem为"工艺"
                    return _selectedCategoryNode.DisplayText;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取当前选中TabItem标题时出错: {ex.Message}");
                return string.Empty;
            }
        }
        /// <summary>
        /// 刷新右侧图元文件列表（根据当前选中的分类节点）
        /// </summary>
        private async Task RefreshFilesForCurrentCategoryAsync()
        {
            try
            {
                if (_selectedCategoryNode == null || _databaseManager == null || !_databaseManager.IsDatabaseAvailable)
                    return;

                var nodeData = _selectedCategoryNode.Data;
                List<FileStorage> files = null;

                if (nodeData is CadCategory main)
                {
                    files = await _databaseManager.GetFilesByCategoryIdAsync(main.Id, "main");
                }
                else if (nodeData is CadSubcategory sub)
                {
                    files = await _databaseManager.GetFilesByCategoryIdAsync(sub.Id, "sub");
                }

                if (files != null)
                {
                    StroageFileDataGrid.ItemsSource = files;
                    StroageFileDataGrid.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"刷新文件列表失败: {ex.Message}");
            }
        }

        #endregion

        #region 电气按键
        private void 横墙电开建筑洞_Btn_Clic(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("横墙电开建洞");
            command?.Invoke();
        }

        private void 纵墙电开建筑洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 矩形电开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 直径电开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 半径电开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region 暖通按键
        private void 横墙暖开建筑洞_Btn_Clic(object sender, RoutedEventArgs e)
        {

        }

        private void 纵墙暖开建筑洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 矩形暖开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 直径暖开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 半径暖开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region 自控按键
        private void 横墙自控开建筑洞_Btn_Clic(object sender, RoutedEventArgs e)
        {

        }

        private void 纵墙自控开建筑洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 矩形自控开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 直径自控开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 半径自控开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region 建筑按键
        private void 吊顶_Btn_Clic(object sender, RoutedEventArgs e)
        {
            VariableDictionary.winForm_Status = false;
            var command = UnifiedCommandManager.GetCommand("吊顶");
            command?.Invoke();
        }

        private void 不吊顶_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("不吊顶");
            command?.Invoke();
        }

        private void 防撞护板_Btn_Clic(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("防撞护板");
            command?.Invoke();
        }

        private void 房间编号_Btn_Clic(object sender, RoutedEventArgs e)
        {
            VariableDictionary.winForm_Status = false;

            var command = UnifiedCommandManager.GetCommand("房间编号");
            command?.Invoke();
        }

        private void 编号检查_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("编号检查");
            command?.Invoke();
        }

        private void 冷藏库降板_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("冷藏库降板");
            command?.Invoke();
        }
        private void 冷冻库降板_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("冷冻库降板");
            command?.Invoke();
        }

        private void 特殊地面做法要求_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("特殊地面做法要求");
            command?.Invoke();
        }

        private void 排水沟_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("排水沟");
            command?.Invoke();
        }
        private void 横墙建筑开洞_Btn_Clic(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("横墙建筑开洞");
            command?.Invoke();
        }

        private void 纵墙建筑开洞_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedCommandManager.GetCommand("纵墙建筑开洞");
            command?.Invoke();
        }
        #endregion

        #region 结构按键
        private void 结开建筑洞_Btn_Clic(object sender, RoutedEventArgs e)
        {

        }

        private void 纵墙结开建筑洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 矩形结开洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 直径结开洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 半径结开洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region 水按键
        private void 横墙水开建筑洞_Btn_Clic(object sender, RoutedEventArgs e)
        {

        }

        private void 纵墙水开建筑洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 矩形水开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 直径水开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void 半径水开结构洞_Btn_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #endregion

        private void 保存设置按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void 测试连接按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取当前输入的设置
                string serverIP = TextBox_Set_ServiceIP.Text.Trim();
                string serverPort = TextBox_Set_ServicePort.Text.Trim();
                string databaseName = TextBox_Set_DatabaseName.Text.Trim();
                string username = TextBox_Set_Username.Text.Trim();
                string password = PasswordBox_Set_Password.Text.Trim();

                if (string.IsNullOrEmpty(serverIP) || string.IsNullOrEmpty(serverPort) ||
                    string.IsNullOrEmpty(databaseName) || string.IsNullOrEmpty(username) ||
                    string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("请填写完整的数据库连接信息", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(serverPort, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("请输入有效的端口号（1-65535）", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 首先测试网络连接
                MessageBox.Show("正在测试网络连接...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

                if (!ServerSyncManager.TestNetworkConnection(serverIP, port))
                {
                    MessageBox.Show($"无法连接到服务器 {serverIP}:{port}\n请检查：\n1. 服务器IP地址是否正确\n2. 网络是否连通\n3. 防火墙是否阻止连接",
                        "网络连接失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // 构建连接字符串
                    string testConnectionString = $"Server={serverIP};Port={port};Database={databaseName};Uid={username};Pwd={password};Connection Timeout=5;";

                    LogManager.Instance.LogInfo($"尝试连接到 {serverIP}:{port}");

                    // 测试连接
                    var testDatabaseManager = new DatabaseManager(testConnectionString);
                    if (testDatabaseManager.IsDatabaseAvailable)
                    {
                        MessageBox.Show($"数据库连接测试成功\n服务器: {serverIP}:{port}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else
                    {
                        MessageBox.Show($"数据库连接测试失败\n服务器: {serverIP}:{port}", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"数据库连接测试失败\n错误信息: {ex.Message}\n服务器: {serverIP}:{port}",
                        "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试连接失败: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void 应用设置按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                ReinitializeDatabase();
                MessageBox.Show("设置已应用，数据库连接已更新", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region 服务器设置方法 
        /// <summary>
        /// 保存设置到配置文件
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                // 更新字段值
                _serverIP = TextBox_Set_ServiceIP.Text.Trim();
                _serverPort = int.TryParse(TextBox_Set_ServicePort.Text.Trim(), out int port) ? port : 3306;
                _databaseName = TextBox_Set_DatabaseName.Text.Trim();
                _username = TextBox_Set_Username.Text.Trim();
                _password = PasswordBox_Set_Password.Text.Trim();
                _storagePath = TextBox_Set_StoragePath.Text.Trim();
                _useDPath = CheckBox_UseDPath.IsChecked ?? true;
                _autoSync = CheckBox_AutoSync.IsChecked ?? true;
                _syncInterval = int.TryParse(TextBox_SyncInterval.Text, out int interval) ? interval : 30;

                // 保存到配置文件
                Properties.Settings.Default.ServerIP = _serverIP;
                Properties.Settings.Default.ServerPort = _serverPort;
                Properties.Settings.Default.DatabaseName = _databaseName;
                Properties.Settings.Default.Username = _username;
                Properties.Settings.Default.Password = _password;
                Properties.Settings.Default.StoragePath = _storagePath;
                Properties.Settings.Default.UseDPath = _useDPath;
                Properties.Settings.Default.AutoSync = _autoSync;
                Properties.Settings.Default.SyncInterval = _syncInterval;
                Properties.Settings.Default.Save();

                LogManager.Instance.LogInfo("设置已保存到配置文件");
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"保存设置时出错: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 检测输入的字符是否合法
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool IsTextAllowed(string text)
        {
            return text.All(char.IsDigit);
        }

        /// <summary>
        /// 处理粘贴操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_Set_ServicePort_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// 在管理员模块中添加查看日志按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 查看日志按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logFilePath = LogManager.Instance.LogFilePath;
                if (File.Exists(logFilePath))
                {
                    // 使用默认程序打开日志文件
                    System.Diagnostics.Process.Start(logFilePath);
                }
                else
                {
                    MessageBox.Show("日志文件不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"打开日志文件失败: {ex.Message}");
                MessageBox.Show($"打开日志文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 或者添加一个显示最新日志的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 显示最新日志按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logFilePath = LogManager.Instance.LogFilePath;
                if (File.Exists(logFilePath))
                {
                    var lines = File.ReadAllLines(logFilePath);
                    // 显示最后100行日志
                    var recentLogs = lines.Skip(Math.Max(0, lines.Length - 100));

                    // 可以显示在弹出窗口或TextBox中
                    string logContent = string.Join(Environment.NewLine, recentLogs);

                    // 创建一个简单的日志查看窗口
                    var logWindow = new Window
                    {
                        Title = "应用程序日志",
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    var textBox = new TextBox
                    {
                        Text = logContent,
                        IsReadOnly = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12
                    };

                    logWindow.Content = textBox;
                    logWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"显示日志失败: {ex.Message}");
            }
        }
        #endregion


        /// <summary>
        /// 添加属性名称映射字典（如果还没有的话）
        /// </summary>
        private readonly Dictionary<string, string> _propertyDisplayNameMap = new Dictionary<string, string>
        {
            // FileStorage 属性映射
            { "Id", "文件ID" },
            { "FileName", "文件名" },
            { "CategoryId", "分类ID" },
            { "FileAttributeId", "属性ID" },
            { "FileStoredName", "存储文件名" },
            { "DisplayName", "显示名称" },
            { "FileType", "文件类型" },
            { "FileHash", "文件哈希" },
            { "ElementBlockName", "元素块名" },
            { "LayerName", "图层名称" },
            { "ColorIndex", "颜色索引" },
            { "FilePath", "文件路径" },
            { "PreviewImageName", "预览图片名" },
            { "PreviewImagePath", "预览图片路径" },
            { "FileSize", "文件大小" },
            { "IsPreview", "是否预览" },
            { "Version", "版本号" },
            { "Description", "描述" },
            { "CreatedAt", "创建时间" },
            { "UpdatedAt", "更新时间" },
            { "CategoryType", "分类类型" },
            { "CreatedBy", "创建者" },
            { "IsActive", "是否激活" },
            { "Title", "标题" },
            { "Keywords", "关键字" },
            { "IsPublic", "是否公开" },
            { "UpdatedBy", "更新者" },
             //FileAttribute 属性映射
            { "FileStorageId", "存储文件ID" },
            { "Length", "长度" },
            { "Width", "宽度" },
            { "Height", "高度" },
            { "Angle", "角度" },
            { "BasePointX", "基点X" },
            { "BasePointY", "基点Y" },
            { "BasePointZ", "基点Z" },
            { "MediumName", "介质" },
            { "Specifications", "规格" },
            { "Material", "材质" },
            { "StandardNumber", "标准编号" },
            { "Power", "功率" },
            { "Volume", "容积" },
            { "Pressure", "压力" },
            { "Temperature", "温度" },
            { "Diameter", "直径" },
            { "OuterDiameter", "外径" },
            { "InnerDiameter", "内径" },
            { "Thickness", "厚度" },
            { "Weight", "重量" },
            { "Model", "型号" },
            { "Remarks", "备注" },
            { "Customize1", "自定义1" },
            { "Customize2", "自定义2" },
            { "Customize3", "自定义3" }
        };

        #region 批量添加文件

        private void 导出模板_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogManager.Instance.LogInfo("开始导出模板");

                // 创建模板DataTable
                DataTable templateTable = CreateTemplateDataTable();

                // 选择保存路径
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "保存模板文件",
                    Filter = "Excel文件 (*.xlsx)|*.xlsx|Excel 97-2003文件 (*.xls)|*.xls",
                    FileName = "图元批量添加模板.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // 导出到Excel
                    if (ExportDataTableToExcel(templateTable, filePath))
                    {
                        MessageBox.Show($"模板已成功导出到:\n{filePath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        LogManager.Instance.LogInfo($"模板导出成功: {filePath}");
                    }
                    else
                    {
                        MessageBox.Show("模板导出失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        LogManager.Instance.LogError("模板导出失败");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"导出模板时出错: {ex.Message}");
                MessageBox.Show($"导出模板时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void 导入模板_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择Excel文件",
                    Filter = "Excel文件 (*.xlsx;*.xls)|*.xlsx;*.xls",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    file_Path.Text = openFileDialog.FileName;
                    LogManager.Instance.LogInfo($"选择Excel文件: {openFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"选择Excel文件时出错: {ex.Message}");
                MessageBox.Show($"选择Excel文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void 批量添加图元_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(file_Path.Text))
                {
                    MessageBox.Show("请先选择Excel文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!File.Exists(file_Path.Text))
                {
                    MessageBox.Show("选择的Excel文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 显示确认对话框
                var result = MessageBox.Show("确定要批量添加图元吗？这将导入Excel文件中的所有数据。",
                    "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                // 开始批量导入
                BatchImportGraphicsAsync(file_Path.Text);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"批量添加图元时出错: {ex.Message}");
                MessageBox.Show($"批量添加图元时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 创建模板DataTable
        /// </summary>
        private DataTable CreateTemplateDataTable()
        {
            DataTable dt = new DataTable("图元批量添加模板");
            #region FileStorage
            // 添加列（基于FileStorage和FileAttribute的所有字段）
            dt.Columns.Add("分类ID", typeof(int));
            dt.Columns.Add("分类类型", typeof(string));
            dt.Columns.Add("文件名", typeof(string));
            dt.Columns.Add("显示名称", typeof(string));
            dt.Columns.Add("文件路径", typeof(string));
            dt.Columns.Add("文件类型", typeof(string));
            dt.Columns.Add("文件大小", typeof(long));
            dt.Columns.Add("元素块名", typeof(string));
            dt.Columns.Add("图层名称", typeof(string));
            dt.Columns.Add("颜色索引", typeof(int));
            dt.Columns.Add("预览图片名称", typeof(string));
            dt.Columns.Add("预览图片路径", typeof(string));
            dt.Columns.Add("是否预览", typeof(int));
            dt.Columns.Add("创建者", typeof(string));
            dt.Columns.Add("标题", typeof(string));
            dt.Columns.Add("关键字", typeof(string));
            dt.Columns.Add("更新者", typeof(string));
            dt.Columns.Add("版本号", typeof(int));
            dt.Columns.Add("是否激活", typeof(int));
            dt.Columns.Add("是否公开", typeof(int));
            dt.Columns.Add("描述", typeof(string));
            #endregion
            #region FileAttribute
            // FileAttribute字段
            dt.Columns.Add("存储文件ID", typeof(string));
            dt.Columns.Add("文件名称", typeof(string));
            dt.Columns.Add("长度", typeof(double));
            dt.Columns.Add("宽度", typeof(double));
            dt.Columns.Add("高度", typeof(double));
            dt.Columns.Add("角度", typeof(string));
            dt.Columns.Add("介质", typeof(string));
            dt.Columns.Add("材质", typeof(string));
            dt.Columns.Add("规格", typeof(string));
            dt.Columns.Add("标准编号", typeof(string));
            dt.Columns.Add("功率", typeof(string));
            dt.Columns.Add("容积", typeof(string));
            dt.Columns.Add("压力", typeof(string));
            dt.Columns.Add("温度", typeof(string));
            dt.Columns.Add("直径", typeof(string));
            dt.Columns.Add("外径", typeof(string));
            dt.Columns.Add("内径", typeof(string));
            dt.Columns.Add("厚度", typeof(string));
            dt.Columns.Add("重量", typeof(string));
            dt.Columns.Add("型号", typeof(string));
            dt.Columns.Add("备注", typeof(string));
            dt.Columns.Add("自定义1", typeof(string));
            dt.Columns.Add("自定义2", typeof(string));
            dt.Columns.Add("自定义3", typeof(string));
            #endregion
            #region FileStorage 示例
            // 添加示例行
            DataRow sampleRow = dt.NewRow();
            sampleRow["分类ID"] = 1;
            sampleRow["分类类型"] = "sub";
            sampleRow["文件名"] = "示例文件.dwg";
            sampleRow["显示名称"] = "示例图元";
            sampleRow["文件路径"] = "C:\\示例路径\\示例文件.dwg";
            sampleRow["文件类型"] = ".dwg";
            sampleRow["文件大小"] = 102400;
            sampleRow["元素块名"] = "220V插座";
            sampleRow["图层名称"] = "TJ(电气专业D)";
            sampleRow["颜色索引"] = "142";
            sampleRow["预览图片名称"] = "示例图片.png";
            sampleRow["预览图片路径"] = "C:\\示例路径\\示例文件.png";
            sampleRow["是否预览"] = 0;
            sampleRow["创建者"] = "张三";
            sampleRow["标题"] = "220V电源插座";
            sampleRow["描述"] = "220V电源插座";
            sampleRow["关键字"] = "220V、电源插座";
            sampleRow["版本号"] = 1;
            sampleRow["是否激活"] = 1;
            sampleRow["是否公开"] = 1;
            #endregion
            #region FileAttribute示例数据
            sampleRow["长度"] = 200.0;
            sampleRow["宽度"] = 100.0;
            sampleRow["高度"] = 50.0;
            sampleRow["角度"] = 90.0;
            sampleRow["介质"] = "水";
            sampleRow["材质"] = "316不锈钢";
            sampleRow["规格"] = "Standard";
            sampleRow["标准编号"] = "2.5";
            sampleRow["功率"] = "10KW";
            sampleRow["容积"] = "100L";
            sampleRow["压力"] = "5MPa";
            sampleRow["温度"] = "100℃";
            sampleRow["直径"] = "100mm";
            sampleRow["外径"] = "10mm";
            sampleRow["内径"] = "90mm";
            sampleRow["厚度"] = "10mm";
            sampleRow["重量"] = "10Kg";
            sampleRow["型号"] = "A4";
            sampleRow["备注"] = "备注";
            sampleRow["自定义1"] = "自定义1";
            sampleRow["自定义2"] = "自定义2";
            sampleRow["自定义3"] = "自定义3";
            #endregion
            dt.Rows.Add(sampleRow);

            return dt;
        }

        /// <summary>
        /// 导出DataTable到Excel
        /// </summary>
        private bool ExportDataTableToExcel(DataTable dataTable, string filePath)
        {
            try
            {
                // 使用EPPlus库导出Excel（推荐方式）
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("图元批量添加模板");

                    // 添加标题行
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;

                        // 修复ExcelFillPatternType的引用问题
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // 添加数据行
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        for (int j = 0; j < dataTable.Columns.Count; j++)
                        {
                            worksheet.Cells[i + 2, j + 1].Value = dataTable.Rows[i][j];
                        }
                    }

                    // 自动调整列宽
                    worksheet.Cells.AutoFitColumns();

                    // 保存文件
                    var fileInfo = new FileInfo(filePath);
                    package.SaveAs(fileInfo);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"导出Excel时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量导入图元
        /// </summary>
        private async Task BatchImportGraphicsAsync(string excelFilePath)
        {
            try
            {
                LogManager.Instance.LogInfo($"开始批量导入图元: {excelFilePath}");

                // 读取Excel文件
                DataTable dataTable = ReadExcelToDataTable(excelFilePath);

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("Excel文件中没有数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int successCount = 0;
                int failCount = 0;

                // 遍历每一行数据
                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        // 创建FileStorage对象
                        var fileStorage = CreateFileStorageFromRow(row);

                        if (fileStorage != null)
                        {
                            // 保存到数据库
                            int fileId = await _databaseManager.AddFileStorageAsync(fileStorage);

                            if (fileId > 0)
                            {
                                // 创建FileAttribute对象
                                var fileAttribute = CreateFileAttributeFromRow(row, fileId);

                                if (fileAttribute != null)
                                {
                                    // 保存文件属性
                                    await _databaseManager.AddFileAttributeAsync(fileAttribute);
                                }

                                successCount++;
                                LogManager.Instance.LogInfo($"成功导入图元: {fileStorage.DisplayName}");
                            }
                            else
                            {
                                failCount++;
                                LogManager.Instance.LogWarning($"导入图元失败: {fileStorage?.DisplayName}");
                            }
                        }
                        else
                        {
                            failCount++;
                            LogManager.Instance.LogWarning("创建FileStorage对象失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        LogManager.Instance.LogError($"导入单个图元时出错: {ex.Message}");
                    }
                }

                // 显示结果
                MessageBox.Show($"批量导入完成\n成功: {successCount} 个\n失败: {failCount} 个",
                    "完成", MessageBoxButton.OK, MessageBoxImage.Information);

                LogManager.Instance.LogInfo($"批量导入完成 - 成功: {successCount}, 失败: {failCount}");

                // 刷新分类树
                await RefreshCategoryTreeAsync();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"批量导入图元时出错: {ex.Message}");
                MessageBox.Show($"批量导入图元时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从Excel行数据创建FileStorage对象
        /// </summary>
        private FileStorage CreateFileStorageFromRow(DataRow row)
        {
            try
            {
                var fileStorage = new FileStorage
                {
                    /*
                        CategoryId("分类ID", typeof(int));
                        CategoryType("分类类型", typeof(string));
                       FileName ("文件名", typeof(string));
                       DisplayName ("显示名称", typeof(string));
                        FilePath("文件路径", typeof(string));
                        FileType("文件类型", typeof(string));
                        FileSize("文件大小", typeof(long));
                        ElementBlockName("元素块名", typeof(string));
                        LayerName("图层名称", typeof(string));
                        ColorIndex("颜色索引", typeof(int));
                        PreviewImageName("预览图片名称", typeof(string));
                        PreviewImagePath("预览图片路径", typeof(string));
                        IsPreview("是否预览", typeof(int));
                        CreatedBy("创建者", typeof(string));
                        Title("标题", typeof(string));
                        Keywords("关键字", typeof(string));
                        UpdatedBy("更新者", typeof(string));
                        Version("版本号", typeof(int));
                        IsActive("是否激活", typeof(int));
                        IsPublic("是否公开", typeof(int));
                        Description("描述", typeof(string));
       
                     */
                    CategoryId = GetIntValue(row, "分类ID"),
                    CategoryType = "sub", // 默认为主分类
                    FileName = GetStringValue(row, "文件名"),
                    DisplayName = GetStringValue(row, "显示名称"),
                    FilePath = GetStringValue(row, "文件路径"),
                    FileType = GetStringValue(row, "文件类型"),
                    FileSize = GetLongValue(row, "文件大小"),
                    ElementBlockName = GetStringValue(row, "元素块名"),
                    LayerName = GetStringValue(row, "图层名称"),
                    ColorIndex = GetIntValue(row, "颜色索引"),
                    PreviewImageName = GetStringValue(row, "预览图片名称"),
                    PreviewImagePath = GetStringValue(row, "预览图片路径"),
                    IsPreview = GetIntValue(row, "是否预览", 0),
                    CreatedBy = GetStringValue(row, "创建者"),
                    Title = GetStringValue(row, "标题"),
                    Version = GetIntValue(row, "版本号", 1),
                    IsActive = GetIntValue(row, "是否激活", 1),
                    IsPublic = GetIntValue(row, "是否公开", 1),
                    Description = GetStringValue(row, "描述"),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                return fileStorage;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"创建FileStorage对象时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从Excel行数据创建FileAttribute对象
        /// </summary>
        private FileAttribute CreateFileAttributeFromRow(DataRow row, int storageFileId)
        {
            try
            {
                var fileAttribute = new FileAttribute
                {
                    /*
                      { "FileStorageId", "存储文件ID" },
                      { "Length", "长度" },
                      { "Width", "宽度" },
                      { "Height", "高度" },
                      { "Angle", "角度" },
                      { "BasePointX", "基点X" },
                      { "BasePointY", "基点Y" },
                      { "BasePointZ", "基点Z" },
                      { "MediumName", "介质" },
                      { "Specifications", "规格" },
                      { "Material", "材质" },
                      { "StandardNumber", "标准编号" },
                      { "Power", "功率" },
                      { "Volume", "容积" },
                      { "Pressure", "压力" },
                      { "Temperature", "温度" },
                      { "Diameter", "直径" },
                      { "OuterDiameter", "外径" },
                      { "InnerDiameter", "内径" },
                      { "Thickness", "厚度" },
                      { "Weight", "重量" },
                      { "Model", "型号" },
                      { "Remarks", "备注" },
                      { "Customize1", "自定义1" },
                      { "Customize2", "自定义2" },
                      { "Customize3", "自定义3" }
                     */
                    FileStorageId = storageFileId,
                    Width = (decimal?)GetDoubleValue(row, "宽度"),
                    Height = (decimal?)GetDoubleValue(row, "高度"),
                    Length = (decimal?)GetDoubleValue(row, "长度"),
                    Angle = (decimal?)GetDoubleValue(row, "角度"),
                    BasePointX = (decimal?)GetDoubleValue(row, "基点X"),
                    BasePointZ = (decimal?)GetDoubleValue(row, "基点Y"),
                    BasePointY = (decimal?)GetDoubleValue(row, "基点Z"),
                    MediumName = GetStringValue(row, "介质"),
                    Specifications = GetStringValue(row, "规格"),
                    Material = GetStringValue(row, "材质"),
                    StandardNumber = GetStringValue(row, "标准编号"),
                    Power = GetStringValue(row, "功率"),
                    Volume = GetStringValue(row, "容积"),
                    Pressure = GetStringValue(row, "压力"),
                    Temperature = GetStringValue(row, "温度"),
                    Diameter = GetStringValue(row, "直径"),
                    OuterDiameter = GetStringValue(row, "外径"),
                    InnerDiameter = GetStringValue(row, "内径"),
                    Thickness = GetStringValue(row, "厚度"),
                    Weight = GetStringValue(row, "重量"),
                    Model = GetStringValue(row, "型号"),
                    Remarks = GetStringValue(row, "备注"),
                    Customize1 = GetStringValue(row, "自定义1"),
                    Customize2 = GetStringValue(row, "自定义2"),
                    Customize3 = GetStringValue(row, "自定义3"),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                return fileAttribute;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"创建FileAttribute对象时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 读取Excel文件到DataTable
        /// </summary>
        private DataTable ReadExcelToDataTable(string filePath)
        {
            try
            {
                DataTable dataTable = new DataTable();

                // 使用EPPlus读取Excel
                using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // 读取第一个工作表

                    // 读取标题行
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var cellValue = worksheet.Cells[1, col].Value?.ToString() ?? "";
                        dataTable.Columns.Add(cellValue);
                    }

                    // 读取数据行
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var dataRow = dataTable.NewRow();
                        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Value ?? DBNull.Value;
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"读取Excel文件时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 辅助方法
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="columnName">列</param>
        /// <returns></returns>
        private string GetStringValue(DataRow row, string columnName)
        {
            try
            {
                if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
                {
                    return row[columnName].ToString();
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 获取整型值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private int GetIntValue(DataRow row, string columnName, int defaultValue = 0)
        {
            try
            {
                if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
                {
                    if (int.TryParse(row[columnName].ToString(), out int result))
                    {
                        return result;
                    }
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 获取长整型值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private long GetLongValue(DataRow row, string columnName, long defaultValue = 0)
        {
            try
            {
                if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
                {
                    if (long.TryParse(row[columnName].ToString(), out long result))
                    {
                        return result;
                    }
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 获取双精度值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private double GetDoubleValue(DataRow row, string columnName, double defaultValue = 0.0)
        {
            try
            {
                if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
                {
                    if (double.TryParse(row[columnName].ToString(), out double result))
                    {
                        return result;
                    }
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        /// <summary>
        /// 获取布尔值
        /// </summary>
        /// <param name="row"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private bool GetBoolValue(DataRow row, string columnName, bool defaultValue = false)
        {
            try
            {
                if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
                {
                    if (bool.TryParse(row[columnName].ToString(), out bool result))
                    {
                        return result;
                    }
                    // 处理"是"/"否"等中文表示
                    string value = row[columnName].ToString().ToLower();
                    if (value == "是" || value == "true" || value == "1")
                        return true;
                    if (value == "否" || value == "false" || value == "0")
                        return false;
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        #endregion

    }

    /// <summary>
    /// 分类属性编辑模型
    /// </summary>
    public class CategoryPropertyEditModel : INotifyPropertyChanged
    {
        private string _propertyName1;
        private string _propertyValue1;
        private string _propertyName2;
        private string _propertyValue2;

        public string PropertyName1
        {
            get => _propertyName1;
            set
            {
                if (_propertyName1 != value)
                {
                    _propertyName1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PropertyValue1
        {
            get => _propertyValue1;
            set
            {
                if (_propertyValue1 != value)
                {
                    _propertyValue1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PropertyName2
        {
            get => _propertyName2;
            set
            {
                if (_propertyName2 != value)
                {
                    _propertyName2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PropertyValue2
        {
            get => _propertyValue2;
            set
            {
                if (_propertyValue2 != value)
                {
                    _propertyValue2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
