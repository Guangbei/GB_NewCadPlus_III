using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 添加一个同步管理类
    /// </summary>
    public class ServerSyncManager
    {
        private readonly DatabaseManager _databaseManager;
        private readonly FileManager _fileManager;
        private Timer _syncTimer;
      

        public ServerSyncManager(DatabaseManager databaseManager, FileManager fileManager)
        {
            _databaseManager = databaseManager;
            _fileManager = fileManager;
        }


        /// <summary>
        /// 执行同步操作
        /// </summary>
        public async Task StartSync(int _syncInterval)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始服务器同步...");

                // 这里可以添加同步逻辑，比如：
                // 1. 检查服务器上是否有新的分类
                // 2. 检查是否有新的文件
                // 3. 同步文件状态变化

                // 示例：重新加载分类树
                // await RefreshCategoryTree();

                System.Diagnostics.Debug.WriteLine("服务器同步完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"同步失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止同步
        /// </summary>
        public void StopSync()
        {
            _syncTimer?.Dispose();
        }

        // 在WpfMainWindow.xaml.cs中添加网络连接测试方法
        public static bool TestNetworkConnection(string host, int port, int timeoutMs = 5000)
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
                    client.EndConnect(result);
                    return success;
                }
            }
            catch
            {
                return false;
            }
        }

    }
}
