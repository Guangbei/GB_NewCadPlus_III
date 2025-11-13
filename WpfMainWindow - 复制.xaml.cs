using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Button = System.Windows.Controls.Button;
using DataGrid = System.Windows.Controls.DataGrid;
using MessageBox = System.Windows.MessageBox;
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
        #region  私有字段和属性
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
        private readonly string _connectionString = "Server=localhost;Database=cad_sw_library;Uid=root;Pwd=root;";

        /// <summary>
        /// 是否使用数据库模式
        /// </summary>
        private bool _useDatabaseMode = true;

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
        }

        /// <summary>
        /// 窗口初始化时运行加载项
        /// </summary>
        /// <param Name="sender"></param>
        /// <param Name="e"></param>
        private void WpfMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取预览Viewbox的引用
            previewViewbox = FindVisualChild<Viewbox>(this, "预览Viewbox") ??
                             FindVisualChild<Viewbox>(this, "Viewbox");
            // 直接通过名称查找TabControl
            var mainTabControl = FindVisualChild<TabControl>(this, "MainTabControl");
            if (mainTabControl != null)
            {
                mainTabControl.SelectionChanged += TabControl_SelectionChanged; // 绑定事件
                System.Diagnostics.Debug.WriteLine("TabControl事件绑定成功");//测试
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("未找到名称为MainTabControl的控件");
            }
            InitializeDatabase();//初始化数据库
            InitializeCategoryTreeAsync(); // 初始化架构树
            Load();
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
                var categories = await _databaseManager.GetAllCadCategoriesAsync();  // 测试数据库连接
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
                    var graphics = await _databaseManager.GetCadGraphicsBySubcategoryIdAsync(subcategory.Id);
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
                    if (_useDatabaseMode && btn.Tag is CadGraphic cadGraphic)
                    {
                        // 数据库模式：处理数据库图元
                        System.Diagnostics.Debug.WriteLine($"点击了数据库图元按钮: {cadGraphic.DisplayName}");
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
        /// <param Name="cadBlock">图元信息</param>
        private void ShowPreviewImageFromDatabase(CadGraphic cadGraphic)
        {
            try
            {
                // 如果没有预览Viewbox，直接返回
                if (previewViewbox == null) return;

                // 清空现有的预览内容
                previewViewbox.Child = null;

                // 检查预览图路径是否存在
                if (!string.IsNullOrEmpty(cadGraphic.PreviewImagePath) && System.IO.File.Exists(cadGraphic.PreviewImagePath))
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
                    bitmap.UriSource = new Uri(cadGraphic.PreviewImagePath);
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
        private void SetRelatedVariablesFromDatabase(CadGraphic cadGraphic, CadGraphicAttribute cadGraphicAttribute)
        {
            try
            {
                // 根据图元属性设置变量
                if (cadGraphicAttribute != null)
                {
                    VariableDictionary.entityRotateAngle = (double)(cadGraphicAttribute.Angle ?? 0);
                    VariableDictionary.btnFileName = cadGraphic.FileName;
                    VariableDictionary.btnBlockLayer = cadGraphic.LayerName ?? "TJ(工艺专业GY)";
                    VariableDictionary.layerColorIndex = cadGraphic.ColorIndex ?? 40;

                    // 设置其他属性
                    VariableDictionary.textbox_S_Width = cadGraphicAttribute.Width?.ToString();
                    VariableDictionary.textbox_S_Height = cadGraphicAttribute.Height?.ToString();
                    VariableDictionary.textBox_S_CirDiameter = (double?)cadGraphicAttribute.Length;
                }
                else
                {
                    // 默认值
                    VariableDictionary.entityRotateAngle = 0;
                    VariableDictionary.btnFileName = cadGraphic.FileName;
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
        private void InsertBlockToAutoCADFromDatabase(CadGraphic cad_Graphic, CadGraphicAttribute cadGraphicAttribute)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    Editor ed = doc.Editor;

                    // 使用SendStringToExecute发送命令
                    string command = $"_INSERT_BLOCK \"{cad_Graphic.FilePath}\" \"{cad_Graphic.DisplayName}\"\n";
                    doc.SendStringToExecute(command, true, false, false);

                    System.Diagnostics.Debug.WriteLine($"已发送插入命令: {cad_Graphic.DisplayName}");
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示架构树失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 架构树选中项改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CategoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is CategoryTreeNode selectedNode)
                {
                    // 根据选中的节点类型显示相应的属性编辑界面
                    DisplayNodePropertiesForEditing(selectedNode);
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
        /// 初始化分类属性编辑界面（分类）- 带现有数据参考
        /// </summary>
        /// <param name="existingCategories"></param>
        private void InitializeCategoryPropertiesForCategory(List<CadCategory> existingCategories = null)
        {
            var categoryProperties = new List<CategoryPropertyEditModel>// 创建一个列表，用于存储分类属性的编辑模型
            {
                new CategoryPropertyEditModel { PropertyName1 = "名称", PropertyValue1 = "", PropertyName2 = "显示名称", PropertyValue2 = "" },// 创建两个属性编辑模型
                new CategoryPropertyEditModel { PropertyName1 = "排序序号", PropertyValue1 = "0", PropertyName2 = "", PropertyValue2 = "" }// 创建一个属性编辑模型
            };
            CategoryPropertiesDataGrid.ItemsSource = categoryProperties;// 将属性列表绑定到数据网格
        }

        /// <summary>
        /// 初始化子分类属性编辑界面（子分类）- 带现有数据参考
        /// </summary>
        /// <param name="existingSubcategories"></param>
        private void InitializeCategoryPropertiesForSubcategory(List<CadSubcategory> existingSubcategories = null)
        {
            var subcategoryProperties = new List<CategoryPropertyEditModel>
    {
        new CategoryPropertyEditModel { PropertyName1 = "父分类ID", PropertyValue1 = "", PropertyName2 = "名称", PropertyValue2 = "" },
        new CategoryPropertyEditModel { PropertyName1 = "显示名称", PropertyValue1 = "", PropertyName2 = "排序序号", PropertyValue2 = "0" }
    };

            // 如果有现有数据，添加参考信息
            if (existingSubcategories != null && existingSubcategories.Count > 0)
            {
                var sampleSubcategory = existingSubcategories[0]; // 使用第一个作为示例
                subcategoryProperties.Add(new CategoryPropertyEditModel
                {
                    PropertyName1 = "参考示例",
                    PropertyValue1 = $"父ID:{sampleSubcategory.ParentId}",
                    PropertyName2 = "名称",
                    PropertyValue2 = sampleSubcategory.Name
                });
            }

            // 添加空行用于用户输入
            subcategoryProperties.Add(new CategoryPropertyEditModel());

            CategoryPropertiesDataGrid.ItemsSource = subcategoryProperties;
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
            public List<CategoryTreeNode> Children { get; set; } = new List<CategoryTreeNode>();
            public object Data { get; set; } // 存储原始数据对象

            public string DisplayText => string.IsNullOrEmpty(DisplayName) ? Name : DisplayName;

            public CategoryTreeNode(int id, string name, string displayName, int level, int parentId, object data)
            {
                Id = id;
                Name = name;
                DisplayName = displayName;
                Level = level;
                ParentId = parentId;
                Data = data;
            }
        }

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
                var graphics = await _databaseManager.GetCadGraphicsBySubcategoryIdAsync(subcategoryId);
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

        /// <summary>
        /// TreeView选中项改变事件
        /// </summary>
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

        #region 管理员按键处理方法...

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
                    _currentOperation = ManagementOperationType.AddCategory;  // 创建新的CAD分类
                    var existingCategories = await _databaseManager.GetAllCadCategoriesAsync();  // 读取现有的分类数据作为参考
                    InitializeCategoryPropertiesForCategory(existingCategories);
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
                // 判断当前选中的节点是否为分类
                if (_currentNodeId <= 0)
                {
                    System.Windows.MessageBox.Show("请先选择一个分类或子分类");
                    return;
                }

                if (_currentDatabaseType == "CAD")
                {
                    _currentOperation = ManagementOperationType.AddSubcategory;
                    InitializeCategoryPropertiesForSubcategory();
                }

                // 重新加载分类树
                await LoadAndDisplayCategoryTreeAsync();
                //System.Windows.MessageBox.Show("子分类添加成功");
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
                            dataTable.Rows.Add("ID", category.Id);
                            dataTable.Rows.Add("名称", category.Name);
                            dataTable.Rows.Add("显示名称", category.DisplayName);
                            dataTable.Rows.Add("排序", category.SortOrder);
                            dataTable.Rows.Add("创建时间", category.CreatedAt);
                            dataTable.Rows.Add("更新时间", category.UpdatedAt);

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                        else if (_currentDatabaseType == "SW")
                        {
                            var category = _currentSelectedNode as SwCategory;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");
                            dataTable.Rows.Add("ID", category.Id);
                            dataTable.Rows.Add("名称", category.Name);
                            dataTable.Rows.Add("显示名称", category.DisplayName);
                            dataTable.Rows.Add("排序", category.SortOrder);
                            dataTable.Rows.Add("创建时间", category.CreatedAt);
                            dataTable.Rows.Add("更新时间", category.UpdatedAt);
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
                            dataTable.Rows.Add("ID", subcategory.Id);
                            dataTable.Rows.Add("分类ID", subcategory.Id);
                            dataTable.Rows.Add("名称", subcategory.Name);
                            dataTable.Rows.Add("显示名称", subcategory.DisplayName);
                            dataTable.Rows.Add("父级ID", subcategory.ParentId);
                            dataTable.Rows.Add("排序", subcategory.SortOrder);
                            dataTable.Rows.Add("创建时间", subcategory.CreatedAt);
                            dataTable.Rows.Add("更新时间", subcategory.UpdatedAt);
                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                        else if (_currentDatabaseType == "SW")
                        {
                            var subcategory = _currentSelectedNode as SwSubcategory;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");
                            dataTable.Rows.Add("ID", subcategory.Id);
                            dataTable.Rows.Add("分类ID", subcategory.Id);
                            dataTable.Rows.Add("名称", subcategory.Name);
                            dataTable.Rows.Add("显示名称", subcategory.DisplayName);
                            dataTable.Rows.Add("父级ID", subcategory.ParentId);
                            dataTable.Rows.Add("排序", subcategory.SortOrder);
                            dataTable.Rows.Add("创建时间", subcategory.CreatedAt);
                            dataTable.Rows.Add("更新时间", subcategory.UpdatedAt);

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
                            dataTable.Rows.Add("ID", graphic.Id);//Id
                            dataTable.Rows.Add("子分类ID", graphic.Id); //子分类Id
                            dataTable.Rows.Add("文件名", graphic.FileName);//文件名
                            dataTable.Rows.Add("显示名称", graphic.DisplayName);//显示名称
                            dataTable.Rows.Add("文件路径", graphic.FilePath);//文件路径
                            dataTable.Rows.Add("预览图名", graphic.PreviewImageName);//预览图名
                            dataTable.Rows.Add("预览图路径", graphic.PreviewImagePath);//预览图路径
                            dataTable.Rows.Add("文件大小", graphic.FileSize);//文件大小
                            dataTable.Rows.Add("创建时间", graphic.CreatedAt);//创建时间
                            dataTable.Rows.Add("更新时间", graphic.UpdatedAt);//更新时间

                            dataGrid.ItemsSource = dataTable.DefaultView;
                        }
                        else if (_currentDatabaseType == "SW")
                        {
                            var graphic = _currentSelectedNode as SwGraphic;
                            var dataTable = new System.Data.DataTable();
                            dataTable.Columns.Add("属性");
                            dataTable.Columns.Add("值");
                            dataTable.Rows.Add("ID", graphic.Id);
                            dataTable.Rows.Add("子分类ID", graphic.Id);
                            dataTable.Rows.Add("文件名", graphic.FileName);
                            dataTable.Rows.Add("显示名称", graphic.DisplayName);
                            dataTable.Rows.Add("文件路径", graphic.FilePath);
                            dataTable.Rows.Add("预览图名", graphic.PreviewImageName);//预览图名
                            dataTable.Rows.Add("预览图路径", graphic.PreviewImagePath);
                            dataTable.Rows.Add("文件大小", graphic.FileSize);
                            dataTable.Rows.Add("创建时间", graphic.CreatedAt);
                            dataTable.Rows.Add("更新时间", graphic.UpdatedAt);
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



        #endregion
        #endregion
    }
    /// <summary>
    /// 分类属性编辑模型
    /// </summary>
    public class CategoryPropertyEditModel : INotifyPropertyChanged
    {
        private string _propertyName1;//属性名称1
        private string _propertyValue1;//属性值1
        private string _propertyName2;//属性名称2
        private string _propertyValue2;//属性值2

        public string PropertyName1//属性名称1
        {
            get => _propertyName1;//属性名称1
            set
            {
                _propertyName1 = value;//属性1的名称值
                OnPropertyChanged();
            }
        }
        public string PropertyValue1//属性值1
        {
            get => _propertyValue1;
            set
            {
                _propertyValue1 = value;
                OnPropertyChanged();
            }
        }
        public string PropertyName2
        {
            get => _propertyName2;
            set
            {
                _propertyName2 = value;
                OnPropertyChanged();
            }
        }
        public string PropertyValue2
        {
            get => _propertyValue2;
            set
            {
                _propertyValue2 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 属性改变事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 属性改变
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));//属性改变
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
