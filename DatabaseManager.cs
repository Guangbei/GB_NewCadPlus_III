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

    /// <summary>
    /// CAD主分类实体
    /// </summary>
    public class CadCategory
    {
    //    public int id { get; set; }
    //    public string name { get; set; }
    //    public string display_name { get; set; }
    //    public int sort_order { get; set; }
    //    public DateTime created_at { get; set; }
    //    public DateTime updated_at { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// CAD子分类实体
    /// </summary>
    public class CadSubcategory
    {
        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public string display_name { get; set; }
        //    public int parent_id { get; set; } // 父级子分类ID，用于支持多级分类
        //    public int sort_order { get; set; }
        //    public DateTime created_at { get; set; }
        //    public DateTime updated_at { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// CAD图元实体
    /// </summary>
    public class CadGraphic
    {
        //public int Id { get; set; }
        //public int subcategory_id { get; set; }
        //public string file_name { get; set; }
        //public string display_name { get; set; }
        //public string file_path { get; set; }
        //public string preview_image_path { get; set; }
        //public long? file_size { get; set; }
        //public DateTime created_at { get; set; }
        //public DateTime updated_at { get; set; }
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
        //public int Id { get; set; }
        //public int cad_graphice_id { get; set; }
        //public string graphice_name { get; set; }
        //public decimal? Length { get; set; }
        //public decimal? Width { get; set; }
        //public decimal? Height { get; set; }
        //public decimal? Angle { get; set; }
        //public decimal? base_point_x { get; set; }
        //public decimal? base_point_y { get; set; }
        //public decimal? base_point_z { get; set; }
        //public string layer_name { get; set; }
        //public int? color_index { get; set; }
        //public string description { get; set; }
        //public DateTime created_at { get; set; }
        //public DateTime updated_at { get; set; }
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

    /// <summary>
    /// SW主分类实体
    /// </summary>
    public class SwCategory
    {

        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public string display_name { get; set; }
        //    public int sort_order { get; set; }
        //    public DateTime created_at { get; set; }
        //    public DateTime updated_at { get; set; }
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
        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public string display_name { get; set; }
        //    public int parent_id { get; set; } // 父级子分类ID，用于支持多级分类
        //    public int sort_order { get; set; }
        //    public DateTime created_at { get; set; }
        //    public DateTime updated_at { get; set; }
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int ParentId { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// SW图元实体
    /// </summary>
    public class SwGraphic
    {
        //public int Id { get; set; }
        //public int subcategory_id { get; set; }
        //public string file_name { get; set; }
        //public string display_name { get; set; }
        //public string file_path { get; set; }
        //public string preview_image_path { get; set; }
        //public long? file_size { get; set; }
        //public DateTime created_at { get; set; }
        //public DateTime updated_at { get; set; }
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
        //public int Id { get; set; }
        //public int cad_graphice_id { get; set; }
        //public string graphice_name { get; set; }
        //public decimal? Length { get; set; }
        //public decimal? Width { get; set; }
        //public decimal? Height { get; set; }
        //public decimal? Angle { get; set; }
        //public decimal? base_point_x { get; set; }
        //public decimal? base_point_y { get; set; }
        //public decimal? base_point_z { get; set; }
        //public string layer_name { get; set; }
        //public int? color_index { get; set; }
        //public string description { get; set; }
        //public DateTime created_at { get; set; }
        //public DateTime updated_at { get; set; }
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
                const string sql = @"
                                   SELECT 
                                       id,
                                       name,
                                       display_name,
                                       sort_order,
                                       created_at,
                                       updated_at
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
                id,
                name,
                display_name,
                sort_order,
                created_at,
                updated_at
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
            var sql = "SELECT * FROM cad_subcategories ORDER BY parent_id, parent_id, sort_order, name";
            return (await connection.QueryAsync<CadSubcategory>(sql)).AsList();
        }
        /// <summary>
        /// 根据分类ID获取子分类
        /// </summary>
        public async Task<List<CadSubcategory>> GetCadSubcategoriesByCategoryIdAsync(int categoryId)
        {
            const string sql = @"
                               SELECT 
                                   id,
                                   category_id,
                                   name,
                                   display_name,
                                   parent_id,
                                   sort_order,
                                   created_at,
                                   updated_at
                               FROM cad_subcategories 
                               WHERE category_id = @categoryId 
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
            const string sql = @"
                               SELECT 
                                   id,
                                   category_id,
                                   name,
                                   display_name,
                                   parent_id,
                                   sort_order,
                                   created_at,
                                   updated_at
                               FROM cad_subcategories 
                               WHERE parent_id = @parentId 
                               ORDER BY sort_order";

            using var connection = new MySqlConnection(_connectionString);
            var subcategories = await connection.QueryAsync<CadSubcategory>(sql, new { parentId });
            return subcategories.AsList();
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
        /// 根据子分类ID获取图元
        /// </summary>
        public async Task<List<CadGraphic>> GetCadGraphicsBySubcategoryIdAsync(int subcategoryId)
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
            FROM cad_graphics 
            WHERE subcategory_id = @subcategoryId 
            ORDER BY file_name";

            using var connection = new MySqlConnection(_connectionString);
            var graphics = await connection.QueryAsync<CadGraphic>(sql, new { subcategoryId });
            return graphics.AsList();
        }
        /// <summary>
        /// 根据ID获取图元属性
        /// </summary>
        public async Task<CadGraphicAttribute> GetCadGraphicAttributeByGraphicIdAsync(int graphicId)
        {
            const string sql = @"
            SELECT 
                id,
                graphic_id,
                layer_name,
                color_index,
                angle,
                width,
                height,
                length
            FROM cad_graphic_attributes 
            WHERE graphic_id = @graphicId";

            using var connection = new MySqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<CadGraphicAttribute>(sql, new { graphicId });
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
        #endregion





    }

}
