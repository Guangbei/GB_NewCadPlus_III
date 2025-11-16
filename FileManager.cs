using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;

namespace GB_NewCadPlus_III
{ // 创建新的文件管理服务类
    public class FileManager
    {
        private readonly DatabaseManager _databaseManager;
        private readonly string _baseStoragePath;
        private readonly bool _useDPath;

        public FileManager(DatabaseManager databaseManager, string baseStoragePath = null, bool useDPath = true)
        {
            _databaseManager = databaseManager;
            _useDPath = useDPath;

            if (!string.IsNullOrEmpty(baseStoragePath))
            {
                _baseStoragePath = baseStoragePath;
            }
            else
            {
                _baseStoragePath = GetBaseStoragePath();
            }
        }

        /// <summary>
        /// 获取基础存储路径（智能选择C盘或D盘）
        /// </summary>
        private string GetBaseStoragePath()
        {
            // 如果启用D盘优先且D盘存在且可写
            if (_useDPath && Directory.Exists("D:\\") && IsDirectoryWritable("D:\\"))
            {
                return "D:\\GB_Tools\\Cad_Sw_Library";
            }
            else
            {
                // 使用C盘作为备选
                return "C:\\GB_Tools\\Cad_Sw_Library";
            }
        }

        /// <summary>
        /// 检查目录是否可写
        /// </summary>
        private bool IsDirectoryWritable(string directoryPath)
        {
            try
            {
                string testFilePath = Path.Combine(directoryPath, "test_write_permission.tmp");
                File.WriteAllText(testFilePath, "test");
                File.Delete(testFilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }
       
        /// <summary>
        /// 下载文件
        /// </summary>
        public async Task<Stream> DownloadFileAsync(int fileId, string userName, string ipAddress)
        {
            try
            {
                // 获取文件信息
                var file = await _databaseManager.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    throw new Exception("文件不存在或已被删除");
                }

                // 检查文件是否存在
                if (!File.Exists(file.FilePath))
                {
                    throw new Exception("文件在磁盘上不存在");
                }

                // 记录访问日志
                var accessLog = new FileAccessLog
                {
                    FileId = fileId,
                    UserName = userName,
                    ActionType = "Download",
                    AccessTime = DateTime.Now,
                    IpAddress = ipAddress
                };
                await _databaseManager.AddFileAccessLogAsync(accessLog);

                // 返回文件流
                return File.OpenRead(file.FilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"下载文件失败: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// 获取分类下的所有文件
        /// </summary>
        public async Task<List<FileStorage>> GetFilesByCategoryAsync(int categoryId, string categoryType)
        {
            return await _databaseManager.GetFilesByCategoryIdAsync(categoryId, categoryType);
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        public async Task<bool> DeleteFileAsync(int fileId, string deletedBy)
        {
            try
            {
                // 获取文件信息
                var file = await _databaseManager.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    return false;
                }

                // 从数据库中软删除
                int result = await _databaseManager.DeleteFileAsync(fileId, deletedBy);

                // 可选：从磁盘删除文件（根据业务需求决定）
                // if (File.Exists(file.FilePath))
                // {
                //     File.Delete(file.FilePath);
                // }

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"删除文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 计算文件哈希值
        /// </summary>
        public static async Task<string> CalculateFileHashAsync(Stream stream)
        {
            stream.Position = 0;
            using (var sha256 = SHA256.Create())
            {
                // 对于大文件，分块读取以避免内存问题
                const int bufferSize = 8192;
                byte[] buffer = new byte[bufferSize];

                // 使用Task.Run在后台线程执行CPU密集型操作
                byte[] hash = await Task.Run(() =>
                {
                    int bytesRead;
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // SHA256的TransformBlock方法可以处理流式数据
                        sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }
                    sha256.TransformFinalBlock(buffer, 0, 0);
                    return sha256.Hash;
                });

                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 判断是否为预览文件
        /// </summary>
        public static int IsPreviewFile(string fileExtension)
        {
            var previewExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };// 预览文件扩展名列表
            if(previewExtensions.Contains(fileExtension.ToLower())) { return 1; }else { return 0; }// 判断文件扩展名是否为预览文件

        }


        // 在FileManager类中添加文件删除方法
        /// <summary>
        /// 删除已上传的文件（用于回滚操作）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteUploadedFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除文件夹（如果为空）
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>是否删除成功</returns>
        public static bool DeleteEmptyFolder(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath) && Directory.GetFileSystemEntries(folderPath).Length == 0)
                {
                    Directory.Delete(folderPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除空文件夹失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 回滚文件上传操作
        /// </summary>
        /// <param name="uploadedFiles">已上传的文件路径列表</param>
        /// <param name="fileStorage">已保存的文件记录</param>
        /// <param name="fileAttribute">已保存的属性记录</param>
        public static async Task RollbackFileUpload(DatabaseManager databaseManager, List<string> uploadedFiles, FileStorage fileStorage, FileAttribute fileAttribute)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始回滚文件上传操作...");

                // 1. 删除已上传的文件
                foreach (string filePath in uploadedFiles)
                {
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            System.Diagnostics.Debug.WriteLine($"已删除文件: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"删除文件失败 {filePath}: {ex.Message}");
                        }
                    }
                }

                // 2. 删除空的文件夹
                if (fileStorage != null)
                {
                    string categoryPath = Path.GetDirectoryName(fileStorage.FilePath);
                    if (Directory.Exists(categoryPath))
                    {
                        try
                        {
                            // 尝试删除分类文件夹（如果为空）
                            DeleteEmptyFolder(categoryPath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"删除文件夹失败: {ex.Message}");
                        }
                    }
                }

                // 3. 如果数据库记录已创建，删除数据库记录
                if (fileAttribute == null)
                {
                    try
                    {
                        // 删除属性记录
                        await databaseManager.DeleteFileAttributeAsync(fileAttribute.Id);
                        System.Diagnostics.Debug.WriteLine("已删除文件属性记录");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"删除属性记录失败: {ex.Message}");
                    }
                }
            

                if (fileStorage != null && fileStorage.Id > 0)
                {
                    try
                    {
                        // 删除文件记录
                        await databaseManager.DeleteFileStorageAsync(fileStorage.Id);
                        System.Diagnostics.Debug.WriteLine("已删除文件存储记录");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"删除文件记录失败: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("文件上传回滚操作完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"回滚操作失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 上传文件到服务器指定路径
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <param name="categoryType">分类类型</param>
        /// <param name="originalFileName">原始文件名</param>
        /// <param name="fileStream">文件流</param>
        /// <param name="description">文件描述</param>
        /// <param name="createdBy">创建者</param>
        /// <returns>文件存储信息</returns>
        public async Task<FileStorage> UploadFileAsync(DatabaseManager databaseManager, int categoryId, string categoryType,
            string originalFileName, Stream fileStream,
            string description, string createdBy)
        {
            try
            {
                // 确保存储路径存在
                EnsureBaseStoragePathExists();

                // 生成唯一的存储文件名
                string fileExtension = Path.GetExtension(originalFileName);
                string storedFileName = $"{Guid.NewGuid()}{fileExtension}";

                // 确定存储路径（按分类类型和ID组织文件夹）
                string categoryPath = Path.Combine(_baseStoragePath, categoryType, categoryId.ToString());
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                }

                string fullPath = Path.Combine(categoryPath, storedFileName);

                // 计算文件哈希值（用于去重）
                string fileHash = await CalculateFileHashAsync(fileStream);

                // 检查文件是否已存在
                //var existingFile = await _databaseManager.GetFileByHashAsync(fileHash);
                //if (existingFile != null && existingFile.IsActive)
                //{
                //    // 文件已存在，返回现有记录
                //    return existingFile;
                //}

                // 保存文件到磁盘
                fileStream.Position = 0;
                using (var fileStreamOutput = File.Create(fullPath))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                // 获取文件大小
                long fileSize = new FileInfo(fullPath).Length;

                // 创建文件记录
                var fileRecord = new FileStorage
                {
                    CategoryId = categoryId,
                    CategoryType = categoryType,
                    FileName = originalFileName,
                    FileStoredName = storedFileName,
                    FilePath = fullPath,  // 存储完整路径
                    FileType = fileExtension.ToLower(),
                    FileSize = fileSize,
                    FileHash = fileHash,
                    DisplayName = Path.GetFileNameWithoutExtension(originalFileName),
                    Description = description,
                    Version = 1,
                    IsPreview = IsPreviewFile(fileExtension),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = createdBy,
                    IsActive = 1,
                    IsPublic = 1
                };

                return fileRecord;
            }
            catch (Exception ex)
            {
                throw new Exception($"上传文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 确保存储基础路径存在
        /// </summary>
        private void EnsureBaseStoragePathExists()
        {
            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }
        }
    }
}
