using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 日志管理器类
    /// </summary>
    public class LogManager
    {
        private static LogManager _instance;
        private static readonly object _lock = new object();
        private string _logFilePath;
        private bool _isInitialized = false;

        private LogManager()
        {
            Initialize();
        }

        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LogManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private void Initialize()
        {
            try
            {
                // 创建日志目录
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GB_CADPLUS", "Logs");

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // 创建日志文件名（按天命名）
                string fileName = $"GB_NewCadPlus_III_{DateTime.Now:yyyyMMdd}.log";
                _logFilePath = Path.Combine(logDirectory, fileName);

                // 确保日志文件存在
                if (!File.Exists(_logFilePath))
                {
                    File.WriteAllText(_logFilePath, "");
                }

                _isInitialized = true;

                // 记录初始化日志
                LogInfo("日志管理器初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日志管理器初始化失败: {ex.Message}");
            }
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public void LogError(string message)
        {
            WriteLog("ERROR", message);
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message);
        }

        private void WriteLog(string level, string message)
        {
            if (!_isInitialized)
                return;

            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

                // 同时输出到调试窗口和日志文件
                System.Diagnostics.Debug.WriteLine(logEntry);

                // 异步写入日志文件
                Task.Run(() =>
                {
                    try
                    {
                        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"写入日志文件失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"记录日志失败: {ex.Message}");
            }
        }

        public string LogFilePath => _logFilePath;
    }
}
