using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 工艺数据模型类，对应数据库中的"工艺"表
    /// 实现INotifyPropertyChanged接口用于数据绑定通知
    /// </summary>
    public class CraftProcess : INotifyPropertyChanged
    {
        // 私有字段
        private int _id;
        private string _name;
        private string _description;
        private int _parentId;

        // 公共属性 - ID
        public int Id
        {
            get { return _id; }  // 获取ID值
            set
            {
                _id = value;  // 设置ID值
                OnPropertyChanged("Id");  // 通知属性更改
            }
        }

        // 公共属性 - 名称
        public string Name
        {
            get { return _name; }  // 获取名称值
            set
            {
                _name = value;  // 设置名称值
                OnPropertyChanged("Name");  // 通知属性更改
            }
        }

        // 公共属性 - 描述
        public string Description
        {
            get { return _description; }  // 获取描述值
            set
            {
                _description = value;  // 设置描述值
                OnPropertyChanged("Description");  // 通知属性更改
            }
        }

        // 公共属性 - 父级ID
        public int ParentId
        {
            get { return _parentId; }  // 获取父级ID值
            set
            {
                _parentId = value;  // 设置父级ID值
                OnPropertyChanged("ParentId");  // 通知属性更改
            }
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
