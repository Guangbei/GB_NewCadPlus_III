using MySql.Data.MySqlClient;
using Org.BouncyCastle.Tls;
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
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 数据库配置窗口
    /// </summary>
    public partial class DatabaseConfigWindow : Window
    {
        // 配置对象
        private DatabaseConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">配置对象，如果为null则创建新配置</param>
        public DatabaseConfigWindow(DatabaseConfig config = null)
        {
            InitializeComponent();  // 初始化组件
            _config = config ?? new DatabaseConfig();  // 使用传入配置或创建新配置
            LoadConfigToUI();  // 加载配置到界面
        }

        /// <summary>
        /// 加载配置到界面控件
        /// </summary>
        private void LoadConfigToUI()
        {
            txtServer.Text = _config.Server;  // 设置服务器文本框
            txtDatabase.Text = _config.Database;  // 设置数据库文本框
            txtUserId.Text = _config.UserId;  // 设置用户名文本框
            txtPassword.Password = _config.Password;  // 设置密码框
            txtPort.Text = _config.Port.ToString();  // 设置端口文本框
        }

        /// <summary>
        /// 从界面控件获取配置
        /// </summary>
        private void GetConfigFromUI()
        {
            _config.Server = txtServer.Text;  // 获取服务器地址
            _config.Database = txtDatabase.Text;  // 获取数据库名称
            _config.UserId = txtUserId.Text;  // 获取用户名
            _config.Password = txtPassword.Password;  // 获取密码
            _config.Port = int.TryParse(txtPort.Text, out int port) ? port : 3306;  // 获取端口，如果解析失败使用默认值
        }

        /// <summary>
        /// 获取配置对象
        /// </summary>
        /// <returns>配置对象</returns>
        public DatabaseConfig GetConfig()
        {
            return _config;  // 返回当前配置对象
        }

        /// <summary>
        /// 测试数据库连接按钮点击事件
        /// </summary>
        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            GetConfigFromUI();  // 从界面获取配置

            try
            {
                // 使用配置创建连接并测试
                using (var connection = new MySqlConnection(_config.GetConnectionString()))
                {
                    connection.Open();  // 打开连接
                    MessageBox.Show("数据库连接成功!", "测试连接",
                                  MessageBoxButton.OK, MessageBoxImage.Information);  // 显示成功消息
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"数据库连接失败: {ex.Message}", "测试连接",
                              MessageBoxButton.OK, MessageBoxImage.Error);  // 显示错误消息
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            GetConfigFromUI();  // 从界面获取配置
            this.DialogResult = true;  // 设置对话框结果为true
            this.Close();  // 关闭窗口
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;  // 设置对话框结果为false
            this.Close();  // 关闭窗口
        }
    }
}
