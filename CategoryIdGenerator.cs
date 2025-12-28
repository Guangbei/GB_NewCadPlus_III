using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 生成分类/子分类 ID 的辅助器（按项目已有编码规则生成，尽量保证唯一且向后兼容）
    /// 规则（保守）：
    /// - 主分类（main）使用 4 位步进（1000,2000,...）：取当前最大主分类 id，+1000；若无则返回 1000。
    /// - 子分类（subcategory）在父 id 的基础上分配区域：base = parentId * 10（确保 >=10000），在 [base, base*10) 区间内查找最大 id 并 +1。
    ///   这样能兼容 parentId 为主分类（如 1000 -> 子区间从 10000 起）或为子分类（继续向下扩展）。
    /// 注意：此生成器仅负责在应用层产生候选 id，数据库唯一性由业务保证（并发场景建议在 DB 侧做校验/重试）。
    /// </summary>
    public static class CategoryIdGenerator
    {
        /// <summary>
        /// 生成主分类 ID（例如：1000,2000,3000...）
        /// </summary>
        public static async Task<int> GenerateMainCategoryIdAsync(DatabaseManager dm)
        {
            if (dm == null) throw new ArgumentNullException(nameof(dm));

            const string sql = @"
                SELECT COALESCE(MAX(id), 0) FROM cad_categories
                WHERE id >= 1000 AND id < 10000";
            try
            {
                using var conn = dm.GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                var maxId = await conn.ExecuteScalarAsync<int>(sql).ConfigureAwait(false);
                if (maxId == 0)
                    return 1000;
                // 以 1000 为步长递增
                return maxId + 1000;
            }
            catch (Exception)
            {
                // 出错时退回默认主分类 ID
                return 1000;
            }
        }

        /// <summary>
        /// 生成子分类 ID（在父ID对应的区间内分配，尽量避免冲突）
        /// </summary>
        public static async Task<int> GenerateSubcategoryIdAsync(DatabaseManager dm, int parentId)
        {
            if (dm == null) throw new ArgumentNullException(nameof(dm));
            if (parentId <= 0) throw new ArgumentException("parentId 必须大于 0", nameof(parentId));

            try
            {
                // base 确保从 10000 起（如果 parentId 是 1000，则 base=1000*10=10000）
                long baseRange = (long)parentId * 10L;
                long upperRange = baseRange * 10L; // 保留一层十倍的空间

                using var conn = dm.GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                // 找到该区间内的最大 id
                const string sql = @"
                    SELECT COALESCE(MAX(id), 0) FROM cad_subcategories
                    WHERE id >= @Base AND id < @Upper";
                var maxId = await conn.ExecuteScalarAsync<long>(sql, new { Base = baseRange, Upper = upperRange }).ConfigureAwait(false);

                if (maxId == 0)
                {
                    // 区间内还没有 id，返回 base+1
                    return (int)(baseRange + 1);
                }

                // 否则返回 max+1（注意防溢出）
                var next = maxId + 1;
                if (next >= upperRange)
                {
                    // 区间耗尽，退回使用全局自增策略（max overall +1）
                    const string globalSql = @"SELECT COALESCE(MAX(id), 10000) FROM cad_subcategories";
                    var globalMax = await conn.ExecuteScalarAsync<long>(globalSql).ConfigureAwait(false);
                    return (int)(globalMax + 1);
                }

                return (int)next;
            }
            catch (Exception)
            {
                // 发生异常时，尝试返回一个安全的 fallback id（时间戳截断）
                var fallback = 10000 + (int)(DateTime.UtcNow.Ticks % 1000000);
                return fallback;
            }
        }
    }
}
