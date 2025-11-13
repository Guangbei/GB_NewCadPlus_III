using Microsoft.VisualBasic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.ComponentModel;
using System.Drawing.Imaging;
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
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
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
            System.Diagnostics.Debug.WriteLine("WPF实例已注册到UnifiedUIManager"); // 调试输出，确认注册成功
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
                    System.Diagnostics.Debug.WriteLine("TabControl事件绑定成功");//测试
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("未找到名称为MainTabControl的控件");
                }

                InitializeDatabase();//初始化数据库

                // 添加右键菜单到分类树
                if (CategoryTreeView != null)
                {
                    AddContextMenuToTreeView(CategoryTreeView);
                    System.Diagnostics.Debug.WriteLine("分类树右键菜单添加成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("未找到CategoryTreeView控件");
                }

                Load();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗口加载时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 修改原有的数据库初始化方法
        /// </summary>
        private async void InitializeDatabase()
        {
            try
            {
                // 加载设置
                LoadSettings();

                System.Diagnostics.Debug.WriteLine("开始初始化数据库连接...");

                // 使用配置的连接字符串
                string connectionString = $"Server={_serverIP};Port={_serverPort};Database={_databaseName};Uid={_username};Pwd={_password};";
                _databaseManager = new DatabaseManager(connectionString);

                // 初始化文件管理器
                _fileManager = new FileManager(_databaseManager);

                // 初始化同步管理器
                _serverSyncManager = new ServerSyncManager(_databaseManager, _fileManager);

                // 如果启用自动同步，开始同步
                if (_autoSync)
                {
                    _serverSyncManager.StartSync(_syncInterval);
                }

                var categories = await _databaseManager.GetAllCadCategoriesAsync();
                if (categories != null)
                {
                    System.Diagnostics.Debug.WriteLine($"数据库连接成功，找到 {categories.Count} 个分类");
                }
                InitializeCategoryPropertyGrid();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}");
                _databaseManager = null;
                _fileManager = null;
                _serverSyncManager = null;
            }
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
        }

        /// <summary>
        /// 从服务器获取预览图片并缓存
        /// </summary>
        private async Task<BitmapImage> GetPreviewImageAsync(FileStorage fileStorage)
        {
            try
            {
                // 检查内存缓存
                if (_imageCache.ContainsKey(fileStorage.FilePath))
                {
                    return _imageCache[fileStorage.FilePath];
                }

                // 检查本地缓存文件
                string cacheFileName = $"{fileStorage.Id}_{Path.GetFileName(fileStorage.PreviewImagePath ?? fileStorage.FilePath)}.png";
                string cacheFilePath = Path.Combine(_previewCachePath, cacheFileName);

                if (File.Exists(cacheFilePath))
                {
                    // 从本地缓存加载
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(cacheFilePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // 添加到内存缓存
                    _imageCache[fileStorage.FilePath] = bitmap;
                    return bitmap;
                }

                // 从服务器下载预览图片
                if (!string.IsNullOrEmpty(fileStorage.PreviewImagePath) && _fileManager != null)
                {
                    try
                    {
                        using (var imageStream = await _fileManager.DownloadFileAsync(
                            fileStorage.Id, Environment.UserName, GetLocalIpAddress()))
                        {
                            if (imageStream != null)
                            {
                                // 保存到本地缓存
                                using (var fileStream = File.Create(cacheFilePath))
                                {
                                    await imageStream.CopyToAsync(fileStream);
                                }

                                // 加载图片
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(cacheFilePath);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();

                                // 添加到内存缓存
                                _imageCache[fileStorage.FilePath] = bitmap;
                                return bitmap;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"下载预览图片失败: {ex.Message}");
                    }
                }

                // 如果没有预览图片，返回默认图片
                return GetDefaultPreviewImage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取预览图片时出错: {ex.Message}");
                return GetDefaultPreviewImage();
            }
        }

        /// <summary>
        /// 获取默认预览图片
        /// </summary>
        private BitmapImage GetDefaultPreviewImage()
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/Resources/default_preview.png");
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                // 如果默认图片不存在，创建一个空白图片
                return new BitmapImage();
            }
        }

        /// <summary>
        /// 清理图片缓存
        /// </summary>
        private void ClearImageCache()
        {
            _imageCache.Clear();

            // 清理本地缓存文件（可选）
            try
            {
                if (Directory.Exists(_previewCachePath))
                {
                    foreach (var file in Directory.GetFiles(_previewCachePath))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理本地缓存失败: {ex.Message}");
            }
        }

        // 在窗口关闭时清理缓存
        protected  void OnClosing(CancelEventArgs e)
        {
            try
            {
                // 停止同步
                _serverSyncManager?.StopSync();

                // 清理图片缓存
                ClearImageCache();

                OnClosing(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭窗口时清理缓存失败: {ex.Message}");
            }
        }

        // 添加手动清理缓存按钮
        private void 清理缓存按钮_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearImageCache();
                MessageBox.Show("缓存已清理", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清理缓存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取本地IP地址
        /// </summary>
        private string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// TabControl选择改变事件
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("TabControl选择改变事件触发");
            // 获取当前选中的TabItem
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
            {
                string header = selectedTab.Header.ToString().Trim();//获取TabItem的标题
                System.Diagnostics.Debug.WriteLine($"选中的TabItem: {header}");
                if (header.Contains("图元集") || header.Contains("图层管理")) // 检查是否是嵌套的TabItem（如"图元集"、"图层管理"等）
                {
                    TabItem parentTabItem = FindParentTabItem(selectedTab);// 如果是嵌套的TabItem，需要找到其父级TabItem
                    if (parentTabItem != null)
                    {
                        string parentHeader = parentTabItem.Header.ToString().Trim();
                        System.Diagnostics.Debug.WriteLine($"父级TabItem: {parentHeader}");
                        LoadButtonsForCategoryFromDatabase(parentHeader, selectedTab);   // 从数据库加载按钮
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("未找到父级TabItem");
                    }
                }
                else
                {
                    // 如果是主TabItem，直接加载
                    TabControl childTabControl = FindVisualChild<TabControl>(selectedTab, null); // 检查这个TabItem是否有子TabControl
                    if (childTabControl != null)
                    {
                        if (childTabControl.Items.Count > 0 && childTabControl.Items[0] is TabItem firstChildTab) // 如果有子TabControl，加载第一个TabItem的内容
                        {
                            LoadButtonsForTabItem(selectedTab, header);
                        }
                    }
                    else
                    {
                        LoadButtonsForTabItem(selectedTab, header);// 没有子TabControl，直接加载
                    }
                    if (header == "工艺")
                    {
                        LoadConditionButtons();
                    }
                }
            }
        }

        /// <summary>
        /// 从数据库加载指定分类的按钮
        /// </summary>
        /// <param Name="categoryName">分类名称</param>
        /// <param Name="tabItem">目标TabItem</param>
        private async Task LoadButtonsForCategoryFromDatabase(string categoryName, TabItem tabItem)
        {
            try
            {
                // 检查是否已经加载过
                if (loadedTabItems.ContainsKey(categoryName) && loadedTabItems[categoryName])
                {
                    System.Diagnostics.Debug.WriteLine($"{categoryName} 已经加载过，跳过");
                    return;
                }

                // 查找目标面板
                WrapPanel panel = FindTargetPanel(tabItem, categoryName);
                if (panel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"找到面板，开始从数据库加载 {categoryName} 的按钮");
                    LoadButtonsForItem(categoryName, panel);

                    // 标记为已加载
                    loadedTabItems[categoryName] = true;
                    System.Diagnostics.Debug.WriteLine($"{categoryName} 按钮加载完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"未找到 {categoryName} 的面板");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从数据库加载{categoryName}按钮时出错: {ex.Message}");
            }
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
        /// 为特定TabItem加载按钮
        /// </summary>
        /// <param Name="tabItem"></param>
        /// <param Name="folderName"></param>
        private void LoadButtonsForTabItem(TabItem tabItem, string folderName)
        {
            System.Diagnostics.Debug.WriteLine($"尝试加载 {folderName} 的按钮");
            if (loadedTabItems.ContainsKey(folderName) && loadedTabItems[folderName]) // 检查是否已经加载过
            {
                System.Diagnostics.Debug.WriteLine($"{folderName} 已经加载过，跳过");
                return;
            }
            try
            {
                // 根据TabItem的Header找到对应的面板
                // WrapPanel panel = FindButtonPanelByTabHeader(tabItem, folderName);
                // 查找目标面板 - 需要处理嵌套的TabControl结构
                WrapPanel panel = FindTargetPanel(tabItem, folderName);
                if (panel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"找到面板，开始加载按钮");
                    LoadButtonsForItem(folderName, panel);
                    loadedTabItems[folderName] = true; // 标记为已加载
                    System.Diagnostics.Debug.WriteLine($"{folderName} 按钮加载完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"未找到 {folderName} 的面板");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载{folderName}按钮时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找目标面板 - 处理嵌套TabControl结构
        /// </summary>
        /// <param Name="tabItem"></param>
        /// <param Name="folderName"></param>
        /// <returns></returns>
        private WrapPanel FindTargetPanel(TabItem tabItem, string folderName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"查找 {folderName} 的目标面板");

                // 方法1: 直接通过字段引用查找（最可靠）
                WrapPanel panel = GetPanelByFolderName(folderName);
                if (panel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"通过字段引用查找成功");
                    return panel;
                }
                #region 没用上  嵌套TabControl结构处理 
                //// 方法2: 在当前TabItem中查找
                //panel = FindPanelInTabItem(tabItem, folderName);
                //if (panel != null)
                //{
                //    System.Diagnostics.Debug.WriteLine($"在当前TabItem中查找成功");
                //    return panel;
                //}

                //// 方法3: 在嵌套的TabItem中查找
                //panel = FindPanelInNestedTabItem(tabItem, folderName);
                //if (panel != null)
                //{
                //    System.Diagnostics.Debug.WriteLine($"在嵌套TabItem中查找成功");
                //    return panel;
                //}
                #endregion
                System.Diagnostics.Debug.WriteLine($"未找到面板");
                return null;


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查找目标面板时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 通过文件夹名称获取对应的面板引用
        /// </summary>
        /// <param Name="folderName"></param>
        /// <returns></returns>
        private WrapPanel GetPanelByFolderName(string folderName)
        {
            switch (folderName)
            {
                case "公共图":
                    return PublicButtonsPanel;
                case "工艺":
                    return CraftButtonsPanel;
                case "建筑":
                    return ArchitectureButtonsPanel;
                case "结构":
                    return StructureButtonsPanel;
                case "给排水":
                    return PlumbingButtonsPanel;
                case "暖通":
                    return HVACButtonsPanel;
                case "电气":
                    return ElectricalButtonsPanel;
                case "自控":
                    return ControlButtonsPanel;
                case "总图":
                    return GeneralButtonsPanel;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 根据数据库中的信息加载按钮到指定面板（支持二级分类结构，带边框和背景色）
        /// </summary>
        /// <param Name="categoryName">分类名称（与TabItem的Header相同）</param>
        /// <param Name="panel">要添加按钮的面板</param>
        private async void LoadButtonsForItem(string categoryName, WrapPanel panel)
        {
            try
            {
                // 清空现有内容
                panel.Children.Clear();

                if (_useDatabaseMode && _databaseManager != null && _databaseManager.IsDatabaseAvailable)
                {
                    // 使用数据库模式
                    System.Diagnostics.Debug.WriteLine($"使用数据库模式加载 {categoryName}");
                    await LoadButtonsFromDatabase(categoryName, panel);
                }
                else
                {
                    // 使用Resources文件夹模式
                    System.Diagnostics.Debug.WriteLine($"使用Resources文件夹模式加载 {categoryName}");
                    LoadButtonsFromResources(categoryName, panel);
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从数据库加载按钮时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"从数据库加载按钮时出错: {ex.Message}\n{ex.StackTrace}");
            }
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
                    System.Diagnostics.Debug.WriteLine("数据库管理器未初始化");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"开始从数据库加载分类 {folderName} 的按钮");

                // 从数据库获取主分类信息
                var category = await _databaseManager.GetCadCategoryByNameAsync(folderName);
                if (category == null)
                {
                    System.Diagnostics.Debug.WriteLine($"未找到分类: {folderName}");
                    return;
                }

                // 获取该分类下的所有子分类
                var subcategories = await _databaseManager.GetCadSubcategoriesByCategoryIdAsync(category.Id);
                System.Diagnostics.Debug.WriteLine($"找到 {subcategories.Count} 个子分类");

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
                    System.Diagnostics.Debug.WriteLine($"处理子分类: {subcategory.DisplayName}");

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
                    System.Diagnostics.Debug.WriteLine($"在 {subcategory.DisplayName} 中找到 {graphics.Count} 个图元文件");

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
                System.Diagnostics.Debug.WriteLine($"从数据库加载按钮时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建图元按钮并添加属性显示功能
        /// </summary>
        private Button CreateGraphicButton(FileStorage fileStorage)
        {
            Button btn = new Button
            {
                Content = fileStorage.DisplayName,
                Width = 85,
                Height = 20,
                Margin = new Thickness(5, 1, 1, 1),
                Tag = fileStorage, // 存储完整的图形信息
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontWeight = FontWeights.Normal
            };

            // 添加点击事件
            btn.Click += async (sender, e) => await GraphicButton_Click(sender, e);

            return btn;
        }

        /// <summary>
        /// 加载并显示属性
        /// </summary>
        private async Task LoadAndDisplayPropertiesAsync(int cad_file_storage_id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始加载图形属性，ID: {cad_file_storage_id}");
                // 清空现有数据
                PropertiesDataGrid.ItemsSource = null;
                // 根据图元Id获取图元信息
                FileStorage? graphicInfo = await _databaseManager.GetCadGraphicByIdAsync(cad_file_storage_id);
                // 根据图元Id从数据库获取图元属性信息
                var attributes = await _databaseManager.GetFileAttributeByGraphicIdAsync(cad_file_storage_id);
                // 检查数据
                if (graphicInfo == null || attributes == null)
                {
                    System.Diagnostics.Debug.WriteLine("未找到图形属性");
                    // 显示"无属性"信息
                    var noPropertyParams = new List<PropertyPair>
                      {
                          new PropertyPair("提示", "未找到该图元的属性信息", "", "")
                      };
                    // 绑定数据
                    PropertiesDataGrid.ItemsSource = noPropertyParams;
                    return;
                }
                // 准备属性数据
                var propertyPairs = PreparePropertyData(graphicInfo, attributes);
                // 绑定到DataGrid
                PropertiesDataGrid.ItemsSource = propertyPairs;

                System.Diagnostics.Debug.WriteLine($"成功加载 {propertyPairs.Count} 行属性数据");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载属性时出错: {ex.Message}");
                // 显示错误信息
                var errorParams = new List<PropertyPair>
                {
                    new PropertyPair("错误", $"加载属性失败: {ex.Message}", "", "")
                };
                PropertiesDataGrid.ItemsSource = errorParams;// 绑定数据
            }
        }

        /// <summary>
        /// 准备属性数据显示数据
        /// </summary>
        private List<PropertyPair> PreparePropertyData(FileStorage cadGraphic, FileAttribute attributes)
        {
            var properties = new List<KeyValuePair<string, string>>();// 创建一个键值对列表
            var propertyPairs = new List<PropertyPair>();// 创建一个列表
            Type graphicType = typeof(FileStorage); // 获取CadGraphic类的属性
            PropertyInfo[] propertyInfos = graphicType.GetProperties(); // 获取属性
            foreach (var propertyItem in propertyInfos) // 遍历属性
            {
                try
                {
                    var propertyValue = propertyItem.GetValue(cadGraphic); // 获取属性值
                    if (propertyValue != null)
                    {
                        // 添加属性
                        properties.Add(new KeyValuePair<string, string>(propertyItem.Name, propertyValue.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{propertyItem.Name}: 获取值时出错 - {ex.Message}");
                }
            }
            var attributesType = typeof(FileAttribute);// 获取CadGraphic类的属性
            propertyInfos = attributesType.GetProperties();// 获取属性
            foreach (var attributesItem in propertyInfos)
            {
                try
                {
                    var propertyValue = attributesItem.GetValue(attributes);// 获取属性值
                    if (propertyValue != null)
                    {
                        // 添加属性
                        properties.Add(new KeyValuePair<string, string>(attributesItem.Name, propertyValue.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{attributesItem.Name}: 获取值时出错 - {ex.Message}");
                }
            }
            // 转换为PropertyPair格式（两列一组）
            for (int i = 0; i < properties.Count; i += 2)
            {
                if (i + 1 < properties.Count)
                {
                    // 有两个属性
                    propertyPairs.Add(new PropertyPair(
                        properties[i].Key, properties[i].Value,
                        properties[i + 1].Key, properties[i + 1].Value
                    ));
                }
                else
                {
                    // 只有一个属性
                    propertyPairs.Add(new PropertyPair(
                        properties[i].Key, properties[i].Value,
                        "", ""
                    ));
                }
            }
            if (propertyPairs.Count == 0) // 如果没有属性，添加提示信息
            {
                propertyPairs.Add(new PropertyPair("提示", "该图元暂无属性信息", "", ""));
            }
            return propertyPairs;
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
                System.Diagnostics.Debug.WriteLine($"应用程序路径: {appPath}");//显示调试信息没找到资源文件夹

                string resourcePath = System.IO.Path.Combine(appPath, "Resources", folderName);//返回本程序的资源文件夹路径；
                System.Diagnostics.Debug.WriteLine($"资源文件夹路径: {resourcePath}");//显示调试信息没找到资源文件夹
                System.Diagnostics.Debug.WriteLine($"资源文件夹是否存在: {System.IO.Directory.Exists(resourcePath)}");//显示调试信息没找到资源文件夹


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
                    System.Diagnostics.Debug.WriteLine($"找到 {subDirectories.Length} 个二级文件夹");

                    // 遍历所有二级文件夹
                    foreach (string subDir in subDirectories)
                    {
                        string subDirName = System.IO.Path.GetFileName(subDir);
                        System.Diagnostics.Debug.WriteLine($"处理二级文件夹: {subDirName}");

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
                        System.Diagnostics.Debug.WriteLine($"在 {subDirName} 中找到 {files.Length} 个dwg文件");//显示文件数量

                        if (files.Length > 0) //创建行面板
                        {
                            // 过滤并处理文件名
                            var buttonInfoList = new List<Tuple<string, string>>(); // (按钮名称, 完整文件路径)
                                                                                    // 遍历所有dwg文件
                            foreach (string file in files)
                            {
                                //调试文件
                                System.Diagnostics.Debug.WriteLine($"处理文件: {file}");
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
                                        btn.Click += (s, e) => Button_Click(s, e, buttonName, fullPath);
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
                System.Diagnostics.Debug.WriteLine("开始加载条件图元按钮...");

                // 清空现有按钮
                ClearConditionButtons();

                // 加载各专业条件按钮
                await LoadSpecializedConditionButtons("电气", 电气条件按钮面板);
                await LoadSpecializedConditionButtons("给排水", 给排水条件按钮面板);
                await LoadSpecializedConditionButtons("自控", 自控条件按钮面板);
                await LoadSpecializedConditionButtons("建筑", 结构条件按钮面板);
                await LoadSpecializedConditionButtons("结构", 结构条件按钮面板);
                await LoadSpecializedConditionButtons("暖通", 暖通条件按钮面板);

                System.Diagnostics.Debug.WriteLine("条件图元按钮加载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载条件图元按钮时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"开始加载{专业名称}条件按钮...");

                if (targetPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine($"目标面板 {专业名称} 为空");
                    return;
                }

                // 从数据库或资源文件夹中获取指定专业的条件文件
                var conditionFiles = await GetConditionFilesForSpecialty(专业名称);
                System.Diagnostics.Debug.WriteLine($"找到 {conditionFiles.Count} 个{专业名称}条件文件");

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
                        Button btn = CreateConditionButton(file);
                        rowPanel.Children.Add(btn);
                    }

                    targetPanel.Children.Add(rowPanel);
                }

                System.Diagnostics.Debug.WriteLine($"{专业名称}条件按钮加载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载{专业名称}条件按钮时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"获取{specialtyName}条件文件时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"从资源获取{specialtyName}条件文件时出错: {ex.Message}");
            }

            return conditionFiles;
        }

        /// <summary>
        /// 创建条件按钮
        /// </summary>
        private Button CreateConditionButton(ConditionFileInfo fileInfo)
        {
            Button btn = new Button
            {
                Content = fileInfo.DisplayName,
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
        /// 图元按钮点击事件
        /// </summary>
        private async Task GraphicButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is FileStorage fileStorage)
                {
                    System.Diagnostics.Debug.WriteLine($"点击图元按钮: {fileStorage.DisplayName}");

                    // 执行图元插入操作
                    //ExecuteGraphicInsert(graphic);

                    // 异步加载并显示属性
                    await LoadAndDisplayPropertiesAsync(fileStorage.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理图元按钮点击时出错: {ex.Message}");
                MessageBox.Show($"处理图元按钮点击时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    System.Diagnostics.Debug.WriteLine($"点击条件按钮: {fileInfo.DisplayName}");

                    // 执行条件插入操作
                    ExecuteConditionInsert(fileInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"执行条件插入时出错: {ex.Message}");
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

                System.Diagnostics.Debug.WriteLine($"成功插入条件: {fileInfo.DisplayName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"插入条件失败: {ex.Message}");
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
                    if (_useDatabaseMode && btn.Tag is FileStorage fileStorage)
                    {
                        // 数据库模式：处理数据库图元
                        System.Diagnostics.Debug.WriteLine($"点击了数据库图元按钮: {fileStorage.DisplayName}");
                        ExecuteDynamicButtonActionFromDatabase(fileStorage);
                    }
                    else if (!_useDatabaseMode && btn.Tag is string filePath)
                    {
                        // Resources模式：处理文件路径
                        System.Diagnostics.Debug.WriteLine($"点击了Resources图元按钮: {filePath}");
                        // 从文件路径提取按钮名称
                        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        string buttonName = fileNameWithoutExt;
                        if (fileNameWithoutExt.Contains("_"))
                        {
                            buttonName = fileNameWithoutExt.Substring(fileNameWithoutExt.IndexOf("_") + 1);
                        }
                        ExecuteDynamicButtonActionFromResources(buttonName, filePath);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("按钮点击事件处理失败：无法识别的数据类型");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理按钮点击事件时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"处理按钮点击事件时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"执行Resources按钮操作时出错: {ex.Message}");
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
                ShowPreviewImageFromDatabase(fileStorage);

                // 2. 获取图元属性
                var graphicAttribute = await _databaseManager.GetFileAttributeByGraphicIdAsync(fileStorage.Id);

                // 3. 设置相关变量
                SetRelatedVariablesFromDatabase(fileStorage, graphicAttribute);

                // 4. 调用AutoCAD命令插入块
                InsertBlockToAutoCADFromDatabase(fileStorage, graphicAttribute);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"执行数据库按钮操作时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"显示预览图时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从数据库信息设置相关变量
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
                    VariableDictionary.btnBlockLayer = fileStorage.LayerName ?? "TJ(工艺专业GY)";
                    VariableDictionary.layerColorIndex = fileStorage.ColorIndex ?? 40;

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
                    VariableDictionary.btnBlockLayer = "TJ(工艺专业GY)";
                    VariableDictionary.layerColorIndex = 40;
                }

                System.Diagnostics.Debug.WriteLine($"已设置变量: btnFileName={VariableDictionary.btnFileName}, btnBlockLayer={VariableDictionary.btnBlockLayer}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置变量时出错: {ex.Message}");
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

                    System.Diagnostics.Debug.WriteLine($"已发送插入命令: {fileStorage.DisplayName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("未找到活动的AutoCAD文档");
                    System.Windows.MessageBox.Show("未找到活动的AutoCAD文档");
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoCAD命令执行错误: {ex.Message}");
                System.Windows.MessageBox.Show($"AutoCAD命令执行错误: {ex.Message}\n错误代码: {ex.ErrorStatus}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"插入块时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"插入块时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 按钮点击事件处理
        /// </summary>
        /// <param Name="sender">事件发送者</param>
        /// <param Name="e">路由事件参数</param>
        /// <param Name="buttonName">按钮名称</param>
        /// <param Name="filePath">文件完整路径</param>
        private void Button_Click(object sender, RoutedEventArgs e, string buttonName, string filePath)
        {
            try
            {
                // 显示预览图
                ShowPreviewImage(filePath, buttonName);

                // 调用Command类中的GB_InsertBlock_方法
                // Command.GB_InsertBlock_(new Point3d(0,0,0), 0.1, buttonName);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行命令时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"初始化架构树失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"加载架构树数据失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"显示架构树失败: {ex.Message}");
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
                    // 根据选中的节点类型显示相应的属性编辑界面
                    DisplayNodePropertiesForEditing(selectedNode);
                    // 加载该分类下的文件
                    await LoadFilesForCategoryAsync(selectedNode);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理架构树选中项改变失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"显示节点属性失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取子分类数量
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
                System.Diagnostics.Debug.WriteLine($"应用分类属性时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"应用子分类属性时出错: {ex.Message}");
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
                    return;

                List<FileStorage> files = new List<FileStorage>();

                if (categoryNode.Level == 0 && categoryNode.Data is CadCategory)
                {
                    // 主分类，加载该分类下的所有文件
                    files = await _databaseManager.GetFilesByCategoryIdAsync(categoryNode.Id, "main");
                }
                else if (categoryNode.Data is CadSubcategory)
                {
                    // 子分类，加载该子分类下的所有文件
                    files = await _databaseManager.GetFilesByCategoryIdAsync(categoryNode.Id, "sub");
                }

                // 绑定到DataGrid
                StroageFileDataGrid.ItemsSource = files;

                // 异步加载预览图片
                await LoadPreviewImagesAsync(files);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    MessageBox.Show($"选中文件: {selectedFile.DisplayName}\n文件ID: {selectedFile.Id}",
                        "文件信息", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理文件选择失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 异步加载预览图片
        /// </summary>
        private async Task LoadPreviewImagesAsync(List<FileStorage> files)
        {
            try
            {
                // 这里可以预加载一些图片，或者在DataGrid的单元格加载时动态加载
                // 为了性能考虑，我们使用虚拟化加载
                System.Diagnostics.Debug.WriteLine($"开始预加载 {files.Count} 个文件的预览图片");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"预加载预览图片失败: {ex.Message}");
            }
        }

        // 添加预览图片加载事件处理
        private async void PreviewImage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var image = sender as Image;
                if (image?.Tag is FileStorage fileStorage)
                {
                    // 异步加载图片
                    var bitmap = await GetPreviewImageAsync(fileStorage);

                    // 在UI线程更新图片
                    Dispatcher.Invoke(() =>
                    {
                        image.Source = bitmap;

                        // 隐藏加载文本
                        var parentGrid = image.Parent as Grid;
                        if (parentGrid != null)
                        {
                            var loadingText = parentGrid.Children.OfType<TextBlock>().FirstOrDefault();
                            if (loadingText != null)
                            {
                                loadingText.Visibility = Visibility;// 隐藏加载文本Collapsed
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载预览图片失败: {ex.Message}");
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
        /// 加载并显示分类树
        /// </summary>
        //private async Task LoadAndDisplayCategoryTreeAsync()
        //{
        //    try
        //    {
        //        System.Diagnostics.Debug.WriteLine($"=== 开始加载分类树，数据库类型: {_currentDatabaseType} ===");
        //        // 查找显示区域的Grid（Grid.Row="2"）
        //        // 首先尝试通过可视化树查找
        //        Grid displayGrid = null;

        //        // 方法3: 手动遍历查找Grid.Row="2"的Grid
        //        if (displayGrid == null)
        //        {
        //            displayGrid = FindGridByRow(this, 2);
        //            System.Diagnostics.Debug.WriteLine("方法3查找结果: " + (displayGrid != null ? "找到" : "未找到"));
        //        }
        //        if (displayGrid != null)
        //        {
        //            System.Diagnostics.Debug.WriteLine("成功找到显示区域Grid");
        //            // 清空显示区域
        //            displayGrid.Children.Clear();
        //            System.Diagnostics.Debug.WriteLine("已清空显示区域");

        //            // 创建TreeView来显示分类树
        //            _categoryTreeView = new TreeView
        //            {
        //                Margin = new Thickness(0, 5, 0, 5),
        //                Background = new SolidColorBrush(Colors.LightBlue),
        //                FontSize = 14,
        //                Foreground = new SolidColorBrush(Colors.Black),
        //            };

        //            System.Diagnostics.Debug.WriteLine("创建TreeView完成");
        //            if (_currentDatabaseType == "CAD")
        //            {
        //                // 加载CAD分类
        //                await LoadCadCategoriesAsync(_categoryTreeView);
        //                // 添加TreeView的选择事件
        //                _categoryTreeView.SelectedItemChanged += CategoryTreeView_SelectedItemChanged;

        //            }
        //            else if (_currentDatabaseType == "SW")
        //            {
        //                // 加载SW分类
        //                await LoadSwCategoriesAsync(_categoryTreeView);
        //                // 添加TreeView的选择事件
        //                _categoryTreeView.SelectedItemChanged += CategoryTreeView_SelectedItemChanged;
        //            }
        //            // 检查TreeView是否已正确填充
        //            System.Diagnostics.Debug.WriteLine($"TreeView项目数量: {_categoryTreeView.Items.Count}");

        //            // 添加TreeView到显示区域
        //            displayGrid.Children.Clear();
        //            displayGrid.Children.Add(_categoryTreeView);
        //            // 添加右键菜单
        //            AddContextMenuToTreeView(_categoryTreeView);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"加载分类树时出错: {ex.Message}");
        //        System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        //        System.Windows.MessageBox.Show($"加载分类树时出错: {ex.Message}");
        //    }
        //}

        /// <summary>
        /// 通过Row索引查找Grid
        /// </summary>
        private Grid FindGridByRow(DependencyObject parent, int targetRow)
        {
            System.Diagnostics.Debug.WriteLine($"开始查找Row={targetRow}的Grid");

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 检查是否是Grid且有Grid.Row属性
                if (child is Grid grid)
                {
                    var row = Grid.GetRow(grid);
                    System.Diagnostics.Debug.WriteLine($"找到Grid，Row={row}");
                    if (row == targetRow)
                    {
                        System.Diagnostics.Debug.WriteLine($"找到目标Grid，Row={targetRow}");
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
        /// 加载CAD分类
        /// </summary>
        private async Task LoadCadCategoriesAsync(TreeView treeView)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始获取所有CAD分类");
                var categories = await _databaseManager.GetAllCadCategoriesAsync(); // 获取所有CAD分类
                System.Diagnostics.Debug.WriteLine($"获取到 {categories.Count} 个CAD分类");
                treeView.Items.Clear(); // 清空现有项目
                foreach (var category in categories)
                {
                    System.Diagnostics.Debug.WriteLine($"处理分类: {category.DisplayName} (ID: {category.Id})");

                    // 创建分类节点
                    TreeViewItem categoryItem = new TreeViewItem
                    {
                        Header = category.DisplayName,// 显示分类名称
                        Tag = new { Type = "Category", Id = category.Id, Object = category }// 设置Tag属性
                    };
                    System.Diagnostics.Debug.WriteLine($"创建分类节点: {category.DisplayName}");

                    // 加载子分类
                    await LoadCadSubcategoriesAsync(category.Id, categoryItem, 0);

                    treeView.Items.Add(categoryItem);
                    System.Diagnostics.Debug.WriteLine($"添加分类节点到TreeView: {category.DisplayName}");
                }
                System.Diagnostics.Debug.WriteLine($"TreeView最终项目数量: {treeView.Items.Count}");
                System.Diagnostics.Debug.WriteLine("CAD分类加载完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载CAD分类时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
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
                System.Diagnostics.Debug.WriteLine($"加载CAD子分类时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载CAD图元
        /// </summary>
        private async Task LoadCadGraphicsAsync(int subcategoryId, TreeViewItem parentItem)
        {
            try
            {
                var graphics = await _databaseManager.GetFileStorageBySubcategoryIdAsync(subcategoryId);
                foreach (var graphic in graphics)
                {
                    TreeViewItem graphicItem = new TreeViewItem
                    {
                        Header = $"    {graphic.LayerName}",
                        Tag = new { Type = "Graphic", Id = graphic.Id, Object = graphic }
                    };
                    parentItem.Items.Add(graphicItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载CAD图元时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载SW分类
        /// </summary>
        private async Task LoadSwCategoriesAsync(TreeView treeView)
        {
            try
            {
                var categories = await _databaseManager.GetAllSwCategoriesAsync();
                foreach (var category in categories)
                {
                    // 创建分类节点
                    TreeViewItem categoryItem = new TreeViewItem
                    {
                        Header = category.DisplayName,
                        Tag = new { Type = "Category", Id = category.Id, Object = category }
                    };

                    // 加载子分类
                    await LoadSwSubcategoriesAsync(category.Id, categoryItem, 0);

                    treeView.Items.Add(categoryItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载SW分类时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"加载SW子分类时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"加载SW图元时出错: {ex.Message}");
            }
        }


        #endregion

        #region 树节点选中与右键操作
        ///// <summary>
        ///// TreeView选中项改变事件
        ///// </summary>
        //private void CategoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    try
        //    {
        //        if (_categoryTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag != null)
        //        {
        //            var tagInfo = selectedItem.Tag as dynamic;
        //            _currentNodeType = tagInfo.Type;
        //            _currentNodeId = tagInfo.Id;
        //            _currentSelectedNode = tagInfo.Object;

        //            System.Diagnostics.Debug.WriteLine($"选中节点: 类型={_currentNodeType}, ID={_currentNodeId}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"TreeView选中项改变时出错: {ex.Message}");
        //    }
        //}

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

                treeView.ContextMenu = contextMenu;

                System.Diagnostics.Debug.WriteLine("右键菜单添加成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加右键菜单时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"更新父级子分类列表失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"更新分类属性时出错: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine($"点击预定义按钮: {tagInfo.ButtonName}");

                    // 通过统一管理器获取并执行对应的命令
                    var command = UnifiedCommandManager.GetCommand(tagInfo.ButtonName);
                    if (command != null)
                    {
                        try
                        {
                            command.Invoke();
                            System.Diagnostics.Debug.WriteLine($"成功执行按钮命令: {tagInfo.ButtonName}");
                        }
                        catch (Exception invokeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"执行按钮命令时出错: {invokeEx.Message}");
                            System.Windows.MessageBox.Show($"执行命令 '{tagInfo.ButtonName}' 时出错: {invokeEx.Message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"未找到按钮 '{tagInfo.ButtonName}' 对应的命令");
                        System.Windows.MessageBox.Show($"未找到按钮 '{tagInfo.ButtonName}' 对应的命令");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理预定义按钮点击事件时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("设置数据库类型为: " + _currentDatabaseType);
                System.Diagnostics.Debug.WriteLine("=== 开始加载CAD数据库 ===");
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
                //await LoadAndDisplayCategoryTreeAsync();

                // System.Windows.MessageBox.Show("CAD数据库加载成功");
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
                    System.Diagnostics.Debug.WriteLine("初始化新建主分类界面");
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

                        System.Diagnostics.Debug.WriteLine($"初始化添加子分类界面，父节点: {_selectedCategoryNode.DisplayText}");
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

                    System.Diagnostics.Debug.WriteLine($"初始化编辑分类界面: {_selectedCategoryNode.DisplayText}");
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
                System.Diagnostics.Debug.WriteLine($"展开所有节点失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"折叠所有节点失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 添加图元按钮点击事件
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
                    InitializeFilePropertiesGrid();
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
            try
            {
                if (_currentNodeId <= 0 || _currentNodeType != "Graphic")
                {
                    System.Windows.MessageBox.Show("请先选择一个图元");
                    return;
                }

                // 确认删除
                var result = System.Windows.MessageBox.Show("确定要删除选中的图元吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    return;
                }

                if (_currentDatabaseType == "CAD")
                {
                    await _databaseManager.DeleteCadGraphicAsync(_currentNodeId);
                }
                else if (_currentDatabaseType == "SW")
                {
                    await _databaseManager.DeleteSwGraphicAsync(_currentNodeId);
                }

                System.Windows.MessageBox.Show("图元删除成功");

                // 重新加载分类树
                //await LoadAndDisplayCategoryTreeAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"删除图元时出错: {ex.Message}");
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
        private void InitializeFilePropertiesGrid()
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
                    Env.Editor.WriteMessage("保存文件属性失败");
                }
                else
                {
                    Env.Editor.WriteMessage("保存文件属性到数据库:成功");
                }

                //获取文件属性ID
                _currentFileAttribute = await _databaseManager.GetFileAttributeAsync(_currentFileStorage.DisplayName);
                if (_currentFileAttribute.Id == null)
                {
                    Env.Editor.WriteMessage("获取文件属性ID失败");
                    // 发生异常，需要回滚操作
                    await FileManager.RollbackFileUpload(_databaseManager, uploadedFiles, _currentFileStorage, _currentFileAttribute);
                    return;
                }
                _currentFileStorage.FileAttributeId = _currentFileAttribute.Id;  // 7. 更新文件记录中的属性ID
               
                var fileResult = await _databaseManager.AddFileStorageAsync(_currentFileStorage);//新加文件到数据库中
                if (fileResult != 0)
                {
                    Env.Editor.WriteMessage("保存文件记录到数据库:失败");
                    // 发生异常，需要回滚操作
                    await FileManager.RollbackFileUpload(_databaseManager, uploadedFiles, _currentFileStorage, _currentFileAttribute);
                    return;
                }
                else
                {
                    Env.Editor.WriteMessage("保存文件记录到数据库:成功");
                }
                ;
                _currentFileStorage= await _databaseManager.GetFileStorageAsync(_currentFileStorage.FileName);//获取文件ID
                _currentFileAttribute.FileStorageId= _currentFileStorage.Id;//文件属性ID

                await _databaseManager.UpdateFileAttributeAsync(_currentFileAttribute);//更新文件属性
                // 8. 处理标签信息
                await ProcessFileTags(_currentFileStorage.Id, properties);

                // 9. 更新分类统计
                await _databaseManager.UpdateCategoryStatisticsAsync(
                    _currentFileStorage.CategoryId,
                    _currentFileStorage.CategoryType);

                // 如果所有操作都成功，标记事务成功
                transactionSuccess = true;
                // 11. 刷新分类树和界面显示
                //await RefreshCategoryTreeAndDisplayAsync();
                // 替换为：
                await RefreshCurrentCategoryFilesAsync();
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

            //try
            //{
            //    if (string.IsNullOrEmpty(_selectedFilePath) || _selectedCategoryNode == null)
            //    {
            //        throw new Exception("文件路径或分类节点为空");
            //    }

            //    // 1. 创建文件存储对象
            //    var fileInfo = new FileInfo(_selectedFilePath);
            //    string fileExtension = fileInfo.Extension.ToLower();
            //    string fileName = fileInfo.Name;
            //    string displayName = Path.GetFileNameWithoutExtension(fileName);

            //    _currentFileStorage = new FileStorage
            //    {
            //        CategoryId = _selectedCategoryNode.Id,
            //        CategoryType = _selectedCategoryNode.Level == 0 ? "main" : "sub",
            //        FileName = fileName,
            //        DisplayName = displayName,
            //        FileType = fileExtension,
            //        FileSize = fileInfo.Length,
            //        IsPreview = FileManager.IsPreviewFile(fileExtension),
            //        Version = 1,
            //        IsActive = true,
            //        IsPublic = true,
            //        CreatedBy = Environment.UserName,
            //        UpdatedBy = Environment.UserName,
            //        Title = displayName,
            //        CreatedAt = DateTime.Now,
            //        UpdatedAt = DateTime.Now
            //    };

            //    // 2. 生成存储文件名和路径
            //    string storedFileName = $"{Guid.NewGuid()}{fileExtension}";
            //    string categoryPath = Path.Combine(AppPath, "FileStorage", _currentFileStorage.CategoryType, _selectedCategoryNode.Id.ToString());


            //    // 确保存储目录存在
            //    if (!Directory.Exists(categoryPath))
            //    {
            //        Directory.CreateDirectory(categoryPath);
            //    }

            //    string storedFilePath = Path.Combine(categoryPath, storedFileName);
            //    _currentFileStorage.FileStoredName = storedFileName;
            //    _currentFileStorage.FilePath = storedFilePath;

            //    // 3. 复制主文件到存储位置
            //    File.Copy(_selectedFilePath, storedFilePath, true);

            //    // 4. 如果有预览图片，也复制预览图片
            //    if (!string.IsNullOrEmpty(_selectedPreviewImagePath))
            //    {
            //        var previewInfo = new FileInfo(_selectedPreviewImagePath);
            //        string previewStoredName = $"{Guid.NewGuid()}{previewInfo.Extension}";
            //        string previewStoredPath = Path.Combine(categoryPath, previewStoredName);
            //        File.Copy(_selectedPreviewImagePath, previewStoredPath, true);

            //        _currentFileStorage.PreviewImageName = previewStoredName;
            //        _currentFileStorage.PreviewImagePath = previewStoredPath;
            //    }
            //    // 5. 计算文件哈希值
            //    using (var fileStream = File.OpenRead(storedFilePath))
            //    {
            //        _currentFileStorage.FileHash = await FileManager.CalculateFileHashAsync(fileStream);
            //    }

            //    // 7. 创建文件属性对象
            //    _currentFileAttribute = new FileAttribute
            //    {
            //        //FileStorageId = _currentFileStorage.Id,
            //        FileName = _currentFileStorage.FileName,
            //        CreatedAt = DateTime.Now,
            //        UpdatedAt = DateTime.Now
            //    };

            //    // 8. 从属性编辑网格中获取属性值
            //    var properties = CategoryPropertiesDataGrid.ItemsSource as List<CategoryPropertyEditModel>;
            //    if (properties != null)
            //    {
            //        foreach (var property in properties)
            //        {
            //            SetFileAttributeProperty(_currentFileAttribute, property.PropertyName1, property.PropertyValue1);
            //            SetFileAttributeProperty(_currentFileAttribute, property.PropertyName2, property.PropertyValue2);
            //        }
            //    }
            //    _currentFileAttribute.FileStorageId = _currentFileStorage.Id;// 关联文件存储
            //    // 9. 保存文件属性到数据库
            //    int attributeResult = await _databaseManager.AddFileAttributeAsync(_currentFileAttribute);
            //    if (attributeResult <= 0)
            //    {
            //        throw new Exception("保存文件属性失败");
            //    }
            //    // 10. 更新文件记录中的属性ID
            //    _currentFileStorage.FileAttributeId = _currentFileAttribute.Id;
            //    // 6. 保存文件存储信息到数据库
            //    var fileResult = _databaseManager.AddFileStorageAsync(_currentFileStorage);
            //    if (fileResult.IsCompleted)
            //    {
            //        throw new Exception("保存文件信息失败");
            //    }

            //    //await _databaseManager.UpdateFileStorageAsync(_currentFileStorage);
            //    // 11. 处理标签信息
            //    await ProcessFileTags(_currentFileStorage.Id, properties);
            //    // 12. 更新分类统计
            //    await _databaseManager.UpdateCategoryStatisticsAsync(
            //        _currentFileStorage.CategoryId,
            //        _currentFileStorage.CategoryType);

            //    if (_currentFileStorage == null)
            //    {
            //        throw new Exception("没有待保存的文件信息");
            //    }
            //    MessageBox.Show($"文件已成功上传并保存到数据库", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception($"文件上传和数据库保存失败: {ex.Message}", ex);
            //}
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
                    case "显示名称":
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
                        _currentFileStorage.IsPublic = boolValue;
                        break;
                    case "描述":
                        _currentFileStorage.Description = propertyValue;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置属性 {propertyName} 时出错: {ex.Message}");
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
                        await _databaseManager.AddFileTagAsync(tag);
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
                        await _databaseManager.AddFileTagAsync(tag);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理文件标签时出错: {ex.Message}");
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
        /// 修改上传文件的方法
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="categoryType"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private async Task UploadFileToCategoryAsync(int categoryId, string categoryType, string filePath)
        {
            try
            {
                if (_fileManager == null)
                {
                    throw new Exception("文件管理器未初始化");
                }

                string fileName = Path.GetFileName(filePath);
                using (var fileStream = File.OpenRead(filePath))
                {
                    var fileRecord = await _fileManager.UploadFileAsync(_databaseManager,
                        categoryId,
                        categoryType,
                        fileName,
                        fileStream,
                        $"上传的文件: {fileName}",
                        Environment.UserName);

                    MessageBox.Show($"文件上传成功: {fileRecord.FileName}", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"文件上传失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 文件属性编辑模型
        /// </summary>
        public class FilePropertyModel
        {
            public string PropertyName { get; set; }
            public string PropertyValue { get; set; }
            public string Description { get; set; }
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
                        System.Diagnostics.Debug.WriteLine($"已刷新 {header} 分类的文件显示");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新当前分类文件显示时出错: {ex.Message}");
            }
        }
        /// <summary>
        /// 刷新分类树和界面显示
        /// </summary>
        private async Task RefreshCategoryTreeAndDisplayAsync()
        {
            try
            {
                // 1. 刷新分类树
                await RefreshCategoryTreeAsync();

                // 2. 如果当前选中的分类节点是文件所在的分类，刷新该分类的显示
                if (_selectedCategoryNode != null)
                {
                    await RefreshCurrentCategoryDisplayAsync(_selectedCategoryNode);
                }

                // 3. 清空文件上传界面
                ClearFileUploadInterface();

                System.Diagnostics.Debug.WriteLine("分类树和界面显示刷新完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新分类树和界面显示时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"刷新当前分类显示时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"刷新主分类显示: {categoryNode.DisplayText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新主分类显示时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"刷新子分类显示: {categoryNode.DisplayText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新子分类显示时出错: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine($"刷新了 {currentTabHeader} 面板的文件显示");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新子分类文件显示时出错: {ex.Message}");
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

                System.Diagnostics.Debug.WriteLine("架构树刷新完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新架构树时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"展开树节点时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"获取当前选中TabItem标题时出错: {ex.Message}");
                return string.Empty;
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

                    System.Diagnostics.Debug.WriteLine($"尝试连接到 {serverIP}:{port}");

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

                System.Diagnostics.Debug.WriteLine("设置已保存到配置文件");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从配置文件加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // 从配置文件加载
                _serverIP = Properties.Settings.Default.ServerIP;
                _serverPort = Properties.Settings.Default.ServerPort;
                _databaseName = Properties.Settings.Default.DatabaseName;
                _username = Properties.Settings.Default.Username;
                _password = Properties.Settings.Default.Password;
                _storagePath = Properties.Settings.Default.StoragePath;
                _useDPath = Properties.Settings.Default.UseDPath;
                _autoSync = Properties.Settings.Default.AutoSync;
                _syncInterval = Properties.Settings.Default.SyncInterval;

                // 更新UI控件
                TextBox_Set_ServiceIP.Text = _serverIP;
                TextBox_Set_ServicePort.Text = _serverPort.ToString();
                TextBox_Set_DatabaseName.Text = _databaseName;
                TextBox_Set_Username.Text = _username;
                PasswordBox_Set_Password.Text = _password;
                TextBox_Set_StoragePath.Text = _storagePath;
                CheckBox_UseDPath.IsChecked = _useDPath;
                CheckBox_AutoSync.IsChecked = _autoSync;
                TextBox_SyncInterval.Text = _syncInterval.ToString();

                System.Diagnostics.Debug.WriteLine("设置已从配置文件加载");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置时出错: {ex.Message}");
                // 使用默认值
                TextBox_Set_ServiceIP.Text = "localhost";
                TextBox_Set_ServicePort.Text = "3306";
                TextBox_Set_DatabaseName.Text = "cad_sw_library";
                TextBox_Set_Username.Text = "root";
                PasswordBox_Set_Password.Text = "root";
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

                // 刷新分类树
                await RefreshCategoryTreeAsync();

                System.Diagnostics.Debug.WriteLine("数据库连接已重新初始化");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重新初始化数据库时出错: {ex.Message}");
                MessageBox.Show($"重新初始化数据库失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// 在WpfMainWindow.xaml.cs中添加端口验证方法
        /// </summary>
        /// <param name="portText"></param>
        /// <returns></returns>
        private bool IsValidPort(string portText)
        {
            if (string.IsNullOrEmpty(portText))
                return false;

            if (int.TryParse(portText, out int port))
            {
                return port > 0 && port <= 65535;
            }
            return false;
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

    /// <summary>
    /// 管理操作类型枚举
    /// </summary>
    public enum ManagementOperationType
    {
        None,
        AddCategory,
        AddSubcategory
    }
}
