using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// DepartmentAdminControl.xaml 的交互逻辑 DepartmentAdminControl
    /// </summary>
    public partial class DepartmentAdminControl : UserControl
    {
        /// <summary>
        /// 数据库服务
        /// </summary>
        private MySqlAuthService _svc;
        /// <summary>
        /// 登录配置
        /// </summary>
        public DepartmentAdminControl()
        {
            InitializeComponent();//加载完成
            Loaded += DepartmentAdminControl_Loaded;//注册加载完成事件
        }
        /// <summary>
        /// 加载完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepartmentAdminControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 从 login 配置优先读取服务器/端口（与之前主界面约定一致）
            try
            {
                /// 从 login 配置优先读取服务器/端口（与之前主界面约定一致）
                var cfgPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GB_NewCadPlus_III", "login_config.json");
                string host = "127.0.0.1";
                string port = "3306";
                if (System.IO.File.Exists(cfgPath))
                {
                    var json = System.IO.File.ReadAllText(cfgPath);//读取配置文件读取配置文件
                    var ser = new System.Web.Script.Serialization.JavaScriptSerializer();//创建JSON序列化器 创建序列化器
                    var cfg = ser.Deserialize<LoginConfig>(json);//反序列化JSON反序列化为配置对象
                    if (cfg != null)
                    {
                        if (!string.IsNullOrWhiteSpace(cfg.ServerIP)) host = cfg.ServerIP;//设置服务器地址
                        if (!string.IsNullOrWhiteSpace(cfg.ServerPort)) port = cfg.ServerPort;//设置端口设置服务器端口
                    }
                }

                _svc = new MySqlAuthService(host, port);
                RefreshDepartmentsAsync();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "初始化失败：" + ex.Message;
            }
        }
        /// <summary>
        /// 刷新部门
        /// </summary>
        private async void RefreshDepartmentsAsync()
        {
            TxtStatus.Text = "正在刷新部门...";
            await Task.Run(() =>
            {
                try
                {
                    _svc.EnsureCategoriesTableExists();
                    _svc.EnsureDepartmentsTableExists();
                }
                catch { }
            });

            await Task.Delay(50);
            try
            {
                var depts = await Task.Run(() => _svc.GetDepartmentsWithCounts());
                DepartmentsGrid.ItemsSource = depts;
                TxtStatus.Text = $"加载完成，共 {depts.Count} 个部门。";
                UsersGrid.ItemsSource = null;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "刷新部门失败：" + ex.Message;
            }
        }
        /// <summary>
        /// 按分类同步部门
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "正在按分类同步部门...";
            try
            {
                await Task.Run(() =>
                {
                    _svc.EnsureCategoriesTableExists();
                    _svc.EnsureDepartmentsTableExists();
                    _svc.SyncDepartmentsFromCadCategories();
                });
                RefreshDepartmentsAsync();
                MessageBox.Show("同步完成。", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "同步失败：" + ex.Message;
            }
        }
        /// <summary>
        /// 新增部门
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddDept_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new DepartmentEditWindow();
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var id = _svc.AddDepartment(dlg.DeptName, dlg.DisplayName, dlg.Description, null, dlg.SortOrder);
                    if (id > 0)
                    {
                        RefreshDepartmentsAsync();
                        MessageBox.Show("新增部门成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else MessageBox.Show("新增部门失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("新增部门异常：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        /// <summary>
        /// 修改部门
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnEditDept_Click(object sender, RoutedEventArgs e)
        {
            var sel = DepartmentsGrid.SelectedItem as DepartmentModel;
            if (sel == null) { MessageBox.Show("请先选择一个部门", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var dlg = new DepartmentEditWindow(sel.Name, sel.DisplayName, sel.Description, sel.SortOrder, sel.ManagerUserId);
            if (dlg.ShowDialog() == true)
            {
                var ok = _svc.UpdateDepartment(sel.Id, dlg.DeptName, dlg.DisplayName, dlg.Description, dlg.SortOrder, dlg.ManagerUserId, dlg.IsActive);
                if (ok)
                {
                    RefreshDepartmentsAsync();
                    MessageBox.Show("修改成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else MessageBox.Show("修改失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 删除部门
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeleteDept_Click(object sender, RoutedEventArgs e)
        {
            var sel = DepartmentsGrid.SelectedItem as DepartmentModel;
            if (sel == null) { MessageBox.Show("请先选择一个部门", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show($"确认删除部门：{sel.DisplayName} ?\n删除后该部门下用户将被置为未分配。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            var ok = _svc.DeleteDepartment(sel.Id);
            if (ok)
            {
                RefreshDepartmentsAsync();
                MessageBox.Show("删除成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else MessageBox.Show("删除失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        /// <summary>
        /// 部门选择变更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepartmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = DepartmentsGrid.SelectedItem as DepartmentModel;
            if (sel == null) { UsersGrid.ItemsSource = null; return; }
            LoadUsersForDepartment(sel.Id);
        }
        /// <summary>
        /// 加载部门用户
        /// </summary>
        /// <param name="departmentId"></param>
        private async void LoadUsersForDepartment(int departmentId)
        {
            TxtStatus.Text = "正在加载用户...";
            try
            {
                var users = await Task.Run(() => _svc.GetUsersByDepartmentId(departmentId));
                UsersGrid.ItemsSource = users;
                TxtStatus.Text = $"部门用户：{users.Count} 个";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "加载用户失败：" + ex.Message;
            }
        }
        /// <summary>
        /// 分配用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAssignUser_Click(object sender, RoutedEventArgs e)
        {
            var sel = DepartmentsGrid.SelectedItem as DepartmentModel;
            if (sel == null) { MessageBox.Show("请先选择部门", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var username = (TxtSearchUser.Text ?? "").Trim();
            if (string.IsNullOrEmpty(username)) { MessageBox.Show("请输入要分配的用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var ok = _svc.AssignUserToDepartmentByUsername(username, sel.Id);
            if (ok)
            {
                LoadUsersForDepartment(sel.Id);
                MessageBox.Show($"用户 {username} 已分配到 {sel.DisplayName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"分配失败，检查用户名是否存在或数据库状态。", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// 刷新部门
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDepartmentsAsync();
        }
    }
}
