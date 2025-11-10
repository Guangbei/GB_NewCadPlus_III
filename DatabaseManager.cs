using Dapper;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using MySqlX.XDevAPI.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III
{

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
    public class CadGraphic
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
    /// CAD图元属性实体
    /// </summary>
    public class CadGraphicAttribute
    {
        public int Id { get; set; }//Id
        public int CadGraphiceId { get; set; }//cad图元id
        public string? GraphiceName { get; set; }//图元名称
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
        public string? Description { get; set; }//描述
        public string? MediumName { get; set; }//介质
        public string? Specifications { get; set; }//规格
        public string? Material { get; set; }//材质
        public string? StandardNumber { get; set; }//标准编号
        public string? Power { get; set; }//功率
        public string? Volume { get; set; }//容积
        public string? Pressure { get; set; }//压力
        public string? Temperature { get; set; }//温度
        public string? Diameter { get; set; }//直径
        public string? OuterDiameter { get; set; }//外径
        public string? InnerDiameter { get; set; }//内径
        public string? Thickness { get; set; }//厚度
        public string? Weight { get; set; }//重量
        public string? Model { get; set; }//型号
        public string? Remarks { get; set; }//备注
        public string? Customize1 { get; set; }//自定义1
        public string? Customize2 { get; set; }//自定义2
        public string? Customize3 { get; set; }//自定义3
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
                System.Diagnostics.Debug.WriteLine($"生成子分类ID失败: {ex.Message}");
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
        public bool IsDatabaseAvailable { get; private set; } = false;
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
                System.Diagnostics.Debug.WriteLine("数据库连接测试成功");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库连接测试失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"查询返回 {categories.AsList().Count} 条记录");
                return categories.AsList();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库查询出错: {ex.Message}");
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
               sort_order AS SortOrder,
               created_at AS CreatedAt,
               updated_at AS UpdatedAt
            FROM cad_categories 
            WHERE name = @categoryName";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<CadCategory>(sql, new { categoryName });
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
                               WHERE parent_id = @categoryId 
                               ORDER BY sort_order";

            using var connection = new MySqlConnection(_connectionString);
            var subcategories = await connection.QueryAsync<CadSubcategory>(sql, new { categoryId });
            return subcategories.AsList();
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
                System.Diagnostics.Debug.WriteLine($"获取子分类时出错: {ex.Message}", ex);
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
                System.Diagnostics.Debug.WriteLine($"成功添加子分类: {subcategory.Name}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加CAD子分类时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"更新父级子分类列表失败: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"更新父级子分类列表失败: {ex.Message}");
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

        #region CAD图元操作
        /// <summary>
        /// 获取CAD图元
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<CadGraphic> GetCadGraphicByIdAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM cad_graphics WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<CadGraphic>(sql, new { Id = id });
        }
        /// <summary>
        /// 根据子分类ID获取图元
        /// </summary>
        public async Task<List<CadGraphic>> GetCadGraphicsBySubcategoryIdAsync(int subcategoryId)
        {
            const string sql = @"
            SELECT 
                id AS Id,
                subcategory_id AS SubcategoryId,//子分类ID
                file_name AS FileName,//文件名
                display_name AS DisplayName,//显示名称
                element_block_name AS ElementBlockName,//元素块名称
                layer_name AS LayerName,//层名称
                color_index AS ColorIndex,//颜色索引
                file_path AS FilePath,//文件路径
                preview_image_name AS PreviewImageName,//预览图片名称
                preview_image_path AS PreviewImagePath,//预览图片路径
                file_size AS FileSize,//文件大小
                created_at AS CreatedAt,//创建时间
                updated_at AS UpdatedAt,//更新时间
            FROM cad_graphics 
            WHERE subcategory_id = @SubcategoryId 
            ORDER BY file_name";
            using var connection = new MySqlConnection(_connectionString);
            var graphics = await connection.QueryAsync<CadGraphic>(sql, new { subcategoryId });
            return graphics.AsList();
        }
        /// <summary>
        /// 根据ID获取图元属性
        /// </summary>
        public async Task<CadGraphicAttribute> GetCadGraphicAttributeByGraphicIdAsync(int cad_graphice_id)
        {
            const string sql = @"
            SELECT 
                id AS Id,
                cad_graphice_id AS CadGraphiceId,//图元ID
                graphice_name AS GraphiceName,//图元名称
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
            FROM cad_graphic_attributes 
            WHERE cad_graphice_id = @graphicId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<CadGraphicAttribute>(sql, new { cad_graphice_id });
        }
        /// <summary>
        /// 添加CAD图元
        /// </summary>
        /// <param name="graphic"></param>
        /// <returns></returns>
        public async Task<int> AddCadGraphicAsync(CadGraphic graphic)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO cad_graphics (subcategory_id, file_name, display_name,element_block_name,layer_name,color_index, file_path,preview_image_name, preview_image_path, file_size) 
                VALUES (@SubcategoryId, @FileName, @DisplayName,@ElementBlockName,@LayerName,@ColorIndex, @FilePath,@PreviewImageName, @PreviewImagePath, @FileSize)";
            return await connection.ExecuteAsync(sql, graphic);
        }
        /// <summary>
        /// 修改CAD图元
        /// </summary>
        /// <param name="graphic"></param>
        /// <returns></returns>
        public async Task<int> UpdateCadGraphicAsync(CadGraphic graphic)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE cad_graphics 
                SET 
                   cad_graphice_id = @CadGraphiceId,//图元ID
                   subcategory_id = @SubcategoryId,
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
            return await connection.ExecuteAsync(sql, graphic);
        }
        /// <summary>
        /// 删除CAD图元
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteCadGraphicAsync(int id)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM cad_graphics WHERE id = @Id";
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
        public async Task<bool> UpdateGraphicsBatchAsync(List<CadGraphic> graphics)
        {
            const string updateSql = @"
            UPDATE cad_graphics 
            SET display_name = @DisplayName,
                file_path = @FilePath,
                preview_image_path = @PreviewImagePath,
                updated_at = NOW()
            WHERE id = @Id";

            using var connection = new MySqlConnection(_connectionString);
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.ExecuteAsync(updateSql, graphics, transaction);
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
    }


}
