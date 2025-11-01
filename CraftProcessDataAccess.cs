using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using GB_NewCadPlus_III.Properties;
using System.Collections.ObjectModel;
using System.Configuration;

namespace GB_NewCadPlus_III.Data
{
    /// <summary>
    /// 工艺数据访问类，负责与MySQL数据库交互
    /// </summary>
    public class CraftProcessDataAccess
    {
        // 数据库配置对象
        private DatabaseConfig _config;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CraftProcessDataAccess()
        {
            // 加载数据库配置
            _config = DatabaseConfig.Load();
        }

        /// <summary>
        /// 从数据库获取所有工艺数据
        /// </summary>
        /// <returns>工艺数据集合</returns>
        public ObservableCollection<CraftProcess> GetAllCraftProcesses()
        {
            // 创建工艺数据集合
            var craftProcesses = new ObservableCollection<CraftProcess>();

            // 获取连接字符串
            string connectionString = _config.GetConnectionString();

            // 使用using语句确保连接正确释放
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();  // 打开数据库连接

                    // SQL查询语句，按父ID和ID排序
                    string query = "SELECT * FROM 工艺 ORDER BY ParentId, Id";

                    // 创建MySQL命令对象
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // 执行查询并获取数据读取器
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            // 遍历所有数据行
                            while (reader.Read())
                            {
                                // 创建工艺对象并填充数据
                                var craft = new CraftProcess
                                {
                                    Id = reader.GetInt32("Id"),  // 读取ID字段
                                    Name = reader.GetString("Name"),  // 读取Name字段
                                    Description = reader.IsDBNull(1) ?
                                                 string.Empty : reader.GetString("Description"),  // 处理空值
                                    ParentId = reader.GetInt32("ParentId")  // 读取ParentId字段
                                };

                                craftProcesses.Add(craft);  // 添加到集合
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    // 显示数据库错误信息
                    System.Windows.MessageBox.Show($"数据库连接失败: {ex.Message}");

                    // 显示配置对话框让用户重新配置
                    ShowConfigurationDialog();
                }
            }

            return craftProcesses;  // 返回工艺数据集合
        }

        /// <summary>
        /// 显示数据库配置对话框
        /// </summary>
        private void ShowConfigurationDialog()
        {
            // 创建配置窗口
            var configWindow = new DatabaseConfigWindow(_config);

            // 显示对话框并等待结果
            if (configWindow.ShowDialog() == true)
            {
                _config = configWindow.GetConfig();  // 获取新配置
                _config.Save();  // 保存配置到文件
            }
        }
    }
}
