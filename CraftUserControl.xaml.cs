using GB_NewCadPlus_III.ViewModel;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;  // 引入泛型集合命名空间
using System.Collections.ObjectModel;  // 引入可观察集合命名空间
using System.Linq;  // 引入LINQ命名空间

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 工艺用户控件
    /// </summary>
    public partial class CraftUserControl : UserControl
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CraftUserControl()
        {
            InitializeComponent();  // 初始化组件

            // 设置数据上下文为主视图模型
            this.DataContext = new MainViewModel();

            // 注册Loaded事件，在控件加载完成后执行
            this.Loaded += CraftUserControl_Loaded;
        }

        /// <summary>
        /// 控件加载完成事件处理
        /// </summary>
        private void CraftUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取视图模型
            var viewModel = this.DataContext as MainViewModel;

            // 检查视图模型是否为空
            if (viewModel != null)
            {
                // 构建树形数据
                var treeData = TreeViewItemViewModel.BuildTreeFromProcesses(viewModel.CraftProcesses);

                // 设置TreeView的数据源
                CraftTreeView.ItemsSource = treeData;
            }
        }
    }
    /// <summary>
    /// 树形视图项视图模型类，用于构建TreeView的层次结构
    /// </summary>
    public class TreeViewItemViewModel
    {
        // 节点名称属性
        public string Name { get; set; }

        // 子节点集合属性
        public ObservableCollection<TreeViewItemViewModel> Children { get; set; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public TreeViewItemViewModel()
        {
            // 初始化子节点集合
            Children = new ObservableCollection<TreeViewItemViewModel>();
        }

        /// <summary>
        /// 从工艺数据构建树形结构
        /// </summary>
        /// <param name="processes">工艺数据集合</param>
        /// <returns>构建好的树形数据集合</returns>
        public static ObservableCollection<TreeViewItemViewModel> BuildTreeFromProcesses(
            ObservableCollection<CraftProcess> processes)
        {
            // 创建根节点集合
            var treeData = new ObservableCollection<TreeViewItemViewModel>();

            // 创建字典用于快速查找节点
            var nodeDictionary = new Dictionary<int, TreeViewItemViewModel>();

            // 第一遍遍历：创建所有节点并添加到字典中
            foreach (var process in processes)
            {
                // 创建新的树节点
                var node = new TreeViewItemViewModel
                {
                    Name = process.Name  // 设置节点名称为工艺名称
                };
                nodeDictionary[process.Id] = node;  // 以ID为键添加到字典
            }

            // 第二遍遍历：构建父子关系
            foreach (var process in processes)
            {
                // 获取当前节点
                var currentNode = nodeDictionary[process.Id];

                // 判断是否为根节点（ParentId为0）
                if (process.ParentId == 0)
                {
                    treeData.Add(currentNode);  // 添加到根节点集合
                }
                // 判断父节点是否存在
                else if (nodeDictionary.ContainsKey(process.ParentId))
                {
                    // 获取父节点
                    var parentNode = nodeDictionary[process.ParentId];
                    parentNode.Children.Add(currentNode);  // 将当前节点添加到父节点的子集合
                }
            }

            return treeData;  // 返回构建好的树形数据
        }
    }
}
