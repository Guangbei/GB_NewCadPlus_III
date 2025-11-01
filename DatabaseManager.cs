using Dapper;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III
{

    // 在Database命名空间中添加以下类

    /// <summary>
    /// CAD主分类实体
    /// </summary>
    public class CadCategory
    {
        public int id { get; set; }
        public string name { get; set; }           
        public string display_name { get; set; }   
        public int sort_order { get; set; }        
        public DateTime created_at { get; set; }   
        public DateTime updated_at { get; set; }   
    }

    /// <summary>
    /// CAD子分类实体
    /// </summary>
    public class CadSubcategory
    {
        public int id { get; set; }
        public string name { get; set; }           
        public string display_name { get; set; }   
        public int parent_id { get; set; } // 父级子分类ID，用于支持多级分类
        public int sort_order { get; set; }        
        public DateTime created_at { get; set; }   
        public DateTime updated_at { get; set; }   
    }

    /// <summary>
    /// CAD图元实体
    /// </summary>
    public class CadGraphic
    {
        public int Id { get; set; }
        public int subcategory_id { get; set; }
        public string file_name { get; set; }
        public string display_name { get; set; }
        public string file_path { get; set; }
        public string preview_image_path { get; set; }
        public long? file_size { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// CAD图元属性实体
    /// </summary>
    public class CadGraphicAttribute
    {
        public int Id { get; set; }
        public int cad_graphice_id { get; set; }
        public string graphice_name { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? Angle { get; set; }
        public decimal? base_point_x { get; set; }
        public decimal? base_point_y { get; set; }
        public decimal? base_point_z { get; set; }
        public string layer_name { get; set; }
        public int? color_index { get; set; }
        public string description { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// SW主分类实体
    /// </summary>
    public class SwCategory
    {
        public int id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public int sort_order { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// SW子分类实体
    /// </summary>
    public class SwSubcategory
    {
        public int id { get; set; }
        public string name { get; set; }
        public string display_name { get; set; }
        public int parent_id { get; set; } // 父级子分类ID，用于支持多级分类
        public int sort_order { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// SW图元实体
    /// </summary>
    public class SwGraphic
    {
        public int Id { get; set; }
        public int subcategory_id { get; set; }
        public string file_name { get; set; }
        public string display_name { get; set; }
        public string file_path { get; set; }
        public string preview_image_path { get; set; }
        public long? file_size { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// SW图元属性实体
    /// </summary>
    public class SwGraphicAttribute
    {
        public int Id { get; set; }
        public int cad_graphice_id { get; set; }
        public string graphice_name { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? Angle { get; set; }
        public decimal? base_point_x { get; set; }
        public decimal? base_point_y { get; set; }
        public decimal? base_point_z { get; set; }
        public string layer_name { get; set; }
        public int? color_index { get; set; }
        public string description { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// 数据库访问类
    /// </summary>
    public class DatabaseManager
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        private readonly string _connectionString;
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
                using var connection = GetConnection();// 创建数据库连接
                System.Diagnostics.Debug.WriteLine("正在执行SQL查询: SELECT * FROM cad_categories ORDER BY sort_order, name");
                var sql = "SELECT * FROM cad_categories ORDER BY sort_order, name";
                var result = (await connection.QueryAsync<CadCategory>(sql)).AsList();
                System.Diagnostics.Debug.WriteLine($"查询返回 {result.Count} 条记录");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库查询出错: {ex.Message}");
                throw;
            }
            //using var connection = GetConnection();/
            //var sql = "SELECT * FROM cad_categories ORDER BY sort_order, name";// SQL查询语句从 cad_categories 表中选择所有列 ORDER BY sort_order, name：结果按 sort_order 字段升序排序，如果 sort_order 相同，则按 name 字段升序排序
            //return (await connection.QueryAsync<CadCategory>(sql)).AsList();// 执行查询并返回结果
            /*
             *connection.QueryAsync<CadCategory>(sql)：使用Dapper的异步查询方法,
             *<CadCategory>：指定查询结果要映射到的实体类型,自动将查询结果转换为 CadCategory 对象的集合.
             *await：异步等待查询完成，不阻塞线程.
             *AsList()：将 IEnumerable<CadCategory> 转换为 List<CadCategory>立即执行查询并将结果具体化为列表
             
             */
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
        /// <summary>
        /// 获取CAD分类
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<CadCategory> GetCadCategoryByNameAsync(string name)
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM cad_categories WHERE name = @Name";
            return await connection.QueryFirstOrDefaultAsync<CadCategory>(sql, new { Name = name });
        }
        /// <summary>
        /// 获取CAD图元属性
        /// </summary>
        /// <param name="graphicId"></param>
        /// <returns></returns>
        public async Task<CadGraphicAttribute> GetCadGraphicAttributeByGraphicIdAsync(int graphicId)
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM cad_graphic_attributes WHERE graphic_id = @GraphicId";
            return await connection.QueryFirstOrDefaultAsync<CadGraphicAttribute>(sql, new { GraphicId = graphicId });
        }

        /// <summary>
        /// 根据父ID获取CAD子分类
        /// </summary>
        public async Task<List<CadSubcategory>> GetCadSubcategoriesByParentIdAsync(int parentId)
        {
            using var connection = GetConnection();
            var sql = @"SELECT * FROM cad_subcategories 
                WHERE parent_id = @ParentId 
                ORDER BY sort_order, name";
            return (await connection.QueryAsync<CadSubcategory>(sql, new { ParentId = parentId })).AsList();
        }
        #endregion

        #region CAD子分类操作
        /// <summary>
        /// 获取指定CAD分类下的所有CAD子分类
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public async Task<List<CadSubcategory>> GetCadSubcategoriesByCategoryIdAsync(int categoryId)
        {
            using var connection = GetConnection();
            var sql = @"SELECT * FROM cad_subcategories 
                WHERE parent_id = @parent_id 
                ORDER BY parent_id, sort_order, name";
            return (await connection.QueryAsync<CadSubcategory>(sql, new { CategoryId = categoryId })).AsList();
        }
        /// <summary>
        /// 获取所有CAD子分类
        /// </summary>
        /// <returns></returns>
        public async Task<List<CadSubcategory>> GetAllCadSubcategoriesAsync()
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM cad_subcategories ORDER BY parent_id, parent_id, sort_order, name";
            return (await connection.QueryAsync<CadSubcategory>(sql)).AsList();
        }
        /// <summary>
        /// 添加CAD子分类
        /// </summary>
        /// <param name="subcategory"></param>
        /// <returns></returns>
        public async Task<int> AddCadSubcategoryAsync(CadSubcategory subcategory)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO cad_subcategories (id, name, display_name, parent_id, sort_order) 
                VALUES (@id, @name, @display_name, @parent_id, @sort_order)";
            return await connection.ExecuteAsync(sql, subcategory);
        }
        /// <summary>
        /// 修改CAD子分类
        /// </summary>
        /// <param name="subcategory"></param>
        /// <returns></returns>
        public async Task<int> UpdateCadSubcategoryAsync(CadSubcategory subcategory)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE cad_subcategories 
                SET name = @name, display_name = @display_name, parent_id = @parent_id, sort_order = @sort_order, updated_at = NOW() 
                WHERE id = @Id";
            return await connection.ExecuteAsync(sql, subcategory);
        }
        /// <summary>
        /// 删除CAD子分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteCadSubcategoryAsync(int id)
        {
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
        /// 获取指定CAD子分类下的所有CAD图元
        /// </summary>
        /// <param name="subcategoryId"></param>
        /// <returns></returns>
        public async Task<List<CadGraphic>> GetCadGraphicsBySubcategoryIdAsync(int subcategoryId)
        {
            using var connection = GetConnection();
            var sql = @"SELECT * FROM cad_graphics 
                WHERE subcategory_id = @SubcategoryId 
                ORDER BY display_name";
            return (await connection.QueryAsync<CadGraphic>(sql, new { SubcategoryId = subcategoryId })).AsList();
        }
        /// <summary>
        /// 添加CAD图元
        /// </summary>
        /// <param name="graphic"></param>
        /// <returns></returns>
        public async Task<int> AddCadGraphicAsync(CadGraphic graphic)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO cad_graphics (subcategory_id, file_name, display_name, file_path, preview_image_path, file_size) 
                VALUES (@SubcategoryId, @FileName, @DisplayName, @FilePath, @PreviewImagePath, @FileSize)";
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
                SET file_name = @FileName, display_name = @DisplayName, file_path = @FilePath, 
                    preview_image_path = @PreviewImagePath, file_size = @FileSize, updated_at = NOW() 
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
        /// <returns></returns>
        public async Task<List<SwCategory>> GetAllSwCategoriesAsync()
        {
            using var connection = GetConnection();
            var sql = "SELECT * FROM sw_categories ORDER BY sort_order, name";
            return (await connection.QueryAsync<SwCategory>(sql)).AsList();
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
        /// 获取指定SW子分类下的所有SW图元
        /// </summary>
        /// <param name="subcategoryId"></param>
        /// <returns></returns>
        public async Task<List<SwGraphic>> GetSwGraphicsBySubcategoryIdAsync(int subcategoryId)
        {
            using var connection = GetConnection();
            var sql = @"SELECT * FROM sw_graphics 
                WHERE subcategory_id = @SubcategoryId 
                ORDER BY display_name";
            return (await connection.QueryAsync<SwGraphic>(sql, new { SubcategoryId = subcategoryId })).AsList();
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
        #endregion

       



    }

}
