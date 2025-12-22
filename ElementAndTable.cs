
using Autodesk.AutoCAD.DatabaseServices;
using GB_NewCadPlus_III.Helpers;
using Mysqlx.Crud;
using OfficeOpenXml;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using AttributeCollection = Autodesk.AutoCAD.DatabaseServices.AttributeCollection;
using DataTable = System.Data.DataTable;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// 设备属性块信息类（用于存储CAD块中的设备信息）
    /// </summary>
    public class EquipmentInfo
    {
        // 设备名称
        public string Name { get; set; } = string.Empty;

        // 设备类型（如阀门、法兰等）
        public string Type { get; set; } = string.Empty;

        // 属性字典（中文属性名-值）
        public Dictionary<string, string> Attributes { get; set; }

        // 英文属性名对照（中文属性名-英文属性名）
        public Dictionary<string, string> EnglishNames { get; set; }

        // 相同设备的数量统计
        public int Count { get; set; }

        // 构造函数初始化字典和默认值
        public EquipmentInfo()
        {
            Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            EnglishNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Count = 1;  // 默认数量为1
        }
    }

    /// <summary>
    /// 插件初始化类
    /// </summary>
    public class PluginInitialization : IExtensionApplication
    {
        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Editor.WriteMessage("\n设备材料表生成器已加载。");
                doc.Editor.WriteMessage("\n使用命令: GenerateEquipmentTable - 生成设备材料表");
                doc.Editor.WriteMessage("\n使用命令: ExportTableToExcel - 导出表格到Excel");
            }
        }

        public void Terminate()
        {
            // 清理资源
        }
    }

    /// <summary>
    /// CAD设备表生成器主类
    /// </summary>
    public class EquipmentTableGenerator
    {
        /// <summary>
        /// 预定义的英文对照表
        /// </summary>
        private static readonly Dictionary<string, string> ChineseToEnglish = new Dictionary<string, string>
            {
                { "管子", "Pipe(m)" },
                { "阀门", "Valve(Pcs.)" },
                { "法兰", "Flange(Pcs.)" },
                { "垫片", "Gasket(Pcs.)" },
                { "螺栓", "Bolt(Pcs.)" },
                { "螺母", "Nut(Pcs.)" },
                { "名称", "Name" },
                { "介质名称", "Medium Name" },
                { "规格", "Specs." },
                { "材料", "Material" },
                { "数量", "Quan." },
                { "图号或标准号", "DWG.No./ STD.No." },
                { "功率", "Power" },
                { "容积", "Volume" },
                { "压力", "Pressure" },
                { "温度", "Temperature" },
                { "直径", "Diameter" },
                { "长度", "Length" },
                { "厚度", "Thickness" },
                { "重量", "Weight" },
                { "型号", "Model" },
                { "隔热隔声代号", "Code" },
                { "是否防腐", "Antisepsis" },
                { "操作压力", "MPaG" },
                { "备注", "Remark" }
            };

        /// <summary>
        /// 主命令：生成设备材料表（按类型拆分为多个表）arrowEntities
        /// </summary>
        /// <summary>
        /// 主命令：生成设备材料表（按类型拆分为多个表）
        /// 修复要点：
        /// - 不再在此处重复调用 GetSelection（避免需要第二次选择的问题）
        /// - 直接使用 SelectAndAnalyzeBlocks 内的选择逻辑
        /// - 每生成并插入一个表后立即刷新界面，确保用户能看到刚插入的表
        /// </summary>
        [CommandMethod("GenerateEquipmentTable")]
        public void GenerateEquipmentTable()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            try
            {
                // 不在此处开启长事务，SelectAndAnalyzeBlocks 自行打开短事务读取所需信息
                var equipments = SelectAndAnalyzeBlocks(ed, doc.Database);
                if (equipments == null || equipments.Count == 0)
                {
                    ed.WriteMessage("\n未找到可用的设备信息。");
                    return;
                }

                // 按 Type 分组：为每个设备类型生成独立表
                var groups = equipments.GroupBy(e => string.IsNullOrWhiteSpace(e.Type) ? "设备" : e.Type);
                foreach (var g in groups)
                {
                    var list = g.ToList();

                    // 每次生成并插入表在独立的短事务中完成（CreateEquipmentTableWithType 自行创建事务）
                    CreateEquipmentTableWithType(doc.Database, list, g.Key);

                    // 强制重生成并刷新显示，确保新插入的表立即可见
                    try
                    {
                        // 强制重生成模型空间显示
                        ed.Regen();
                        // 有时还需强制更新屏幕
                        Application.UpdateScreen();
                    }
                    catch
                    {
                        // 忽略刷新失败
                    }

                    ed.WriteMessage($"\n已为类型 '{g.Key}' 生成表，包含 {list.Count} 条汇总项。");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n生成设备表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// “生成(工艺)管道表”：GeneratePipeTableFromSelection GenerateEquipmentTable
        /// - 交互式选择实体
        /// - 提取属性（优先使用属性中包含“长度”的字段作为长度）
        /// - 按属性组合或几何尺寸分组并累加长度
        /// - 最终以表格形式（沿用本类表格样式）生成一张或多张“管道”表（如果选择中包含多个不同管道规格，会生成一张表列出所有分组）
        /// 说明：此方法同时可被命令调用或者由 UI 层调用（无需 UI 层再自行实现分组/样式）
        /// </summary>
        [CommandMethod("GeneratePipeTableFromSelection")]
        public void GeneratePipeTableFromSelection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            AutoCadHelper.ExecuteInDocumentTransaction((d, tr) =>
            {
                var ed = d.Editor;
                try
                {
                    ed.WriteMessage("\n开始生成管道表：请选择要统计的管道图元（完成选择回车）.");
                    var pso = new PromptSelectionOptions { MessageForAdding = "\n请选择要统计的管道图元：" };
                    var psr = ed.GetSelection(pso);
                    if (psr.Status != PromptStatus.OK || psr.Value == null)
                    {
                        ed.WriteMessage("\n未选择实体或已取消。");
                        return;
                    }

                    var selIds = psr.Value.GetObjectIds();
                    if (selIds == null || selIds.Length == 0)
                    {
                        ed.WriteMessage("\n未选择任何实体。");
                        return;
                    }

                    // 单位换算（默认毫米->米 1000）
                    double unitToMeters = 1000.0;

                    // 关键字集合
                    string[] pipeNoKeys = new[] { "管段号", "管段编号", "Pipeline No", "Pipeline", "Pipe No", "No" };
                    string[] startKeys = new[] { "起点", "始点", "From" };
                    string[] endKeys = new[] { "终点", "止点", "To" };

                    // 局部 helper：先精确再包含匹配，返回第一个匹配值
                    string FindFirstAttrValueLocal(Dictionary<string, string>? attrs, string[] candidates)
                    {
                        if (attrs == null) return string.Empty;
                        foreach (var c in candidates)
                        {
                            if (attrs.TryGetValue(c, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
                        }
                        foreach (var kv in attrs)
                        {
                            if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                            foreach (var c in candidates)
                            {
                                if (kv.Key.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0 && !string.IsNullOrWhiteSpace(kv.Value))
                                    return kv.Value;
                            }
                        }
                        return string.Empty;
                    }

                    // 逐实体解析：每个选中实体对应表格中的一行（不做按管段号合并）
                    var perPipeList = new List<EquipmentInfo>();
                    int seqIndex = 0;

                    foreach (var id in selIds)
                    {
                        seqIndex++;
                        try
                        {
                            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent == null) continue;

                            // 尝试获取属性
                            var attrMap = GetEntityAttributeMap(tr, ent);

                            // 计算长度：优先属性中包含“长度”的字段；其次根据实体类型计算几何长度；最后使用包围盒X长度
                            double length_m = double.NaN;
                            if (attrMap != null)
                            {
                                foreach (var k in attrMap.Keys)
                                {
                                    if (!string.IsNullOrWhiteSpace(k) && k.IndexOf("长度", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        var parsed = ParseLengthValueFromAttribute(attrMap[k]);
                                        if (!double.IsNaN(parsed) && parsed > 0.0)
                                        {
                                            length_m = parsed;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (double.IsNaN(length_m))
                            {
                                // 根据实体类型计算长度
                                if (ent is Line lineEnt)
                                {
                                    length_m = lineEnt.Length / unitToMeters;
                                }
                                else if (ent is Polyline plEnt)
                                {
                                    length_m = plEnt.Length / unitToMeters;
                                }
                                else if (ent is BlockReference brEnt)
                                {
                                    try
                                    {
                                        double l = GetLength(brEnt);
                                        if (!double.IsNaN(l) && l > 0.0) length_m = l;
                                    }
                                    catch { }
                                }
                                // 回退：包围盒X方向尺寸
                                if (double.IsNaN(length_m))
                                {
                                    try
                                    {
                                        var ext = ent.GeometricExtents;
                                        double sizeX = Math.Abs(ext.MaxPoint.X - ext.MinPoint.X);
                                        length_m = sizeX / unitToMeters;
                                    }
                                    catch
                                    {
                                        length_m = 0.0;
                                    }
                                }
                            }

                            // 起点/终点：优先属性，否则按实体几何获取
                            string startStr = FindFirstAttrValueLocal(attrMap, startKeys);
                            string endStr = FindFirstAttrValueLocal(attrMap, endKeys);

                            if (string.IsNullOrWhiteSpace(startStr) || string.IsNullOrWhiteSpace(endStr))
                            {
                                if (ent is Line lineEnt2)
                                {
                                    if (string.IsNullOrWhiteSpace(startStr))
                                        startStr = $"X={lineEnt2.StartPoint.X:F3},Y={lineEnt2.StartPoint.Y:F3}";
                                    if (string.IsNullOrWhiteSpace(endStr))
                                        endStr = $"X={lineEnt2.EndPoint.X:F3},Y={lineEnt2.EndPoint.Y:F3}";
                                }
                                else if (ent is Polyline plEnt2)
                                {
                                    try
                                    {
                                        var p0 = plEnt2.GetPoint3dAt(0);
                                        var pN = plEnt2.GetPoint3dAt(plEnt2.NumberOfVertices - 1);
                                        if (string.IsNullOrWhiteSpace(startStr))
                                            startStr = $"X={p0.X:F3},Y={p0.Y:F3}";
                                        if (string.IsNullOrWhiteSpace(endStr))
                                            endStr = $"X={pN.X:F3},Y={pN.Y:F3}";
                                    }
                                    catch { }
                                }
                                else if (ent is BlockReference brEnt2)
                                {
                                    try
                                    {
                                        var (s, e) = GetEndPoints(brEnt2);
                                        if (string.IsNullOrWhiteSpace(startStr))
                                            startStr = $"X={s.X:F3},Y={s.Y:F3}";
                                        if (string.IsNullOrWhiteSpace(endStr))
                                            endStr = $"X={e.X:F3},Y={e.Y:F3}";
                                    }
                                    catch { }
                                }

                                // 最终回退到包围盒的Min/Max
                                if (string.IsNullOrWhiteSpace(startStr) || string.IsNullOrWhiteSpace(endStr))
                                {
                                    try
                                    {
                                        var ext = ent.GeometricExtents;
                                        if (string.IsNullOrWhiteSpace(startStr))
                                            startStr = $"X={ext.MinPoint.X:F3},Y={ext.MinPoint.Y:F3}";
                                        if (string.IsNullOrWhiteSpace(endStr))
                                            endStr = $"X={ext.MaxPoint.X:F3},Y={ext.MaxPoint.Y:F3}";
                                    }
                                    catch
                                    {
                                        if (string.IsNullOrWhiteSpace(startStr)) startStr = "N/A";
                                        if (string.IsNullOrWhiteSpace(endStr)) endStr = "N/A";
                                    }
                                }
                            }

                            // 管段号：先从属性中找
                            string pipeNo = FindFirstAttrValueLocal(attrMap, pipeNoKeys);
                            if (string.IsNullOrWhiteSpace(pipeNo))
                            {
                                if (ent is BlockReference brEnt3)
                                {
                                    try
                                    {
                                        var btr = tr.GetObject(brEnt3.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                                        if (btr != null)
                                        {
                                            var nm = btr.Name ?? string.Empty;
                                            var m = Regex.Match(nm, @"\d+");
                                            if (m.Success) pipeNo = m.Value;
                                        }
                                    }
                                    catch { }
                                }
                            }

                            // 构建 EquipmentInfo（每条选中管道一条记录）
                            var info = new EquipmentInfo
                            {
                                Name = $"PIPE_{seqIndex}",
                                Type = "管道"
                            };

                            // 复制所有原始属性（保留）
                            if (attrMap != null)
                            {
                                foreach (var kv in attrMap) info.Attributes[kv.Key] = kv.Value;
                            }

                            // 覆盖/设置我们需要的标准字段
                            if (!string.IsNullOrWhiteSpace(pipeNo)) info.Attributes["管段号"] = pipeNo;
                            info.Attributes["起点"] = startStr;
                            info.Attributes["终点"] = endStr;
                            info.Attributes["长度(m)"] = length_m.ToString("F3");
                            // 保留累计长度字段与长度相同（每条一行，累计长度同长度）
                            info.Attributes["累计长度(m)"] = length_m.ToString("F3");
                            // 如果存在旧键 "介质" 而没有 "介质名称"，把值迁移到 "介质名称"
                            if (info.Attributes.TryGetValue("介质", out var medVal) && !info.Attributes.ContainsKey("介质名称"))
                            {
                                info.Attributes["介质名称"] = medVal;
                            }
                            perPipeList.Add(info);
                        }
                        catch (System.Exception exEnt)
                        {
                            ed.WriteMessage($"\n处理实体 {id} 时出错: {exEnt.Message}");
                        }
                    }

                    if (perPipeList.Count == 0)
                    {
                        ed.WriteMessage("\n未生成任何管道记录。");
                        return;
                    }

                    // 对列表排序：按管段号数字部分升序；管段号为空的按选择顺序放后
                    int ExtractPipeNoNumber(string s)
                    {
                        if (string.IsNullOrWhiteSpace(s)) return int.MaxValue;
                        var m = Regex.Match(s, @"\d+");
                        if (m.Success && int.TryParse(m.Value, out var v)) return v;
                        return int.MaxValue;
                    }

                    var finalList = perPipeList
                        .Select((e, idx) => new { Item = e, Orig = idx })
                        .OrderBy(x =>
                        {
                            if (x.Item.Attributes.TryGetValue("管段号", out var pn) && !string.IsNullOrWhiteSpace(pn))
                                return (ExtractPipeNoNumber(pn), 0, x.Orig);
                            return (int.MaxValue, 1, x.Orig);
                        })
                        .Select(x => x.Item)
                        .ToList();

                    // 注意：材料值将由“示例属性编辑”对话框提供，移除单独的字符串提示
                    // 生成表格：每个选中管道生成一行（相同管段号不会合并）
                    CreateEquipmentTableWithType(d.Database, finalList, "管道明细");
                    ed.WriteMessage($"\n管道表已生成，共 {finalList.Count} 条记录（每选中一条管道一行）。");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n生成管道表时发生错误: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 迁移：为 UI 按钮 [插入管道表] 提供命令入口（已迁移到此类）
        /// 简单实现：直接调用 GeneratePipeTableFromSelection，使用统一逻辑（包含选择、分组、插入点提示）。
        /// 如果 WPF 需直接调用此方法，请在 WPF 中触发此命令或直接调用 GeneratePipeTableFromSelection。
        /// </summary>
        [CommandMethod("InsertPipeTable")]
        public void InsertPipeTable()
        {
            // 迁移后直接复用 GeneratePipeTableFromSelection 的逻辑
            GeneratePipeTableFromSelection();
        }

        /// <summary>
        /// 从管道标题中提取管道号，例如 "350-AR-1002-1.0G11" -> "AR-1002"
        /// 优先返回字母-数字形式的片段，若无法匹配返回 empty
        /// </summary>
        private string ExtractPipeCodeFromTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            title = title!.Trim();

            // 常见形式：以字母开头 + '-' + 数字，例如 AR-1002
            var m = Regex.Match(title, @"[A-Za-z]+-\d+");
            if (m.Success) return m.Value;

            // 保险：也尝试在 -...- 中间提取类似模式
            var m2 = Regex.Match(title, @"-(?<code>[A-Za-z]+-\d+)-");
            if (m2.Success && m2.Groups["code"].Success) return m2.Groups["code"].Value;

            // 若仍找不到，可以尝试更宽松的匹配（包含数字在后）
            var m3 = Regex.Match(title, @"[A-Za-z0-9]+-[0-9A-Za-z]+");
            if (m3.Success) return m3.Value;

            return string.Empty;
        }

        /// <summary>
        /// ----------- 辅助：从实体中读取属性（AttributeReference / Xrecord / XData） ------------
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="ent"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetEntityAttributeMap(Transaction tr, Entity ent)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (ent == null) return map;

                // 1) AttributeReference（块参照）
                if (ent is BlockReference br)
                {
                    try
                    {
                        var attCol = br.AttributeCollection;
                        foreach (ObjectId attId in attCol)
                        {
                            try
                            {
                                var ar = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                if (ar != null)
                                {
                                    var tag = (ar.Tag ?? string.Empty).Trim();
                                    var val = (ar.TextString ?? string.Empty).Trim();
                                    if (!string.IsNullOrEmpty(tag) && !map.ContainsKey(tag)) map[tag] = val;
                                }
                            }
                            catch { /* 忽略单个属性读取失败 */ }
                        }
                    }
                    catch { /* 忽略 */ }
                }

                // 2) ExtensionDictionary 的 Xrecord
                try
                {
                    if (ent.ExtensionDictionary != ObjectId.Null)
                    {
                        var extDict = tr.GetObject(ent.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                        if (extDict != null)
                        {
                            foreach (var entry in extDict)
                            {
                                try
                                {
                                    var xrec = tr.GetObject(entry.Value, OpenMode.ForRead) as Xrecord;
                                    if (xrec != null && xrec.Data != null)
                                    {
                                        var vals = xrec.Data.Cast<TypedValue>().Select(tv => tv.Value?.ToString() ?? "").ToArray();
                                        var key = entry.Key ?? string.Empty;
                                        var value = string.Join("|", vals);
                                        if (!map.ContainsKey(key)) map[key] = value;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { /* 忽略 */ }

                // 3) RegApp XData
                try
                {
                    var db = ent.Database;
                    var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
                    foreach (ObjectId appId in rat)
                    {
                        try
                        {
                            var app = tr.GetObject(appId, OpenMode.ForRead) as RegAppTableRecord;
                            if (app == null) continue;
                            var appName = app.Name;
                            var rb = ent.GetXDataForApplication(appName);
                            if (rb != null)
                            {
                                var vals = rb.Cast<TypedValue>().Select(tv => tv.Value?.ToString() ?? "").ToArray();
                                var key = $"XDATA:{appName}";
                                var value = string.Join("|", vals);
                                if (!map.ContainsKey(key)) map[key] = value;
                            }
                        }
                        catch { }
                    }
                }
                catch { /* 忽略 */ }
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nGetEntityAttributeMap 异常: {ex.Message}");
            }
            return map;
        }

        /// <summary>
        /// ----------- 辅助：从属性字符串中解析长度（返回米，支持 mm/m） ------------
        /// </summary>
        /// <param name="rawValue"></param>
        /// <returns></returns>
        private double ParseLengthValueFromAttribute(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return double.NaN;
            try
            {
                var s = rawValue.Trim().ToLowerInvariant();
                bool containsMm = s.Contains("mm") || s.Contains("毫米");
                bool containsM = (s.Contains("m") && !containsMm) || s.Contains("米");

                var m = Regex.Match(s, @"[-+]?[0-9]*\.?[0-9]+");
                if (!m.Success) return double.NaN;
                if (!double.TryParse(m.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
                    return double.NaN;

                if (containsMm) return value / 1000.0;
                if (containsM) return value;

                // 无单位启发式：若值 >=1000 视为 mm
                if (value >= 1000.0) return value / 1000.0;
                return value;
            }
            catch { return double.NaN; }
        }

        /// <summary>
        /// "生成(管道)列表格"
        /// 内部：把给定管道列表以本类样式生成表格（表标题包含类型）
        /// 修复要点：
        /// - 插入表前使用单次 GetPoint 提示用户指定插入位置（按用户期望：选完实体后按空格/右键结束选择即可在此提示插入）
        /// - 插入并提交后立即调用 Editor.UpdateScreen() 刷新显示，允许用户在插入下一张表前看到已插入的表
        /// </summary>
        /// <param name="db">  </param>
        /// <param name="equipmentList"></param>
        /// <param name="typeTitle"></param>
        /// <summary>
        private void CreateEquipmentTableWithType(Database db, List<EquipmentInfo> equipmentList, string typeTitle)
        {
            if (equipmentList == null || equipmentList.Count == 0) return;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            // 插入点提示
            PromptPointOptions ppo = new PromptPointOptions($"\n'{typeTitle}' 表：指定插入位置（点击或输入点）：");
            ppo.AllowNone = false;
            var ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n未指定插入位置，跳过该类型表的插入。");
                return;
            }
            Point3d insertPosition = ppr.Value;

            // 基础固定列数（索引 0..9）
            const int baseFixedCols = 10;
            string[] pipeNoKeys = new[] { "管段号", "管段编号", "Pipeline No", "Pipeline", "Pipe No", "No" };
            string[] startKeys = new[] { "起点", "始点", "From" };
            string[] endKeys = new[] { "终点", "止点", "To" };

            // 迁移 "介质" -> "介质名称"
            foreach (var e in equipmentList)
            {
                if (e.Attributes == null) continue;
                if (e.Attributes.TryGetValue("介质", out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    if (!e.Attributes.ContainsKey("介质名称") || string.IsNullOrWhiteSpace(e.Attributes["介质名称"]))
                        e.Attributes["介质名称"] = val;
                }
                if (!e.Attributes.ContainsKey("介质名称"))
                {
                    if (e.Attributes.TryGetValue("Medium", out var mv) && !string.IsNullOrWhiteSpace(mv)) e.Attributes["介质名称"] = mv;
                    else if (e.Attributes.TryGetValue("Medium Name", out var mn) && !string.IsNullOrWhiteSpace(mn)) e.Attributes["介质名称"] = mn;
                }
            }

            // 收集所有属性键
            var allAttrKeysSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in equipmentList)
            {
                if (e.Attributes == null) continue;
                foreach (var k in e.Attributes.Keys)
                {
                    if (string.IsNullOrWhiteSpace(k)) continue;
                    allAttrKeysSet.Add(k);
                }
            }

            // 移除会被合并到固定列的键（按包含关系移除，避免 "操作温度T(℃)" 等变体生成动态列）
            var reservedKeySubstrings = new[]
            {
                 "管道标题","管段号","管道号","起点","始点","终点","止点","管道等级",
                 "介质","介质名称","Medium","Medium Name","操作温度","操作压力",
                 "隔热隔声代号","是否防腐","Length","长度"
             };
            foreach (var key in allAttrKeysSet.ToList())
            {
                if (reservedKeySubstrings.Any(s => !string.IsNullOrWhiteSpace(s) &&
                    key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    allAttrKeysSet.Remove(key);
                }
            }

            // 合并/映射标准号相关同义词，避免产生单独的 "标准号" 列（按包含关系移除）
            var standardSynonyms = new[] { "标准号", "图号", "DWG.No.", "STD.No.", "标准" };
            foreach (var key in allAttrKeysSet.ToList())
            {
                if (standardSynonyms.Any(s => !string.IsNullOrWhiteSpace(s) &&
                    key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    allAttrKeysSet.Remove(key);
                }
            }

            // 管道组首选字段（保持顺序），后续任何未匹配字段都追加到管道组
            var pipeGroupPreferred = new[]
            {
                "名称", "材料", "图号或标准号", "数量", "泵前/后"
            };

            var pipeGroupColumns = new List<string>();
            // 先按首选字段加入存在的项（保持顺序）
            foreach (var pk in pipeGroupPreferred)
            {
                if (allAttrKeysSet.Contains(pk))
                {
                    pipeGroupColumns.Add(pk);
                    allAttrKeysSet.Remove(pk);
                }
            }
            // 确保关键列存在以便填充（名称/材料/图号/数量/泵前/后）
            foreach (var pk in pipeGroupPreferred)
            {
                if (!pipeGroupColumns.Contains(pk))
                    pipeGroupColumns.Add(pk);
            }

            // 剩余属性全部追加到管道组（用户要求：未匹配的列也放到管道组下）
            var remainingAttrs = allAttrKeysSet.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            // 剔除任何会引起重复的同义词（保守处理）
            remainingAttrs = remainingAttrs.Where(k => !pipeGroupPreferred.Any(pk => string.Equals(pk, k, StringComparison.OrdinalIgnoreCase))).ToList();
            pipeGroupColumns.AddRange(remainingAttrs);

            int pipeGroupCount = pipeGroupColumns.Count;

            // 其余动态列（在管道组之后）-- 这里通常为0，因为我们把未匹配属性都放到管道组中
            var restKeys = new List<string>(); // 保留接口，通常为空

            // 排序设备（依据管段号数字部分）
            string FindFirstAttrValue(Dictionary<string, string>? attrs, string[] candidates)
            {
                if (attrs == null) return string.Empty;
                foreach (var c in candidates)
                {
                    if (attrs.TryGetValue(c, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
                }
                foreach (var kv in attrs)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    foreach (var c in candidates)
                    {
                        if (kv.Key.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0 && !string.IsNullOrWhiteSpace(kv.Value))
                            return kv.Value;
                    }
                }
                return string.Empty;
            }
            int ParsePipeNumberNumeric(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return int.MaxValue;
                var m = Regex.Match(s!, @"\d+");
                if (m.Success && int.TryParse(m.Value, out int v)) return v;
                return int.MaxValue;
            }

            var sortedEquipmentList = equipmentList
                .Select((e, idx) => new { Item = e, OrigIndex = idx })
                .OrderBy(x =>
                {
                    var txt = FindFirstAttrValue(x.Item.Attributes, pipeNoKeys);
                    int num = ParsePipeNumberNumeric(txt);
                    return (num, x.OrigIndex);
                })
                .Select(x => x.Item)
                .ToList();

            // 创建表格并填充
            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    int fixedCols = baseFixedCols + pipeGroupCount; // 固定列包括管道组扩展
                    int dynamicCols = restKeys.Count;
                    int totalCols = fixedCols + dynamicCols;
                    int dataRows = sortedEquipmentList.Count;
                    int rows = 1 + 3 + dataRows; // 标题 + 两层表头 + 数据
                    var table = new Autodesk.AutoCAD.DatabaseServices.Table();
                    table.SetSize(rows, totalCols);
                    table.Position = insertPosition;

                    SetTableStyle(db, table, tr);

                    // 标题
                    table.MergeCells(CellRange.Create(table, 0, 0, 0, table.Columns.Count - 1));
                    table.Cells[0, 0].TextString = $"{typeTitle} - 设备材料明细表";
                    table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;

                    // 固定表头：传入实际 pipeGroupCount
                    FillFixedHeaders(table, pipeGroupCount);

                    // 将管道组子列名写入（覆盖 FillFixedHeaders 中空的额外列名）
                    int pipeStart = baseFixedCols;
                    for (int i = 0; i < pipeGroupColumns.Count && (pipeStart + i) < table.Columns.Count; i++)
                    {
                        string header = pipeGroupColumns[i];
                        string english = ChineseToEnglish.ContainsKey(header) ? ChineseToEnglish[header] : header;
                        table.Cells[2, pipeStart + i].TextString = header + (english == header ? "" : "\n" + english);
                    }

                    // 动态表头（从 fixedCols 开始）: restKeys 列（通常为空）
                    for (int i = 0; i < restKeys.Count; i++)
                    {
                        var key = restKeys[i];
                        int col = fixedCols + i;
                        table.Cells[1, col].TextString = key;
                        table.Cells[2, col].TextString = (ChineseToEnglish.ContainsKey(key) ? ChineseToEnglish[key] : key);
                    }

                    // 填充数据行
                    int dataStartRow = 3;
                    for (int r = 0; r < sortedEquipmentList.Count; r++)
                    {
                        var item = sortedEquipmentList[r];
                        int rowIndex = dataStartRow + r;

                        // 0: 管道标题
                        string title = null;
                        if (item.Attributes != null && item.Attributes.TryGetValue("管道标题", out var tv) && !string.IsNullOrWhiteSpace(tv))
                            title = tv;
                        else
                        {
                            var nm = item.Name ?? string.Empty;
                            int pos = nm.LastIndexOf('_');
                            title = pos >= 0 && pos < nm.Length - 1 ? nm.Substring(pos + 1) : nm;
                        }
                        table.Cells[rowIndex, 0].TextString = title ?? string.Empty;

                        // 1: 管段号 — 优先使用属性中明确的“管段号”等字段；若无，再尝试从标题提取，最后回退兼容键
                        string pipeNoVal = string.Empty;
                        if (item.Attributes != null)
                        {
                            pipeNoVal = FindFirstAttrValue(item.Attributes, pipeNoKeys);
                        }

                        // 兼容性：若属性中未提供，则尝试从管道标题中提取编码/号段（使用已有提取函数）
                        if (string.IsNullOrWhiteSpace(pipeNoVal) && !string.IsNullOrWhiteSpace(title))
                        {
                            // 尝试提取常见的片段（如 AR-1002 或数字序列）
                            pipeNoVal = ExtractPipeCodeFromTitle(title);
                            // 如果仍为空，尝试提取最后的数字序列
                            if (string.IsNullOrWhiteSpace(pipeNoVal))
                            {
                                var m = Regex.Match(title, @"\d+");
                                if (m.Success) pipeNoVal = m.Value;
                            }
                        }

                        // 最后回退：查找更多同义键（防止遗漏）
                        if (string.IsNullOrWhiteSpace(pipeNoVal) && item.Attributes != null)
                        {
                            var fallbackKeys = new[] { "管段编号", "Pipeline No", "Pipeline", "Pipe No", "No" };
                            foreach (var k in fallbackKeys)
                            {
                                if (item.Attributes.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                                {
                                    pipeNoVal = v;
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(pipeNoVal))
                            table.Cells[rowIndex, 1].TextString = pipeNoVal;

                        // 2/3 起点/终点
                        var startVal = FindFirstAttrValue(item.Attributes, startKeys);
                        if (!string.IsNullOrWhiteSpace(startVal)) table.Cells[rowIndex, 2].TextString = startVal;
                        var endVal = FindFirstAttrValue(item.Attributes, endKeys);
                        if (!string.IsNullOrWhiteSpace(endVal)) table.Cells[rowIndex, 3].TextString = endVal;

                        // 4 管道等级 - 优先使用属性，否则从管道标题中提取并回写到 Attributes
                        string pipeClass = string.Empty;
                        if (item.Attributes != null && item.Attributes.TryGetValue("管道等级", out var cls) && !string.IsNullOrWhiteSpace(cls))
                        {
                            pipeClass = cls;
                        }
                        else
                        {
                            // 从 title 中提取，例如 "350-AR-1002-1.0G11" -> "1.0G11"
                            pipeClass = ExtractPipeClassFromTitle(title);
                            // 回退匹配一些可能的同义键
                            if (string.IsNullOrWhiteSpace(pipeClass) && item.Attributes != null)
                            {
                                var fallbackKeys = new[] { "等级", "Class", "管级", "级别" };
                                foreach (var fk in fallbackKeys)
                                {
                                    if (item.Attributes.TryGetValue(fk, out var fv) && !string.IsNullOrWhiteSpace(fv))
                                    {
                                        pipeClass = fv;
                                        break;
                                    }
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(pipeClass) && item.Attributes != null)
                                item.Attributes["管道等级"] = pipeClass;
                        }
                        if (!string.IsNullOrWhiteSpace(pipeClass))
                            table.Cells[rowIndex, 4].TextString = pipeClass;

                        // 5-7 设计条件
                        var mediumVal = FindFirstAttrValue(item.Attributes, new[] { "介质名称", "介质", "Medium Name" });
                        if (!string.IsNullOrWhiteSpace(mediumVal)) table.Cells[rowIndex, 5].TextString = mediumVal;

                        // --- 把属性中各种变体的操作温度都映射到固定列 6（保证不会被当成动态列单独生成） ---
                        string opTemp = string.Empty;
                        if (item.Attributes != null)
                        {
                            var tempKey = item.Attributes.Keys.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k) &&
                                (k.IndexOf("操作温度", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 k.IndexOf("T(", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 k.IndexOf("℃", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 k.IndexOf("°C", StringComparison.OrdinalIgnoreCase) >= 0));
                            if (!string.IsNullOrWhiteSpace(tempKey))
                                opTemp = item.Attributes[tempKey];
                        }
                        if (!string.IsNullOrWhiteSpace(opTemp))
                            table.Cells[rowIndex, 6].TextString = opTemp;
                        else if (item.Attributes != null && item.Attributes.TryGetValue("操作温度", out var tval) && !string.IsNullOrWhiteSpace(tval))
                            table.Cells[rowIndex, 6].TextString = tval;

                        if (item.Attributes != null && item.Attributes.TryGetValue("操作压力", out var pval) && !string.IsNullOrWhiteSpace(pval))
                            table.Cells[rowIndex, 7].TextString = pval;

                        // 8-9 隔热及防腐
                        if (item.Attributes != null && item.Attributes.TryGetValue("隔热隔声代号", out var code) && !string.IsNullOrWhiteSpace(code))
                            table.Cells[rowIndex, 8].TextString = code;
                        if (item.Attributes != null && item.Attributes.TryGetValue("是否防腐", out var anti) && !string.IsNullOrWhiteSpace(anti))
                            table.Cells[rowIndex, 9].TextString = anti;

                        // 管道组列写入：根据 pipeGroupColumns 列表依次匹配属性值
                        for (int i = 0; i < pipeGroupColumns.Count; i++)
                        {
                            int col = baseFixedCols + i;
                            var headerKey = pipeGroupColumns[i];

                            // 特殊合并规则：核算流速 合并所有包含 流速/流量/flow 的属性
                            if (string.Equals(headerKey, "核算流速", StringComparison.OrdinalIgnoreCase))
                            {
                                var flows = new List<string>();
                                if (item.Attributes != null)
                                {
                                    foreach (var kv in item.Attributes)
                                    {
                                        if (kv.Key.IndexOf("流速", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            kv.Key.IndexOf("流量", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            kv.Key.IndexOf("flow", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            if (!string.IsNullOrWhiteSpace(kv.Value) && !flows.Contains(kv.Value)) flows.Add(kv.Value);
                                        }
                                    }
                                }
                                if (item.Attributes != null && item.Attributes.TryGetValue("核算流速", out var hv) && !string.IsNullOrWhiteSpace(hv) && !flows.Contains(hv))
                                    flows.Insert(0, hv);
                                if (flows.Count > 0) table.Cells[rowIndex, col].TextString = string.Join(", ", flows);
                                continue;
                            }

                            // 数量优先用属性，否则用 equipment.Count
                            if (string.Equals(headerKey, "数量", StringComparison.OrdinalIgnoreCase))
                            {
                                if (item.Attributes != null && item.Attributes.TryGetValue("数量", out var qv) && !string.IsNullOrWhiteSpace(qv))
                                    table.Cells[rowIndex, col].TextString = qv;
                                else
                                    table.Cells[rowIndex, col].TextString = item.Count.ToString();
                                continue;
                            }

                            // 特殊：图号或标准号 列优先使用属性键 "标准号" / "图号" / "DWG.No." / "STD.No."
                            if (string.Equals(headerKey, "图号或标准号", StringComparison.OrdinalIgnoreCase))
                            {
                                string matched = string.Empty;
                                if (item.Attributes != null)
                                {
                                    // 精确优先
                                    if (item.Attributes.TryGetValue("标准号", out var stdVal) && !string.IsNullOrWhiteSpace(stdVal)) matched = stdVal;
                                    else if (item.Attributes.TryGetValue("图号", out var dwgVal) && !string.IsNullOrWhiteSpace(dwgVal)) matched = dwgVal;
                                    else if (item.Attributes.TryGetValue("DWG.No.", out var dwgDot) && !string.IsNullOrWhiteSpace(dwgDot)) matched = dwgDot;
                                    else if (item.Attributes.TryGetValue("STD.No.", out var stdDot) && !string.IsNullOrWhiteSpace(stdDot)) matched = stdDot;

                                    // 宽松匹配：属性名包含 "标准" / "图号" / "DWG" / "STD"
                                    if (string.IsNullOrWhiteSpace(matched))
                                    {
                                        foreach (var kv in item.Attributes)
                                        {
                                            if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
                                            var key = kv.Key;
                                            if (key.IndexOf("标准", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                key.IndexOf("图号", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                key.IndexOf("DWG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                key.IndexOf("STD", StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                matched = kv.Value;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(matched))
                                    table.Cells[rowIndex, col].TextString = matched;

                                continue;
                            }

                            // 普通键：精确或包含匹配（回退）
                            string matchedValue = string.Empty;
                            if (item.Attributes != null)
                            {
                                // 精确
                                if (item.Attributes.TryGetValue(headerKey, out var dv) && !string.IsNullOrWhiteSpace(dv))
                                    matchedValue = dv;
                                else
                                {
                                    // 包含匹配
                                    foreach (var kv in item.Attributes)
                                    {
                                        if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
                                        if (kv.Key.IndexOf(headerKey, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            headerKey.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            matchedValue = kv.Value;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(matchedValue))
                                table.Cells[rowIndex, col].TextString = matchedValue;
                        }

                        // 其余 restKeys（通常为空）
                        for (int di = 0; di < restKeys.Count; di++)
                        {
                            int col = fixedCols + di;
                            var key = restKeys[di];
                            if (item.Attributes != null && item.Attributes.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                            {
                                table.Cells[rowIndex, col].TextString = v;
                            }
                        }
                    }

                    // 自适应列宽与插入
                    AutoResizeColumns(table);
                    ms.AppendEntity(table);
                    tr.AddNewlyCreatedDBObject(table, true);

                    tr.Commit();
                }
                catch
                {
                    tr.Abort();
                    throw;
                }
            }

            // 刷新显示
            try
            {
                ed.Regen();
                Application.UpdateScreen();
            }
            catch { }
        }

        /// <summary>
        /// 从管道标题中提取管道等级，例如 "350-AR-1002-1.0G11" -> "1.0G11"
        /// 优先匹配含小数点的等级格式（如 1.0G11），再做宽松匹配。
        /// </summary>
        private string ExtractPipeClassFromTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;
            title = title!.Trim();

            // 先整体搜索常见格式：数字.数字 + 可选字母数字（如 1.0G11）
            var m = Regex.Match(title, @"\d+\.\d+[A-Za-z0-9]*", RegexOptions.IgnoreCase);
            if (m.Success) return m.Value;

            // 如果没有小数点形式，按分隔符拆分并从后向前查找合适片段
            var parts = title.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                var seg = parts[i].Trim();
                if (string.IsNullOrEmpty(seg)) continue;

                // 常见样式：数字+字母+数字 或 字母+数字（如 1G11 / G11）
                if (Regex.IsMatch(seg, @"^\d+[A-Za-z]+\d*$", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(seg, @"^[A-Za-z]+\d+$", RegexOptions.IgnoreCase))
                {
                    return seg;
                }

                // 也接受含字母后跟数字的片段
                if (Regex.IsMatch(seg, @"[A-Za-z]\d", RegexOptions.IgnoreCase))
                    return seg;
            }

            // 退回到更宽松的全局匹配：数字.数字 或 带字母的数字段
            var m2 = Regex.Match(title, @"\d+\.\d+|[A-Za-z]*\d+[A-Za-z]+\d*", RegexOptions.IgnoreCase);
            if (m2.Success) return m2.Value;

            return string.Empty;
        }


        /// <summary>
        /// 计算总列数
        /// </summary>
        private int CalculateTotalColumns(List<EquipmentInfo> equipmentList)
        {
            const int baseFixedCols = 10; // 基础固定列 0..9
            // 收集所有属性键（去除保留/已合并键）
            var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in equipmentList)
            {
                if (e.Attributes == null) continue;
                foreach (var k in e.Attributes.Keys)
                {
                    if (string.IsNullOrWhiteSpace(k)) continue;
                    allKeys.Add(k);
                }
            }

            // 移除保留键
            var reserved = new[]
            {
                "管道标题","管段号","管道号","起点","始点","终点","止点","管道等级",
                "介质","介质名称","Medium","Medium Name","操作温度","操作压力",
                "隔热隔声代号","是否防腐","Length","长度","长度(m)"
            };
            foreach (var r in reserved) allKeys.Remove(r);

            // 合并标准号同义词，避免单独列出 "标准号"
            var standardSynonyms = new[] { "标准号", "图号", "DWG.No.", "STD.No.", "标准" };
            foreach (var s in standardSynonyms) allKeys.Remove(s);

            // 管道组默认首选字段
            var pipeGroupPreferred = new[]
            {
                "名称", "材料", "图号或标准号", "数量", "泵前/后",
            };

            int pipeCount = 0;
            // 首先统计首选字段中存在的（或保留）数量
            foreach (var pk in pipeGroupPreferred)
            {
                // 保证列存在（即使属性缺失，也保留位）
                pipeCount++;
                // 如果存在于 allKeys，则从集合移除（避免重复计数）
                if (allKeys.Contains(pk)) allKeys.Remove(pk);
            }

            // 剩余属性全部放入管道组
            pipeCount += allKeys.Count;

            // total = baseFixedCols + pipeCount
            return baseFixedCols + pipeCount;
        }

        /// <summary>
        /// 选择并分析属性块或动态属性块，提取设备信息并整理
        /// </summary>
        /// <param name="ed">AutoCAD编辑器对象，用于用户交互</param>
        /// <param name="db">当前图形数据库对象</param>
        /// <returns>设备信息列表，包含从属性块中提取的所有设备数据</returns>
        private List<EquipmentInfo> SelectAndAnalyzeBlocks(Editor ed, Database db)
        {
            // 用于存储最终设备信息的列表
            List<EquipmentInfo> equipmentList = new List<EquipmentInfo>();
            // 用于合并相同设备的字典（键为设备唯一标识，值为设备信息）
            Dictionary<string, EquipmentInfo> equipmentDict = new Dictionary<string, EquipmentInfo>();
            // 创建选择选项对象，用于配置用户选择行为
            PromptSelectionOptions opts = new PromptSelectionOptions();
            // 设置用户选择时的提示信息
            opts.MessageForAdding = "\n请选择要生成设备表的属性块或动态属性块：";
            // 允许选择重复的对象
            opts.AllowDuplicates = true;
            // 提示用户在图纸中选择对象
            PromptSelectionResult selResult = ed.GetSelection(opts);
            // 如果用户取消选择或选择失败，返回空列表
            if (selResult.Status != PromptStatus.OK)
                return equipmentDict.Values.ToList();
            // 开启数据库事务，确保操作的原子性（要么全部成功，要么全部回滚）
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 遍历用户选择的所有对象
                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        // 将选择的对象转换为块引用（只读模式打开）
                        BlockReference blockRef = trans.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                        // 如果不是块引用，跳过当前对象
                        if (blockRef == null) continue;

                        // 根据块引用获取对应的块定义（块的模板信息）
                        BlockTableRecord blockDef = trans.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        // 如果块定义无效，跳过当前对象
                        if (blockDef == null) continue;
                        // 获取块名称（作为设备名称的基础）
                        string blockName = blockDef.Name;
                        // 创建设备信息对象
                        EquipmentInfo equipment = new EquipmentInfo();
                        equipment.Name = blockName;
                        // 根据块名称判断设备类型（需实现DetermineEquipmentType方法）
                        equipment.Type = DetermineEquipmentType(blockName);

                        // 读取块引用中的属性信息（静态属性）
                        foreach (ObjectId attId in blockRef.AttributeCollection)
                        {
                            // 将属性ID转换为属性引用对象
                            AttributeReference attRef = trans.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                            if (attRef != null)
                            {
                                // 属性标签（如"型号"、"规格"）
                                string tag = attRef.Tag;
                                // 属性值（如"ABC-123"）
                                string value = attRef.TextString;
                                // 将属性添加到设备信息中
                                equipment.Attributes[tag] = value;

                                // 处理属性的英文名称映射（假设ChineseToEnglish是预定义的字典）
                                if (ChineseToEnglish.ContainsKey(tag))
                                {
                                    equipment.EnglishNames[tag] = ChineseToEnglish[tag];
                                }
                                else
                                {
                                    // 如果没有对应的英文名称，使用标签原名
                                    equipment.EnglishNames[tag] = tag;
                                }
                            }
                        }

                        // 处理动态属性（如动态块中的拉伸参数、可见性参数等）
                        ProcessDynamicProperties(blockRef, equipment);

                        // 如果存在旧键 "介质" 且没有 "介质名称"，迁移
                        if (equipment.Attributes.TryGetValue("介质", out var mv) && !equipment.Attributes.ContainsKey("介质名称"))
                            equipment.Attributes["介质名称"] = mv;

                        // 生成设备的唯一标识键（用于合并相同设备）
                        string equipmentKey = GenerateEquipmentKey(equipment);
                        // 检查字典中是否已存在该设备
                        if (equipmentDict.ContainsKey(equipmentKey))
                        {
                            // 如果存在，数量加1
                            equipmentDict[equipmentKey].Count++;
                        }
                        else
                        {
                            // 如果不存在，添加到字典，初始数量为1
                            equipment.Count = 1;
                            equipmentDict[equipmentKey] = equipment;
                        }
                    }

                    // 提交事务，保存所有操作
                    trans.Commit();
                }
                catch
                {
                    // 如果发生异常，回滚事务
                    trans.Abort();
                    // 抛出异常，由上层处理
                    throw;
                }
            }

            // 将字典中的设备信息转换为列表并返回
            return equipmentDict.Values.ToList();
        }

        /// <summary>
        /// 处理动态块中的动态属性（如拉伸参数、可见性参数等）
        /// </summary>
        /// <param name="blockRef">动态块引用对象</param>
        /// <param name="equipment">设备信息对象，用于存储提取的属性值</param>
        private void ProcessDynamicProperties(BlockReference blockRef, EquipmentInfo equipment)
        {
            try
            {
                // 获取动态块的所有动态属性集合
                DynamicBlockReferencePropertyCollection dynProps = blockRef.DynamicBlockReferencePropertyCollection;

                // 遍历所有动态属性
                foreach (DynamicBlockReferenceProperty dynProp in dynProps)
                {
                    // 跳过只读属性（通常为系统保留属性，不可修改）
                    if (dynProp.ReadOnly) continue;
                    // 获取属性名称（如"拉伸距离"、"旋转角度"）
                    string propName = dynProp.PropertyName;
                    // 获取属性值并转换为字符串（处理可能的空值）
                    string propValue = dynProp.Value?.ToString() ?? "";
                    // 将动态属性添加到设备信息中，使用"动态_"前缀以便与普通属性区分
                    equipment.Attributes[$"动态_{propName}"] = propValue;
                    // 设置英文名称，使用"Dyn_"前缀表示动态属性
                    equipment.EnglishNames[$"动态_{propName}"] = $"Dyn_{propName}";
                }
            }
            catch (Exception ex)
            {
                // 忽略动态属性读取错误（确保不会因单个属性错误导致整个流程中断）
                // 可根据需要添加日志记录，如：
                Env.Editor.WriteMessage($"读取动态块属性失败: {ex.Message}");
            }
        }
        #region
        /// <summary>
        /// 帮助：基于已知关键字优先级构建分组键（属性存在时）
        /// </summary>
        /// <param name="attrMap"></param>
        /// <returns></returns>
        private string BuildAttributeGroupKey(Dictionary<string, string> attrMap)
        {
            if (attrMap == null || attrMap.Count == 0) return string.Empty;

            // 关注的属性顺序（优先级）：可以按需扩展
            var priorityKeys = new[]
            {
            "直径", "外径", "内径", "厚度", "规格", "型号",
            "宽度", "高度",
            "材料", "介质",
            "标准号", "标准",
            "功率", "容积", "压力", "温度",
            "材质" // 兼容不同命名
        };

            var parts = new List<string>();
            foreach (var pk in priorityKeys)
            {
                // 找到 attrMap 中包含 pk 的键（不区分大小写，包含匹配）
                var found = attrMap.Keys.FirstOrDefault(k => k.IndexOf(pk, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!string.IsNullOrEmpty(found))
                {
                    var v = (attrMap[found] ?? string.Empty).Trim();
                    parts.Add($"{pk}:{v}");
                }
            }

            // 若没有匹配到优先字段，则把所有属性按键排序并拼接，保证分组稳定性
            if (parts.Count == 0)
            {
                foreach (var kv in attrMap.OrderBy(k => k.Key))
                {
                    parts.Add($"{kv.Key}:{(kv.Value ?? string.Empty).Trim()}");
                }
            }

            return string.Join("|", parts);
        }

        #endregion

        /*
         GeneratePipeTableFromSelection CalculateTotalColumns SyncPipeProperties  ExportTableToExcelFile
         */

        #region 获取动态块信息的方法

        /// <summary>
        /// 获取动态块的端点坐标（在模型空间中）
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <returns>包含起点和终点的元组</returns>
        public static (Point3d startPoint, Point3d endPoint) GetEndPoints(BlockReference blockRef)
        {
            // 获取当前数据库
            Database db = blockRef.Database;

            // 初始化返回值
            Point3d worldStartPoint = Point3d.Origin;
            Point3d worldEndPoint = Point3d.Origin;

            // 开始事务
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 获取块表记录
                    BlockTableRecord blockTableRecord = trans.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                    // 查找多段线
                    foreach (ObjectId entId in blockTableRecord)
                    {
                        Entity ent = trans.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent is Polyline polyline)
                        {
                            // 获取多段线的局部坐标端点
                            Point3d localStartPoint = polyline.GetPoint3dAt(0);
                            Point3d localEndPoint = polyline.GetPoint3dAt(polyline.NumberOfVertices - 1);

                            // 转换到世界坐标系
                            worldStartPoint = blockRef.BlockTransform * localStartPoint;
                            worldEndPoint = blockRef.BlockTransform * localEndPoint;

                            break; // 找到第一个多段线就退出
                        }
                    }

                    // 提交事务
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    // 错误处理
                    Env.Editor.WriteMessage($"\n获取端点时发生错误: {ex.Message}");
                }
            }
            // 返回世界坐标系下的起始点与终点
            return (worldStartPoint, worldEndPoint);
        }

        /// <summary>
        /// 获取动态块的中点坐标（在模型空间中）
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <returns>中点坐标</returns>
        public static Point3d GetMidPoint(BlockReference blockRef)
        {
            // 获取端点
            var (startPoint, endPoint) = GetEndPoints(blockRef);

            // 计算中点
            Point3d midPoint = new Point3d(
                (startPoint.X + endPoint.X) / 2,
                (startPoint.Y + endPoint.Y) / 2,
                (startPoint.Z + endPoint.Z) / 2
            );
            // 返回中点
            return midPoint;
        }

        /// <summary>
        /// 获取动态块的长度
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <returns>多段线长度</returns>
        public static double GetLength(BlockReference blockRef)
        {
            // 直接从动态块参数获取长度
            foreach (DynamicBlockReferenceProperty prop in blockRef.DynamicBlockReferencePropertyCollection)
            {
                // 查找包含长度信息的参数
                if (prop.PropertyName.Contains("Length") ||
                    prop.PropertyName.Contains("Distance") ||
                    prop.PropertyName.Contains("Stretch"))
                {
                    try
                    {
                        // 返回参数值作为长度
                        return Convert.ToDouble(prop.Value);
                    }
                    catch
                    {
                        // 如果转换失败，继续查找
                        continue;
                    }
                }
            }
            // 获取端点
            var (startPoint, endPoint) = GetEndPoints(blockRef);
            // 计算距离
            double length = startPoint.DistanceTo(endPoint);
            // 返回距离
            return length;
        }

        /// <summary>
        /// 获取动态块的所有信息
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <returns>包含所有信息的结构</returns>
        public static DynamicBlockInfo GetBlockInfo(BlockReference blockRef)
        {
            var info = new DynamicBlockInfo();
            // 获取端点
            var (startPoint, endPoint) = GetEndPoints(blockRef);
            info.StartPoint = startPoint;
            info.EndPoint = endPoint;
            // 计算中点
            info.MidPoint = GetMidPoint(blockRef);
            // 计算长度
            info.Length = GetLength(blockRef);
            // 获取旋转角度
            info.Rotation = blockRef.Rotation;
            // 获取插入点
            info.Position = blockRef.Position;
            // 返回所有信息
            return info;
        }

        #endregion

        #region

        /// 定义存储线段信息的类
        public class LineSegmentInfo
        {
            public Point3d MidPoint { get; set; }             // 中点坐标
            public double LineWeight { get; set; }            // 线宽（毫米）
            public ObjectId Id { get; set; }                  // 线段ID
            public Point3d StartPoint { get; set; }           // 起点坐标
            public Point3d EndPoint { get; set; }             // 终点坐标
            public List<Point3d>? MidPoints { get; set; }      // 中间点（多段线专用）
            public double Length { get; set; }                // 线段长度
            public double Angle { get; set; }                 // 线段角度（弧度）
            public string? Layer { get; set; }                 // 所在图层
            public int ColorIndex { get; set; }               // 颜色索引
            public double LinetypeScale { get; set; }         // 线型比例
            public string? EntityType { get; set; }            // 实体类型（Line/Polyline）
        }
        /// <summary>
        /// 获取所有选择的线段信息
        /// </summary>
        [CommandMethod("CollectLineInfo")]
        public static void CollectLineInfo()
        {
            // 获取当前文档和编辑器
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 创建选择过滤器，只允许选择直线(LINE)和轻量多段线(LWPOLYLINE)
                TypedValue[] filterValues = new TypedValue[] {
                        new TypedValue((int)DxfCode.Operator, "<OR"),
                        new TypedValue((int)DxfCode.Start, "LINE"),  // 使用 DxfCode.Start
                        new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),  // 使用 DxfCode.Start
                        new TypedValue((int)DxfCode.Operator, "OR>")
                    };

                SelectionFilter filter = new SelectionFilter(filterValues);

                // 设置选择选项
                PromptSelectionOptions opts = new PromptSelectionOptions
                {
                    MessageForAdding = "\n选择线段或多段线: ",
                    AllowDuplicates = false
                };

                // 获取用户选择
                PromptSelectionResult selResult = ed.GetSelection(opts, filter);

                if (selResult.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n未选择对象或选择已取消。");
                    return;
                }

                SelectionSet selectionSet = selResult.Value;
                ed.WriteMessage($"\n已选择 {selectionSet.Count} 个对象");

                // 存储所有线段信息的列表
                List<LineSegmentInfo> lineInfos = new List<LineSegmentInfo>();

                // 开始事务处理
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // 遍历所有选中的对象
                    foreach (SelectedObject selObj in selectionSet)
                    {
                        if (selObj == null) continue;

                        Entity entity = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Entity;

                        if (entity is Line line)
                        {
                            ed.WriteMessage("\n找到直线对象");
                            lineInfos.Add(ProcessLine(line, tr));
                        }
                        else if (entity is Polyline pline) // 处理轻量多段线
                        {
                            ed.WriteMessage("\n找到多段线对象");
                            lineInfos.Add(ProcessPolyline(pline, tr));
                        }
                        else
                        {
                            ed.WriteMessage($"\n跳过不支持的类型: {entity?.GetType().Name}");
                        }
                    }

                    // 输出收集到的信息
                    if (lineInfos.Count > 0)
                    {
                        PrintLineInfos(ed, lineInfos);
                    }
                    else
                    {
                        ed.WriteMessage("\n未找到可处理的线段对象");
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 处理直线(LINE)对象
        /// </summary>
        /// <param name="line"></param>
        /// <param name="tr"></param>
        /// <returns></returns>
        private static LineSegmentInfo ProcessLine(Line line, Transaction tr)
        {
            // 计算线段角度（与X轴正方向的夹角，弧度）
            Vector3d lineVector = line.EndPoint - line.StartPoint;
            return new LineSegmentInfo
            {
                Id = line.ObjectId,
                StartPoint = line.StartPoint,
                EndPoint = line.EndPoint,
                MidPoints = new List<Point3d>(),
                Length = line.Length,
                Angle = lineVector.GetAngleTo(Vector3d.XAxis),
                Layer = GetLayerName(line.LayerId, tr),
                ColorIndex = line.ColorIndex,
                LinetypeScale = line.LinetypeScale,
                EntityType = "LINE"
            };
        }

        /// <summary>
        /// 处理多段线(POLYLINE)对象
        /// </summary>
        /// <param name="pline"></param>
        /// <param name="tr"></param>
        /// <returns></returns>
        private static LineSegmentInfo ProcessPolyline(Polyline pline, Transaction tr)
        {
            List<Point3d> vertices = new List<Point3d>();
            int numVertices = pline.NumberOfVertices;

            // 获取所有顶点
            for (int i = 0; i < numVertices; i++)
            {
                vertices.Add(pline.GetPoint3dAt(i));
            }

            // 计算总长度（考虑闭合情况）
            double totalLength = pline.Length;

            // 获取第一段线段的角度
            double angle = 0;
            if (numVertices >= 2)
            {
                Vector3d vector = pline.GetPoint3dAt(1) - pline.GetPoint3dAt(0);
                angle = vector.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
            }

            return new LineSegmentInfo
            {
                Id = pline.ObjectId,
                StartPoint = pline.StartPoint,
                EndPoint = pline.EndPoint,
                MidPoints = vertices.Skip(1).Take(vertices.Count - 2).ToList(),
                Length = totalLength,
                Angle = angle,
                Layer = GetLayerName(pline.LayerId, tr),
                ColorIndex = pline.ColorIndex,
                LinetypeScale = pline.LinetypeScale,
                EntityType = "LWPOLYLINE"
            };
        }

        /// <summary>
        /// 根据图层ID获取图层名称
        /// </summary>
        /// <param name="layerId"></param>
        /// <param name="tr"></param>
        /// <returns></returns>
        private static string GetLayerName(ObjectId layerId, Transaction tr)
        {
            if (layerId.IsNull) return "0";

            try
            {
                LayerTableRecord ltr = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                return ltr?.Name ?? "0";
            }
            catch
            {
                return "0";
            }
        }

        /// <summary>
        /// 输出收集到的线段信息
        /// </summary>
        /// <param name="ed"></param>
        /// <param name="infos"></param>
        private static void PrintLineInfos(Editor ed, List<LineSegmentInfo> infos)
        {
            ed.WriteMessage("\n\n===== 线段信息报告 =====");
            ed.WriteMessage($"\n共处理 {infos.Count} 个线段对象");

            foreach (var info in infos)
            {
                ed.WriteMessage("\n--------------------------------");
                ed.WriteMessage($"\n对象ID: {info.Id}");
                ed.WriteMessage($"\n类型: {info.EntityType}");
                ed.WriteMessage($"\n起点: X={info.StartPoint.X:F2}, Y={info.StartPoint.Y:F2}, Z={info.StartPoint.Z:F2}");
                ed.WriteMessage($"\n终点: X={info.EndPoint.X:F2}, Y={info.EndPoint.Y:F2}, Z={info.EndPoint.Z:F2}");

                if (info.MidPoints?.Count > 0)
                {
                    ed.WriteMessage($"\n中间点({info.MidPoints.Count}个):");
                    foreach (var pt in info.MidPoints)
                    {
                        ed.WriteMessage($"\n  X={pt.X:F2}, Y={pt.Y:F2}, Z={pt.Z:F2}");
                    }
                }

                ed.WriteMessage($"\n长度: {info.Length:F2}");
                ed.WriteMessage($"\n角度: {RadiansToDegrees(info.Angle):F1}°");
                ed.WriteMessage($"\n图层: {info.Layer}");
                ed.WriteMessage($"\n颜色索引: {info.ColorIndex}");
                ed.WriteMessage($"\n线型比例: {info.LinetypeScale:F2}");
            }

            ed.WriteMessage("\n\n===== 报告结束 =====");
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        private static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        #endregion

        #region 同步管道的实现方法

        /// <summary>
        /// 存储从示例块中分析出的管道信息
        /// </summary>
        public class SamplePipeInfo
        {
            public Polyline PipeBodyTemplate { get; set; }
            public Polyline DirectionArrowTemplate { get; set; }
            public List<AttributeDefinition> AttributeDefinitions { get; set; } = new List<AttributeDefinition>();
            public Point3d BasePoint { get; set; }
        }

        // 替换 SyncPipeProperties 内关于箭头生成的那几行（用 CreateDirectionalArrowsAndTitles 取代只在中点生成单个箭头）
        // 定位：在方法中计算 pipeLocal 之后、CloneAttributeDefinitionsLocal 之前的位置
        // 原始片段包含 EnsureArrowEntities / AlignArrowToDirection / midArrowOutline 的逻辑，替换为下面代码。 pipeTitle  DrawPipeByClicks


        /// <summary>
        /// 同步管道\属性
        /// </summary>        
        [CommandMethod("SyncPipeProperties")]
        public void SyncPipeProperties()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 选择线段
            var lineSelResult = ed.GetSelection(
                new PromptSelectionOptions { MessageForAdding = "\n请选择要同步的线段 (LINE 或 LWPOLYLINE):" },
                new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE") })
            );
            if (lineSelResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n操作取消。");
                return;
            }
            var sourceLineIds = lineSelResult.Value.GetObjectIds().ToList();

            // 选择示例管线块（作为样例）
            var blockSelResult = ed.GetEntity("\n请选择示例管线块:");
            if (blockSelResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n操作取消。");
                return;
            }

            using (var tr = new DBTrans())
            {
                try
                {
                    // 读取示例块参照
                    var sampleBlockRef = tr.GetObject(blockSelResult.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (sampleBlockRef == null)
                    {
                        ed.WriteMessage("\n错误：选择的不是块参照。");
                        return;
                    }

                    // 解析示例块（提取 polyline / arrow / attribute definitions）
                    var sampleInfo = AnalyzeSampleBlock(tr, sampleBlockRef);
                    if (sampleInfo?.PipeBodyTemplate == null)
                    {
                        ed.WriteMessage("\n错误：示例块中未找到作为管道主体的 Polyline。");
                        return;
                    }

                    // 收集并构建顶点顺序
                    var lineSegments = CollectLineSegments(tr, sourceLineIds);
                    if (lineSegments == null || lineSegments.Count == 0)
                    {
                        ed.WriteMessage("\n未找到可处理的线段。");
                        return;
                    }

                    var orderedVertices = BuildOrderedVerticesFromSegments(lineSegments, 0.1);
                    if (orderedVertices == null || orderedVertices.Count < 2)
                    {
                        ed.WriteMessage("\n顶点不足，无法生成管线。");
                        return;
                    }

                    // 读取示例块的所有属性
                    var sampleAttrMap = GetEntityAttributeMap(tr, sampleBlockRef) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // 加载上次保存的属性（此处取决于 sampleBlockRef 名称中是否包含入口/出口关键词）
                    // 既然 SyncPipeProperties 没有 isOutlet 参数，尝试根据名称判断
                    // 加载上次保存的属性（根据示例块名称判断入口/出口）
                    bool sampleIsOutlet = (sampleBlockRef.Name ?? string.Empty).ToLowerInvariant().Contains("出口") ||
                                          (sampleBlockRef.Name ?? string.Empty).ToLowerInvariant().Contains("outlet");

                    // 从磁盘读取历史属性
                    var savedAttrsSync = FileManager.LoadLastPipeAttributes(sampleIsOutlet);

                    // 打开属性编辑窗，传入合并后的初始字典
                    using (var editor = new PipeAttributeEditorForm(savedAttrsSync))
                    {
                        var dr = editor.ShowDialog();
                        if (dr != DialogResult.OK)
                        {
                            ed.WriteMessage("\n已取消属性编辑，停止同步操作。");
                            return;
                        }

                        // 保存历史属性供下次使用
                        var editedAttrs = editor.Attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        FileManager.SaveLastPipeAttributes(sampleIsOutlet, editedAttrs);

                        // 把用户修改后的属性写回示例块（只写存在的 AttributeReference）
                        try
                        {
                            var sampleBrWrite = tr.GetObject(sampleBlockRef.ObjectId, OpenMode.ForWrite) as BlockReference;
                            if (sampleBrWrite != null)
                            {
                                foreach (ObjectId aid in sampleBrWrite.AttributeCollection)
                                {
                                    try
                                    {
                                        var ar = tr.GetObject(aid, OpenMode.ForWrite) as AttributeReference;
                                        if (ar == null || string.IsNullOrWhiteSpace(ar.Tag)) continue;
                                        if (editedAttrs.TryGetValue(ar.Tag, out var newVal))
                                        {
                                            ar.TextString = newVal ?? string.Empty;
                                            try { ar.AdjustAlignment(db); } catch { }
                                        }
                                    }
                                    catch { /* 单个属性写回失败不阻塞整体 */ }
                                }
                            }
                        }
                        catch (System.Exception exWriteSample)
                        {
                            ed.WriteMessage($"\n写回示例块属性时出错: {exWriteSample.Message}");
                        }
                    }
                    // 开始构建新管道块
                    double pipelineLength = 0.0;
                    for (int i = 0; i < orderedVertices.Count - 1; i++)
                        pipelineLength += orderedVertices[i].DistanceTo(orderedVertices[i + 1]);

                    var (midPoint, midAngle) = ComputeMidPointAndAngle(orderedVertices, pipelineLength);
                    Vector3d targetDir = ComputeDirectionAtPoint(orderedVertices, midPoint, 1e-6);
                    Vector3d segmentDir = ComputeAggregateSegmentDirection(lineSegments);
                    if (!segmentDir.IsZeroLength() && targetDir.DotProduct(segmentDir) < 0)
                        targetDir = -targetDir;

                    Vector3d targetDirNormalized = targetDir.IsZeroLength() ? Vector3d.XAxis : targetDir.GetNormal();
                    Polyline pipeLocal = BuildPipePolylineLocal(sampleInfo.PipeBodyTemplate, orderedVertices, midPoint);

                    // 复制属性定义（基于示例块的定义）
                    var attDefsLocal = CloneAttributeDefinitionsLocal(sampleInfo.AttributeDefinitions, midPoint, 0.0, pipelineLength, sampleBlockRef.Name)
                                        ?? new List<AttributeDefinition>();

                    // 重新读取示例块属性（刚刚可能已被编辑并写回）
                    var latestSampleAttrs = GetEntityAttributeMap(tr, sampleBlockRef) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // 生成标题（优先属性中的管道标题）
                    string pipeTitle = latestSampleAttrs.TryGetValue("管道标题", out var sampleTitle) && !string.IsNullOrWhiteSpace(sampleTitle)
                                       ? sampleTitle
                                       : sampleBlockRef.Name ?? "管道";

                    // 为每一段生成局部坐标下的箭头与标题（相对于 midPoint），仅使用分段箭头/文字
                    var arrowEntities = CreateDirectionalArrowsAndTitles(tr, sampleInfo, orderedVertices, midPoint, pipeTitle, sampleBlockRef.Name);
                    
                    // 把最新属性写入/覆盖到 attDefsLocal（存在则覆盖，不存在则新增）
                    double attHeight = attDefsLocal.Count > 0 ? attDefsLocal[0].Height : 3.5;// 默认高度
                    double yOffsetBase = attDefsLocal.Count > 0 ? attDefsLocal[0].Position.Y - attHeight * 1.2 : -attHeight * 2.0;// 默认偏移 基础 Y 偏移
                    int extraIndex = 0;
                    foreach (var kv in latestSampleAttrs)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key)) continue;// 忽略空标签
                        // 检查属性标签是否已存在 尝试更新 查找是否已存在该属性定义
                        var existing = attDefsLocal.FirstOrDefault(a => string.Equals(a.Tag, kv.Key, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                        {
                            existing.TextString = kv.Value ?? string.Empty;// 属性值 更新属性值
                            existing.Invisible = false; // 先临时设置，后面统一控制显示
                            existing.Constant = false;// 属性定义
                        }
                        else
                        {
                            // 新增 新增属性定义
                            attDefsLocal.Add(new AttributeDefinition
                            {
                                Tag = kv.Key,// 属性标签
                                Position = new Point3d(0, yOffsetBase - extraIndex * attHeight * 1.2, 0),// 默认位置 属性位置
                                Rotation = 0.0,// 默认旋转 默认旋转角度
                                TextString = kv.Value ?? string.Empty,// 默认值
                                Height = attHeight,// 默认高度
                                Invisible = false, // 临时
                                Constant = false
                            });
                            extraIndex++;
                        }
                    }
                   
                    // 新增或覆盖 起点/终点 属性定义（保持原逻辑）
                    Point3d worldStart = orderedVertices.First();
                    Point3d worldEnd = orderedVertices.Last();
                    string startCoordStr = $"X={worldStart.X:F3},Y={worldStart.Y:F3}";
                    string endCoordStr = $"X={worldEnd.X:F3},Y={worldEnd.Y:F3}";
                    int nextSegNum = GetNextPipeSegmentNumber(db);

                    // 取管段号，优先从属性或标题提取
                    string extractedPipeNo = string.Empty;
                    if (latestSampleAttrs.TryGetValue("管道标题", out var titleFromSample) && !string.IsNullOrWhiteSpace(titleFromSample))
                    {
                        extractedPipeNo = ExtractPipeCodeFromTitle(titleFromSample);
                    }
                    if (string.IsNullOrWhiteSpace(extractedPipeNo))
                    {
                        extractedPipeNo = ExtractPipeCodeFromTitle(sampleBlockRef.Name);
                    }
                    if (string.IsNullOrWhiteSpace(extractedPipeNo))
                    {
                        if (latestSampleAttrs.TryGetValue("管段号", out var pn) && !string.IsNullOrWhiteSpace(pn))
                            extractedPipeNo = pn;
                        else if (latestSampleAttrs.TryGetValue("管段编号", out var pn2) && !string.IsNullOrWhiteSpace(pn2))
                            extractedPipeNo = pn2;
                    }
                    if (string.IsNullOrWhiteSpace(extractedPipeNo))
                    {
                        extractedPipeNo = nextSegNum.ToString("D4");
                    }
                    // 生成管段号属性 局部函数：设置或新增属性定义
                    void SetOrAddAttrLocal(string tag, string text)
                    {
                        // 尝试更新 查找是否已存在该属性定义
                        var existing = attDefsLocal.FirstOrDefault(a => string.Equals(a.Tag, tag, StringComparison.OrdinalIgnoreCase));
                        if (existing != null)
                        {
                            existing.TextString = text;// 更新属性值
                            existing.Invisible = false;// 临时 先临时设置，后面统一控制显示
                            existing.Constant = false;// 临时 非常量
                        }
                        else
                        {
                            attDefsLocal.Add(new AttributeDefinition// 新增 新增属性定义
                            {
                                Tag = tag,// 属性标签
                                Position = new Point3d(0, yOffsetBase - extraIndex * attHeight * 1.2, 0),// 属性位置
                                Rotation = 0.0,// 属性旋转角度
                                TextString = text,// 属性值
                                Height = attHeight,// 属性高度
                                Invisible = false,// 临时
                                Constant = false// 非常量
                            });
                            extraIndex++;
                        }
                    }

                    SetOrAddAttrLocal("始点", startCoordStr);
                    SetOrAddAttrLocal("终点", endCoordStr);
                    SetOrAddAttrLocal("管段号", extractedPipeNo);
                    // 移除块定义中的中点“管道标题”属性（避免在块中重复显示中点标题）
                    // 并把其余属性都设置为隐藏（块内不显示），让分段文字/箭头负责显示标题
                    attDefsLocal.RemoveAll(ad => string.Equals(ad.Tag, "管道标题", StringComparison.OrdinalIgnoreCase));
                    foreach (var ad in attDefsLocal)
                    {
                        ad.Invisible = true; // 隐藏所有属性（块内不显示）
                        ad.Constant = false;
                    }
                  

                    // 构建块定义并插入新块
                    string desiredName = sampleBlockRef.Name;
                    string newBlockName = BuildPipeBlockDefinition(tr, desiredName, (Polyline)pipeLocal.Clone(), arrowEntities, attDefsLocal);

                    var attValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var a in attDefsLocal)
                    {
                        if (string.IsNullOrWhiteSpace(a?.Tag)) continue;
                        attValues[a.Tag] = a.TextString ?? string.Empty;
                    }

                    var newBrId = InsertPipeBlockWithAttributes(tr, midPoint, newBlockName, 0.0, attValues);
                    var newBr = tr.GetObject(newBrId, OpenMode.ForWrite) as BlockReference;
                    if (newBr != null)
                        newBr.Layer = sampleInfo.PipeBodyTemplate.Layer;

                    // 删除原始线段
                    foreach (var seg in lineSegments)
                    {
                        var ent = tr.GetObject(seg.Id, OpenMode.ForWrite) as Entity;
                        if (ent != null)
                            ent.Erase();
                    }

                    tr.Commit();
                    ed.WriteMessage($"\n管线块已生成：新增/更新属性 [始点][终点][管段号]={extractedPipeNo}。仅显示字段：管道标题。");
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n发生错误: {ex.Message}\n{ex.StackTrace}");
                    tr.Abort();
                }
            }
        }

        /// <summary>
        /// 新增表单窗口：PipeAttributeEditorForm —— 编辑示例图元的属性表（键不可改，值可编辑）
        /// </summary>
        public class PipeAttributeEditorForm : Form
        {
            private DataGridView _grid;// 属性表 属性表网格
            private Button _btnOk;// 确认按钮
            private Button _btnCancel;// 取消按钮 确认和取消按钮
            private Dictionary<string, string> _attributes;// 属性表/ 存储属性表

            /// <summary>
            /// 属性表编辑后的属性表
            /// </summary>
            public Dictionary<string, string> Attributes => new Dictionary<string, string>(_attributes, StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// 属性表编辑窗口
            /// </summary>
            /// <param name="initialAttributes"></param>
            public PipeAttributeEditorForm(Dictionary<string, string> initialAttributes)
            {
                _attributes = new Dictionary<string, string>(initialAttributes ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
                InitializeComponent();
                LoadAttributesToGrid();
            }
            /// <summary>
            /// 初始化控件
            /// </summary>
            private void InitializeComponent()
            {
                this.Text = "示例管道属性编辑";
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.StartPosition = FormStartPosition.CenterParent;
                this.ClientSize = new Size(640, 420);
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.MinimizeBox = false;
                this.ShowInTaskbar = false;
                this.AutoScaleMode = AutoScaleMode.Font;

                _grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.CellSelect,
                    MultiSelect = false
                };

                var colKey = new DataGridViewTextBoxColumn { Name = "Key", HeaderText = "字段", ReadOnly = true };
                var colVal = new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "值", ReadOnly = false };

                _grid.Columns.Add(colKey);
                _grid.Columns.Add(colVal);

                _btnOk = new Button { Text = "完成", DialogResult = DialogResult.OK, Width = 90, Height = 30 };
                _btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 90, Height = 30 };

                _btnOk.Click += BtnOk_Click;
                _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                // 底部按钮面板，右对齐
                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
                    Padding = new Padding(8),
                    WrapContents = false
                };

                // 添加按钮到面板（右到左）
                panel.Controls.Add(_btnCancel);
                panel.Controls.Add(_btnOk);

                // 设置接受/取消按钮
                this.AcceptButton = _btnOk;
                this.CancelButton = _btnCancel;

                // 按钮与网格先后添加，保证 DockFill 占满剩余空间
                this.Controls.Add(_grid);
                this.Controls.Add(panel);
            }

            private void LoadAttributesToGrid()
            {
                _grid.Rows.Clear();
                foreach (var kv in _attributes.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    _grid.Rows.Add(kv.Key, kv.Value);
                }
                if (_grid.Rows.Count > 0)
                    _grid.CurrentCell = _grid.Rows[0].Cells[1];
            }

            private void BtnOk_Click(object sender, EventArgs e)
            {
                // 保存网格中用户编辑的值回 _attributes
                try
                {
                    var newDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < _grid.Rows.Count; i++)
                    {
                        var row = _grid.Rows[i];
                        if (row.IsNewRow) continue;
                        var keyCell = row.Cells["Key"].Value;
                        var valCell = row.Cells["Value"].Value;
                        if (keyCell == null) continue;
                        string key = keyCell.ToString() ?? string.Empty;
                        string val = valCell?.ToString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(key)) continue;
                        newDict[key] = val;
                    }
                    _attributes = newDict;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存属性失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 扫描模型空间中已有块引用的属性，找出最大已用管段号并返回下一个编号（整数）
        /// 编号规则：解析属性值内的首个连续数字序列作为编号；若无，跳过。
        /// </summary>
        /// <param name="db">当前数据库</param>
        /// <returns>下一个管段号（从 1 开始）</returns>
        private int GetNextPipeSegmentNumber(Database db)
        {
            int max = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead); 
                    foreach (ObjectId id in ms)
                    {
                        try
                        {
                            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent is BlockReference br)
                            {
                                foreach (ObjectId aid in br.AttributeCollection)
                                {
                                    try
                                    {
                                        var ar = tr.GetObject(aid, OpenMode.ForRead) as AttributeReference;
                                        if (ar == null) continue;
                                        if (!string.Equals(ar.Tag, "管段号", StringComparison.OrdinalIgnoreCase) &&
                                            !string.Equals(ar.Tag, "管段编号", StringComparison.OrdinalIgnoreCase))
                                            continue;

                                        string txt = ar.TextString ?? string.Empty;
                                        if (string.IsNullOrWhiteSpace(txt)) continue;

                                        // 提取首个连续数字序列
                                        var m = Regex.Match(txt, @"\d+");
                                        if (m.Success && int.TryParse(m.Value, out int val))
                                        {
                                            if (val > max) max = val;
                                        }
                                    }
                                    catch { /* 忽略单个属性读取问题 */ }
                                }
                            }
                        }
                        catch { /* 忽略单个实体读取问题 */ }
                    }

                    tr.Commit();
                }
                catch
                {
                    tr.Abort();
                }
            }
            return max + 1;
        }

        /// <summary>
        /// 将箭头几何按照指定方向对齐
        /// </summary>
        private (Polyline outline, Solid? fill) AlignArrowToDirection(Polyline arrowTemplate, Solid? fillTemplate, Vector3d direction)
        {
            Vector3d dir = direction.IsZeroLength() ? Vector3d.XAxis : direction.GetNormal();
            Vector3d yAxis = Vector3d.ZAxis.CrossProduct(dir);
            if (yAxis.IsZeroLength())
                yAxis = Vector3d.YAxis;
            else
                yAxis = yAxis.GetNormal();

            Matrix3d alignMatrix = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                Point3d.Origin, dir, yAxis, Vector3d.ZAxis
            );

            var outline = (Polyline)arrowTemplate.Clone();
            outline.TransformBy(alignMatrix);

            Solid? fill = null;
            if (fillTemplate != null)
            {
                fill = (Solid)fillTemplate.Clone();
                fill.TransformBy(alignMatrix);
            }

            return (outline, fill);
        }

        /// <summary>
        /// 获取箭头
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        private static Vector3d ComputeAggregateSegmentDirection(List<LineSegmentInfo> segments)
        {
            if (segments == null || segments.Count == 0)
                return new Vector3d(0, 0, 0);

            Vector3d sum = new Vector3d(0, 0, 0);
            foreach (var seg in segments)
            {
                Vector3d dir = seg.EndPoint - seg.StartPoint;
                if (!dir.IsZeroLength())
                    sum += dir.GetNormal();
            }

            return sum.IsZeroLength() ? new Vector3d(0, 0, 0) : sum.GetNormal();
        }

        /// <summary>
        /// 计算某点附近的方向向量（优先使用与 referencePoint 最近的线段）
        /// </summary>
        private static Vector3d ComputeDirectionAtPoint(List<Point3d> orderedVertices, Point3d referencePoint, double tol = 1e-6)
        {
            if (orderedVertices == null || orderedVertices.Count < 2)
                return Vector3d.XAxis;

            Vector3d fallbackDir = ComputePathDirectionVector(orderedVertices, tol);
            double bestDist = double.MaxValue;
            Vector3d bestDir = fallbackDir.IsZeroLength() ? Vector3d.XAxis : fallbackDir;

            for (int i = 0; i < orderedVertices.Count - 1; i++)
            {
                Point3d start = orderedVertices[i];
                Point3d end = orderedVertices[i + 1];
                Vector3d segment = end - start;
                if (segment.IsZeroLength())
                    continue;

                Point3d projected = ProjectPointToSegment(referencePoint, start, end);
                double dist = referencePoint.DistanceTo(projected);
                if (dist + tol < bestDist)
                {
                    bestDist = dist;
                    bestDir = segment.GetNormal();
                }
            }

            if (!bestDir.IsZeroLength() && !fallbackDir.IsZeroLength() && bestDir.DotProduct(fallbackDir) < 0)
            {
                bestDir = -bestDir;
            }

            return bestDir.IsZeroLength() ? fallbackDir : bestDir;
        }

        /// <summary>
        /// 将点投影到指定线段上
        /// </summary>
        private static Point3d ProjectPointToSegment(Point3d point, Point3d segmentStart, Point3d segmentEnd)
        {
            Vector3d segment = segmentEnd - segmentStart;
            if (segment.IsZeroLength())
                return segmentStart;

            Vector3d toPoint = point - segmentStart;
            double t = toPoint.DotProduct(segment) / segment.DotProduct(segment);
            t = Math.Max(0.0, Math.Min(1.0, t));
            return segmentStart + segment * t;
        }

        /// <summary>
        /// 计算整条路径的总体方向向量（UCS，Z=+）
        /// </summary>
        private static Vector3d ComputePathDirectionVector(List<Point3d> orderedVertices, double tol = 1e-6)
        {
            if (orderedVertices == null || orderedVertices.Count < 2)
                return Vector3d.XAxis;

            // 直接用整体起点→终点的向量，保证箭头指向终点（流向）
            Vector3d overall = orderedVertices.Last() - orderedVertices.First();
            if (overall.Length > tol)
                return overall.GetNormal();

            // 回退：选择最长段方向
            double maxLen = 0.0;
            Vector3d longestDir = Vector3d.XAxis;
            for (int i = 0; i < orderedVertices.Count - 1; i++)
            {
                Vector3d v = orderedVertices[i + 1] - orderedVertices[i];
                if (v.Length > maxLen)
                {
                    maxLen = v.Length;
                    longestDir = v.GetNormal();
                }
            }
            return longestDir;
        }

        /// <summary>
        /// 获取选择的线段信息
        /// </summary>
        /// <param name="orderedVertices">有序顶点列表</param>
        /// <param name="totalLength">总长度</param>
        /// <returns></returns>
        private (Point3d midPoint, double midAngle) ComputeMidPointAndAngle(List<Point3d> orderedVertices, double totalLength)
        {
            double halfLen = totalLength / 2.0;
            double acc = 0.0;
            Point3d midPoint = orderedVertices[0];
            double midAngle = 0.0;

            for (int i = 0; i < orderedVertices.Count - 1; i++)
            {
                var p1 = orderedVertices[i];
                var p2 = orderedVertices[i + 1];
                double segLen = p1.DistanceTo(p2);
                if (acc + segLen >= halfLen)
                {
                    double t = (halfLen - acc) / segLen;
                    midPoint = new Point3d(
                        p1.X + (p2.X - p1.X) * t,
                        p1.Y + (p2.Y - p1.Y) * t,
                        p1.Z + (p2.Z - p1.Z) * t
                    );
                    midAngle = ComputeSegmentAngleUcs(p1, p2);
                    break;
                }
                acc += segLen;
            }
            return (midPoint, midAngle);
        }

        /// <summary>
        /// 计算线段角度
        /// </summary>
        /// <param name="p1">起点</param>
        /// <param name="p2">终点</param>
        /// <returns>线段在UCS中的角度</returns>
        private static double ComputeSegmentAngleUcs(Point3d p1, Point3d p2)
        {
            // 当前UCS的XY平面，保证与AutoCAD旋转角同一参考
            var plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            Vector3d dir = (p2 - p1).GetNormal();
            double angle = dir.AngleOnPlane(plane); // 以正X为0，逆时针为正
                                                    // 归一化到 [0, 2π)
            if (angle < 0) angle += 2.0 * Math.PI;
            return angle;
        }

        /// <summary>
        /// 构建局部坐标的管线 Polyline
        /// </summary>
        /// <param name="template">模板 Polyline</param>
        /// <param name="verticesWorld">全局坐标系下的顶点列表</param>
        /// <param name="midPointWorld">全局坐标系下的中点</param>
        /// <returns>局部坐标系下的管线 Polyline</returns>
        private Polyline BuildPipePolylineLocal(Polyline template, List<Point3d> verticesWorld, Point3d midPointWorld)
        {
            var pl = new Polyline();
            for (int i = 0; i < verticesWorld.Count; i++)
            {
                var local = new Point2d(verticesWorld[i].X - midPointWorld.X, verticesWorld[i].Y - midPointWorld.Y);
                //var local = new Point2d(verticesWorld[i].X, verticesWorld[i].Y);
                pl.AddVertexAt(i, local, 0, template.ConstantWidth, template.ConstantWidth);
            }
            pl.Layer = template.Layer;
            pl.Color = template.Color;
            pl.LineWeight = template.LineWeight;
            pl.Linetype = template.Linetype;
            pl.LinetypeScale = template.LinetypeScale;
            pl.Elevation = 0;
            pl.Normal = Vector3d.ZAxis;
            pl.Closed = false;
            return pl;
        }

        /// <summary>
        /// 新增：创建方向箭头（轮廓 + 填充）
        /// </summary>
        /// <param name="arrowLength">箭头长度</param>
        /// <param name="arrowHeight">箭头高度</param>
        /// <param name="colorIndex">颜色索引</param>
        /// <param name="pipeTemplate">管道模板</param>
        /// <returns>轮廓和填充的元组</returns>
        private (Polyline outline, Solid fill) CreateArrowTriangleFilled(double arrowLength, double arrowHeight, short colorIndex, Polyline pipeTemplate)
        {
            // 三角顶点（局部坐标，尖端朝 +X）
            var tip = new Point2d(arrowLength / 2.0, 0.0);
            var leftBottom = new Point2d(-arrowLength / 2.0, -arrowHeight / 2.0);
            var leftTop = new Point2d(-arrowLength / 2.0, arrowHeight / 2.0);

            // 轮廓
            var arrow = new Polyline();
            arrow.AddVertexAt(0, tip, 0, 0, 0);
            arrow.AddVertexAt(1, leftBottom, 0, 0, 0);
            arrow.AddVertexAt(2, leftTop, 0, 0, 0);
            arrow.Closed = true;
            arrow.Layer = pipeTemplate.Layer;
            //arrow.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex);
            //arrow.Linetype = pipeTemplate.Linetype;
            arrow.LinetypeScale = pipeTemplate.LinetypeScale;
            arrow.LineWeight = pipeTemplate.LineWeight;
            arrow.Elevation = 0;
            arrow.Normal = Vector3d.ZAxis;

            // 填充（二维实心三角形）
            var solid = new Solid(
                new Point3d(tip.X, tip.Y, 0),
                new Point3d(leftBottom.X, leftBottom.Y, 0),
                new Point3d(leftTop.X, leftTop.Y, 0),
                new Point3d(leftTop.X, leftTop.Y, 0) // 三角形第四点与第三点相同
            );
            solid.Layer = pipeTemplate.Layer;
            solid.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex);
            solid.LineWeight = pipeTemplate.LineWeight;
            solid.Normal = Vector3d.ZAxis;

            return (arrow, solid);
        }

        /// <summary>
        /// 修改：根据示例名称创建箭头（轮廓 + 填充），若示例已有模板则仍使用模板轮廓并补上填充
        /// </summary>
        /// <param name="sampleInfo">示例管道信息</param>
        /// <param name="sampleBlockName">示例块名称</param>
        /// <returns>轮廓和填充的元组</returns>
        private (Polyline outline, Solid? fill) EnsureArrowEntities(SamplePipeInfo sampleInfo, string sampleBlockName)
        {
            if (sampleInfo.DirectionArrowTemplate != null)
            {
                // 使用示例箭头模板轮廓（已移动尖端到原点）
                var arrowClone = (Polyline)sampleInfo.DirectionArrowTemplate.Clone();
                if (string.IsNullOrEmpty(arrowClone.Layer))
                    arrowClone.Layer = sampleInfo.PipeBodyTemplate.Layer;
                arrowClone.Color = sampleInfo.DirectionArrowTemplate.Color;
                arrowClone.Linetype = sampleInfo.DirectionArrowTemplate.Linetype;
                arrowClone.LinetypeScale = sampleInfo.DirectionArrowTemplate.LinetypeScale;
                arrowClone.LineWeight = sampleInfo.DirectionArrowTemplate.LineWeight;

                // 用模板箭头顶点生成一个实心填充 Solid（三角形）
                if (arrowClone.NumberOfVertices >= 3)
                {
                    var p0 = arrowClone.GetPoint3dAt(0);
                    var p1 = arrowClone.GetPoint3dAt(1);
                    var p2 = arrowClone.GetPoint3dAt(2);
                    var solid = new Solid(p0, p1, p2, p2);
                    solid.Layer = arrowClone.Layer;
                    solid.Color = arrowClone.Color;
                    solid.LineWeight = arrowClone.LineWeight;
                    solid.Normal = Vector3d.ZAxis;
                    return (arrowClone, solid);
                }
                return (arrowClone, null);
            }

            // 无模板时，自绘：出口=黄(ACI 2)，入口=绿(ACI 3)，长度=10，高=3
            var (colorIndex, length, height) = DetermineArrowStyleByName(sampleBlockName);
            var (outline, fill) = CreateArrowTriangleFilled(length, height, colorIndex, sampleInfo.PipeBodyTemplate);
            return (outline, fill);
        }

        /// <summary>
        /// 修改：块定义构建，加入额外实体（管线 + 箭头）
        /// </summary>
        /// <param name="tr">数据库事务</param>
        /// <param name="desiredName">期望的块名称</param>
        /// <param name="pipeLocal">管道轮廓</param>
        /// <param name="overlayEntities">附加实体列表（一般为箭头轮廓和填充）</param>
        /// <param name="attDefsLocal">属性定义列表</param>
        /// <returns>块定义名称</returns>
        private string BuildPipeBlockDefinition(DBTrans tr, string desiredName, Polyline pipeLocal, List<Entity> overlayEntities, List<AttributeDefinition> attDefsLocal)
        {
            string finalName = desiredName;
            int suf = 1;
            while (tr.BlockTable.Has(finalName))
                finalName = desiredName + "_PIPEGEN_" + suf++;

            tr.BlockTable.Add(
                finalName,
                btr =>
                {
                    btr.Origin = Point3d.Origin;
                },
                () =>
                {
                    var entities = new List<Entity>
                    {
                        (Polyline)pipeLocal.Clone()
                    };
                    if (overlayEntities != null)
                    {
                        foreach (var entity in overlayEntities)
                        {
                            if (entity == null)
                                continue;

                            var clone = entity.Clone() as Entity;
                            if (clone != null)
                                entities.Add(clone);
                        }
                    }
                    return entities;
                },
                () => attDefsLocal
            );

            return finalName;
        }

        /// <summary>
        /// 根据名称确定箭头样式
        /// </summary>
        /// <param name="blockName">块名称</param>
        /// <returns>箭头样式元组</returns>
        private (short colorIndex, double length, double height) DetermineArrowStyleByName(string blockName)
        {
            string nameLower = (blockName ?? string.Empty).ToLowerInvariant();
            bool isOutlet = nameLower.Contains("出口") || nameLower.Contains("outlet");
            bool isInlet = nameLower.Contains("入口") || nameLower.Contains("inlet");

            // 出口=黄色(ACI 2)，入口=绿色(ACI 3)，默认黄色
            short colorIndex = isInlet ? (short)3 : (short)2;
            if (!isInlet && !isOutlet)
            {
                colorIndex = 2;
            }

            return (colorIndex, 10.0, 3.0);
        }

        /// <summary>
        /// 创建属性定义
        /// </summary>
        /// <param name="defs">属性定义列表</param>
        /// <param name="midPointWorld">中点位置（世界坐标系）</param>
        /// <param name="finalRotation">最终旋转角度</param>
        /// <param name="pipelineLength">管道长度</param>
        /// <param name="titleFallback">管道标题后备值</param>
        /// <returns>属性定义列表</returns>
        private List<AttributeDefinition> CloneAttributeDefinitionsLocal(List<AttributeDefinition> defs, Point3d midPointWorld, double finalRotation, double pipelineLength, string titleFallback)
        {
            var result = new List<AttributeDefinition>();
            bool hasTitle = false;

            foreach (var def in defs)
            {
                var cloned = def.Clone() as AttributeDefinition;
                if (cloned == null) continue;

                // 转为局部坐标（相对中点）
                var localPos = new Point3d(def.Position.X - midPointWorld.X, def.Position.Y - midPointWorld.Y, 0);
                cloned.Position = localPos;
                cloned.Rotation = def.Rotation;
                cloned.Invisible = def.Invisible;
                cloned.Constant = def.Constant;
                cloned.Tag = def.Tag;
                cloned.TextString = def.TextString;
                cloned.Height = def.Height;

                if (!string.IsNullOrWhiteSpace(cloned.Tag))
                {
                    var tagLower = cloned.Tag.ToLowerInvariant();
                    if (tagLower.Contains("长度") || tagLower.Contains("length"))
                    {
                        double baseValue = 0.0;
                        if (double.TryParse(cloned.TextString, out double parsed)) baseValue = parsed;
                        cloned.TextString = (baseValue + pipelineLength).ToString("0.###");
                    }
                    if (string.Equals(cloned.Tag, "管道标题", StringComparison.OrdinalIgnoreCase))
                    {
                        hasTitle = true;
                        cloned.Position = Point3d.Origin;
                        cloned.Rotation = finalRotation;
                        cloned.Invisible = false;
                        if (string.IsNullOrWhiteSpace(cloned.TextString))
                            cloned.TextString = titleFallback ?? "管道";
                    }
                }

                result.Add(cloned);
            }

            if (!hasTitle)
            {
                result.Add(new AttributeDefinition
                {
                    Tag = "管道标题",
                    Position = Point3d.Origin,
                    Rotation = finalRotation,
                    TextString = string.IsNullOrWhiteSpace(titleFallback) ? "管道" : titleFallback,
                    Height = defs != null && defs.Count > 0 ? defs[0].Height : 2.5,
                    Invisible = false,
                    Constant = false
                });
            }

            return result;
        }

        /// <summary>
        /// 插入管道块
        /// </summary>
        /// <param name="tr">数据库事务</param>
        /// <param name="insertPointWorld">插入点（世界坐标）</param>
        /// <param name="blockName">块名称</param>
        /// <param name="rotation">旋转角度</param>
        /// <param name="attValues">属性值字典</param>
        /// <returns>新插入块的对象ID</returns>
        private ObjectId InsertPipeBlockWithAttributes(DBTrans tr, Point3d insertPointWorld, string blockName, double rotation, Dictionary<string, string> attValues)
        {
            ObjectId btrId = tr.BlockTable[blockName];
            ObjectId newBrId = tr.CurrentSpace.InsertBlock(insertPointWorld, btrId, rotation: rotation, atts: attValues);
            return newBrId;
        }

        /// <summary>
        /// 新增：根据首尾相连的线段集合，按连通顺序构建连续顶点列表（起点、每个连接点、终点）
        /// </summary>
        /// <param name="segments">线段集合</param>
        /// <param name="tol">容差</param>
        /// <returns></returns>
        private List<Point3d> BuildOrderedVerticesFromSegments(List<LineSegmentInfo> segments, double tol = 1e-6)
        {
            var result = new List<Point3d>();// 结果顶点列表
            if (segments == null || segments.Count == 0) return result;

            // 比较两点是否相等（使用容差）
            static bool PointsEqual(Point3d a, Point3d b, double tol)
            {
                return Math.Abs(a.X - b.X) <= tol && Math.Abs(a.Y - b.Y) <= tol && Math.Abs(a.Z - b.Z) <= tol;
            }
            // 构建唯一点列表并统计度数（出现次数）
            var uniquePoints = new List<Point3d>();
            Func<Point3d, int> getIndex = p =>
            {
                for (int i = 0; i < uniquePoints.Count; i++)
                {
                    if (PointsEqual(uniquePoints[i], p, tol)) return i;
                }
                uniquePoints.Add(p);
                return uniquePoints.Count - 1;
            };
            // 构建索引列表
            var counts = new List<int>();
            var segPairs = new List<(int s, int e)>();
            foreach (var seg in segments)
            {
                var si = getIndex(seg.StartPoint);
                var ei = getIndex(seg.EndPoint);
                segPairs.Add((si, ei));

                // ensure counts capacity
                while (counts.Count < uniquePoints.Count) counts.Add(0);
                counts[si]++;
                counts[ei]++;
            }
            // 找到链的端点：度为1的点（非闭合链）
            int startPointIndex = -1;
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i] == 1)
                {
                    startPointIndex = i;
                    break;
                }
            }
            // 若都是度 >=2（闭合回路或多分支），退回到第一个段的起点
            if (startPointIndex == -1)
            {
                startPointIndex = segPairs.Count > 0 ? segPairs[0].s : 0;
            }
            // 从 startPointIndex 开始按链遍历段
            var visited = new bool[segPairs.Count];
            Point3d current = uniquePoints[startPointIndex];
            result.Add(current);
            bool progressed;
            do
            {
                progressed = false;
                for (int i = 0; i < segPairs.Count; i++)
                {
                    if (visited[i]) continue;
                    var (si, ei) = segPairs[i];
                    if (PointsEqual(uniquePoints[si], current, tol))
                    {
                        // forward
                        var next = uniquePoints[ei];
                        if (!PointsEqual(next, result.Last(), tol))
                            result.Add(next);
                        current = next;
                        visited[i] = true;
                        progressed = true;
                        break;
                    }
                    else if (PointsEqual(uniquePoints[ei], current, tol))
                    {
                        // reverse
                        var next = uniquePoints[si];
                        if (!PointsEqual(next, result.Last(), tol))
                            result.Add(next);
                        current = next;
                        visited[i] = true;
                        progressed = true;
                        break;
                    }
                }
            } while (progressed);

            // 新增校验：确保最终的方向与线段聚合方向一致
            try
            {
                if (result.Count >= 2)
                {
                    var overallVec = result.Last() - result.First();
                    if (!overallVec.IsZeroLength())
                    {
                        var agg = ComputeAggregateSegmentDirection(segments);
                        if (!agg.IsZeroLength())
                        {
                            // 如果总体向量与聚合向量点积为负，则反转顶点顺序
                            if (overallVec.DotProduct(agg) < 0)
                            {
                                result.Reverse();
                            }
                        }
                    }
                }
            }
            catch
            {
                // 容错：若聚合计算失败，不影响已有顺序
            }
            return result;
        }

        /// <summary>
        /// 分析示例块，提取管道、箭头和属性信息
        /// </summary>
        private SamplePipeInfo AnalyzeSampleBlock(Transaction tr, BlockReference blockRef)
        {
            //获取块的属性 初始化结果对象
            var info = new SamplePipeInfo();
            // 打开块表记录
            var btr = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);
            info.BasePoint = btr.Origin;//获取块的基点
            //遍历块中的对象 遍历块中的所有实体
            List<Polyline> polylines = new List<Polyline>();

            foreach (ObjectId id in btr)
            {
                //获取对象
                var dbObj = tr.GetObject(id, OpenMode.ForRead);
                if (dbObj is Polyline pl)
                {
                    //创建副本收集所有多段线
                    polylines.Add(pl.Clone() as Polyline);
                }
                else if (dbObj is AttributeDefinition attDef)//属性定义获取属性定义
                {
                    //创建副本添加属性定义到结果对象 添加属性定义到结果对象的属性定义列表
                    info.AttributeDefinitions.Add(attDef.Clone() as AttributeDefinition);
                }
            }
            //创建结果对象 分析多段线以识别管道主体和方向箭头
            if (polylines.Count == 0) return info;

            // 假设最长的Polyline是管道主体
            polylines = polylines.OrderByDescending(p => p.Length).ToList();
            info.PipeBodyTemplate = polylines[0];//设置管道主体模板设置管道主体模板为最长的多段线

            // 假设闭合的、有3个顶点的Polyline是方向箭头
            info.DirectionArrowTemplate = polylines.FirstOrDefault(p => p.Closed && p.NumberOfVertices == 3);

            if (info.DirectionArrowTemplate != null)
            {
                //获取箭头尖端的点 将箭头移动到原点，便于后续变换
                Point3d arrowTip = info.DirectionArrowTemplate.GetPoint3dAt(0);//获取箭头尖端的点假设第一个顶点是箭头尖端
                //创建一个矩阵，将箭头移动到原点创建变换矩阵将箭头移动到原点 创建变换矩阵将箭头移动到原点
                Matrix3d toOrigin = Matrix3d.Displacement(Point3d.Origin - arrowTip);
                //将箭头移动到原点应用变换 将变换应用到箭头模板上
                info.DirectionArrowTemplate.TransformBy(toOrigin);
            }

            return info;
        }

        /// <summary>
        /// 从选择的ObjectId集合中收集所有线段信息
        /// </summary>
        private List<LineSegmentInfo> CollectLineSegments(Transaction tr, List<ObjectId> ids)
        {
            var segments = new List<LineSegmentInfo>();
            foreach (var id in ids)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead);
                if (ent is Line line)
                {
                    segments.Add(ProcessLine(line, tr));
                }
                else if (ent is Polyline pl)
                {
                    for (int i = 0; i < pl.NumberOfVertices - 1; i++)
                    {
                        if (pl.GetSegmentType(i) == SegmentType.Line)
                        {
                            var p1 = pl.GetPoint3dAt(i);
                            var p2 = pl.GetPoint3dAt(i + 1);
                            var vec = p2 - p1;
                            segments.Add(new LineSegmentInfo
                            {
                                StartPoint = p1,
                                EndPoint = p2,
                                Length = vec.Length,
                                Angle = vec.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis),
                                Layer = pl.Layer,
                                ColorIndex = pl.ColorIndex,
                                LinetypeScale = pl.LinetypeScale,
                                EntityType = "POLYLINE_SEGMENT"
                            });
                        }
                    }
                }
            }
            return segments;
        }

        #endregion

        #region 操作动态块的方法

        /// <summary>
        /// 移动动态块到新位置
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <param name="newPosition">新位置坐标</param>
        public static void MoveBlock(BlockReference blockRef, Point3d newPosition)
        {
            // 获取当前数据库
            Database db = blockRef.Database;

            // 开始事务
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 打开块参照进行写操作
                    BlockReference br = trans.GetObject(blockRef.ObjectId, OpenMode.ForWrite) as BlockReference;

                    // 计算移动向量
                    Vector3d moveVector = newPosition - br.Position;

                    // 创建移动矩阵
                    Matrix3d moveMatrix = Matrix3d.Displacement(moveVector);

                    // 应用变换
                    br.TransformBy(moveMatrix);

                    // 提交事务
                    trans.Commit();

                    // 输出信息
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n动态块已移动到: {newPosition}");
                }
                catch (Exception ex)
                {
                    // 错误处理
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n移动块时发生错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 旋转动态块
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <param name="rotationAngle">旋转角度（弧度）</param>
        /// <param name="basePoint">旋转基点（可选，默认为块插入点）</param>
        public static void RotateBlock(BlockReference blockRef, double rotationAngle, Point3d? basePoint = null)
        {
            // 获取当前数据库
            Database db = blockRef.Database;

            // 开始事务
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 打开块参照进行写操作
                    BlockReference? br = trans.GetObject(blockRef.ObjectId, OpenMode.ForWrite) as BlockReference;

                    // 确定旋转基点
                    Point3d rotationBase = basePoint ?? br.Position;

                    // 创建旋转矩阵
                    Matrix3d rotationMatrix = Matrix3d.Rotation(rotationAngle, Vector3d.ZAxis, rotationBase);

                    // 应用变换
                    br.TransformBy(rotationMatrix);

                    // 提交事务
                    trans.Commit();

                    // 输出信息
                    double angleDegrees = rotationAngle * 180 / Math.PI;
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n动态块已旋转 {angleDegrees:F2} 度");
                }
                catch (Exception ex)
                {
                    // 错误处理
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n旋转块时发生错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 拉伸动态块到指定的端点
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <param name="newStartPoint">新的起点坐标</param>
        /// <param name="newEndPoint">新的终点坐标</param>
        public static void StretchBlock(BlockReference blockRef, Point3d newStartPoint, Point3d newEndPoint)
        {
            // 获取当前数据库和编辑器
            Database db = blockRef.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // 开始事务
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 打开块参照进行写操作
                    BlockReference br = trans.GetObject(blockRef.ObjectId, OpenMode.ForWrite) as BlockReference;

                    // 获取当前端点
                    var (currentStartPoint, currentEndPoint) = GetEndPoints(br);

                    // 计算当前方向向量
                    Vector3d currentDirection = (currentEndPoint - currentStartPoint).GetNormal();

                    // 计算拉伸距离
                    double startStretchDistance = (newStartPoint - currentStartPoint).DotProduct(currentDirection);
                    double endStretchDistance = (newEndPoint - currentEndPoint).DotProduct(currentDirection);

                    // 查找并设置动态块参数
                    DynamicBlockReferencePropertyCollection props = br.DynamicBlockReferencePropertyCollection;

                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                        // 输出所有参数名称（用于调试）
                        ed.WriteMessage($"\n找到参数: {prop.PropertyName}");

                        // 设置拉伸参数
                        if (prop.PropertyName.Contains("Distance") ||
                            prop.PropertyName.Contains("Length") ||
                            prop.PropertyName.Contains("Stretch"))
                        {
                            // 计算新的总长度
                            double newLength = currentStartPoint.DistanceTo(currentEndPoint) +
                                             Math.Abs(startStretchDistance) +
                                             Math.Abs(endStretchDistance);

                            // 尝试设置参数值
                            try
                            {
                                prop.Value = newLength;
                                ed.WriteMessage($"\n设置参数 {prop.PropertyName} 为: {newLength}");
                            }
                            catch
                            {
                                ed.WriteMessage($"\n无法设置参数 {prop.PropertyName}");
                            }
                        }
                    }

                    // 提交事务
                    trans.Commit();

                    ed.WriteMessage($"\n动态块拉伸完成");
                }
                catch (Exception ex)
                {
                    // 错误处理
                    ed.WriteMessage($"\n拉伸块时发生错误: {ex.Message}");
                }
            }
        }

        #endregion

        #region 命令方法示例


        /// <summary>
        /// 测试命令 - 获取动态块信息
        /// </summary>
        [CommandMethod("TestGetBlockInfo")]
        public static void TestGetBlockInfo()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 选择块参照
            PromptEntityOptions opts = new PromptEntityOptions("\n选择动态块:");
            opts.SetRejectMessage("\n请选择块参照.");
            opts.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult result = ed.GetEntity(opts);
            if (result.Status != PromptStatus.OK) return;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 获取块参照
                BlockReference blockRef = trans.GetObject(result.ObjectId, OpenMode.ForRead) as BlockReference;

                // 获取块信息
                DynamicBlockInfo info = GetBlockInfo(blockRef);

                // 显示信息
                ed.WriteMessage($"\n===== 动态块信息 =====");
                ed.WriteMessage($"\n起点: {info.StartPoint}");
                ed.WriteMessage($"\n终点: {info.EndPoint}");
                ed.WriteMessage($"\n中点: {info.MidPoint}");
                ed.WriteMessage($"\n长度: {info.Length:F3}");
                ed.WriteMessage($"\n旋转角度: {info.Rotation * 180 / Math.PI:F2} 度");
                ed.WriteMessage($"\n插入点: {info.Position}");

                trans.Commit();
            }
        }

        /// <summary>
        /// 测试命令 - 移动动态块
        /// </summary>
        [CommandMethod("TestMoveBlock")]
        public static void TestMoveBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 选择块参照
            PromptEntityOptions opts = new PromptEntityOptions("\n选择要移动的动态块:");
            opts.SetRejectMessage("\n请选择块参照.");
            opts.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult result = ed.GetEntity(opts);
            if (result.Status != PromptStatus.OK) return;

            // 选择新位置
            PromptPointOptions pointOpts = new PromptPointOptions("\n选择新位置:");
            PromptPointResult pointResult = ed.GetPoint(pointOpts);
            if (pointResult.Status != PromptStatus.OK) return;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 获取块参照
                BlockReference blockRef = trans.GetObject(result.ObjectId, OpenMode.ForRead) as BlockReference;

                // 移动块
                MoveBlock(blockRef, pointResult.Value);

                trans.Commit();
            }
        }

        #endregion

        #region 动态块拉伸

        /// <summary>
        /// 绘制管线动态块
        /// </summary>
        [CommandMethod("Draw_GD_PipeLine_DynamicBlock")]
        public void Draw_GD_PipeLine_DynamicBlock()
        {
            // 获取当前文档和数据库
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 开始事务
            using (var tr = new DBTrans())
            {
                try
                {
                    ObjectId insertBlockObjectId = new ObjectId();
                    //插入块
                    Command.GB_InsertBlock_(new Point3d(0, 0, 0), 0, ref insertBlockObjectId);

                    if (insertBlockObjectId == new ObjectId())
                    {
                        Env.Editor.WriteMessage("未找到块");
                        return;
                    }
                    ;
                    // 打开块参照
                    BlockReference? blockRef = tr.GetObject(insertBlockObjectId, OpenMode.ForWrite) as BlockReference;

                    if (blockRef == null)
                    {
                        doc.Editor.WriteMessage("\n没有找到有效的块参照.");
                        return;
                    }
                    // 第二步：提示用户指定起点
                    PromptPointOptions startPointOpts = new PromptPointOptions("\n请指定拉伸的起点: ");
                    startPointOpts.AllowNone = false; // 不允许直接回车
                    PromptPointResult startPointResult = ed.GetPoint(startPointOpts);
                    if (startPointResult.Status != PromptStatus.OK)
                    {
                        Env.Editor.WriteMessage("\n未指定起点,取消绘制管道");
                        tr.BlockTable.Remove(insertBlockObjectId);
                        return;
                    }

                    // 第三步：提示用户指定终点（使用橡皮筋效果）
                    PromptPointOptions endPointOpts = new PromptPointOptions("\n请指定拉伸的终点: ");
                    endPointOpts.BasePoint = startPointResult.Value; // 设置基点为起点
                    endPointOpts.UseBasePoint = true; // 启用基点
                    endPointOpts.UseDashedLine = true; // 使用虚线显示橡皮筋效果
                    PromptPointResult endPointResult = ed.GetPoint(endPointOpts);
                    if (endPointResult.Status != PromptStatus.OK)
                    {
                        Env.Editor.WriteMessage("\n未指定终点,取消绘制管道");

                        tr.BlockTable.Remove(insertBlockObjectId);
                        return;
                    }
                    //拿到新的起点和终点
                    Point3d newStartPoint = startPointResult.Value;
                    Point3d newEndPoint = endPointResult.Value;
                    double newAngle = 0;
                    if (newEndPoint.X == newStartPoint.X)
                        newAngle = -Math.Atan2(newEndPoint.Y - newStartPoint.Y, newEndPoint.X - newStartPoint.X);
                    if (newEndPoint.Y == newStartPoint.Y)
                        newAngle = Math.Atan2(newEndPoint.Y - newStartPoint.Y, newEndPoint.X - newStartPoint.X);

                    // 确保事务处于写入模式
                    if (!blockRef.IsWriteEnabled) blockRef.UpgradeOpen();

                    DynamicBlockReferencePropertyCollection dynProps = blockRef.DynamicBlockReferencePropertyCollection;

                    //如果起点和终点相同，则退出
                    if (newStartPoint == newEndPoint)
                    {
                        doc.Editor.WriteMessage("\n起点和终点不能相同.");
                        return;
                    }
                    //else if (newStartPoint.X < newEndPoint.X)//如果起点在终点的右侧，则交换起点和终点
                    //{
                    //    newStartPoint = endPointResult.Value;
                    //    newEndPoint = startPointResult.Value;
                    //}
                    //else if (Convert.ToInt32(newStartPoint.X) == Convert.ToInt32(newEndPoint.X) && newStartPoint.Y > newEndPoint.Y)//如果起点在终点的下方，则交换起点和终点
                    //{
                    //    //newStartPoint = endPointResult.Value;
                    //    //newEndPoint = startPointResult.Value;
                    //    newStartPoint = startPointResult.Value;
                    //    newEndPoint = endPointResult.Value;
                    //}
                    if (Convert.ToInt32(newStartPoint.X) == Convert.ToInt32(newEndPoint.X))//如果起点和终点在同一X方向，则移动块到Y轴方向
                    {
                        //移动块
                        MoveBlock(blockRef, new Point3d(newStartPoint.X, (newStartPoint.Y + newEndPoint.Y) / 2, 0));
                        // 设置角度
                        SetDynamicBlockNewAngle(dynProps, newAngle, ed);

                    }
                    else if (Convert.ToInt32(newStartPoint.Y) == Convert.ToInt32(newEndPoint.Y))
                    {
                        //移动块
                        MoveBlock(blockRef, new Point3d((newStartPoint.X + newEndPoint.X) / 2, newStartPoint.Y, 0));
                        // 设置角度
                        SetDynamicBlockNewAngle(dynProps, newAngle, ed);
                    }
                    // 打开块表记录
                    BlockTableRecord? blockTableRecord = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                    // 查找多段线（Polyline）
                    Polyline polyline = new Polyline();
                    if (blockTableRecord != null)
                        foreach (ObjectId entId in blockTableRecord)
                        {
                            Entity? ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                            if (ent != null && ent is Polyline pl)
                            {
                                polyline = pl;
                                break;
                            }
                        }

                    if (polyline == null)
                    {
                        doc.Editor.WriteMessage("\n在动态块中没有找到多段线.");
                        return;
                    }
                    // 获取原始多段线的起点和终点（在块局部坐标系中）
                    var originalStartPoint = polyline.GetPoint3dAt(0);
                    var originalEndPoint = polyline.GetPoint3dAt(polyline.NumberOfVertices - 1);

                    // 转换为世界坐标系
                    var worldStartPoint = blockRef.BlockTransform * originalStartPoint;
                    var worldEndPoint = blockRef.BlockTransform * originalEndPoint;

                    // 计算多段线的原始方向向量
                    Vector3d originalDirection = (worldEndPoint - worldStartPoint).GetNormal();
                    //计算拉伸距离（投影到原始多段线方向）
                    //如果是向右拉伸，则拉伸距离为负数；如果向左拉伸，则拉伸距离为正数
                    var startStretchDistance = (newStartPoint - worldStartPoint).DotProduct(originalDirection);

                    //如果向左拉伸，则拉伸距离为负数；如果向右拉伸，则拉伸距离为正数
                    var endStretchDistance = (newEndPoint - worldEndPoint).DotProduct(originalDirection);

                    // 设置起始点与终点
                    SetDynamicBlockEndPoints(dynProps, startStretchDistance, endStretchDistance, ed);

                    // 提交事务
                    tr.Commit();
                    Env.Editor.Redraw();
                }
                catch (Exception ex)
                {
                    // 处理可能的异常
                    doc.Editor.WriteMessage($"\n发生错误: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// 动态块拉伸
        /// </summary>
        [CommandMethod("StretchDynamicBlockPolyline")]
        public void StretchDynamicBlockPolyline()
        {
            // 获取当前文档和数据库
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 开始事务
            using (var tr = new DBTrans())
            {
                try
                {
                    // 提示用户选择动态块参照
                    PromptEntityOptions promptOpts = new PromptEntityOptions("\n选择要拉伸的动态块:");
                    promptOpts.SetRejectMessage("\n请选择一个块参照.");
                    promptOpts.AddAllowedClass(typeof(BlockReference), true);
                    PromptEntityResult result = doc.Editor.GetEntity(promptOpts);

                    // 如果用户取消选择，则退出
                    if (result.Status != PromptStatus.OK)
                        return;

                    // 打开块参照
                    BlockReference? blockRef = tr.GetObject(result.ObjectId, OpenMode.ForWrite) as BlockReference;

                    if (blockRef == null)
                    {
                        doc.Editor.WriteMessage("\n没有找到有效的块参照.");
                        return;
                    }
                    // 第二步：提示用户指定起点
                    PromptPointOptions startPointOpts = new PromptPointOptions("\n请指定拉伸的起点: ");
                    startPointOpts.AllowNone = false; // 不允许直接回车
                    PromptPointResult startPointResult = ed.GetPoint(startPointOpts);
                    if (startPointResult.Status != PromptStatus.OK)
                        return;
                    // 第三步：提示用户指定终点（使用橡皮筋效果）
                    PromptPointOptions endPointOpts = new PromptPointOptions("\n请指定拉伸的终点: ");
                    endPointOpts.BasePoint = startPointResult.Value; // 设置基点为起点
                    endPointOpts.UseBasePoint = true; // 启用基点
                    endPointOpts.UseDashedLine = true; // 使用虚线显示橡皮筋效果
                    PromptPointResult endPointResult = ed.GetPoint(endPointOpts);
                    if (endPointResult.Status != PromptStatus.OK)
                        return;

                    //拿到新的起点和终点
                    Point3d newStartPoint = startPointResult.Value;
                    Point3d newEndPoint = endPointResult.Value;
                    double newAngle = Math.Atan2(newEndPoint.Y - newStartPoint.Y, newEndPoint.X - newStartPoint.X);
                    // 创建新块参照的克隆
                    BlockReference newBlockRef = (BlockReference)blockRef.Clone();

                    // 添加到当前空间
                    var newBlockRefObjectId = tr.CurrentSpace.AddEntity(newBlockRef);

                    // 确保事务处于写入模式
                    if (!newBlockRef.IsWriteEnabled) newBlockRef.UpgradeOpen();

                    DynamicBlockReferencePropertyCollection dynProps = newBlockRef.DynamicBlockReferencePropertyCollection;

                    //如果起点和终点相同，则退出
                    if (newStartPoint == newEndPoint)
                    {
                        doc.Editor.WriteMessage("\n起点和终点不能相同.");
                        return;
                    }
                    else if (newStartPoint.X > newEndPoint.X)//如果起点在终点的右侧，则交换起点和终点
                    {
                        newStartPoint = endPointResult.Value;
                        newEndPoint = startPointResult.Value;
                    }
                    else if (Convert.ToInt16(newStartPoint.X) == Convert.ToInt16(newEndPoint.X) && newStartPoint.Y > newEndPoint.Y)//如果起点在终点的下方，则交换起点和终点
                    {
                        newStartPoint = endPointResult.Value;
                        newEndPoint = startPointResult.Value;
                    }
                    if (Convert.ToInt16(newStartPoint.X) == Convert.ToInt16(newEndPoint.X))//如果起点和终点在同一X方向，则移动块到Y轴方向
                    {
                        //移动块
                        MoveBlock(newBlockRef, new Point3d(newStartPoint.X, (newStartPoint.Y + newEndPoint.Y) / 2, 0));
                        // 设置角度
                        SetDynamicBlockNewAngle(dynProps, newAngle, ed);

                    }
                    else if (Convert.ToInt16(newStartPoint.Y) == Convert.ToInt16(newEndPoint.Y))
                    {
                        //移动块
                        MoveBlock(newBlockRef, new Point3d((newStartPoint.X + newEndPoint.X) / 2, newStartPoint.Y, 0));
                    }
                    // 打开块表记录
                    BlockTableRecord? blockTableRecord = tr.GetObject(newBlockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                    // 查找多段线（Polyline）
                    Polyline polyline = new Polyline();
                    if (blockTableRecord != null)
                        foreach (ObjectId entId in blockTableRecord)
                        {
                            Entity? ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                            if (ent != null && ent is Polyline pl)
                            {
                                polyline = pl;
                                break;
                            }
                        }

                    if (polyline == null)
                    {
                        doc.Editor.WriteMessage("\n在动态块中没有找到多段线.");
                        return;
                    }
                    // 获取原始多段线的起点和终点（在块局部坐标系中）
                    var originalStartPoint = polyline.GetPoint3dAt(0);
                    var originalEndPoint = polyline.GetPoint3dAt(polyline.NumberOfVertices - 1);

                    // 转换为世界坐标系
                    var worldStartPoint = newBlockRef.BlockTransform * originalStartPoint;
                    var worldEndPoint = newBlockRef.BlockTransform * originalEndPoint;

                    // 计算多段线的原始方向向量
                    Vector3d originalDirection = (worldEndPoint - worldStartPoint).GetNormal();
                    //计算拉伸距离（投影到原始多段线方向）
                    //如果是向右拉伸，则拉伸距离为负数；如果向左拉伸，则拉伸距离为正数
                    var startStretchDistance = (newStartPoint - worldStartPoint).DotProduct(originalDirection);

                    //如果向左拉伸，则拉伸距离为负数；如果向右拉伸，则拉伸距离为正数
                    var endStretchDistance = (newEndPoint - worldEndPoint).DotProduct(originalDirection);

                    // 设置起始点与终点
                    SetDynamicBlockEndPoints(dynProps, startStretchDistance, endStretchDistance, ed);

                    // 提交事务
                    tr.Commit();
                    Env.Editor.Redraw();
                }
                catch (Exception ex)
                {
                    // 处理可能的异常
                    doc.Editor.WriteMessage($"\n发生错误: {ex.Message}");
                }
            }
        }

        #endregion

        /*
         SyncPipeProperties
         */

        #region 绘制管道线

        // 新增：通过点击采集点并生成管道块（入口/出口两种命令）
        // 放在类 EquipmentTableGenerator 的任意合适位置（例如 SyncPipeProperties 方法附近）
        [CommandMethod("DrawOutletPipeByClicks")]
        public void DrawOutletPipeByClicks()
        {
            DrawPipeByClicks(isOutlet: true);
        }

        [CommandMethod("DrawInletPipeByClicks")]
        public void DrawInletPipeByClicks()
        {
            DrawPipeByClicks(isOutlet: false);
        }

        /// <summary>
        /// 主实现：交互式采点并基于示例块生成管道块
        /// 说明：Polyline pipeLocal = BuildPipePolylineLocal
        ///— 1) 首先要求用户选择一个示例管道块（作为模板/属性来源）；
        ///— 2) 交互式点击多个点（每次点击会记录坐标），按 Esc 或在提示阶段取消结束采点；
        ///— 3) 采集结束后用示例块生成管道块（复用现有的 Clone/Build/Insert 逻辑）。
        /// </summary>
        private void DrawPipeByClicks(bool isOutlet)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // 先尝试在块表中寻找示例块（优先按名称包含“出口/出口/outlet/入口/inlet”）
                ObjectId sampleBtrId = ObjectId.Null;
                string sampleBtrName = string.Empty;
                using (var tx = db.TransactionManager.StartTransaction())
                {
                    // 遍历块表
                    var bt = (BlockTable)tx.GetObject(db.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId btrId in bt)// 遍历块表
                    {
                        try
                        {
                            var btr = tx.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                            if (btr == null) continue;
                            var nm = (btr.Name ?? string.Empty).ToLowerInvariant();
                            if (isOutlet)
                            {
                                if (nm.Contains("出口") || nm.Contains("outlet"))
                                {
                                    sampleBtrId = btrId;
                                    sampleBtrName = btr.Name;
                                    break;
                                }
                            }
                            else
                            {
                                if (nm.Contains("入口") || nm.Contains("inlet"))
                                {
                                    sampleBtrId = btrId;
                                    sampleBtrName = btr.Name;
                                    break;
                                }
                            }
                        }
                        catch { /* 忽略单条读取错误 */ }
                    }
                    tx.Commit();
                }

                bool tempInserted = false;
                BlockReference sampleBr = null;

                using (var tr = new DBTrans())
                {
                    try
                    {
                        // 如果在块表里找到了合适的样例块定义，先把它插入到当前空间（临时），位置用图纸原点
                        if (!sampleBtrId.IsNull)
                        {
                            Point3d insertPoint = Point3d.Origin;
                            // 插入到当前空间
                            ObjectId tempSampleBrId = tr.CurrentSpace.InsertBlock(insertPoint, sampleBtrId, rotation: 0.0, atts: null);
                            sampleBr = tr.GetObject(tempSampleBrId, OpenMode.ForWrite) as BlockReference;
                            tempInserted = true;
                        }
                        else
                        {
                            // 未找到自动样例块，回退到用户选择示例块（原有流程）
                            ed.WriteMessage("\n未在块表中找到自动样例块，请手动选择示例块作为模板。");
                            var peo = new PromptEntityOptions("\n请选择示例管线块（作为模板，用于属性与样式）:");
                            peo.SetRejectMessage("\n请选择块参照对象.");
                            peo.AddAllowedClass(typeof(BlockReference), true);
                            var per = ed.GetEntity(peo);
                            if (per.Status != PromptStatus.OK)
                            {
                                ed.WriteMessage("\n未选择示例块，取消操作。");
                                tr.Abort();
                                return;
                            }
                            sampleBr = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;
                            // ensure writable for attribute write later
                            if (sampleBr != null && !sampleBr.IsWriteEnabled) sampleBr.UpgradeOpen();
                            tempInserted = false;
                        }

                        if (sampleBr == null)
                        {
                            ed.WriteMessage("\n无法获取示例块参照，取消操作。");
                            tr.Abort();
                            return;
                        }

                        // 读取示例块属性并弹出属性编辑窗体（在插入后立即编辑）
                        // 读取示例块属性
                        var sampleAttrMap = GetEntityAttributeMap(tr, sampleBr) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        // 加载上次保存的属性（入口/出口区分）
                        var loadedAttrs = FileManager.LoadLastPipeAttributes(isOutlet);

                        // 使用 FileManager 提供的合并方法（示例优先，但示例为空/占位时使用历史值；历史新增键也会加入）
                        //var mergedForEditor = FileManager.MergeSavedPipeAttributes(sampleAttrMap, savedAttrs);

                        //// 可选调试：命令行输出
                        //try
                        //{
                        //    ed.WriteMessage($"\n[Debug] LoadLastPipeAttributes 返回 {savedAttrs.Count} 项，传给编辑窗 {mergedForEditor.Count} 项");
                        //}
                        //catch { }

                        // 弹出属性编辑窗，传入合并结果
                        using (var editor = new PipeAttributeEditorForm(loadedAttrs))
                        {
                            var dr = editor.ShowDialog();
                            if (dr != DialogResult.OK)
                            {
                                // 用户取消属性编辑，移除临时插入的样例块并退出
                                if (tempInserted)
                                {
                                    try
                                    {
                                        var brToDel = tr.GetObject(sampleBr.ObjectId, OpenMode.ForWrite) as BlockReference;
                                        brToDel?.Erase();
                                    }
                                    catch { }
                                }
                                tr.Abort();
                                ed.WriteMessage("\n已取消属性编辑，操作终止。");
                                return;
                            }

                            // 用户确认后获取编辑结果并保存为默认供下次使用
                            var editedAttrs = editor.Attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            FileManager.SaveLastPipeAttributes(isOutlet, editedAttrs);

                            // 把用户修改后的属性写回示例块（只写存在的 AttributeReference）
                            try
                            {
                                var sampleBrWrite = tr.GetObject(sampleBr.ObjectId, OpenMode.ForWrite) as BlockReference;
                                if (sampleBrWrite != null)
                                {
                                    foreach (ObjectId aid in sampleBrWrite.AttributeCollection)
                                    {
                                        try
                                        {
                                            var ar = tr.GetObject(aid, OpenMode.ForWrite) as AttributeReference;
                                            if (ar == null || string.IsNullOrWhiteSpace(ar.Tag)) continue;
                                            if (editedAttrs.TryGetValue(ar.Tag, out var newVal))
                                            {
                                                ar.TextString = newVal ?? string.Empty;
                                                try { ar.AdjustAlignment(db); } catch { }
                                            }
                                        }
                                        catch { /* 单个属性写回失败不阻塞整体 */ }
                                    }
                                }
                            }
                            catch { /* 忽略写回失败 */ }
                        }
                        // 属性编辑完成后，开始采点（用户使用左键点击采点，空格/右键/ESC结束）
                        var points = new List<Point3d>();
                        var firstOpts = new PromptPointOptions("\n指定起点（点击或输入坐标）:");
                        firstOpts.AllowNone = false;
                        var firstRes = ed.GetPoint(firstOpts);
                        if (firstRes.Status != PromptStatus.OK)
                        {
                            ed.WriteMessage("\n未指定起点，取消操作。");
                            // 清理临时样例块
                            if (tempInserted)
                            {
                                try { var brToDel = tr.GetObject(sampleBr.ObjectId, OpenMode.ForWrite) as BlockReference; brToDel?.Erase(); }
                                catch { }
                            }
                            tr.Abort();
                            return;
                        }
                        points.Add(firstRes.Value);

                        while (true)
                        {
                            var nextOpts = new PromptPointOptions("\n指定下一个点（点击或输入坐标），按 Esc/取消结束：");
                            nextOpts.UseBasePoint = true;
                            nextOpts.BasePoint = points.Last();
                            nextOpts.AllowNone = false;
                            var nextRes = ed.GetPoint(nextOpts);
                            if (nextRes.Status != PromptStatus.OK)
                                break;
                            var pt = nextRes.Value;
                            if (pt.IsEqualTo(points.Last())) break;
                            points.Add(pt);
                        }

                        if (points.Count < 2)
                        {
                            ed.WriteMessage("\n采集点不足（至少需要两个点），取消生成。");
                            // 清理临时样例块
                            if (tempInserted)
                            {
                                try { var brToDel = tr.GetObject(sampleBr.ObjectId, OpenMode.ForWrite) as BlockReference; brToDel?.Erase(); }
                                catch { }
                            }
                            tr.Abort();
                            return;
                        }

                        // 使用示例块（sampleBr）生成管道块（与以前逻辑一致）
                        var sampleInfo = AnalyzeSampleBlock(tr, sampleBr);
                        if (sampleInfo?.PipeBodyTemplate == null)
                        {
                            ed.WriteMessage("\n示例块中未找到管道主体（PolyLine），无法生成管道。");
                            // 清理临时样例块
                            if (tempInserted)
                            {
                                try { var brToDel = tr.GetObject(sampleBr.ObjectId, OpenMode.ForWrite) as BlockReference; brToDel?.Erase(); }
                                catch { }
                            }
                            tr.Abort();
                            return;
                        }

                        // 计算管道总长度与中点
                        double pipelineLength = 0.0;
                        for (int i = 0; i < points.Count - 1; i++)
                            pipelineLength += points[i].DistanceTo(points[i + 1]);

                        var (midPoint, midAngle) = ComputeMidPointAndAngle(points, pipelineLength);
                        Vector3d targetDir = ComputeDirectionAtPoint(points, midPoint, 1e-6);

                        Vector3d segmentDir = ComputeAggregateSegmentDirection(BuildOrderedLineSegmentsFromPoints(points));
                        if (!segmentDir.IsZeroLength() && targetDir.DotProduct(segmentDir) < 0)
                            targetDir = -targetDir;
                        Vector3d targetDirNormalized = targetDir.IsZeroLength() ? Vector3d.XAxis : targetDir.GetNormal();

                        // 局部 polyline（以 midPoint 为基准）
                        var pipeLocal = BuildPipePolylineLocal(sampleInfo.PipeBodyTemplate, points, midPoint);

                        // 重新读取示例块属性（以保证使用用户已编辑的值）
                        var latestSampleAttrs = GetEntityAttributeMap(tr, sampleBr) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        string pipeTitle = latestSampleAttrs.TryGetValue("管道标题", out var sampleTitle) && !string.IsNullOrWhiteSpace(sampleTitle)
                                         ? sampleTitle
                                         : (sampleBr.IsDynamicBlock ? sampleBr.Name : sampleBr.Name) ?? "管道";

                        // 为每段生成局部坐标下的箭头与标题（相对于 midPoint），仅使用分段箭头/文字
                        var arrowEntities = CreateDirectionalArrowsAndTitles(tr, sampleInfo, points, midPoint, pipeTitle, sampleBr.Name);

                        // 复制属性定义并写入最新属性值
                        var attDefsLocal = CloneAttributeDefinitionsLocal(sampleInfo.AttributeDefinitions, midPoint, 0.0, pipelineLength, sampleBr.Name)
                                            ?? new List<AttributeDefinition>();

                        double attHeight = attDefsLocal.Count > 0 ? attDefsLocal[0].Height : 2.5;
                        double yOffsetBase = attDefsLocal.Count > 0 ? attDefsLocal[0].Position.Y - attHeight * 1.2 : -attHeight * 2.0;
                        int extraIndex = 0;
                        foreach (var kv in latestSampleAttrs)
                        {
                            if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                            var existing = attDefsLocal.FirstOrDefault(a => string.Equals(a.Tag, kv.Key, StringComparison.OrdinalIgnoreCase));
                            if (existing != null)
                            {
                                existing.TextString = kv.Value ?? string.Empty;
                                existing.Invisible = false;
                                existing.Constant = false;
                            }
                            else
                            {
                                attDefsLocal.Add(new AttributeDefinition
                                {
                                    Tag = kv.Key,
                                    Position = new Point3d(0, yOffsetBase - extraIndex * attHeight * 1.2, 0),
                                    Rotation = 0.0,
                                    TextString = kv.Value ?? string.Empty,
                                    Height = attHeight,
                                    Invisible = false,
                                    Constant = false
                                });
                                extraIndex++;
                            }
                        }

                        // 确保始点/终点/管段号属性
                        string startCoordStr = $"X={points.First().X:F3},Y={points.First().Y:F3}";
                        string endCoordStr = $"X={points.Last().X:F3},Y={points.Last().Y:F3}";
                        SetOrAddAttr(attDefsLocal, "始点", startCoordStr, ref extraIndex, yOffsetBase, attHeight);
                        SetOrAddAttr(attDefsLocal, "终点", endCoordStr, ref extraIndex, yOffsetBase, attHeight);
                        int nextSegNum = GetNextPipeSegmentNumber(db);
                        string extractedPipeNo = string.Empty;
                        if (latestSampleAttrs.TryGetValue("管道标题", out var titleFromSample) && !string.IsNullOrWhiteSpace(titleFromSample))
                            extractedPipeNo = ExtractPipeCodeFromTitle(titleFromSample);
                        if (string.IsNullOrWhiteSpace(extractedPipeNo))
                        {
                            if (latestSampleAttrs.TryGetValue("管段号", out var pn) && !string.IsNullOrWhiteSpace(pn))
                                extractedPipeNo = pn;
                        }
                        if (string.IsNullOrWhiteSpace(extractedPipeNo))
                            extractedPipeNo = nextSegNum.ToString("D4");
                        SetOrAddAttr(attDefsLocal, "管段号", extractedPipeNo, ref extraIndex, yOffsetBase, attHeight);

                        // 在构建块定义之前：移除/隐藏中点“管道标题”
                        attDefsLocal.RemoveAll(ad => string.Equals(ad.Tag, "管道标题", StringComparison.OrdinalIgnoreCase));
                        foreach (var ad in attDefsLocal)
                        {
                            ad.Invisible = true;
                            ad.Constant = false;
                        }

                        // 构建块定义并插入新块
                        string desiredName = sampleBr.Name;
                        string newBlockName = BuildPipeBlockDefinition(tr, desiredName, (Polyline)pipeLocal.Clone(), arrowEntities, attDefsLocal);

                        var attValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var a in attDefsLocal)
                        {
                            if (string.IsNullOrWhiteSpace(a?.Tag)) continue;
                            attValues[a.Tag] = a.TextString ?? string.Empty;
                        }

                        var newBrId = InsertPipeBlockWithAttributes(tr, midPoint, newBlockName, 0.0, attValues);
                        var newBr = tr.GetObject(newBrId, OpenMode.ForWrite) as BlockReference;
                        if (newBr != null)
                            newBr.Layer = sampleInfo.PipeBodyTemplate.Layer;

                        // 清理：如果之前我们临时插入了样例块，删除它（不影响已创建的管线块）
                        if (tempInserted)
                        {
                            try
                            {
                                var brToDel = tr.GetObject(sampleBr.ObjectId, OpenMode.ForWrite) as BlockReference;
                                brToDel?.Erase();
                            }
                            catch { /* 忽略删除失败 */ }
                        }

                        tr.Commit();
                        ed.WriteMessage($"\n管道已生成（{(isOutlet ? "出口" : "入口")}），点数: {points.Count}，管段号: {extractedPipeNo}");
                    }
                    catch (Exception ex)
                    {
                        tr.Abort();
                        ed.WriteMessage($"\n生成管道时出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 辅助：把属性定义中存在或不存在的 Tag 设置/新增值
        /// </summary>
        private void SetOrAddAttr(List<AttributeDefinition> attDefs, string tag, string text, ref int extraIndex, double yOffsetBase, double attHeight)
        {
            var existing = attDefs.FirstOrDefault(a => string.Equals(a.Tag, tag, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.TextString = text;
                existing.Invisible = false;
                existing.Constant = false;
            }
            else
            {
                attDefs.Add(new AttributeDefinition
                {
                    Tag = tag,
                    Position = new Point3d(0, yOffsetBase - extraIndex * attHeight * 1.2, 0),
                    Rotation = 0.0,
                    TextString = text,
                    Height = attHeight,
                    Invisible = false,
                    Constant = false
                });
                extraIndex++;
            }
        }

        /// <summary>
        /// 辅助：根据点列表构建按顺序的线段信息（仅用于方向聚合计算）
        /// </summary>
        private List<LineSegmentInfo> BuildOrderedLineSegmentsFromPoints(List<Point3d> pts)
        {
            var segs = new List<LineSegmentInfo>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                segs.Add(new LineSegmentInfo
                {
                    StartPoint = pts[i],
                    EndPoint = pts[i + 1],
                    Length = pts[i].DistanceTo(pts[i + 1]),
                    Angle = ComputeSegmentAngleUcs(pts[i], pts[i + 1])
                });
            }
            return segs;
        }

        /// <summary>
        /// 新增辅助：为每段生成方向三角和管道标题文字（放在 EquipmentTableGenerator 类内，放置在 BuildOrderedLineSegmentsFromPoints 之后）
        /// </summary>
        /// <param name="sampleInfo"></param>
        /// <param name="verticesWorld"></param>
        /// <param name="pipeTitle"></param>
        /// <param name="sampleBlockName"></param>
        /// <returns></returns>
        private List<Entity> CreateDirectionalArrowsAndTitles(DBTrans tr, SamplePipeInfo sampleInfo, List<Point3d> verticesWorld, Point3d midPointWorld, string pipeTitle, string sampleBlockName)
        {
            var overlay = new List<Entity>();// 用于返回的临时对象 返回的实体列表
            if (sampleInfo == null || verticesWorld == null || verticesWorld.Count < 2) return overlay;// 无效输入 参数检查

            // 备份模板与填充（若无模板则自绘三角）
            Polyline arrowTemplate = sampleInfo.DirectionArrowTemplate;// 模板 箭头模板
            Solid? fillTemplate = null;// 填充 填充模板
            double explicitArrowLength = 10.0;// 箭头长度 显式箭头长度
            double explicitArrowHeight = 3.0;// 箭头高度 显式箭头高度
            if (arrowTemplate == null)// 无模板 若无模板
            {
                // 使用默认样式（依据块名判断入/出口颜色和大小）
                var (colorIdx, length, height) = DetermineArrowStyleByName(sampleBlockName);
                explicitArrowLength = length;// 箭头长度
                explicitArrowHeight = height;// 箭头高度
                // 创建填充 自绘三角形箭头 自绘三角形箭头
                var (outline, fill) = CreateArrowTriangleFilled(length, height, colorIdx, sampleInfo.PipeBodyTemplate);
                arrowTemplate = outline;// 模板
                fillTemplate = fill;// 填充
            }

            // 文本样式参考
            double textHeight = 3.5;
            if (sampleInfo.AttributeDefinitions != null && sampleInfo.AttributeDefinitions.Count > 0)// 有属性定义
                textHeight = Math.Max(1.0, sampleInfo.AttributeDefinitions[0].Height);// 文本高度 文本高度 参考属性定义的高度
            // 文本 确保文字样式已注册
            for (int i = 0; i < verticesWorld.Count - 1; i++)
            {
                var p1 = verticesWorld[i];// 起点
                var p2 = verticesWorld[i + 1];// 终点
                var seg = p2 - p1;// 线段
                if (seg.IsZeroLength()) continue;// 无效线段 跳过 忽略零长度段
                // 计算线段方向 方向向量 线段方向
                var dir = seg.GetNormal();
                var mid = new Point3d((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, (p1.Z + p2.Z) / 2.0);// 管道的中点位置

                // 生成对齐后的箭头（以箭头模板为基准，尖端朝 +X）
                Polyline? outlineAligned = null;
                Solid? fillAligned = null;
                try
                {
                    (outlineAligned, fillAligned) = AlignArrowToDirection(arrowTemplate, fillTemplate, dir);// 对齐箭头

                    // 把箭头移动到相对于 midPointWorld 的局部坐标（block 内）
                    var localDisp = mid - midPointWorld;
                    if (outlineAligned != null)// 箭头 存在轮廓
                    {
                        // 把箭头移动到相对于 midPointWorld 的局部坐标（block 内） 移动位置
                        outlineAligned.TransformBy(Matrix3d.Displacement(new Vector3d(localDisp.X, localDisp.Y, localDisp.Z)));
                        outlineAligned.Layer = sampleInfo.PipeBodyTemplate.Layer;// 箭头 层 设置图层
                        overlay.Add(outlineAligned);// 箭头 添加 添加到返回列表
                    }
                    if (fillAligned != null)// 填充 存在填充
                    {
                        // 把填充移动到相对于 midPointWorld 的局部坐标（block 内）
                        fillAligned.TransformBy(Matrix3d.Displacement(new Vector3d(localDisp.X, localDisp.Y, localDisp.Z)));
                        fillAligned.Layer = sampleInfo.PipeBodyTemplate.Layer;// 填充 层 设置图层
                        overlay.Add(fillAligned);// 填充 添加 添加到返回列表
                    }
                }
                catch
                {
                    // 忽略箭头生成异常，继续生成文本
                }

                // 生成文字（DBText），放在箭头上侧并保证始终可读（不倒置）
                try
                {
                    // perp 是相对于段方向的局部“上侧”向量
                    var perp = new Vector3d(-dir.Y, dir.X, 0.0);
                    if (perp.IsZeroLength())
                        perp = Vector3d.YAxis;
                    else
                        perp = perp.GetNormal();

                    // 强制 perp 指向全局的 +Y 方向一侧，确保文字始终位于管道的“上侧”（图纸上方）
                    if (perp.DotProduct(Vector3d.YAxis) < 0)
                        perp = -perp;

                    // 估算箭头高度（使用已对齐实体的几何包围盒）
                    double arrowHalfHeight = explicitArrowHeight / 2.0;
                    try
                    {
                        Entity sizeEntity = (Entity?)outlineAligned ?? (Entity?)fillAligned;
                        if (sizeEntity != null)
                        {
                            var ext = sizeEntity.GeometricExtents;
                            arrowHalfHeight = Math.Abs(ext.MaxPoint.Y - ext.MinPoint.Y) / 2.0;
                            if (arrowHalfHeight < 1e-6) arrowHalfHeight = explicitArrowHeight / 2.0;
                        }
                    }
                    catch
                    {
                        // 忽略，使用默认
                        arrowHalfHeight = explicitArrowHeight / 2.0;
                    }

                    // 将文字放在箭头上方：沿 perp 方向偏移 arrowHalfHeight + 文字高度的适当间距
                    double offset = arrowHalfHeight + textHeight * 0.8;
                    var worldTextPos = mid + perp * offset;// 文字位置
                    // 转为局部坐标（相对于 midPointWorld）
                    var localTextPos = new Point3d(worldTextPos.X - midPointWorld.X, worldTextPos.Y - midPointWorld.Y, worldTextPos.Z - midPointWorld.Z);
                    //localTextPos = new Point3d(worldTextPos.X - midPointWorld.X - 10, worldTextPos.Y - midPointWorld.Y, worldTextPos.Z - midPointWorld.Z);
                    // 计算文本旋转角度（使文字沿管线方向排布）
                    double segAngle = ComputeSegmentAngleUcs(p1, p2); // 返回范围 [0, 2π)
                    double textRot = segAngle;

                    // 保证文字始终正向可读：使得文字基向量的 X 分量为非负（cos(rotation) >= 0）
                    // 若 cos(textRot) < 0，则旋转 180°（加 PI），以避免文字“头上脚下”
                    if (Math.Cos(textRot) < 0)
                        textRot += Math.PI;

                    // 将角度归一化到 (-π, π]，AutoCAD 更容易接受
                    if (textRot > Math.PI)
                        textRot -= 2.0 * Math.PI;
                    if (textRot <= -Math.PI)
                        textRot += 2.0 * Math.PI;
                    // 创建 DBText 创建 DBText 文字实体
                    var dbText = new DBText
                    {
                        Position = localTextPos,// 文字位置 位置 局部位置
                        Height = textHeight,// 文字高度 高度
                        TextString = string.IsNullOrWhiteSpace(pipeTitle) ? sampleBlockName ?? "管道" : pipeTitle,// 文字字符串 文本字符串
                        Rotation = textRot,// 旋转角度 旋转角度
                        Layer = sampleInfo.PipeBodyTemplate.Layer,// 层 层 图层 设置图层
                        Normal = Vector3d.ZAxis,// 法向量 法向量
                        Oblique = 0.0// 偏斜角度 偏斜角度
                    };
                    // 应用标题样式 应用文字样式
                    FontsStyleHelper.ApplyTitleToDBText(tr, dbText);

                    // 使文字水平居中（以 Position 为中心）
                    try
                    {
                        dbText.AlignmentPoint = localTextPos;// 文字位置 对齐点 对齐点 设置为位置
                        dbText.HorizontalMode = TextHorizontalMode.TextCenter;// 文字水平居中 水平模式 水平模式 设置为居中
                        dbText.VerticalMode = TextVerticalMode.TextVerticalMid;// 文字垂直居中 垂直模式 垂直模式 设置为居中
                    }
                    catch
                    {
                        // 有些 AutoCAD API 版本对这些属性可能有限制，忽略异常
                    }

                    overlay.Add(dbText);
                }
                catch
                {
                    // 忽略文字生成异常
                }
            }

            return overlay;
            //var overlay = new List<Entity>();
            //if (sampleInfo == null || verticesWorld == null || verticesWorld.Count < 2) return overlay;

            //// 备份模板与填充（若无模板则自绘三角）
            //Polyline arrowTemplate = sampleInfo.DirectionArrowTemplate;
            //Solid? fillTemplate = null;
            //double explicitArrowLength = 10.0;
            //double explicitArrowHeight = 3.0;
            //if (arrowTemplate == null)
            //{
            //    // 使用默认样式（依据块名判断入/出口颜色和大小）
            //    var (colorIdx, length, height) = DetermineArrowStyleByName(sampleBlockName);
            //    explicitArrowLength = length;
            //    explicitArrowHeight = height;
            //    var (outline, fill) = CreateArrowTriangleFilled(length, height, colorIdx, sampleInfo.PipeBodyTemplate);
            //    arrowTemplate = outline;
            //    fillTemplate = fill;
            //}

            //// 文本样式参考
            //double textHeight = 2.5;
            //if (sampleInfo.AttributeDefinitions != null && sampleInfo.AttributeDefinitions.Count > 0)
            //    textHeight = Math.Max(1.0, sampleInfo.AttributeDefinitions[0].Height);

            //for (int i = 0; i < verticesWorld.Count - 1; i++)
            //{
            //    var p1 = verticesWorld[i];
            //    var p2 = verticesWorld[i + 1];
            //    var seg = p2 - p1;
            //    if (seg.IsZeroLength()) continue;

            //    var dir = seg.GetNormal();
            //    var mid = new Point3d((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, (p1.Z + p2.Z) / 2.0);

            //    // 生成对齐后的箭头（以箭头模板为基准，尖端朝 +X）
            //    Polyline? outlineAligned = null;
            //    Solid? fillAligned = null;
            //    try
            //    {
            //        (outlineAligned, fillAligned) = AlignArrowToDirection(arrowTemplate, fillTemplate, dir);

            //        // 把箭头移动到相对于 midPointWorld 的局部坐标（block 内）
            //        var localDisp = mid - midPointWorld;
            //        if (outlineAligned != null)
            //        {
            //            outlineAligned.TransformBy(Matrix3d.Displacement(new Vector3d(localDisp.X, localDisp.Y, localDisp.Z)));
            //            outlineAligned.Layer = sampleInfo.PipeBodyTemplate.Layer;
            //            overlay.Add(outlineAligned);
            //        }
            //        if (fillAligned != null)
            //        {
            //            fillAligned.TransformBy(Matrix3d.Displacement(new Vector3d(localDisp.X, localDisp.Y, localDisp.Z)));
            //            fillAligned.Layer = sampleInfo.PipeBodyTemplate.Layer;
            //            overlay.Add(fillAligned);
            //        }
            //    }
            //    catch
            //    {
            //        // 忽略箭头生成异常，继续生成文本
            //    }

            //    // 生成文字（DBText），放在箭头上方（沿箭头本地 Y 方向，即 perp），并转换为局部坐标
            //    try
            //    {
            //        var perp = new Vector3d(-dir.Y, dir.X, 0.0);
            //        if (perp.IsZeroLength())
            //            perp = Vector3d.YAxis;
            //        else
            //            perp = perp.GetNormal();

            //        // 估算箭头高度（使用已对齐实体的几何包围盒）
            //        double arrowHalfHeight = explicitArrowHeight / 2.0;
            //        try
            //        {
            //            Entity sizeEntity = (Entity?)outlineAligned ?? (Entity?)fillAligned;
            //            if (sizeEntity != null)
            //            {
            //                var ext = sizeEntity.GeometricExtents;
            //                // 注意：在 align 后，local Y 对应法向 perp；用高度一半作为偏移基准
            //                arrowHalfHeight = Math.Abs(ext.MaxPoint.Y - ext.MinPoint.Y) / 2.0;
            //                if (arrowHalfHeight < 1e-6) arrowHalfHeight = explicitArrowHeight / 2.0;
            //            }
            //        }
            //        catch
            //        {
            //            // 忽略，使用默认
            //            arrowHalfHeight = explicitArrowHeight / 2.0;
            //        }

            //        // 将文字放在箭头上方：沿 perp 方向偏移 arrowHalfHeight + 文字高度的适当间距
            //        double offset = arrowHalfHeight + textHeight * 0.8;

            //        var worldTextPos = mid + perp * offset;

            //        // 转为局部坐标（相对于 midPointWorld）
            //        var localTextPos = new Point3d(worldTextPos.X - midPointWorld.X, worldTextPos.Y - midPointWorld.Y, worldTextPos.Z - midPointWorld.Z);

            //        var dbText = new DBText
            //        {
            //            Position = localTextPos,
            //            Height = textHeight,
            //            TextString = string.IsNullOrWhiteSpace(pipeTitle) ? sampleBlockName ?? "管道" : pipeTitle,
            //            Rotation = ComputeSegmentAngleUcs(p1, p2),
            //            Layer = sampleInfo.PipeBodyTemplate.Layer,
            //            Normal = Vector3d.ZAxis,
            //            Oblique = 0.0
            //        };

            //        // 使文字水平居中（以 Position 为中心）
            //        try
            //        {
            //            dbText.AlignmentPoint = localTextPos;
            //            dbText.HorizontalMode = TextHorizontalMode.TextCenter;
            //        }
            //        catch
            //        {
            //            // 有些 AutoCAD API 版本对这些属性可能有限制，忽略异常
            //        }

            //        overlay.Add(dbText);
            //    }
            //    catch
            //    {
            //        // 忽略文字生成异常
            //    }
            //}

            //return overlay;
        }

        #endregion


        /// <summary>
        /// 设置动态块的角度
        /// </summary>
        /// <param name="dynProps">动态块</param>
        /// <param name="newAngle">新角度</param>
        /// <param name="ed">Editor</param>
        private static void SetDynamicBlockNewAngle(DynamicBlockReferencePropertyCollection dynProps, double newAngle, Editor ed)
        {
            //循环动态块的所有属性
            foreach (DynamicBlockReferenceProperty dynProp in dynProps)
            {
                if (dynProp.PropertyName.Contains("管道角度"))
                {
                    if (!dynProp.ReadOnly)
                    {
                        // 输出拉伸参数信息，帮助调试
                        ed.WriteMessage($"\n角度参数: {dynProp.PropertyName}");
                        // 将度转换为弧度
                        //double rotationRadians = pdr.Value * Math.PI / 180.0;

                        // 设置旋转参数
                        dynProp.Value = newAngle;
                        ed.WriteMessage($"\n设置角度：（{newAngle}）");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 设置动态块内方向的翻转状态
        /// </summary>
        /// <param name="dynProps">动态块</param>
        /// <param name="newRotated">新反转状态</param>
        /// <param name="ed">Editor</param>
        private static void SetDynamicBlockNewRotated(DynamicBlockReferencePropertyCollection dynProps, bool newRotated, Editor ed)
        {
            foreach (DynamicBlockReferenceProperty dynProp in dynProps)
            {
                if (dynProp.PropertyName.Contains("翻转状态1"))
                {
                    if (!dynProp.ReadOnly)
                    {

                        // 翻转当前状态（切换0/1）
                        //int currentValue = Convert.ToInt16(dynProp.Value);
                        //dynProp.Value = currentValue == 0 ? 1 : 0;

                        //ed.WriteMessage($"\n已切换翻转状态: {dynProp.PropertyName} -> {dynProp.Value}");

                        //// 输出拉伸参数信息，帮助调试
                        //ed.WriteMessage($"\n{dynProp.PropertyName}");

                        // 设置旋转参数
                        if (newRotated)
                        {
                            dynProp.Value = 1;

                        }
                        else
                        {
                            dynProp.Value = 0;
                        }
                        ed.WriteMessage($"\n翻转状态1：（{newRotated}）");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 设置动态块的起点和终点坐标
        /// </summary>
        /// <param name="blockRef">块参照对象</param>
        /// <param name="startPoint">起点坐标</param>
        /// <param name="endPoint">终点坐标</param>
        /// <param name="ed">编辑器对象</param>
        private static void SetDynamicBlockEndPoints(DynamicBlockReferencePropertyCollection dynProps, double startPoint, double endPoint, Editor ed)
        {

            foreach (DynamicBlockReferenceProperty dynProp in dynProps)
            {
                // 查找起点相关属性
                if (dynProp.PropertyName.Contains("管道向左拉伸"))
                {
                    if (!dynProp.ReadOnly)
                    {
                        // 输出拉伸参数信息，帮助调试
                        ed.WriteMessage($"\n起点拉伸参数: {dynProp.PropertyName}");
                        if (startPoint <= 0)
                        {
                            // 设置起点坐标
                            dynProp.Value = Convert.ToDouble(dynProp.Value) + Math.Abs(startPoint);
                        }
                        else
                        {
                            //dynProp.Value = startPoint - Convert.ToDouble(dynProp.Value);
                            if (startPoint > Convert.ToDouble(dynProp.Value))
                                dynProp.Value = startPoint - Convert.ToDouble(dynProp.Value);
                            else
                                dynProp.Value = Convert.ToDouble(dynProp.Value) - startPoint;
                        }
                        ed.WriteMessage($"\n设置起点: ({dynProp.Value})");
                    }
                }
                // 查找终点相关属性
                else if (dynProp.PropertyName.Contains("管道向右拉伸"))
                {
                    if (!dynProp.ReadOnly)
                    {
                        // 输出拉伸参数信息，帮助调试
                        ed.WriteMessage($"\n起点拉伸参数: {dynProp.PropertyName}");
                        if (endPoint >= 0)
                        {
                            dynProp.Value = Convert.ToDouble(dynProp.Value) + endPoint;
                        }
                        else
                        {
                            if (Math.Abs(endPoint) > Convert.ToDouble(dynProp.Value))
                                dynProp.Value = Math.Abs(endPoint) - Convert.ToDouble(dynProp.Value);
                            else
                                dynProp.Value = Convert.ToDouble(dynProp.Value) - Math.Abs(endPoint);
                        }
                        ed.WriteMessage($"\n设置终点: ({dynProp.Value})");
                    }
                }
            }
        }

        /// <summary>
        /// 辅助命令：列出选中动态块的所有可用属性
        /// </summary>
        [CommandMethod("LISTDYNPROPS")]
        public void ListDynamicBlockProperties()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // 提示用户选择动态块
            PromptEntityOptions peo = new PromptEntityOptions("\n请选择动态块以查看其属性: ");
            peo.SetRejectMessage("\n只能选择块参照对象!");
            peo.AddAllowedClass(typeof(BlockReference), true);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
                return;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // 获取块参照对象
                BlockReference blockRef = trans.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;

                if (blockRef != null && blockRef.IsDynamicBlock)
                {
                    ed.WriteMessage("\n========== 动态块属性列表 ==========");

                    // 遍历并显示所有动态属性
                    DynamicBlockReferencePropertyCollection dynProps = blockRef.DynamicBlockReferencePropertyCollection;

                    for (int i = 0; i < dynProps.Count; i++)
                    {
                        DynamicBlockReferenceProperty dynProp = dynProps[i];
                        ed.WriteMessage($"\n  属性 {i + 1}:");
                        ed.WriteMessage($"\n  名称: {dynProp.PropertyName}");
                        ed.WriteMessage($"\n  描述: {dynProp.Description}");
                        ed.WriteMessage($"\n  参数类型: {dynProp.Value.GetType().Name}");
                        ed.WriteMessage($"\n  当前值: {dynProp.Value}");
                        ed.WriteMessage($"\n  单位类型: {dynProp.UnitsType}");
                        ed.WriteMessage($"\n  是否只读: {dynProp.ReadOnly}");
                        ed.WriteMessage($"\n  是否可见: {dynProp.Show}");
                        ed.WriteMessage("\n" + new string('-', 30));
                    }
                }
                else
                {
                    ed.WriteMessage("\n所选对象不是动态块!");
                }

                trans.Commit();
            }
        }

        /// <summary>
        /// 确定设备类型
        /// </summary>
        private string DetermineEquipmentType(string blockName)
        {
            string lowerName = blockName.ToLower();
            if (lowerName.Contains("阀") || lowerName.Contains("valve"))
                return "阀门";
            else if (lowerName.Contains("法兰") || lowerName.Contains("flange"))
                return "法兰";
            else if (lowerName.Contains("管") || lowerName.Contains("pipe"))
                return "管道";
            else
                return "设备";
        }

        /// <summary>
        /// 生成设备唯一键
        /// </summary>
        private string GenerateEquipmentKey(EquipmentInfo equipment)
        {
            return $"{equipment.Name}_{string.Join("_", equipment.Attributes.Values)}";
        }

        /// <summary>
        /// 创建设备表格
        /// </summary>
        /// <param name="db"></param>
        /// <param name="equipmentList"></param>
        private void CreateEquipmentTable(Database db, List<EquipmentInfo> equipmentList)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord modelSpace = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // 计算表格尺寸
                    int totalColumns = CalculateTotalColumns(equipmentList);
                    int totalRows = 5; // 固定4行

                    // 创建表格
                    Table table = new Table();
                    table.SetSize(totalRows, totalColumns);

                    // 让用户指定表格插入位置
                    PromptPointResult ppr = Env.Editor.GetPoint("\n指定插入位置: ");
                    if (ppr.Status == PromptStatus.OK)
                    {
                        table.Position = ppr.Value; // 设置表格位置
                    }

                    // 设置表格样式
                    SetTableStyle(db, table, trans);

                    // 填充表格内容
                    FillTableContent(table, equipmentList);

                    // 添加到模型空间
                    modelSpace.AppendEntity(table);
                    trans.AddNewlyCreatedDBObject(table, true);

                    trans.Commit();
                }
                catch
                {
                    trans.Abort();
                    throw;
                }
            }
        }

        /// <summary>
        /// 设置表格样式 - 修正版
        /// </summary>
        private void SetTableStyle(Database db, Table table, Transaction trans)
        {
            try
            {
                // 获取或创建表格样式
                ObjectId tableStyleId = GetOrCreateTableStyle(db, trans);

                // 应用表格样式
                if (!tableStyleId.IsNull)
                {
                    table.TableStyle = tableStyleId;
                }

                // 设置表格基本属性
                table.FlowDirection = Autodesk.AutoCAD.DatabaseServices.FlowDirection.TopToBottom;

                // 设置行高
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    double rowHeight = 8.0; // 默认行高
                    if (i == 0) // 标题行稍高一些
                        rowHeight = 12.0;
                    else if (i == 1) // 第二行（设备名称行）
                        rowHeight = 10.0;

                    table.SetRowHeight(i, rowHeight);
                }

                // 设置列宽
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    table.SetColumnWidth(j, 25.0); // 默认列宽
                }

                // 设置表格边框
                SetTableBorders(table);

                // 设置单元格样式
                SetCellStyles(table);
            }
            catch (Exception ex)
            {
                // 如果样式设置失败，使用基本设置
                SetBasicTableStyle(table);
            }
        }

        /// <summary>
        /// 获取或创建表格样式
        /// </summary>
        private ObjectId GetOrCreateTableStyle(Database db, Transaction trans)
        {
            try
            {
                // 获取表格样式字典
                DBDictionary tableStyleDict = trans.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead) as DBDictionary;

                ObjectId tableStyleId = ObjectId.Null;

                // 尝试获取Standard表格样式
                if (tableStyleDict.Contains("Standard"))
                {
                    tableStyleId = tableStyleDict.GetAt("Standard");
                }
                else if (tableStyleDict.Contains("_STANDARD"))
                {
                    tableStyleId = tableStyleDict.GetAt("_STANDARD");
                }
                else
                {
                    // 创建自定义表格样式
                    tableStyleId = CreateCustomTableStyle(db, trans, tableStyleDict);
                }

                return tableStyleId;
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        /// <summary>
        /// 创建自定义表格样式
        /// </summary>
        private ObjectId CreateCustomTableStyle(Database db, Transaction trans, DBDictionary tableStyleDict)
        {
            try
            {
                // 升级字典访问权限
                tableStyleDict.UpgradeOpen();

                // 创建新的表格样式
                TableStyle newTableStyle = new TableStyle();
                newTableStyle.Name = "EquipmentTableStyle";

                // 设置标题行样式
                newTableStyle.SetAlignment(CellAlignment.MiddleCenter, (int)RowType.TitleRow);
                newTableStyle.SetTextHeight(3.5, (int)RowType.TitleRow);
                newTableStyle.SetColor(Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1), (int)RowType.TitleRow);
                newTableStyle.SetMargin(cellMargin: default, 1.5, RowType.TitleRow.ToString());

                // 设置表头行样式
                newTableStyle.SetAlignment(CellAlignment.MiddleCenter, (int)RowType.HeaderRow);
                newTableStyle.SetTextHeight(2.5, (int)RowType.HeaderRow);
                newTableStyle.SetColor(Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 2), (int)RowType.HeaderRow);
                newTableStyle.SetMargin(cellMargin: default, 1.0, RowType.HeaderRow.ToString());

                // 设置数据行样式
                newTableStyle.SetAlignment(CellAlignment.MiddleCenter, (int)RowType.DataRow);
                newTableStyle.SetTextHeight(2.0, (int)RowType.DataRow);
                newTableStyle.SetColor(Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7), (int)RowType.DataRow);
                newTableStyle.SetMargin(cellMargin: default, 1.0, RowType.DataRow.ToString());

                // 设置网格线
                newTableStyle.SetGridLineWeight(LineWeight.LineWeight025, (int)GridLineType.AllGridLines, (int)RowType.TitleRow);
                newTableStyle.SetGridLineWeight(LineWeight.LineWeight025, (int)GridLineType.AllGridLines, (int)RowType.HeaderRow);
                newTableStyle.SetGridLineWeight(LineWeight.LineWeight025, (int)GridLineType.AllGridLines, (int)RowType.DataRow);

                // 添加到字典
                tableStyleDict.SetAt("EquipmentTableStyle", newTableStyle);
                trans.AddNewlyCreatedDBObject(newTableStyle, true);

                return newTableStyle.ObjectId;
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        /// <summary>
        /// 设置表格边框
        /// </summary>
        private void SetTableBorders(Table table)
        {
            try
            {
                // 设置所有网格线
                GridLineType allLines = GridLineType.HorizontalTop |
                                       GridLineType.HorizontalBottom |
                                       GridLineType.HorizontalInside |
                                       GridLineType.VerticalLeft |
                                       GridLineType.VerticalRight |
                                       GridLineType.VerticalInside;
            }
            catch
            {
                // 忽略边框设置错误
            }
        }

        /// <summary>
        /// 设置单元格样式
        /// </summary>
        private void SetCellStyles(Table table)
        {
            try
            {
                // 设置所有单元格的基本样式
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        Cell cell = table.Cells[i, j];

                        // 设置对齐方式
                        cell.Alignment = CellAlignment.MiddleCenter;

                        // 设置文字高度
                        if (i == 0) // 标题行
                        {
                            cell.TextHeight = 3.5;
                        }
                        else if (i == 1 || i == 2) // 表头行
                        {
                            cell.TextHeight = 2.5;
                        }
                        else // 数据行
                        {
                            cell.TextHeight = 2.0;
                        }
                    }
                }
            }
            catch
            {
                // 忽略单元格样式设置错误
            }
        }

        /// <summary>
        /// 设置基本表格样式（备选方案）
        /// </summary>
        private void SetBasicTableStyle(Table table)
        {
            try
            {
                // 基本的行高设置
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    table.SetRowHeight(i, i == 0 ? 12.0 : 8.0);
                }

                // 基本的列宽设置
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    table.SetColumnWidth(j, 25.0);
                }

                // 设置基本边框
                //table.SetGridLineWeight(LineWeight.LineWeight025, (int)GridLineType.AllGridLines);
            }
            catch
            {
                // 忽略错误
            }
        }

        /// <summary>
        /// 填充表格内容
        /// </summary>
        private void FillTableContent(Table table, List<EquipmentInfo> equipmentList)
        {
            // 第一行：标题
            table.MergeCells(CellRange.Create(table, 0, 0, 0, table.Columns.Count - 1));
            table.Cells[0, 0].TextString = "设备材料明细表";
            table.Cells[0, 0].Alignment = CellAlignment.MiddleCenter;

            // 填充固定列头，默认管道组为 8 列（兼容旧调用）
            FillFixedHeaders(table, 8);

            // 填充动态列头和数据
            FillDynamicContent(table, equipmentList);
        }

        /// <summary>
        /// 填充固定列头
        /// pipeGroupCount: 管道组（"管道 Pipe (m)"）的子列数（可以大于初始 8）
        /// 基本固定列索引说明（基列数 baseFixedCols = 10）：
        /// 0: 管道标题
        /// 1: 管段号
        /// 2-3: 起点/终点（合并为组）
        /// 4: 管道等级
        /// 5-7: 设计条件（介质/温度/压力）
        /// 8-9: 隔热及防腐（Code/Antisepsis）
        /// 随后 pipeGroupCount 个列为管道组子列（名称/材料/...等）
        /// </summary>
        private void FillFixedHeaders(Table table, int pipeGroupCount)
        {
            try
            {
                // 基础固定列（0..9）
                // 管道标题：第0列（索引0），跨2行
                table.MergeCells(CellRange.Create(table, 1, 0, 2, 0));
                table.Cells[1, 0].TextString = "管道标题\nPipe Title";

                // 管段号：第1列（索引1），跨2行
                table.MergeCells(CellRange.Create(table, 1, 1, 2, 1));
                table.Cells[1, 1].TextString = "管段号\nPipeline\nNo.";

                // 管段起止点：第2-3列（索引2,3），row1 合并为组，row2 分别为 起点/终点
                table.MergeCells(CellRange.Create(table, 1, 2, 1, 3));
                table.Cells[1, 2].TextString = "管段起止点\nPipeline From Start To End";
                table.Cells[2, 2].TextString = "起点\nFrom";
                table.Cells[2, 3].TextString = "终点\nTo";

                // 管道等级：第4列（索引4），跨2行
                table.MergeCells(CellRange.Create(table, 1, 4, 2, 4));
                table.Cells[1, 4].TextString = "管道\n等级\nPipe Class";

                // 设计条件：第5-7列（索引5..7），row1 合并为组，row2: 介质名称/操作温度/操作压力
                table.MergeCells(CellRange.Create(table, 1, 5, 1, 7));
                table.Cells[1, 5].TextString = "设计条件 \nDesign Condition";
                table.Cells[2, 5].TextString = "介质名称\nMedium Name";
                table.Cells[2, 6].TextString = "操作温度\nT(℃)";
                table.Cells[2, 7].TextString = "操作压力\nP(MPaG)";

                // 隔热及防腐：第8-9列（索引8..9），row1 合并为组，row2: Code / Antisepsis
                table.MergeCells(CellRange.Create(table, 1, 8, 1, 9));
                table.Cells[1, 8].TextString = "隔热及防腐 \nInsul.& Antisepsis";
                table.Cells[2, 8].TextString = "隔热隔声代号\nCode";
                table.Cells[2, 9].TextString = "是否防腐\nAntisepsis";

                // 管道组：从第10列开始，动态宽度由 pipeGroupCount 决定
                int pipeGroupStart = 10;
                int pipeGroupEnd = pipeGroupStart + Math.Max(0, pipeGroupCount - 1);
                if (pipeGroupEnd >= table.Columns.Count) pipeGroupEnd = table.Columns.Count - 1;
                if (pipeGroupStart < table.Columns.Count)
                {
                    table.MergeCells(CellRange.Create(table, 1, pipeGroupStart, 1, pipeGroupEnd));
                    table.Cells[1, pipeGroupStart].TextString = "管道\nPipe (m)";
                    table.Cells[1, pipeGroupStart].Alignment = CellAlignment.MiddleCenter;

                    // 默认子列标题（前8项为常用）；额外列留空，由 CreateEquipmentTableWithType 填写具体名字
                    string[] defaultPipeSubHeaders = new[]
                    {
                        "名称\nName",
                        "材料\nMaterial",
                        "图号或标准号\nDWG.No./ STD.No.",
                        "数量\nQuan.",
                        "泵前/后\nPump F/B",
                        "核算流速\n(M/S)",
                        "管道长度(mm)\nLength(mm)",
                        "累计长度(mm)\nAllLength(mm)"
                    };

                    for (int i = 0; i <= pipeGroupEnd - pipeGroupStart; i++)
                    {
                        int col = pipeGroupStart + i;
                        if (i < defaultPipeSubHeaders.Length)
                            table.Cells[2, col].TextString = defaultPipeSubHeaders[i];
                        else
                            table.Cells[2, col].TextString = string.Empty; // 额外列由上层动态填写列名
                    }
                }
            }
            catch
            {
                // 容错：忽略任何设置异常
            }
        }

        /// <summary>
        /// 填充动态内容
        /// </summary>
        private void FillDynamicContent(Table table, List<EquipmentInfo> equipmentList)
        {
            // 保持与现有表头对齐，原实现从第19列开始（索引18）
            int currentColumn = 18;

            // 要从动态属性中排除的保留关键词（按包含匹配）
            var reservedSubstrings = new[]
            {
                "管道标题","管段号","管道号","起点","始点","终点","止点","管道等级",
                "介质","介质名称","Medium","Medium Name","操作温度","操作压力",
                "隔热隔声代号","是否防腐","Length","长度",
                "标准号","图号","DWG.No.","STD.No.","标准"
            };

            foreach (var equipment in equipmentList)
            {
                // 过滤掉保留键，避免重复生成 "管段号" 等列（按包含关系）
                var attrs = (equipment.Attributes ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
                            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) &&
                                         !reservedSubstrings.Any(s => !string.IsNullOrWhiteSpace(s) &&
                                             kv.Key.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
                            .ToList();

                int startColumn = currentColumn;
                int columnSpan = Math.Max(0, attrs.Count);
                string result = string.Empty;

                // 如果没有可写的动态属性则跳过（仍保留管道标题单元）
                if (columnSpan == 0)
                {
                    // 尝试将管道标题写到起始列（如果需要）
                    try
                    {
                        result = equipment.Attributes != null && equipment.Attributes.TryGetValue("管道标题", out var tv) ? tv : string.Empty;
                    }
                    catch { result = string.Empty; }

                    if (!string.IsNullOrEmpty(result) && startColumn < table.Columns.Count)
                    {
                        table.Cells[1, startColumn].TextString = result;
                        table.Cells[1, startColumn].Alignment = CellAlignment.MiddleCenter;
                    }

                    // 不改变 currentColumn
                    continue;
                }

                // 合并标题单元（如果需要）
                if (columnSpan > 1 && startColumn + columnSpan - 1 < table.Columns.Count)
                {
                    table.MergeCells(CellRange.Create(table, 1, startColumn, 1, startColumn + columnSpan - 1));
                }

                try
                {
                    result = equipment.Attributes != null && equipment.Attributes.TryGetValue("管道标题", out var tv) ? tv : string.Empty;
                }
                catch { result = string.Empty; }

                if (startColumn < table.Columns.Count)
                {
                    table.Cells[1, startColumn].TextString = result;
                    table.Cells[1, startColumn].Alignment = CellAlignment.MiddleCenter;
                }

                // 写入动态列的列名（第2行）
                int colIndex = 0;
                for (int i = 0; i < attrs.Count; i++)
                {
                    var attr = attrs[i];
                    int col = startColumn + colIndex;
                    if (col >= table.Columns.Count) break;
                    table.Cells[2, col].TextString = attr.Key;
                    colIndex++;
                }

                // 写入动态列的数据行（第3行：数据开始行索引在原实现是3）
                colIndex = 0;
                int dataRow = 3; // 数据行起始行（与 CreateEquipmentTableWithType 保持一致）
                for (int i = 0; i < attrs.Count; i++)
                {
                    var attr = attrs[i];
                    int col = startColumn + colIndex;
                    if (col >= table.Columns.Count) break;
                    string value = attr.Value ?? string.Empty;
                    // 数量字段优先使用设备 Count
                    if (string.Equals(attr.Key, "数量", StringComparison.OrdinalIgnoreCase) || attr.Key.ToLowerInvariant().Contains("quantity"))
                        value = equipment.Count.ToString();
                    // 写入到对应的数据行
                    if (dataRow < table.Rows.Count)
                        table.Cells[dataRow, col].TextString = value;
                    colIndex++;
                }

                currentColumn += columnSpan;
            }

            AutoResizeColumns(table);
        }

        /// <summary>
        /// 自适应调整列宽
        /// </summary>
        private void AutoResizeColumns(Table table)
        {
            for (int j = 0; j < table.Columns.Count; j++)
            {
                double maxWidth = 25.0; // 最小宽度

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    string text = table.Cells[i, j].TextString;
                    if (!string.IsNullOrEmpty(text))
                    {
                        // 简单的宽度估算：中文字符按2个字符计算
                        double estimatedWidth = EstimateTextWidth(text);
                        if (estimatedWidth > maxWidth)
                            maxWidth = estimatedWidth;
                    }
                }

                table.SetColumnWidth(j, Math.Min(maxWidth, 60.0)); // 最大宽度限制为60
            }
        }

        /// <summary>
        /// 估算文本宽度
        /// </summary>
        private double EstimateTextWidth(string text)
        {
            double width = 0;
            foreach (char c in text)
            {
                if (c > 127) // 中文字符
                    width += 3.0;
                else
                    width += 1.5;
            }
            return width + 4.0; // 增加一些边距
        }

        /// <summary>
        /// 导出到Excel命令
        /// </summary>
        [CommandMethod("ExportTableToExcel")]
        public void ExportTableToExcel()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // 选择表格
                PromptEntityOptions opts = new PromptEntityOptions("\n请选择要导出的表格：");
                opts.SetRejectMessage("\n请选择一个表格对象。");
                opts.AddAllowedClass(typeof(Table), true);

                PromptEntityResult selResult = ed.GetEntity(opts);
                if (selResult.Status != PromptStatus.OK)
                    return;

                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    Table table = trans.GetObject(selResult.ObjectId, OpenMode.ForRead) as Table;
                    if (table == null)
                    {
                        ed.WriteMessage("\n选择的对象不是表格。");
                        return;
                    }

                    // 导出到Excel
                    ExportTableToExcelFile(table, ed);
                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n导出Excel时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出表格到Excel文件（保留合并单元格与文本、简单样式）
        /// 改进：合并检测改为严格检查候选矩形内所有单元格均未被标记为已合并且为空，以避免产生部分重叠的合并区域（解决 "Can't delete/overwrite merged cells" 错误）。
        /// </summary>
        private void ExportTableToExcelFile(Table table, Editor ed)
        {
            // 获取保存路径
            System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = "Excel文件|*.xlsx";
            saveDialog.Title = "保存设备材料表";
            saveDialog.FileName = $"设备材料表_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            if (saveDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("设备材料表");

                    int rows = table.Rows.Count;
                    int cols = table.Columns.Count;

                    // 先把所有文本写入单元格（仅文本，不处理合并）
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            string cellText = table.Cells[r, c].TextString ?? string.Empty;
                            worksheet.Cells[r + 1, c + 1].Value = string.IsNullOrEmpty(cellText) ? "" : cellText;
                            worksheet.Cells[r + 1, c + 1].Style.WrapText = true;
                        }
                    }

                    // 标记已被合并/处理的单元格，初始为 false
                    var mergedMark = new bool[rows, cols];

                    // 扫描每个单元格，找到非空且未处理的起始单元格后尝试扩展为矩形合并区域
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            if (mergedMark[r, c])
                                continue;

                            string txt = (table.Cells[r, c].TextString ?? string.Empty).Trim();
                            if (string.IsNullOrEmpty(txt))
                                continue;

                            // 计算最大水平扩展：要求右侧单元为空且未被标记
                            int maxH = 1;
                            while (c + maxH < cols)
                            {
                                if (mergedMark[r, c + maxH]) break;
                                var rightTxt = (table.Cells[r, c + maxH].TextString ?? string.Empty).Trim();
                                if (!string.IsNullOrEmpty(rightTxt)) break;
                                maxH++;
                            }

                            // 计算最大垂直扩展：对于每一行，要求从 c..c+maxH-1 都为空且未被标记
                            int maxV = 1;
                            while (r + maxV < rows)
                            {
                                bool rowOk = true;
                                for (int cc = c; cc < c + maxH; cc++)
                                {
                                    if (mergedMark[r + maxV, cc])
                                    {
                                        rowOk = false;
                                        break;
                                    }
                                    var downTxt = (table.Cells[r + maxV, cc].TextString ?? string.Empty).Trim();
                                    if (!string.IsNullOrEmpty(downTxt))
                                    {
                                        rowOk = false;
                                        break;
                                    }
                                }
                                if (!rowOk) break;
                                maxV++;
                            }

                            // 进一步确保矩形内部所有单元均未被标记（防止与先前合并产生部分重叠）
                            bool rectangleClear = true;
                            for (int rr = r; rr < r + maxV && rectangleClear; rr++)
                            {
                                for (int cc = c; cc < c + maxH; cc++)
                                {
                                    if (mergedMark[rr, cc])
                                    {
                                        rectangleClear = false;
                                        break;
                                    }
                                }
                            }

                            if (!rectangleClear)
                            {
                                // 如果候选矩形内部有已标记单元，则退回为单元格不合并（标记当前单元）
                                mergedMark[r, c] = true;
                                continue;
                            }

                            // 只有当矩形尺寸大于1才合并，否则单个单元标记为已处理
                            if (maxH > 1 || maxV > 1)
                            {
                                int excelRow1 = r + 1;
                                int excelCol1 = c + 1;
                                int excelRow2 = r + maxV;
                                int excelCol2 = c + maxH;

                                try
                                {
                                    worksheet.Cells[excelRow1, excelCol1, excelRow2, excelCol2].Merge = true;
                                    worksheet.Cells[excelRow1, excelCol1, excelRow2, excelCol2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                    worksheet.Cells[excelRow1, excelCol1, excelRow2, excelCol2].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                }
                                catch
                                {
                                    // 若 EPPlus 抛出合并冲突（极少数并发合并情况），回退为只处理顶格并继续，避免中断导出
                                    worksheet.Cells[excelRow1, excelCol1].Value = worksheet.Cells[excelRow1, excelCol1].Value;
                                }

                                // 标记该矩形已被处理
                                for (int rr = r; rr < r + maxV; rr++)
                                    for (int cc = c; cc < c + maxH; cc++)
                                        mergedMark[rr, cc] = true;
                            }
                            else
                            {
                                mergedMark[r, c] = true;
                            }
                        }
                    }

                    // 特殊处理：若第1行为标题并在 AutoCAD 中被合并（大多数场景是如此），确保 Excel 中也是合并并加粗居中
                    try
                    {
                        string firstCell = (table.Cells[0, 0].TextString ?? string.Empty).Trim();
                        bool otherEmpty = true;
                        for (int cc = 1; cc < cols; cc++)
                        {
                            if (!string.IsNullOrWhiteSpace(table.Cells[0, cc].TextString ?? string.Empty))
                            {
                                otherEmpty = false;
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(firstCell) && otherEmpty)
                        {
                            worksheet.Cells[1, 1, 1, cols].Merge = true;
                            worksheet.Cells[1, 1].Style.Font.Bold = true;
                            worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            worksheet.Cells[1, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                        }
                    }
                    catch { /* 忽略 */ }

                    // 自动调整列宽
                    worksheet.Cells.AutoFitColumns();

                    // 基本样式设置
                    worksheet.Cells.Style.Font.Name = "宋体";
                    worksheet.Cells[1, 1, rows, cols].Style.Border.Top.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[1, 1, rows, cols].Style.Border.Bottom.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[1, 1, rows, cols].Style.Border.Left.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[1, 1, rows, cols].Style.Border.Right.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    // 保存文件
                    package.SaveAs(new FileInfo(saveDialog.FileName));
                }

                ed.WriteMessage($"\n设备材料表已成功导出到: {saveDialog.FileName}");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n导出Excel文件时发生错误: {ex.Message}");
            }
        }
    }

}
/// <summary>
/// 属性块块和表格管理命令类
/// </summary>
public class CadCommands
{

    #region
    /// <summary>
    /// 存储属性块的列表（建议改为存储ObjectId，此处暂保留原逻辑）
    /// </summary>
    private static List<BlockTableRecord> attributeBlocks = new List<BlockTableRecord>();
    /// <summary>
    /// 存储块名称与对应数据表格的映射（块属性数据）
    /// </summary>
    private static Dictionary<string, DataTable> blockDataTables = new Dictionary<string, DataTable>();
    /// <summary>
    /// 定义AutoCAD命令：创建带属性的块
    /// </summary>
    [CommandMethod("CreateAttributeBlock")]
    public void CreateAttributeBlock()
    {
        // 获取当前活动文档
        Document doc = Application.DocumentManager.MdiActiveDocument;
        // 获取文档对应的数据库
        Database db = doc.Database;
        // 获取编辑器（用于用户交互）
        Editor ed = doc.Editor;
        // 设置选择选项
        PromptSelectionOptions opts = new PromptSelectionOptions();

        // 提示用户选择图元
        opts.MessageForAdding = "\n请选择一个或多个要添加属性的图元：";

        // 过滤可选择的图元类型（任何图元，包括块、线、圆、弧等所有类型）
        SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "*") }); // 通配符*匹配所有图元类型

        // 执行选择操作
        PromptSelectionResult psr = ed.GetSelection(opts, filter);

        // 如果用户未选择图元，退出方法
        if (psr.Status != PromptStatus.OK)
            return;

        try
        {
            // 启动事务（AutoCAD中操作数据库必须在事务内）
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 打开块表（只读模式）
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                // 获取当前空间（模型空间或图纸空间）
                BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                // 打开模型空间块表记录（可写模式，用于添加新实体）
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // 计算新块的索引（基于现有属性块数量）
                int index = attributeBlocks.Count + 1;
                // 定义块名称（格式：ATTR_BLOCK_0001）
                string blockName = $"ATTR_BLOCK_{index:D4}";

                // 创建新的块定义
                BlockTableRecord atr = new BlockTableRecord();
                atr.Name = blockName;
                // 将块定义添加到块表
                bt.Add(atr);
                // 将新创建的块定义注册到事务（确保数据库持久化）
                tr.AddNewlyCreatedDBObject(atr, true);

                // 创建第一个属性定义（序号）
                AttributeDefinition attDef1 = new AttributeDefinition();
                attDef1.Tag = "序号"; // 属性标签（唯一标识）
                attDef1.Position = new Point3d(0, 0, 0); // 属性位置（块内坐标）
                attDef1.TextString = index.ToString(); // 默认文本
                                                       // 设置属性为可见
                attDef1.Verifiable = true;
                attDef1.Constant = false;
                attDef1.Invisible = false;
                // 将属性定义添加到块定义
                atr.AppendEntity(attDef1);
                tr.AddNewlyCreatedDBObject(attDef1, true);

                // 创建第二个属性定义（部件ID）
                AttributeDefinition attDef2 = new AttributeDefinition();
                attDef2.Tag = "部件ID";
                attDef2.Position = new Point3d(0, -1, 0); // Y轴向下偏移1单位
                attDef2.TextString = $"id{index:D4}";
                attDef2.Verifiable = true;
                attDef2.Constant = false;
                attDef2.Invisible = false;
                atr.AppendEntity(attDef2);
                tr.AddNewlyCreatedDBObject(attDef2, true);

                // 创建第三个属性定义（部件名）
                AttributeDefinition attDef3 = new AttributeDefinition();
                attDef3.Tag = "部件名";
                attDef3.Position = new Point3d(0, -2, 0);
                attDef3.Verifiable = true;
                attDef3.Constant = false;
                attDef3.Invisible = false;
                atr.AppendEntity(attDef3);
                tr.AddNewlyCreatedDBObject(attDef3, true);

                // 创建第四个属性定义（参数）
                AttributeDefinition attDef4 = new AttributeDefinition();
                attDef4.Tag = "参数";
                attDef4.Position = new Point3d(0, -3, 0);
                attDef4.Verifiable = true;
                attDef4.Constant = false;
                attDef4.Invisible = false;
                atr.AppendEntity(attDef4);
                tr.AddNewlyCreatedDBObject(attDef4, true);

                // 将新创建的块定义添加到属性块列表
                attributeBlocks.Add(atr);

                // 收集所有选中图元的图层信息
                Dictionary<string, int> layerCount = new Dictionary<string, int>();
                foreach (SelectedObject so in psr.Value)
                {
                    Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent != null)
                    {
                        if (layerCount.ContainsKey(ent.Layer))
                            layerCount[ent.Layer]++;
                        else
                            layerCount[ent.Layer] = 1;
                    }
                }

                // 确定基准图层
                string baseLayer = "";
                if (layerCount.Count == 1)
                {
                    // 如果所有图元都在同一图层，直接使用该图层
                    baseLayer = layerCount.Keys.First();
                }
                else if (layerCount.Count > 1)
                {
                    // 如果有多个图层，提示用户选择基准图层
                    PromptKeywordOptions pko = new PromptKeywordOptions("\n发现多个图层，请选择基准图层:");
                    foreach (string layer in layerCount.Keys)
                    {
                        pko.Keywords.Add(layer);
                    }
                    pko.AllowNone = false;
                    PromptResult pr = ed.GetKeywords(pko);

                    if (pr.Status == PromptStatus.OK)
                        baseLayer = pr.StringResult;
                    else
                        baseLayer = layerCount.Keys.First(); // 默认使用第一个图层
                }
                // 关键修复：将原始实体添加到块定义中
                foreach (SelectedObject so in psr.Value)
                {
                    Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;

                    // 克隆实体并将其添加到块定义
                    Entity clone = ent.Clone() as Entity;
                    atr.AppendEntity(clone);
                    tr.AddNewlyCreatedDBObject(clone, true);
                }


                // 遍历用户选中的图元，为每个图元创建块引用
                foreach (SelectedObject so in psr.Value)
                {
                    Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;

                    // 获取原始实体的位置和方向
                    Point3d position = Point3d.Origin;
                    Vector3d xAxis = Vector3d.XAxis;
                    Vector3d yAxis = Vector3d.YAxis;
                    double rotation = 0.0;
                    //double scaleDouble = 1.0;
                    //Scale3d scale3D=new Scale3d(0,0,0);
                    Scale3d scale3D = new Scale3d(100, 100, 100);
                    // 处理不同类型的实体
                    if (ent is BlockReference blockRef)
                    {
                        // 块引用：使用其位置和旋转角度
                        position = blockRef.Position;
                        rotation = blockRef.Rotation;
                        xAxis = blockRef.BlockTransform.CoordinateSystem3d.Xaxis;
                        yAxis = blockRef.BlockTransform.CoordinateSystem3d.Yaxis;
                        scale3D = new Scale3d(blockRef.ScaleFactors.X, blockRef.ScaleFactors.Y, blockRef.ScaleFactors.Z);
                    }
                    else
                    {
                        // 其他实体：计算几何中心作为位置
                        try
                        {
                            Extents3d extents = ent.GeometricExtents;
                            position = new Point3d(
                                (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                                (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                                (extents.MinPoint.Z + extents.MaxPoint.Z) / 2
                            );
                        }
                        catch
                        {
                            position = Point3d.Origin;
                        }
                    }

                    // 创建块引用
                    BlockReference br = new BlockReference(position, atr.ObjectId);

                    // 设置旋转角度
                    br.Rotation = rotation;
                    br.Scale(position, scale3D.X);

                    // 设置坐标系
                    if (ent is BlockReference)
                    {
                        br.TransformBy(Matrix3d.AlignCoordinateSystem(
                            Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                            position, xAxis, yAxis, Vector3d.ZAxis
                        ));
                    }
                    // 设置图层
                    if (!string.IsNullOrEmpty(baseLayer))
                        br.Layer = baseLayer;
                    else
                        br.Layer = ent.Layer;
                    // 添加到当前空间
                    currentSpace.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);

                    // 创建属性引用
                    br.AttributeCollection.AppendAttribute(
                        new AttributeReference(attDef1.Position, attDef1.TextString,
                        attDef1.Tag, attDef1.TextStyleId));
                    br.AttributeCollection.AppendAttribute(
                        new AttributeReference(attDef2.Position, attDef2.TextString,
                        attDef2.Tag, attDef2.TextStyleId));
                    br.AttributeCollection.AppendAttribute(
                        new AttributeReference(attDef3.Position, attDef3.TextString,
                        attDef3.Tag, attDef3.TextStyleId));
                    br.AttributeCollection.AppendAttribute(
                        new AttributeReference(attDef4.Position, attDef4.TextString,
                        attDef4.Tag, attDef4.TextStyleId));
                    // 调整属性对齐
                    foreach (ObjectId id in br.AttributeCollection)
                    {
                        AttributeReference ar = tr.GetObject(id, OpenMode.ForWrite) as AttributeReference;
                        ar.AdjustAlignment(db);
                        ar.Layer = br.Layer;
                    }

                    // 删除原图元
                    ent.Erase();
                }

                // 提交事务（保存所有修改）
                tr.Commit();

                // 显示属性编辑对话框
                ShowAttributeDialog(blockName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("创建错误：" + ex.Message);
        }
    }

    /// <summary>
    /// 显示属性编辑对话框
    /// </summary>
    /// <param name="blockName"></param>
    private void ShowAttributeDialog(string blockName)
    {
        // 创建属性编辑表单，传入最新的块定义和数据表格字典
        AttributeForm form = new AttributeForm(attributeBlocks.Last(), blockDataTables);
        // 如果用户点击"确定"，保存数据表格
        if (form.ShowDialog() == DialogResult.OK)
        {
            blockDataTables[blockName] = form.DataTable;
        }
    }

    /// <summary>
    /// 定义AutoCAD命令：编辑属性块
    /// </summary>
    [CommandMethod("EditAttributeBlock")]
    public void EditAttributeBlock()
    {
        Document doc = Application.DocumentManager.MdiActiveDocument;
        Database db = doc.Database;
        Editor ed = doc.Editor;

        // 设置实体选择选项
        PromptEntityOptions peo = new PromptEntityOptions("\n选择要编辑的属性块: ");
        peo.SetRejectMessage("\n这不是一个有效的属性块."); // 拒绝非属性块的提示
        peo.AddAllowedClass(typeof(BlockReference), false); // 只允许选择块引用
                                                            // 执行选择
        PromptEntityResult per = ed.GetEntity(peo);

        if (per.Status != PromptStatus.OK)
            return;

        using (Transaction tr = db.TransactionManager.StartTransaction())
        {
            // 打开选中的块引用（只读模式）
            BlockReference br = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;

            // 获取块引用关联的块定义
            BlockTableRecord btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
            // 检查该块是否为属性块（通过名称匹配）
            if (!attributeBlocks.Any(ab => ab.Name == btr.Name))
            {
                //ed.WriteMessage("\n这不是一个有效的属性块.");
                //return;
            }

            // 显示编辑表单
            AttributeForm form = new AttributeForm(btr, blockDataTables);
            if (form.ShowDialog() == DialogResult.OK)
            {
                // 保存编辑后的数据表格
                blockDataTables[btr.Name] = form.DataTable;
                // 更新块引用的属性值
                UpdateAttributes(tr, br, form.DataTable);
            }

            tr.Commit();
        }
    }

    /// <summary>
    /// 更新块引用的属性值
    /// </summary>
    /// <param name="tr"></param>
    /// <param name="br"></param>
    /// <param name="dataTable"></param>
    private void UpdateAttributes(Transaction tr, BlockReference br, DataTable dataTable)
    {
        AttributeCollection ac = br.AttributeCollection;
        // 遍历所有属性引用，更新文本值
        for (int i = 0; i < ac.Count; i++)
        {
            AttributeReference ar = tr.GetObject(ac[i], OpenMode.ForWrite) as AttributeReference;
            // 从数据表格中获取对应标签的属性值
            if (dataTable.Columns.Contains(ar.Tag))
            {
                ar.TextString = dataTable.Rows[0][ar.Tag].ToString();
            }
        }
    }

    #endregion
}

/// <summary>
/// 属性编辑表单
/// </summary>
public partial class AttributeForm : Form
{
    private BlockTableRecord _blockTableRecord; // 关联的块定义
    private DataGridView dataGridView; // 数据表格控件
    private Button btnAddRow; // 添加行按钮
    private Button btnDeleteRow; // 删除行按钮
    private Button btnSave; // 保存按钮
    private Button btnCancel; // 取消按钮
    private DataTable _dataTable; // 存储属性数据的表格

    /// <summary>
    /// 公开数据表格供外部访问
    /// </summary>
    public DataTable DataTable => _dataTable;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="blockTableRecord"></param>
    /// <param name="blockDataTables"></param>
    public AttributeForm(BlockTableRecord blockTableRecord, Dictionary<string, DataTable> blockDataTables)
    {
        InitializeComponent();
        _blockTableRecord = blockTableRecord;

        // 加载已有数据或初始化新表格
        if (blockDataTables.ContainsKey(_blockTableRecord.Name))
        {
            _dataTable = blockDataTables[_blockTableRecord.Name].Copy();
        }
        else
        {
            _dataTable = new DataTable();
            _dataTable.Columns.Add("序号", typeof(int));
            _dataTable.Columns.Add("部件ID", typeof(string));
            _dataTable.Columns.Add("部件名", typeof(string));
            _dataTable.Columns.Add("参数", typeof(string));

            // 添加初始行
            DataRow row = _dataTable.NewRow();
            row["序号"] = _dataTable.Rows.Count + 1;
            row["部件ID"] = $"id{_dataTable.Rows.Count + 1:D4}";
            _dataTable.Rows.Add(row);
        }
        if (dataGridView is null)
        {
            return;
        }
        else
        {
            // 绑定数据到表格控件
            dataGridView.DataSource = _dataTable;

            // 设置列标题
            dataGridView.Columns["序号"].HeaderText = "序号";
            dataGridView.Columns["部件ID"].HeaderText = "部件ID";
            dataGridView.Columns["部件名"].HeaderText = "部件名";
            dataGridView.Columns["参数"].HeaderText = "参数";
            // 设置序号列为只读
            dataGridView.Columns["序号"].ReadOnly = true;
            dataGridView.Columns["部件ID"].ReadOnly = true;
        }

    }

    /// <summary>
    /// 初始化表单控件
    /// </summary>
    private void InitializeComponent()
    {
        this.dataGridView = new DataGridView();
        this.btnAddRow = new Button();
        this.btnDeleteRow = new Button();
        this.btnSave = new Button();
        this.btnCancel = new Button();
        ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
        this.SuspendLayout();

        // 数据表格控件设置
        this.dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dataGridView.Location = new Point(12, 12);
        this.dataGridView.Size = new Size(560, 300);
        this.dataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.dataGridView.AllowUserToAddRows = false;
        this.dataGridView.AllowUserToDeleteRows = false;
        this.dataGridView.ReadOnly = false;
        this.dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        // 添加行按钮
        this.btnAddRow.Text = "添加一行属性";
        this.btnAddRow.Location = new Point(12, 318);
        this.btnAddRow.Size = new Size(120, 30);
        this.btnAddRow.Click += new EventHandler(this.btnAddRow_Click);

        // 删除行按钮
        this.btnDeleteRow.Text = "删除选定属性";
        this.btnDeleteRow.Location = new Point(142, 318);
        this.btnDeleteRow.Size = new Size(120, 30);
        this.btnDeleteRow.Click += new EventHandler(this.btnDeleteRow_Click);

        // 保存按钮
        this.btnSave.Text = "确定";
        this.btnSave.DialogResult = DialogResult.OK;
        this.btnSave.Location = new Point(392, 318);
        this.btnSave.Size = new Size(80, 30);
        this.btnSave.Click += new EventHandler(this.btnSave_Click);

        // 取消按钮
        this.btnCancel.Text = "取消";
        this.btnCancel.DialogResult = DialogResult.Cancel;
        this.btnCancel.Location = new Point(482, 318);
        this.btnCancel.Size = new Size(80, 30);

        // 表单设置
        this.ClientSize = new Size(584, 361);
        this.Controls.Add(this.dataGridView);
        this.Controls.Add(this.btnAddRow);
        this.Controls.Add(this.btnDeleteRow);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.btnCancel);
        this.MinimumSize = new Size(600, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "自定义属性 - ";

        this.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
    }

    /// <summary>
    /// 添加行按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnAddRow_Click(object sender, EventArgs e)
    {
        DataRow row = _dataTable.NewRow();
        row["序号"] = _dataTable.Rows.Count + 1;
        row["部件ID"] = $"id{_dataTable.Rows.Count + 1:D4}";
        _dataTable.Rows.Add(row);

        // 滚动到最后一行
        dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.RowCount - 1;
    }

    /// <summary>
    /// 删除行按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnDeleteRow_Click(object sender, EventArgs e)
    {
        if (dataGridView.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择要删除的行！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show("确定要删除选中的行吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
        {
            return;
        }

        // 反向遍历选中行，避免集合修改异常
        var selectedIndices = dataGridView.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.Index)
            .OrderByDescending(i => i)
            .ToList();

        foreach (int index in selectedIndices)
        {
            if (index >= 0 && index < _dataTable.Rows.Count)
            {
                _dataTable.Rows.RemoveAt(index);
            }
        }

        // 重新计算序号
        for (int i = 0; i < _dataTable.Rows.Count; i++)
        {
            _dataTable.Rows[i]["序号"] = i + 1;
            _dataTable.Rows[i]["部件ID"] = $"id{i + 1:D4}";
        }
    }

    /// <summary>
    /// 保存按钮点击事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void btnSave_Click(object sender, EventArgs e)
    {
        dataGridView.EndEdit(); // 结束编辑，保存修改

        // 验证数据
        foreach (DataRow row in _dataTable.Rows)
        {
            if (string.IsNullOrWhiteSpace(row["部件名"]?.ToString()))
            {
                MessageBox.Show("部件名不能为空！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
    }
}

/// <summary>
/// 动态块信息结构
/// </summary>
public class DynamicBlockInfo
{
    public Point3d StartPoint { get; set; }     // 起点坐标
    public Point3d EndPoint { get; set; }       // 终点坐标
    public Point3d MidPoint { get; set; }       // 中点坐标
    public double Length { get; set; }          // 长度
    public double Rotation { get; set; }        // 旋转角度（弧度）
    public Point3d Position { get; set; }       // 插入点坐标
}
