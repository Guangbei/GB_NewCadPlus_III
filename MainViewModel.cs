using GB_NewCadPlus_III.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III.ViewModel
{
    /// <summary>
    /// 主视图模型类，作为UI的数据上下文
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // 工艺数据集合
        private ObservableCollection<CraftProcess> _craftProcesses;

        // 数据访问对象
        private readonly CraftProcessDataAccess _dataAccess;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public MainViewModel()
        {
            // 初始化数据访问对象
            _dataAccess = new CraftProcessDataAccess();

            // 加载工艺数据
            LoadCraftProcesses();
        }

        /// <summary>
        /// 工艺数据集合属性，用于绑定到TreeView
        /// </summary>
        public ObservableCollection<CraftProcess> CraftProcesses
        {
            get { return _craftProcesses; }  // 获取工艺数据集合
            set
            {
                _craftProcesses = value;  // 设置工艺数据集合
                OnPropertyChanged("CraftProcesses");  // 通知属性更改
            }
        }

        /// <summary>
        /// 加载工艺数据
        /// </summary>
        private void LoadCraftProcesses()
        {
            // 从数据库获取数据并赋值给属性
            CraftProcesses = _dataAccess.GetAllCraftProcesses();
        }

        // 属性更改事件
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 属性更改通知方法
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));  // 触发属性更改事件
        }
    }
}
