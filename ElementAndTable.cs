
// Excel操作命名空间
using Mysqlx.Crud;
using OfficeOpenXml;
using System.Data;
using System.Drawing;
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
            Attributes = new Dictionary<string, string>();
            EnglishNames = new Dictionary<string, string>();
            Count = 1;  // 默认数量为1
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
                { "介质", "Medium Name" },
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
                { "备注", "Remark" }
            };

        /// <summary>
        /// 主命令：生成设备材料表
        /// </summary>
        [CommandMethod("GenerateEquipmentTable")]
        public void GenerateEquipmentTable()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            try
            {
                // 选择属性块
                List<EquipmentInfo> equipmentList = SelectAndAnalyzeBlocks(ed, db);
                if (equipmentList == null || equipmentList.Count == 0)
                {
                    ed.WriteMessage("\n没有选择到有效的属性块。");
                    return;
                }
                // 生成表格
                CreateEquipmentTable(db, equipmentList);
                ed.WriteMessage($"\n成功生成设备材料表，共包含 {equipmentList.Count} 种设备。");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n生成设备表时发生错误: {ex.Message}");
            }
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

        /// <summary>
        /// 同步管道\属性
        /// </summary>        
        [CommandMethod("SyncPipeProperties")]
        public void SyncPipeProperties()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            //获取选择的线段
            var lineSelResult = ed.GetSelection(
                new PromptSelectionOptions { MessageForAdding = "\n请选择要同步的线段 (LINE 或 LWPOLYLINE):" },
                new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LINE,LWPOLYLINE") })
            );
            if (lineSelResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n操作取消。");
                return;
            }
            var sourceLineIds = lineSelResult.Value.GetObjectIds().ToList();//获取选择的线段
            var blockSelResult = ed.GetEntity("\n请选择示例管线块:");//选择示例管线块提示用户选择一个块参照
            if (blockSelResult.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n操作取消。");
                return;
            }
            using (var tr = new DBTrans())
            {
                try
                {
                    //获取选择的块参照
                    var sampleBlockRef = tr.GetObject(blockSelResult.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (sampleBlockRef == null)
                    {
                        ed.WriteMessage("\n错误：选择的不是块参照。");
                        return;
                    }
                    //获取示例块信息
                    var sampleInfo = AnalyzeSampleBlock(tr, sampleBlockRef);
                    if (sampleInfo.PipeBodyTemplate == null)
                    {
                        ed.WriteMessage("\n错误：示例块中未找到作为管道主体的 Polyline。");
                        return;
                    }
                    //获取选择的线段信息
                    var lineSegments = CollectLineSegments(tr, sourceLineIds);
                    if (lineSegments == null || lineSegments.Count == 0)
                    {
                        ed.WriteMessage("\n未找到可处理的线段。");
                        return;
                    }
                    // 1. 构建连续顶点与总长度
                    var orderedVertices = BuildOrderedVerticesFromSegments(lineSegments, 0.1);
                    if (orderedVertices.Count < 2)
                    {
                        ed.WriteMessage("\n顶点不足，无法生成管线。");
                        return;
                    }
                    double pipelineLength = 0.0;//管道总长度
                    for (int i = 0; i < orderedVertices.Count - 1; i++)//遍历顶点
                        pipelineLength += orderedVertices[i].DistanceTo(orderedVertices[i + 1]);//计算总长度
                    // 2. 中点与角度（用于插入点）
                    var (midPoint, midAngle) = ComputeMidPointAndAngle(orderedVertices, pipelineLength);

                    // 目标方向：仅依据中点附近线段方向（保证箭头朝向正确流向）
                    Vector3d targetDir = ComputeDirectionAtPoint(orderedVertices, midPoint, 1e-6);
                    Vector3d segmentDir = ComputeAggregateSegmentDirection(lineSegments);
                    if (!segmentDir.IsZeroLength() && targetDir.DotProduct(segmentDir) < 0)
                    {
                        targetDir = -targetDir;
                    }

                    // 3. 管线主体（局部，不旋转）
                    Polyline pipeLocal = BuildPipePolylineLocal(sampleInfo.PipeBodyTemplate, orderedVertices, midPoint);

                    // 4. 箭头模板（局部朝 +X）
                    var (arrowOutline, arrowFill) = EnsureArrowEntities(sampleInfo, sampleBlockRef.Name);

                    // 5. 属性（局部）
                    var attDefsLocal = CloneAttributeDefinitionsLocal(sampleInfo.AttributeDefinitions, midPoint, 0.0, pipelineLength, sampleBlockRef.Name);

                    // 6. 坐标系对齐矩阵：局部 +X → 起点→终点的整体方向
                    Vector3d targetY = Vector3d.ZAxis.CrossProduct(targetDir).GetNormal();
                    if (targetY.IsZeroLength())
                        targetY = Vector3d.YAxis;
                    // 目标坐标系
                    Matrix3d alignLocal = Matrix3d.AlignCoordinateSystem(
                        Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                        Point3d.Origin, targetDir, targetY, Vector3d.ZAxis
                    );

                    // 管线主体：不旋转
                    var pipeAligned = (Polyline)pipeLocal.Clone();

                    // 箭头：对齐到整体方向（尖端朝向终点）
                    var arrowOutlineAligned = (Polyline)arrowOutline.Clone();
                    // 对齐
                    arrowOutlineAligned.TransformBy(alignLocal);
                    // 填充
                    Solid? arrowFillAligned = null;
                    if (arrowFill != null)
                    {
                        arrowFillAligned = (Solid)arrowFill.Clone();// 克隆填充实体
                        arrowFillAligned.TransformBy(alignLocal);// 对齐 
                    }
                    // 对箭头进行对齐 箭头位置：中点处 + 起点处 + 终点处
                    const double ArrowOffsetFromEnds = 20.0;
                    // 对齐 箭头
                    var (startArrowPoint, startDirRaw) = SamplePointAndDirectionAlongPath(orderedVertices, Math.Min(ArrowOffsetFromEnds, pipelineLength));
                    // 对齐 起点
                    var (endArrowPoint, endDirRaw) = SamplePointAndDirectionAlongPath(orderedVertices, Math.Max(0.0, pipelineLength - ArrowOffsetFromEnds));
                    // 起点
                    Vector3d startOffsetLocal = startArrowPoint - midPoint;
                    // 终点
                    Vector3d endOffsetLocal = endArrowPoint - midPoint;
                   // 目标方向
                    Vector3d targetDirNormalized = targetDir.IsZeroLength() ? Vector3d.XAxis : targetDir.GetNormal();
                    // 起点向量
                    Vector3d startDir = startDirRaw.IsZeroLength()? targetDirNormalized : startDirRaw.GetNormal();
                    if (targetDirNormalized.DotProduct(startDir) < 0)
                        startDir = -startDir;
                    // 终点向量
                    Vector3d endDir = endDirRaw.IsZeroLength() ? targetDirNormalized : endDirRaw.GetNormal();
                    if (targetDirNormalized.DotProduct(endDir) < 0)
                        endDir = -endDir;
                    // 中点向量 对齐箭头实体
                    var (midArrowOutline, midArrowFill) = AlignArrowToDirection(arrowOutline, arrowFill, targetDirNormalized);
                    // 箭头实体
                    var arrowEntities = new List<Entity> { midArrowOutline };
                    if (midArrowFill != null)
                    {
                        arrowEntities.Add(midArrowFill);
                    }
                    // 箭头偏移
                    if (startOffsetLocal.Length > 1e-3)
                    {
                        // 起始箭头
                        var (startArrow, startFill) = AlignArrowToDirection(arrowOutline, arrowFill, startDir);
                        // 起始箭头偏移
                        startArrow.TransformBy(Matrix3d.Displacement(startOffsetLocal));
                        // 起始箭头填充
                        arrowEntities.Add(startArrow);
                        if (startFill != null)
                        {
                            // 起始箭头填充
                            startFill.TransformBy(Matrix3d.Displacement(startOffsetLocal));
                            arrowEntities.Add(startFill);
                        }
                    }
                    // 结束箭头 
                    if (endOffsetLocal.Length > 1e-3)
                    {
                        // 结束箭头
                        var (endArrow, endFill) = AlignArrowToDirection(arrowOutline, arrowFill, endDir);
                        // 偏移
                        endArrow.TransformBy(Matrix3d.Displacement(endOffsetLocal));
                        arrowEntities.Add(endArrow);//添加结束箭头
                        if (endFill != null)
                        {
                            endFill.TransformBy(Matrix3d.Displacement(endOffsetLocal));
                            arrowEntities.Add(endFill);
                        }
                    }
                    // 7. 以中点为插入点、旋转=0 创建并插入块（箭头与管线在中点处重合且同向）
                    string desiredName = sampleBlockRef.Name;
                    // 创建块  
                    string newBlockName = BuildPipeBlockDefinition(tr, desiredName, pipeAligned, arrowEntities, attDefsLocal);
                    // 属性字典赋值
                    var attValues = attDefsLocal.ToDictionary(a => a.Tag, a => a.TextString ?? string.Empty);
                    // 插入块并返回块ID
                    var newBrId = InsertPipeBlockWithAttributes(tr, midPoint, newBlockName, 0.0, attValues);
                    // 获取块引用
                    var newBr = tr.GetObject(newBrId, OpenMode.ForWrite) as BlockReference;
                    if (newBr != null)
                    {
                        newBr.Layer = sampleInfo.PipeBodyTemplate.Layer;//设置块的图层
                    }
                    foreach (var line in lineSegments)
                    {
                        var lineWrite = tr.GetObject(line.Id, OpenMode.ForWrite) as Line;//获取对象
                        lineWrite.Erase();//删除线
                    }
                    tr.Commit();
                    ed.WriteMessage("\n管线块已生成：基点=中点，方向顺时针90°，包含管线、方向箭头与属性。");
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n发生错误: {ex.Message}\n{ex.StackTrace}");
                    tr.Abort();
                }
            }
        }
        /// <summary>
        /// 创建管道
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static (Point3d point, Vector3d direction) SamplePointAndDirectionAlongPath(List<Point3d> vertices, double distance)
        {
            if (vertices == null || vertices.Count == 0)
                return (Point3d.Origin, Vector3d.XAxis);
            if (vertices.Count == 1)
                return (vertices[0], Vector3d.XAxis);

            double target = Math.Max(0.0, distance);

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                Point3d start = vertices[i];
                Point3d end = vertices[i + 1];
                Vector3d delta = end - start;
                double segLen = start.DistanceTo(end);
                if (segLen <= 0)
                    continue;

                if (target <= segLen)
                {
                    double t = target / segLen;
                    var sampled = new Point3d(
                        start.X + delta.X * t,
                        start.Y + delta.Y * t,
                        start.Z + delta.Z * t
                    );
                    Vector3d dir = delta.IsZeroLength() ? Vector3d.XAxis : delta.GetNormal();
                    return (sampled, dir);
                }

                target -= segLen;
            }

            Vector3d fallbackDir = (vertices.Last() - vertices[vertices.Count - 2]).GetNormal();
            if (fallbackDir.IsZeroLength())
                fallbackDir = Vector3d.XAxis;
            return (vertices.Last(), fallbackDir);
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
        /// 获取两个点之间的方向
        /// </summary>
        /// <param name="origin">起点</param>
        /// <param name="flowDir">流向</param>
        /// <returns></returns>
        private (Matrix3d worldToLocal, Matrix3d localToWorld) CreatePipeCoordinateSystem(Point3d origin, Vector3d flowDir)
        {
            Vector3d xAxis = flowDir.IsZeroLength() ? Vector3d.XAxis : flowDir.GetNormal();
            Vector3d yAxis = Vector3d.ZAxis.CrossProduct(xAxis).IsZeroLength()
                ? Vector3d.YAxis
                : Vector3d.ZAxis.CrossProduct(xAxis).GetNormal();

            Matrix3d localToWorld = Matrix3d.AlignCoordinateSystem(
                Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                origin, xAxis, yAxis, Vector3d.ZAxis);

            return (localToWorld.Inverse(), localToWorld);
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
                for (int i = 0; i < uniquePoints.Count; i++)//
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
        /// 计算总列数
        /// </summary>
        private int CalculateTotalColumns(List<EquipmentInfo> equipmentList)
        {
            int fixedColumns = 9; // 固定的前9列
            int dynamicColumns = 0;

            foreach (var equipment in equipmentList)
            {
                dynamicColumns += equipment.Attributes.Count;
            }

            return fixedColumns + dynamicColumns;
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

                //table.SetGridLineWeight(LineWeight.LineWeight025, (int)allLines);

                // 设置边框颜色为黑色
                //table.SetGridColor(Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 7), (int)allLines);

                //table.GridColor(7, (int)allLines);
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

            // 填充固定列头
            FillFixedHeaders(table);

            // 填充动态列头和数据
            FillDynamicContent(table, equipmentList);
        }

        /// <summary>
        /// 填充固定列头 0-8列
        /// </summary>
        private void FillFixedHeaders(Table table)
        {
            // 第1列
            // 合并第1列中的第2-4行(注意：CAD表格索引从0开始)
            // 合并单元格方法参数：起始行、起始列、结束行、结束列
            table.MergeCells(CellRange.Create(table, 1, 0, 2, 0));
            table.Cells[1, 0].TextString = "管段号\nPipeline\nNo.";
            //table.Cells[2, 0].TextString = "Pipeline";
            //table.Cells[3, 0].TextString = "No.";

            //合并 第2行的第2-3列
            table.MergeCells(CellRange.Create(table, 1, 1, 1, 2));
            table.Cells[1, 1].TextString = "管段起止点\nPipeline From Start To End";
            //合并 第2列的第2-3行
            //table.MergeCells(CellRange.Create(table, 2, 1, 3, 1));
            table.Cells[2, 1].TextString = "起点\nFrom";
            //table.Cells[3, 1].TextString = "From";
            //合并 第3列的第2-3行
            //table.MergeCells(CellRange.Create(table, 2, 2, 3, 2));
            table.Cells[2, 2].TextString = "终点\nTo";
            //table.Cells[3, 2].TextString = "To";

            // 合并 第4列的第3-4行
            table.MergeCells(CellRange.Create(table, 1, 3, 2, 3));
            table.Cells[1, 3].TextString = "管道\n等级\nPipe Class";
            //table.Cells[2, 3].TextString = "等级";
            //table.Cells[3, 3].TextString = "Pipe Class";

            // 合并 第2行的第5-7列
            table.MergeCells(CellRange.Create(table, 1, 4, 1, 6));
            table.Cells[1, 4].TextString = "设计条件 \nDesign Condition";
            // 合并 第5列的第2-3行
            table.MergeCells(CellRange.Create(table, 2, 4, 2, 4));
            table.Cells[2, 4].TextString = "介质名称\nMedium Name";
            //table.Cells[3, 4].TextString = "Medium Name";
            // 合并 第6列的第2-3行
            //table.MergeCells(CellRange.Create(table, 2,5, 3, 5));
            table.Cells[2, 5].TextString = "操作温度\nT(℃)";
            //table.Cells[3, 5].TextString = "T(℃)";
            // 合并 第7列的第2-3行
            //table.MergeCells(CellRange.Create(table, 2, 6, 3, 6));
            table.Cells[2, 6].TextString = "操作压力\nP(MPaG)";
            //table.Cells[3, 6].TextString = "P(MPaG)";

            // 第8-9列合并
            table.MergeCells(CellRange.Create(table, 1, 7, 1, 8));
            table.Cells[1, 7].TextString = "隔热及防腐 \nInsul.& Antisepsis";
            // 合并 第8列的第2-3行
            //table.MergeCells(CellRange.Create(table, 2, 7, 3, 7));
            table.Cells[2, 7].TextString = "隔热隔声代号\nCode";
            //table.Cells[3, 7].TextString = "Code";
            // 合并 第9列的第2-3行
            //table.MergeCells(CellRange.Create(table, 2, 8, 3, 8));
            table.Cells[2, 8].TextString = "是否防腐\nAntisepsis";
            //table.Cells[3, 8].TextString = "Antisepsis";
        }

        /// <summary>
        /// 填充动态内容
        /// </summary>
        private void FillDynamicContent(Table table, List<EquipmentInfo> equipmentList)
        {
            int currentColumn = 9; // 从第10列开始

            foreach (var equipment in equipmentList)
            {
                //设置开始列
                int startColumn = currentColumn;
                //设置列数
                int columnSpan = equipment.Attributes.Count;
                //获取结果
                string result = "";
                // 合并第一行作为设备名称
                if (columnSpan > 1)
                {
                    //合并单元格：把这个零部件的名称合并到所有属性列；
                    table.MergeCells(CellRange.Create(table, 1, startColumn, 1, startColumn + columnSpan - 1));
                }

                //var name = equipment.Attributes["管道标题"];


                try
                {
                    result = equipment.Attributes["管道标题"];
                }
                catch
                {
                    // 查找最后一个下划线'_'的位置
                    int lastUnderscoreIndex = equipment.Name.LastIndexOf('_');

                    // 检查是否找到下划线
                    if (lastUnderscoreIndex >= 0)
                    {
                        // 计算截取起始位置（最后一个下划线后一位）
                        int startIndex = lastUnderscoreIndex + 1;

                        // 检查截取位置是否有效（避免超出字符串长度）
                        if (startIndex <= equipment.Name.Length - 1)
                        {
                            // 截取下划线后的子字符串
                            result = equipment.Name.Substring(startIndex);
                            // 输出结果
                            Console.WriteLine("截取结果: " + result);
                        }
                        else
                        {
                            // 处理下划线在末尾的特殊情况
                            Console.WriteLine("下划线位于字符串末尾，无后续内容");
                        }
                    }
                    else
                    {
                        // 处理未找到下划线的情况
                        Console.WriteLine("字符串中未找到下划线");
                    }
                }





                // 填充零部件名称
                table.Cells[1, startColumn].TextString = result;
                // 设置名称列居中
                table.Cells[1, startColumn].Alignment = CellAlignment.MiddleCenter;

                // 填充属性名和英文名
                int colIndex = 0;
                // 遍历这个零部件的所有属性
                foreach (var attr in equipment.Attributes)
                {
                    //先计算出列索引
                    int col = startColumn + colIndex;
                    string englishName = equipment.EnglishNames.ContainsKey(attr.Key)
                        ? equipment.EnglishNames[attr.Key]
                        : attr.Key;
                    //table.Cells[3, col].TextString = englishName;
                    //填充属性名
                    //table.Cells[2, col].TextString = attr.Key + "\n" + englishName;
                    table.Cells[2, col].TextString = attr.Key;
                    colIndex++;
                }

                // 填充属性值（第4行，索引为3）
                colIndex = 0;
                foreach (var attr in equipment.Attributes)
                {
                    int col = startColumn + colIndex;
                    string value = attr.Value;

                    // 如果是数量列，显示统计数量
                    if (attr.Key == "数量" || attr.Key.ToLower().Contains("quantity"))
                    {
                        value = equipment.Count.ToString();
                    }

                    table.Cells[3, col].TextString = value;
                    colIndex++;
                }

                currentColumn += columnSpan;
            }

            // 自适应列宽
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
        /// 导出表格到Excel文件
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

                    // 复制表格数据到Excel
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        for (int j = 0; j < table.Columns.Count; j++)
                        {
                            string cellText = table.Cells[i, j].TextString;
                            worksheet.Cells[i + 1, j + 1].Value = cellText;
                        }
                    }

                    // 处理合并单元格（简化处理）
                    // 标题行合并
                    if (table.Columns.Count > 1)
                    {
                        worksheet.Cells[1, 1, 1, table.Columns.Count].Merge = true;
                        worksheet.Cells[1, 1, 1, table.Columns.Count].Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    // 自动调整列宽
                    worksheet.Cells.AutoFitColumns();

                    // 设置样式
                    worksheet.Cells.Style.Font.Name = "宋体";
                    worksheet.Cells[1, 1, 1, table.Columns.Count].Style.Font.Bold = true;
                    worksheet.Cells[1, 1, table.Rows.Count, table.Columns.Count].Style.Border.Top.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[1, 1, table.Rows.Count, table.Columns.Count].Style.Border.Bottom.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[1, 1, table.Rows.Count, table.Columns.Count].Style.Border.Left.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    worksheet.Cells[1, 1, table.Rows.Count, table.Columns.Count].Style.Border.Right.Style =
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
}




public class CadCommands
{
    /// <summary>
    /// 设备属性块信息类（用于存储CAD块中的设备信息）
    /// </summary>
    //public class EquipmentInfo
    //{
    //    // 设备名称
    //    public string Name { get; set; }

    //    // 设备类型（如阀门、法兰等）
    //    public string Type { get; set; }

    //    // 属性字典（中文属性名-值）
    //    public Dictionary<string, string> Attributes { get; set; }

    //    // 英文属性名对照（中文属性名-英文属性名）
    //    public Dictionary<string, string> EnglishNames { get; set; }

    //    // 相同设备的数量统计
    //    public int Count { get; set; }

    //    // 构造函数初始化字典和默认值
    //    public EquipmentInfo()
    //    {
    //        Attributes = new Dictionary<string, string>();
    //        EnglishNames = new Dictionary<string, string>();
    //        Count = 1;  // 默认数量为1
    //    }
    //}


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

                    //Matrix3d mt = Matrix3d.Displacement(Point3d.0rigin - insertPoint);//对选择集内的图块进行原点偏移 矩阵偏移
                    //ObjectIdCollection objCol = new ObjectIdCollection();
                    //foreach (var id in acSSet.GetobjectIds())
                    //{
                    //    Entity ent = acTrans.GetObject(id, OpenMode.ForWrite) as Entity;
                    //    ent.TransformBy(mt);
                    //    objCo1.Add(id);

                    //}

                    //newBhockTableRecord.Assume0wershipof(objCo1); //添加图元 已经完成了块表的定义
                    //// 将块引用添加到模型空间
                    //btr.AppendEntity(br);
                    //tr.AddNewlyCreatedDBObject(br, true);

                    //// 调整属性引用的对齐方式
                    //AttributeCollection ac = br.AttributeCollection;
                    //foreach (ObjectId id in ac)
                    //{
                    //    AttributeReference ar = tr.GetObject(id, OpenMode.ForWrite) as AttributeReference;
                    //    ar.AdjustAlignment(db);
                    //    ar.Layer = br.Layer;
                    //}

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

    /// <summary>
    /// 定义AutoCAD命令：生成设备表
    /// </summary>
    //[CommandMethod("GenerateEquipmentTable1")]
    //public void GenerateEquipmentTable1()
    //{
    //    Document doc = Application.DocumentManager.MdiActiveDocument;
    //    Database db = doc.Database;
    //    Editor ed = doc.Editor;

    //    // 选择要生成表格的属性块
    //    PromptSelectionOptions opts = new PromptSelectionOptions();
    //    opts.MessageForAdding = "\n选择要生成设备表的属性块: ";
    //    // 过滤块引用（INSERT是块引用的DXF代码）
    //    SelectionFilter filter = new SelectionFilter(new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") });
    //    PromptSelectionResult psr = ed.GetSelection(opts, filter);

    //    if (psr.Status != PromptStatus.OK)
    //        return;

    //    using (Transaction tr = db.TransactionManager.StartTransaction())
    //    {
    //        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    //        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

    //        // 创建合并的数据表格（用于生成设备表）
    //        DataTable combinedTable = new DataTable();
    //        combinedTable.Columns.Add("序号", typeof(int));
    //        combinedTable.Columns.Add("部件序号", typeof(string));

    //        // 收集所有唯一的属性标签（列名）
    //        HashSet<string> uniqueTags = new HashSet<string>();
    //        foreach (SelectedObject so in psr.Value)
    //        {
    //            BlockReference br = tr.GetObject(so.ObjectId, OpenMode.ForRead) as BlockReference;
    //            BlockTableRecord atr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
    //            if (blockDataTables.ContainsKey(atr.Name))
    //            {
    //                DataTable blockTable = blockDataTables[atr.Name];
    //                foreach (DataColumn column in blockTable.Columns)
    //                {
    //                    if (!combinedTable.Columns.Contains(column.ColumnName))
    //                        combinedTable.Columns.Add(column.ColumnName, typeof(string));
    //                    uniqueTags.Add(column.ColumnName);
    //                }
    //            }
    //        }

    //        // 添加固定列
    //        combinedTable.Columns.Add("数量", typeof(int));
    //        combinedTable.Columns.Add("备注", typeof(string));

    //        // 填充数据行
    //        int rowIndex = 1;
    //        foreach (SelectedObject so in psr.Value)
    //        {
    //            BlockReference br = tr.GetObject(so.ObjectId, OpenMode.ForRead) as BlockReference;
    //            BlockTableRecord atr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
    //            if (blockDataTables.ContainsKey(atr.Name))
    //            {
    //                DataRow row = combinedTable.NewRow();
    //                row["序号"] = rowIndex++;
    //                // 提取部件序号（块名称的后4位）
    //                row["部件序号"] = atr.Name.Substring(atr.Name.Length - 4);

    //                DataTable blockTable = blockDataTables[atr.Name];
    //                // 填充属性值
    //                foreach (string tag in uniqueTags)
    //                {
    //                    if (blockTable.Columns.Contains(tag))
    //                        row[tag] = blockTable.Rows[0][tag];
    //                }

    //                row["数量"] = 1;
    //                row["备注"] = "";

    //                combinedTable.Rows.Add(row);
    //            }
    //        }

    //        // 创建AutoCAD表格
    //        Table table = new Table();
    //        // 使用标准表格样式
    //        //table.TableStyle = db.StandardTableStyle;

    //        // 设置表格大小（行数=数据行+表头，列数=总列数）
    //        table.SetSize(combinedTable.Rows.Count + 1, combinedTable.Columns.Count);

    //        // 设置单元格尺寸
    //        table.SetRowHeight(4);
    //        //table.SetColumnWidth(8);
    //        table.SetColumnWidth(25);
    //        // 设置标题行高度
    //        table.Rows[0].Height = 5;





    //        // 填充表头
    //        for (int col = 0; col < combinedTable.Columns.Count; col++)
    //        {
    //            table.Cells[0, col].TextHeight = 3;
    //            table.Cells[0, col].Value = combinedTable.Columns[col].ColumnName; // 列名
    //            table.Cells[0, col].Alignment = CellAlignment.MiddleCenter;
    //        }

    //        // 填充数据
    //        for (int row = 0; row < combinedTable.Rows.Count; row++)
    //        {
    //            for (int col = 0; col < combinedTable.Columns.Count; col++)
    //            {
    //                table.Cells[row + 1, col].TextHeight = 2;
    //                table.Cells[row + 1, col].Value = combinedTable.Rows[row][col].ToString();
    //            }
    //        }
    //        // 调整列宽根据内容
    //        for (int col = 0; col < combinedTable.Columns.Count; col++)
    //        {
    //            double maxWidth = 0;

    //            // 查找该列的最大内容宽度
    //            for (int row = 0; row < combinedTable.Rows.Count + 1; row++)
    //            {
    //                // 获取单元格文本
    //                //string cellText = table.GetCellText(row, col);

    //                string cellText = table.GetTextString(row, col, 0);
    //                // 获取内容的宽度
    //                double textWidth = GetTextWidth(cellText);/*选择合适的字体和高度*/
    //                maxWidth = Math.Max(maxWidth, textWidth);
    //            }

    //            // 设置列宽
    //            table.SetColumnWidth(col, maxWidth + 2); // 额外增加一些间距
    //        }


    //        // 让用户指定表格插入位置
    //        PromptPointResult ppr = ed.GetPoint("\n指定插入位置: ");
    //        if (ppr.Status == PromptStatus.OK)
    //        {
    //            table.Position = ppr.Value; // 设置表格位置
    //            btr.AppendEntity(table);
    //            tr.AddNewlyCreatedDBObject(table, true);
    //        }

    //        tr.Commit();
    //    }
    //}


    //private double GetTextWidth(string text)/* 参数以适合字体和大小 */
    //{
    //    // 使用 AutoCAD 的工具计算文本的宽度
    //    // 这里需要实现具体的逻辑来根据字体、大小计算文本宽度
    //    double width = 0;

    //    // 示例返回值，需具体实现
    //    return width;
    //}



    /// <summary>
    /// 定义AutoCAD命令：导出到Excel
    /// </summary>
    //[CommandMethod("ExportToExcel")]
    //public void ExportToExcel()
    //{
    //    // 设置EPPlus许可上下文（非商业用途）
    //    // ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    //    Document doc = Application.DocumentManager.MdiActiveDocument;
    //    Database db = doc.Database;
    //    Editor ed = doc.Editor;

    //    // 获取保存文件路径（修正后）
    //    PromptSaveFileOptions pfo = new PromptSaveFileOptions("\n保存为Excel文件");
    //    pfo.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
    //    pfo.DialogName = "xlsx";
    //    PromptFileNameResult pfnr = ed.GetFileNameForSave(pfo);

    //    if (pfnr.Status != PromptStatus.OK)
    //        return;

    //    FileInfo fileInfo = new FileInfo(pfnr.StringResult);
    //    using (ExcelPackage package = new ExcelPackage(fileInfo)) // 使用using确保释放资源
    //    {
    //        // 添加工作表
    //        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("设备表");

    //        int rowIndex = 1;
    //        // 遍历所有块数据表格，写入Excel
    //        foreach (KeyValuePair<string, DataTable> entry in blockDataTables)
    //        {
    //            // 写入块名称
    //            worksheet.Cells[$"A{rowIndex}"].Value = "部件序号";
    //            worksheet.Cells[$"B{rowIndex}"].Value = entry.Key.Substring(entry.Key.Length - 4);
    //            rowIndex++;

    //            // 写入列标题
    //            for (int col = 0; col < entry.Value.Columns.Count; col++)
    //            {
    //                string columnName = GetExcelColumnName(col);
    //                worksheet.Cells[$"{columnName}{rowIndex}"].Value = entry.Value.Columns[col].ColumnName;
    //                worksheet.Cells[$"{columnName}{rowIndex}"].Style.Font.Bold = true;
    //            }
    //            rowIndex++;

    //            // 写入数据行
    //            foreach (DataRow row in entry.Value.Rows)
    //            {
    //                for (int col = 0; col < entry.Value.Columns.Count; col++)
    //                {
    //                    string columnName = GetExcelColumnName(col);
    //                    worksheet.Cells[$"{columnName}{rowIndex}"].Value = row[col];
    //                }
    //                rowIndex++;
    //            }

    //            rowIndex += 2; // 空两行分隔不同块
    //        }

    //        // 自动调整列宽
    //        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

    //        // 保存Excel文件
    //        package.Save();
    //    }
    //    ed.WriteMessage($"\n文件已保存到: {fileInfo.FullName}");
    //}

    /// <summary>
    /// 【新增】将列索引转换为Excel列名（如0→A，26→AA）
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <returns></returns>
    //private string GetExcelColumnName(int columnIndex)
    //{
    //    int dividend = columnIndex;
    //    string columnName = string.Empty;
    //    while (dividend >= 0)
    //    {
    //        int modulo = dividend % 26;
    //        columnName = Convert.ToChar('A' + modulo) + columnName;
    //        dividend = dividend / 26 - 1;
    //    }
    //    return columnName;
    //}

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
