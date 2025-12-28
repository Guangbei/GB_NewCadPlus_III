using Dapper;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace GB_NewCadPlus_III
{

    /*
     EnsureAttributeDefinitionExistsAsync
     */
    #region
    /// <summary>
    /// 数据库访问类  
    /// </summary>
    public class DatabaseManager
    {
        /// <summary>
        /// 简化的属性定义类型，供 AttributeEditorControl 使用（与数据库列映射）
        /// </summary>
        public class AttributeDefinition
        {
            public int Id { get; set; }
            public string KeyName { get; set; }
            public string DisplayName { get; set; }
            public string DataType { get; set; }
            public string Unit { get; set; }
        }

        /// <summary>
        /// 缓存 file_attribute_values 表存在性的检查结果，减少重复查询 information_schema 的开销
        /// </summary>
        private volatile bool _fileAttrValuesTableChecked = false;
        /// <summary>
        /// file_attribute_values 表存在性检查结果
        /// </summary>
        private volatile bool _fileAttrValuesTableExists = false;
        /// <summary>
        /// 缓存 file_attributes 表存在性的检查结果，减少重复查询 information_schema 的开销
        /// </summary>
        private readonly object _fileAttrValuesTableLock = new object();

        /// <summary>
        /// 对外公开数据库连接（注意：调用方负责不要忘记关闭/处置）
        /// </summary>
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// 用户实体（对应 users 表）
        /// </summary>
        public class User
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string DisplayName { get; set; }
            public string Gender { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public int? DepartmentId { get; set; }
            public string Role { get; set; }
            public int Status { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }


        #region 初始化数据库核心表与基础数据
        /// <summary>
        /// 对外公开：初始化所有核心表并填充基础数据（幂等）。
        /// 可传入服务器连接信息，或直接调用 CreateDatabaseAndCoreTables 先创建 DB 再执行初始化。
        /// </summary>
        /// <param name="server">MySQL 主机（可为空，若为空请确保当前 _connectionString 可用）</param>
        /// <param name="port">端口</param>
        /// <param name="user">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="database">数据库名</param>
        public async Task<bool> InitializeAllCoreTablesAsync(string server = null, int port = 3306, string user = null, string password = null, string database = "cad_sw_library")
        {
            try
            {
                // 如果传入了服务器信息，尝试创建数据库与表结构（静态方法）
                if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(user))
                {
                    var ok = await Task.Run(() => CreateDatabaseAndCoreTables(server, port, user, password, database));
                    if (!ok)
                        return false;
                }

                // 刷新可用状态（如果构造时无效，可以再次检测）
                IsDatabaseAvailable = TestDatabaseConnection();

                if (!IsDatabaseAvailable)
                {
                    LogManager.Instance.LogInfo("InitializeAllCoreTablesAsync: 数据库不可用，初始化被中止。");
                    return false;
                }

                // 逐个初始化（每个方法均为幂等）
                await InitializeDepartmentsAsync().ConfigureAwait(false);
                await InitializeUsersAsync().ConfigureAwait(false);
                await InitializeCategoriesAsync().ConfigureAwait(false);
                await InitializeSubcategoriesAsync().ConfigureAwait(false);
                await InitializeFileStorageAsync().ConfigureAwait(false);
                await InitializeFileAttributesAsync().ConfigureAwait(false);

                // 同步分类到部门映射（保证一致性）
                _ = SyncDepartmentsFromCadCategoriesAsync();

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"InitializeAllCoreTablesAsync 出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 幂等：初始化 departments 表的默认专业（若无数据则插入）
        /// </summary>
        public async Task InitializeDepartmentsAsync()
        {
            if (!IsDatabaseAvailable) throw new InvalidOperationException("数据库不可用");

            var defaults = new[] { "工艺", "建筑", "结构", "电气", "给排水", "暖通", "自控", "总图", "公共图" };

            using var conn = GetConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            var count = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM departments").ConfigureAwait(false);
            if (count > 0) return;

            using var tx = conn.BeginTransaction();
            int sort = 1;
            foreach (var name in defaults)
            {
                var sql = @"INSERT INTO departments (name, display_name, sort_order, is_active, created_at, updated_at)
                            VALUES (@Name, @DisplayName, @SortOrder, 1, NOW(), NOW())";
                await conn.ExecuteAsync(sql, new { Name = name, DisplayName = name, SortOrder = sort++ }, tx).ConfigureAwait(false);
            }
            tx.Commit();
        }

        /// <summary>
        /// 幂等：初始化 users 表 — 如果表为空，创建一个默认管理员（用户名 sa，初始密码 sa，使用 SHA256 存储哈希）
        /// 建议上线后强制修改密码或通过更安全流程创建管理员。
        /// </summary>
        public async Task InitializeUsersAsync()
        {
            if (!IsDatabaseAvailable) throw new InvalidOperationException("数据库不可用");

            using var conn = GetConnection();
            await conn.OpenAsync().ConfigureAwait(false);

            var count = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM users").ConfigureAwait(false);
            if (count > 0) return;

            string defaultUser = "sa";
            string defaultPassword = "sa";
            string passwordHash = ComputeSha256Hash(defaultPassword);

            var insertSql = @"INSERT INTO users (username, password_hash, display_name, gender, phone, email, department_id, role, status, created_at, updated_at)
                                  VALUES (@Username, @PasswordHash, @DisplayName, '无信息', NULL, NULL, NULL, 'Administrator', 1, NOW(), NOW())";
            await conn.ExecuteAsync(insertSql, new { Username = defaultUser, PasswordHash = passwordHash, DisplayName = "管理员" }).ConfigureAwait(false);
        }

        /// <summary>
        /// 幂等：初始化 cad_categories（若为空则插入标准主分类）
        /// </summary>
        public async Task InitializeCategoriesAsync()
        {
            if (!IsDatabaseAvailable) throw new InvalidOperationException("数据库不可用");

            var mains = new[]
            {
                new { Name = "公共图", Display = "公共图", Sort = 0 },
                new { Name = "工艺", Display = "工艺", Sort = 1 },
                new { Name = "建筑", Display = "建筑", Sort = 2 },
                new { Name = "结构", Display = "结构", Sort = 3 },
                new { Name = "电气", Display = "电气", Sort = 4 },
                new { Name = "给排水", Display = "给排水", Sort = 5 },
                new { Name = "暖通", Display = "暖通", Sort = 6 },
                new { Name = "自控", Display = "自控", Sort = 7 },
                new { Name = "总图", Display = "总图", Sort = 8 }
            };

            using var conn = GetConnection();// GetConnection 获取数据库连接
            await conn.OpenAsync().ConfigureAwait(false);// 打开数据库连接
            // 检查 cad_categories 表是否为空 检查是否已有分类
            var cnt = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM cad_categories").ConfigureAwait(false);
            if (cnt > 0) return;// 有数据则返回 如果已有分类则直接返回

            // 尽量使用已有 AddCadCategoryAsync（内部会触发部门同步）
            foreach (var m in mains)
            {
                try
                {
                    // 添加分类 使用 AddCadCategoryAsync 插入新分类
                    var cat = new CadCategory { Name = m.Name, DisplayName = m.Display, SortOrder = m.Sort };
                    // 添加分类
                    await AddCadCategoryAsync(cat).ConfigureAwait(false);
                }
                catch
                {
                    // 回退：直接插入（若 AddCadCategoryAsync 因约束失败）
                    var sql = @"INSERT INTO cad_categories (name, display_name, subcategory_ids, sort_order, created_at, updated_at)
                                VALUES (@Name, @DisplayName, '', @SortOrder, NOW(), NOW())";
                    // 插入
                    await conn.ExecuteAsync(sql, new { Name = m.Name, DisplayName = m.Display, SortOrder = m.Sort }).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 幂等：为每个主分类插入示例子分类（若 cad_subcategories 为空则填充）
        /// 真实场景可以不插入示例子分类；此方法仅便于首次运行时 UI 有可见数据。
        /// </summary>
        public async Task InitializeSubcategoriesAsync()
        {
            if (!IsDatabaseAvailable) throw new InvalidOperationException("数据库不可用");

            using var conn = GetConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            var cnt = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM cad_subcategories").ConfigureAwait(false);
            if (cnt > 0) return;

            var mainCats = await GetAllCadCategoriesAsync().ConfigureAwait(false);
            if (mainCats == null || mainCats.Count == 0) return;

            using var tx = conn.BeginTransaction();
            foreach (var main in mainCats)
            {
                try
                {
                    var sub = new CadSubcategory
                    {
                        ParentId = main.Id,
                        Name = main.Name + "子项",
                        DisplayName = main.DisplayName + "子项",
                        SortOrder = 1,
                        Level = 1,
                        SubcategoryIds = ""
                    };
                    await AddCadSubcategoryAsync(sub).ConfigureAwait(false);
                }
                catch
                {
                    var sql = @"INSERT INTO cad_subcategories (parent_id, name, display_name, sort_order, level, subcategory_ids, created_at, updated_at)
                                VALUES (@ParentId, @Name, @DisplayName, @SortOrder, @Level, '', NOW(), NOW())";
                    await conn.ExecuteAsync(sql, new { ParentId = main.Id, Name = main.Name + "子项", DisplayName = main.DisplayName + "子项", SortOrder = 1, Level = 1 }, tx).ConfigureAwait(false);
                }
            }
            tx.Commit();
        }

        /// <summary>
        /// 幂等：插入示例 cad_file_storage 行（若表为空），主要用于调试与 UI 显示字段映射。
        /// 生产环境建议不插入占位记录。
        /// </summary>
        public async Task InitializeFileStorageAsync()
        {
            if (!IsDatabaseAvailable) throw new InvalidOperationException("数据库不可用");

            using var conn = GetConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            var cnt = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM cad_file_storage").ConfigureAwait(false);
            if (cnt > 0) return;

            var sampleSql = @"INSERT INTO cad_file_storage
                                  (category_id, category_type, file_attribute_id, file_name, file_stored_name, display_name, file_type, file_hash, file_path, preview_image_path, file_size, is_preview, version, description, is_active, created_by, created_at, updated_at)
                                  VALUES
                                  (@CategoryId, @CategoryType, @FileAttributeId, @FileName, @FileStoredName, @DisplayName, @FileType, @FileHash, @FilePath, @PreviewImagePath, @FileSize, 0, 1, @Description, 1, @CreatedBy, NOW(), NOW())";
            await conn.ExecuteAsync(sampleSql, new
            {
                CategoryId = 0,
                CategoryType = "sub",
                FileAttributeId = 0,
                FileName = "示例文件.dwg",
                FileStoredName = "示例文件_存储名.dwg",
                DisplayName = "示例图元",
                FileType = ".dwg",
                FileHash = Guid.NewGuid().ToString("N"),
                FilePath = "",
                PreviewImagePath = "",
                FileSize = 0L,
                Description = "初始化占位记录",
                CreatedBy = Environment.UserName
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 幂等：插入示例 cad_file_attributes 行（若表为空）
        /// </summary>
        public async Task InitializeFileAttributesAsync()
        {
            if (!IsDatabaseAvailable) throw new InvalidOperationException("数据库不可用");

            using var conn = GetConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            var cnt = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM cad_file_attributes").ConfigureAwait(false);
            if (cnt > 0) return;

            var sql = @"INSERT INTO cad_file_attributes
                            (category_id, file_storage_id, file_name, length, width, height, angle, base_point_x, base_point_y, base_point_z, description, medium_name, specifications, material, standard_number, created_at, updated_at)
                            VALUES
                            (0, 0, @FileName, NULL, NULL, NULL, NULL, NULL, NULL, NULL, @Description, NULL, NULL, NULL, NULL, NOW(), NOW())";
            await conn.ExecuteAsync(sql, new { FileName = "示例文件.dwg", Description = "初始化占位文件属性" }).ConfigureAwait(false);
        }

        /// <summary>
        /// 计算字符串的 SHA256 十六进制哈希（用于初始密码示例）
        /// </summary>
        private static string ComputeSha256Hash(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            using var sha = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
            var hashed = sha.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hashed) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// 根据用户名查询用户（用于注册后获取 id）
        /// </summary>
        /// <param name="username"></param>
        /// <returns>匹配的 User 或 null</returns>
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            const string sql = @"
                SELECT
                    id AS Id,
                    username AS Username,
                    password_hash AS PasswordHash,
                    display_name AS DisplayName,
                    gender AS Gender,
                    phone AS Phone,
                    email AS Email,
                    department_id AS DepartmentId,
                    role AS Role,
                    status AS Status,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM users
                WHERE username = @Username
                LIMIT 1";

            try
            {
                using var conn = GetConnection();
                var user = await conn.QuerySingleOrDefaultAsync<User>(sql, new { Username = username }).ConfigureAwait(false);
                return user;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"GetUserByUsernameAsync 出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查指定数据库中是否缺少核心表。
        /// 返回：
        /// - 若数据库不存在：返回包含单项 "__DATABASE_MISSING__"
        /// - 若数据库存在但缺少表：返回缺失表名列表（不为空）
        /// - 若一切正常：返回空列表
        /// </summary>
        public static List<string> CheckMissingCoreTables(string server, int port, string user, string password, string database = "cad_sw_library")
        {
            var missing = new List<string>();
            try
            {
                // 连接到目标数据库以检查表
                var connStr = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};";
                using var conn = new MySqlConnection(connStr);
                conn.Open();

                // 需要保证的核心表
                var required = new[]
                {
                    "cad_categories",
                    "cad_subcategories",
                    "cad_file_storage",
                    "cad_file_attributes",
                    "system_config",
                    "users",
                    "departments",
                    "department_users",
                    "category_department_map"
                };

                var sql = @"SELECT table_name FROM information_schema.tables
                        WHERE table_schema = @schema AND table_name IN @names";
                var found = conn.Query<string>(sql, new { schema = database, names = required }).AsList();

                foreach (var t in required)
                {
                    if (!found.Contains(t))
                        missing.Add(t);
                }

                return missing;
            }
            catch (MySqlException mex)
            {
                // 数据库不存在
                if (mex.Number == 1049)
                {
                    return new List<string> { "__DATABASE_MISSING__" };
                }
                return new List<string> { $"__DB_ERROR__:{mex.Message}" };
            }
            catch (Exception)
            {
                return new List<string> { "__DB_CHECK_FAILED__" };
            }
        }

        /// <summary>
        /// 创建数据库（若不存在）并创建缺失的核心表。
        /// 返回 true 表示成功（即创建完毕或已存在），false 表示失败。
        /// 注意：该方法会在服务器上执行 DDL，请确保凭据具有相应权限。
        /// </summary>
        public static bool CreateDatabaseAndCoreTables(string server, int port, string user, string password, string database = "cad_sw_library")
        {
            try
            {
                // 1) 先连接到 server（不指定数据库）以便创建数据库
                var masterConn = $"Server={server};Port={port};Uid={user};Pwd={password};";
                using (var conn = new MySqlConnection(masterConn))
                {
                    conn.Open();
                    var createDbSql = $"CREATE DATABASE IF NOT EXISTS `{database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;";
                    conn.Execute(createDbSql);
                }

                // 2) 连接到刚创建/存在的数据库，逐个创建表（IF NOT EXISTS）
                var dbConn = $"Server={server};Port={port};Database={database};Uid={user};Pwd={password};";
                using (var conn = new MySqlConnection(dbConn))
                {
                    conn.Open();
                    var sql = new StringBuilder();

                    // cad_categories
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `cad_categories` (
                        `id` INT NOT NULL PRIMARY KEY,
                        `name` VARCHAR(200) NOT NULL,
                        `display_name` VARCHAR(200),
                        `subcategory_ids` TEXT,
                        `sort_order` INT DEFAULT 0,
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // cad_subcategories
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `cad_subcategories` (
                        `id` INT NOT NULL PRIMARY KEY,
                        `parent_id` INT NOT NULL,
                        `name` VARCHAR(200) NOT NULL,
                        `display_name` VARCHAR(200),
                        `sort_order` INT DEFAULT 0,
                        `level` INT DEFAULT 1,
                        `subcategory_ids` TEXT,
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                        INDEX(`parent_id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // cad_file_storage
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `cad_file_storage` (
                        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        `category_id` INT,
                        `category_type` VARCHAR(16) DEFAULT 'sub',
                        `file_attribute_id` INT,
                        `file_name` VARCHAR(512),
                        `file_stored_name` VARCHAR(512),
                        `display_name` VARCHAR(512),
                        `file_type` VARCHAR(32),
                        `file_hash` VARCHAR(128),
                        `file_path` VARCHAR(1024),
                        `preview_image_path` VARCHAR(1024),
                        `preview_image_name` VARCHAR(512),
                        `file_size` BIGINT,
                        `is_preview` TINYINT DEFAULT 0,
                        `version` INT DEFAULT 1,
                        `description` TEXT,
                        `is_active` TINYINT DEFAULT 1,
                        `created_by` VARCHAR(128),
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // cad_file_attributes
                    // 新增大量物性/测量属性字段（支持字符串/数值自由存储）
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `cad_file_attributes` (
                        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        `category_id` INT,
                        `file_storage_id` INT,
                        `file_name` VARCHAR(512),
                        `length` DECIMAL(18,4),
                        `width` DECIMAL(18,4),
                        `height` DECIMAL(18,4),
                        `angle` DECIMAL(10,4),
                        `base_point_x` DECIMAL(18,4),
                        `base_point_y` DECIMAL(18,4),
                        `base_point_z` DECIMAL(18,4),
                        `description` TEXT,
                        `medium_name` VARCHAR(200),
                        `specifications` VARCHAR(200),
                        `material` VARCHAR(200),
                        `standard_number` VARCHAR(200),
                        `power` VARCHAR(100),
                        `volume` VARCHAR(100),
                        `density` VARCHAR(100),
                        `voltage` VARCHAR(100),
                        `current` VARCHAR(100),
                        `flow` VARCHAR(100),
                        `moisture` VARCHAR(100),
                        `humidity` VARCHAR(100),
                        `vacuum` VARCHAR(100),
                        `radiation` VARCHAR(100),
                        `velocity` VARCHAR(100),
                        `frequency` VARCHAR(100),
                        `temperature` VARCHAR(100),
                        `conductivity` VARCHAR(100),
                        `pressure` VARCHAR(100),
                        `diameter` VARCHAR(100),
                        `outer_diameter` VARCHAR(100),
                        `inner_diameter` VARCHAR(100),
                        `thickness` VARCHAR(100),
                        `weight` VARCHAR(100),
                        `model` VARCHAR(200),
                        `lift` VARCHAR(100),
                        `remarks` TEXT,
                        `customize1` VARCHAR(200),
                        `customize2` VARCHAR(200),
                        `customize3` VARCHAR(200),
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // system_config
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `system_config` (
                        `config_key` VARCHAR(200) PRIMARY KEY,
                        `config_value` TEXT
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // departments
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `departments` (
                        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        `name` VARCHAR(200) NOT NULL UNIQUE,
                        `display_name` VARCHAR(200),
                        `sort_order` INT DEFAULT 0,
                        `is_active` TINYINT DEFAULT 1,
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // users (详细人员信息表)
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `users` (
                        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        `username` VARCHAR(100) NOT NULL UNIQUE,
                        `password_hash` VARCHAR(512),
                        `display_name` VARCHAR(200),
                        `gender` ENUM('男','女','无信息') DEFAULT '无信息',
                        `phone` VARCHAR(32),
                        `email` VARCHAR(200),
                        `department_id` INT,
                        `role` VARCHAR(64),
                        `status` TINYINT DEFAULT 1,
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                        INDEX(`department_id`),
                        CONSTRAINT `fk_users_department` FOREIGN KEY (`department_id`) REFERENCES `departments`(`id`) ON DELETE SET NULL ON UPDATE CASCADE
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // department_users (可选的多对多)
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `department_users` (
                        `department_id` INT NOT NULL,
                        `user_id` INT NOT NULL,
                        PRIMARY KEY (`department_id`,`user_id`),
                        INDEX(`user_id`),
                        CONSTRAINT `fk_dept_users_dept` FOREIGN KEY (`department_id`) REFERENCES `departments`(`id`) ON DELETE CASCADE ON UPDATE CASCADE,
                        CONSTRAINT `fk_dept_users_user` FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE ON UPDATE CASCADE
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // category_department_map (一对一映射 cad_categories.id -> departments.id)
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `category_department_map` (
                        `category_id` INT NOT NULL PRIMARY KEY,
                        `department_id` INT NOT NULL UNIQUE,
                        CONSTRAINT `fk_map_category` FOREIGN KEY (`category_id`) REFERENCES `cad_categories`(`id`) ON DELETE CASCADE ON UPDATE CASCADE,
                        CONSTRAINT `fk_map_department` FOREIGN KEY (`department_id`) REFERENCES `departments`(`id`) ON DELETE CASCADE ON UPDATE CASCADE
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    // file_access_logs & file_tags & file_version_history (ensure basic tables)
                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `file_access_logs` (
                        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        `file_id` INT,
                        `user_name` VARCHAR(200),
                        `action_type` VARCHAR(50),
                        `ip_address` VARCHAR(64),
                        `user_agent` VARCHAR(512),
                        `access_time` DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `file_tags` (
                        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        `file_id` INT,
                        `tag_name` VARCHAR(200),
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    sql.AppendLine(@"CREATE TABLE IF NOT EXISTS `file_version_history` (
                        `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                        `file_id` INT,
                        `version` INT,
                        `file_name` VARCHAR(512),
                        `stored_file_name` VARCHAR(512),
                        `file_path` VARCHAR(1024),
                        `file_size` BIGINT,
                        `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                        `updated_by` VARCHAR(200),
                        `change_description` TEXT
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

                    conn.Execute(sql.ToString());
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"CreateDatabaseAndCoreTables 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 主窗口
        /// </summary>
        private readonly WpfMainWindow _wpfMainWindow;

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public readonly string _connectionString;

        /// <summary>
        /// 数据库是否可用
        /// </summary>
        public bool IsDatabaseAvailable { get; private set; } = true;

        /// <summary>
        /// 数据库管理类构造函数
        /// </summary>
        /// <param name="connectionString"> 链接字符串
        ///  </param>
        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
            IsDatabaseAvailable = TestDatabaseConnection();
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <returns></returns>
        private bool TestDatabaseConnection()
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                LogManager.Instance.LogInfo("数据库连接测试成功");
                return true;
            }
            catch (MySqlException ex)
            {
                LogManager.Instance.LogInfo($"MySQL连接错误: {ex.Number} - {ex.Message}");
                switch (ex.Number)
                {
                    case 0:
                        LogManager.Instance.LogInfo("无法连接到MySQL服务器");
                        break;
                    case 1042:
                        LogManager.Instance.LogInfo("无法解析主机名");
                        break;
                    case 1045:
                        LogManager.Instance.LogInfo("用户名或密码错误");
                        break;
                    case 1049:
                        LogManager.Instance.LogInfo("未知数据库");
                        break;
                    case 2002:
                        LogManager.Instance.LogInfo("连接超时或服务器无响应");
                        break;
                    default:
                        LogManager.Instance.LogInfo($"MySQL错误代码: {ex.Number}");
                        break;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"数据库连接测试失败: {ex.Message}");
                return false;
            }
        }

        #region 部门与人员同步方法

        // 新增：部门实体
        public class Department
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public int SortOrder { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        /// <summary>
        /// 获取所有部门（用于注册窗口下拉列表）
        /// </summary>
        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            const string sql = @"
              SELECT
                  id AS Id,
                  name AS Name,
                  display_name AS DisplayName,
                  sort_order AS SortOrder,
                  is_active AS IsActive,
                  created_at AS CreatedAt,
                  updated_at AS UpdatedAt
              FROM departments
              ORDER BY sort_order, name";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var depts = await connection.QueryAsync<Department>(sql).ConfigureAwait(false);
                return depts.AsList();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"GetAllDepartmentsAsync 出错: {ex.Message}");
                return new List<Department>();
            }
        }


        /// <summary>
        /// 将 cad_categories 中尚未映射到 departments 的分类逐条创建部门并建立映射。
        /// 保持幂等：已存在的映射或同名部门不会重复创建（会尝试复用同名部门）。
        /// </summary>
        public async Task SyncDepartmentsFromCadCategoriesAsync()
        {
            try
            {
                var categories = await GetAllCadCategoriesAsync().ConfigureAwait(false);
                if (categories == null || categories.Count == 0) return;

                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                foreach (var cat in categories)
                {
                    // 检查是否已有映射
                    var mapSql = "SELECT department_id FROM category_department_map WHERE category_id = @CategoryId";
                    var mapped = await conn.QueryFirstOrDefaultAsync<int?>(mapSql, new { CategoryId = cat.Id }).ConfigureAwait(false);
                    if (mapped.HasValue) continue;

                    // 尝试按名称查找已有部门
                    var deptSql = "SELECT id FROM departments WHERE name = @Name LIMIT 1";
                    var deptId = await conn.QueryFirstOrDefaultAsync<int?>(deptSql, new { Name = cat.Name }).ConfigureAwait(false);
                    if (!deptId.HasValue)
                    {
                        // 插入新部门
                        var insertDeptSql = @"INSERT INTO departments (name, display_name, sort_order, created_at, updated_at) 
                                              VALUES (@Name, @DisplayName, @SortOrder, NOW(), NOW());
                                              SELECT LAST_INSERT_ID();";
                        var newDeptId = await conn.ExecuteScalarAsync<int>(insertDeptSql, new { Name = cat.Name, DisplayName = cat.DisplayName ?? cat.Name, SortOrder = cat.SortOrder }).ConfigureAwait(false);
                        deptId = newDeptId;
                    }

                    if (deptId.HasValue)
                    {
                        // 建立映射
                        var insertMap = "INSERT INTO category_department_map (category_id, department_id) VALUES (@CategoryId, @DepartmentId)";
                        await conn.ExecuteAsync(insertMap, new { CategoryId = cat.Id, DepartmentId = deptId.Value }).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"SyncDepartmentsFromCadCategoriesAsync 出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除与分类相关的部门映射与（可选）部门记录。
        /// 当分类被删除时调用：如果该部门没有其他映射且没有用户（或你选择直接删除），则删除部门。
        /// </summary>
        public async Task RemoveDepartmentMappingForCategoryAsync(int categoryId)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                var getDeptSql = "SELECT department_id FROM category_department_map WHERE category_id = @CategoryId";
                var deptId = await conn.QueryFirstOrDefaultAsync<int?>(getDeptSql, new { CategoryId = categoryId }).ConfigureAwait(false);
                if (!deptId.HasValue)
                {
                    // 无映射， nothing to do
                    return;
                }

                // 删除映射
                var delMapSql = "DELETE FROM category_department_map WHERE category_id = @CategoryId";
                await conn.ExecuteAsync(delMapSql, new { CategoryId = categoryId }).ConfigureAwait(false);

                // 检查该部门是否仍被其它分类映射或有用户
                var usedByCatSql = "SELECT COUNT(*) FROM category_department_map WHERE department_id = @DepartmentId";
                var usedByCat = await conn.QuerySingleAsync<int>(usedByCatSql, new { DepartmentId = deptId.Value }).ConfigureAwait(false);
                var userCountSql = "SELECT COUNT(*) FROM users WHERE department_id = @DepartmentId";
                var userCount = await conn.QuerySingleAsync<int>(userCountSql, new { DepartmentId = deptId.Value }).ConfigureAwait(false);

                if (usedByCat == 0 && userCount == 0)
                {
                    var delDeptSql = "DELETE FROM departments WHERE id = @DepartmentId";
                    await conn.ExecuteAsync(delDeptSql, new { DepartmentId = deptId.Value }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"RemoveDepartmentMappingForCategoryAsync 出错: {ex.Message}");
            }
        }

        #endregion

        #region CAD分类操作

        /// <summary>
        /// 添加CAD分类（并同步创建部门映射）
        /// </summary>
        public async Task<int> AddCadCategoryAsync(CadCategory category)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO cad_categories (name, display_name, sort_order) 
                VALUES (@Name, @DisplayName, @SortOrder)";
            var affected = await connection.ExecuteAsync(sql, category).ConfigureAwait(false);

            // 异步触发同步（保证分类与部门一致）
            _ = SyncDepartmentsFromCadCategoriesAsync();
            return affected;
        }

        /// <summary>
        /// 修改CAD分类（并同步部门信息）
        /// </summary>
        public async Task<int> UpdateCadCategoryAsync(CadCategory category)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE cad_categories 
                SET name = @Name, display_name = @DisplayName, sort_order = @SortOrder, updated_at = NOW() 
                WHERE id = @Id";
            var affected = await connection.ExecuteAsync(sql, category).ConfigureAwait(false);

            // 如果分类名或显示名变更，更新对应部门（若已存在映射）
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                var mapSql = "SELECT department_id FROM category_department_map WHERE category_id = @CategoryId";
                var deptId = await conn.QueryFirstOrDefaultAsync<int?>(mapSql, new { CategoryId = category.Id }).ConfigureAwait(false);
                if (deptId.HasValue)
                {
                    var updateDeptSql = @"UPDATE departments SET name = @Name, display_name = @DisplayName, updated_at = NOW() WHERE id = @Id";
                    await conn.ExecuteAsync(updateDeptSql, new { Name = category.Name, DisplayName = category.DisplayName ?? category.Name, Id = deptId.Value }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"UpdateCadCategoryAsync 同步部门时出错: {ex.Message}");
            }

            // 确保全量同步以修正遗漏
            _ = SyncDepartmentsFromCadCategoriesAsync();
            return affected;
        }

        /// <summary>
        /// 删除CAD分类（并删除部门映射及可选部门）
        /// </summary>
        public async Task<int> DeleteCadCategoryAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM cad_categories WHERE id = @Id";
            var affected = await connection.ExecuteAsync(sql, new { Id = id }).ConfigureAwait(false);

            // 删除映射并在必要时删除部门
            _ = RemoveDepartmentMappingForCategoryAsync(id);
            return affected;
        }

        /// <summary>
        /// 获取所有CAD分类
        /// </summary>
        /// <returns> 返回List<CadCategory>分类list</returns>
        public async Task<List<CadCategory>> GetAllCadCategoriesAsync()
        {
            try
            {
                const string sql = @"
                                   SELECT 
                                       id AS Id,
                                       name AS Name,
                                       display_name AS DisplayName,
                                       subcategory_ids AS SubcategoryIds,
                                       sort_order AS SortOrder,
                                       created_at AS CreatedAt,
                                       updated_at AS UpdatedAt
                                   FROM cad_categories 
                                   ORDER BY sort_order";

                using var connection = new MySqlConnection(_connectionString);
                var categories = await connection.QueryAsync<CadCategory>(sql);
                LogManager.Instance.LogInfo($"查询返回 {categories.AsList().Count} 条记录");
                return categories.AsList();

            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"数据库查询出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 根据名称获取CAD分类
        /// </summary>
        public async Task<CadCategory> GetCadCategoryByNameAsync(string categoryName)
        {
            const string sql = @"
            SELECT 
               id AS Id,
               name AS Name,
               display_name AS DisplayName,
               subcategory_ids AS SubcategoryIds,
               sort_order AS SortOrder,
               created_at AS CreatedAt,
               updated_at AS UpdatedAt
            FROM cad_categories 
            WHERE name LIKE @Name";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var parameters = new Dictionary<string, object>();
                parameters.Add("Name", $"%{categoryName}%");
                var result = await connection.QuerySingleOrDefaultAsync<CadCategory>(sql, parameters);
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"数据库查询出错: {ex.Message}");
                throw;
            }


        }

        #endregion

        #region CAD子分类操作


        /// <summary>
        /// 获取所有CAD子分类
        /// </summary>
        /// <returns></returns>
        public async Task<List<CadSubcategory>> GetAllCadSubcategoriesAsync()
        {
            using var connection = GetConnection();
            //var sql = "SELECT * FROM cad_subcategories ORDER BY parent_id, id, parent_id, name, display_name, level , subcategory_ids, sort_order";
            const string sql = @"
                               SELECT 
                                   id AS Id,
                                   parent_id AS ParentId,
                                   name AS Name,
                                   display_name AS DisplayName,
                                   sort_order AS SortOrder,
                                   level AS Level,
                                   subcategory_ids AS SubcategoryIds,
                                   created_at AS CreatedAt,
                                   updated_at AS UpdatedAt
                               FROM cad_subcategories 
                               ORDER BY parent_id, id, parent_id, name, display_name, level , subcategory_ids, sort_order";
            return (await connection.QueryAsync<CadSubcategory>(sql)).AsList();
        }

        /// <summary>
        /// 通过Id获取子分类的方法
        /// </summary>
        /// <returns>  </returns>
        public async Task<CadSubcategory> GetCadSubcategoryByIdAsync(int id)
        {
            const string sql = @"
                               SELECT 
                                   id AS Id,
                                   parent_id AS ParentId,
                                   name AS Name,
                                   display_name AS DisplayName,
                                   sort_order AS SortOrder,
                                   level AS Level,
                                   subcategory_ids AS SubcategoryIds,
                                   created_at AS CreatedAt,
                                   updated_at AS UpdatedAt
                               FROM cad_subcategories 
                               WHERE id = @id";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<CadSubcategory>(sql, new { id });
        }


        /// <summary>
        /// 根据子分类ID获取这个子分类同级的所有兄弟子分类
        /// </summary>
        public async Task<List<CadSubcategory>> GetCadSubcategoriesByCategoryIdAsync(int categoryId)
        {
            const string sql = @"
                               SELECT 
                                    id AS Id,
                                    parent_id AS ParentId,
                                    name AS Name,
                                    display_name AS DisplayName,
                                    sort_order AS SortOrder,
                                    level AS Level,
                                    subcategory_ids AS SubcategoryIds,
                                    created_at AS CreatedAt,
                                    updated_at AS UpdatedAt
                               FROM cad_subcategories 
                               WHERE parent_id = @ParentId 
                               ORDER BY sort_order";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var parameters = new Dictionary<string, object>();
                parameters.Add("ParentId", categoryId);

                var subcategories = await connection.QueryAsync<CadSubcategory>(sql, parameters);
                return subcategories.AsList();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"数据库查询出错: {ex.Message}");
                throw;
            }

        }

        /// <summary>
        /// 根据父ID获取子分类（用于递归加载）
        /// </summary>
        public async Task<List<CadSubcategory>> GetCadSubcategoriesByParentIdAsync(int parentId)
        {
            try
            {
                const string sql = @"
                               SELECT 
                                   *
                               FROM cad_subcategories 
                               WHERE parent_id = @parentId 
                               ORDER BY sort_order";

                using var connection = new MySqlConnection(_connectionString);
                var subcategories = await connection.QueryAsync<CadSubcategory>(sql, new { parentId });
                return subcategories.AsList();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取子分类时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 添加CAD子分类
        /// </summary>
        /// <param name="subcategory"></param>
        /// <returns>返回受影响的行数</returns>
        public async Task<int> AddCadSubcategoryAsync(CadSubcategory subcategory)
        {
            try
            {
                // 验证输入参数
                if (subcategory == null)
                    throw new ArgumentNullException(nameof(subcategory));

                if (string.IsNullOrEmpty(subcategory.Name))
                    throw new ArgumentException("子分类名称不能为空", nameof(subcategory.Name));

                using var connection = GetConnection();

                // SQL语句修正：移除id字段（假设是自增），修正参数名
                var sql = @"INSERT INTO cad_subcategories ( id,parent_id, name, display_name, sort_order, level, subcategory_ids, created_at, updated_at) 
            VALUES ( @Id, @ParentId, @Name, @DisplayName, @SortOrder, @Level, @SubcategoryIds, NOW(), NOW())";

                var result = await connection.ExecuteAsync(sql, subcategory);
                LogManager.Instance.LogInfo($"成功添加子分类: {subcategory.Name}");
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"添加CAD子分类时出错: {ex.Message}");
                // throw new Exception($"添加CAD子分类失败: {ex.Message}", ex);
                return 0;
            }
        }
        /// <summary>
        /// 修改CAD子分类
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task<int> UpdateCadSubcategoryAsync(CadSubcategory cadSubcategory)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE cad_subcategories 
           SET  parent_id = @ParentId, name = @Name, display_name = @DisplayName, subcategory_ids = @SubcategoryIds,sort_order = @SortOrder, updated_at = NOW() 
           WHERE id = @Id";
            return await connection.ExecuteAsync(sql, cadSubcategory);
        }
        /// <summary>
        /// 添加更新父级子分类列表的方法
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="newSubcategoryId"></param>
        /// <returns></returns>
        public async Task<int> UpdateParentSubcategoryListAsync(int parentId, int newSubcategoryId)
        {
            try
            {
                using var connection = GetConnection();
                string selectSql;// 获取父级记录
                object parameters;
                if (parentId >= 10000)
                {
                    selectSql = "SELECT subcategory_ids FROM cad_subcategories WHERE id = @parentId";   // 父级是子分类
                    parameters = new { parentId };
                }
                else
                {
                    selectSql = "SELECT subcategory_ids FROM cad_categories WHERE id = @parentId";   // 父级是主分类
                    parameters = new { parentId };
                }
                string currentSubcategoryIds = await connection.QuerySingleOrDefaultAsync<string>(selectSql, parameters);
                string newSubcategoryIds;// 更新子分类列表
                if (string.IsNullOrEmpty(currentSubcategoryIds))
                {
                    newSubcategoryIds = newSubcategoryId.ToString();// 创建新的子分类列表
                }
                else
                {
                    var ids = currentSubcategoryIds.Split(',').Select(id => id.Trim()).ToList();// 将字符串转换为列表
                    if (!ids.Contains(newSubcategoryId.ToString()))// 如果不存在
                    {
                        ids.Add(newSubcategoryId.ToString());// 添加
                        newSubcategoryIds = string.Join(",", ids);// 重新组合为字符串
                    }
                    else
                    {
                        newSubcategoryIds = currentSubcategoryIds; // 已存在，不需要更新
                    }
                }
                string updateSql; // 更新数据库
                if (parentId >= 10000)
                {
                    updateSql = "UPDATE cad_subcategories SET subcategory_ids = @newSubcategoryIds, updated_at = NOW() WHERE id = @parentId"; // 更新子分类表
                }
                else
                {
                    updateSql = "UPDATE cad_categories SET subcategory_ids = @newSubcategoryIds, updated_at = NOW() WHERE id = @parentId"; // 更新主分类表
                }

                return await connection.ExecuteAsync(updateSql, new { newSubcategoryIds, parentId });
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新父级子分类列表失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 添加更新父级子分类列表的方法
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="newSubcategoryIds"></param>
        /// <returns></returns>
        public async Task<int> UpdateParentSubcategoryListAsync(int parentId, string newSubcategoryIds)
        {
            try
            {
                using var connection = GetConnection();// 创建数据库连接
                string updateSql; // 更新数据库
                if (parentId >= 10000)
                {
                    updateSql = "UPDATE cad_subcategories SET subcategory_ids = @newSubcategoryIds, updated_at = NOW() WHERE id = @parentId"; // 更新子分类表
                }
                else
                {
                    updateSql = "UPDATE cad_categories SET subcategory_ids = @newSubcategoryIds, updated_at = NOW() WHERE id = @parentId"; // 更新主分类表
                }

                return await connection.ExecuteAsync(updateSql, new { newSubcategoryIds, parentId });
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新父级子分类列表失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 删除CAD子分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteCadSubcategoryAsync(int id)
        {
            //这个方法还有不完善的地方，比如子分类下还有子分类或图元，如果不删除子分类下的图元，则无法删除子分类，需要前删除子分类下的图元，不然这个分类下的图元与子分类就是在数据库中的垃圾数据；
            using var connection = GetConnection();
            var sql = "DELETE FROM cad_subcategories WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id });
        }
        #endregion

        #region SW分类操作
        /// <summary>
        /// 获取所有SW分类
        /// </summary>
        public async Task<List<SwCategory>> GetAllSwCategoriesAsync()
        {
            const string sql = @"
            SELECT 
                id,
                name,
                display_name,
                sort_order,
                created_at,
                updated_at
            FROM sw_categories 
            ORDER BY sort_order";

            using var connection = new MySqlConnection(_connectionString);
            var categories = await connection.QueryAsync<SwCategory>(sql);
            return categories.AsList();
        }

        /// <summary>
        /// 添加SW分类
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task<int> AddSwCategoryAsync(SwCategory category)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO sw_categories (name, display_name, sort_order) 
                VALUES (@Name, @DisplayName, @SortOrder)";
            return await connection.ExecuteAsync(sql, category);
        }
        /// <summary>
        /// 修改SW分类
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task<int> UpdateSwCategoryAsync(SwCategory category)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE sw_categories 
                SET name = @Name, display_name = @DisplayName, sort_order = @SortOrder, updated_at = NOW() 
                WHERE id = @Id";
            return await connection.ExecuteAsync(sql, category);
        }
        /// <summary>
        /// 删除SW分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteSwCategoryAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM sw_categories WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id });
        }
        /// <summary>
        /// 根据父ID获取SW子分类
        /// </summary>
        public async Task<List<SwSubcategory>> GetSwSubcategoriesByParentIdAsync(int parentId)
        {
            using var connection = GetConnection();
            var sql = @"SELECT * FROM sw_subcategories 
                WHERE parent_id = @ParentId 
                ORDER BY sort_order, name";
            return (await connection.QueryAsync<SwSubcategory>(sql, new { ParentId = parentId })).AsList();
        }
        #endregion

        #region SW子分类操作
        /// <summary>
        /// 获取指定SW分类下的所有SW子分类
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public async Task<List<SwSubcategory>> GetSwSubcategoriesByCategoryIdAsync(int categoryId)
        {
            using var connection = GetConnection();
            var sql = @"SELECT * FROM sw_subcategories 
                WHERE category_id = @CategoryId 
                ORDER BY parent_id, sort_order, name";
            return (await connection.QueryAsync<SwSubcategory>(sql, new { CategoryId = categoryId })).AsList();
        }
        /// <summary>
        /// 获取所有SW子分类
        /// </summary>
        /// <returns></returns>
        public async Task<List<SwSubcategory>> GetAllSwSubcategoriesAsync()
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM sw_subcategories ORDER BY category_id, parent_id, sort_order, name";
            return (await connection.QueryAsync<SwSubcategory>(sql)).AsList();
        }
        /// <summary>
        /// 添加SW子分类
        /// </summary>
        /// <param name="subcategory"></param>
        /// <returns></returns>
        public async Task<int> AddSwSubcategoryAsync(SwSubcategory subcategory)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO sw_subcategories (category_id, name, display_name, parent_id, sort_order) 
                VALUES (@CategoryId, @Name, @DisplayName, @ParentId, @SortOrder)";
            return await connection.ExecuteAsync(sql, subcategory);
        }
        /// <summary>
        /// 修改SW子分类
        /// </summary>
        /// <param name="subcategory"></param>
        /// <returns></returns>
        public async Task<int> UpdateSwSubcategoryAsync(SwSubcategory subcategory)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE sw_subcategories 
                SET name = @Name, display_name = @DisplayName, parent_id = @ParentId, sort_order = @SortOrder, updated_at = NOW() 
                WHERE id = @Id";
            return await connection.ExecuteAsync(sql, subcategory);
        }
        /// <summary>
        /// 删除SW子分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteSwSubcategoryAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM sw_subcategories WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id });
        }
        #endregion

        #region SW图元操作
        /// <summary>
        /// 根据子分类ID获取SW图元
        /// </summary>
        public async Task<List<SwGraphic>> GetSwGraphicsBySubcategoryIdAsync(int subcategoryId)
        {
            const string sql = @"
                               SELECT 
                                   id,
                                   subcategory_id,
                                   file_name,
                                   display_name,
                                   file_path,
                                   preview_image_path,
                                   file_size,
                                   created_at,
                                   updated_at
                               FROM sw_graphics 
                               WHERE subcategory_id = @subcategoryId 
                               ORDER BY file_name";

            using var connection = new MySqlConnection(_connectionString);
            var graphics = await connection.QueryAsync<SwGraphic>(sql, new { subcategoryId });
            return graphics.AsList();
        }

        /// <summary>
        /// 添加SW图元
        /// </summary>
        /// <param name="graphic"></param>
        /// <returns></returns>
        public async Task<int> AddSwGraphicAsync(SwGraphic graphic)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO sw_graphics (subcategory_id, file_name, display_name, file_path, preview_image_path, file_size) 
                VALUES (@SubcategoryId, @FileName, @DisplayName, @FilePath, @PreviewImagePath, @FileSize)";
            return await connection.ExecuteAsync(sql, graphic);
        }

        /// <summary>
        /// 修改SW图元
        /// </summary>
        /// <param name="graphic"></param>
        /// <returns></returns>
        public async Task<int> UpdateSwGraphicAsync(SwGraphic graphic)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE sw_graphics 
                SET file_name = @FileName, display_name = @DisplayName, file_path = @FilePath, 
                    preview_image_path = @PreviewImagePath, file_size = @FileSize, updated_at = NOW() 
                WHERE id = @Id";
            return await connection.ExecuteAsync(sql, graphic);
        }

        /// <summary>
        /// 删除SW图元
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteSwGraphicAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM sw_graphics WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id });
        }

        /// <summary>
        /// 获取SW图元
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<SwGraphic> GetSwGraphicByIdAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM sw_graphics WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<SwGraphic>(sql, new { Id = id });
        }
        #endregion

        #region 设备表相关操作

        /// <summary>
        /// 获取所有设备信息（用于设备表生成）
        /// </summary>
        public async Task<List<EquipmentInfo>> GetAllEquipmentInfoAsync()
        {
            const string sql = @"
            SELECT 
                equipment_id AS Id,
                equipment_name AS Name,
                equipment_type AS Type,
                medium_name AS MediumName,
                specifications AS Specifications,
                material AS Material,
                quantity AS Quantity,
                drawing_number AS DrawingNumber,
                power AS Power,
                volume AS Volume,
                pressure AS Pressure,
                temperature AS Temperature,
                diameter AS Diameter,
                length AS Length,
                thickness AS Thickness,
                weight AS Weight,
                model AS Model,
                remarks AS Remarks
            FROM equipment_info 
            ORDER BY equipment_name";

            using var connection = new MySqlConnection(_connectionString);
            var equipmentList = await connection.QueryAsync<EquipmentInfo>(sql);
            return equipmentList.AsList();
        }

        /// <summary>
        /// 批量插入设备信息
        /// </summary>
        public async Task<int> InsertEquipmentInfoBatchAsync(List<EquipmentInfo> equipmentList)
        {
            const string sql = @"
            INSERT INTO equipment_info (
                equipment_name, equipment_type, medium_name, specifications,
                material, quantity, drawing_number, power, volume, pressure,
                temperature, diameter, length, thickness, weight, model, remarks
            ) VALUES (
                @Name, @Type, @MediumName, @Specifications,
                @Material, @Quantity, @DrawingNumber, @Power, @Volume, @Pressure,
                @Temperature, @Diameter, @Length, @Thickness, @Weight, @Model, @Remarks
            )";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, equipmentList);
        }

        #endregion

        #region 事务操作示例

        /// <summary>
        /// 事务操作示例：批量更新图元信息
        /// </summary>
        public async Task<bool> UpdateFileBatchAsync(List<FileStorage> file)
        {
            const string updateSql = @"
            UPDATE cad_file_storage 
            SET display_name = @DisplayName,
                file_path = @FilePath,
                preview_image_path = @PreviewImagePath,
                updated_at = NOW()
            WHERE id = @Id";

            using var connection = new MySqlConnection(_connectionString);
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.ExecuteAsync(updateSql, file, transaction);
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        #endregion

        #region 系统配置操作

        /// <summary>
        /// 获取系统配置
        /// </summary>
        /// <param name="configKey"></param>
        /// <returns></returns>
        public async Task<string> GetConfigValueAsync(string configKey)
        {
            using var connection = GetConnection();
            var sql = "SELECT config_value FROM system_config WHERE config_key = @ConfigKey";
            return await connection.QueryFirstOrDefaultAsync<string>(sql, new { ConfigKey = configKey });
        }

        /// <summary>
        /// 设置系统配置
        /// </summary>
        /// <param name="configKey"></param>
        /// <param name="configValue"></param>
        /// <returns></returns>
        public async Task<int> SetConfigValueAsync(string configKey, string configValue)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO system_config (config_key, config_value) 
                VALUES (@ConfigKey, @ConfigValue) 
                ON DUPLICATE KEY UPDATE config_value = @ConfigValue";
            return await connection.ExecuteAsync(sql, new { ConfigKey = configKey, ConfigValue = configValue });
        }

        /// <summary>
        /// 获取所有系统配置
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetAllConfigAsync()
        {
            using var connection = GetConnection();
            var sql = "SELECT config_key, config_value FROM system_config";
            var result = await connection.QueryAsync<(string, string)>(sql);
            return result.ToDictionary(x => x.Item1, x => x.Item2);
        }

        /// <summary>
        /// 获取CAD分类的最大排序序号
        /// </summary>
        public async Task<int> GetMaxCadCategorySortOrderAsync()
        {
            const string sql = "SELECT COALESCE(MAX(sort_order), 0) FROM cad_categories";

            using var connection = new MySqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync<int>(sql);
            return result;
        }

        /// <summary>
        /// 获取指定父分类下子分类的最大排序序号
        /// </summary>
        public async Task<int> GetMaxCadSubcategorySortOrderAsync(int parentId)
        {
            const string sql = "SELECT COALESCE(MAX(sort_order), 0) FROM cad_subcategories WHERE parent_id = @parentId";

            using var connection = new MySqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync<int>(sql, new { parentId });
            return result;
        }

        /// <summary>
        /// 获取所有子分类的最大排序序号（用于主分类下的直接子分类）
        /// </summary>
        public async Task<int> GetMaxCadSubcategorySortOrderForMainCategoryAsync(int parentId)
        {
            const string sql = "SELECT COALESCE(MAX(sort_order), 0) FROM cad_subcategories WHERE parent_id = @ParentId";

            using var connection = new MySqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync<int>(sql, new { parentId });
            return result;
        }

        #endregion

        #region 优化的文件管理方法

        public async Task<int?> GetAttributeDefinitionIdByKeyNameAsync(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return null;
            try
            {
                using var conn = GetConnection();
                const string sql = "SELECT id FROM attribute_definitions WHERE key_name = @KeyName LIMIT 1";
                var id = await conn.ExecuteScalarAsync<int?>(sql, new { KeyName = keyName }).ConfigureAwait(false);
                return id;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"GetAttributeDefinitionIdByKeyNameAsync 出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 新增：按 id 批量/单个读取 attribute_definitions（放在 DatabaseManager 类中）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<DbAttributeDefinition> GetAttributeDefinitionByIdAsync(int id)
        {
            try
            {
                using var conn = GetConnection();
                const string sql = @"
            SELECT id AS Id, key_name AS KeyName, display_name AS DisplayName, data_type AS DataType, unit AS Unit,
                   is_searchable AS IsSearchable, is_core_field AS IsCoreField, validation_regex AS ValidationRegex,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM attribute_definitions
            WHERE id = @Id
            LIMIT 1";
                return await conn.QueryFirstOrDefaultAsync<DbAttributeDefinition>(sql, new { Id = id }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"GetAttributeDefinitionByIdAsync 出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取属性定义
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<Dictionary<int, DbAttributeDefinition>> GetAttributeDefinitionsByIdsAsync(IEnumerable<int> ids)
        {
            var dict = new Dictionary<int, DbAttributeDefinition>();
            try
            {
                var idArray = ids?.Where(i => i > 0).Distinct().ToArray();
                if (idArray == null || idArray.Length == 0) return dict;

                using var conn = GetConnection();
                const string sql = @"
            SELECT id AS Id, key_name AS KeyName, display_name AS DisplayName, data_type AS DataType, unit AS Unit,
                   is_searchable AS IsSearchable, is_core_field AS IsCoreField, validation_regex AS ValidationRegex,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM attribute_definitions
            WHERE id IN @Ids";
                var rows = (await conn.QueryAsync<DbAttributeDefinition>(sql, new { Ids = idArray }).ConfigureAwait(false)).AsList();
                foreach (var r in rows) dict[r.Id] = r;
                return dict;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"GetAttributeDefinitionsByIdsAsync 出错: {ex.Message}");
                return dict;
            }
        }

        public async Task<int?> EnsureAttributeDefinitionExistsAsync(string keyName, string displayName = null, string dataType = "string")
        {
            if (string.IsNullOrWhiteSpace(keyName)) return null;
            try
            {
                // 先尝试查找
                var existed = await GetAttributeDefinitionIdByKeyNameAsync(keyName).ConfigureAwait(false);
                if (existed.HasValue) return existed.Value;

                using var conn = GetConnection();
                const string insertSql = @"
            INSERT INTO attribute_definitions (key_name, display_name, data_type, created_at, updated_at)
            VALUES (@KeyName, @DisplayName, @DataType, NOW(), NOW());
            SELECT LAST_INSERT_ID();";
                var newId = await conn.ExecuteScalarAsync<int?>(insertSql, new { KeyName = keyName, DisplayName = displayName ?? keyName, DataType = dataType }).ConfigureAwait(false);
                return newId;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"EnsureAttributeDefinitionExistsAsync 出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 尝试从 JSON 文本中抽取可能的属性 keyName（容错，返回第一个匹配项）
        /// 支持形式："key_name":"xxx" / "keyName":"xxx" / "key":"xxx" / "attribute":"xxx"
        /// </summary>
        private static bool TryExtractKeyNameFromJson(string json, out string keyName)
        {
            keyName = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            // 简单正则匹配键值对
            var patterns = new[]
            {
        "\"key_name\"\\s*:\\s*\"(?<v>[^\"]+)\"",
        "\"keyName\"\\s*:\\s*\"(?<v>[^\"]+)\"",
        "\"key\"\\s*:\\s*\"(?<v>[^\"]+)\"",
        "\"attribute\"\\s*:\\s*\"(?<v>[^\"]+)\""
    };

            foreach (var p in patterns)
            {
                var m = Regex.Match(json, p, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    keyName = m.Groups["v"].Value.Trim();
                    if (!string.IsNullOrEmpty(keyName)) return true;
                }
            }

            return false;
        }

        public async Task<bool> SaveFileAttributeValueAsync(FileAttributeValue fav)
        {
            if (fav == null) throw new ArgumentNullException(nameof(fav));

            // 如果没有 attribute id，尝试从 ValueJson 中提取 key 并创建定义
            if (fav.AttributeId <= 0)
            {
                string keyName = null;
                if (!string.IsNullOrWhiteSpace(fav.ValueJson) && TryExtractKeyNameFromJson(fav.ValueJson, out keyName))
                {
                    var ensured = await EnsureAttributeDefinitionExistsAsync(keyName).ConfigureAwait(false);
                    if (ensured.HasValue)
                    {
                        fav.AttributeId = ensured.Value;
                    }
                }
            }

            // 基本验证
            if (fav.FileId <= 0)
            {
                LogManager.Instance.LogInfo("SaveFileAttributeValueAsync: FileId 无效");
                return false;
            }
            if (fav.AttributeId <= 0)
            {
                LogManager.Instance.LogInfo("SaveFileAttributeValueAsync: AttributeId 无效，且无法从上下文创建 attribute_definitions");
                return false;
            }

            try
            {
                var ok = await EnsureFileAttributeValuesTableExistsAsync().ConfigureAwait(false);
                if (!ok)
                {
                    LogManager.Instance.LogInfo("SaveFileAttributeValueAsync: 无法确保 file_attribute_values 表存在");
                    return false;
                }

                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                const string upsertSql = @"
            INSERT INTO file_attribute_values (file_id, attribute_id, value_string, value_number, value_date, value_json, created_at, updated_at)
            VALUES (@FileId, @AttributeId, @ValueString, @ValueNumber, @ValueDate, @ValueJson, NOW(), NOW())
            ON DUPLICATE KEY UPDATE
                value_string = VALUES(value_string),
                value_number = VALUES(value_number),
                value_date = VALUES(value_date),
                value_json = VALUES(value_json),
                updated_at = NOW();";

                await conn.ExecuteAsync(upsertSql, new
                {
                    FileId = fav.FileId,
                    AttributeId = fav.AttributeId,
                    ValueString = fav.ValueString,
                    ValueNumber = fav.ValueNumber,
                    ValueDate = fav.ValueDate,
                    ValueJson = fav.ValueJson
                }).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"SaveFileAttributeValueAsync 出错: {ex}");
                return false;
            }
        }

        public async Task<bool> SaveFileAttributeValuesAsync(IEnumerable<FileAttributeValue> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var list = values.ToList();
            if (list.Count == 0) return true;

            // 预处理：对缺失 attributeId 的项尝试从 ValueJson 提取 key 并创建定义
            for (int i = 0; i < list.Count; i++)
            {
                var fav = list[i];
                if (fav == null) continue;
                if (fav.AttributeId > 0) continue;

                string keyName = null;
                if (!string.IsNullOrWhiteSpace(fav.ValueJson) && TryExtractKeyNameFromJson(fav.ValueJson, out keyName))
                {
                    var ensured = await EnsureAttributeDefinitionExistsAsync(keyName).ConfigureAwait(false);
                    if (ensured.HasValue)
                    {
                        fav.AttributeId = ensured.Value;
                    }
                }
            }

            // 基本校验：至少有有效项
            if (!list.Any(v => v != null && v.FileId > 0 && v.AttributeId > 0))
            {
                LogManager.Instance.LogInfo("SaveFileAttributeValuesAsync: 无有效待保存项");
                return false;
            }

            try
            {
                var ok = await EnsureFileAttributeValuesTableExistsAsync().ConfigureAwait(false);
                if (!ok)
                {
                    LogManager.Instance.LogInfo("SaveFileAttributeValuesAsync: 无法确保 file_attribute_values 表存在");
                    return false;
                }

                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                using var tx = conn.BeginTransaction();

                const string upsertSql = @"
            INSERT INTO file_attribute_values (file_id, attribute_id, value_string, value_number, value_date, value_json, created_at, updated_at)
            VALUES (@FileId, @AttributeId, @ValueString, @ValueNumber, @ValueDate, @ValueJson, NOW(), NOW())
            ON DUPLICATE KEY UPDATE
                value_string = VALUES(value_string),
                value_number = VALUES(value_number),
                value_date = VALUES(value_date),
                value_json = VALUES(value_json),
                updated_at = NOW();";

                try
                {
                    foreach (var fav in list)
                    {
                        if (fav == null) continue;
                        if (fav.FileId <= 0 || fav.AttributeId <= 0) continue;
                        await conn.ExecuteAsync(upsertSql, new
                        {
                            FileId = fav.FileId,
                            AttributeId = fav.AttributeId,
                            ValueString = fav.ValueString,
                            ValueNumber = fav.ValueNumber,
                            ValueDate = fav.ValueDate,
                            ValueJson = fav.ValueJson
                        }, tx).ConfigureAwait(false);
                    }

                    tx.Commit();
                    return true;
                }
                catch (Exception exTx)
                {
                    try { tx.Rollback(); } catch { }
                    LogManager.Instance.LogInfo($"SaveFileAttributeValuesAsync 事务失败: {exTx}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"SaveFileAttributeValuesAsync 出错: {ex}");
                return false;
            }
        }


        /// <summary>
        /// 尝试获取给定分类关联的属性模板（若无模板则返回空集合）。
        /// 实现对数据库缺失的容错：若表不存在或查询失败，直接返回空列表。
        /// </summary>
        public async Task<List<AttributeTemplate>> GetAttributeTemplatesForCategoryAsync(int categoryId)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                // 检查表是否存在（容错）
                var tableExists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = @db AND table_name = 'attribute_templates';",
                    new { db = conn.Database }).ConfigureAwait(false);

                if (tableExists == 0)
                    return new List<AttributeTemplate>();

                // 读取模板及其项（简单实现，不做复杂缓存）
                const string sqlTemplates = @"SELECT id AS Id, name AS Name, description AS Description FROM attribute_templates WHERE category_id = @CategoryId";
                var templates = (await conn.QueryAsync<AttributeTemplate>(sqlTemplates, new { CategoryId = categoryId }).ConfigureAwait(false)).AsList();

                if (templates == null || templates.Count == 0)
                    return new List<AttributeTemplate>();

                // 读取所有模板项
                var templateIds = templates.Select(t => t.Id).ToArray();
                if (templateIds.Length == 0)
                    return templates;

                const string sqlItems = @"SELECT id AS Id, template_id AS TemplateId, attribute_id AS AttributeId, required AS Required, display_order AS DisplayOrder
                                          FROM attribute_template_items
                                          WHERE template_id IN @Ids
                                          ORDER BY display_order";
                var items = (await conn.QueryAsync<AttributeTemplateItem>(sqlItems, new { Ids = templateIds }).ConfigureAwait(false)).AsList();

                // 关联
                foreach (var t in templates)
                {
                    t.Items = items.Where(i => i.TemplateId == t.Id).OrderBy(i => i.DisplayOrder).ToList();
                }

                return templates;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"GetAttributeTemplatesForCategoryAsync 出错: {ex.Message}");
                return new List<AttributeTemplate>();
            }
        }
             
        /// <summary>
        /// 确保 file_attribute_values 表存在（带缓存与并发保护）
        /// </summary>
        private async Task<bool> EnsureFileAttributeValuesTableExistsAsync()
        {
            // 快路径：已检查且存在
            if (_fileAttrValuesTableChecked && _fileAttrValuesTableExists)
                return true;

            // 并发保护，只有第一个线程会做实际检查/创建
            lock (_fileAttrValuesTableLock)
            {
                if (_fileAttrValuesTableChecked && _fileAttrValuesTableExists)
                    return true;
            }

            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                // 查询是否存在（一次性检查）
                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = @db AND table_name = 'file_attribute_values';",
                    new { db = conn.Database }).ConfigureAwait(false);

                if (exists == 0)
                {
                    // 创建表（幂等）
                    var createSql = @"
                        CREATE TABLE IF NOT EXISTS `file_attribute_values` (
                            `id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                            `file_id` INT NOT NULL,
                            `attribute_id` INT NOT NULL,
                            `value_string` TEXT NULL,
                            `value_number` DOUBLE NULL,
                            `value_date` DATETIME NULL,
                            `value_json` TEXT NULL,
                            `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
                            `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                            UNIQUE KEY `ux_file_attr` (`file_id`,`attribute_id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                    await conn.ExecuteAsync(createSql).ConfigureAwait(false);
                }

                lock (_fileAttrValuesTableLock)
                {
                    _fileAttrValuesTableExists = true;
                    _fileAttrValuesTableChecked = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"EnsureFileAttributeValuesTableExistsAsync 出错: {ex.Message}");
                lock (_fileAttrValuesTableLock)
                {
                    _fileAttrValuesTableExists = false;
                    _fileAttrValuesTableChecked = true; // 已检查但失败，避免无限重试
                }
                return false;
            }
        }     

        /// <summary>
        /// 从 EAV 表 file_attribute_values 读取指定 fileId 的所有值（返回空列表而不是 null）。
        /// 使用 Ensure 方法避免重复检查 information_schema。
        /// </summary>
        public async Task<List<FileAttributeValue>> GetFileAttributeValuesAsync(int fileId)
        {
            try
            {
                if (fileId <= 0) return new List<FileAttributeValue>();

                var ok = await EnsureFileAttributeValuesTableExistsAsync().ConfigureAwait(false);
                if (!ok) return new List<FileAttributeValue>();

                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                const string sql = @"
                    SELECT id AS Id, file_id AS FileId, attribute_id AS AttributeId,
                           value_string AS ValueString, value_number AS ValueNumber, value_date AS ValueDate, value_json AS ValueJson,
                           created_at AS CreatedAt, updated_at AS UpdatedAt
                    FROM file_attribute_values
                    WHERE file_id = @FileId";
                var rows = (await conn.QueryAsync<FileAttributeValue>(sql, new { FileId = fileId }).ConfigureAwait(false)).AsList();
                return rows;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"GetFileAttributeValuesAsync 出错: {ex}");
                return new List<FileAttributeValue>();
            }
        }

        /// <summary>
        /// 获取分类下的所有文件（支持分页和排序）
        /// </summary>
        public async Task<List<FileStorage>> GetFilesByCategoryAsync(int categoryId, string categoryType = "sub", int page = 1, int pageSize = 50, string orderBy = "created_at DESC")
        {
            string sql = @"
        SELECT 
            id AS Id,
            category_id AS CategoryId,
            file_attribute_id AS FileAttributeId,
            file_name AS FileName,
            file_stored_name AS FileStoredName,
            file_type AS FileType,
            is_tianzheng AS IsTianZheng,
            file_hash AS FileHash,
            display_name AS DisplayName,
            element_block_name AS ElementBlockName,
            layer_name AS LayerName,
            color_index AS ColorIndex,
            file_path AS FilePath,
            preview_image_name AS PreviewImageName,
            preview_image_path AS PreviewImagePath,
            file_size AS FileSize,
            is_preview AS IsPreview,
            version AS Version,
            description AS Description,
            is_active AS IsActive,
            created_by AS CreatedBy,
            category_type AS CategoryType,
            title AS Title,
            keywords AS Keywords,
            is_public AS IsPublic,
            updated_by AS UpdatedBy,
            last_accessed_at AS LastAccessedAt,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM cad_file_storage
        WHERE category_id = @CategoryId 
          AND category_type = @CategoryType
          AND is_active = 1";
            try
            {
                if (!string.IsNullOrEmpty(orderBy))
                {
                    sql += $" ORDER BY {orderBy}";
                }
                sql += " LIMIT @offset, @pageSize";

                var parameters = new
                {
                    CategoryId = categoryId,
                    CategoryType = categoryType,
                    offset = (page - 1) * pageSize,
                    pageSize
                };

                using var connection = new MySqlConnection(_connectionString);
                return (await connection.QueryAsync<FileStorage>(sql, parameters)).AsList();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取分类下的文件时出错: {ex.Message}");
                return new List<FileStorage>();
            }
        }

        /// <summary>
        ///  获取分类下的所有文件
        /// </summary>
        /// <param name="categoryId">分类Id</param>
        /// <param name="categoryType">分类类型</param>
        /// <returns></returns>
        public async Task<List<FileStorage>> GetFilesByCategoryIdAsync(int categoryId, string categoryType)
        {
            const string sql = @"
                 SELECT 
                 id AS Id,
                 category_id AS CategoryId,
                 category_type AS CategoryType,
                 file_name AS FileName,
                 file_stored_name AS FileStoredName,
                 file_path AS FilePath,
                 file_type AS FileType,
                 is_tianzheng AS IsTianZheng,
                 file_size AS FileSize,
                 file_hash AS FileHash,
                 display_name AS DisplayName,
                 element_block_name AS ElementBlockName,
                 layer_name AS LayerName,
                 color_index AS ColorIndex,
                 preview_image_name AS PreviewImageName,
                 preview_image_path AS PreviewImagePath,
                 description AS Description,
                 version AS Version,
                 is_preview AS IsPreview,
                 is_active AS IsActive,
                 created_by AS CreatedBy,
                 created_at AS CreatedAt,
                 updated_at AS UpdatedAt
             FROM cad_file_storage 
             WHERE category_id = @CategoryId 
               AND category_type = @CategoryType
             ORDER BY created_at DESC";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var parameters = new Dictionary<string, object>();
                parameters.Add("CategoryId", categoryId);
                parameters.Add("CategoryType", categoryType);
                var result = await connection.QueryAsync<FileStorage>(sql, parameters);
                return result.AsList();
            }
            catch (Exception e)
            {
                Env.Editor.WriteMessage(e.Message);
                return new List<FileStorage>();
            }
        }

        /// <summary>
        /// 根据文件扩展名获取分类下的文件
        /// </summary>
        public async Task<List<FileStorage>> GetFilesByCategoryAndExtensionAsync(int categoryId, string fileType)
        {
            const string sql = @"
        SELECT 
            id AS Id,
            category_id AS CategoryId,
            file_attribute_id AS FileAttributeId,
            file_name AS FileName,
            file_stored_name AS FileStoredName,
            file_type AS FileType,
            file_hash AS FileHash,
            display_name AS DisplayName,
            element_block_name AS ElementBlockName,
            layer_name AS LayerName,
            color_index AS ColorIndex,
            file_path AS FilePath,
            preview_image_name AS PreviewImageName,
            preview_image_path AS PreviewImagePath,
            file_size AS FileSize,
            is_preview AS IsPreview,
            version AS Version,
            description AS Description,
            is_active AS IsActive,
            created_by AS CreatedBy,
            category_type AS CategoryType,
            title AS Title,
            keywords AS Keywords,
            is_public AS IsPublic,
            updated_by AS UpdatedBy,
            last_accessed_at AS LastAccessedAt,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM cad_file_storage 
        WHERE category_id = @categoryId 
          AND file_type = @fileType
          AND is_active = 1
        ORDER BY created_at DESC";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return (await connection.QueryAsync<FileStorage>(sql, new { categoryId, fileType })).AsList();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"根据文件扩展名获取分类下的文件时出错: {ex.Message}");
                return new List<FileStorage>();
            }
        }

        /// <summary>
        /// 搜索文件（支持关键词搜索）
        /// </summary>
        public async Task<List<FileStorage>> SearchFilesAsync(string keyword, int? categoryId = null)
        {
            string sql = @"
        SELECT 
            id AS Id,
            category_id AS CategoryId,
            file_attribute_id AS FileAttributeId,
            file_name AS FileName,
            file_stored_name AS FileStoredName,
            file_type AS FileType,
            file_hash AS FileHash,
            display_name AS DisplayName,
            element_block_name AS ElementBlockName,
            layer_name AS LayerName,
            color_index AS ColorIndex,
            file_path AS FilePath,
            preview_image_name AS PreviewImageName,
            preview_image_path AS PreviewImagePath,
            file_size AS FileSize,
            is_preview AS IsPreview,
            version AS Version,
            description AS Description,
            is_active AS IsActive,
            created_by AS CreatedBy,
            category_type AS CategoryType,
            title AS Title,
            keywords AS Keywords,
            is_public AS IsPublic,
            updated_by AS UpdatedBy,
            last_accessed_at AS LastAccessedAt,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM cad_file_storage 
        WHERE is_active = 1";
            try
            {
                var parameters = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(keyword))
                {
                    sql += @" AND (title LIKE @keyword 
         OR file_name LIKE @keyword 
         OR display_name LIKE @keyword 
         OR description LIKE @keyword 
         OR keywords LIKE @keyword)";
                    parameters.Add("keyword", $"%{keyword}%");
                }

                if (categoryId.HasValue)
                {
                    sql += " AND category_id = @categoryId";
                    parameters.Add("categoryId", categoryId.Value);
                }

                sql += " ORDER BY created_at DESC LIMIT 100";

                using var connection = new MySqlConnection(_connectionString);
                return (await connection.QueryAsync<FileStorage>(sql, parameters)).AsList();
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"搜索文件时出错: {ex.Message}");
                return new List<FileStorage>();
            }

        }

        /// <summary>
        /// 获取文件的详细信息（包括属性）
        /// </summary>
        public async Task<(FileStorage File, FileAttribute Attribute)> GetFileWithAttributeAsync(int fileId)
        {
            const string fileSql = @"
                 SELECT 
                     id AS Id,
                     category_id AS CategoryId,
                     file_attribute_id AS FileAttributeId,
                     file_name AS FileName,
                     file_stored_name AS FileStoredName,
                     display_name AS DisplayName,
                     file_type AS FileType,
                     file_hash AS FileHash,
                     element_block_name AS ElementBlockName,
                     layer_name AS LayerName,
                     color_index AS ColorIndex,
                     file_path AS FilePath,
                     preview_image_name AS PreviewImageName,
                     preview_image_path AS PreviewImagePath,
                     file_size AS FileSize,
                     is_preview AS IsPreview,
                     version AS Version,
                     description AS Description,
                     is_active AS IsActive,
                     created_by AS CreatedBy,
                     category_type AS CategoryType,
                     title AS Title,
                     keywords AS Keywords,
                     is_public AS IsPublic,
                     updated_by AS UpdatedBy,
                     last_accessed_at AS LastAccessedAt,
                     created_at AS CreatedAt,
                     updated_at AS UpdatedAt
                 FROM cad_file_storage
                 WHERE id = @Id";
              
                     const string attributeSql = @"
                 SELECT
                     id AS Id,
                     category_id AS CategoryId,
                     file_storage_id AS FileStorageId,
                     file_name AS FileName,
                     `length` AS Length,
                     `width` AS Width,
                     `height` AS Height,
                     `angle` AS Angle,
                     base_point_x AS BasePointX,
                     base_point_y AS BasePointY,
                     base_point_z AS BasePointZ,
                     description AS Description,
                     medium_name AS MediumName,
                     specifications AS Specifications,
                     material AS Material,
                     standard_number AS StandardNumber,
                     power AS Power,
                     volume AS Volume,
                     density AS Density,
                     voltage AS Voltage,
                     current AS Current,
                     flow AS Flow,
                     moisture AS Moisture,
                     humidity AS Humidity,
                     `vacuum` AS Vacuum,
                     radiation AS Radiation,
                     velocity AS Velocity,
                     frequency AS Frequency,
                     temperature AS Temperature,
                     conductivity AS Conductivity,
                     pressure AS Pressure,
                     diameter AS Diameter,
                     outer_diameter AS OuterDiameter,
                     inner_diameter AS InnerDiameter,
                     thickness AS Thickness,
                     `weight` AS Weight,
                     `model` AS Model,
                     lift AS Lift,
                     remarks AS Remarks,
                     customize1 AS Customize1,
                     customize2 AS Customize2,
                     customize3 AS Customize3,
                     created_at AS CreatedAt,
                     updated_at AS UpdatedAt
                 FROM cad_file_attributes
                 WHERE file_storage_id = @FileStorageId";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var file = await connection.QuerySingleOrDefaultAsync<FileStorage>(fileSql, new { Id = fileId });
                var attribute = await connection.QuerySingleOrDefaultAsync<FileAttribute>(attributeSql, new { FileStorageId = fileId });
                return (file, attribute);
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"获取文件详细信息时出错: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// 获取文件的详细信息（包括属性）
        /// </summary>
        public async Task<FileStorage> GetFileStorageAsync(string filehash)
        {
            if (string.IsNullOrWhiteSpace(filehash))
                return null;

            const string fileSql = @"
             SELECT 
                 id AS Id,
                 category_id AS CategoryId,
                 file_attribute_id AS FileAttributeId,
                 file_name AS FileName,
                 file_stored_name AS FileStoredName,
                 display_name AS DisplayName,
                 file_type AS FileType,
                 file_hash AS FileHash,
                 element_block_name AS ElementBlockName,
                 layer_name AS LayerName,
                 color_index AS ColorIndex,
                 file_path AS FilePath,
                 preview_image_name AS PreviewImageName,
                 preview_image_path AS PreviewImagePath,
                 file_size AS FileSize,
                 is_preview AS IsPreview,
                 version AS Version,
                 description AS Description,
                 is_active AS IsActive,
                 created_by AS CreatedBy,
                 category_type AS CategoryType,
                 title AS Title,
                 keywords AS Keywords,
                 is_public AS IsPublic,
                 updated_by AS UpdatedBy,
                 last_accessed_at AS LastAccessedAt,
                 created_at AS CreatedAt,
                 updated_at AS UpdatedAt
             FROM cad_file_storage 
             WHERE file_hash = @FileHash
             LIMIT 1";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var fileStorageInfo = await connection.QueryFirstOrDefaultAsync<FileStorage>(
                    fileSql, new { FileHash = filehash });
                return fileStorageInfo;
            }
            catch (Exception ex)
            {

                LogManager.Instance.LogInfo($"获取文件详细信息时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将 FileStorage 保存到数据库，成功返回新记录的 Id（>0），失败返回 0。
        /// </summary>
        public async Task<int> AddFileStorageAsync(FileStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            var now = DateTime.Now;
            storage.CreatedAt = storage.CreatedAt == default ? now : storage.CreatedAt;
            storage.UpdatedAt = storage.UpdatedAt == default ? now : storage.UpdatedAt;

            const string sql = @"
                INSERT INTO cad_file_storage
                (`category_id`, `file_name`, `file_attribute_id`, `file_stored_name`, `display_name`, `file_type`,
                 `is_tianzhen`, `scale`, `file_hash`, `element_block_name`, `layer_name`, `color_index`,
                 `file_path`, `preview_image_name`, `preview_image_path`, `file_size`, `is_preview`, `version`,
                 `description`, `created_at`, `updated_at`, `is_active`, `created_by`, `category_type`,
                 `title`, `keywords`, `is_public`, `updated_by`, `last_accessed_at`)
                VALUES
                (@CategoryId, @FileName, @FileAttributeId, @FileStoredName, @DisplayName, @FileType,
                 @IsTianZheng, @scale, @FileHash, @ElementBlockName, @LayerName, @ColorIndex,
                 @FilePath, @PreviewImageName, @PreviewImagePath, @FileSize, @IsPreview, @Version,
                 @Description, @CreatedAt, @UpdatedAt, @IsActive, @CreatedBy, @CategoryType,
                 @Title, @Keywords, @IsPublic, @UpdatedBy, @LastAccessedAt);
                SELECT LAST_INSERT_ID();";

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    // Dapper 会根据属性名映射参数
                    var idObj = await conn.ExecuteScalarAsync<long>(sql, storage).ConfigureAwait(false);
                    return Convert.ToInt32(idObj);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"AddFileStorageAsync 失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 将文件属性插入到数据库，成功返回新记录的 Id（>0），失败返回 0。
        /// </summary>
        public async Task<int> AddFileAttributeAsync(FileAttribute attribute)
        {
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));

            // 保证时间字段
            var now = DateTime.Now;
            attribute.CreatedAt = attribute.CreatedAt == default ? now : attribute.CreatedAt;
            attribute.UpdatedAt = attribute.UpdatedAt == default ? now : attribute.UpdatedAt;

            const string sql = @"
               INSERT INTO cad_file_attributes
               (file_storage_id, file_name, `length`, `width`, `height`, `angle`,
                base_point_x, base_point_y, base_point_z,
                created_at, updated_at,
                description, medium_name, specifications, material, standard_number,
                power, volume, density, voltage, current, flow, moisture, humidity, `vacuum`,
                radiation, velocity, frequency, temperature, conductivity, pressure,
                diameter, outer_diameter, inner_diameter, thickness, `weight`, `model`, lift,
                remarks, customize1, customize2, customize3)
               VALUES
               (@FileStorageId, @FileName, @Length, @Width, @Height, @Angle,
                @BasePointX, @BasePointY, @BasePointZ,
                @CreatedAt, @UpdatedAt,
                @Description, @MediumName, @Specifications, @Material, @StandardNumber,
                @Power, @Volume, @Density, @Voltage, @Current, @Flow, @Moisture, @Humidity, @Vacuum,
                @Radiation, @Velocity, @Frequency, @Temperature, @Conductivity, @Pressure,
                @Diameter, @OuterDiameter, @InnerDiameter, @Thickness, @Weight, @Model, @Lift,
                @Remarks, @Customize1, @Customize2, @Customize3);
               SELECT LAST_INSERT_ID();";

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    var idObj = await conn.ExecuteScalarAsync<long>(sql, attribute).ConfigureAwait(false);
                    return Convert.ToInt32(idObj);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"AddFileAttributeAsync 失败: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 更新已存在的文件属性记录，成功返回 true（影响行数>0），否则 false。
        /// </summary>
        public async Task<bool> UpdateFileAttributeAsync(FileAttribute attribute)
        {
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));
            if (attribute.Id == 0) throw new ArgumentException("属性对象 Id 无效", nameof(attribute));

            attribute.UpdatedAt = DateTime.Now;

            const string sql = @"
                 UPDATE cad_file_attributes SET
                     file_storage_id = @FileStorageId,
                     file_name = @FileName,
                     `length` = @Length,
                     `width` = @Width,
                     `height` = @Height,
                     `angle` = @Angle,
                     base_point_x = @BasePointX,
                     base_point_y = @BasePointY,
                     base_point_z = @BasePointZ,
                     updated_at = @UpdatedAt,
                     description = @Description,
                     medium_name = @MediumName,
                     specifications = @Specifications,
                     material = @Material,
                     standard_number = @StandardNumber,
                     power = @Power,
                     volume = @Volume,
                     density = @Density,
                     voltage = @Voltage,
                     current = @Current,
                     flow = @Flow,
                     moisture = @Moisture,
                     humidity = @Humidity,
                     `vacuum` = @Vacuum,
                     radiation = @Radiation,
                     velocity = @Velocity,
                     frequency = @Frequency,
                     temperature = @Temperature,
                     conductivity = @Conductivity,
                     pressure = @Pressure,
                     diameter = @Diameter,
                     outer_diameter = @OuterDiameter,
                     inner_diameter = @InnerDiameter,
                     thickness = @Thickness,
                     `weight` = @Weight,
                     `model` = @Model,
                     lift = @Lift,
                     remarks = @Remarks,
                     customize1 = @Customize1,
                     customize2 = @Customize2,
                     customize3 = @Customize3
                 WHERE id = @Id;";

            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync().ConfigureAwait(false);
                    var affected = await conn.ExecuteAsync(sql, attribute).ConfigureAwait(false);
                    return affected > 0;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"UpdateFileAttributeAsync 失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 确保 cad_file_attributes 表存在并包含常用属性列，缺失则尝试添加（返回 true 表示成功或已同步）。
        /// 使用场景：UI 按钮调用以确保表结构包含新增的物性/测量字段。
        /// </summary>
        public async Task<bool> EnsureFileAttributesTableSchemaAsync()
        {
            try
            {
                // 检查表结构 打开数据库连接
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync().ConfigureAwait(false);

                    // 确认表是否存在
                    const string tableExistsSql = @"
                        SELECT COUNT(1) FROM information_schema.tables
                        WHERE table_schema = @db AND table_name = 'cad_file_attributes';";
                    var tableCount = await conn.ExecuteScalarAsync<int>(tableExistsSql, new { db = conn.Database }).ConfigureAwait(false);
                    if (tableCount == 0)
                    {
                        // 尝试创建一个最小表结构（后续会补齐列）
                        const string createSql = @"
                            CREATE TABLE `cad_file_attributes` (
                                `id` INT AUTO_INCREMENT PRIMARY KEY,
                                `file_storage_id` INT NULL,
                                `file_name` VARCHAR(255) NULL,
                                `created_at` DATETIME NULL,
                                `updated_at` DATETIME NULL
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                        await conn.ExecuteAsync(createSql).ConfigureAwait(false);
                        LogManager.Instance.LogInfo("EnsureFileAttributesTableSchemaAsync: 已创建表 cad_file_attributes（最小结构）。");
                    }

                    // 查询现有列
                    const string colsSql = @"
                        SELECT COLUMN_NAME FROM information_schema.columns
                        WHERE table_schema = @db AND table_name = 'cad_file_attributes';";
                    var existingCols = (await conn.QueryAsync<string>(colsSql, new { db = conn.Database }).ConfigureAwait(false))
                                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // 需要保证存在的列及类型（若已存在则跳过）
                    var requiredAdds = new List<string>();

                    void AddIfMissing(string colDef, string colName)
                    {
                        if (!existingCols.Contains(colName))
                            requiredAdds.Add(colDef);
                    }

                    AddIfMissing("ADD COLUMN `file_attribute_id` INT NULL", "file_attribute_id");
                    AddIfMissing("ADD COLUMN `length` DECIMAL(18,6) NULL", "length");
                    AddIfMissing("ADD COLUMN `width` DECIMAL(18,6) NULL", "width");
                    AddIfMissing("ADD COLUMN `height` DECIMAL(18,6) NULL", "height");
                    AddIfMissing("ADD COLUMN `angle` DECIMAL(18,6) NULL", "angle");
                    AddIfMissing("ADD COLUMN `base_point_x` DECIMAL(18,6) NULL", "base_point_x");
                    AddIfMissing("ADD COLUMN `base_point_y` DECIMAL(18,6) NULL", "base_point_y");
                    AddIfMissing("ADD COLUMN `base_point_z` DECIMAL(18,6) NULL", "base_point_z");
                    AddIfMissing("ADD COLUMN `description` TEXT NULL", "description");
                    AddIfMissing("ADD COLUMN `medium_name` VARCHAR(200) NULL", "medium_name");
                    AddIfMissing("ADD COLUMN `specifications` VARCHAR(200) NULL", "specifications");
                    AddIfMissing("ADD COLUMN `material` VARCHAR(200) NULL", "material");
                    AddIfMissing("ADD COLUMN `standard_number` VARCHAR(200) NULL", "standard_number");
                    AddIfMissing("ADD COLUMN `power` VARCHAR(100) NULL", "power");
                    AddIfMissing("ADD COLUMN `volume` VARCHAR(100) NULL", "volume");
                    AddIfMissing("ADD COLUMN `density` DECIMAL(18,6) NULL", "density");
                    AddIfMissing("ADD COLUMN `voltage` VARCHAR(100) NULL", "voltage");
                    AddIfMissing("ADD COLUMN `current` VARCHAR(100) NULL", "current");
                    AddIfMissing("ADD COLUMN `flow` VARCHAR(100) NULL", "flow");
                    AddIfMissing("ADD COLUMN `moisture` VARCHAR(100) NULL", "moisture");
                    AddIfMissing("ADD COLUMN `humidity` VARCHAR(100) NULL", "humidity");
                    AddIfMissing("ADD COLUMN `vacuum` VARCHAR(100) NULL", "vacuum");
                    AddIfMissing("ADD COLUMN `radiation` VARCHAR(100) NULL", "radiation");
                    AddIfMissing("ADD COLUMN `velocity` VARCHAR(100) NULL", "velocity");
                    AddIfMissing("ADD COLUMN `frequency` VARCHAR(100) NULL", "frequency");
                    AddIfMissing("ADD COLUMN `temperature` VARCHAR(100) NULL", "temperature");
                    AddIfMissing("ADD COLUMN `conductivity` VARCHAR(100) NULL", "conductivity");
                    AddIfMissing("ADD COLUMN `pressure` VARCHAR(100) NULL", "pressure");
                    AddIfMissing("ADD COLUMN `diameter` VARCHAR(100) NULL", "diameter");
                    AddIfMissing("ADD COLUMN `outer_diameter` VARCHAR(100) NULL", "outer_diameter");
                    AddIfMissing("ADD COLUMN `inner_diameter` VARCHAR(100) NULL", "inner_diameter");
                    AddIfMissing("ADD COLUMN `thickness` VARCHAR(100) NULL", "thickness");
                    AddIfMissing("ADD COLUMN `weight` VARCHAR(100) NULL", "weight");
                    AddIfMissing("ADD COLUMN `model` VARCHAR(200) NULL", "model");
                    AddIfMissing("ADD COLUMN `lift` VARCHAR(100) NULL", "lift");
                    AddIfMissing("ADD COLUMN `remarks` TEXT NULL", "remarks");
                    AddIfMissing("ADD COLUMN `customize1` VARCHAR(200) NULL", "customize1");
                    AddIfMissing("ADD COLUMN `customize2` VARCHAR(200) NULL", "customize2");
                    AddIfMissing("ADD COLUMN `customize3` VARCHAR(200) NULL", "customize3");

                    if (requiredAdds.Count > 0)
                    {
                        // 合并为单次 ALTER TABLE 提交（MySQL 支持多个 ADD 子句）
                        var alterSql = "ALTER TABLE `cad_file_attributes` " + string.Join(", ", requiredAdds) + ";";
                        await conn.ExecuteAsync(alterSql).ConfigureAwait(false);
                        LogManager.Instance.LogInfo($"EnsureFileAttributesTableSchemaAsync: 已添加 {requiredAdds.Count} 列到 cad_file_attributes。");
                    }
                    else
                    {
                        LogManager.Instance.LogInfo("EnsureFileAttributesTableSchemaAsync: cad_file_attributes 已包含所有必要列。");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogError($"EnsureFileAttributesTableSchemaAsync 异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取文件属性的详细信息（按文件名模糊匹配）GetFileAttributeAsync
        /// </summary>
        public async Task<FileAttribute> GetFileAttributeAsync(string fileName)
        {
            string noExtName = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
            string rawName = Path.GetFileName(fileName) ?? string.Empty;

            const string attributeSql = @"
                SELECT
                    id AS Id,
                    category_id AS CategoryId,
                    file_storage_id AS FileStorageId,
                    file_name AS FileName,
                    `length` AS Length,
                    `width` AS Width,
                    `height` AS Height,
                    `angle` AS Angle,
                    base_point_x AS BasePointX,
                    base_point_y AS BasePointY,
                    base_point_z AS BasePointZ,
                    description AS Description,
                    medium_name AS MediumName,
                    specifications AS Specifications,
                    material AS Material,
                    standard_number AS StandardNumber,
                    power AS Power,
                    volume AS Volume,
                    density AS Density,
                    voltage AS Voltage,
                    current AS Current,
                    flow AS Flow,
                    moisture AS Moisture,
                    humidity AS Humidity,
                    `vacuum` AS Vacuum,
                    radiation AS Radiation,
                    velocity AS Velocity,
                    frequency AS Frequency,
                    temperature AS Temperature,
                    conductivity AS Conductivity,
                    pressure AS Pressure,
                    diameter AS Diameter,
                    outer_diameter AS OuterDiameter,
                    inner_diameter AS InnerDiameter,
                    thickness AS Thickness,
                    `weight` AS Weight,
                    `model` AS Model,
                    lift AS Lift,
                    remarks AS Remarks,
                    customize1 AS Customize1,
                    customize2 AS Customize2,
                    customize3 AS Customize3,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM cad_file_attributes
                WHERE file_name LIKE CONCAT('%', @Name1, '%')
                   OR file_name LIKE CONCAT('%', @Name2, '%')
                LIMIT 1";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var parameters = new { Name1 = noExtName, Name2 = rawName };
                var attribute = await connection.QueryFirstOrDefaultAsync<FileAttribute>(attributeSql, parameters);
                return attribute;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取文件详细信息时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 更新分类统计信息
        /// </summary>
        public async Task<bool> UpdateCategoryStatisticsAsync(int categoryId, string categoryType)
        {
            const string sql = @"
             INSERT INTO category_statistics 
                 (category_id, category_type, file_count, total_size, last_file_added, updated_at)
             SELECT 
                 @CategoryId,
                 @CategoryType,
                 COUNT(*),
                 COALESCE(SUM(file_size), 0),
                 MAX(created_at),
                 CURRENT_TIMESTAMP
             FROM cad_file_storage 
             WHERE category_id = @CategoryId 
               AND category_type = @CategoryType 
               AND is_active = 1
             ON DUPLICATE KEY UPDATE
                 file_count = VALUES(file_count),
                 total_size = VALUES(total_size),
                 last_file_added = VALUES(last_file_added),
                 updated_at = CURRENT_TIMESTAMP";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var affected = await connection.ExecuteAsync(sql, new { CategoryId = categoryId, CategoryType = categoryType });
                // MySQL 的 INSERT ... ON DUPLICATE KEY UPDATE 返回受影响的行数：
                // 插入返回1，更新返回2（或更多）。这里只需判断执行是否成功。
                return affected > 0;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新分类统计信息时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        public async Task<CategoryStatistics> GetCategoryStatisticsAsync(int categoryId, string categoryType)
        {
            const string sql = @"
        SELECT 
            id AS Id,
            category_id AS CategoryId,
            category_type AS CategoryType,
            file_count AS FileCount,
            total_size AS TotalSize,
            last_file_added AS LastFileAdded,
            updated_at AS UpdatedAt
        FROM category_statistics 
        WHERE category_id = @categoryId AND category_type = @categoryType";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return await connection.QuerySingleOrDefaultAsync<CategoryStatistics>(sql, new { categoryId, categoryType });
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取分类统计信息时出错: {ex.Message}");
                return null;
            }

        }

        #endregion

        #region 文件操作方法
        #region CAD图元操作
        /// <summary>
        /// 删除CAD图元
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="deletedBy"></param>
        /// <returns></returns>
        public async Task<int> DeleteFileAsync(int fileId, string deletedBy)
        {
            const string sql = @"
                UPDATE cad_file_storage 
                SET is_active = 0, 
                    updated_at = @UpdatedAt
                WHERE id = @Id";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, new { Id = fileId, UpdatedAt = DateTime.Now });
        }

        /// <summary>
        /// 更新文件信息新CAD图元信息
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<int> UpdateFileAsync(FileStorage file)
        {
            const string sql = @"
                 UPDATE cad_file_storage 
                 SET file_name = @FileName, 
                     description = @Description,
                     updated_at = @UpdatedAt
                 WHERE id = @Id";

            using var connection = new MySqlConnection(_connectionString);
            file.UpdatedAt = file.UpdatedAt == default ? DateTime.Now : file.UpdatedAt;
            return await connection.ExecuteAsync(sql, new { file.FileName, file.Description, UpdatedAt = file.UpdatedAt, Id = file.Id });
        }

        // 新增：删除文件属性记录（按 id）
        // 将此方法粘贴到 DatabaseManager 类中合适位置（与其它文件操作方法并列）
        public async Task<int> DeleteFileAttributeAsync(int id)
        {
            if (id <= 0) return 0;
            try
            {
                using var conn = GetConnection();
                const string sql = "DELETE FROM cad_file_attributes WHERE id = @Id";
                return await conn.ExecuteAsync(sql, new { Id = id }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"DeleteFileAttributeAsync 出错: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 新增：删除文件存储记录（按 id，硬删除）
        /// </summary>
        public async Task<int> DeleteFileStorageAsync(int id)
        {
            if (id <= 0) return 0;
            try
            {
                using var conn = GetConnection();
                const string sql = "DELETE FROM cad_file_storage WHERE id = @Id";
                return await conn.ExecuteAsync(sql, new { Id = id }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"DeleteFileStorageAsync 出错: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 更新文件存储记录（整体更新）
        /// </summary>
        public async Task<int> UpdateFileStorageAsync(FileStorage file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            try
            {
                using var conn = GetConnection();
                const string sql = @"
            UPDATE cad_file_storage SET
                category_id = @CategoryId,
                category_type = @CategoryType,
                file_attribute_id = @FileAttributeId,
                file_name = @FileName,
                file_stored_name = @FileStoredName,
                display_name = @DisplayName,
                file_type = @FileType,
                file_hash = @FileHash,
                element_block_name = @ElementBlockName,
                layer_name = @LayerName,
                color_index = @ColorIndex,
                file_path = @FilePath,
                preview_image_name = @PreviewImageName,
                preview_image_path = @PreviewImagePath,
                file_size = @FileSize,
                is_preview = @IsPreview,
                version = @Version,
                description = @Description,
                is_active = @IsActive,
                updated_by = @UpdatedBy,
                last_accessed_at = @LastAccessedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id";
                return await conn.ExecuteAsync(sql, file).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"UpdateFileStorageAsync 出错: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 添加文件标签（file_tags），返回 true 表示插入成功
        /// </summary>
        public async Task<bool> AddFileTagAsync(FileTag tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            try
            {
                using var conn = GetConnection();
                const string sql = @"INSERT INTO file_tags (file_id, tag_name, created_at) VALUES (@FileId, @TagName, @CreatedAt)";
                var affected = await conn.ExecuteAsync(sql, new { tag.FileId, tag.TagName, tag.CreatedAt }).ConfigureAwait(false);
                return affected > 0;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"AddFileTagAsync 出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据分类Id获取CAD图元列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<FileStorage> GetCadGraphicByIdAsync(int categoryId)
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM cad_file_storage WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<FileStorage>(sql, new { Id = categoryId });
        }     

        /// <summary>
        /// 根据子分类ID获取图元
        /// </summary>
        public async Task<List<FileStorage>> GetFileStorageBySubcategoryIdAsync(int subcategoryId)
        {
            const string sql = @"
             SELECT 
                 id AS Id,
                 category_id AS CategoryId,
                 file_name AS FileName,
                 display_name AS DisplayName,
                 element_block_name AS ElementBlockName,
                 layer_name AS LayerName,
                 color_index AS ColorIndex,
                 file_path AS FilePath,
                 preview_image_name AS PreviewImageName,
                 preview_image_path AS PreviewImagePath,
                 file_size AS FileSize,
                 created_at AS CreatedAt,
                 updated_at AS UpdatedAt
             FROM cad_file_storage 
             WHERE category_id = @SubcategoryId AND category_type = 'sub'
             ORDER BY file_name";
            try
            {
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString);
                var result = await connection.QueryAsync<FileStorage>(sql, new { SubcategoryId = subcategoryId }).ConfigureAwait(false);
                return result.AsList();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取子分类图元列表时出错: {ex.Message}");
                return new List<FileStorage>();
            }
        }

        /// <summary>
        /// 根据ID获取图元属性
        /// </summary>
        public async Task<FileAttribute> GetFileAttributeByGraphicIdAsync(int fileStorageId)
        {
            const string sql = @"
               SELECT
                   id AS Id,
                   category_id AS CategoryId,
                   file_storage_id AS FileStorageId,
                   file_name AS FileName,
                   length AS DECIMAL(18,4),
                   width AS DECIMAL(18,4),
                   height AS DECIMAL(18,4),
                   angle AS DECIMAL(10,4),
                   base_point_x AS DECIMAL(18,4),
                   base_point_y AS DECIMAL(18,4),
                   base_point_z AS DECIMAL(18,4),
                   description AS TEXT,
                   medium_name AS VARCHAR(200),
                   specifications AS VARCHAR(200),
                   material AS VARCHAR(200),
                   standard_number AS VARCHAR(200),
                   power AS VARCHAR(100),
                   volume AS VARCHAR(100),
                   density AS VARCHAR(100),
                   voltage AS VARCHAR(100),
                   current AS VARCHAR(100),
                   flow AS VARCHAR(100),
                   moisture AS VARCHAR(100),
                   humidity AS VARCHAR(100),
                   vacuum AS VARCHAR(100),
                   radiation AS VARCHAR(100),
                   velocity AS VARCHAR(100),
                   frequency AS VARCHAR(100),
                   temperature AS VARCHAR(100),
                   conductivity AS VARCHAR(100),
                   pressure AS VARCHAR(100),
                   diameter AS VARCHAR(100),
                   outer_diameter AS VARCHAR(100),
                   inner_diameter AS VARCHAR(100),
                   thickness AS VARCHAR(100),
                   weight AS VARCHAR(100),
                   model AS VARCHAR(200),
                   lift AS VARCHAR(100),
                   remarks AS TEXT,
                   customize1 AS VARCHAR(200),
                   customize2 AS VARCHAR(200),
                   customize3 AS VARCHAR(200),
                   created_at AS DATETIME DEFAULT CURRENT_TIMESTAMP,
                   updated_at AS DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
               FROM cad_file_attributes
               WHERE file_storage_id = @FileStorageId
               LIMIT 1;";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var result = await connection.QuerySingleOrDefaultAsync<FileAttribute>(sql, new { FileStorageId = fileStorageId }).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"根据ID获取图元属性时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 修改CAD图元
        /// </summary>
        /// <param name="fileStorage"></param>
        /// <returns></returns>
        public async Task<int> UpdateCadGraphicAsync(FileStorage fileStorage)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE cad_file_storage 
          SET 
             id = @Id,//图元ID
             category_id = @CategoryId,
             file_name = @FileName,
             display_name = @DisplayName,
             element_block_name = @ElementBlockName,
             layer_name = @LayerName,
             color_index = @ColorIndex,
             file_path = @FilePath,
             preview_image_name = @PreviewImageName,
             preview_image_path = @PreviewImagePath,
             file_size = @FileSize,
             is_tianzheng = @IsTianZheng,
             updated_at = NOW()
             graphice_name = @GraphiceName,//图元名称
             sort_order = @SortOrder,//排序
             length = @Length,//长度
             width = @Width,//宽度
             height = @Height,//高度
             angle = @Angle,//角度
             base_point_x = @BasePointX,//基点X
             base_point_y = @BasePointY,//基点Y
             base_point_z = @BasePointZ,//基点Z
             created_at = @CreatedAt,//创建时间
             updated_at = @UpdatedAt,//更新时间
             description = @Description,//描述
             medium_name = @MediumName,//中文名称
             specifications = @Specifications,//规格
             material = @Material,//材质
             standard_number = @StandardNumber,//标准编号
             power = @Power,//功率
             volume = @Volume,//容积
             pressure = @Pressure,//压力
             temperature = @Temperature,//温度
             diameter = @Diameter,//直径
             outer_diameter = @OuterDiameter,//外径
             inner_diameter = @InnerDiameter,//内径
             thickness = @Thickness,//厚度
             weight = @Weight,//重量
             model = @Model,//材质
             remarks = @Remarks,//备注
             customize1 = @Customize1,//自定义字段1
             customize2 = @Customize2,//自定义字段2
             customize3 = @Customize3,//自定义字段3
          WHERE id = @Id";
            return await connection.ExecuteAsync(sql, fileStorage);
        }

        /// <summary>
        /// 删除CAD图元  DeleteCadGraphicCascadeAsync AddFileAttributeAsync
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteCadGraphicAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM cad_file_storage WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id });
        }

        /// <summary>
        /// 级联删除 CAD 图元（数据库所有关联 + 物理文件）
        /// </summary>
        /// <param name="fileId">文件主键ID</param>
        /// <param name="physicalDelete">是否物理删除磁盘文件</param>
        /// <returns>删除是否成功</returns>
        public async Task<bool> DeleteCadGraphicCascadeAsync(int fileId, bool physicalDelete = true)
        {
            const string selectSql = @"
             SELECT 
                 id AS Id,
                 category_id AS CategoryId,
                 file_path AS FilePath,
                 preview_image_path AS PreviewImagePath,
                 category_type AS CategoryType
             FROM cad_file_storage
             WHERE id = @Id";

            const string deleteTagsSql = @"DELETE FROM file_tags WHERE file_id = @FileId";//标签删除标签关联
            const string deleteAccessLogsSql = @"DELETE FROM file_access_logs WHERE file_id = @FileId";//访问日志访问日志删除
            const string deleteVersionHistorySql = @"DELETE FROM file_version_history WHERE file_id = @FileId";//版本历史删除
            const string deleteAttributesByFileIdSql = @"DELETE FROM cad_file_attributes WHERE file_storage_id = @FileId";//删除属性关联图元
            const string deleteMainSql = @"DELETE FROM cad_file_storage WHERE id = @FileId";

            using var connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString);//创建数据库连接使用 MySqlConnection
            await connection.OpenAsync().ConfigureAwait(false);//打开数据库连接
            using var tx = await connection.BeginTransactionAsync().ConfigureAwait(false);//开启事务

            try
            {
                // 查询文件信息
                var fileRow = await connection.QuerySingleOrDefaultAsync<dynamic>(selectSql, new { Id = fileId }, tx).ConfigureAwait(false);
                if (fileRow == null)
                {
                    await tx.RollbackAsync().ConfigureAwait(false);//回滚事务
                    LogManager.Instance.LogInfo($"级联删除失败：未找到文件 Id={fileId}");//记录日志:文件不存在
                    return false;
                }

                string filePath = (string)fileRow.FilePath;//获取文件路径
                string previewPath = (string)fileRow.PreviewImagePath;//获取预览图片路径
                int categoryId = (int)fileRow.CategoryId;//获取分类ID
                string categoryType = (string)fileRow.CategoryType;//获取分类类型

                // 删除附属数据
                await connection.ExecuteAsync(deleteTagsSql, new { FileId = fileId }, tx).ConfigureAwait(false);//删除标签关联
                await connection.ExecuteAsync(deleteAccessLogsSql, new { FileId = fileId }, tx).ConfigureAwait(false);//删除访问日志
                await connection.ExecuteAsync(deleteVersionHistorySql, new { FileId = fileId }, tx).ConfigureAwait(false);//删除版本历史
                await connection.ExecuteAsync(deleteAttributesByFileIdSql, new { FileId = fileId }, tx).ConfigureAwait(false);//删除属性关联图元
                await connection.ExecuteAsync(deleteMainSql, new { FileId = fileId }, tx).ConfigureAwait(false);//删除主文件

                // 提交事务
                await tx.CommitAsync().ConfigureAwait(false);//提交事务

                // 物理删除（在事务成功后）
                if (physicalDelete)
                {
                    TryDeleteFileSafe(filePath);//删除主文件
                    TryDeleteFileSafe(previewPath);//删除预览图片
                }

                // 更新分类统计（后台执行，忽略异常）
                _ = UpdateCategoryStatisticsAsync(categoryId, categoryType);//更新分类统计

                LogManager.Instance.LogInfo($"级联删除完成 Id={fileId}");//记录日志:级联删除完成
                return true;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync().ConfigureAwait(false);//回滚事务
                LogManager.Instance.LogInfo($"级联删除失败 Id={fileId}，错误：{ex.Message}");//记录日志:级联删除失败
                return false;
            }
        }

        /// <summary>
        /// 安全删除单个文件（忽略异常并记录日志）
        /// </summary>
        private void TryDeleteFileSafe(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))//检查文件是否存在
                {
                    File.Delete(path);//删除文件
                    LogManager.Instance.LogInfo($"物理文件已删除: {path}");//记录日志:物理文件已删除
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"物理文件删除失败: {path} - {ex.Message}");//记录日志:物理文件删除失败
            }
        }

        /// <summary>
        /// 根据文件ID获取文件信息
        /// </summary>
        public async Task<FileStorage> GetFileByIdAsync(int fileId)
        {
            const string sql = @"
             SELECT 
                 id AS Id,
                 category_id AS CategoryId,
                 file_name AS FileName,
                 file_stored_name AS FileStoredName,
                 display_name AS DisplayName,
                 file_type AS FileType,
                 file_hash AS FileHash,
                 element_block_name AS ElementBlockName,
                 layer_name AS LayerName,
                 color_index AS ColorIndex,
                 file_path AS FilePath,
                 preview_image_name AS PreviewImageName,
                 preview_image_path AS PreviewImagePath,
                 file_size AS FileSize,
                 is_preview AS IsPreview,
                 version AS Version,
                 description AS Description,
                 is_active AS IsActive,
                 created_by AS CreatedBy,
                 category_type AS CategoryType,
                 title AS Title,
                 keywords AS Keywords,
                 is_public AS IsPublic,
                 updated_by AS UpdatedBy,
                 last_accessed_at AS LastAccessedAt,
                 created_at AS CreatedAt,
                 updated_at AS UpdatedAt
             FROM cad_file_storage 
             WHERE id = @fileId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<FileStorage>(sql, new { fileId });
        }

        /// <summary>
        /// 根据文件哈希值检查文件是否已存在
        /// </summary>
        public async Task<FileStorage> GetFileByHashAsync(string fileHash)
        {
            const string sql = @"
             SELECT 
                 id AS Id,
                 category_id AS CategoryId,
                 file_attribute_id AS FileAttributeId,
                 file_name AS FileName,
                 file_stored_name AS FileStoredName,
                 display_name AS DisplayName,
                 file_type AS FileType,
                 file_size AS FileSize,
                 file_hash AS FileHash,
                 description AS Description,
                 version AS Version,
                 is_preview AS IsPreview,
                 created_at AS CreatedAt,
                 updated_at AS UpdatedAt,
                 created_by AS CreatedBy,
                 is_active AS IsActive
             FROM cad_file_storage 
             WHERE file_hash = @fileHash AND is_active = 1
             LIMIT 1";
            try
            {
                using var connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString);
                var result = await connection.QuerySingleOrDefaultAsync<FileStorage>(sql, new { fileHash }).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取哈希值检查文件出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 记录文件访问日志（file_access_logs），返回受影响行数
        /// </summary>
        public async Task<int> AddFileAccessLogAsync(FileAccessLog accessLog)
        {
            if (accessLog == null) throw new ArgumentNullException(nameof(accessLog));

            const string sql = @"
        INSERT INTO file_access_logs (file_id, user_name, action_type, access_time, ip_address)
        VALUES (@FileId, @UserName, @ActionType, @AccessTime, @IpAddress)";
            try
            {
                using var conn = GetConnection();
                return await conn.ExecuteAsync(sql, new
                {
                    accessLog.FileId,
                    accessLog.UserName,
                    accessLog.ActionType,
                    AccessTime = accessLog.AccessTime == default ? DateTime.Now : accessLog.AccessTime,
                    accessLog.IpAddress
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"AddFileAccessLogAsync 出错: {ex.Message}");
                return 0;
            }
        }

        #endregion

        /// <summary>
        /// 在WpfMainWindow.xaml.cs中添加网络连接测试方法
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
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

    #endregion

    #region 文件实体管理

    /// 文件版本历史实体
    /// </summary>
    public class FileVersionHistory
    {
        public int Id { get; set; }//id
        public int FileId { get; set; }//文件id
        public int Version { get; set; }//文件版本
        public string FileName { get; set; }//文件名
        public string StoredFileName { get; set; }//存储文件名
        public string FilePath { get; set; }//文件存储路径
        public long FileSize { get; set; }//文件大小
        public DateTime UpdatedAt { get; set; }//更新时间
        public string UpdatedBy { get; set; }//更新者
        public string ChangeDescription { get; set; }//文件修改描述
    }

    /// <summary>
    /// 文件标签实体
    /// </summary>
    public class FileTag
    {
        public int Id { get; set; }           // 标签ID
        public int FileId { get; set; }       // 文件ID
        public string TagName { get; set; }   // 标签名称
        public DateTime CreatedAt { get; set; } // 创建时间
    }

    /// <summary>
    /// 文件访问日志实体
    /// </summary>
    public class FileAccessLog
    {
        public int Id { get; set; }//id
        public int FileId { get; set; }//文件id
        public string UserName { get; set; }//用户名
        public string ActionType { get; set; }//操作类型Download, View, Upload等
        public DateTime AccessTime { get; set; }//访问时间
        public string IpAddress { get; set; }//IP地址
    }

    #endregion
    // 在Database命名空间中添加以下类
    #region 数据库CAD实体
    /// <summary>
    /// CAD主分类实体
    /// </summary>
    public class CadCategory
    {
        public int Id { get; set; }         // 主分类ID：1000, 2000, 3000...
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int SortOrder { get; set; }
        public string SubcategoryIds { get; set; } // 子分类ID列表，用逗号分隔
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// CAD子分类实体
    /// </summary>
    public class CadSubcategory
    {
        public int Id { get; set; }              // 子分类ID：根据层级编码
        public int ParentId { get; set; }         // 父分类ID
        public string Name { get; set; }            // 子分类名称
        public string DisplayName { get; set; }
        public int SortOrder { get; set; }
        public int Level { get; set; }           // 分类层级（1=二级分类，2=三级分类...）
        public string SubcategoryIds { get; set; } // 下级子分类ID列表，用逗号分隔
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// CAD图元实体
    /// </summary>
    public class FileStorage
    {

        public int Id { get; set; }//id
        public int CategoryId { get; set; }//所在子类id
        public string? FileName { get; set; }//文件名
        public int FileAttributeId { get; set; }//属性id
        public string FileStoredName { get; set; } // 存储文件名（唯一）
        public string DisplayName { get; set; }//显示名
        public string FileType { get; set; }     // 文件类型（.dwg, .png, .jpg等）
        public int IsTianZheng { get; set; }//判断是不是天正图元(0=不是天正图元,1=是天正图元)
        public double scale { get; set; } // 缩放比例
        public string FileHash { get; set; }     // 文件哈希值（用于去重）
        public string ElementBlockName { get; set; }//元素块名
        public string LayerName { get; set; }//层名
        public int? ColorIndex { get; set; }//颜色索引
        public string FilePath { get; set; }//文件路径
        public string PreviewImageName { get; set; }//预览图片名称
        public string PreviewImagePath { get; set; }//预览图片路径
        public long? FileSize { get; set; }//文件大小
        public int IsPreview { get; set; }      // 是否为预览文件
        public int Version { get; set; }         // 文件版本
        public string? Description { get; set; }  // 文件描述
        public DateTime CreatedAt { get; set; }//创建时间
        public DateTime UpdatedAt { get; set; }//更新时间
        public int IsActive { get; set; }       // 是否激活
        public string? CreatedBy { get; set; }    // 创建者
        public string CategoryType { get; set; } = "sub"; // main 或 sub
        public string? Title { get; set; }//标题
        public string? Keywords { get; set; }//关键字
        public int IsPublic { get; set; } = 1;//是否公开
        public string? UpdatedBy { get; set; }//更新者
        public DateTime? LastAccessedAt { get; set; }//最后访问时间
    }

    /// <summary>
    /// CAD图元属性实体（已扩展字段）
    /// </summary>
    public class FileAttribute
    {

        public int Id { get; set; }//属性Id id AS Id,
        public int? CategoryId { get; set; }//分类id category_id AS CategoryId,
        public int FileStorageId { get; set; }//文件id file_storage_id AS FileStorageId,
        public string? FileName { get; set; }//文件名称 file_name AS FileName,
        public decimal? Length { get; set; }//长度 length AS Length,
        public decimal? Width { get; set; }//宽度 width AS Width,
        public decimal? Height { get; set; }//高度  height AS Height,
        public decimal? Angle { get; set; }//角度 angle AS Angle,
        public decimal? BasePointX { get; set; }//基点X base_point_x AS BasePointX,
        public decimal? BasePointY { get; set; }//基点Y base_point_y AS BasePointY,
        public decimal? BasePointZ { get; set; }//基点Z base_point_z AS BasePointZ,
        public DateTime CreatedAt { get; set; }//创建时间 created_at AS CreatedAt,
        public DateTime UpdatedAt { get; set; }//更新时间 updated_at AS UpdatedAt,
        public string? Description { get; set; }//描述 description AS Description,
        public string? MediumName { get; set; }//介质 medium_name AS MediumName,
        public string? Specifications { get; set; }//规格 specifications AS Specifications,
        public string? Material { get; set; }//材质 material AS Material,
        public string? StandardNumber { get; set; }//标准编号 standard_number AS StandardNumber,
        public string? Power { get; set; }//功率 power AS Power, 
        public string? Volume { get; set; }//容积 volume AS Volume,
        public string? Density { get; set; }//密度 density AS Density,
        public string? Voltage { get; set; }//电压 voltage AS Voltage,
        public string? Current { get; set; }//电流 current AS Current,
        public string? Flow { get; set; }//流量 flow AS Flow,
        public string? Moisture { get; set; }//水份 moisture AS Moisture,
        public string? Humidity { get; set; }//湿度 humidity AS Humidity,
        public string? Vacuum { get; set; }//真空 vacuum AS Vacuum,
        public string? Radiation { get; set; }//辐射 radiation AS Radiation,
        public string? Velocity { get; set; }//速度 velocity AS Velocity,
        public string? Frequency { get; set; }//频率 frequency AS Frequency,
        public string? Temperature { get; set; }//温度 temperature AS Temperature,
        public string? Conductivity { get; set; }//电导率 conductivity AS Conductivity,
        public string? Pressure { get; set; }//压力 pressure AS Pressure,
        public string? Diameter { get; set; }//直径 diameter AS Diameter,
        public string? OuterDiameter { get; set; }//外径 outer_diameter AS OuterDiameter,
        public string? InnerDiameter { get; set; }//内径 inner_diameter AS InnerDiameter,
        public string? Thickness { get; set; }//厚度 thickness AS Thickness,
        public string? Weight { get; set; }//重量 weight AS Weight,
        public string? Model { get; set; }//型号 model AS Model,
        public string? Lift { get; set; }//扬程 lift AS Lift,
        public string? Remarks { get; set; }//备注 remarks AS Remarks,
        public string? Customize1 { get; set; }//自定义1 customize1 AS Customize1,
        public string? Customize2 { get; set; }//自定义2 customize2 AS Customize2,
        public string? Customize3 { get; set; }//自定义3 customize3 AS Customize3
    }

    // 属性定义（attribute_definitions）
    public class DbAttributeDefinition
    {
        // 数据库属性定义实体（对应表 attribute_definitions）
        // 注意：此类用于数据库映射，命名为 DbAttributeDefinition 避免与 AutoCAD API 的 AttributeDefinition 冲突
        public int Id { get; set; }
        public string KeyName { get; set; } = string.Empty;        // 内部键：diameter
        public string DisplayName { get; set; } = string.Empty;    // 显示名：直径
        public string DataType { get; set; } = "string";           // string/number/date/json/bool
        public string Unit { get; set; } = string.Empty;           // 单位
        public bool IsSearchable { get; set; } = false;            // 是否用于搜索索引
        public bool IsCoreField { get; set; } = false;             // 是否为 core 字段（应同步到 cad_file_attributes）
        public string ValidationRegex { get; set; } = string.Empty;// 校验正则（可选）
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 属性模板
    /// </summary>
    public class AttributeTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }          // 模板名，如 "泵模板"
        public string Description { get; set; }
        public List<AttributeTemplateItem> Items { get; set; } = new List<AttributeTemplateItem>();
    }

    /// <summary>
    /// 模板项：模板包含的属性及是否必填
    /// </summary>
    public class AttributeTemplateItem
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int AttributeId { get; set; }
        public bool Required { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// EAV 值表实体
    /// </summary>
    public class FileAttributeValue
    {
        public long Id { get; set; }
        public int FileId { get; set; }           // cad_file_storage.id
        public int AttributeId { get; set; }      // attribute_definitions.id
        public string ValueString { get; set; }
        public double? ValueNumber { get; set; }
        public DateTime? ValueDate { get; set; }
        public string ValueJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    /// <summary>
    /// 分类统计信息
    /// </summary>
    public class CategoryStatistics
    {
        public int Id { get; set; }//ID
        public int CategoryId { get; set; }//分类ID
        public string CategoryType { get; set; }//分类类型
        public int FileCount { get; set; }//文件数量
        public long TotalSize { get; set; }//总大小
        public DateTime? LastFileAdded { get; set; }//最后添加文件时间
        public DateTime UpdatedAt { get; set; }//更新时间
    }

    /// <summary>
    /// 文件访问统计
    /// </summary>
    public class FileAccessStats
    {
        public int TotalAccess { get; set; }//总访问次数
        public int DownloadCount { get; set; }//下载次数
        public int ViewCount { get; set; }//浏览次数
        public DateTime? FirstAccess { get; set; }//首次访问时间
        public DateTime? LastAccess { get; set; }//最后访问时间
    }

    #endregion

    #region 显示图元属性

    /// <summary>
    /// 属性数据模型
    /// </summary>
    public class PropertyPair
    {
        public string PropertyName1 { get; set; }
        public string PropertyValue1 { get; set; }
        public string PropertyName2 { get; set; }
        public string PropertyValue2 { get; set; }

        public PropertyPair(string name1, string value1, string name2 = "", string value2 = "")
        {
            PropertyName1 = name1 ?? "";
            PropertyValue1 = value1 ?? "";
            PropertyName2 = name2 ?? "";
            PropertyValue2 = value2 ?? "";
        }
    }

    #endregion

    #region SW实体
    /// <summary>
    /// SW主分类实体
    /// </summary>
    public class SwCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// SW子分类实体
    /// </summary>
    public class SwSubcategory
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// SW图元实体
    /// </summary>
    public class SwGraphic
    {
        public int Id { get; set; }//id
        public int SubcategoryId { get; set; }//所在子类id
        public string FileName { get; set; }//文件名
        public string DisplayName { get; set; }//显示名
        public string ElementBlockName { get; set; }//元素块名
        public string LayerName { get; set; }//层名
        public int? ColorIndex { get; set; }//颜色索引
        public string FilePath { get; set; }//文件路径
        public string PreviewImageName { get; set; }
        public string PreviewImagePath { get; set; }//预览图片路径
        public long? FileSize { get; set; }//文件大小
        public DateTime CreatedAt { get; set; }//创建时间
        public DateTime UpdatedAt { get; set; }//更新时间
    }

    /// <summary>
    /// SW图元属性实体
    /// </summary>
    public class SwGraphicAttribute
    {
        public int Id { get; set; }//Id
        public int CadGraphiceId { get; set; }//cad图元id
        public string GraphiceName { get; set; }//图元名称
        public int SortOrder { get; set; }//排序序号
        public decimal? Length { get; set; }//长度
        public decimal? Width { get; set; }//宽度
        public decimal? Height { get; set; }//高度
        public decimal? Angle { get; set; }//角度
        public decimal? BasePointX { get; set; }//基点X
        public decimal? BasePointY { get; set; }//基点Y
        public decimal? BasePointZ { get; set; }//基点Z
        public DateTime CreatedAt { get; set; }//创建时间
        public DateTime UpdatedAt { get; set; }//更新时间
        public string Description { get; set; }//描述
        public string MediumName { get; set; }//介质
        public string Specifications { get; set; }//规格
        public string Material { get; set; }//材质
        public string StandardNumber { get; set; }//标准编号
        public string Power { get; set; }//功率
        public string Volume { get; set; }//容积
        public string Pressure { get; set; }//压力
        public string Temperature { get; set; }//温度
        public string Diameter { get; set; }//直径
        public string OuterDiameter { get; set; }//外径
        public string InnerDiameter { get; set; }//内径
        public string Thickness { get; set; }//厚度
        public string Weight { get; set; }//重量
        public string Model { get; set; }//型号
        public string Remarks { get; set; }//备注
        public string Customize1 { get; set; }//自定义1
        public string Customize2 { get; set; }//自定义2
        public string Customize3 { get; set; }//自定义3
    }
    #endregion


    #endregion 属性

}
