using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows.ToolPalette;
using Dapper;
using MySql.Data.MySqlClient;
using Mysqlx.Session;
using MySqlX.XDevAPI.Common;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.FormulaParsing.Utilities;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III
{
    #region 文件实体管理

    // 在DatabaseManager.cs中添加以下实体类

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
        public string FileHash { get; set; }     // 文件哈希值（用于去重）
        public string ElementBlockName { get; set; }//元素块名
        public string LayerName { get; set; }//层名
        public int? ColorIndex { get; set; }//颜色索引
        public string FilePath { get; set; }//文件路径
        public string PreviewImageName { get; set; }
        public string PreviewImagePath { get; set; }//预览图片路径
        public long? FileSize { get; set; }//文件大小
        public bool IsPreview { get; set; }      // 是否为预览文件
        public int Version { get; set; }         // 文件版本
        public string Description { get; set; }  // 文件描述
        public DateTime CreatedAt { get; set; }//创建时间
        public DateTime UpdatedAt { get; set; }//更新时间
        public bool IsActive { get; set; }       // 是否激活
        public string? CreatedBy { get; set; }    // 创建者
        public string CategoryType { get; set; } = "sub"; // main 或 sub
        public string? Title { get; set; }//标题
        public string Keywords { get; set; }//关键字
        public bool IsPublic { get; set; } = true;//是否公开
        public string UpdatedBy { get; set; }//更新者
        public DateTime? LastAccessedAt { get; set; }//最后访问时间
    }

    /// <summary>
    /// CAD图元属性实体
    /// </summary>
    public class FileAttribute
    {

        public int Id { get; set; }//属性Id id AS Id,
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
        public string? Pressure { get; set; }//压力 pressure AS Pressure,
        public string? Temperature { get; set; }//温度 temperature AS Temperature,
        public string? Diameter { get; set; }//直径 diameter AS Diameter,
        public string? OuterDiameter { get; set; }//外径 outer_diameter AS OuterDiameter,
        public string? InnerDiameter { get; set; }//内径 inner_diameter AS InnerDiameter,
        public string? Thickness { get; set; }//厚度 thickness AS Thickness,
        public string? Weight { get; set; }//重量 weight AS Weight,
        public string? Model { get; set; }//型号 model AS Model,
        public string? Remarks { get; set; }//备注 remarks AS Remarks,
        public string? Customize1 { get; set; }//自定义1 customize1 AS Customize1,
        public string? Customize2 { get; set; }//自定义2 customize2 AS Customize2,
        public string? Customize3 { get; set; }//自定义3 customize3 AS Customize3
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

    /// <summary>
    /// 分类ID生成器
    /// </summary>
    public static class CategoryIdGenerator
    {
        /// <summary>
        /// 生成主分类ID
        /// </summary>
        public static async Task<int> GenerateMainCategoryIdAsync(DatabaseManager dbManager)
        {
            try
            {
                // 获取最大的主分类ID
                var categories = await dbManager.GetAllCadCategoriesAsync();
                if (categories == null || categories.Count == 0)
                {
                    return 1000; // 第一个主分类ID
                }
                // 查找最大的主分类ID（千位数）
                var maxMainId = categories.Max(c => c.Id);
                int nextMainId = ((maxMainId / 1000) + 1) * 1000;
                return nextMainId;
            }
            catch
            {
                return 1000; // 出错时返回默认值
            }
        }

        /// <summary>
        /// 生成子分类ID
        /// </summary>
        public static async Task<int> GenerateSubcategoryIdAsync(DatabaseManager dbManager, int parentId)
        {
            try
            {
                int level = await DetermineCategoryLevelAsync(dbManager, parentId);// 确定层级
                if (level == 1)  // 生成新ID
                {
                    return await GenerateLevel2SubcategoryIdAsync(dbManager, parentId);   // 二级子分类：父ID(4位) + 序号(3位)
                }
                else
                {
                    return await GenerateLevel3PlusSubcategoryIdAsync(dbManager, parentId, level); // 三级及以上子分类：父ID + 序号(3位)
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"生成子分类ID失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确定分类层级
        /// </summary>
        private static async Task<int> DetermineCategoryLevelAsync(DatabaseManager dbManager, int parentId)
        {
            if (parentId < 10000)
            {
                return 1;  // 父级是主分类（4位），这是二级子分类
            }
            else
            {
                var parentSubcategory = await dbManager.GetCadSubcategoryByIdAsync(parentId); // 父级是子分类，需要确定是几级子分类
                if (parentSubcategory != null)
                {
                    return parentSubcategory.Level + 1;
                }
                else
                {
                    return 1; // 默认为二级分类
                }
            }
        }

        /// <summary>
        /// 生成二级子分类ID
        /// </summary>
        private static async Task<int> GenerateLevel2SubcategoryIdAsync(DatabaseManager dbManager, int parentId)
        {

            string parentPrefix = parentId.ToString();// 格式：父ID(4位) + 序号(3位)
            var subcategories = await dbManager.GetCadSubcategoriesByParentIdAsync(parentId);    // 获取同一父级下的所有二级子分类

            if (subcategories.Count == 0) return int.Parse($"{parentPrefix}001");
            var level2Subcategories = subcategories.Where(s => s.Level == 1).ToList();// 筛选出二级子分类

            var maxId = level2Subcategories.Max(s => s.Id);                                       // 找到最大的序号
            string maxIdStr = maxId.ToString();                                                              // 获取最大ID的末尾3位
            if (maxIdStr.Length >= 3)                                                                        // 确保最大ID的末尾有3位
            {
                string sequenceStr = maxIdStr.Substring(maxIdStr.Length - 3);                       // 获取最大ID的末尾3位
                if (int.TryParse(sequenceStr, out int sequence))                                    // 尝试将末尾3位转换为数字
                {
                    return int.Parse($"{parentPrefix}{(sequence + 1):D3}");                               // 返回下一个ID
                }
            }
            int nextSequence = level2Subcategories.Count + 1;                                               // 如果解析失败，使用计数方式
            return int.Parse($"{parentPrefix}{nextSequence:D3}");                                        // 返回下一个ID

        }

        /// <summary>
        /// 生成三级及以上子分类ID
        /// </summary>
        private static async Task<int> GenerateLevel3PlusSubcategoryIdAsync(DatabaseManager dbManager, int parentId, int level)
        {
            string parentPrefix = parentId.ToString(); // 格式：父ID + 序号(3位)
            var subcategories = await dbManager.GetCadSubcategoriesByParentIdAsync(parentId);  // 获取同一父级下的所有同级子分类
            var sameLevelSubcategories = subcategories.Where(s => s.Level == level).ToList();// 筛选出同一层级的子分类
            if (sameLevelSubcategories.Count == 0)// 如果没有同级子分类
            {
                return int.Parse($"{parentPrefix}001");
            }
            else
            {
                var maxId = sameLevelSubcategories.Max(s => s.Id);// 找到最大的序号
                string maxIdStr = maxId.ToString();// 获取最大ID的末尾3位
                if (maxIdStr.Length >= 3)// 确保最大ID的末尾有3位
                {
                    string sequenceStr = maxIdStr.Substring(maxIdStr.Length - 3);// 获取最大ID的末尾3位
                    if (int.TryParse(sequenceStr, out int sequence))// 尝试将末尾3位转换为数字
                    {
                        return int.Parse($"{parentPrefix}{(sequence + 1):D3}");// 返回下一个ID
                    }
                }
                int nextSequence = sameLevelSubcategories.Count + 1;   // 如果解析失败，使用计数方式
                return int.Parse($"{parentPrefix}{nextSequence:D3}");// 返回下一个ID
            }
        }
    }

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

    /// <summary>
    /// 数据库访问类
    /// </summary>
    public class DatabaseManager
    {
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
        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <returns></returns>
        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }


        #region CAD分类操作
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
        /// <summary>
        /// 添加CAD分类
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task<int> AddCadCategoryAsync(CadCategory category)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO cad_categories (name, display_name, sort_order) 
                VALUES (@Name, @DisplayName, @SortOrder)";
            return await connection.ExecuteAsync(sql, category);
        }
        /// <summary>
        /// 修改CAD分类
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task<int> UpdateCadCategoryAsync(CadCategory category)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE cad_categories 
                SET name = @Name, display_name = @DisplayName, sort_order = @SortOrder, updated_at = NOW() 
                WHERE id = @Id";
            return await connection.ExecuteAsync(sql, category);
        }
        /// <summary>
        /// 删除CAD分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteCadCategoryAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM cad_categories WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id });
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
                
                var subcategories = await connection.QueryAsync<CadSubcategory>(sql,  parameters );
                return subcategories.AsList();
            }
            catch(Exception ex)
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
           SET  parent_id = @ParentId, name = @Name, display_name = @DisplayName, subcategory_ids = @newSubcategoryIds, sort_order = @SortOrder, updated_at = NOW() 
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
        /// <param name="newSubcategoryId"></param>
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
            //这个方法还有不完善的地方，比如子分类下还有子分类或图元，如果不删除子分类下的图元，则无法删除子分类，需要先删除子分类下的图元，不然这个分类下的图元与子分类就是在数据库中的垃圾数据；
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
            const string sql = "SELECT COALESCE(MAX(sort_order), 0) FROM cad_subcategories WHERE parent_id = @parentId";

            using var connection = new MySqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync<int>(sql, new { parentId });
            return result;
        }

        #endregion

        #region 优化的文件管理方法

        /// <summary>
        /// 获取分类下的所有文件（支持分页和排序）
        /// </summary>
        public async Task<List<FileStorage>> GetFilesByCategoryAsync(int categoryId, string categoryType = "sub",
            int page = 1, int pageSize = 50, string orderBy = "created_at DESC")
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
        FROM cad_file_stroage 
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
                    categoryId,
                    categoryType,
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
        /// 根据分类ID获取文件列表
        /// </summary>
        //public async Task<List<FileStorage>> GetFilesByCategoryIdAsync(int categoryId, string categoryType = "sub")
        //{
        //    const string sql = @"
        //SELECT 
        //    id AS Id,
        //    category_id AS CategoryId,
        //    category_type AS CategoryType,
        //    file_name AS FileName,
        //    file_stored_name AS FileStoredName,
        //    file_path AS FilePath,
        //    file_type AS FileType,
        //    file_size AS FileSize,
        //    file_hash AS FileHash,
        //    display_name AS DisplayName,
        //    element_block_name AS ElementBlockName,
        //    layer_name AS LayerName,
        //    color_index AS ColorIndex,
        //    preview_image_name AS PreviewImageName,
        //    preview_image_path AS PreviewImagePath,
        //    description AS Description,
        //    version AS Version,
        //    is_preview AS IsPreview,
        //    is_active AS IsActive,
        //    created_by AS CreatedBy,
        //    created_at AS CreatedAt,
        //    updated_at AS UpdatedAt
        //FROM cad_file_storage 
        //WHERE category_id = @categoryId 
        //  AND category_type = @categoryType
        //  AND is_active = 1
        //ORDER BY created_at DESC";
        //    try
        //    {
        //        using var connection = new MySqlConnection(_connectionString);
        //        return (await connection.QueryAsync<FileStorage>(sql, new { categoryId, categoryType })).AsList();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return new List<FileStorage>();
        //    }

        //}
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
            // 获取文件信息
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

            // 获取文件属性信息
            const string attributeSql = @"
            SELECT 
                id AS Id,
                file_storage_id AS FileStorageId,
                file_name AS FileName,
                length AS Length,
                width AS Width,
                height AS Height,
                angle AS Angle,
                base_point_x AS BasePointX,
                base_point_y AS BasePointY,
                base_point_z AS BasePointZ,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                description AS Description,
                medium_name AS MediumName,
                specifications AS Specifications,
                material AS Material,
                standard_number AS StandardNumber,
                power AS Power,
                volume AS Volume,
                pressure AS Pressure,
                temperature AS Temperature,
                diameter AS Diameter,
                outer_diameter AS OuterDiameter,
                inner_diameter AS InnerDiameter,
                thickness AS Thickness,
                weight AS Weight,
                model AS Model,
                remarks AS Remarks,
                customize1 AS Customize1,
                customize2 AS Customize2,
                customize3 AS Customize3
            FROM cad_file_attributes 
            WHERE file_storage_id = @FileStorageId";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                int Id = fileId;
                var file = await connection.QuerySingleOrDefaultAsync<FileStorage>(fileSql, new { Id });
                var FileStorageId = fileId;
                var attribute = await connection.QuerySingleOrDefaultAsync<FileAttribute>(attributeSql, new { FileStorageId });

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
        public async Task<FileStorage> GetFileStorageAsync(string fileName)
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
             WHERE file_name = @FileName";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var parameters = new { FileName = fileName };
                var file = await connection.QuerySingleOrDefaultAsync<FileStorage>(fileSql, parameters);
                return file;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取文件详细信息时出错: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 获取文件属性的详细信息
        /// </summary>
        public async Task<FileAttribute> GetFileAttributeAsync(string fileName)
        {

            // 获取文件属性信息
            const string attributeSql = @"
            SELECT 
                id AS Id,
                file_storage_id AS FileStorageId,
                file_name AS FileName,
                length AS Length,
                width AS Width,
                height AS Height,
                angle AS Angle,
                base_point_x AS BasePointX,
                base_point_y AS BasePointY,
                base_point_z AS BasePointZ,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                description AS Description,
                medium_name AS MediumName,
                specifications AS Specifications,
                material AS Material,
                standard_number AS StandardNumber,
                power AS Power,
                volume AS Volume,
                pressure AS Pressure,
                temperature AS Temperature,
                diameter AS Diameter,
                outer_diameter AS OuterDiameter,
                inner_diameter AS InnerDiameter,
                thickness AS Thickness,
                weight AS Weight,
                model AS Model,
                remarks AS Remarks,
                customize1 AS Customize1,
                customize2 AS Customize2,
                customize3 AS Customize3
            FROM cad_file_attributes 
            WHERE file_name = @FileName";

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // 使用参数化查询，避免 SQL 注入
                var parameters = new { FileName = Path.GetFileNameWithoutExtension(fileName) };
                var attribute = await connection.QuerySingleOrDefaultAsync<FileAttribute>(attributeSql, parameters);

                return attribute; // 移除了多余的括号
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
        public async Task UpdateCategoryStatisticsAsync(int categoryId, string categoryType)
        {
            const string sql = @"
        INSERT INTO category_statistics 
        (category_id, category_type, file_count, total_size, last_file_added)
        SELECT 
            @CategoryId,
            @CategoryType,
            COUNT(*),
            COALESCE(SUM(file_size), 0),
            MAX(created_at)
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
                await connection.ExecuteAsync(sql, new { categoryId, categoryType });
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"更新分类统计信息时出错: {ex.Message}");
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

        /// <summary>
        /// 获取文件访问统计
        /// </summary>
        public async Task<FileAccessStats> GetFileAccessStatsAsync(int fileId, int days = 30)
        {
            const string sql = @"
        SELECT 
            COUNT(*) as TotalAccess,
            COUNT(CASE WHEN action_type = 'download' THEN 1 END) as DownloadCount,
            COUNT(CASE WHEN action_type = 'view' THEN 1 END) as ViewCount,
            MIN(access_time) as FirstAccess,
            MAX(access_time) as LastAccess
        FROM file_access_logs 
        WHERE file_id = @fileId 
          AND access_time >= DATE_SUB(NOW(), INTERVAL @days DAY)";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return await connection.QuerySingleOrDefaultAsync<FileAccessStats>(sql, new { fileId, days });
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取文件访问统计时出错: {ex.Message}");
                return new FileAccessStats();
            }

        }

        /// <summary>
        /// 记录文件访问日志
        /// </summary>
        public async Task<int> LogFileAccessAsync(int fileId, string userName, string actionType,
            string ipAddress = "", string userAgent = "")
        {
            const string sql = @"
        INSERT INTO file_access_logs 
        (file_id, user_name, action_type, ip_address, user_agent, access_time)
        VALUES 
        (@FileId, @UserName, @ActionType, @IpAddress, @UserAgent, @AccessTime)";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return await connection.ExecuteAsync(sql, new
                {
                    FileId = fileId,
                    UserName = userName,
                    ActionType = actionType,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AccessTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"记录文件访问日志时出错: {ex.Message}");
                return 0;
            }

        }

        /// <summary>
        /// 获取热门文件（按访问次数排序）
        /// </summary>
        public async Task<List<FileStorage>> GetPopularFilesAsync(int limit = 10)
        {
            const string sql = @"
        SELECT 
            cg.id AS Id,
            cg.category_id AS CategoryId,
            cg.file_attribute_id AS FileAttributeId,
            cg.file_name AS FileName,
            cg.file_stored_name AS FileStoredName,
            cg.file_type AS FileType,
            cg.file_hash AS FileHash,
            cg.display_name AS DisplayName,
            cg.element_block_name AS ElementBlockName,
            cg.layer_name AS LayerName,
            cg.color_index AS ColorIndex,
            cg.file_path AS FilePath,
            cg.preview_image_name AS PreviewImageName,
            cg.preview_image_path AS PreviewImagePath,
            cg.file_size AS FileSize,
            cg.is_preview AS IsPreview,
            cg.version AS Version,
            cg.description AS Description,
            cg.is_active AS IsActive,
            cg.created_by AS CreatedBy,
            cg.category_type AS CategoryType,
            cg.title AS Title,
            cg.keywords AS Keywords,
            cg.is_public AS IsPublic,
            cg.updated_by AS UpdatedBy,
            cg.last_accessed_at AS LastAccessedAt,
            cg.created_at AS CreatedAt,
            cg.updated_at AS UpdatedAt,
            COUNT(fal.id) as AccessCount
        FROM cad_file_storage cg
        LEFT JOIN file_access_logs fal ON cg.id = fal.file_id
        WHERE cg.is_active = 1
        GROUP BY cg.id
        ORDER BY AccessCount DESC, cg.updated_at DESC
        LIMIT @limit";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return (await connection.QueryAsync<FileStorage>(sql, new { limit })).AsList();

            }
            catch (Exception ex)
            {
                LogManager.Instance.LogInfo($"获取热门文件时出错: {ex.Message}");
                return new List<FileStorage>();
            }

        }

        #endregion

        #region 文件操作方法
        #region CAD图元操作
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
                 subcategory_id AS SubcategoryId,
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
                 updated_at AS UpdatedAt,
             FROM cad_file_storage 
             WHERE subcategory_id = @SubcategoryId 
             ORDER BY file_name";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var parameters = new { SubcategoryId = subcategoryId };
                var FileStorage = await connection.QueryAsync<FileStorage>(sql, new { parameters });
                return FileStorage.AsList();
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"获取子分类图元列表时出错: {ex.Message}");
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
                 id AS Id,//主键ID
                 file_storage_id AS FileStorageId ,//文件ID
                 file_Name AS FileName,//图元名称
                 length AS Length,//长度
                 width AS Width,//宽度
                 height AS Height,//高度
                 angle AS Angle,//角度
                 base_point_x AS BasePointX,//基点X
                 base_point_y AS BasePointY,//基点Y
                 base_point_z AS BasePointZ,//基点Z
                 created_at AS CreatedAt,//创建时间
                 updated_at AS UpdatedAt,//更新时间
                 description AS Description,//描述
                 medium_name AS MediumName,//中文名称
                 specifications AS Specifications,//规格
                 material AS Material,//材质
                 standard_number AS StandardNumber,//标准编号
                 power AS Power,//功率
                 volume AS Volume,//容积
                 pressure AS Pressure,//压力
                 temperature AS Temperature,//温度
                 diameter AS Diameter,//直径
                 outer_diameter AS OuterDiameter,//外径
                 inner_diameter AS InnerDiameter,//内径
                 thickness AS Thickness,//厚度
                 weight AS Weight,//重量
                 model AS Model,//材质
                 remarks AS Remarks,//备注
                 customize1 AS Customize1,//自定义字段1
                 customize2 AS Customize2,//自定义字段2
                 customize3 AS Customize3,//自定义字段3
             FROM cad_file_attributes 
             WHERE file_storage_id = @FileStorageId";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var FileStorageId = fileStorageId;
                var result = await connection.QuerySingleOrDefaultAsync<FileAttribute>(sql, new { FileStorageId });
                return result;
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"获取图元属性时出错: {ex.Message}");
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
        /// 删除CAD图元
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
                 file_path AS FilePath,
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
             WHERE id = @fileId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<FileStorage>(sql, new { fileId });
        }

        /// <summary>
        /// 更新文件信息
        /// </summary>
        public async Task<int> UpdateFileAsync(FileStorage file)
        {
            const string sql = @"
            UPDATE file_storage 
            SET file_name = @FileName, 
                description = @Description,
                updated_at = @UpdatedAt
            WHERE id = @Id";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, file);
        }

        /// <summary>
        /// 删除文件（软删除）
        /// </summary>
        public async Task<int> DeleteFileAsync(int fileId, string deletedBy)
        {
            const string sql = @"
                UPDATE file_storage 
             SET is_active = 0, 
                 updated_at = @UpdatedAt
             WHERE id = @Id";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, new { Id = fileId, UpdatedAt = DateTime.Now });
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
                file_name AS FileName,
                file_stored_name AS FileStoredName,
                display_name AS DisplayName,
                file_path AS FilePath,
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
                using var connection = new MySqlConnection(_connectionString);
                var result = await connection.QuerySingleOrDefaultAsync<FileStorage>(sql, fileHash);
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取哈希值检查文件出错：", ex.Message);
                return null;
            }
        }


        #region 文件访问日志

        /// <summary>
        /// 记录文件访问日志
        /// </summary>
        public async Task<int> AddFileAccessLogAsync(FileAccessLog accessLog)
        {
            const string sql = @"
             INSERT INTO file_access_logs (file_id, user_name, action_type, access_time, ip_address)
             VALUES (@FileId, @UserName, @ActionType, @AccessTime, @IpAddress)";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, accessLog);
        }

        /// <summary>
        /// 获取文件访问统计
        /// </summary>
        public async Task<int> GetFileAccessCountAsync(int fileId)
        {
            const string sql = "SELECT COUNT(*) FROM file_access_logs WHERE file_id = @fileId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<int>(sql, new { fileId });
        }

        #endregion



        #endregion
        /// <summary>
        /// 添加文件存储记录
        /// </summary>
        public async Task<int> AddFileStorageAsync(FileStorage file)
        {
            //字段映射核与数据库字段对完毕
            const string sql = @"
            INSERT INTO cad_file_storage 
            (category_id, file_attribute_id, file_name, file_stored_name, display_name, file_type, file_hash,
             element_block_name, layer_name, color_index, file_path, preview_image_name, preview_image_path,
             file_size, is_preview, version, description, is_active, created_by, category_type, title, keywords,
             is_public, updated_by, last_accessed_at, created_at, updated_at)
            VALUES 
            (@CategoryId, @FileAttributeId, @FileName, @FileStoredName, @DisplayName, @FileType, @FileHash,
             @ElementBlockName, @LayerName, @ColorIndex, @FilePath,@PreviewImageName, @PreviewImagePath,
             @FileSize, @IsPreview, @Version, @Description, @IsActive, @CreatedBy, @CategoryType, @Title, @Keywords,
             @IsPublic, @UpdatedBy, @LastAccessedAt, @CreatedAt, @UpdatedAt)";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var result = await connection.QuerySingleOrDefaultAsync<int>(sql, file);
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("插入CAD文件属性失败", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 添加文件属性记录
        /// </summary>
        public async Task<int> AddFileAttributeAsync(FileAttribute attribute)
        {
            //字段映射核与数据库字段对完毕
            const string sql = @"
            INSERT INTO cad_file_attributes 
            (file_storage_id, file_name, length, width, height, angle, base_point_x, base_point_y, base_point_z,
             created_at, updated_at, description, medium_name, specifications, material, standard_number,
             power, volume, pressure, temperature, diameter, outer_diameter, inner_diameter,
             thickness, weight, model, remarks, customize1, customize2, customize3)
            VALUES 
            (@FileStorageId, @FileName, @Length, @Width, @Height, @Angle, @BasePointX, @BasePointY, @BasePointZ,
             @CreatedAt, @UpdatedAt, @Description, @MediumName, @Specifications, @Material, @StandardNumber,
             @Power, @Volume, @Pressure, @Temperature, @Diameter, @OuterDiameter, @InnerDiameter,
             @Thickness, @Weight, @Model, @Remarks, @Customize1, @Customize2, @Customize3)";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var result = await connection.ExecuteAsync(sql, attribute);
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("插入CAD文件属性失败", ex.Message);
                return 0;
            }
        }



        /// <summary>
        /// 更新文件信息
        /// </summary>
        public async Task<int> UpdateFileStorageAsync(FileStorage file)
        {
            const string sql = @"
        UPDATE cad_file_storage 
        SET 
            file_attribute_id = @FileAttributeId,
            file_name = @FileName,
            file_stored_name = @FileStoredName,
            file_type = @FileType,
            file_hash = @FileHash,
            display_name = @DisplayName,
            element_block_name = @ElementBlockName,
            layer_name = @LayerName,
            color_index = @ColorIndex,
            file_path = @FilePath,
            preview_image_name = @PreviewImageName,
            preview_image_path = @PreviewImagePath,
            file_size = @FileSize,
            created_at = @CreatedAt,
            updated_at = @UpdatedAt
            is_preview = @IsPreview,
            version = @Version,
            description = @Description,
            is_active = @IsActive,
            created_by = @CreatedBy,
            category_type = @CategoryType,
            title = @Title,
            keywords = @Keywords,
            is_public = @IsPublic,
            updated_by = @UpdatedBy,
            last_accessed_at = @LastAccessedAt,
        WHERE id = @Id";
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                return await connection.ExecuteAsync(sql, file);
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新CAD文件属性失败", ex.Message);
                return 0;
            }

        }

        /// <summary>
        /// 更新文件属性
        /// </summary>
        public async Task<int> UpdateFileAttributeAsync(FileAttribute attribute)
        {
            const string sql = @"
        UPDATE cad_file_attributes 
        SET 
            file_storage_id = @FileStorageId,
            file_name = @FileName,
            length = @Length,
            width = @Width,
            height = @Height,
            angle = @Angle,
            base_point_x = @BasePointX,
            base_point_y = @BasePointY,
            base_point_z = @BasePointZ,
            created_at = @CreatedAt,
            updated_at = @UpdatedAt,
            description = @Description,
            medium_name = @MediumName,
            specifications = @Specifications,
            material = @Material,
            standard_number = @StandardNumber,
            power = @Power,
            volume = @Volume,
            pressure = @Pressure,
            temperature = @Temperature,
            diameter = @Diameter,
            outer_diameter = @OuterDiameter,
            inner_diameter = @InnerDiameter,
            thickness = @Thickness,
            weight = @Weight,
            model = @Model,
            remarks = @Remarks,
            customize1 = @Customize1,
            customize2 = @Customize2,
            customize3 = @Customize3
        WHERE id = @Id";

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var result = await connection.ExecuteAsync(sql, attribute);
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("插入CAD文件属性失败", ex.Message);
                return 0;
            }
        }


        /// <summary>
        /// 删除文件存储记录
        /// </summary>
        public async Task<int> DeleteFileStorageAsync(int fileId)
        {
            try
            {
                const string sql = "DELETE FROM cad_file_storage WHERE id = @fileId";

                using var connection = new MySqlConnection(_connectionString);
                return await connection.ExecuteAsync(sql, new { fileId });
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除CAD文件存储记录失败", ex.Message);
                return 0;
            }

        }

        /// <summary>
        /// 删除文件属性记录
        /// </summary>
        public async Task<int> DeleteFileAttributeAsync(int attributeId)
        {
            try
            {
                const string sql = "DELETE FROM cad_file_attributes WHERE id = @attributeId";

                using var connection = new MySqlConnection(_connectionString);
                return await connection.ExecuteAsync(sql, new { attributeId });
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除CAD文件属性记录失败", ex.Message);
                return 0;
            }

        }

        /// <summary>
        /// 删除文件标签记录
        /// </summary>
        public async Task<int> DeleteFileTagsByFileIdAsync(int fileId)
        {
            try
            {
                const string sql = "DELETE FROM file_tags WHERE file_id = @fileId";

                using var connection = new MySqlConnection(_connectionString);
                return await connection.ExecuteAsync(sql, new { fileId });
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除CAD文件标签记录失败", ex.Message);
                return 0;
            }

        }





        #region 文件标签数据库方法
        /// <summary>
        /// 添加文件标签
        /// </summary>
        /// <param name="tag">文件标签对象</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> AddFileTagAsync(FileTag tag)
        {
            const string sql = @"
        INSERT INTO file_tags 
        (file_id, tag_name, created_at)
        VALUES 
        (@FileId, @TagName, @CreatedAt)";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, new
            {
                FileId = tag.FileId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt
            });
        }

        /// <summary>
        /// 根据文件ID获取所有标签
        /// </summary>
        public async Task<List<FileTag>> GetFileTagsByFileIdAsync(int fileId)
        {
            const string sql = @"
        SELECT 
            id AS Id,
            file_id AS FileId,
            tag_name AS TagName,
            created_at AS CreatedAt
        FROM file_tags 
        WHERE file_id = @fileId
        ORDER BY created_at DESC";

            using var connection = new MySqlConnection(_connectionString);
            return (await connection.QueryAsync<FileTag>(sql, new { fileId })).AsList();
        }

        /// <summary>
        /// 根据标签名称获取文件ID列表
        /// </summary>
        public async Task<List<int>> GetFileIdsByTagNameAsync(string tagName)
        {
            const string sql = @"
        SELECT DISTINCT file_id 
        FROM file_tags 
        WHERE tag_name = @tagName";

            using var connection = new MySqlConnection(_connectionString);
            return (await connection.QueryAsync<int>(sql, new { tagName })).AsList();
        }

        /// <summary>
        /// 删除文件的所有标签
        /// </summary>
        public async Task<int> DeleteFileTagsAsync(int fileId)
        {
            const string sql = "DELETE FROM file_tags WHERE file_id = @fileId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, new { fileId });
        }

        /// <summary>
        /// 删除特定的文件标签
        /// </summary>
        public async Task<int> DeleteFileTagAsync(int fileId, string tagName)
        {
            const string sql = "DELETE FROM file_tags WHERE file_id = @fileId AND tag_name = @tagName";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, new { fileId, tagName });
        }

        /// <summary>
        /// 检查文件是否具有特定标签
        /// </summary>
        public async Task<bool> FileHasTagAsync(int fileId, string tagName)
        {
            const string sql = "SELECT COUNT(*) FROM file_tags WHERE file_id = @fileId AND tag_name = @tagName";

            using var connection = new MySqlConnection(_connectionString);
            var count = await connection.QuerySingleAsync<int>(sql, new { fileId, tagName });
            return count > 0;
        }

        /// <summary>
        /// 获取所有唯一的标签名称
        /// </summary>
        public async Task<List<string>> GetAllUniqueTagNamesAsync()
        {
            const string sql = "SELECT DISTINCT tag_name FROM file_tags ORDER BY tag_name";

            using var connection = new MySqlConnection(_connectionString);
            return (await connection.QueryAsync<string>(sql)).AsList();
        }
        #endregion

        #endregion


    }
}