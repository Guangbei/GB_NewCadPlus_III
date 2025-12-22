using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB_NewCadPlus_III.Helpers
{
    public static class FontsStyleHelper
    {
        // 新增：确保并应用 _TitleStyle 以及辅助函数（放到 Command 类内部、靠近 TextStyleAndLayerInfo 定义处）EnsureTitleTextStyle
        public static void EnsureTitleTextStyle(DBTrans tr)
        {
            // 创建或更新 _TitleStyle，使用 gbenor.shx / gbcbig.shx
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
                tr.TextStyleTable.Change("_TitleStyle", ttr =>
                {
                    ttr.FileName = "gbenor.shx";
                    ttr.BigFontFileName = "gbcbig.shx";
                    ttr.XScale = 1.0;
                    
                });
            }
        }

        /// <summary>
        /// 将 _TitleStyle 应用于 DBText（设置 TextStyleId 与 WidthFactor = 0.75）
        /// </summary>
        public static void ApplyTitleToDBText(DBTrans tr, DBText dbText)
        {
            try
            {
                if (dbText == null) return;
                EnsureTitleTextStyle(tr);
                dbText.TextStyleId = tr.TextStyleTable["_TitleStyle"];
                dbText.WidthFactor = 0.75;
                dbText.Height  = 3.5; // 强制更新以应用 WidthFactor
            }
            catch { }
        }

        /// <summary>
        /// 将 _TitleStyle 应用于 MText（设置 TextStyleId 并尽量调整宽度）
        /// </summary>
        public static void ApplyTitleToMText(DBTrans tr, MText mt)
        {
            try
            {
                if (mt == null) return;
                EnsureTitleTextStyle(tr);
                mt.TextStyleId = tr.TextStyleTable["_TitleStyle"];
                mt.Height = 3.5;
                // MText 无 WidthFactor：按请求将宽度尝试设置为高度的若干倍再乘以 0.75 以近似“宽度因子”
                if (mt.Width <= 0)
                    mt.Width = Math.Max(1.0, mt.TextHeight * 10.0) * 0.75;
                else
                    mt.Width = mt.Width * 0.75;
            }
            catch { }
        }
    }
}
