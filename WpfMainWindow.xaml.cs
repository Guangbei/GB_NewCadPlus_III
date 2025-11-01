using Microsoft.VisualBasic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using TabControl = System.Windows.Controls.TabControl;
using TreeView = System.Windows.Controls.TreeView;
using UserControl = System.Windows.Controls.UserControl;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// WpfMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WpfMainWindow : UserControl
    {
        /// <summary>
        /// 添加数据库管理器
        /// </summary>
        private DatabaseManager _databaseManager;

        /// <summary>
        /// 是否使用数据库模式
        /// </summary>
        private bool _useDatabaseMode = false;

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
        private object _currentSelectedNode = null;

        /// <summary>
        /// 用于显示分类树的TreeView控件
        /// </summary>
        private System.Windows.Controls.TreeView _categoryTreeView;

        /// <summary>
        /// 数据库连接字符串（应该从配置文件读取）
        /// </summary>
        private readonly string _connectionString = "Server=localhost;Database=cad_sw_library;Uid=root;Pwd=root;";

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

        /// <summary>
        /// WpfMainWindow主界面
        /// </summary>
        public WpfMainWindow()
        {
            InitializeComponent();//初始化界面
            InitializeDatabase();//初始化数据库
            NewTjLayer();//初始化图层
            Loaded += WpfMainWindow_Loaded;//加载按钮
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async void InitializeDatabase()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始初始化数据库连接...");
                _databaseManager = new DatabaseManager(_connectionString);

                if (_databaseManager.IsDatabaseAvailable)
                {
                    _useDatabaseMode = true;
                    System.Diagnostics.Debug.WriteLine("数据库连接成功，使用数据库模式");
                }
                else
                {
                    _useDatabaseMode = false;
                    System.Diagnostics.Debug.WriteLine("数据库连接失败，使用Resources文件夹模式");
                }
            }
            catch (Exception ex)
            {
                _useDatabaseMode = false;
                System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}，使用Resources文件夹模式");
            }
        }

        /// <summary>
        /// 窗口初始化时运行加载项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WpfMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取预览Viewbox的引用
            previewViewbox = FindVisualChild<Viewbox>(this, "预览Viewbox") ??
                             FindVisualChild<Viewbox>(this, "Viewbox");

            // 直接通过名称查找TabControl
            var mainTabControl = FindVisualChild<TabControl>(this, "MainTabControl");
            if (mainTabControl != null)
            {
                // 绑定事件
                mainTabControl.SelectionChanged += TabControl_SelectionChanged;
                System.Diagnostics.Debug.WriteLine("TabControl事件绑定成功");//测试
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("未找到名称为MainTabControl的控件");
            }

            // 初始化数据库

            InitializeDatabaseAsync();

            Load();
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                _databaseManager = new DatabaseManager(_connectionString);///创建数据库管理器
                System.Diagnostics.Debug.WriteLine("数据库初始化成功");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}");
                System.Windows.MessageBox.Show($"数据库初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// TabControl选择改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("TabControl选择改变事件触发");

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
            {
                string header = selectedTab.Header.ToString().Trim();//获取TabItem的标题
                System.Diagnostics.Debug.WriteLine($"选中的TabItem: {header}");

                // 检查是否是嵌套的TabItem（如"图元集"、"图层管理"等）
                if (header.Contains("图元集") || header.Contains("图层管理"))
                {
                    // 如果是嵌套的TabItem，需要找到其父级TabItem
                    TabItem parentTabItem = FindParentTabItem(selectedTab);
                    if (parentTabItem != null)
                    {
                        string parentHeader = parentTabItem.Header.ToString().Trim();
                        System.Diagnostics.Debug.WriteLine($"父级TabItem: {parentHeader}");

                        // 从数据库加载按钮
                        LoadButtonsForCategoryFromDatabase(parentHeader, selectedTab);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("未找到父级TabItem");
                    }
                }
                else
                {
                    // 如果是主TabItem，直接加载
                    // 检查这个TabItem是否有子TabControl
                    TabControl childTabControl = FindVisualChild<TabControl>(selectedTab, null);
                    if (childTabControl != null)
                    {
                        // 如果有子TabControl，加载第一个TabItem的内容
                        if (childTabControl.Items.Count > 0 && childTabControl.Items[0] is TabItem firstChildTab)
                        {
                            LoadButtonsForTabItem(selectedTab, header);
                        }
                    }
                    else
                    {
                        // 没有子TabControl，直接加载
                        LoadButtonsForTabItem(selectedTab, header);
                    }
                }
            }
        }

        /// <summary>
        /// 从数据库加载指定分类的按钮
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <param name="tabItem">目标TabItem</param>
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
                System.Windows.MessageBox.Show($"从数据库加载{categoryName}按钮时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找TabItem的父级TabItem
        /// </summary>
        /// <param name="tabItem"></param>
        /// <returns></returns>
        private TabItem FindParentTabItem(TabItem tabItem)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(tabItem);
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
        /// <param name="tabItem"></param>
        /// <param name="folderName"></param>
        private void LoadButtonsForTabItem(TabItem tabItem, string folderName)
        {
            System.Diagnostics.Debug.WriteLine($"尝试加载 {folderName} 的按钮");

            // 检查是否已经加载过
            if (loadedTabItems.ContainsKey(folderName) && loadedTabItems[folderName])
            {
                System.Diagnostics.Debug.WriteLine($"{folderName} 已经加载过，跳过");
                return;
            }

            try
            {
                // 根据TabItem的Header找到对应的面板
                //WrapPanel panel = FindButtonPanelByTabHeader(tabItem, folderName);
                // 查找目标面板 - 需要处理嵌套的TabControl结构
                WrapPanel panel = FindTargetPanel(tabItem, folderName);
                if (panel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"找到面板，开始加载按钮");
                    LoadButtonsForItem(folderName, panel);
                    // 标记为已加载
                    loadedTabItems[folderName] = true;
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
                System.Windows.MessageBox.Show($"加载{folderName}按钮时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找目标面板 - 处理嵌套TabControl结构
        /// </summary>
        /// <param name="tabItem"></param>
        /// <param name="folderName"></param>
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
        /// <param name="folderName"></param>
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
        /// <param name="categoryName">分类名称（与TabItem的Header相同）</param>
        /// <param name="panel">要添加按钮的面板</param>
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
        /// <param name="folderName">分类名称</param>
        /// <param name="panel">目标面板</param>
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
                var subcategories = await _databaseManager.GetCadSubcategoriesByCategoryIdAsync(category.id);
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
                    System.Diagnostics.Debug.WriteLine($"处理子分类: {subcategory.display_name}");

                    // 为每个子分类创建一个带边框和背景色的区域
                    Border sectionBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Gray),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(5),
                        Margin = new Thickness(0, 5, 0, 10),
                        Width = 300,
                        Background = new SolidColorBrush(backgroundColors[colorIndex % backgroundColors.Count]),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    };

                    // 创建区域内容的StackPanel
                    StackPanel sectionPanel = new StackPanel
                    {
                        Margin = new Thickness(5)
                    };

                    // 添加区域标题
                    TextBlock sectionHeader = new TextBlock
                    {
                        Text = subcategory.display_name,
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 5, 0, 10),
                        Foreground = new SolidColorBrush(Colors.DarkBlue)
                    };
                    sectionPanel.Children.Add(sectionHeader);

                    // 从数据库获取该子分类下的所有图元文件
                    var graphics = await _databaseManager.GetCadGraphicsBySubcategoryIdAsync(subcategory.id);
                    System.Diagnostics.Debug.WriteLine($"在 {subcategory.display_name} 中找到 {graphics.Count} 个图元文件");

                    if (graphics.Count > 0)
                    {
                        // 按显示名称排序
                        graphics.Sort((x, y) => x.display_name.CompareTo(y.display_name));

                        // 按3列分组处理
                        int columns = 3;
                        for (int i = 0; i < graphics.Count; i += columns)
                        {
                            // 创建水平StackPanel用于放置一行按钮
                            StackPanel rowPanel = new StackPanel
                            {
                                Orientation = System.Windows.Controls.Orientation.Horizontal,
                                Margin = new Thickness(0, 0, 0, 5)
                            };

                            // 添加该行的按钮（最多3个）
                            for (int j = 0; j < columns && (i + j) < graphics.Count; j++)
                            {
                                var graphic = graphics[i + j];

                                // 检查是否是预定义的按钮
                                //var commandInfo = ButtonCommandMapper.GetCommandInfo(graphic.display_name);
                                string buttonName = graphic.display_name;
                                // 创建按钮
                                Button btn = new Button
                                {
                                    Content = graphic.display_name,
                                    Width = 88,
                                    Height = 30,
                                    Margin = new Thickness(0, 0, 5, 0),
                                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                                    Tag = graphic // 存储完整的图元信息
                                };

                                // 检查是否是预定义的按钮
                                if (UnifiedButtonCommandManager.IsPredefinedButton(buttonName))
                                {
                                    // 如果是预定义按钮
                                    btn.Tag = new ButtonTagCommandInfo
                                    {
                                        Type = "Predefined",
                                        ButtonName = buttonName,
                                        Graphic = graphic
                                    };
                                    btn.Click += PredefinedButton_Click;
                                }
                                else
                                {
                                    // 如果是普通图元按钮，存储图元信息
                                    btn.Tag = new ButtonTagCommandInfo
                                    {
                                        Type = "Graphic",
                                        ButtonName = buttonName,
                                        Graphic = graphic
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从数据库加载按钮时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从Resources文件夹加载按钮（支持命令映射）
        /// </summary>
        /// <param name="folderName">文件夹名称</param>
        /// <param name="panel">目标面板</param>
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
                            Margin = new Thickness(0, 5, 0, 10),//间隔
                            Width = 300,
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
                            FontSize = 14,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(0, 5, 0, 10),
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
                                        Height = 30,//按钮高度
                                        Margin = new Thickness(0, 0, 5, 0), // 按钮右侧间隔5
                                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left,//水平居左
                                        VerticalAlignment = System.Windows.VerticalAlignment.Top,//垂直居上
                                        Tag = fullPath // 将完整路径存储在Tag属性中
                                    };

                                    // 检查是否是预定义的按钮
                                    if (UnifiedButtonCommandManager.IsPredefinedButton(buttonName))
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
        /// 提取字符串中的中文字符，去除所有符号与英文字母
        /// </summary>
        /// <param name="input">输入字符串</param>
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
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void DynamicButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查发送者是否为按钮
                if (sender is Button btn)
                {
                    if (_useDatabaseMode && btn.Tag is CadGraphic cadGraphic)
                    {
                        // 数据库模式：处理数据库图元
                        System.Diagnostics.Debug.WriteLine($"点击了数据库图元按钮: {cadGraphic.display_name}");
                        ExecuteDynamicButtonActionFromDatabase(cadGraphic);
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
        /// <param name="buttonName">按钮名称</param>
        /// <param name="filePath">文件路径</param>
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
        /// <param name="cadGraphic">图元信息</param>
        private async void ExecuteDynamicButtonActionFromDatabase(CadGraphic cadGraphic)
        {
            try
            {
                // 1. 显示预览图
                ShowPreviewImageFromDatabase(cadGraphic);

                // 2. 获取图元属性
                var graphicAttribute = await _databaseManager.GetCadGraphicAttributeByGraphicIdAsync(cadGraphic.Id);

                // 3. 设置相关变量
                SetRelatedVariablesFromDatabase(cadGraphic, graphicAttribute);

                // 4. 调用AutoCAD命令插入块
                InsertBlockToAutoCADFromDatabase(cadGraphic, graphicAttribute);
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
        /// <param name="cadBlock">图元信息</param>
        private void ShowPreviewImageFromDatabase(CadGraphic cadGraphic)
        {
            try
            {
                // 如果没有预览Viewbox，直接返回
                if (previewViewbox == null) return;

                // 清空现有的预览内容
                previewViewbox.Child = null;

                // 检查预览图路径是否存在
                if (!string.IsNullOrEmpty(cadGraphic.preview_image_path) && System.IO.File.Exists(cadGraphic.preview_image_path))
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
                    bitmap.UriSource = new Uri(cadGraphic.preview_image_path);
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
        /// <param name="cadBlock">图元信息</param>
        /// <param name="blockAttribute">图元属性</param>
        private void SetRelatedVariablesFromDatabase(CadGraphic cadGraphic, CadGraphicAttribute cadGraphicAttribute)
        {
            try
            {
                // 根据图元属性设置变量
                if (cadGraphicAttribute != null)
                {
                    VariableDictionary.entityRotateAngle = (double)(cadGraphicAttribute.Angle ?? 0);
                    VariableDictionary.btnFileName = cadGraphic.file_name;
                    VariableDictionary.btnBlockLayer = cadGraphicAttribute.layer_name ?? "TJ(工艺专业GY)";
                    VariableDictionary.layerColorIndex = cadGraphicAttribute.color_index ?? 40;

                    // 设置其他属性
                    VariableDictionary.textbox_S_Width = cadGraphicAttribute.Width?.ToString();
                    VariableDictionary.textbox_S_Height = cadGraphicAttribute.Height?.ToString();
                    VariableDictionary.textBox_S_CirDiameter = (double?)cadGraphicAttribute.Length;
                }
                else
                {
                    // 默认值
                    VariableDictionary.entityRotateAngle = 0;
                    VariableDictionary.btnFileName = cadGraphic.file_name;
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
        /// <param name="cadBlock">图元信息</param>
        /// <param name="blockAttribute">图元属性</param>
        private void InsertBlockToAutoCADFromDatabase(CadGraphic cad_Graphic, CadGraphicAttribute cadGraphicAttribute)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    Editor ed = doc.Editor;

                    // 使用SendStringToExecute发送命令
                    string command = $"_INSERT_BLOCK \"{cad_Graphic.file_path}\" \"{cad_Graphic.display_name}\"\n";
                    doc.SendStringToExecute(command, true, false, false);

                    System.Diagnostics.Debug.WriteLine($"已发送插入命令: {cad_Graphic.display_name}");
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
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        /// <param name="buttonName">按钮名称</param>
        /// <param name="filePath">文件完整路径</param>
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
        /// <param name="dwgFilePath">dwg文件路径</param>
        /// <param name="buttonName">按钮名称</param>
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
        /// <typeparam name="T">要查找的元素类型</typeparam>
        /// <param name="parent">父元素</param>
        /// <param name="childName">子元素名称</param>
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
            var command = UnifiedButtonCommandManager.GetCommandForButton("上");
            command?.Invoke();
        }

        private void 右上_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("右上");
            command?.Invoke();
        }

        private void 右_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("右");
            command?.Invoke();
        }

        private void 右下_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("右下");
            command?.Invoke();

        }

        private void 下_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("下");
            command?.Invoke();
        }

        private void 左下_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("左下");
            command?.Invoke();
        }

        private void 左_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("左");
            command?.Invoke();
        }

        private void 左上_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("左上");
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



        /// <summary>
        /// 公共调用DBTextLabel方法
        /// </summary>
        /// <param name="commandName">命令名称</param>
        /// <param name="btnFileName">按键名</param>
        /// <param name="btnBlockLayer">图层名称</param>
        /// <param name="layerColorIndex">图层颜色</param>
        /// <param name="rotateAngle">图元角度</param>
        private void ExecuteProcessCommand(string commandName, string btnFileName, string btnBlockLayer, int layerColorIndex, double rotateAngle)
        {
            try
            {
                VariableDictionary.entityRotateAngle = rotateAngle;
                VariableDictionary.btnFileName = btnFileName;
                VariableDictionary.btnBlockLayer = btnBlockLayer;//设置为被插入的图层名
                VariableDictionary.layerColorIndex = layerColorIndex;//设置为被插入的图层颜色

                Env.Document.SendStringToExecute("DBTextLabel ", false, false, false);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"执行命令时出错: {ex.Message}");
            }
        }

        #region CAD\SW 管理员数据库操作

        #region CAD\SW 分类树

        /// <summary>
        /// 加载并显示分类树
        /// </summary>
        private async Task LoadAndDisplayCategoryTreeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== 开始加载分类树，数据库类型: {_currentDatabaseType} ===");
                // 查找显示区域的Grid（Grid.Row="2"）
                // 首先尝试通过可视化树查找
                Grid displayGrid = null;


                // 方法3: 手动遍历查找Grid.Row="2"的Grid
                if (displayGrid == null)
                {
                    displayGrid = FindGridByRow(this, 2);
                    System.Diagnostics.Debug.WriteLine("方法3查找结果: " + (displayGrid != null ? "找到" : "未找到"));
                }
                if (displayGrid != null)
                {
                    System.Diagnostics.Debug.WriteLine("成功找到显示区域Grid");
                    // 清空显示区域
                    displayGrid.Children.Clear();
                    System.Diagnostics.Debug.WriteLine("已清空显示区域");

                    // 创建TreeView来显示分类树
                    _categoryTreeView = new TreeView
                    {
                        Margin = new Thickness(0, 5, 0, 5),
                        Background = new SolidColorBrush(Colors.LightBlue),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Colors.Black),
                    };

                    System.Diagnostics.Debug.WriteLine("创建TreeView完成");
                    if (_currentDatabaseType == "CAD")
                    {
                        // 加载CAD分类
                        await LoadCadCategoriesAsync(_categoryTreeView);
                        // 添加TreeView的选择事件
                        _categoryTreeView.SelectedItemChanged += CategoryTreeView_SelectedItemChanged;

                    }
                    else if (_currentDatabaseType == "SW")
                    {
                        // 加载SW分类
                        await LoadSwCategoriesAsync(_categoryTreeView);
                        // 添加TreeView的选择事件
                        _categoryTreeView.SelectedItemChanged += CategoryTreeView_SelectedItemChanged;
                    }
                    // 检查TreeView是否已正确填充
                    System.Diagnostics.Debug.WriteLine($"TreeView项目数量: {_categoryTreeView.Items.Count}");

                    // 添加TreeView到显示区域
                    displayGrid.Children.Clear();
                    displayGrid.Children.Add(_categoryTreeView);
                    // 添加右键菜单
                    AddContextMenuToTreeView(_categoryTreeView);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载分类树时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                System.Windows.MessageBox.Show($"加载分类树时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过Row索引查找Grid
        /// </summary>
        private Grid FindGridByRowIndex(DependencyObject parent, int targetRow)
        {
            System.Diagnostics.Debug.WriteLine($"开始查找Row={targetRow}的Grid");

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 检查是否是Grid且有Grid.Row属性
                if (child is Grid grid)
                {
                    var row = Grid.GetRow(grid);
                    System.Diagnostics.Debug.WriteLine($"找到Grid，Name: {grid.Name}, Row={row}");
                    if (row == targetRow)
                    {
                        System.Diagnostics.Debug.WriteLine($"找到目标Grid，Row={targetRow}");
                        return grid;
                    }
                }

                // 递归查找子元素
                var result = FindGridByRowIndex(child, targetRow);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

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
                    System.Diagnostics.Debug.WriteLine($"处理分类: {category.display_name} (ID: {category.id})");

                    // 创建分类节点
                    TreeViewItem categoryItem = new TreeViewItem
                    {
                        Header = category.display_name,// 显示分类名称
                        Tag = new { Type = "Category", Id = category.id, Object = category }// 设置Tag属性
                    };
                    System.Diagnostics.Debug.WriteLine($"创建分类节点: {category.display_name}");

                    // 加载子分类
                    await LoadCadSubcategoriesAsync(category.id, categoryItem, 0);

                    //_sort_order += _sort_order + 1;

                    treeView.Items.Add(categoryItem);
                    System.Diagnostics.Debug.WriteLine($"添加分类节点到TreeView: {category.display_name}");
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
                var subcategories = await _databaseManager.GetCadSubcategoriesByParentIdAsync(parentId);
                foreach (var subcategory in subcategories)
                {
                    // 创建子分类节点
                    string indent = new string(' ', level * 2); // 根据层级添加缩进
                    TreeViewItem subcategoryItem = new TreeViewItem
                    {
                        Header = $"{indent}{subcategory.display_name}",
                        Tag = new { Type = "Subcategory", Id = subcategory.id, Object = subcategory }
                    };

                    // 加载图元
                    await LoadCadGraphicsAsync(subcategory.id, subcategoryItem);

                    // 递归加载子子分类
                    await LoadCadSubcategoriesAsync(subcategory.id, subcategoryItem, level + 1);

                    parentItem.Items.Add(subcategoryItem);
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
                var graphics = await _databaseManager.GetCadGraphicsBySubcategoryIdAsync(subcategoryId);
                foreach (var graphic in graphics)
                {
                    TreeViewItem graphicItem = new TreeViewItem
                    {
                        Header = $"    {graphic.file_name}",
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
                        Header = category.display_name,
                        Tag = new { Type = "Category", Id = category.id, Object = category }
                    };

                    // 加载子分类
                    await LoadSwSubcategoriesAsync(category.id, categoryItem, 0);

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
                        Header = $"{indent}{subcategory.display_name}",
                        Tag = new { Type = "Subcategory", Id = subcategory.id, Object = subcategory }
                    };

                    // 加载图元
                    await LoadSwGraphicsAsync(subcategory.id, subcategoryItem);

                    // 递归加载子子分类
                    await LoadSwSubcategoriesAsync(subcategory.id, subcategoryItem, level + 1);

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
                        Header = $"    {graphic.file_name}",
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

        /// <summary>
        /// TreeView选中项改变事件
        /// </summary>
        private void CategoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (_categoryTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag != null)
                {
                    var tagInfo = selectedItem.Tag as dynamic;
                    _currentNodeType = tagInfo.Type;
                    _currentNodeId = tagInfo.Id;
                    _currentSelectedNode = tagInfo.Object;

                    System.Diagnostics.Debug.WriteLine($"选中节点: 类型={_currentNodeType}, ID={_currentNodeId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TreeView选中项改变时出错: {ex.Message}");
            }
        }

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加右键菜单时出错: {ex.Message}");
            }
        }


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
            public CadGraphic Graphic { get; set; }
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

            public double RotateAngle { get; set; }

            public int LayerColorIndex {  get; set; }
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PredefinedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is ButtonTagCommandInfo tagInfo)
                {
                    System.Diagnostics.Debug.WriteLine($"点击预定义按钮: {tagInfo.ButtonName}");

                    // 通过统一管理器获取并执行对应的命令
                    var command = UnifiedButtonCommandManager.GetCommandForButton(tagInfo.ButtonName);
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
            var command = UnifiedButtonCommandManager.GetCommandForButton("纯化水");
            command?.Invoke();//执行命令
        }

        private void 纯蒸汽_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("纯蒸汽");
            command?.Invoke();
        }

        private void 注射用水_Btn_Click(object sender, RoutedEventArgs e)
        {
           
            var command = UnifiedButtonCommandManager.GetCommandForButton("注射用水");
            command?.Invoke();
        }

        private void 凝结回水_Btn_Click(object sender, RoutedEventArgs e)
        {
           
            var command = UnifiedButtonCommandManager.GetCommandForButton("凝结回水");
            command?.Invoke();
        }

        private void 氧气_Btn_Click(object sender, RoutedEventArgs e)
        {
           
            var command = UnifiedButtonCommandManager.GetCommandForButton("氧气");
            command?.Invoke();
        }

        private void 氮气_Btn_Click(object sender, RoutedEventArgs e)
        {
          
            var command = UnifiedButtonCommandManager.GetCommandForButton("氮气");
            command?.Invoke();
        }

        private void 二氧化碳_Btn_Click(object sender, RoutedEventArgs e)
        {
          
            var command = UnifiedButtonCommandManager.GetCommandForButton("二氧化碳");
            command?.Invoke();
        }

        private void 无菌压缩空气_Btn_Click(object sender, RoutedEventArgs e)
        {
           
            var command = UnifiedButtonCommandManager.GetCommandForButton("无菌压缩空气");
            command?.Invoke();
        }

        private void 仪表压缩空气_Btn_Click(object sender, RoutedEventArgs e)
        {
          
            var command = UnifiedButtonCommandManager.GetCommandForButton("仪表压缩空气");
            command?.Invoke();
        }

        private void 低压蒸汽_Btn_Click(object sender, RoutedEventArgs e)
        {
          
            var command = UnifiedButtonCommandManager.GetCommandForButton("低压蒸汽");
            command?.Invoke();
        }

        private void 低温循环上水_Btn_Click(object sender, RoutedEventArgs e)
        {
           
            var command = UnifiedButtonCommandManager.GetCommandForButton("低温循环上水");
            command?.Invoke();
        }

        private void 常温循环上水_Btn_Click(object sender, RoutedEventArgs e)
        {
          
            var command = UnifiedButtonCommandManager.GetCommandForButton("常温循环上水");
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





        /// <summary>
        /// 加载CAD数据库按钮点击事件
        /// </summary>
        private async void LoadCadDatabase_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始加载CAD数据库 ===");
                if (!_useDatabaseMode || _databaseManager == null || !_databaseManager.IsDatabaseAvailable)
                {
                    System.Windows.MessageBox.Show("数据库不可用，请检查数据库连接配置");
                    return;
                }


                // 设置当前数据库类型
                _currentDatabaseType = "CAD";
                System.Diagnostics.Debug.WriteLine("设置数据库类型为: " + _currentDatabaseType);


                // 获取CAD存储路径
                _cadStoragePath = await _databaseManager.GetConfigValueAsync("cad_storage_path");
                if (string.IsNullOrEmpty(_cadStoragePath))
                {
                    _cadStoragePath = System.IO.Path.Combine(AppPath, "CadFiles");
                }

                // 确保存储路径存在
                System.IO.Directory.CreateDirectory(_cadStoragePath);


                // 加载并显示CAD分类树
                await LoadAndDisplayCategoryTreeAsync();

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
                await LoadAndDisplayCategoryTreeAsync();

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

                // 显示输入对话框获取新分类名称
                string categoryName = Interaction.InputBox("请输入新子分类名称:", "添加子分类", "新子分类");

                if (string.IsNullOrEmpty(categoryName))
                {
                    return;
                }

                if (_currentDatabaseType == "CAD")
                {
                    // 创建新的CAD分类
                    var newCategory = new CadCategory
                    {
                        name = categoryName,
                        display_name = categoryName,
                        sort_order = _sort_order,
                        created_at = DateTime.Now,
                    };
                    await _databaseManager.AddCadCategoryAsync(newCategory);
                }
                else if (_currentDatabaseType == "SW")
                {
                    // 创建新的SW分类
                    var newCategory = new SwCategory
                    {
                        name = categoryName,
                        display_name = categoryName,
                        sort_order = _sort_order,
                        created_at = DateTime.Now,

                    };
                    await _databaseManager.AddSwCategoryAsync(newCategory);
                }

                // 重新加载分类树
                await LoadAndDisplayCategoryTreeAsync();

                System.Windows.MessageBox.Show("分类创建成功");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"新建分类时出错: {ex.Message}");
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
                // 判断当前选中的节点是否为分类
                if (_currentNodeId <= 0)
                {
                    System.Windows.MessageBox.Show("请先选择一个分类或子分类");
                    return;
                }

                // 显示输入对话框获取新子分类名称
                string subcategoryName = Microsoft.VisualBasic.Interaction.InputBox("请输入新子分类名称:", "添加子分类", "新子分类");
                if (string.IsNullOrEmpty(subcategoryName))
                {
                    return;
                }

                if (_currentDatabaseType == "CAD")
                {
                    int categoryId = 0;
                    int parentId = 0;

                    // 根据当前选中节点类型确定category_id和parent_id
                    if (_currentNodeType == "Category")
                    {
                        categoryId = _currentNodeId;
                        parentId = 0; // 顶级子分类
                        System.Diagnostics.Debug.WriteLine($"创建顶级子分类: CategoryId={categoryId}, ParentId={parentId}");
                    }
                    else if (_currentNodeType == "Subcategory")
                    {
                        var subcategory = _currentSelectedNode as CadSubcategory;
                        categoryId = subcategory.id;
                        parentId = _currentNodeId; // 当前子分类作为父级
                        System.Diagnostics.Debug.WriteLine($"创建子子分类: CategoryId={categoryId}, ParentId={parentId}");
                    }

                    // 创建新的CAD子分类
                    var newSubcategory = new CadSubcategory
                    {
                        id = categoryId,
                        name = subcategoryName,
                        display_name = subcategoryName,
                        parent_id = parentId,
                        sort_order = 0
                    };

                    System.Diagnostics.Debug.WriteLine($"准备添加子分类: CategoryId={newSubcategory.id}, Name={newSubcategory.name}, ParentId={newSubcategory.parent_id}");
                    await _databaseManager.AddCadSubcategoryAsync(newSubcategory);
                }
                else if (_currentDatabaseType == "SW")
                {
                    int categoryId = 0;
                    int parentId = 0;

                    // 根据当前选中节点类型确定category_id和parent_id
                    if (_currentNodeType == "Category")
                    {
                        categoryId = _currentNodeId;
                        parentId = 0; // 顶级子分类
                        System.Diagnostics.Debug.WriteLine($"创建顶级SW子分类: CategoryId={categoryId}, ParentId={parentId}");
                    }
                    else if (_currentNodeType == "Subcategory")
                    {
                        var subcategory = _currentSelectedNode as SwSubcategory;
                        categoryId = subcategory.id;
                        parentId = _currentNodeId; // 当前子分类作为父级
                        System.Diagnostics.Debug.WriteLine($"创建SW子子分类: CategoryId={categoryId}, ParentId={parentId}");
                    }

                    // 创建新的SW子分类
                    var newSubcategory = new SwSubcategory
                    {
                        id = categoryId,
                        name = subcategoryName,
                        display_name = subcategoryName,
                        parent_id = parentId,
                        sort_order = 0
                    };
                    System.Diagnostics.Debug.WriteLine($"准备添加SW子分类: CategoryId={newSubcategory.id}, Name={newSubcategory.name}, ParentId={newSubcategory.parent_id}");
                    await _databaseManager.AddSwSubcategoryAsync(newSubcategory);
                }

                // 重新加载分类树
                await LoadAndDisplayCategoryTreeAsync();

                System.Windows.MessageBox.Show("子分类添加成功");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加子分类时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 修改菜单项点击事件
        /// </summary>
        private async void Edit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentNodeId <= 0)
                {
                    System.Windows.MessageBox.Show("请先选择一个项目");
                    return;
                }

                // 在DataGrid中显示选中项的详细信息以便编辑
                var dataGrid = FindVisualChild<DataGrid>(this, null); // 需要根据实际XAML调整
                if (dataGrid != null)
                {
                    if (_currentNodeType == "Category")
                    {
                        // 显示分类信息在DataGrid中
                        if (_currentDatabaseType == "CAD")
                        {
                            var category = _currentSelectedNode as CadCategory;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");

                            dataTable.Rows.Add("ID", category.id);
                            dataTable.Rows.Add("名称", category.name);
                            dataTable.Rows.Add("显示名称", category.display_name);
                            dataTable.Rows.Add("排序", category.sort_order);
                            dataTable.Rows.Add("创建时间", category.created_at);
                            dataTable.Rows.Add("更新时间", category.updated_at);

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                        else if (_currentDatabaseType == "SW")
                        {
                            var category = _currentSelectedNode as SwCategory;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");

                            dataTable.Rows.Add("ID", category.id);
                            dataTable.Rows.Add("名称", category.name);
                            dataTable.Rows.Add("显示名称", category.display_name);
                            dataTable.Rows.Add("排序", category.sort_order);
                            dataTable.Rows.Add("创建时间", category.created_at);
                            dataTable.Rows.Add("更新时间", category.updated_at);

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                    }
                    else if (_currentNodeType == "Subcategory")
                    {
                        // 显示子分类信息在DataGrid中
                        if (_currentDatabaseType == "CAD")
                        {
                            var subcategory = _currentSelectedNode as CadSubcategory;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");

                            dataTable.Rows.Add("ID", subcategory.id);
                            dataTable.Rows.Add("分类ID", subcategory.id);
                            dataTable.Rows.Add("名称", subcategory.name);
                            dataTable.Rows.Add("显示名称", subcategory.display_name);
                            dataTable.Rows.Add("父级ID", subcategory.parent_id);
                            dataTable.Rows.Add("排序", subcategory.sort_order);
                            dataTable.Rows.Add("创建时间", subcategory.created_at);
                            dataTable.Rows.Add("更新时间", subcategory.updated_at);

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                        else if (_currentDatabaseType == "SW")
                        {
                            var subcategory = _currentSelectedNode as SwSubcategory;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");

                            dataTable.Rows.Add("ID", subcategory.id);
                            dataTable.Rows.Add("分类ID", subcategory.id);
                            dataTable.Rows.Add("名称", subcategory.name);
                            dataTable.Rows.Add("显示名称", subcategory.display_name);
                            dataTable.Rows.Add("父级ID", subcategory.parent_id);
                            dataTable.Rows.Add("排序", subcategory.sort_order);
                            dataTable.Rows.Add("创建时间", subcategory.created_at);
                            dataTable.Rows.Add("更新时间", subcategory.updated_at);

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                    }
                    else if (_currentNodeType == "Graphic")
                    {
                        // 显示图元信息在DataGrid中
                        if (_currentDatabaseType == "CAD")
                        {
                            var graphic = _currentSelectedNode as CadGraphic;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");

                            dataTable.Rows.Add("ID", graphic.Id);
                            dataTable.Rows.Add("子分类ID", graphic.subcategory_id);
                            dataTable.Rows.Add("文件名", graphic.file_name);
                            dataTable.Rows.Add("显示名称", graphic.display_name);
                            dataTable.Rows.Add("文件路径", graphic.file_path);
                            dataTable.Rows.Add("预览图路径", graphic.preview_image_path);
                            dataTable.Rows.Add("文件大小", graphic.file_size);
                            dataTable.Rows.Add("创建时间", graphic.created_at);
                            dataTable.Rows.Add("更新时间", graphic.updated_at);

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                        else if (_currentDatabaseType == "SW")
                        {
                            var graphic = _currentSelectedNode as SwGraphic;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");

                            dataTable.Rows.Add("ID", graphic.Id);
                            dataTable.Rows.Add("子分类ID", graphic.subcategory_id);
                            dataTable.Rows.Add("文件名", graphic.file_name);
                            dataTable.Rows.Add("显示名称", graphic.display_name);
                            dataTable.Rows.Add("文件路径", graphic.file_path);
                            dataTable.Rows.Add("预览图路径", graphic.preview_image_path);
                            dataTable.Rows.Add("文件大小", graphic.file_size);
                            dataTable.Rows.Add("创建时间", graphic.created_at);
                            dataTable.Rows.Add("更新时间", graphic.updated_at);

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"编辑时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除菜单项点击事件
        /// </summary>
        private async void Delete_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentNodeId <= 0)
                {
                    System.Windows.MessageBox.Show("请先选择一个项目");
                    return;
                }

                // 确认删除
                var result = System.Windows.MessageBox.Show("确定要删除选中的项目吗？这将删除所有相关的子项目。", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    return;
                }

                if (_currentNodeType == "Category")
                {
                    if (_currentDatabaseType == "CAD")
                    {
                        await _databaseManager.DeleteCadCategoryAsync(_currentNodeId);
                    }
                    else if (_currentDatabaseType == "SW")
                    {
                        await _databaseManager.DeleteSwCategoryAsync(_currentNodeId);
                    }
                }
                else if (_currentNodeType == "Subcategory")
                {
                    if (_currentDatabaseType == "CAD")
                    {
                        await _databaseManager.DeleteCadSubcategoryAsync(_currentNodeId);
                    }
                    else if (_currentDatabaseType == "SW")
                    {
                        await _databaseManager.DeleteSwSubcategoryAsync(_currentNodeId);
                    }
                }
                else if (_currentNodeType == "Graphic")
                {
                    if (_currentDatabaseType == "CAD")
                    {
                        await _databaseManager.DeleteCadGraphicAsync(_currentNodeId);
                    }
                    else if (_currentDatabaseType == "SW")
                    {
                        await _databaseManager.DeleteSwGraphicAsync(_currentNodeId);
                    }
                }

                // 重新加载分类树
                await LoadAndDisplayCategoryTreeAsync();

                System.Windows.MessageBox.Show("删除成功");

                // 清除选中状态
                _currentNodeId = 0;
                _currentNodeType = "";
                _currentSelectedNode = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"删除时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加图元按钮点击事件
        /// </summary>
        private async void AddGraphic_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentNodeId <= 0)
                {
                    System.Windows.MessageBox.Show("请先选择一个子分类");
                    return;
                }

                // 打开文件选择对话框
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = _currentDatabaseType == "CAD"
                        ? "DWG文件|*.dwg|所有文件|*.*"
                        : "SLDPRT文件|*.sldprt|所有文件|*.*",
                    Title = "选择图元文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFile = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(selectedFile);
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(selectedFile);

                    // 构造存储路径
                    string storagePath = _currentDatabaseType == "CAD" ? _cadStoragePath : _swStoragePath;
                    string fileStoragePath = System.IO.Path.Combine(storagePath, fileName);

                    // 复制文件到存储路径
                    System.IO.File.Copy(selectedFile, fileStoragePath, true);

                    if (_currentDatabaseType == "CAD")
                    {
                        // 添加CAD图元
                        var newGraphic = new CadGraphic
                        {
                            subcategory_id = _currentNodeId,
                            file_name = fileName,
                            display_name = fileNameWithoutExt,
                            file_path = fileStoragePath,
                            file_size = new System.IO.FileInfo(fileStoragePath).Length
                        };
                        await _databaseManager.AddCadGraphicAsync(newGraphic);
                    }
                    else if (_currentDatabaseType == "SW")
                    {
                        // 添加SW图元
                        var newGraphic = new SwGraphic
                        {
                            subcategory_id = _currentNodeId,
                            file_name = fileName,
                            display_name = fileNameWithoutExt,
                            file_path = fileStoragePath,
                            file_size = new System.IO.FileInfo(fileStoragePath).Length
                        };
                        await _databaseManager.AddSwGraphicAsync(newGraphic);
                    }

                    System.Windows.MessageBox.Show("图元添加成功");

                    // 重新加载分类树
                    await LoadAndDisplayCategoryTreeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加图元时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加预览图按钮点击事件
        /// </summary>
        private async void AddPreviewImage_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentNodeId <= 0)
                {
                    System.Windows.MessageBox.Show("请先选择一个图元");
                    return;
                }

                // 打开文件选择对话框
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
                    Title = "选择预览图文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFile = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(selectedFile);
                    string fileExtension = System.IO.Path.GetExtension(selectedFile);

                    // 构造预览图存储路径
                    string storagePath = _currentDatabaseType == "CAD" ? _cadStoragePath : _swStoragePath;
                    string previewStoragePath = System.IO.Path.Combine(storagePath, "Previews", fileName);

                    // 确保预览图目录存在
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(previewStoragePath));

                    // 复制预览图到存储路径
                    System.IO.File.Copy(selectedFile, previewStoragePath, true);

                    if (_currentNodeType == "Graphic")
                    {
                        if (_currentDatabaseType == "CAD")
                        {
                            // 更新CAD图元的预览图路径
                            var graphic = await _databaseManager.GetCadGraphicByIdAsync(_currentNodeId);
                            if (graphic != null)
                            {
                                graphic.preview_image_path = previewStoragePath;
                                await _databaseManager.UpdateCadGraphicAsync(graphic);
                            }
                        }
                        else if (_currentDatabaseType == "SW")
                        {
                            // 更新SW图元的预览图路径
                            var graphic = await _databaseManager.GetSwGraphicByIdAsync(_currentNodeId);
                            if (graphic != null)
                            {
                                graphic.preview_image_path = previewStoragePath;
                                await _databaseManager.UpdateSwGraphicAsync(graphic);
                            }
                        }
                    }

                    System.Windows.MessageBox.Show("预览图添加成功");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加预览图时出错: {ex.Message}");
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
                await LoadAndDisplayCategoryTreeAsync();
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
                // 实现还原初始值逻辑
                // 这里需要根据具体的数据绑定情况来实现
                System.Windows.MessageBox.Show("已还原到初始值");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"还原初始值时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用属性按钮点击事件
        /// </summary>
        private async void ApplyProperties_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 实现应用属性逻辑
                // 这里需要根据DataGrid中的编辑内容来更新数据库
                System.Windows.MessageBox.Show("属性已应用并保存到数据库");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"应用属性时出错: {ex.Message}");
            }
        }


        #endregion
        #region 电气按键
        private void 横墙电开建筑洞_Btn_Clic(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("横墙电开建洞");
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
            var command = UnifiedButtonCommandManager.GetCommandForButton("吊顶");
            command?.Invoke();
        }

        private void 不吊顶_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("不吊顶");
            command?.Invoke();
        }

        private void 防撞护板_Btn_Clic(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("防撞护板");
            command?.Invoke();
        }

        private void 房间编号_Btn_Clic(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("房间编号");
            command?.Invoke();
        }

        private void 编号检查_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("编号检查");
            command?.Invoke();
        }

        private void 冷藏库降板_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("冷藏库降板");
            command?.Invoke();
        }
        private void 冷冻库降板_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("冷冻库降板");
            command?.Invoke();
        }

        private void 特殊地面做法要求_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("特殊地面做法要求");
            command?.Invoke();
        }

        private void 排水沟_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("排水沟");
            command?.Invoke();
        }
        private void 横墙建筑开洞_Btn_Clic(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("横墙建筑开洞");
            command?.Invoke();
        }

        private void 纵墙建筑开洞_Btn_Click(object sender, RoutedEventArgs e)
        {
            var command = UnifiedButtonCommandManager.GetCommandForButton("纵墙建筑开洞");
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

    }
}
