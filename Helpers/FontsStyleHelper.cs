using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III.Helpers
{
    /// <summary>
    /// 辅助类：处理字体样式
    /// </summary>
    public static class FontsStyleHelper
    {
        /// <summary>
        /// 新增：确保标题文本样式
        /// 确保并应用 _TitleStyle 以及辅助函数（放到 Command 类内部、靠近 TextStyleAndLayerInfo 定义处）EnsureTitleTextStyle
        /// </summary>
        /// <param name="tr"></param>
        public static void EnsureTitleTextStyle(DBTrans tr)
        {
            // 查找 _TitleStyle 如果不存在 _TitleStyle，则创建它
            if (!tr.TextStyleTable.Has("_TitleStyle"))
            {
                tr.TextStyleTable.Add("_TitleStyle", ttr =>
                {
                    ttr.FileName = "gbenor.shx";
                    ttr.BigFontFileName = "gbcbig.shx";
                    ttr.XScale = 1.0;
                });
            }
            else
            {
                /// 更新已有的 _TitleStyle
                tr.TextStyleTable.Change("_TitleStyle", ttr =>
                {
                    ttr.FileName = "gbenor.shx";
                    ttr.BigFontFileName = "gbcbig.shx";
                    ttr.XScale = 1.0;
                });
            }
        }

        /// <summary>
        /// 根据基本字高与图纸比例计算最终字高
        /// baseHeight: 比例为 1:1 时的字高（例如 3.5）
        /// scaleDenominator: 比例的分母，例如 1 表示 1:1，100 表示 1:100
        /// </summary>
        /// <param name="baseHeight"></param>
        /// <param name="scaleDenominator"></param>
        /// <returns></returns>
        public static double ComputeScaledHeight(double baseHeight, double scaleDenominator)
        {
            if (baseHeight <= 0) baseHeight = 3.5;
            if (scaleDenominator <= 0) scaleDenominator = 1.0;
            return baseHeight * scaleDenominator;
        }

        /// <summary>
        /// 将 _TitleStyle 应用于 DBText（设置 TextStyleId 与 WidthFactor = 0.75）
        /// scaleDenominator: 传入比例分母（1 表示 1:1，100 表示 1:100），默认 1
        /// </summary>
        public static void ApplyTitleToDBText(DBTrans tr, DBText dbText, double scaleDenominator = 1.0)
        {
            try
            {
                if (dbText == null) return;
                EnsureTitleTextStyle(tr);
                dbText.TextStyleId = tr.TextStyleTable["_TitleStyle"];
                dbText.WidthFactor = 0.75;
                // 根据比例设置字高：1:1 -> 3.5, 1:100 -> 350
                dbText.Height = ComputeScaledHeight(3.5, scaleDenominator);
            }
            catch { }
        }

        /// <summary>
        /// 将 _TitleStyle 应用于 MText（设置 TextStyleId 并尽量调整宽度）
        /// scaleDenominator: 传入比例分母（1 表示 1:1，100 表示 1:100），默认 1
        /// </summary>
        public static void ApplyTitleToMText(DBTrans tr, MText mt, double scaleDenominator = 1.0)
        {
            try
            {
                if (mt == null) return;
                EnsureTitleTextStyle(tr);
                mt.TextStyleId = tr.TextStyleTable["_TitleStyle"];
                // 按图纸比例设置高度
                mt.Height = ComputeScaledHeight(3.5, scaleDenominator);
                // MText 无 WidthFactor：按请求将宽度尝试设置为高度的若干倍再乘以 0.75 以近似“宽度因子”
                if (mt.Width <= 0)
                    mt.Width = Math.Max(1.0, mt.Height * 10.0) * 0.75;
                else
                    mt.Width = mt.Width * 0.75;
            }
            catch { }
        }
    }
}
