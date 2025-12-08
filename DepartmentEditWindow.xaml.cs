using System;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 部门编辑窗口
    /// </summary>
    public partial class DepartmentEditWindow : Window
    {
        public string DeptName => TxtDeptName.Text.Trim();
        public string DisplayName => TxtDisplayName.Text.Trim();
        public string Description => TxtDescription.Text.Trim();
        public int SortOrder { get; private set; }
        public int? ManagerUserId { get; private set; }
        public bool IsActive => ChkIsActive.IsChecked == true;
        /// <summary>
        /// 部门编辑窗口
        /// </summary>
        /// <param name="name">部门名称</param>
        /// <param name="display">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="sortOrder">排序</param>
        /// <param name="managerUserId">负责人用户ID</param>
        /// <param name="isActive">是否激活</param>
        public DepartmentEditWindow(string name = "", string display = "", string description = "", int sortOrder = 0, int? managerUserId = null, bool isActive = true)
        {
            InitializeComponent();//初始化窗体加载完成
            TxtDeptName.Text = name ?? "";//部门名称设置部门名称
            TxtDisplayName.Text = string.IsNullOrEmpty(display) ? (name ?? "") : display;//显示名称设置显示名称
            TxtDescription.Text = description ?? "";//描述设置描述
            TxtSortOrder.Text = sortOrder.ToString();//排序设置排序
            TxtManagerUserId.Text = managerUserId?.ToString() ?? "";//负责人用户ID设置负责人用户ID
            ChkIsActive.IsChecked = isActive;
        }
        /// <summary>
        /// 确定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DeptName))
            {
                MessageBox.Show("部门名称不能为空。", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDeptName.Focus();
                return;
            }

            if (!int.TryParse(TxtSortOrder.Text.Trim(), out int so))//排序排序转换失败
                so = 0;//排序排序转换成功排序设置为0
            SortOrder = so;//排序设置为排序

            if (int.TryParse(TxtManagerUserId.Text.Trim(), out int mid))//负责人用户ID转换成功负责人用户ID负责人用户ID转换成功
                ManagerUserId = mid;//负责人用户ID设置为转换成功的负责人用户ID
            else
                ManagerUserId = null;//负责人用户ID设置为null

            DialogResult = true;//确定对话结果设置为true
            Close();
        }
        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
