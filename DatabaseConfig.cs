using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GB_NewCadPlus_III
{ 
    /// <summary>
  /// 数据库配置类，用于存储和管理数据库连接配置
  /// </summary>
    public class DatabaseConfig
    {
        // 数据库服务器地址属性，默认值为localhost
        public string Server { get; set; } = "localhost";

        // 数据库名称属性，默认值为工艺数据库
        public string Database { get; set; } = "工艺数据库";

        // 数据库用户名属性，默认值为root
        public string UserId { get; set; } = "root";

        // 数据库密码属性，默认值为空
        public string Password { get; set; } = "";

        // 数据库端口属性，默认值为3306
        public int Port { get; set; } = 3306;

        /// <summary>
        /// 构建MySQL连接字符串
        /// </summary>
        /// <returns>格式化后的连接字符串</returns>
        public string GetConnectionString()
        {
            // 使用字符串插值构建连接字符串
            return $"Server={Server};Database={Database};Uid={UserId};Pwd={Password};Port={Port};";
        }

        /// <summary>
        /// 保存配置到XML文件
        /// </summary>
        /// <param name="filePath">配置文件路径，默认为DatabaseConfig.xml</param>
        public void Save(string filePath = "DatabaseConfig.xml")
        {
            try
            {
                // 创建XML序列化器，指定类型为DatabaseConfig
                var serializer = new XmlSerializer(typeof(DatabaseConfig));

                // 使用StreamWriter创建文件流
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, this);  // 序列化对象到XML文件
                }
            }
            catch (System.Exception ex)
            {
                // 显示错误消息框
                System.Windows.MessageBox.Show($"保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从XML文件加载配置
        /// </summary>
        /// <param name="filePath">配置文件路径，默认为DatabaseConfig.xml</param>
        /// <returns>加载的配置对象，如果失败则返回默认配置</returns>
        public static DatabaseConfig Load(string filePath = "DatabaseConfig.xml")
        {
            try
            {
                // 检查配置文件是否存在
                if (File.Exists(filePath))
                {
                    // 创建XML序列化器
                    var serializer = new XmlSerializer(typeof(DatabaseConfig));

                    // 使用StreamReader读取文件
                    using (var reader = new StreamReader(filePath))
                    {
                        return (DatabaseConfig)serializer.Deserialize(reader);  // 反序列化XML到对象
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 显示错误消息框
                System.Windows.MessageBox.Show($"加载配置失败: {ex.Message}");
            }

            // 如果文件不存在或加载失败，返回新的默认配置对象
            return new DatabaseConfig();
        }
    }
}
