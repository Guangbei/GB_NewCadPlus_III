using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Windows;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Windows.Forms.Integration;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Line = Autodesk.AutoCAD.DatabaseServices.Line;
using Path = System.IO.Path;

/// <summary>
/// CAD远行时自动运行
/// </summary>
public class AutodeskRun : IExtensionApplication
{
    /// <summary>
    /// 启动加载的命令扩展
    /// </summary>
    public void Initialize()
    {
        AddMenus.AddMenu();
    }
    /// <summary>
    /// 卸载
    /// </summary>
    public void Terminate()
    {

    }
}
/// <summary>
/// 命令参数类
/// </summary>
public class PsetArgs
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PsetArgs() { }
    /// <summary>
    /// 字符串字典
    /// </summary>
    public Dictionary<string, string> dictS { get; set; } = new Dictionary<string, string>();
}
namespace GB_NewCadPlus_III
{
    public static class Point3dExtensions
    {
        /// <summary>
        /// 扩展方法：根据极坐标计算点的位置 ffff
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="angle">角度</param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Point3d PolarPoint(this Point3d center, double angle, double distance)
        {
            double radians = angle * Math.PI / 180; // 将角度转换为弧度
            double x = center.X + distance * Math.Cos(radians);
            double y = center.Y + distance * Math.Sin(radians);
            return new Point3d(x, y, center.Z);
        }
    }

    /// <summary>
    /// 委托：在要发送内容的类里，建立一个委托，再实例化这个委托。同时给实例化的委托sendSum传值（sendSum?.invoke(传递值)）；在接收类里建立一个赋值方法，这个方法是这个值给到接收文本框显示的值，再在接收页面初始化方法里把这个委托值给到赋值方法即可；
    /// </summary>
    /// <param name="text">传递的text</param>
    public delegate void sendText(string text);

    /// <summary>
    /// 主命令类
    /// </summary>
    public class Command
    {
        /// <summary>
        /// 静态变量，用于保存图库管理窗体
        /// </summary>
        private static PaletteSet? Wpf_Cad_PaletteSet;

        /// <summary>
        /// 显示主窗体
        /// </summary>
        [CommandMethod(nameof(ffff))]
        public static void ffff()
        {
            DateTime setDate = new DateTime(2026, 12, 30);
            if (DateTime.Now < setDate)
            {
                //FormMain.GB_CadToolsForm.ShowToolsPanel();

                if (Wpf_Cad_PaletteSet is null)
                {

                    Wpf_Cad_PaletteSet = new PaletteSet("GB_CADTools");  //初始化窗体容器；
                    Wpf_Cad_PaletteSet.MinimumSize = new System.Drawing.Size(350, 800);//初始化窗体容器最小的尺寸

                    var wpfWindows = new WpfMainWindow();//初始化这个图库管理窗体；
                    var host = new ElementHost()//初始化子面板
                    {
                        AutoSize = true,//设置子面板自动大小
                        Dock = DockStyle.Fill,//子面板整体覆盖
                        Child = wpfWindows//设置子面板的子项为wpfWindows
                    };
                    Wpf_Cad_PaletteSet.Add("abc", host);//添加子面板
                    Wpf_Cad_PaletteSet.Visible = true;//显示窗体容器
                    Wpf_Cad_PaletteSet.Dock = DockSides.Left;//窗体容器的停靠位置

                    return;
                }
                Wpf_Cad_PaletteSet.Visible = !Wpf_Cad_PaletteSet.Visible;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("试用时间过期！");
                return;
            };
        }

        /// <summary>
        /// 存储用户点击的点
        /// </summary>
        private static List<Point3d> pointS = new List<Point3d>();

       /// <summary>
        /// 块统计
        /// </summary>
        public void BlockCountStatistics()
        {
            using var tr = new DBTrans();
            var i = tr.CurrentSpace
                    .GetEntities<BlockReference>()
                    .Where(brf => brf.GetBlockName() == "自定义块")
                    .Count();
            Env.Print(i);
        }

        #region 选择外参并从中选图元复制到当前空间内

        #region 选中外部参照中的图元
        [CommandMethod("SELECTXREFENTITY")]
        public void SelectXrefEntity()
        {
            //try
            //{
            //    FormMain.ReferenceEntity.Items.Clear();
            //    FormMain.SelectEntity.Items.Clear();
            //    FormMain.Reference.Items.Clear();
            //    // 第一步：创建嵌套实体选择选项
            //    PromptNestedEntityOptions options = new PromptNestedEntityOptions("\n请点击外部参照中的图元: ");
            //    // 允许用户选择任意层级的嵌套实体（外部参照中的实体）
            //    options.AllowNone = false;
            //    // 第二步：获取用户选择的嵌套实体
            //    PromptNestedEntityResult result = Env.Editor.GetNestedEntity(options);
            //    if (result.Status != PromptStatus.OK) return;

            //    using (var tr = new DBTrans())
            //    {
            //        // 第三步：获取选中的嵌套图元ObjectId
            //        ObjectId nestedId = result.ObjectId;
            //        // 获取外部参照的变换矩阵（包含位置/旋转/缩放信息）
            //        Matrix3d transform = result.Transform;
            //        // 获取鼠标点击位置（WCS坐标）
            //        Point3d pickPoint = result.PickedPoint;
            //        // 第四步：打开嵌套图元
            //        Entity nestedEntity = .GetObject(nestedId, OpenMode.ForRead) as Entity;
            //        if (nestedEntity == null)
            //        {
            //            Env.Editor.WriteMessage("\n错误：选中的对象不是图元。");
            //            return;
            //        }

            //        using var tr = new DBTrans();

            //        // 获取外部参照中的图元
            //        xrefEntities = Command.GetXrefEntities(res.ObjectId);
            //        // 第五步：获取外部参照名称
            //        string xrefName = Command.getXrefName(tr, res.ObjectId);
            //        Reference.Items.Add(xrefName);
            //        // 添加图元到左侧列表
            //        foreach (ObjectId entityId in xrefEntities)
            //        {
            //            Entity entity = Command.GetEntity(entityId);
            //            if (entity is not null)
            //            {
            //                selectedEntities.Add(entityId);
            //                ReferenceEntity.Items.Add(Command.getXrefName(tr, entityId));
            //            }
            //        }

            //        // 第七步：提交事务
            //        tr.Commit();
            //        第八步：刷新界面
            //        Env.Editor.Redraw();

            //        Env.Editor.WriteMessage("\n成功复制图元到当前位置！");
            //    }
            //}
            //catch (Autodesk.AutoCAD.Runtime.Exception ex)
            //{
            //    Env.Editor.WriteMessage($"\n错误: {ex.Message}");
            //}
        }
        #endregion


        #region   通过拿到外参中的图元id后台打开外参中的图元再插入到当前文档中
        [CommandMethod("CopyXrefEntity")]
        public void CopyXrefEntity()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //Database db = doc.Database;
            Editor ed = doc.Editor;
            using (var tr = new DBTrans())
            {
                try
                {
                    // 第一步：创建嵌套实体选择选项
                    PromptNestedEntityOptions options = new PromptNestedEntityOptions("\n请点击外部参照中的图元: ");
                    // 允许用户选择任意层级的嵌套实体（外部参照中的实体）
                    options.AllowNone = false;
                    // 第二步：获取用户选择的嵌套实体
                    PromptNestedEntityResult per = Env.Editor.GetNestedEntity(options);
                    if (per.Status != PromptStatus.OK) return;
                    // 2. 获取选中的图元对象
                    Entity selectedEnt = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;

                    string classId = selectedEnt.ClassID.ToString();
                    if (classId == null)
                    {
                        Env.Editor.WriteMessage("\n未找到ClassID。");
                        return;
                    }
                    string bounds = selectedEnt.Bounds.ToString();
                    if (bounds == null)
                    {
                        Env.Editor.WriteMessage("\n未找到Bounds。");
                        return;
                    }
                    // 获取嵌套容器（父块参照链）
                    ObjectId[] containers = per.GetContainers();

                    if (containers.Length == 0)
                    {
                        Env.Editor.WriteMessage("\n未找到父块参照。");
                        return;
                    }
                    #region 父级块
                    // 一般我们取最后一个或倒数第二个
                    ObjectId parentBlockRefId = containers.Last(); // 最外层块
                    BlockReference parentBlockRef = tr.GetObject(parentBlockRefId, OpenMode.ForRead) as BlockReference;
                    if (parentBlockRef == null)
                    {
                        Env.Editor.WriteMessage("\n父块参照无效。");
                        return;
                    }
                    // 获取父块参照（文件）的名称
                    string parentBlockName = parentBlockRef.Name;
                    Env.Editor.WriteMessage($"\n父块参照名称: {parentBlockName}");
                    // 获取父级块表记录
                    BlockTableRecord btr = tr.GetObject(parentBlockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null)
                    {
                        Env.Editor.WriteMessage("\n块表记录无效。");
                        return;
                    }
                    // 判断是否为外部参照
                    if (btr.IsFromExternalReference)
                    {
                        // 获取外部参照信息
                        var xrefDb = btr.GetXrefDatabase(true);
                        if (xrefDb != null)
                        {
                            // 获取外部参照文件路径
                            Env.Editor.WriteMessage($"\n外部参照文件路径: {xrefDb.Filename}");
                        }
                        else
                        {
                            Env.Editor.WriteMessage("\n无法获取外部参照数据库。");
                        }
                    }
                    else
                    {
                        Env.Editor.WriteMessage("\n该块不是外部参照。");
                    }
                    #endregion
                    // 获取外部参照路径信息
                    string xrefPath = btr.PathName;
                    string fileName = System.IO.Path.GetFileName(xrefPath);
                    if (CopyEntityByClassIdFromDwg(xrefPath, classId, bounds))
                    {
                        Env.Editor.WriteMessage("\n已复制外部参照：", fileName);
                    }
                    else
                    {
                        // 获取块名称
                        var blockName = selectedEnt.BlockName;
                        // 去除 |前缀
                        blockName = blockName.Split('|').Last();
                        if (GB_XrefInsertBlock(xrefPath, blockName))
                        {
                            Env.Editor.WriteMessage("\n已复制外部参照中的（天正）图元：", fileName);
                        }
                        else if (GB_XrefInsertBlock(xrefPath, blockName, "1"))
                        {
                            Env.Editor.WriteMessage("\n已复制外部参照中的（块）图元：", fileName);
                        }
                        else
                        {
                            Env.Editor.WriteMessage("\n未复制外部参照中的（任何）图元：", fileName);
                        }

                    }
                    tr.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    ed.WriteMessage($"\n错误: {ex.Message}");
                }
            }
        }

        [CommandMethod("CopyXrefAllEntity")]
        public void CopyXrefAllEntity()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            //Database db = doc.Database;
            Editor ed = doc.Editor;
            using (var tr = new DBTrans())
            {
                try
                {
                    #region 拿到外参图元信息
                    // 第一步：创建嵌套实体选择选项
                    PromptNestedEntityOptions options = new PromptNestedEntityOptions("\n请点击外部参照中的图元: ");
                    // 允许用户选择任意层级的嵌套实体（外部参照中的实体）
                    options.AllowNone = false;
                    // 第二步：获取用户选择的嵌套实体
                    PromptNestedEntityResult per = Env.Editor.GetNestedEntity(options);
                    if (per.Status != PromptStatus.OK) return;
                    // 2. 获取选中的图元对象
                    Entity selectedEnt = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    #endregion
                    #region 获取外参路径与块名
                    // 获取嵌套容器（父块参照链）
                    ObjectId[] containers = per.GetContainers();
                    if (containers.Length == 0)
                    {
                        Env.Editor.WriteMessage("\n未找到父块参照。");
                        return;
                    }
                    // 一般我们取最后一个或倒数第二个
                    ObjectId parentBlockRefId = containers.Last(); // 最外层块
                    BlockReference parentBlockRef = tr.GetObject(parentBlockRefId, OpenMode.ForRead) as BlockReference;
                    if (parentBlockRef == null)
                    {
                        Env.Editor.WriteMessage("\n父块参照无效。");
                        return;
                    }
                    // 获取父块参照（文件）的名称
                    string parentBlockName = parentBlockRef.Name;
                    Env.Editor.WriteMessage($"\n父块参照名称: {parentBlockName}");
                    // 获取父级块表记录
                    BlockTableRecord btr = tr.GetObject(parentBlockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null)
                    {
                        Env.Editor.WriteMessage("\n块表记录无效。");
                        return;
                    }
                    // 判断是否为外部参照
                    if (btr.IsFromExternalReference)
                    {
                        // 获取外部参照信息
                        var xrefDb = btr.GetXrefDatabase(true);
                        if (xrefDb != null)
                        {
                            // 获取外部参照文件路径
                            Env.Editor.WriteMessage($"\n外部参照文件路径: {xrefDb.Filename}");
                        }
                        else
                        {
                            Env.Editor.WriteMessage("\n无法获取外部参照数据库。");
                        }
                    }
                    else
                    {
                        Env.Editor.WriteMessage("\n该块不是外部参照。");
                    }
                    #endregion
                    // 获取外部参照路径信息
                    string xrefPath = btr.PathName;
                    string fileName = System.IO.Path.GetFileName(xrefPath);
                    // 获取块名称
                    var blockName = selectedEnt.BlockName;
                    // 去除 |前缀
                    blockName = blockName.Split('|').Last();
                    if (GB_XrefInsertAllBlock(xrefPath, blockName))
                    {
                        Env.Editor.WriteMessage("\n已复制外部参照：", fileName);
                    }
                    else
                    {
                        Env.Editor.WriteMessage("\n复制外部参照失败：", fileName);
                    }
                    tr.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    ed.WriteMessage($"\n错误: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// 从指定DWG文件中复制符合ClassID和边界条件的实体到当前图纸
        /// </summary>
        /// <param name="sourceFilePath">源DWG文件路径</param>
        /// <param name="classId">目标实体的ClassID（用于筛选特定类型对象）</param>
        /// <param name="bounds">目标实体的边界范围（格式如"0,0,0;100,100,0"，用于筛选位置）</param>
        public static bool CopyEntityByClassIdFromDwg(string sourceFilePath, string classId, string bounds)
        {
            // 获取当前AutoCAD应用程序的当前文档和数据库
            var curDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            var curDb = curDoc.Database; // 当前图纸的数据库（用于写入复制后的实体）

            // 创建源DWG文件的数据库对象（不自动打开事务，不保留事务日志）
            using (var sourceDb = new Autodesk.AutoCAD.DatabaseServices.Database(false, true))
            {
                /* 步骤1：读取源DWG文件到临时数据库 */
                // 从指定路径读取DWG文件到源数据库（FileShare.ReadWrite允许其他进程读写，true表示加载外部参照）
                sourceDb.ReadDwgFile(sourceFilePath, System.IO.FileShare.ReadWrite, true, null);

                /* 步骤2：启动源数据库的事务，用于访问其数据 */
                using (var sourceTr = sourceDb.TransactionManager.StartTransaction())
                {
                    // 获取源数据库的块表（存储所有块定义，如模型空间、图纸空间）
                    var sourceBT = (BlockTable)sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    // 获取模型空间的块表记录（模型空间是默认的绘图区域）
                    var sourceBTR = (BlockTableRecord)sourceTr.GetObject(sourceBT[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    /* 步骤3：遍历模型空间的所有实体，查找符合条件的目标实体 */
                    Entity? foundEntity = null; // 存储找到的目标实体（可空类型）
                    foreach (ObjectId entityObjId in sourceBTR) // 遍历模型空间中的每个实体ID
                    {
                        // 从事务中获取实体对象（只读模式）
                        var sourceEntity = sourceTr.GetObject(entityObjId, OpenMode.ForRead) as Entity;
                        // 筛选条件：实体存在、ClassID匹配、边界范围匹配
                        if (sourceEntity != null
                            && sourceEntity.Bounds.ToString() == bounds)   // 边界范围匹配（筛选位置）
                        {
                            foundEntity = sourceEntity; // 找到符合条件的实体，保存到变量
                            break; // 找到后退出循环
                        }
                    }

                    // 检查是否找到目标实体
                    if (foundEntity == null)
                    {
                        // 未找到时弹出提示对话框
                        //Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("未找到图元。");
                        Env.Editor.WriteMessage("\n未找到天正图元。");
                        return false; // 结束方法
                    }

                    /* 步骤4：通过临时数据库中转，复制实体到当前图纸 */
                    // 创建临时数据库（用于中转克隆对象，避免直接操作源数据库或当前数据库）
                    using (var tempDb = new Autodesk.AutoCAD.DatabaseServices.Database(true, true))
                    {
                        // 准备要克隆的对象ID集合（仅包含找到的目标实体）
                        ObjectIdCollection ids = new ObjectIdCollection { foundEntity.ObjectId };
                        // ID映射表（记录源对象ID到目标对象ID的映射关系，用于处理重复ID）
                        IdMapping mapping = new IdMapping();

                        /* 子步骤4.1：将目标实体从源数据库克隆到临时数据库的当前空间 */
                        // WblockCloneObjects：将对象从源数据库克隆到目标数据库
                        // 参数说明：源对象ID集合、目标空间ID、ID映射表、重复记录处理方式（替换）、是否保留颜色/线型
                        sourceDb.WblockCloneObjects(ids, tempDb.CurrentSpaceId, mapping, DuplicateRecordCloning.Replace, false);

                        /* 子步骤4.2：将临时数据库中的实体克隆到当前图纸的当前空间 */
                        // 启动当前数据库的事务（用于写入克隆后的实体）
                        using (var curTrans = curDb.TransactionManager.StartTransaction())
                        {
                            // 获取当前图纸的模型空间块表记录（可写模式）
                            var curMs = (BlockTableRecord)curTrans.GetObject(
                                curDb.CurrentSpaceId, OpenMode.ForWrite);

                            // 准备临时数据库中需要克隆的实体ID集合（从临时数据库的当前空间获取）
                            ObjectIdCollection tmpIds = new ObjectIdCollection();
                            // 启动临时数据库的事务（用于读取克隆后的实体）
                            using (var tmpTrans = tempDb.TransactionManager.StartTransaction())
                            {
                                // 获取临时数据库的当前空间块表记录（只读模式）
                                var tmpMs = (BlockTableRecord)tmpTrans.GetObject(
                                    tempDb.CurrentSpaceId, OpenMode.ForRead);
                                // 遍历临时空间中的所有实体ID，添加到tmpIds集合
                                foreach (ObjectId id in tmpMs)
                                {
                                    tmpIds.Add(id);
                                }
                                tmpTrans.Commit(); // 提交临时数据库事务（释放资源）
                            }

                            // ID映射表（记录临时数据库对象ID到当前数据库对象ID的映射）
                            IdMapping curMapping = new IdMapping();
                            // 将临时数据库中的实体克隆到当前图纸的当前空间
                            // 参数说明：临时对象ID集合、当前空间ID、ID映射表、重复记录处理方式（替换）、是否保留颜色/线型
                            tempDb.WblockCloneObjects(tmpIds, curMs.ObjectId, curMapping, DuplicateRecordCloning.Replace, false);

                            curTrans.Commit(); // 提交当前数据库事务（保存克隆的实体）
                        }
                    }
                    sourceTr.Commit(); // 提交源数据库事务（释放资源）
                }
            }
            return true;
        }

        #endregion

        /// <summary>
        /// 复制外参1Line2Polyline
        /// </summary>
        [CommandMethod("CopyAndSync1")]
        public void CopyAndSync1()
        {
            try
            {

                //选择的外部参照
                PromptSelectionResult getselection = Env.Editor.GetSelection(new PromptSelectionOptions() { MessageForAdding = "请选择待处理图形:\n" });
                if (getselection.Status == PromptStatus.OK)
                {
                    using (var tr = new DBTrans())
                    {
                        //ojectid集合
                        List<ObjectId> needids = new List<ObjectId>();
                        //当前文件的块表
                        BlockTable bt = (BlockTable)tr.GetObject(Env.Database.BlockTableId, OpenMode.ForWrite);
                        //循环选择的外参中的每个元素的objectid
                        foreach (ObjectId oneid in getselection.Value.GetObjectIds())
                        {
                            //每个元素的dbobject
                            DBObject getdbo = tr.GetObject(oneid, OpenMode.ForWrite);
                            //如果这个元素是参照块
                            if (getdbo is BlockReference)
                            {
                                ObjectId newid = ObjectId.Null;
                                //判断是不是动态块
                                if ((getdbo as BlockReference).IsDynamicBlock)
                                    //newid赋值动态块表记录
                                    newid = (getdbo as BlockReference).DynamicBlockTableRecord;
                                else
                                {
                                    //newid赋值匿名块表记录
                                    newid = (getdbo as BlockReference).AnonymousBlockTableRecord;

                                }//newid是不是为空

                                if (newid.IsNull)
                                    //newid赋值为块表记录
                                    newid = (getdbo as BlockReference).BlockTableRecord;
                                else if (!newid.IsNull)
                                {
                                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(newid, OpenMode.ForWrite);
                                    //检查btr块表记录是不是外部参照
                                    if (btr.IsFromExternalReference)
                                        needids.Add(newid);
                                }
                            }
                        }
                        //把外参绑定进当前图里
                        Env.Database.BindXrefs(new ObjectIdCollection(needids.ToArray()), false);

                        foreach (ObjectId oneid in needids)
                        {
                            //块表记录btr
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(oneid, OpenMode.ForWrite);
                            //获取块表记录里的所有块引用
                            ObjectIdCollection findids = btr.GetBlockReferenceIds(true, false);
                            //获取块表记录里的所有匿名块引用
                            ObjectIdCollection findids1 = btr.GetAnonymousBlockIds();
                            foreach (ObjectId newid in findids)
                            {
                                //获取块引用
                                BlockReference newblk = (BlockReference)tr.GetObject(newid, OpenMode.ForWrite);
                                // 炸开这个块引用
                                newblk.ExplodeToOwnerSpace();

                            }
                            foreach (ObjectId newid in findids1)
                            {
                                BlockReference newblk = (BlockReference)tr.GetObject(newid, OpenMode.ForWrite);
                                newblk.ExplodeToOwnerSpace();
                            }
                            //btr.Erase();
                        }
                        tr.Commit();
                        Env.Editor.Redraw();
                    }
                }
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("处理完成!");
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("选择图元复制到当前空间内失败！");
                Env.Editor.WriteMessage($"\n错误: {ex.Message}");
            }
            try
            {
                var iFoxTr = new DBTrans();
                // 提示用户选择外部参照的图
                PromptEntityOptions entityOptions = new PromptEntityOptions("请选择外部参照的图");
                entityOptions.SetRejectMessage("请选择一个外部参照的图");
                entityOptions.AddAllowedClass(typeof(BlockReference), true);//设定选定的文件为外部参照块；
                PromptEntityResult entityResult = iFoxTr.Editor.GetEntity(entityOptions);//获取外部引用文件的实体；
                if (entityResult.Status != PromptStatus.OK) return;

                #region 处理文件新
                //Document thisdoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                List<ObjectId> needids = new List<ObjectId>();
                do
                {
                    #region
                    using (var tr = new DBTrans())
                    {
                        ////获取当前文档的块表
                        BlockTable bt = (BlockTable)tr.GetObject(Env.Database.BlockTableId, OpenMode.ForRead);

                        //循环块表
                        foreach (ObjectId oneid in bt)
                        {
                            //获取块表记录
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(oneid, OpenMode.ForRead);
                            //检查btr块表记录是不是外部参照
                            if (btr.IsFromExternalReference)
                                needids.Add(oneid);
                        }
                        tr.Commit();
                    }
                    ///去重
                    needids = needids.Distinct().ToList();
                    #endregion
                    #region
                    if (needids.Count > 0)
                    {
                        using (var tr = new DBTrans())
                        {
                            //获取当前文档的块表
                            BlockTable bt = (BlockTable)tr.GetObject(Env.Database.BlockTableId, OpenMode.ForRead);
                            //绑定外参
                            Env.Database.BindXrefs(new ObjectIdCollection(needids.ToArray()), false);
                            //循环块表
                            foreach (ObjectId oneid in needids)
                            {
                                ///获取块表记录
                                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(oneid, OpenMode.ForRead);
                                ///获取块表记录里的所有块引用
                                ObjectIdCollection findids = btr.GetBlockReferenceIds(true, false);
                                ///获取块表记录里的所有匿名块引用
                                ObjectIdCollection findids1 = btr.GetAnonymousBlockIds();
                                ///循环块引用
                                foreach (ObjectId newid in findids)
                                {
                                    ///获取块引用
                                    BlockReference newblk = (BlockReference)tr.GetObject(newid, OpenMode.ForWrite);
                                    ///炸开这个块引用
                                    newblk.ExplodeToOwnerSpace();
                                }
                                foreach (ObjectId newid in findids1)
                                {
                                    BlockReference newblk = (BlockReference)tr.GetObject(newid, OpenMode.ForWrite);
                                    newblk.ExplodeToOwnerSpace();
                                }
                                ///删除块表记录
                                btr.Erase();
                            }

                            tr.Commit();
                        }
                        //
                        needids.Clear();
                    }
                    #endregion

                    #region
                    using (Transaction tr = Env.Database.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(Env.Database.BlockTableId, OpenMode.ForWrite);
                        #region
                        foreach (ObjectId oneid in bt)
                        {
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(oneid, OpenMode.ForWrite);
                            if (btr.IsFromExternalReference)
                                needids.Add(oneid);
                        }
                        #endregion

                        tr.Commit();
                    }
                    #endregion

                } while (needids.Count > 0);

                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("处理完成!");
                #endregion
                iFoxTr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("选择图元复制到当前空间内失败！");
            }
        }

        /// <summary>
        /// 外参复制
        /// </summary>
        [CommandMethod("ReferenceCopy")]
        public void ReferenceCopy()
        {
            try
            {
                using var tr = new DBTrans();
                foreach (var objectId in FormMain.selectedEntities)
                {
                    //Entity entity = Command.GetEntity(objectId);
                    Entity entity = tr.GetObject(objectId, OpenMode.ForRead) as Entity;
                    if (entity.Handle.ToString() == FormMain.selectItem)
                        tr.CurrentSpace.AddEntity(entity);
                }
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage(ex.Message);
            }
        }

        /// <summary>
        /// 判断选择图元与当前空间中的图无是不相同，不同时复制到当前空间内
        /// </summary>
        [CommandMethod("CompareAndReplace")]
        public void CompareAndReplace()
        {
            try
            {
                // 选择复制进来的图元
                PromptSelectionResult selectionResult = Env.Editor.GetSelection();
                if (selectionResult.Status != PromptStatus.OK) return;

                using (var tr = new DBTrans())
                {
                    // 获取当前图中的所有图元
                    List<Entity> currentEntities = new List<Entity>();
                    foreach (ObjectId bTOBId in tr.BlockTable)
                    {
                        Entity? entity = tr.GetObject(bTOBId, OpenMode.ForRead) as Entity;
                        if (entity != null)
                            currentEntities.Add(entity);
                    }
                    // 获取选择集中的图元
                    SelectionSet selectionSet = selectionResult.Value;
                    foreach (SelectedObject selectedObject in selectionSet)
                    {
                        Entity? copiedEntity = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead) as Entity;
                        // 判断复制进来的图元是否与当前图中的任何一个图元相同
                        bool isSame = false;
                        if (copiedEntity != null)
                            foreach (Entity currentEntity in currentEntities)
                            {
                                if (IsSameEntity(copiedEntity, currentEntity))
                                {
                                    isSame = true;
                                    break;
                                }
                            }
                        if (!isSame)
                        {
                            if (copiedEntity != null)
                            {
                                // 复制进来的图元与当前图中的图元不同，将其添加到当前图中
                                Entity? copy = copiedEntity.Clone() as Entity;
                                if (copy != null)
                                    tr.CurrentSpace.AddEntity(copy);
                            }
                        }
                    }
                    // 提交事务
                    tr.Commit();
                    Env.Editor.Redraw();
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("选择图元复制到当前空间内失败！");
                Env.Editor.WriteMessage(ex.Message);
            }
        }

        /// <summary>
        /// 判断两个实体是否相同
        /// </summary>
        /// <param name="entity1">实体一</param>
        /// <param name="entity2">实体二</param>
        /// <returns></returns>
        private bool IsSameEntity(Entity entity1, Entity entity2)
        {

            // 比较图层
            if (entity1.Layer != entity2.Layer)
                return false;

            // 比较位置
            if (!entity1.GeometricExtents.MinPoint.IsEqualTo(entity2.GeometricExtents.MinPoint, Tolerance.Global))
                return false;

            // 比较大小
            if (!entity1.GeometricExtents.MaxPoint.IsEqualTo(entity2.GeometricExtents.MaxPoint, Tolerance.Global))
                return false;

            // 比较颜色
            if (entity1.Color != entity2.Color)
                return false;

            // 其他属性比较...

            return true;
        }

        #endregion 

        #region 插入图元
       

        /// <summary>
        /// 当前图纸空间的ObjectId
        /// </summary>
        private static List<ObjectId>? currentSpaceObjectId = new List<ObjectId>();


        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="resourcePath">源文件路径</param>
        /// <param name="blockName">块名</param>
        /// <param name="point">坐标点</param>
        public static bool GB_XrefInsertBlock(string resourcePath, string resourceBlockName)
        {
            #region 方法：进一步完善版本   功能基本完成
            try
            {
                string layerName = "0";
                int layerColorIndex = 255;
                Autodesk.AutoCAD.Colors.Color? layerColor = null;
                Action<Point3d, double>? entityScale;

                //创建源DWG文件的数据库对象（不自动打开事务，不保留事务日志）
                using (var sourceDb = new Autodesk.AutoCAD.DatabaseServices.Database(false, true))
                {
                    /* 步骤1：读取源DWG文件到临时数据库 */
                    // 从指定路径读取DWG文件到源数据库（FileShare.ReadWrite允许其他进程读写，true表示加载外部参照）
                    sourceDb.ReadDwgFile(resourcePath, System.IO.FileShare.ReadWrite, true, null);

                    /* 步骤2：启动源数据库的事务，用于访问其数据 */
                    using (var sourceTr = sourceDb.TransactionManager.StartTransaction())
                    {
                        // 获取源数据库的块表（存储所有块定义，如模型空间、图纸空间）
                        //原文件块表
                        BlockTable sourceBlockTable = sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        if (sourceBlockTable.Has(resourceBlockName))
                        {
                            //原文件块表记录
                            BlockTableRecord blockDef = sourceTr.GetObject(sourceBlockTable[resourceBlockName], OpenMode.ForRead) as BlockTableRecord;
                            var blockTableRecordName = blockDef.Name;

                            if (blockDef != null)
                            {
                                foreach (ObjectId id in blockDef)
                                {
                                    Entity entity = sourceTr.GetObject(id, OpenMode.ForRead) as Entity;
                                    if (entity != null)
                                    {
                                        layerName = entity.Layer;
                                        VariableDictionary.layerColorIndex = entity.ColorIndex;
                                        layerColor = entity.Color;
                                        entityScale = entity.Scale;
                                    }
                                }
                            }
                        }
                    }
                }
                var tr = new DBTrans();
                var resourBlockOBJid = tr.BlockTable.GetBlockFormA(resourcePath, resourceBlockName, true);
                if (!resourBlockOBJid.IsNull)
                {
                    //把块插入到当前空间
                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, resourBlockOBJid);
                    if (!referenceFileBlock.IsNull)
                    {
                        //尝试转换为实体
                        if (tr.GetObject(referenceFileBlock) is not Entity referenceFileEntity)
                            return false;
                        double tempAngle = 0;
                        var startPoint = new Point3d(0, 0, 0);
                        referenceFileEntity.Scale(startPoint, 100);
                        referenceFileEntity.Layer = layerName;
                        referenceFileEntity.ColorIndex = VariableDictionary.layerColorIndex;
                        if (layerColor != Color.FromColorIndex(ColorMethod.ByLayer, 0))
                            referenceFileEntity.Color = layerColor;
                        var entityBlock = new JigEx((mpw, _) =>
                        {
                            referenceFileEntity.Move(startPoint, mpw);
                            startPoint = mpw;
                            if (VariableDictionary.entityRotateAngle == tempAngle)
                            { return; }
                            else if (VariableDictionary.entityRotateAngle != tempAngle)
                            {
                                referenceFileEntity.Rotation(center: mpw, 0);
                                tempAngle = VariableDictionary.entityRotateAngle;
                                referenceFileEntity.Rotation(center: mpw, tempAngle);
                            }
                        });
                        entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(referenceFileEntity));
                        entityBlock.SetOptions(msg: "\n指定插入点");
                        var endPoint = Env.Editor.Drag(entityBlock);
                        if (endPoint.Status != PromptStatus.OK)
                            return false;
                        Env.Editor.Redraw();
                        Env.Editor.WriteMessage("\n块操作完成。");
                        tr.Commit();
                        return true;
                    }
                    else
                    {
                        Env.Editor.WriteMessage("\n未选择任何块。");
                        tr.Commit();
                        return false;
                    }
                }
                tr.Commit();
                return false;
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n插入图元失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
                return false;
            }
            #endregion
        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="resourcePath">源文件路径</param>
        /// <param name="blockName">块名</param>
        /// <param name="point">坐标点</param>
        public static bool GB_XrefInsertBlock(string resourcePath, string resourceBlockName, string number)
        {
            #region 方法：进一步完善版本   功能基本完成
            try
            {
                string layerName = "0";
                List<BlockReferenceInfo> blockNames = new List<BlockReferenceInfo>();
                //创建源DWG文件的数据库对象（不自动打开事务，不保留事务日志）
                using (var sourceDb = new Autodesk.AutoCAD.DatabaseServices.Database(false, true))
                {
                    /* 步骤1：读取源DWG文件到临时数据库 */
                    // 从指定路径读取DWG文件到源数据库（FileShare.ReadWrite允许其他进程读写，true表示加载外部参照）
                    sourceDb.ReadDwgFile(resourcePath, System.IO.FileShare.ReadWrite, true, null);

                    /* 步骤2：启动源数据库的事务，用于访问其数据 */
                    using (var sourceTr = sourceDb.TransactionManager.StartTransaction())
                    {
                        // 获取源数据库的块表（存储所有块定义，如模型空间、图纸空间）
                        BlockTable sourceBlockTable = sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        // 获取目标数据库中的块表
                        BlockTableRecord targetBlock = null;
                        if (sourceBlockTable.Has(resourceBlockName))
                        {
                            //原文件块表记录
                            BlockTableRecord blockBTR = sourceTr.GetObject(sourceBlockTable[resourceBlockName], OpenMode.ForRead) as BlockTableRecord;

                            if (blockBTR != null && blockBTR.Name.Equals(resourceBlockName, StringComparison.Ordinal))
                            {
                                foreach (ObjectId id in blockBTR)
                                {
                                    Entity entity = sourceTr.GetObject(id, OpenMode.ForRead) as Entity;
                                    if (entity != null && layerName == "0")
                                    {
                                        //拿到指定块的图层
                                        layerName = entity.Layer;
                                        BlockReference blockRef = sourceTr.GetObject(id, OpenMode.ForRead) as BlockReference;
                                        if (blockRef != null && blockRef.Layer == layerName)
                                        {
                                            BlockReferenceInfo blockReferenceInfo = new BlockReferenceInfo();
                                            blockReferenceInfo.Name = blockRef.Name;
                                            blockReferenceInfo.Layer = blockRef.Layer;
                                            blockReferenceInfo.ColorIndex = blockRef.ColorIndex;
                                            blockReferenceInfo.Color = blockRef.Color;
                                            blockReferenceInfo.Rotation = blockRef.Rotation;
                                            blockReferenceInfo.Position = blockRef.Position;
                                            blockReferenceInfo.Linetype = blockRef.Id;
                                            blockReferenceInfo.ScaleFactors = blockRef.ScaleFactors;
                                            blockReferenceInfo.LinetypeScale = blockRef.LinetypeScale;
                                            blockReferenceInfo.Normal = blockRef.Normal;
                                            blockNames.Add(blockReferenceInfo);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                var tr = new DBTrans();

                // var tr = new DBTrans();
                // 1. 先缓存所有需要的块定义 ObjectId
                var blockIdDict = new Dictionary<string, ObjectId>();
                foreach (var blockRefItem in blockNames)
                {
                    if (!blockIdDict.ContainsKey(blockRefItem.Name))
                    {
                        var objId = tr.BlockTable.GetBlockFormA(resourcePath, blockRefItem.Name, true);
                        if (!objId.IsNull)
                            blockIdDict[blockRefItem.Name] = objId;
                    }
                }

                // 2. 批量插入
                foreach (var blockRefItem in blockNames)
                {
                    if (!blockIdDict.TryGetValue(blockRefItem.Name, out var resourBlockOBJid))
                        continue;

                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, resourBlockOBJid);
                    if (referenceFileBlock.IsNull)
                        continue;

                    if (tr.GetObject(referenceFileBlock) is not BlockReference referenceFileEntity)
                        continue;

                    referenceFileEntity.Layer = blockRefItem.Layer;
                    referenceFileEntity.ColorIndex = blockRefItem.ColorIndex;
                    referenceFileEntity.Color = blockRefItem.Color;
                    referenceFileEntity.Rotation = blockRefItem.Rotation;
                    referenceFileEntity.Position = blockRefItem.Position;
                    referenceFileEntity.ScaleFactors = blockRefItem.ScaleFactors;
                    referenceFileEntity.LinetypeScale = blockRefItem.LinetypeScale;
                    referenceFileEntity.Normal = blockRefItem.Normal;
                }
                tr.Commit();
                Env.Editor.Redraw();
                Env.Editor.WriteMessage($"\n共插入{blockNames.Count}个块。");

                //“blockNames存的是要插入块的块名”这段代码是循环的把变量中blockNames，的块名称，插入到当前图纸空间中，可这个blockNames变量中，有很多相同的块名称，只不过是这个块的Position、Rotation、ScaleFactors等属性不同，有不有更好的方法，让这些块插入到当前图纸空间的速度更快，方法更简单
                return true;
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n插入图元失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
                return false;
            }
            #endregion
        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="resourcePath">源文件路径</param>
        /// <param name="blockName">块名</param>
        /// <param name="point">坐标点</param>
        public static bool GB_XrefInsertAllBlock(string resourcePath, string resourceBlockName)
        {
            #region 方法：进一步完善版本   功能基本完成
            try
            {
                string layerName = "0";
                List<BlockReferenceInfo> blockNames = new List<BlockReferenceInfo>();
                //创建源DWG文件的数据库对象（不自动打开事务，不保留事务日志）
                using (var sourceDb = new Autodesk.AutoCAD.DatabaseServices.Database(false, true))
                {
                    /* 步骤1：读取源DWG文件到临时数据库 */
                    // 从指定路径读取DWG文件到源数据库（FileShare.ReadWrite允许其他进程读写，true表示加载外部参照）
                    sourceDb.ReadDwgFile(resourcePath, System.IO.FileShare.ReadWrite, true, null);

                    /* 步骤2：启动源数据库的事务，用于访问其数据 */
                    using (var sourceTr = sourceDb.TransactionManager.StartTransaction())
                    {
                        // 获取源数据库的块表（存储所有块定义，如模型空间、图纸空间）
                        BlockTable sourceBlockTable = sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                        // 获取目标数据库中的块表
                        //BlockTableRecord targetBlock = null;
                        if (sourceBlockTable != null)
                            if (sourceBlockTable.Has(resourceBlockName))
                            {
                                //原文件块表记录
                                BlockTableRecord blockBTR = sourceTr.GetObject(sourceBlockTable[resourceBlockName], OpenMode.ForRead) as BlockTableRecord;

                                if (blockBTR != null && blockBTR.Name.Equals(resourceBlockName, StringComparison.Ordinal))
                                {
                                    foreach (ObjectId id in blockBTR)
                                    {
                                        Entity entity = sourceTr.GetObject(id, OpenMode.ForRead) as Entity;
                                        if (entity != null && layerName == "0")
                                        {
                                            //拿到指定块的图层
                                            layerName = entity.Layer;
                                            break;
                                        }
                                    }
                                }
                                foreach (ObjectId blockItemId in sourceBlockTable)
                                {
                                    var AllBlockBTR = sourceTr.GetObject(blockItemId, OpenMode.ForRead) as BlockTableRecord;
                                    if (AllBlockBTR != null)
                                    {
                                        foreach (ObjectId id in AllBlockBTR)
                                        {
                                            BlockReference blockRef = sourceTr.GetObject(id, OpenMode.ForRead) as BlockReference;
                                            if (blockRef != null && blockRef.Layer == layerName)
                                            {
                                                BlockReferenceInfo blockReferenceInfo = new BlockReferenceInfo();
                                                blockReferenceInfo.Name = blockRef.Name;
                                                blockReferenceInfo.Layer = blockRef.Layer;
                                                blockReferenceInfo.ColorIndex = blockRef.ColorIndex;
                                                blockReferenceInfo.Color = blockRef.Color;
                                                blockReferenceInfo.Rotation = blockRef.Rotation;
                                                blockReferenceInfo.Position = blockRef.Position;
                                                blockReferenceInfo.Linetype = blockRef.Id;
                                                blockReferenceInfo.ScaleFactors = blockRef.ScaleFactors;
                                                //blockReferenceInfo.Scale=new Scale3d(100,100,100);
                                                blockReferenceInfo.LinetypeScale = blockRef.LinetypeScale;
                                                blockReferenceInfo.Normal = blockRef.Normal;
                                                blockNames.Add(blockReferenceInfo);

                                            }
                                        }
                                    }
                                }
                            }
                    }
                }
                var tr = new DBTrans();
                #region 基本方法，外插块太多时，很慢
                //foreach (var blockRefItem in blockNames)
                //{
                //    //获取源文件块的objectid
                //    var resourBlockOBJid = tr.BlockTable.GetBlockFormA(resourcePath, blockRefItem.Name, true);
                //    if (!resourBlockOBJid.IsNull)
                //    {
                //        //把块插入到当前空间
                //        var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, resourBlockOBJid);
                //        if (!referenceFileBlock.IsNull)
                //        {
                //            //尝试转换为实体
                //            if (tr.GetObject(referenceFileBlock) is not BlockReference referenceFileEntity) return false;
                //            //块的图层名称
                //            referenceFileEntity.Layer = blockRefItem.Layer;
                //            //块的图层索引
                //            referenceFileEntity.ColorIndex = blockRefItem.ColorIndex;
                //            //块的色
                //            referenceFileEntity.Color = blockRefItem.Color;
                //            //块的旋转角度
                //            referenceFileEntity.Rotation = blockRefItem.Rotation;
                //            //块的坐标点
                //            referenceFileEntity.Position = blockRefItem.Position;
                //            //块的缩放因子
                //            referenceFileEntity.ScaleFactors = blockRefItem.ScaleFactors;
                //            //块的线型缩放因子
                //            referenceFileEntity.LinetypeScale = blockRefItem.LinetypeScale;
                //            //块的法向量
                //            referenceFileEntity.Normal = blockRefItem.Normal;


                //            Env.Editor.Redraw();
                //            Env.Editor.WriteMessage("\n块操作完成。");

                //        }
                //        else
                //        {
                //            Env.Editor.WriteMessage("\n未选择任何块。");
                //            tr.Commit();
                //            return false;
                //        }
                //    }
                //}
                //tr.Commit();
                #endregion


                // var tr = new DBTrans();
                // 1. 先缓存所有需要的块定义 ObjectId
                var blockIdDict = new Dictionary<string, ObjectId>();
                foreach (var blockRefItem in blockNames)
                {
                    if (!blockIdDict.ContainsKey(blockRefItem.Name))
                    {
                        var objId = tr.BlockTable.GetBlockFormA(resourcePath, blockRefItem.Name, true);
                        if (!objId.IsNull)
                            blockIdDict[blockRefItem.Name] = objId;
                    }
                }

                // 2. 批量插入
                foreach (var blockRefItem in blockNames)
                {
                    if (!blockIdDict.TryGetValue(blockRefItem.Name, out var resourBlockOBJid))
                        continue;

                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, resourBlockOBJid);
                    if (referenceFileBlock.IsNull)
                        continue;

                    if (tr.GetObject(referenceFileBlock) is not BlockReference referenceFileEntity)
                        continue;

                    referenceFileEntity.Layer = blockRefItem.Layer;
                    referenceFileEntity.ColorIndex = blockRefItem.ColorIndex;
                    referenceFileEntity.Color = blockRefItem.Color;
                    referenceFileEntity.Rotation = blockRefItem.Rotation;
                    referenceFileEntity.Position = blockRefItem.Position;
                    referenceFileEntity.ScaleFactors = blockRefItem.ScaleFactors;
                    referenceFileEntity.LinetypeScale = blockRefItem.LinetypeScale;
                    referenceFileEntity.Normal = blockRefItem.Normal;
                }
                tr.Commit();
                Env.Editor.Redraw();
                Env.Editor.WriteMessage($"\n共插入{blockNames.Count}个块。");

                //“blockNames存的是要插入块的块名”这段代码是循环的把变量中blockNames，的块名称，插入到当前图纸空间中，可这个blockNames变量中，有很多相同的块名称，只不过是这个块的Position、Rotation、ScaleFactors等属性不同，有不有更好的方法，让这些块插入到当前图纸空间的速度更快，方法更简单
                return true;
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n插入图元失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
                return false;
            }
            #endregion
        }

        /// <summary>
        /// 手动插入图元到0点坐标；
        /// </summary>
        public static void GB_InsertBlock_(Point3d startPoint, double tempAngle, ref ObjectId refObjectId)
        {
            #region 方法1：
            try
            {
                Directory.CreateDirectory(GetPath.referenceFile);  //获取到本工具的系统目录；
                if (VariableDictionary.btnFileName == null) return; //判断点现的按键名是不是空；
                if (VariableDictionary.resourcesFile == null) return; //判断点现的原文件是不是空；
                using var tr = new DBTrans();
                //拿到本工具的系统目录下的按键名的原文件的objectid；
                var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, VariableDictionary.btnFileName_blockName, true);
                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;//拿到原文件的块表记录；

                if (refFileRec != null)
                {
                    Env.Editor.WriteMessage($"{refFileRec.Name}");
                    //把块插入到当前空间
                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, referenceFileObId);
                    //尝试转换为实体
                    if (tr.GetObject(referenceFileBlock) is not Entity referenceFileEntity)
                        return;
                    //设置比例
                    referenceFileEntity.Scale(startPoint, VariableDictionary.blockScale);

                    if (VariableDictionary.entityRotateAngle != tempAngle)
                    {
                        referenceFileEntity.Rotation(startPoint, 0);
                        tempAngle = VariableDictionary.entityRotateAngle;
                        referenceFileEntity.Rotation(startPoint, tempAngle);
                    }
                    referenceFileEntity.Layer = VariableDictionary.btnBlockLayer;
                    //refObjectId = tr.CurrentSpace.AddEntity(referenceFileEntity);
                    refObjectId = referenceFileBlock;
                    //Env.Editor.Redraw();
                }

                tr.Commit();
                //Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage("错误信息: " + ex.Message);
            }
            #endregion

        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        [CommandMethod(nameof(GB_InsertBlock))]
        public static void GB_InsertBlock()
        {
            #region 方法：进一步完善版本   功能基本完成
            try
            {
                #region 拿到源文件
                // 确保引用文件目录存在  
                Directory.CreateDirectory(GetPath.referenceFile);
                if (VariableDictionary.btnFileName == null) return;

                if (!Directory.Exists(GetPath.referenceFile)) //如果不存在这个文件夹，我们就创建这个文件夹  
                    Directory.CreateDirectory(GetPath.referenceFile);
                GetPath.filePathAndName = System.IO.Path.Combine(GetPath.referenceFile, VariableDictionary.btnFileName + ".dwg");//获得引用文件全路径与文件名  
                if (!File.Exists(GetPath.filePathAndName))
                    File.WriteAllBytes(GetPath.filePathAndName, VariableDictionary.resourcesFile);
                using var tr = new DBTrans();
                if (currentSpaceObjectId.Count == 0 || !currentSpaceObjectId.Contains(tr.CurrentSpace.ObjectId))
                {
                    currentSpaceObjectId.Add(tr.CurrentSpace.ObjectId);
                }

                // 从应用程序Resources目录或引用文件目录获取DWG文件  
                string resourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", $"{VariableDictionary.btnFileName}.dwg");

                // 检查文件是否存在  
                if (!File.Exists(resourcePath))
                {
                    // 尝试在引用文件目录中查找  
                    resourcePath = Path.Combine(GetPath.referenceFile, $"{VariableDictionary.btnFileName}.dwg");

                    if (!File.Exists(resourcePath))
                    {
                        Env.Editor.WriteMessage($"\n无法找到资源文件: {VariableDictionary.btnFileName}.dwg");
                        return;
                    }
                }
                Env.Editor.WriteMessage($"\n使用资源文件: {resourcePath}");
                #endregion
                // 打开源数据库  
                var sourceDb = new Database(false, true);
                sourceDb.ReadDwgFile(resourcePath, FileOpenMode.OpenForReadAndAllShare, false, "");
                // 获取源数据库中的块表  
                using (Transaction sourceTr = sourceDb.TransactionManager.StartTransaction())
                {
                    //原文件块表
                    BlockTable sourceBlockTable = sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) as BlockTable;
                    // 获取目标数据库中的块表  
                    BlockTable destBlockTable = tr.GetObject(tr.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    // 存储源文件中的块集合  
                    Dictionary<string, ObjectId> sourceBlocks = new Dictionary<string, ObjectId>();
                    // 存储块定义的推荐图层信息  
                    Dictionary<string, string> blockLayerInfo = new Dictionary<string, string>();
                    // 收集源文件中的块 (排除"YQTICK"块)  
                    foreach (ObjectId blockId in sourceBlockTable)
                    {
                        //原文件块表记录
                        BlockTableRecord blockDef = sourceTr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;
                        if (!blockDef.IsLayout)
                        {
                            if (blockDef.Name.Equals($"{VariableDictionary.btnFileName_blockName}", StringComparison.OrdinalIgnoreCase))
                            {
                                //加入到块表集合
                                sourceBlocks.Add(blockDef.Name, blockId);
                            }
                            // 确定块的默认图层  
                            string blockLayer = "0"; // 默认为"0"图层  
                            // 尝试查找块内第一个实体的图层作为参考  
                            bool foundLayer = false;
                            foreach (ObjectId entId in blockDef)
                            {
                                Entity ent = sourceTr.GetObject(entId, OpenMode.ForRead) as Entity;
                                if (ent != null)
                                {
                                    blockLayer = ent.Layer;
                                    foundLayer = true;
                                    break; // 只取第一个实体的图层  
                                }
                            }
                            blockLayerInfo.Add(blockDef.Name, blockLayer);
                            Env.Editor.WriteMessage($"\n源文件中找到块: {blockDef.Name}，推荐图层: {blockLayer}" + (foundLayer ? " (来自块内实体)" : " (默认图层)"));
                        }
                    }
                    // 查找当前空间中的同名块引用并记录它们的信息  
                    Dictionary<string, List<BlockReferenceInfo>> existingBlocks = new Dictionary<string, List<BlockReferenceInfo>>();
                    // 存储每种块类型的最常用比例  
                    Dictionary<string, Scale3d> commonScales = new Dictionary<string, Scale3d>();
                    // 临时存储每种块类型所有的比例  
                    Dictionary<string, List<Scale3d>> allBlockScales = new Dictionary<string, List<Scale3d>>();
                    //判断是不是220插座
                    if (!VariableDictionary.btnFileName.Contains("单相插座"))
                        //循环当前空间内的ObjectId
                        foreach (ObjectId objId in tr.CurrentSpace)
                        {
                            Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;//循环的objectid是不是实体
                            if (entity is BlockReference blockRef)//这个实体是不是可以转换为块参照（块引用）的对象，如果是则转换
                            {
                                //拿到这个块表记录
                                BlockTableRecord btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                                string blockName = btr.Name;
                                if (sourceBlocks.ContainsKey(blockName))
                                {
                                    if (!existingBlocks.ContainsKey(blockName))
                                    {
                                        existingBlocks[blockName] = new List<BlockReferenceInfo>();
                                        allBlockScales[blockName] = new List<Scale3d>();
                                    }
                                    // 收集块引用的所有属性值（如果有）  
                                    Dictionary<string, object> attributeValues = new Dictionary<string, object>();
                                    if (blockRef.AttributeCollection != null && blockRef.AttributeCollection.Count > 0)
                                    {
                                        foreach (ObjectId attId in blockRef.AttributeCollection)
                                        {
                                            AttributeReference att = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                                            attributeValues[att.Tag] = att.TextString;
                                        }
                                    }
                                    // 记录这个块引用的比例  
                                    Scale3d blockScale = blockRef.ScaleFactors;
                                    allBlockScales[blockName].Add(blockScale);
                                    existingBlocks[blockName].Add(new BlockReferenceInfo
                                    {
                                        Id = objId,
                                        Position = blockRef.Position,
                                        Scale = blockScale,
                                        Rotation = blockRef.Rotation,
                                        Layer = blockRef.Layer,
                                        LinetypeScale = blockRef.LinetypeScale,
                                        Lineweight = blockRef.LineWeight,
                                        Color = blockRef.Color,
                                        AttributeValues = attributeValues,
                                        Visibility = blockRef.Visible
                                    });
                                    Env.Editor.WriteMessage($"\n找到需要替换的块引用: {blockName}，比例: X={blockScale.X:F2}, Y={blockScale.Y:F2}, Z={blockScale.Z:F2}");
                                }
                            }
                        }
                    // 从源数据库导入所有块定义  
                    destBlockTable.UpgradeOpen();
                    IdMapping mapping = new IdMapping();
                    ObjectIdCollection blockIds = [.. sourceBlocks.Values];
                    // 一次性导入所有块定义  
                    sourceDb.WblockCloneObjects(blockIds, tr.Database.BlockTableId, mapping, DuplicateRecordCloning.Replace, false);
                    // 处理每个块：替换现有块并插入新块  
                    foreach (var kvp in sourceBlocks)
                    {
                        string blockName = kvp.Key;
                        ObjectId sourceBlockId = kvp.Value;
                        if (mapping.Contains(sourceBlockId))
                        {
                            ObjectId newBlockId = mapping[sourceBlockId].Value;
                            // 获取块的推荐图层  
                            string blockLayer = blockLayerInfo[blockName];
                            // 获取推荐的比例（如果有）  
                            Scale3d recommendedScale = new Scale3d(100, 100, 100); // 默认比例为1:100  
                            if (VariableDictionary.btnFileName.Contains("两点互锁") || VariableDictionary.btnFileName.Contains("三点互锁"))
                            {
                                recommendedScale = new Scale3d(0.8, 0.8, 0.8); // 默认比例为1:1
                            }
                            if (commonScales.ContainsKey(blockName))
                            {
                                recommendedScale = commonScales[blockName];
                            }
                            if (VariableDictionary.btnFileName.Contains("PTJ"))
                            {
                                // 获取推荐的比例（如果有）  
                                recommendedScale = new Scale3d(1, 1, 1); // 默认比例为1:1
                                if (commonScales.ContainsKey(blockName))
                                {
                                    recommendedScale = commonScales[blockName];
                                }
                            }
                            else if (VariableDictionary.btnBlockLayer.Contains("EQUIP-通讯") || VariableDictionary.btnBlockLayer.Contains("EQUIP-安防"))
                            {
                                // 获取推荐的比例（如果有）  
                                recommendedScale = new Scale3d(500, 500, 500); // 默认比例为1:100  
                                if (commonScales.ContainsKey(blockName))
                                {
                                    recommendedScale = commonScales[blockName];
                                }
                            }
                            // 确保块定义的图层确实存在  
                            LayerTable layerTable = tr.GetObject(tr.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                            if (!layerTable.Has(blockLayer))
                            {
                                // 如果图层不存在，就创建该图层  
                                layerTable.UpgradeOpen();
                                LayerTableRecord newLayer = new LayerTableRecord();
                                newLayer.Name = blockLayer;
                                ObjectId layerId = layerTable.Add(newLayer);
                                Env.Editor.WriteMessage($"\n创建新图层: {blockLayer}");
                            }
                            // 1. 如果有同名块引用，替换它们  
                            if (!VariableDictionary.btnFileName.Contains("单相插座") && existingBlocks.ContainsKey(blockName) && existingBlocks[blockName].Count > 0)
                            {
                                foreach (var blockInfo in existingBlocks[blockName])
                                {
                                    // 删除原有块引用  
                                    Entity oldEntity = tr.GetObject(blockInfo.Id, OpenMode.ForWrite) as Entity;
                                    oldEntity.Erase();

                                    // 创建新块引用，保持原位置和属性  
                                    BlockReference newBlockRef = new BlockReference(blockInfo.Position, newBlockId);

                                    // 保留原有块的所有属性  
                                    newBlockRef.ScaleFactors = blockInfo.Scale; // 使用原有块的比例  
                                    newBlockRef.Rotation = blockInfo.Rotation;

                                    // 使用块的推荐图层  
                                    newBlockRef.Layer = blockLayer;

                                    // 其他属性沿用原有块的设置  
                                    newBlockRef.LinetypeScale = blockInfo.LinetypeScale;
                                    newBlockRef.LineWeight = blockInfo.Lineweight;
                                    newBlockRef.Color = blockInfo.Color;
                                    newBlockRef.Visible = blockInfo.Visibility;

                                    // 添加到当前空间  
                                    ObjectId newRefId = tr.CurrentSpace.AddEntity(newBlockRef);
                                    Env.Editor.Redraw();
                                    // 处理属性  
                                    BlockTableRecord btr = tr.GetObject(newBlockId, OpenMode.ForRead) as BlockTableRecord;
                                    if (btr.HasAttributeDefinitions)
                                    {
                                        // 添加属性  
                                        foreach (ObjectId id in btr)
                                        {
                                            DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                                            if (obj is AttributeDefinition attDef && !attDef.Constant)
                                            {
                                                AttributeReference attRef = new AttributeReference();
                                                attRef.SetAttributeFromBlock(attDef, newBlockRef.BlockTransform);

                                                // 应用原有的属性值（如果存在）  
                                                if (blockInfo.AttributeValues.ContainsKey(attDef.Tag))
                                                {
                                                    attRef.TextString = blockInfo.AttributeValues[attDef.Tag].ToString();
                                                }
                                                else
                                                {
                                                    attRef.TextString = attDef.TextString;
                                                }
                                                newBlockRef.AttributeCollection.AppendAttribute(attRef);
                                            }
                                        }
                                    }
                                    Env.Editor.WriteMessage($"\n已替换块引用: {blockName}，使用图层: {blockLayer}，比例: X={blockInfo.Scale.X:F2}, Y={blockInfo.Scale.Y:F2}, Z={blockInfo.Scale.Z:F2}");

                                }
                            }
                            Env.Editor.Redraw();
                            // 收集块的属性定义  
                            List<AttributeDefinition> attDefs = new List<AttributeDefinition>();
                            BlockTableRecord btr1 = tr.GetObject(newBlockId, OpenMode.ForRead) as BlockTableRecord;
                            if (btr1 != null && btr1.HasAttributeDefinitions)
                            {
                                foreach (ObjectId id in btr1)
                                {
                                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                                    if (obj is AttributeDefinition attDef && !attDef.Constant)
                                    {
                                        attDefs.Add(attDef);
                                    }
                                }
                            }
                            // 使用自定义Jig类进行拖拽，应用推荐比例  
                            EnhancedBlockPlacementJig jig = new EnhancedBlockPlacementJig(newBlockId, attDefs, blockLayer, recommendedScale);
                            PromptResult result = Env.Editor.Drag(jig);
                            if (result.Status == PromptStatus.OK)
                            {
                                // 用户确认后，创建真正的块引用  
                                BlockReference finalBlockRef = new BlockReference(jig.Position, newBlockId);
                                finalBlockRef.Rotation = jig.Rotation;
                                // 设置块引用的图层与推荐图层一致  
                                finalBlockRef.Layer = blockLayer;
                                // 应用推荐的比例  
                                finalBlockRef.ScaleFactors = jig.Scale;
                                ObjectId blockRefId = tr.CurrentSpace.AddEntity(finalBlockRef);
                                Env.Editor.Redraw();
                                // 添加属性  
                                if (btr1.HasAttributeDefinitions)
                                {
                                    foreach (AttributeDefinition attDef in attDefs)
                                    {
                                        AttributeReference attRef = new AttributeReference();
                                        attRef.SetAttributeFromBlock(attDef, finalBlockRef.BlockTransform);
                                        attRef.TextString = attDef.TextString;
                                        finalBlockRef.AttributeCollection.AppendAttribute(attRef);
                                    }
                                }
                                Env.Editor.WriteMessage($"\n已插入新块引用: {blockName}，使用图层: {blockLayer}，比例: X={jig.Scale.X:F2}, Y={jig.Scale.Y:F2}, Z={jig.Scale.Z:F2}");
                            }
                        }
                    }
                    Env.Editor.Redraw();
                    sourceTr.Commit();
                }

                tr.Commit();
                sourceDb.Dispose();
                Env.Editor.WriteMessage("\n块操作完成。");
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n插入图元失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
            #endregion
        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        [CommandMethod(nameof(GB_InsertBlock_1))]
        public static void GB_InsertBlock_1()
        {
            #region 方法1：
            try
            {
                Directory.CreateDirectory(GetPath.referenceFile);  //获取到本工具的系统目录；
                if (VariableDictionary.btnFileName == null) return; //判断按键名是不是空；
                if (VariableDictionary.resourcesFile == null) return; //判断原文件是不是空；
                using var tr = new DBTrans();
                var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, VariableDictionary.btnFileName_blockName, true);//拿到本工具的系统目录下的按键名的原文件的objectid；
                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;//拿到原文件的块表记录；

                if (refFileRec == null) return;
                var fileEntity = tr.GetObject(referenceFileObId, OpenMode.ForRead) as Entity;

                if (fileEntity == null) return;
                var fileEntityCopy = fileEntity.Clone() as Entity;
                fileEntityCopy.Scale(new Point3d(0, 0, 0), VariableDictionary.blockScale);
                //fileEntityCopy.Scale(fileEntityCopy, VariableDictionary.blockScale);
                //Name = "$TwtSys$00000571", ObjectId = {ObjectClass = {DxfName = "BLOCK_RECORD", Name = "AcDbBlockTableRecord", MyParent = {Autodesk.AutoCAD.Runtime.RXClass}, AppName = "ObjectDBX Classes"}}, PathName = "", IncludingErased = {Autodesk.AutoCAD.DatabaseServices.BlockTableRecord}
                //tr.CurrentSpace.DeepCloneEx(fileEntity)//深度克隆，可以复制天正图元；
                //(vlax-dump-object (vlax-ename->vla-object (car (entsel )))T)//这个是在cad命令里能读出天正属性的lisp命令；
                var dxfName = referenceFileObId.ObjectClass.DxfName;//抓取图元的DXFName,判断是不是天正的图元
                var fileType = dxfName.Split('_');//截取‘_’字符
                if (fileType[0] == "TCH")
                {
                    Env.Editor.WriteMessage("PTJ-TCH！");
                    var fileEntityCopyObId = tr.CurrentSpace.AddEntity(fileEntityCopy);
                    Env.Editor.Redraw();
                    double tempAngle = 0;
                    var startPoint = new Point3d(0, 0, 0);
                    var entityBlock = new JigEx((mpw, _) =>
                    {
                        fileEntityCopy.Move(startPoint, mpw);
                        startPoint = mpw;
                        if (VariableDictionary.entityRotateAngle == tempAngle)
                        {
                            return;
                        }
                        else if (VariableDictionary.entityRotateAngle != tempAngle)
                        {
                            fileEntityCopy.Rotation(center: mpw, 0);
                            tempAngle = VariableDictionary.entityRotateAngle;
                            fileEntityCopy.Rotation(center: mpw, tempAngle);
                        }
                    });
                    entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(fileEntityCopy));
                    entityBlock.SetOptions(msg: "\n指定插入点");
                    //entityBlock.SetOptions(startPoint, msg: "\n指定插入点");这个startpoint，是有个参考线在里面，用于托拽时的辅助；
                    var endPoint = Env.Editor.Drag(entityBlock);
                    if (endPoint.Status != PromptStatus.OK) return;
                    tr.BlockTable.Remove(referenceFileObId);
                }
                else if (fileEntityCopy is BlockReference)
                {
                    Env.Editor.WriteMessage("PTJ-块表记录！");
                    //if (fileEntityCopy.ColorIndex.ToString() != "130") return;
                    var fileEntityCopyObId = tr.CurrentSpace.AddEntity(fileEntityCopy);
                    double tempAngle = 0;
                    var startPoint = new Point3d(0, 0, 0);
                    var entityBlock = new JigEx((mpw, _) =>
                    {
                        fileEntityCopy.Move(startPoint, mpw);
                        startPoint = mpw;
                        if (VariableDictionary.entityRotateAngle == tempAngle)
                        {
                            return;
                        }
                        else if (VariableDictionary.entityRotateAngle != tempAngle)
                        {
                            fileEntityCopy.Rotation(center: mpw, 0);
                            tempAngle = VariableDictionary.entityRotateAngle;
                            fileEntityCopy.Rotation(center: mpw, tempAngle);
                        }
                    });
                    entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(fileEntityCopy));
                    entityBlock.SetOptions(msg: "\n指定插入点");
                    //entityBlock.SetOptions(startPoint, msg: "\n指定插入点");这个startpoint，是有个参考线在里面，用于托拽时的辅助；
                    var endPoint = Env.Editor.Drag(entityBlock);
                    if (endPoint.Status != PromptStatus.OK) return;
                    tr.BlockTable.Remove(referenceFileObId);
                    tr.Commit();
                    Env.Editor.Redraw();
                }
                else
                {
                    Env.Editor.WriteMessage("PTJ-块！");

                }
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage(ex.ToString());
            }
            #endregion

        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        [CommandMethod(nameof(GB_InsertBlock_2))]
        public static void GB_InsertBlock_2()
        {
            #region 方法1：
            try
            {
                Directory.CreateDirectory(GetPath.referenceFile);  //获取到本工具的系统目录；
                if (VariableDictionary.btnFileName == null) return; //判断点现的按键名是不是空；
                if (VariableDictionary.resourcesFile == null) return; //判断点现的原文件是不是空；
                using var tr = new DBTrans();
                //var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, true);//拿到本工具的系统目录下的按键名的原文件的objectid；
                //拿到本工具的系统目录下的按键名的原文件的objectid；
                var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, VariableDictionary.btnFileName_blockName, true);
                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;//拿到原文件的块表记录；
                var dimPoint = new Point3d(0, 0, 0);

                if (refFileRec != null)
                {
                    Env.Editor.WriteMessage("块！");
                    //把块插入到当前空间
                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, referenceFileObId);
                    //尝试转换为实体
                    if (tr.GetObject(referenceFileBlock) is not Entity referenceFileEntity)
                        return;
                    //设置比例
                    referenceFileEntity.Scale(new Point3d(0, 0, 0), VariableDictionary.blockScale);
                    double tempAngle = 0;
                    var startPoint = new Point3d(0, 0, 0);
                    var entityBlock = new JigEx((mpw, _) =>
                    {
                        referenceFileEntity.Move(startPoint, mpw);
                        startPoint = mpw;
                        if (VariableDictionary.entityRotateAngle == tempAngle)
                        { return; }
                        else if (VariableDictionary.entityRotateAngle != tempAngle)
                        {
                            referenceFileEntity.Rotation(center: mpw, 0);
                            tempAngle = VariableDictionary.entityRotateAngle;
                            referenceFileEntity.Rotation(center: mpw, tempAngle);
                        }
                    });
                    entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(referenceFileEntity));
                    entityBlock.SetOptions(msg: "\n指定插入点");
                    var endPoint = Env.Editor.Drag(entityBlock);
                    if (endPoint.Status != PromptStatus.OK)
                        return;
                    referenceFileEntity.Layer = VariableDictionary.btnBlockLayer;
                    dimPoint = entityBlock.MousePointWcsLast;
                    Env.Editor.Redraw();
                }

                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage("错误信息: " + ex.Message);
            }
            #endregion

        }

        /// <summary>
        /// 用于保存块引用信息的辅助类  
        /// </summary>
        private class BlockReferenceInfo
        {
            /// <summary>
            /// 名称
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 项目id
            /// </summary>
            public ObjectId Id { get; set; }
            /// <summary>
            /// 3D坐标点
            /// </summary>
            public Point3d Position { get; set; }
            /// <summary>
            /// 比例因子
            /// </summary>
            public Scale3d ScaleFactors { get; set; }
            /// <summary>
            /// 角度
            /// </summary>
            public double Rotation { get; set; }
            /// <summary>
            /// 比例
            /// </summary>
            public Scale3d Scale { get; set; }
            /// <summary>
            /// 图层名
            /// </summary>
            public string Layer { get; set; }
            /// <summary>
            /// 线类型 objectid
            /// </summary>
            public ObjectId Linetype { get; set; }
            /// <summary>
            /// 线型比例
            /// </summary>
            public double LinetypeScale { get; set; }
            /// <summary>
            /// 线宽
            /// </summary>
            public Autodesk.AutoCAD.DatabaseServices.LineWeight Lineweight { get; set; }
            /// <summary>
            /// 线色号
            /// </summary>
            public Autodesk.AutoCAD.Colors.Color Color { get; set; }
            /// <summary>
            /// 线色号
            /// </summary>
            public int ColorIndex { get; set; }
            /// <summary>
            /// 属性
            /// </summary>
            public Dictionary<string, object> AttributeValues { get; set; } = new Dictionary<string, object>();
            /// <summary>
            /// 透明度
            /// </summary>
            public bool Visibility { get; set; }
            /// <summary>
            /// 块法向量
            /// </summary>
            public Vector3d Normal { get; set; }
        }

        /// <summary>  
        /// 增强的块放置拖拽类，完整支持组合块预览  
        /// </summary>  
        private class EnhancedBlockPlacementJig : EntityJig
        {
            private Point3d _position;
            private double _rotation;
            private Scale3d _scale;
            private ObjectId _blockId;
            private List<AttributeDefinition> _attDefs;
            private string _blockLayer;

            // 属性  
            public Point3d Position => _position;
            public double Rotation => _rotation;
            public Scale3d Scale => _scale;

            public EnhancedBlockPlacementJig(ObjectId blockId, List<AttributeDefinition> attDefs, string? blockLayer = null, Scale3d? scale = null)
                : base(new BlockReference(Point3d.Origin, blockId))
            {
                _blockId = blockId;
                _position = Point3d.Origin;
                _rotation = VariableDictionary.entityRotateAngle; // 使用预设旋转角度  
                _attDefs = attDefs;
                _blockLayer = blockLayer ?? "0";
                _scale = scale ?? new Scale3d(1, 1, 1);

                // 配置预览实体  
                BlockReference blockRef = (BlockReference)Entity;
                blockRef.Layer = _blockLayer;
                blockRef.ScaleFactors = _scale;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                // 获取插入点  
                JigPromptPointOptions pointOpts = new JigPromptPointOptions("\n指定组合块插入点（右键确认）:");
                pointOpts.UserInputControls = UserInputControls.Accept3dCoordinates;
                pointOpts.UseBasePoint = false;

                PromptPointResult pointResult = prompts.AcquirePoint(pointOpts);

                // 如果用户取消  
                if (pointResult.Status != PromptStatus.OK)
                    return SamplerStatus.Cancel;

                // 如果位置没变，返回NoChange  
                if (_position.DistanceTo(pointResult.Value) < 0.001)
                    return SamplerStatus.NoChange;

                _position = pointResult.Value;

                // 始终使用VariableDictionary.entityRotateAngle作为旋转角度  
                _rotation = VariableDictionary.entityRotateAngle;

                return SamplerStatus.OK;
            }

            protected override bool Update()
            {
                try
                {
                    // 更新块引用的位置、旋转和比例  
                    BlockReference blockRef = (BlockReference)Entity;
                    blockRef.Position = _position;
                    blockRef.Rotation = _rotation;
                    blockRef.ScaleFactors = _scale;

                    return true;
                }
                catch (Exception ex)
                {
                    // 记录错误但不抛出异常  
                    Env.Editor.WriteMessage($"\n块更新时发生错误: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 插入外部图元
        /// </summary>
        [CommandMethod(nameof(GB_InsertBlock_3))]
        public static void GB_InsertBlock_3()
        {
            #region 方法1：
            try
            {
                Directory.CreateDirectory(GetPath.referenceFile);  //获取到本工具的系统目录；
                if (VariableDictionary.btnFileName == null) return; //判断点现的按键名是不是空；
                if (VariableDictionary.resourcesFile == null) return; //判断点现的原文件是不是空；
                using var tr = new DBTrans();
                var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, VariableDictionary.btnFileName_blockName, true);//拿到本工具的系统目录下的按键名的原文件的objectid；
                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;//拿到原文件的块表记录；
                var dimPoint = new Point3d(0, 0, 0);

                if (refFileRec != null)
                {
                    Env.Editor.WriteMessage("块！");
                    //把块插入到当前空间
                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, referenceFileObId);
                    //尝试转换为实体
                    if (tr.GetObject(referenceFileBlock) is not Entity referenceFileEntity)
                        return;
                    //设置比例
                    //referenceFileEntity.Scale(new Point3d(0, 0, 0), VariableDictionary.blockScale);
                    double tempAngle = 0;
                    var startPoint = new Point3d(0, 0, 0);
                    var entityBlock = new JigEx((mpw, _) =>
                    {
                        referenceFileEntity.Move(startPoint, mpw);
                        startPoint = mpw;
                        if (VariableDictionary.entityRotateAngle == tempAngle)
                        { return; }
                        else if (VariableDictionary.entityRotateAngle != tempAngle)
                        {
                            referenceFileEntity.Rotation(center: mpw, 0);
                            tempAngle = VariableDictionary.entityRotateAngle;
                            referenceFileEntity.Rotation(center: mpw, tempAngle);
                        }
                    });
                    entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(referenceFileEntity));
                    entityBlock.SetOptions(msg: "\n指定插入点");
                    var endPoint = Env.Editor.Drag(entityBlock);
                    if (endPoint.Status != PromptStatus.OK)
                        return;
                    referenceFileEntity.Layer = VariableDictionary.btnBlockLayer;
                    dimPoint = entityBlock.MousePointWcsLast;
                    Env.Editor.Redraw();
                }
                if (VariableDictionary.btnBlockLayer != null && VariableDictionary.dimString != null)
                    DDimLinear(VariableDictionary.dimString, Convert.ToInt16(VariableDictionary.layerColorIndex), VariableDictionary.btnBlockLayer, dimPoint);

                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage($"\n插入图元失败: {ex.Message}");
            }
            #endregion
        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        [CommandMethod(nameof(GB_InsertBlock_4))]
        public static void GB_InsertBlock_4()
        {
            try
            {
                Directory.CreateDirectory(GetPath.referenceFile);  //获取到本工具的系统目录；
                if (VariableDictionary.btnFileName == null) return; //判断点现的按键名是不是空；
                if (VariableDictionary.resourcesFile == null) return; //判断点现的原文件是不是空；
                using var tr = new DBTrans();
                var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, true);
                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;
                if (refFileRec == null) return;
                // 创建列表存储复制的图元  
                List<Entity> copiedEntities = new List<Entity>();
                // 第一步：复制所有符合条件的图元到当前空间  
                foreach (var fileId in refFileRec)
                {
                    if (fileId.ObjectClass.DxfName == null) continue;
                    var fileEntity = tr.GetObject(fileId, OpenMode.ForRead) as Entity;
                    if (fileEntity == null) continue;
                    var dxfName = fileId.ObjectClass.DxfName;
                    var fileType = dxfName.Split('_');
                    var fileEntityCopy = fileEntity.Clone() as Entity;
                    if (fileEntityCopy != null)
                    {
                        tr.CurrentSpace.AddEntity(fileEntityCopy);
                        copiedEntities.Add(fileEntityCopy);
                    }
                }
                if (copiedEntities.Count == 0)
                {
                    Env.Editor.WriteMessage("\n未找到可复制的天正图元！");
                    return;
                }
                // 计算所有复制图元的包围盒，用于确定基准点  
                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double minZ = double.MaxValue;
                foreach (var entity in copiedEntities)
                {
                    Extents3d ext = entity.GeometricExtents;
                    minX = Math.Min(minX, ext.MinPoint.X);
                    minY = Math.Min(minY, ext.MinPoint.Y);
                    minZ = Math.Min(minZ, ext.MinPoint.Z);
                }
                Point3d basePoint = new Point3d(minX, minY, minZ);
                var startPoint = basePoint;
                double tempAngle = 0;

                var entityBlock = new JigEx((mpw, _) =>
                {
                    // 计算移动向量  
                    Vector3d moveVector = mpw - startPoint;

                    // 移动所有图元  
                    foreach (var entity in copiedEntities)
                    {
                        entity.TransformBy(Matrix3d.Displacement(moveVector));
                    }
                    startPoint = mpw;
                    // 处理旋转  
                    if (VariableDictionary.entityRotateAngle != tempAngle)
                    {
                        // 创建旋转矩阵  
                        Matrix3d rotationMatrix = Matrix3d.Rotation(
                            VariableDictionary.entityRotateAngle - tempAngle,  // 旋转角度差值  
                            Vector3d.ZAxis,                          // 绕Z轴旋转  
                            mpw                                      // 旋转中心点  
                        );

                        foreach (var entity in copiedEntities)
                        {
                            entity.TransformBy(rotationMatrix);
                        }

                        tempAngle = VariableDictionary.entityRotateAngle;
                    }
                });
                // 设置拖拽时的图元显示  
                entityBlock.DatabaseEntityDraw(wd =>
                {
                    foreach (var entity in copiedEntities)
                    {
                        wd.Geometry.Draw(entity);
                    }
                });
                // 设置拖拽提示  
                entityBlock.SetOptions(msg: "\n指定插入点位置");
                // 执行拖拽操作  
                var endPoint = Env.Editor.Drag(entityBlock);
                if (endPoint.Status != PromptStatus.OK)
                {
                    // 如果用户取消，删除已复制的图元  
                    foreach (var entity in copiedEntities)
                    {
                        entity.Erase();
                    }
                    return;
                }
                // 成功后删除原引用块  
                tr.BlockTable.Remove(referenceFileObId);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage("错误信息: " + ex.Message);
            }
        }

        /// <summary>
        /// 一个块反复插入图中
        /// </summary>
        [CommandMethod(nameof(GB_InsertBlock_5))]
        public static void GB_InsertBlock_5()
        {
            #region 方法1：  
            try
            {
                pointS.Clear();
                Directory.CreateDirectory(GetPath.referenceFile);
                if (VariableDictionary.btnFileName == null) return;

                if (VariableDictionary.resourcesFile == null) return; //判断点现的原文件是不是空；
                using var tr = new DBTrans();

                // 获取对应块的 ObjectId  
                var referenceFileObId = tr.BlockTable.GetBlockFormA(
                    VariableDictionary.resourcesFile,
                    VariableDictionary.btnFileName,
                    VariableDictionary.btnFileName_blockName,
                    true);

                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;
                if (refFileRec == null)
                {
                    Env.Editor.WriteMessage("未找到块记录！");
                    return;
                }

                Env.Editor.WriteMessage("块！");
                while (true)
                {
                    // 把块插入到当前空间  
                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, referenceFileObId);

                    // 检查是否为实体  
                    if (tr.GetObject(referenceFileBlock) is not Entity referenceFileEntity)
                        return;

                    // 设置图层和颜色等属性  
                    referenceFileEntity.Layer = VariableDictionary.btnBlockLayer;
                    referenceFileEntity.ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                    referenceFileEntity.Scale(new Point3d(0, 0, 0), VariableDictionary.blockScale);

                    //double tempAngle = 0; // 原始角度  
                    var startPoint = new Point3d(0, 0, 0);

                    var jigBlock = new JigEx((mpw, _) =>
                    {
                        // 先移动  
                        referenceFileEntity.Move(startPoint, mpw);
                        startPoint = mpw;
                    });
                    jigBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(referenceFileEntity));
                    jigBlock.SetOptions(msg: "\n指定插入点");

                    // 拖拽  
                    var endPoint = Env.Editor.Drag(jigBlock);
                    if (endPoint.Status != PromptStatus.OK)
                    {
                        // 用户取消插入，则删除已插入的块  
                        tr.GetObject(referenceFileBlock, OpenMode.ForWrite);
                        referenceFileBlock.Erase();
                        break;
                    }

                    // 存储插入点坐标（WCS）  
                    var UcsEndPoint = jigBlock.MousePointWcsLast;
                    pointS.Add(UcsEndPoint);


                    Env.Editor.Redraw();
                }

                // ======================  
                // 在此处根据插入数量绘图  
                // ======================  
                int count = pointS.Count;
                Env.Editor.WriteMessage($"\n已插入 {count} 个块，开始绘制外围图形...");

                if (count == 3)
                { // 三点生成外接圆，圆心与3点等距  
                    Point3d p1 = pointS[0];
                    Point3d p2 = pointS[1];
                    Point3d p3 = pointS[2];

                    // 计算三角形外接圆圆心（与三点等距的点）  
                    Point3d circleCenter = GetCircumcenter(p1, p2, p3);

                    // 计算圆心到三个点的距离，取最大值，然后加上150作为新圆的半径  
                    double radius = p1.DistanceTo(circleCenter) + 150.0;

                    // 检查计算出的圆心是否与三点等距  
                    double dist1 = circleCenter.DistanceTo(p1);
                    double dist2 = circleCenter.DistanceTo(p2);
                    double dist3 = circleCenter.DistanceTo(p3);

                    // 记录到日志，以验证计算正确性  
                    Env.Editor.WriteMessage($"\n圆心到三点的距离: {dist1:F4}, {dist2:F4}, {dist3:F4}");

                    // 正确创建圆：使用外接圆圆心和半径  
                    var circle = new Circle(circleCenter, Vector3d.ZAxis, radius);

                    circle.Layer = VariableDictionary.btnBlockLayer;
                    circle.ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);


                    tr.CurrentSpace.AddEntity(circle);
                    Env.Editor.Redraw();
                    Env.Editor.WriteMessage("\n已创建外围圆形，与三点等距并向外扩展150。");

                }
                else if (count == 4)
                {
                    // 计算中心点  
                    var center = new Point3d(
                        pointS.Average(p => p.X),
                        pointS.Average(p => p.Y),
                        pointS.Average(p => p.Z)
                    );

                    // 绘制矩形 - 使用原始4点作为矩形顶点，向外扩展150  
                    List<Point2d> expandedPoints = new List<Point2d>();

                    foreach (var point in pointS)
                    {
                        // 计算从中心到点的方向向量  
                        Vector3d dirVector = point - center;
                        dirVector = dirVector.GetNormal(); // 单位化向量  

                        // 创建新点：沿着方向向量延伸150的距离  
                        Point3d expandedPoint3d = point + dirVector * 150.0;
                        Point2d expandedPoint = new Point2d(expandedPoint3d.X, expandedPoint3d.Y);
                        expandedPoints.Add(expandedPoint);
                    }

                    // 确保点按顺时针或逆时针排序  
                    // 对顶点按角度排序  
                    var sortedPoints = expandedPoints.Select((p, index) => new
                    {
                        Point = p,
                        Angle = Math.Atan2(p.Y - center.Y, p.X - center.X)
                    })
                    .OrderBy(item => item.Angle)
                    .Select(item => item.Point)
                    .ToList();

                    // 创建Polyline并添加扩展后的顶点  
                    var pl = new Polyline();
                    for (int i = 0; i < sortedPoints.Count; i++)
                    {
                        pl.AddVertexAt(i, sortedPoints[i], 0, 30, 30);
                    }

                    // 闭合  
                    pl.Closed = true;

                    pl.Layer = VariableDictionary.btnBlockLayer;
                    pl.ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);

                    tr.CurrentSpace.AddEntity(pl);
                    Env.Editor.Redraw();
                    Env.Editor.WriteMessage("\n已创建外围矩形，向外扩展150。");
                }
                else if (count > 4)
                {
                    // 计算中心点  
                    var center = new Point3d(
                        pointS.Average(p => p.X),
                        pointS.Average(p => p.Y),
                        pointS.Average(p => p.Z)
                    );

                    // 创建多边形 - 使用原始点作为多边形顶点，向外扩展150  
                    List<Point2d> expandedPoints = new List<Point2d>();

                    foreach (var point in pointS)
                    {
                        // 计算从中心到点的方向向量  
                        Vector3d dirVector = point - center;
                        dirVector = dirVector.GetNormal(); // 单位化向量  

                        // 创建新点：沿着方向向量延伸150的距离  
                        Point3d expandedPoint3d = point + dirVector * 150.0;
                        Point2d expandedPoint = new Point2d(expandedPoint3d.X, expandedPoint3d.Y);
                        expandedPoints.Add(expandedPoint);
                    }

                    // 对顶点按角度排序，确保多边形正确  
                    var sortedPoints = expandedPoints.Select((p, index) => new
                    {
                        Point = p,
                        Angle = Math.Atan2(p.Y - center.Y, p.X - center.X)
                    })
                    .OrderBy(item => item.Angle)
                    .Select(item => item.Point)
                    .ToList();

                    // 创建多边形  
                    var polygon = new Polyline();

                    for (int i = 0; i < sortedPoints.Count; i++)
                    {
                        polygon.AddVertexAt(i, sortedPoints[i], 0, 30, 30);
                    }

                    // 闭合多边形  
                    polygon.Closed = true;

                    polygon.Layer = VariableDictionary.btnBlockLayer;
                    polygon.ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);

                    tr.CurrentSpace.AddEntity(polygon);
                    Env.Editor.Redraw();
                    Env.Editor.WriteMessage($"\n已创建{count}边形外围，向外扩展150。");

                }
                else if (count > 0)
                {
                    Env.Editor.WriteMessage($"\n已插入{count}个块，但数量不满足绘制外围图形的条件（需要至少3个点）。");
                }
                //加标注
                // DDimLinear("总重:" + VariableDictionary.dimString + "kg" + "\n" + $"{count}点着地", Convert.ToInt16(pointS.Count));
                var dimColorLine = VariableDictionary.layerColorIndex;
                if (VariableDictionary.btnFileName.Contains("结构"))
                {
                    dimColorLine = 3;
                }
                if (VariableDictionary.dimString != null)
                    DDimLinear(VariableDictionary.dimString, count.ToString(), Convert.ToInt16(dimColorLine));
                tr.Commit();
                Env.Editor.Redraw();
                Env.Editor.WriteMessage("\n操作完成。");
                pointS.Clear();
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"\n插入图元失败：{ex.Message}");
                // 可以添加更详细的错误信息记录  
                Env.Editor.WriteMessage($"\n错误详情：{ex.StackTrace}");
            }
            #endregion
        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        [CommandMethod(nameof(GB_InsertBlock_Ptj))]
        public static void GB_InsertBlock_Ptj()
        {
            #region 方法2：
            try
            {
                Directory.CreateDirectory(GetPath.referenceFile);  //获取到本工具的系统目录；
                if (VariableDictionary.btnFileName == null) return; //判断点现的按键名是不是空；
                if (VariableDictionary.resourcesFile == null) return; //判断点现的原文件是不是空；
                using var tr = new DBTrans();
                var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, true);//拿到本工具的系统目录下的按键名的原文件的objectid；
                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;//拿到原文件的块表记录；
                int isNo = -1;
                if (refFileRec != null)
                    foreach (var fileId in refFileRec)
                    {
                        isNo++;
                        if (VariableDictionary.btnFileName == "PTJ_给水点") isNo = -1;
                        else if (isNo != VariableDictionary.TCH_Ptj_No) continue;
                        //判读是不是为0，0就是天正元素
                        //if (fileId.ObjectClass.DxfName == "INSERT") continue;
                        var fileEntity = tr.GetObject(fileId, OpenMode.ForRead) as Entity;
                        //Env.Editor.WriteMessage("PTJ元素！");
                        if (fileEntity == null) continue;
                        var fileEntityCopy = fileEntity.Clone() as Entity;
                        //tr.CurrentSpace.DeepCloneEx(fileEntity,)//深度克隆，可以复制天正图元；
                        //(vlax-dump-object (vlax-ename->vla-object (car (entsel )))T)//这个是在cad命令里能读出天正属性的lisp命令；
                        if (fileEntityCopy == null) continue;
                        var dxfName = fileId.ObjectClass.DxfName;//抓取图元的DXFName,判断是不是天正的图元
                        var fileType = dxfName.Split('_');//截取‘_’字符
                        if (fileType[0] == "TCH")
                        {
                            Env.Editor.WriteMessage("PTJ-TCH！");
                            var fileEntityCopyObId = tr.CurrentSpace.AddEntity(fileEntityCopy);
                            double tempAngle = 0;
                            var startPoint = new Point3d(0, 0, 0);
                            var entityBlock = new JigEx((mpw, _) =>
                            {
                                fileEntityCopy.Move(startPoint, mpw);
                                startPoint = mpw;
                                if (VariableDictionary.entityRotateAngle == tempAngle)
                                {
                                    return;
                                }
                                else if (VariableDictionary.entityRotateAngle != tempAngle)
                                {
                                    fileEntityCopy.Rotation(center: mpw, 0);
                                    tempAngle = VariableDictionary.entityRotateAngle;
                                    fileEntityCopy.Rotation(center: mpw, tempAngle);
                                }
                            });
                            entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(fileEntityCopy));
                            entityBlock.SetOptions(msg: "\n指定插入点");
                            //entityBlock.SetOptions(startPoint, msg: "\n指定插入点");这个startpoint，是有个参考线在里面，用于托拽时的辅助；
                            var endPoint = Env.Editor.Drag(entityBlock);
                            if (endPoint.Status != PromptStatus.OK) return;
                            tr.BlockTable.Remove(referenceFileObId);
                        }
                        else if (fileEntityCopy is BlockReference)
                        {
                            Env.Editor.WriteMessage("PTJ-块表记录！");
                            //if (fileEntityCopy.ColorIndex.ToString() != "130") return;
                            var fileEntityCopyObId = tr.CurrentSpace.AddEntity(fileEntityCopy);//在当前图纸空间中加入这个实体并获取它的ObjoectId
                            double tempAngle = 0;
                            var startPoint = new Point3d(0, 0, 0);
                            var entityBlock = new JigEx((mpw, _) =>
                            {
                                fileEntityCopy.Move(startPoint, mpw);
                                startPoint = mpw;
                                if (VariableDictionary.entityRotateAngle == tempAngle)
                                {
                                    return;
                                }
                                else if (VariableDictionary.entityRotateAngle != tempAngle)
                                {
                                    fileEntityCopy.Rotation(center: mpw, 0);
                                    tempAngle = VariableDictionary.entityRotateAngle;
                                    fileEntityCopy.Rotation(center: mpw, tempAngle);
                                }
                            });
                            entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(fileEntityCopy));
                            entityBlock.SetOptions(msg: "\n指定插入点");
                            //entityBlock.SetOptions(startPoint, msg: "\n指定插入点");这个startpoint，是有个参考线在里面，用于托拽时的辅助；
                            var endPoint = Env.Editor.Drag(entityBlock);
                            if (endPoint.Status != PromptStatus.OK) return;
                            tr.BlockTable.Remove(referenceFileObId);
                        }
                        else
                        {
                            Env.Editor.WriteMessage("PTJ-块！");
                            var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, referenceFileObId);
                            //tr.BlockTable.Remove(referenceFileObId);
                            if (tr.GetObject(referenceFileBlock) is not Entity referenceFileEntity) return;
                            double tempAngle = 0;
                            var startPoint = new Point3d(0, 0, 0);
                            var entityBlock = new JigEx((mpw, _) =>
                            {
                                referenceFileEntity.Move(startPoint, mpw);
                                startPoint = mpw;
                                if (VariableDictionary.entityRotateAngle == tempAngle)
                                {
                                    return;
                                }
                                else if (VariableDictionary.entityRotateAngle != tempAngle)
                                {
                                    referenceFileEntity.Rotation(center: mpw, 0);
                                    tempAngle = VariableDictionary.entityRotateAngle;
                                    referenceFileEntity.Rotation(center: mpw, tempAngle);
                                }
                            });
                            entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(referenceFileEntity));
                            entityBlock.SetOptions(msg: "\n指定插入点");
                            var endPoint = Env.Editor.Drag(entityBlock);
                            if (endPoint.Status != PromptStatus.OK) return;
                            referenceFileEntity.Layer = VariableDictionary.btnBlockLayer;
                            break;
                        }
                    }

                tr.Commit();
                Env.Editor.Redraw();

            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage("错误信息: " + ex.Message);
            }
            #endregion
        }

        /// <summary>
        /// 插入外部条件图元
        /// </summary>
        [CommandMethod(nameof(GB_Draw_InsertBlock))]
        public static void GB_Draw_InsertBlock()
        {
            #region 方法1：
            try
            {
                Directory.CreateDirectory(GetPath.referenceFile);  //获取到本工具的系统目录；
                if (VariableDictionary.btnFileName == null) return; //判断点现的按键名是不是空；
                if (VariableDictionary.resourcesFile == null) return; //判断点现的原文件是不是空；
                using var tr = new DBTrans();
                //var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, true);//拿到本工具的系统目录下的按键名的原文件的objectid；
                //拿到本工具的系统目录下的按键名的原文件的objectid；
                var referenceFileObId = tr.BlockTable.GetBlockFormA(VariableDictionary.resourcesFile, VariableDictionary.btnFileName, VariableDictionary.btnFileName_blockName, true);
                var refFileRec = tr.GetObject(referenceFileObId, OpenMode.ForRead) as BlockTableRecord;//拿到原文件的块表记录；
                //var dimPoint = new Point3d(0, 0, 0);

                if (refFileRec != null)
                {
                    Env.Editor.WriteMessage("块！");
                    //把块插入到当前空间
                    var referenceFileBlock = tr.CurrentSpace.InsertBlock(Point3d.Origin, referenceFileObId);
                    //尝试转换为实体
                    if (tr.GetObject(referenceFileBlock) is not Entity referenceFileEntity)
                        return;
                    //设置比例
                    referenceFileEntity.Scale(new Point3d(0, 0, 0), VariableDictionary.blockScale);
                    double tempAngle = 0;
                    var startPoint = Env.Editor.GetPoint("请输入起始点");
                    if (startPoint.Status != PromptStatus.OK) return;
                    var startPointUcs = startPoint.Value;

                    //var startPoint = new Point3d(0, 0, 0);
                    var entityBlock = new JigEx((mpw, _) =>
                    {
                        referenceFileEntity.Move(startPointUcs, mpw);
                        startPointUcs = mpw;
                        if (VariableDictionary.entityRotateAngle == tempAngle)
                        { return; }
                        else if (VariableDictionary.entityRotateAngle != tempAngle)
                        {
                            referenceFileEntity.Rotation(center: mpw, 0);
                            tempAngle = VariableDictionary.entityRotateAngle;
                            referenceFileEntity.Rotation(center: mpw, tempAngle);
                        }
                    });
                    entityBlock.DatabaseEntityDraw(wd => wd.Geometry.Draw(referenceFileEntity));
                    entityBlock.SetOptions(msg: "\n指定插入点");
                    var endPoint = Env.Editor.Drag(entityBlock);
                    if (endPoint.Status != PromptStatus.OK)
                        return;
                    referenceFileEntity.Layer = VariableDictionary.btnBlockLayer;
                    //dimPoint = entityBlock.MousePointWcsLast;
                    Env.Editor.Redraw();
                }

                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage("错误信息: " + ex.Message);
            }
            #endregion

        }

        /// <summary>
        /// 计算三角形外接圆圆心，确保圆心与三个点等距 
        /// </summary>
        /// <param name="A">A</param>
        /// <param name="B">B</param>
        /// <param name="C">C</param>
        /// <returns></returns>
        private static Point3d GetCircumcenter(Point3d A, Point3d B, Point3d C)
        {
            // 处理共线情况：如果三点共线，则返回三点的平均点  
            if (ArePointsCollinear(A, B, C))
            {
                return new Point3d(
                    (A.X + B.X + C.X) / 3.0,
                    (A.Y + B.Y + C.Y) / 3.0,
                    (A.Z + B.Z + C.Z) / 3.0
                );
            }
            // 计算分母 d  
            double d = 2 * (A.X * (B.Y - C.Y) + B.X * (C.Y - A.Y) + C.X * (A.Y - B.Y));
            if (Math.Abs(d) < 1e-10)
            {
                // 保护性返回：当分母太小时则视为共线，返回平均值  
                return new Point3d(
                    (A.X + B.X + C.X) / 3.0,
                    (A.Y + B.Y + C.Y) / 3.0,
                    (A.Z + B.Z + C.Z) / 3.0
                );
            }
            // 分别计算各点的 (x^2 + y^2)  
            double Asq = A.X * A.X + A.Y * A.Y;
            double Bsq = B.X * B.X + B.Y * B.Y;
            double Csq = C.X * C.X + C.Y * C.Y;
            // 使用标准公式计算圆心 X/Y 坐标  
            double centerX = (Asq * (B.Y - C.Y) + Bsq * (C.Y - A.Y) + Csq * (A.Y - B.Y)) / d;
            double centerY = (Asq * (C.X - B.X) + Bsq * (A.X - C.X) + Csq * (B.X - A.X)) / d;
            double centerZ = (A.Z + B.Z + C.Z) / 3.0; // Z 坐标取平均值  
            return new Point3d(centerX, centerY, centerZ);
        }

        /// <summary>
        /// 检查三点是否共线
        /// </summary>
        /// <param name="A">A</param>
        /// <param name="B">B</param>
        /// <param name="C">C</param>
        /// <returns></returns>
        private static bool ArePointsCollinear(Point3d A, Point3d B, Point3d C)
        {
            Vector3d v1 = B - A;
            Vector3d v2 = C - A;
            Vector3d crossProduct = v1.CrossProduct(v2);
            return crossProduct.Length < 1e-8;
        }
        #endregion

        #region 结构画方、园、多边形
        /// <summary>
        /// 结构-用户指定原点后半径画圆；
        /// </summary>
        [CommandMethod(nameof(CirRadius))]
        public static void CirRadius()
        {
            try
            {
                double width = 0;
                //获取图层名称
                string? layerName = VariableDictionary.btnBlockLayer;//图层名
                VariableDictionary.layerColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);//图层颜色
                double cirPlus = Convert.ToDouble(VariableDictionary.textbox_CirPlus_Text) * 2;//拿到圆的扩展值；
                if (VariableDictionary.textbox_CirPlus_Text == null) cirPlus = 0;//如果没有搌值，那就是0；
                using var tr = new DBTrans();//开启事务
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, VariableDictionary.layerColorIndex);//添加图层；
                var userPoint1 = Env.Editor.GetPoint("\n请指定圆形洞口的起点");//指定圆的第一点
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();//把指定的点转成Wcs2坐标；
                // 创建polyline
                Polyline polylineHatch = new Polyline();
                Point3d center = new Point3d(0, 0, 0);//圆心
                //拖动实现
                using var cir = new JigEx((mpw, queue) =>
                {
                    var userPoint2 = mpw.Z20();//mpw为鼠标移动变量；
                    var userCir = new Circle(UcsUserPoint1, Vector3d.ZAxis, (userPoint2.DistanceTo(UcsUserPoint1)) + cirPlus);//创建半径动态圆；
                    userCir.Layer = layerName;//动态圆设置图层；
                    userCir.ColorIndex = VariableDictionary.layerColorIndex;
                    center = userCir.Center;//圆心
                    double radius = userCir.Radius;//圆半径
                    width = radius * 2;
                    Point3d polyline1Start = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y - radius * Math.Sin(Math.PI / 4), 0);
                    Point3d polylineCenter = new Point3d(center.X - radius * Math.Cos(Math.PI / 4) / 2, center.Y + radius * Math.Sin(Math.PI / 4) / 2, 0);
                    Point3d polyline2End = new Point3d(center.X + radius * Math.Cos(Math.PI / 4), center.Y + radius * Math.Sin(Math.PI / 4), 0);
                    Point3d pointOnArc = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y - radius * Math.Sin(Math.PI / 4), 0);

                    var polyline1 = new Polyline();
                    double bulge = CalculateBulge(polyline1Start, pointOnArc, polyline2End, center, radius);
                    polyline1.AddVertexAt(0, new Point2d(polyline1Start.X, polyline1Start.Y), -bulge, 0, 0);
                    polyline1.AddVertexAt(1, new Point2d(polyline2End.X, polyline2End.Y), 0, 0, 0);
                    polyline1.AddVertexAt(2, new Point2d(polylineCenter.X, polylineCenter.Y), 0, 0, 0);
                    polyline1.AddVertexAt(3, new Point2d(polyline1Start.X, polyline1Start.Y), 0, 0, 0);
                    //polyline1.Closed = true;
                    polyline1.Layer = layerName;
                    polyline1.ColorIndex = VariableDictionary.layerColorIndex;
                    queue.Enqueue(polyline1);
                    queue.Enqueue(userCir);
                    polylineHatch = polyline1;
                });
                cir.SetOptions(UcsUserPoint1, msg: "\n请指定圆形洞口的终点");//提示用户输入第二点；
                var r2 = Env.Editor.Drag(cir);//拿到第二点
                if (r2.Status != PromptStatus.OK) tr.Abort();
                tr.CurrentSpace.AddEntity(cir.Entities);//把圆的实体写入当前的空间；
                if (layerName != null && layerName.Contains("结构"))
                    //调用填充
                    autoHatch(tr, layerName, VariableDictionary.layerColorIndex, 50, "DOTS", polylineHatch.ObjectId);
                Env.Editor.Redraw();
                DDimLinear((width).ToString("0.00"), Convert.ToInt16(VariableDictionary.layerColorIndex), (0).ToString("0.00"), center);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构-用户指定两点为直径画圆失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 结构-用户指定两点为半径画圆；
        /// </summary>
        [CommandMethod(nameof(CirRadius_2))]
        public static void CirRadius_2()
        {
            try
            {
                string? layerName = VariableDictionary.btnBlockLayer;
                int layerColor = VariableDictionary.layerColorIndex;
                double cirPlus = Convert.ToDouble(VariableDictionary.textbox_CirPlus_Text);//拿到圆的扩展值；
                double cirRedius = 1;//圆的半径
                if (VariableDictionary.textbox_S_Cirradius != null) cirRedius = VariableDictionary.textbox_S_Cirradius.Value;//圆的半径
                if (VariableDictionary.textbox_CirPlus_Text == null) cirPlus = 0;//如果没有搌值，那就是0；
                using var tr = new DBTrans();//开启事务
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, 231);//添加图层；
                var cirCenter = new Point3d(0, 0, 0);
                var polylineHatch = new Polyline();
                Point3d center = new Point3d(0, 0, 0);//圆的中心
                //拖动圆
                using var cir = new JigEx((mpw, queue) =>
                {
                    var userPoint2 = mpw.Z20();//mpw为鼠标移动变量；
                    var userCir = new Circle(mpw, Vector3d.ZAxis, (cirRedius) + cirPlus);//创建指定直径的圆；
                    userCir.Layer = layerName;//圆设置图层；
                    userCir.ColorIndex = layerColor;
                    center = userCir.Center;//圆的中心
                    double radius = userCir.Radius;//圆的半径
                    // 计算两个polyline的点
                    Point3d polyline1Start = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y - radius * Math.Sin(Math.PI / 4), 0);
                    Point3d polylineCenter = new Point3d(center.X - radius * Math.Cos(Math.PI / 4) / 2, center.Y + radius * Math.Sin(Math.PI / 4) / 2, 0);
                    Point3d polyline2End = new Point3d(center.X + radius * Math.Cos(Math.PI / 4), center.Y + radius * Math.Sin(Math.PI / 4), 0);
                    Point3d pointOnArc = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y - radius * Math.Sin(Math.PI / 4), 0);

                    var polyline1 = new Polyline();
                    double bulge = CalculateBulge(polyline1Start, pointOnArc, polyline2End, center, radius);
                    polyline1.AddVertexAt(0, new Point2d(polyline1Start.X, polyline1Start.Y), -bulge, 0, 0);
                    polyline1.AddVertexAt(1, new Point2d(polyline2End.X, polyline2End.Y), 0, 0, 0);
                    polyline1.AddVertexAt(2, new Point2d(polylineCenter.X, polylineCenter.Y), 0, 0, 0);
                    polyline1.AddVertexAt(3, new Point2d(polyline1Start.X, polyline1Start.Y), 0, 0, 0);
                    //polyline1.Closed = true;
                    polyline1.Layer = layerName;
                    polyline1.ColorIndex = layerColor;
                    queue.Enqueue(polyline1);
                    queue.Enqueue(userCir);
                    polylineHatch = polyline1;
                });
                cir.SetOptions(msg: "\n请指定圆形洞口的终点");//提示用户输入第二点；
                var r2 = Env.Editor.Drag(cir);//拿到第二点
                if (r2.Status != PromptStatus.OK) tr.Abort();
                tr.CurrentSpace.AddEntity(cir.Entities);//把圆的实体写入当前的空间；
                if (layerName != null && layerName.Contains("结构"))
                    //调用填充
                    autoHatch(tr, layerName, layerColor, 50, "DOTS", polylineHatch.ObjectId);
                Env.Editor.Redraw();
                DDimLinear((cirRedius * 2 + cirPlus * 2).ToString("0.00"), (0).ToString("0.00"), Convert.ToInt16(layerColor), center);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构-用户指定两点为直径画圆失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 结构-用户指定数值为直径画圆；
        /// </summary>
        [CommandMethod(nameof(CirDiameter))]
        public static void CirDiameter()
        {
            try
            {
                double width = 0;
                string? layerName = VariableDictionary.btnBlockLayer;
                int layerColor = VariableDictionary.layerColorIndex;
                double cirPlus = Convert.ToDouble(VariableDictionary.textbox_CirPlus_Text);//拿到圆的扩展值；
                if (VariableDictionary.textbox_CirPlus_Text == null) cirPlus = 0;//如果没有搌值，那就是0；
                using var tr = new DBTrans();//开启事务
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, layerColor);//添加图层；
                var userPoint1 = Env.Editor.GetPoint("\n请指定圆形洞口的起点");//指定圆的第一点
                if (userPoint1.Status != PromptStatus.OK) return; //指定成功
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();//把指定的点转成Wcs2坐标；
                // 声明一个变量保存填充边界的多段线（用于 SOLID 填充）  
                Polyline polylineHatch = new Polyline();
                Point3d center = new Point3d(0, 0, 0);
                //拖动实现
                using var cir = new JigEx((mpw, queue) =>
                {
                    var userPoint2 = mpw.Z20();//mpw为鼠标移动变量；
                    var userCir = new Circle(UcsUserPoint1.GetMidPointTo(pt2: userPoint2), Vector3d.ZAxis, (userPoint2.DistanceTo(UcsUserPoint1) / 2) + cirPlus);//创建动态圆；
                    userCir.Layer = layerName;//动态圆设置图层；
                    userCir.ColorIndex = layerColor;
                    queue.Enqueue(userCir);//动态的圆，跟随鼠标变化；
                    center = userCir.Center;
                    double radius = userCir.Radius;
                    width = radius * 2;
                    // 计算两个polyline的点
                    Point3d polyline1Start = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y - radius * Math.Sin(Math.PI / 4), 0);
                    Point3d polylineCenter = new Point3d(center.X - radius * Math.Cos(Math.PI / 4) / 2, center.Y + radius * Math.Sin(Math.PI / 4) / 2, 0);
                    Point3d polyline2End = new Point3d(center.X + radius * Math.Cos(Math.PI / 4), center.Y + radius * Math.Sin(Math.PI / 4), 0);
                    Point3d pointOnArc = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y - radius * Math.Sin(Math.PI / 4), 0);
                    // 创建第一个polyline
                    var polyline1 = new Polyline();
                    double bulge = CalculateBulge(polyline1Start, pointOnArc, polyline2End, center, radius);
                    polyline1.AddVertexAt(0, new Point2d(polyline1Start.X, polyline1Start.Y), -bulge, 0, 0);
                    polyline1.AddVertexAt(1, new Point2d(polyline2End.X, polyline2End.Y), 0, 0, 0);
                    polyline1.AddVertexAt(2, new Point2d(polylineCenter.X, polylineCenter.Y), 0, 0, 0);
                    polyline1.AddVertexAt(3, new Point2d(polyline1Start.X, polyline1Start.Y), 0, 0, 0);
                    queue.Enqueue(polyline1);
                    polyline1.Layer = layerName;
                    polyline1.ColorIndex = layerColor;
                    polylineHatch = polyline1;
                });
                cir.SetOptions(UcsUserPoint1, msg: "\n请指定圆形洞口的终点");//提示用户输入第二点；
                var r2 = Env.Editor.Drag(cir);//拿到第二点
                if (r2.Status != PromptStatus.OK) tr.Abort();
                var cirEntity = tr.CurrentSpace.AddEntity(cir.Entities);//把圆的实体写入当前的空间；
                if (layerName != null && layerName.Contains("结构"))
                    //调用填充
                    autoHatch(tr, layerName, layerColor, 50, "DOTS", polylineHatch.ObjectId);
                Env.Editor.Redraw();
                DDimLinear((width).ToString("0.00"), (0).ToString("0.00"), Convert.ToInt16(layerColor), center);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构-用户指定两点为直径画圆失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 结构-用户指定两点为直径画圆；
        /// </summary>
        [CommandMethod(nameof(CirDiameter_2))]
        public static void CirDiameter_2()
        {
            try
            {
                string? layerName = VariableDictionary.btnBlockLayer;
                int layerColor = VariableDictionary.layerColorIndex;
                double cirPlus = Convert.ToDouble(VariableDictionary.textbox_CirPlus_Text) * 2;//拿到圆的扩展值；
                double cirDiameter = 1;
                if (VariableDictionary.textBox_S_CirDiameter != null) cirDiameter = VariableDictionary.textBox_S_CirDiameter.Value;
                if (VariableDictionary.textbox_CirPlus_Text == null) cirPlus = 0;//如果没有搌值，那就是0；

                using var tr = new DBTrans();//开启事务
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, 231);//添加图层；
                var cirCenter = new Point3d(0, 0, 0);
                // 创建第一个polyline
                Polyline polylineHatch = new Polyline();
                Point3d center = new Point3d(0, 0, 0);//圆的中心
                //拖动圆
                using var cir = new JigEx((mpw, queue) =>
                {
                    var userCir = new Circle(mpw, Vector3d.ZAxis, (cirDiameter / 2) + cirPlus / 2);//创建指定直径的圆；
                    userCir.Layer = layerName;//圆设置图层；
                    userCir.ColorIndex = layerColor;
                    center = userCir.Center;//圆的中心
                    double radius = userCir.Radius;//圆的半径
                    // 计算两个polyline的点
                    var polyline1Start = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y - radius * Math.Sin(Math.PI / 4), 0);
                    var polylineCenter = new Point3d(center.X - radius * Math.Cos(Math.PI / 4) / 2, center.Y + radius * Math.Sin(Math.PI / 4) / 2, 0);
                    var polyline2End = new Point3d(center.X + radius * Math.Cos(Math.PI / 4), center.Y + radius * Math.Sin(Math.PI / 4), 0);
                    //拿到在开始点到结束点中间的一个点
                    var pointOnArc = new Point3d(center.X - radius * Math.Cos(Math.PI / 4), center.Y + radius * Math.Sin(Math.PI / 4), 0);

                    var polyline1 = new Polyline();
                    double bulge = CalculateBulge(polyline1Start, pointOnArc, polyline2End, center, radius);
                    polyline1.AddVertexAt(0, new Point2d(polyline1Start.X, polyline1Start.Y), bulge, 0, 0);
                    polyline1.AddVertexAt(1, new Point2d(polyline2End.X, polyline2End.Y), 0, 0, 0);
                    polyline1.AddVertexAt(2, new Point2d(polylineCenter.X, polylineCenter.Y), 0, 0, 0);
                    polyline1.AddVertexAt(3, new Point2d(polyline1Start.X, polyline1Start.Y), 0, 0, 0);
                    //polyline1.Closed = true;
                    polyline1.Layer = layerName;
                    polyline1.ColorIndex = layerColor;
                    queue.Enqueue(polyline1);
                    queue.Enqueue(userCir);
                    polylineHatch = polyline1;
                });
                cir.SetOptions(msg: "\n请指定圆形洞口的终点");//提示用户输入第二点；
                var r2 = Env.Editor.Drag(cir);//拿到第二点
                if (r2.Status != PromptStatus.OK) tr.Abort();//如果不是ok，那就撤消
                tr.CurrentSpace.AddEntity(cir.Entities);//把圆的实体写入当前的空间；
                if (layerName != null && layerName.Contains("结构"))
                    //调用填充
                    autoHatch(tr, layerName, layerColor, 50, "DOTS", polylineHatch.ObjectId);
                Env.Editor.Redraw();


                DDimLinear((cirDiameter + cirPlus).ToString("0.00"), (0).ToString("0.00"), Convert.ToInt16(layerColor), center);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构-用户指定两点为直径画圆失败: {ex.Message}");
            }
        }


        /// <summary>
        /// 计算bulge
        /// </summary>
        /// <param name="polyline1Start">pl线开始点</param>
        /// <param name="pointOnArc">圆弧上的点</param>
        /// <param name="polyline2End">pl线结束点</param>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns></returns>
        private static double CalculateBulge(Point3d polyline1Start, Point3d pointOnArc, Point3d polyline2End, Point3d center, double radius)
        {
            //求得圆心到开始点的向量
            var cs = center.GetVectorTo(polyline1Start);
            //求得圆心到结束点的向量
            var ce = center.GetVectorTo(polyline2End);
            //X方向向量
            Vector3d xvector = new Vector3d(1, 0, 0);
            //绘制一个临时圆弧
            CircularArc3d cirArc = new CircularArc3d(polyline1Start, pointOnArc, polyline2End);
            ////计算圆弧的开如角度
            double startAngle = cs.Y > 0 ? xvector.GetAngleTo(cs) : -xvector.GetAngleTo(cs);
            ////计算圆弧的结束角度
            double endAngle = ce.Y > 0 ? xvector.GetAngleTo(ce) : -xvector.GetAngleTo(ce);
            //绘制一个圆弧

            Arc arc = new Arc(center, radius, startAngle, endAngle);

            // 计算圆弧的夹角
            //double angle = v1.GetAngleTo(v2);
            // 计算 bulge 值（tan(角度/4)）
            double bulge = Math.Tan(startAngle);
            if (((pointOnArc.X - polyline1Start.X) * (polyline2End.Y - polyline1Start.Y) - (pointOnArc.Y - polyline1Start.Y) * (polyline2End.X - polyline1Start.X)) < 0)
                bulge = -bulge;
            return bulge;
        }

        /// <summary>
        /// 结构用户用鼠标画矩形
        /// </summary>
        /// <param name="layerName"></param>
        [CommandMethod(nameof(Rec2PolyLine))]
        public static void Rec2PolyLine()
        {
            try
            {
                double width = 0;
                double height = 0;
                // 指定图层名及颜色  
                var layerName = VariableDictionary.btnBlockLayer;
                var layerColor = VariableDictionary.layerColorIndex;
                if (layerName == null)
                    return;
                double recPlus = Convert.ToDouble(VariableDictionary.textbox_RecPlus_Text); // 指定的偏移量  
                using var tr = new DBTrans();
                if (!tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, layerColor);
                // 获取方形洞口的第一点  
                var userPoint1 = Env.Editor.GetPoint("\n请指定方形洞口第一点");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                // 声明一个变量保存填充边界的多段线（用于 SOLID 填充）  
                Polyline hatchBoundary = new Polyline();
                var newRectPointMin = new Point3d(0, 0, 0);
                var newRectPointMax = new Point3d(0, 0, 0);

                // 使用 JigEx 动态预览，绘制矩形以及两条辅助线  
                using var rec = new JigEx((mpw, queue) =>
                {
                    var UcsUserPoint2 = mpw.Z20();
                    // 动态绘制原始矩形（从用户第一点到第二点）  
                    Polyline polylineRec = new Polyline();
                    polylineRec.AddVertexAt(0, new Point2d(UcsUserPoint1.X, UcsUserPoint1.Y), 0, 0, 0);
                    polylineRec.AddVertexAt(1, new Point2d(UcsUserPoint2.X, UcsUserPoint1.Y), 0, 0, 0);
                    polylineRec.AddVertexAt(2, new Point2d(UcsUserPoint2.X, UcsUserPoint2.Y), 0, 0, 0);
                    polylineRec.AddVertexAt(3, new Point2d(UcsUserPoint1.X, UcsUserPoint2.Y), 0, 0, 0);
                    polylineRec.Closed = true; // 闭合成方形  
                    Extents3d polyLineRecExt = new Extents3d();
                    // 获取原始矩形的边界  
                    if (polylineRec.Bounds != null) polyLineRecExt = (Extents3d)polylineRec.Bounds;
                    // 计算扩大后的矩形边界（偏移 recPlus）  
                    Extents3d newRectBounds = new Extents3d(
                        new Point3d(polyLineRecExt.MinPoint.X - recPlus, polyLineRecExt.MinPoint.Y - recPlus, 0),
                        new Point3d(polyLineRecExt.MaxPoint.X + recPlus, polyLineRecExt.MaxPoint.Y + recPlus, 0));
                    width = newRectBounds.MaxPoint.X - newRectBounds.MinPoint.X;//拿到宽
                    height = newRectBounds.MaxPoint.Y - newRectBounds.MinPoint.Y;//拿到高
                    // 辅助计算：取扩大矩形的左上与右下点，计算二者间 1/4 点（作为两条线交合点）  
                    var leftUp = new Point3d(newRectBounds.MinPoint.X, newRectBounds.MaxPoint.Y, 0);
                    var rightDown = new Point3d(newRectBounds.MaxPoint.X, newRectBounds.MinPoint.Y, 0);
                    double targetX = leftUp.X + (rightDown.X - leftUp.X) * 1.0 / 4;
                    double targetY = leftUp.Y + (rightDown.Y - leftUp.Y) * 1.0 / 4;
                    double targetZ = 0; // Z 为 0  
                    Point3d targetPoint = new Point3d(targetX, targetY, targetZ);

                    // 创建扩大后的矩形  
                    Polyline newRect = new Polyline();
                    newRect.AddVertexAt(0, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MinPoint.Y), 0, 3, 3);
                    newRect.AddVertexAt(1, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MaxPoint.Y), 0, 3, 3);
                    newRect.AddVertexAt(2, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MaxPoint.Y), 0, 3, 3);
                    newRect.AddVertexAt(3, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MinPoint.Y), 0, 3, 3);
                    newRect.Closed = true;
                    newRect.Layer = layerName;
                    newRect.ColorIndex = layerColor;
                    newRectPointMin = new Point3d(newRectBounds.MinPoint.X, newRectBounds.MinPoint.Y, 0);
                    newRectPointMax = new Point3d(newRectBounds.MaxPoint.X, newRectBounds.MaxPoint.Y, 0);
                    queue.Enqueue(newRect); // 放大后的矩形  
                    // 绘制用来辅助生成填充边界的两条线（或辅助多段线）  
                    // 此处构造的填充边界区域：由扩大矩形的左下角、左上角、右上角，以及计算得到的交合点构成  
                    if (layerName.Contains("结构"))
                    {
                        hatchBoundary = new Polyline();
                        hatchBoundary.AddVertexAt(0, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MinPoint.Y), 0, 3, 3);
                        hatchBoundary.AddVertexAt(1, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MaxPoint.Y), 0, 3, 3);
                        hatchBoundary.AddVertexAt(2, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MaxPoint.Y), 0, 3, 3);
                        hatchBoundary.AddVertexAt(3, new Point2d(targetPoint.X, targetPoint.Y), 0, 3, 3);
                        hatchBoundary.Closed = true;
                        hatchBoundary.Layer = layerName;
                        hatchBoundary.ColorIndex = layerColor;
                        queue.Enqueue(hatchBoundary);
                    }
                });
                rec.SetOptions(UcsUserPoint1, msg: "\n请指定方形洞口第二点");
                var userPoint2 = Env.Editor.Drag(rec);
                if (userPoint2.Status != PromptStatus.OK)
                    tr.Abort();
                // 将动态绘制得到的所有实体添加到当前空间  
                tr.CurrentSpace.AddEntity(rec.Entities);
                Env.Editor.Redraw();
                if (layerName.Contains("结构"))
                    //调用填充
                    autoHatch(tr, layerName, layerColor, 50, "DOTS", hatchBoundary.ObjectId);
                Env.Editor.Redraw();
                //计算洞口的中心点
                var centerPoint = new Point3d(((newRectPointMin.X + newRectPointMax.X) / 2), (newRectPointMin.Y + newRectPointMax.Y) / 2, 0);
                DDimLinear((width).ToString("0.00"), (height).ToString("0.00"), Convert.ToInt16(layerColor), centerPoint);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构指定数值生成矩形失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 暖通用户指定两点为对角线画方Rec2PolyLine_N
        /// </summary>
        /// <param name="layerName"></param>
        [CommandMethod(nameof(Rec2PolyLine_N))]
        public static void Rec2PolyLine_N()
        {
            try
            {
                // 获取当前编辑器
                string? layerName = VariableDictionary.btnBlockLayer;
                VariableDictionary.layerColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);//设置图层颜色
                Point3d centerPoint;//初始化起始点
                if (layerName == null)
                    return;
                double recLRPlus = Convert.ToDouble(VariableDictionary.textbox_Width);// 指定左右偏移量
                double recUDPlus = Convert.ToDouble(VariableDictionary.textbox_Height);// 指定左右偏移量
                using var tr = new DBTrans();
                //查找图层，没有即创建；
                if (!tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, ltt =>
                    {
                        if (layerName == "TJ(建筑专业J)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                        else if (layerName == "TJ(结构专业JG)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                        else if (layerName == "TJ(给排水专业S)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                        else if (layerName == "TJ(暖通专业N)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                        else if (layerName == "TJ(热工专业R)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                        else if (layerName == "TJ(电气专业D)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                        else if (layerName == "TJ(自控专业ZK)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                        else if (layerName == "TJ(总图专业ZT)")
                        {
                            ltt.Color = Color.FromColorIndex(ColorMethod.ByColor, 1);
                            ltt.LineWeight = LineWeight.LineWeight030;
                            ltt.IsPlottable = true;
                        }
                    });
                //// 先记住原始的全局线型比例
                //double oldLtscale = Convert.ToDouble(Application.GetSystemVariable("LTSCALE"));
                //Application.SetSystemVariable("LTSCALE", oldLtscale);  // 或直接设 1.0

                //查找线型，没有即创建；
                if (!tr.LinetypeTable.Has("DASH"))
                {
                    tr.LinetypeTable.Add("DASH", ltr =>
                    {
                        ltr.Name = "DASH";
                        ltr.AsciiDescription = " - - - - - ";//线型描述
                        ltr.PatternLength = 1; //线型总长度
                        ltr.NumDashes = 2;//组成线型的笔画数目
                        ltr.SetDashLengthAt(0, 0.6);//0.5个单位的画线
                        ltr.SetDashLengthAt(1, -0.4);//0.3个单位的空格
                        ltr.SetShapeStyleAt(1, tr.TextStyleTable["tJText"]);//设置文字的样式
                        //ltr.SetShapeNumberAt(1, 0);//设置空格处包含的图案图形
                        ltr.SetShapeOffsetAt(1, new Vector2d(-0.1, -0.05));//图形在X轴方向上左移0.1个单位，在Y轴方向上下移0.05个单位
                        ltr.SetShapeScaleAt(1, 0.1);//图形的缩放比例
                        ltr.SetShapeIsUcsOrientedAt(1, false);
                        Application.SetSystemVariable("LTSCALE", 100.0);
                        //ltr.SetShapeRotationAt(1, 0);//ltr.SetTextAt(1, "测绘");//文字内容
                        //ltr.SetDashLengthAt(2, -0.2);//0.2个单位的空格
                    });
                }
                var userPoint1 = Env.Editor.GetPoint("\n请指定方形洞口第一点");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                // 计算扩大后的矩形坐标
                var targetPoint = new Point3d(0, 0, 0);
                using var rec = new JigEx((mpw, queue) =>
                {
                    var UcsUserPoint2 = mpw.Z20();//鼠标所在的动态位置；
                    Polyline polylineRec = new Polyline();
                    polylineRec.AddVertexAt(0, new Point2d(UcsUserPoint1.X, UcsUserPoint1.Y), 0, 0, 0);
                    polylineRec.AddVertexAt(1, new Point2d(UcsUserPoint2.X, UcsUserPoint1.Y), 0, 0, 0);
                    polylineRec.AddVertexAt(2, new Point2d(UcsUserPoint2.X, UcsUserPoint2.Y), 0, 0, 0);
                    polylineRec.AddVertexAt(3, new Point2d(UcsUserPoint1.X, UcsUserPoint2.Y), 0, 0, 0);
                    polylineRec.Closed = true;//闭合成方形

                    centerPoint = new Point3d((UcsUserPoint1.X + UcsUserPoint2.X) / 2, (UcsUserPoint1.Y + UcsUserPoint2.Y) / 2, 0);
                    Extents3d polyLineRecExt = new Extents3d();
                    // 获取原始矩形的边界  
                    if (polylineRec.Bounds != null) polyLineRecExt = (Extents3d)polylineRec.Bounds;
                    Extents3d newRectBounds = new Extents3d();
                    if (recLRPlus > 0)
                    {
                        // 计算扩大矩形的边界框
                        newRectBounds = new Extents3d(
                            new Point3d(polyLineRecExt.MinPoint.X - recLRPlus, polyLineRecExt.MinPoint.Y - recUDPlus, 0),
                            new Point3d(polyLineRecExt.MaxPoint.X + recLRPlus, polyLineRecExt.MaxPoint.Y + recUDPlus, 0));
                    }
                    else
                    {
                        // 计算扩大矩形的边界框
                        newRectBounds = new Extents3d(
                            new Point3d(polyLineRecExt.MinPoint.X - recLRPlus, polyLineRecExt.MinPoint.Y - recUDPlus, 0),
                            new Point3d(polyLineRecExt.MaxPoint.X + recLRPlus, polyLineRecExt.MaxPoint.Y + recUDPlus, 0));
                    }

                    // 创建扩大矩形
                    Polyline newRect = new Polyline();
                    newRect.LinetypeId = tr.LinetypeTable["DASH"];//为矩形设置线型
                    newRect.Layer = layerName;
                    newRect.ColorIndex = VariableDictionary.layerColorIndex;
                    newRect.AddVertexAt(0, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MinPoint.Y), 0, 0, 0);
                    newRect.AddVertexAt(1, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MaxPoint.Y), 0, 0, 0);
                    newRect.AddVertexAt(2, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MaxPoint.Y), 0, 0, 0);
                    newRect.AddVertexAt(3, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MinPoint.Y), 0, 0, 0);
                    newRect.Closed = true;
                    newRect.ConstantWidth = 30;
                    queue.Enqueue(newRect);//放大后的矩形
                });
                rec.SetOptions(UcsUserPoint1, msg: "\n请指定方形洞口第二点");
                var userPoint2 = Env.Editor.Drag(rec);
                if (userPoint2.Status != PromptStatus.OK) return;
                var polyLineEntityObj = tr.CurrentSpace.AddEntity(rec.Entities);
                Env.Editor.Redraw();
                //MousePointWcsLast为鼠标最后的点击点；
                var UcsUserPoint2 = rec.MousePointWcsLast;
                //计算洞口的中心点
                centerPoint = new Point3d((UcsUserPoint1.X + UcsUserPoint2.X) / 2, (UcsUserPoint1.Y + UcsUserPoint2.Y) / 2, 0);
                //调用标注的方法，给定第一点坐标注与图层名；
                DDimLinear(layerName, Convert.ToInt16(VariableDictionary.layerColorIndex), centerPoint);
                //调用读取天正数据的方法
                tzData();
                Env.Editor.Redraw();
                //if (!VariableDictionary.btnBlockLayer.Contains("给排水"))
                //如果厚度为0时
                if (hvacR3 == "0")
                {
                    int plusX = Convert.ToInt32(Math.Abs(UcsUserPoint1.X - UcsUserPoint2.X)) + Convert.ToInt32(VariableDictionary.textbox_Width) * 2;
                    int plusY = Convert.ToInt32(Math.Abs(UcsUserPoint1.Y - UcsUserPoint2.Y)) + Convert.ToInt32(VariableDictionary.textbox_Height) * 2;
                    PointDim(centerPoint, "洞：" + plusX.ToString(), "x" + plusY.ToString(), " ", layerName, Convert.ToInt16(VariableDictionary.layerColorIndex));

                }
                else
                {
                    int plusX = Convert.ToInt32(hvacR4) + Convert.ToInt32(VariableDictionary.textbox_Width) * 2;
                    int plusY = Convert.ToInt32(hvacR3) + Convert.ToInt32(VariableDictionary.textbox_Height) * 2;
                    PointDim(centerPoint, "洞：" + plusX.ToString(), " x " + plusY.ToString(), "\n距地：" + strHvacStart, layerName, Convert.ToInt16(VariableDictionary.layerColorIndex));
                }

                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n暖通用户指定两点为对角线画方失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");

            }
        }


        /// <summary>
        /// 填充方法
        /// </summary>
        /// <param name="tr">DBTrans</param>
        /// <param name="layerName">图层名</param>
        /// <param name="hatchColorIndex">填充颜色</param>
        /// <param name="hatchPatternScale">填充比例</param>
        /// <param name="patternName">填充图案</param>
        /// <param name="polylineId">填充处边</param>
        public static void autoHatch(DBTrans tr, string layerName, int hatchColorIndex, int hatchPatternScale, string patternName, ObjectId entityObjId)
        {
            try
            {
                // 创建填充图案
                Hatch hatch = new Hatch();
                hatch.SetHatchPattern(HatchPatternType.PreDefined, $"{patternName}"); // 设置填充图案为 patternName
                hatch.PatternScale = hatchPatternScale; // 设置填充图案比例为 200
                hatch.Layer = layerName; // 设置图层
                hatch.ColorIndex = hatchColorIndex;//设置填充图案色号
                //hatch.Associative = true;
                hatch.PatternAngle = 0;//设置填充角度
                hatch.Normal = Vector3d.ZAxis;
                ObjectIdCollection boundaryIds = new ObjectIdCollection(); // 设置填充边界 创建边界集合  
                boundaryIds.Add(entityObjId);
                //hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);// 添加外部环  
                hatch.AppendLoop(HatchLoopTypes.Outermost, boundaryIds);// 添加外部环  
                hatch.EvaluateHatch(true); // 强制计算填充图案
                var hatchId = tr.CurrentSpace.AddEntity(hatch); // 将填充添加到模型空间
                Env.Editor.Redraw();  // 强制刷新视图
                Env.Editor.WriteMessage("\n多边形绘制完成并闭合，填充图案已添加。");
                //return hatchId;
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n创建填充时出错: {ex.Message}");
            }
        }
        public static void autoHatch(DBTrans tr, string layerName, int hatchColorIndex, int hatchPatternScale, string patternName, ObjectId entityObjId, ref ObjectId hatchId)
        {
            try
            {
                // 创建填充图案
                Hatch hatch = new Hatch();
                hatch.SetHatchPattern(HatchPatternType.PreDefined, $"{patternName}"); // 设置填充图案为 ANSI38
                hatch.PatternScale = hatchPatternScale; // 设置填充图案比例为 200
                hatch.Layer = layerName; // 设置图层
                hatch.ColorIndex = hatchColorIndex; //设置填充图案色号
                //hatch.Associative = true;
                hatch.PatternAngle = 0;//设置填充角度
                hatch.Normal = Vector3d.ZAxis;
                ObjectIdCollection boundaryIds = new ObjectIdCollection(); // 设置填充边界 创建边界集合  
                boundaryIds.Add(entityObjId);
                //hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);// 添加外部环
                hatch.AppendLoop(HatchLoopTypes.Outermost, boundaryIds);// 添加外部环 
                hatch.EvaluateHatch(true); // 强制计算填充图案
                hatchId = tr.CurrentSpace.AddEntity(hatch); // 将填充添加到模型空间
                Env.Editor.Redraw();  // 强制刷新视图
                Env.Editor.WriteMessage("\n多边形绘制完成并闭合，填充图案已添加。");
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n创建填充时出错: {ex.Message}");
            }
        }



        /// <summary>
        /// 结构、用户输入长宽后画矩形；
        /// </summary>
        [CommandMethod(nameof(DrawRec))]
        public static void DrawRec()
        {
            #region 方法一
            try
            {
                #region 原始矩形
                double width = Convert.ToDouble(VariableDictionary.textbox_Width); ;
                double height = Convert.ToDouble(VariableDictionary.textbox_Height);
                double recPlus = Convert.ToDouble(VariableDictionary.textbox_RecPlus_Text);// 指定的扩大偏移量
                var layerName = VariableDictionary.btnBlockLayer;
                var layerColor = VariableDictionary.layerColorIndex;
                var leftUp = new Point3d(0, 0, 0);
                var rightDown = new Point3d(leftUp.X + width, leftUp.Y + height, 0);
                var mouseEndPoint = new Point3d(0, 0, 0);
                using var tr = new DBTrans();
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, layerColor);
                var polyLineRec = new Polyline();
                polyLineRec.AddVertexAt(0, new(leftUp.X, leftUp.Y), 0, 0, 0);
                polyLineRec.AddVertexAt(1, new(leftUp.X, rightDown.Y), 0, 0, 0);
                polyLineRec.AddVertexAt(2, new(rightDown.X, rightDown.Y), 0, 0, 0);
                polyLineRec.AddVertexAt(3, new(rightDown.X, leftUp.Y), 0, 0, 0);
                polyLineRec.Closed = true;
                polyLineRec.Layer = layerName;
                polyLineRec.ColorIndex = layerColor;

                #endregion
                #region 计算扩大后的矩形
                //拿到动态绘制矩形的边界
                Extents3d polyLineRecExt = (Extents3d)polyLineRec.Bounds;
                // 计算扩大矩形的边界框
                Extents3d newRectBounds = new Extents3d(
                    new Point3d(polyLineRecExt.MinPoint.X - recPlus, polyLineRecExt.MinPoint.Y - recPlus, 0),
                    new Point3d(polyLineRecExt.MaxPoint.X + recPlus, polyLineRecExt.MaxPoint.Y + recPlus, 0));
                //在生成的矩形上的两个临时点，一个左上，一下右下
                var recLeftUp = new Point3d(newRectBounds.MinPoint.X, newRectBounds.MaxPoint.Y, 0);
                var recRightDowm = new Point3d(newRectBounds.MaxPoint.X, newRectBounds.MinPoint.Y, 0);
                double totalLength = recLeftUp.DistanceTo(recRightDowm);//计算出左上与右下的辅助点的长度；
                double targetLength = totalLength * 1 / 4;//计算出1/4的点
                double targetX = recLeftUp.X + (recRightDowm.X - recLeftUp.X) * 1 / 4;
                double targetY = recLeftUp.Y + (recRightDowm.Y - recLeftUp.Y) * 1 / 4;
                double targetZ = recLeftUp.Z + (recRightDowm.Z - recLeftUp.Z) * 1 / 4;
                Point3d targetPoint = new Point3d(targetX, targetY, targetZ);//找到1/4点的坐标
                // 创建扩大矩形
                Polyline newRect = new Polyline();
                newRect.AddVertexAt(0, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MinPoint.Y), 0, 0, 0);
                newRect.AddVertexAt(1, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MaxPoint.Y), 0, 0, 0);
                newRect.AddVertexAt(2, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MaxPoint.Y), 0, 0, 0);
                newRect.AddVertexAt(3, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MinPoint.Y), 0, 0, 0);
                newRect.Closed = true;
                newRect.Layer = layerName;
                newRect.ColorIndex = layerColor;

                if (layerName.Contains("结构"))
                {
                    // 绘制用来辅助生成填充边界的两条线（或辅助多段线）  
                    // 此处构造的填充边界区域：由扩大矩形的左下角、左上角、右上角，以及计算得到的交合点构成  
                    var hatchBoundary = new Polyline();
                    hatchBoundary.AddVertexAt(0, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MinPoint.Y), 0, 0, 0);
                    hatchBoundary.AddVertexAt(1, new Point2d(newRectBounds.MinPoint.X, newRectBounds.MaxPoint.Y), 0, 0, 0);
                    hatchBoundary.AddVertexAt(2, new Point2d(newRectBounds.MaxPoint.X, newRectBounds.MaxPoint.Y), 0, 0, 0);
                    hatchBoundary.AddVertexAt(3, new Point2d(targetPoint.X, targetPoint.Y), 0, 0, 0);

                    hatchBoundary.Closed = true;
                    hatchBoundary.Layer = layerName;
                    hatchBoundary.ColorIndex = layerColor;
                    #endregion
                    var newRectObjectId = tr.CurrentSpace.AddEntity(newRect);
                    var hatchBoundaryObjectId = tr.CurrentSpace.AddEntity(hatchBoundary);
                    var hatchObectId = new ObjectId();
                    //调用填充方法
                    autoHatch(tr, layerName, layerColor, 50, "DOTS", hatchBoundary.ObjectId, ref hatchObectId);
                    var hatchEntity = tr.GetObject(hatchObectId, OpenMode.ForRead) as Entity;
                    var polylineIds = new ObjectId[2];
                    polylineIds[0] = newRectObjectId;
                    polylineIds[1] = hatchBoundaryObjectId;
                    using var polyLineRecMove = new JigEx((mpw, _) =>
                    {
                        newRect.Move(leftUp, mpw);
                        hatchBoundary.Move(leftUp, mpw);
                        if (hatchEntity != null)
                            hatchEntity.Move(leftUp, mpw);
                        leftUp = mpw;
                    });
                    polyLineRecMove.DatabaseEntityDraw(wd => wd.Geometry.Draw(newRect));
                    polyLineRecMove.SetOptions(msg: "指定插入点：");
                    var endPoint = Env.Editor.Drag(polyLineRecMove);
                    if (endPoint.Status != PromptStatus.OK) tr.Abort();
                    var mpwl = polyLineRecMove.MousePointWcsLast;
                    //计算洞口的中心点
                    mouseEndPoint = new Point3d(mpwl.X + (newRectBounds.MinPoint.X + newRectBounds.MaxPoint.X) / 2, mpwl.Y + (newRectBounds.MinPoint.Y + newRectBounds.MaxPoint.Y) / 2, 0);
                }
                else
                {
                    tr.CurrentSpace.AddEntity(newRect);
                    using var polyLineRecMove = new JigEx((mpw, _) =>
                    {
                        newRect.Move(leftUp, mpw);
                        leftUp = mpw;
                    });
                    polyLineRecMove.DatabaseEntityDraw(wd => wd.Geometry.Draw(newRect));
                    polyLineRecMove.SetOptions(msg: "指定插入点：");
                    var endPoint = Env.Editor.Drag(polyLineRecMove);
                    if (endPoint.Status != PromptStatus.OK) tr.Abort();
                    var mpwl = polyLineRecMove.MousePointWcsLast;
                    //计算洞口的中心点
                    mouseEndPoint = new Point3d(mpwl.X + (newRectBounds.MinPoint.X + newRectBounds.MaxPoint.X) / 2, mpwl.Y + (newRectBounds.MinPoint.Y + newRectBounds.MaxPoint.Y) / 2, 0);
                }
                Env.Editor.Redraw();
                DDimLinear((width + recPlus).ToString("0.00"), (height + recPlus).ToString("0.00"), Convert.ToInt16(layerColor), mouseEndPoint);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构指定数值生成矩形失败: {ex.Message}");
            }
            #endregion
        }

        /// <summary>
        /// 后并多图元返回实体objectid
        /// </summary>
        /// <param name="tr">tr事件</param>
        /// <param name="polylineIds">多个PL线Id</param>
        /// <param name="hatchId">填充Id</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>

        public static ObjectId CombineEntitiesToRegion(DBTrans tr, ObjectId[] polylineIds, ObjectId hatchId)
        {
            // 创建区域
            DBObjectCollection polylineCollection = new DBObjectCollection();
            foreach (ObjectId polylineId in polylineIds)
            {
                // 获取多段线对象
                Entity polyline = tr.GetObject<Entity>(polylineId, OpenMode.ForWrite);
                // 将多段线添加到集合中
                polylineCollection.Add(polyline);
            }

            // 从多段线集合创建区域
            DBObjectCollection regionCollection = Autodesk.AutoCAD.DatabaseServices.Region.CreateFromCurves(polylineCollection);

            // 检查是否成功创建了一个区域
            if (regionCollection.Count != 1)
            {
                throw new InvalidOperationException("无法从多段线创建单个区域");
            }
            // 获取创建的区域
            Autodesk.AutoCAD.DatabaseServices.Region region = regionCollection[0] as Autodesk.AutoCAD.DatabaseServices.Region;
            // 将填充应用到区域
            Hatch hatch = tr.GetObject<Hatch>(hatchId, OpenMode.ForWrite);
            // 设置填充为关联填充
            hatch.Associative = true;
            // 将区域作为填充的外环
            hatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { region.ObjectId });
            // 将区域添加到当前空间
            ObjectId regionId = tr.CurrentSpace.AddEntity(region);
            // 返回区域的 ObjectId
            return regionId;
        }

        /// <summary>
        /// 多点画不规则图形并填充-面着地
        /// </summary>
        [CommandMethod(nameof(NLinePolyline))]
        public static void NLinePolyline()
        {
            try
            {
                var layerName = VariableDictionary.btnBlockLayer;
                using var tr = new DBTrans();

                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, 231);
                var userPoint1 = Env.Editor.GetPoint("\n指定多边形的第一个点：");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                pointS.Add(UcsUserPoint1);
                while (true)
                {
                    using var polyLine = new JigEx((mpw, queue) =>
                    {
                        var UcsUserPoint2 = mpw.Z20();
                        Polyline polyline1 = new Polyline()
                        {
                            Layer = layerName,
                            ColorIndex = 231
                        };
                        for (int i = 0; i < pointS.Count; i++)
                        {
                            polyline1.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        }
                        polyline1.AddVertexAt(pointS.Count, new Point2d(UcsUserPoint2.X, UcsUserPoint2.Y), 0, 0, 0);
                        if (pointS.Count >= 2)
                        {
                            polyline1.Closed = true;
                        }
                        queue.Enqueue(polyline1);
                    });

                    polyLine.SetOptions(UcsUserPoint1, msg: "\n指定多边形的下一个点（右键结束）：");
                    var userPoint2 = Env.Editor.Drag(polyLine);
                    if (userPoint2.Status != PromptStatus.OK) break;
                    var UcsUserPoint2 = polyLine.MousePointWcsLast;
                    pointS.Add(UcsUserPoint2);
                    UcsUserPoint1 = UcsUserPoint2;

                    Env.Editor.Redraw();
                }

                if (pointS.Count >= 3)
                {
                    // 创建最终的多段线  
                    Polyline finalPolyline = new Polyline();
                    for (int i = 0; i < pointS.Count; i++)
                    {
                        finalPolyline.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        finalPolyline.SetStartWidthAt(i, 30);
                        finalPolyline.SetEndWidthAt(i, 30);
                    }
                    finalPolyline.Closed = true;
                    finalPolyline.Layer = layerName;
                    finalPolyline.ColorIndex = 231;

                    // 添加多段线并获取其ID  
                    var polylineId = tr.CurrentSpace.AddEntity(finalPolyline);
                    if (layerName != null)
                        //调用填充方法
                        autoHatch(tr, layerName, 231, 200, "ANSI38", polylineId);

                }
                else
                {
                    Env.Editor.WriteMessage("\n至少需要三个点才能绘制闭合多边形。");
                }
                pointS.Clear();
                if (VariableDictionary.dimString != null)
                    DDimLinear(VariableDictionary.dimString);
                tr.Commit();
                Env.Editor.Redraw();// 强制刷新视图
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n多点画不规则图形并填充失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 多点画不规则图形不填充-框着地
        /// </summary>
        [CommandMethod(nameof(NLinePolyline_Not))]
        public static void NLinePolyline_Not()
        {
            try
            {
                pointS.Clear();
                var layerName = VariableDictionary.btnBlockLayer; // 设置图层名称
                using var tr = new DBTrans();//开启事务
                                             // 检查图层是否存在，如果不存在则创建
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, 231); // 231 是图层颜色索引
                                                       // 获取第一个点
                var userPoint1 = Env.Editor.GetPoint("\n指定多边形的第一个点：");
                if (userPoint1.Status != PromptStatus.OK) return;
                // 将第一个点转换为 UCS 坐标并存储
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                pointS.Add(UcsUserPoint1);
                while (true)
                {
                    // 使用 JigEx 动态绘制多段线
                    using var polyLine = new JigEx((mpw, queue) =>
                    {
                        var UcsUserPoint2 = mpw.Z20(); // 将当前鼠标点转换为 UCS 坐标
                                                       // 创建多段线
                        Polyline polyline1 = new Polyline()
                        {
                            Layer = layerName,  // 设置图层
                            ColorIndex = 231
                        };
                        for (int i = 0; i < pointS.Count; i++)
                        {
                            polyline1.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        }
                        // 添加当前鼠标点作为临时顶点
                        polyline1.AddVertexAt(pointS.Count, new Point2d(UcsUserPoint2.X, UcsUserPoint2.Y), 0, 0, 0);
                        // 如果点数大于 2，则闭合多边形
                        if (pointS.Count >= 2)
                        {
                            polyline1.Closed = true;
                        }
                        queue.Enqueue(polyline1); // 将多段线加入绘制队列
                    });
                    // 设置 JigEx 的起始点和提示信息
                    polyLine.SetOptions(UcsUserPoint1, msg: "\n指定多边形的下一个点（右键结束）：");
                    // 获取用户输入的下一个点
                    var userPoint2 = Env.Editor.Drag(polyLine);
                    if (userPoint2.Status != PromptStatus.OK) break;
                    // 将用户输入的点转换为 UCS 坐标并存储
                    var UcsUserPoint2 = polyLine.MousePointWcsLast;
                    pointS.Add(UcsUserPoint2);
                    // 更新起始点为当前点
                    UcsUserPoint1 = UcsUserPoint2;
                    // 刷新视图
                    Env.Editor.Redraw();
                }
                // 如果点数大于 2，则闭合多边形并添加到模型空间
                if (pointS.Count >= 3)
                {
                    // 创建最终的多段线
                    Polyline finalPolyline = new Polyline();
                    for (int i = 0; i < pointS.Count; i++)
                    {
                        finalPolyline.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        // 设置线宽为 30
                        finalPolyline.SetStartWidthAt(i, 30);
                        finalPolyline.SetEndWidthAt(i, 30);
                    }
                    finalPolyline.Closed = true; // 闭合多边形
                    finalPolyline.Layer = layerName; // 设置图层
                    finalPolyline.ColorIndex = 231;
                    var polylineId = tr.CurrentSpace.AddEntity(finalPolyline);// 将多段线添加到模型空间
                    Env.Editor.Redraw();// 强制刷新视图
                    Env.Editor.WriteMessage("\n多边形绘制完成并闭合，填充图案已添加。");
                }
                else
                {
                    Env.Editor.WriteMessage("\n至少需要三个点才能绘制闭合多边形。");
                }
                pointS.Clear();  // 清空点列表

                while (true)
                {
                    try
                    {
                        // 创建 PromptPointOptions，并允许空输入（即右键取消时不产生错误提示）  
                        PromptPointOptions ppo = new PromptPointOptions("\n请指定框着地内线第一点");
                        ppo.AllowNone = true;

                        // 获取第一个点（左键点击有效，右键取消则返回 None）  
                        var userPointX1 = Env.Editor.GetPoint(ppo);
                        if (userPointX1.Status != PromptStatus.OK)
                        {
                            // 如果状态非 OK，则退出循环  
                            break;
                        }

                        var UcsUserPointX1 = userPointX1.Value.Wcs2Ucs().Z20(); // 转换为 UCS 坐标  

                        using var polylineX = new JigEx((mpw, queue) =>
                        {
                            var UcsUserPointX2 = mpw.Z20();
                            // 定义第一条直线  
                            Polyline polylineX1 = new Polyline();
                            polylineX1.AddVertexAt(0, new Point2d(UcsUserPointX1.X, UcsUserPointX1.Y), 0, 0, 0);
                            polylineX1.AddVertexAt(1, new Point2d(UcsUserPointX2.X, UcsUserPointX2.Y), 0, 0, 0);
                            polylineX1.Closed = false;
                            polylineX1.Layer = layerName; // 设置线条图层  
                            polylineX1.ColorIndex = 231;  // 设置线条颜色  
                            polylineX1.SetStartWidthAt(0, 30);
                            polylineX1.SetEndWidthAt(0, 30);
                            queue.Enqueue(polylineX1);

                            // 定义闭合的方形  
                            Polyline polylineX2 = new Polyline();
                            polylineX2.AddVertexAt(0, new Point2d(UcsUserPointX2.X, UcsUserPointX1.Y), 0, 0, 0);
                            polylineX2.AddVertexAt(1, new Point2d(UcsUserPointX1.X, UcsUserPointX2.Y), 0, 0, 0);
                            polylineX2.Closed = true; // 闭合成方形  
                            polylineX2.Layer = layerName; // 设置线条图层  
                            polylineX2.ColorIndex = 231;
                            polylineX2.SetStartWidthAt(0, 30);
                            polylineX2.SetEndWidthAt(0, 30);
                        });

                        polylineX.SetOptions(UcsUserPointX1, msg: "\n请指定框着地内线第二点");
                        // 获取拖曳过程中的第二个点  
                        var userPointX2 = Env.Editor.Drag(polylineX);
                        if (userPointX2.Status == PromptStatus.OK)
                        {
                            // 如果拖曳成功，则将生成的图元加入当前空间并刷新界面  
                            tr.CurrentSpace.AddEntity(polylineX.Entities);
                            Env.Editor.Redraw();
                        }
                        else
                        {
                            // 如果拖曳过程中右键取消，同样退出循环  
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // 记录错误日志  
                        Env.Editor.WriteMessage($"\n多点画不规则图形不填充-框着地失败！错误信息: {ex.Message}");
                        Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
                    }
                }
                if (VariableDictionary.dimString != null)
                    DDimLinear(VariableDictionary.dimString);
                tr.Commit(); // 提交事务
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n多点画不规则图形不填充-框着地失败: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 结构、画多边形-水平荷载
        /// </summary>
        [CommandMethod(nameof(NLinePolyline_N))]
        public static void NLinePolyline_N()
        {
            try
            {
                pointS.Clear();
                var layerName = VariableDictionary.btnBlockLayer; // 设置图层名称
                using var tr = new DBTrans();//开启事务
                                             // 检查图层是否存在，如果不存在则创建
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, 231); // 231 是图层颜色索引
                                                       // 获取第一个点
                var userPoint1 = Env.Editor.GetPoint("\n指定多边形的第一个点：");
                if (userPoint1.Status != PromptStatus.OK) return;
                // 将第一个点转换为 UCS 坐标并存储
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                pointS.Add(UcsUserPoint1);
                while (true)
                {
                    // 使用 JigEx 动态绘制多段线
                    using var polyLine = new JigEx((mpw, queue) =>
                    {
                        var UcsUserPoint2 = mpw.Z20(); // 将当前鼠标点转换为 UCS 坐标
                                                       // 创建多段线
                        Polyline polyline1 = new Polyline()
                        {
                            Layer = layerName,  // 设置图层
                            ColorIndex = 231
                        };
                        for (int i = 0; i < pointS.Count; i++)
                        {
                            polyline1.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        }
                        // 添加当前鼠标点作为临时顶点
                        polyline1.AddVertexAt(pointS.Count, new Point2d(UcsUserPoint2.X, UcsUserPoint2.Y), 0, 0, 0);

                        if (pointS.Count >= 2)// 如果点数大于 2，则闭合多边形
                        {
                            polyline1.Closed = true;
                        }
                        queue.Enqueue(polyline1); // 将多段线加入绘制队列
                    });
                    // 设置 JigEx 的起始点和提示信息
                    polyLine.SetOptions(UcsUserPoint1, msg: "\n指定多边形的下一个点（右键结束）");
                    var userPoint2 = Env.Editor.Drag(polyLine); // 获取用户输入的下一个点
                    if (userPoint2.Status != PromptStatus.OK) break;
                    var UcsUserPoint2 = polyLine.MousePointWcsLast;// 将用户输入的点转换为 UCS 坐标并存储
                    pointS.Add(UcsUserPoint2);
                    UcsUserPoint1 = UcsUserPoint2;// 更新起始点为当前点
                    Env.Editor.Redraw();// 刷新视图
                }
                // 如果点数大于 2，则闭合多边形并添加到模型空间
                if (pointS.Count >= 3)
                {
                    // 创建最终的多段线
                    Polyline finalPolyline = new Polyline();
                    for (int i = 0; i < pointS.Count; i++)
                    {
                        finalPolyline.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        // 设置线宽为 30
                        finalPolyline.SetStartWidthAt(i, 30);
                        finalPolyline.SetEndWidthAt(i, 30);
                    }
                    finalPolyline.Closed = true; // 闭合多边形
                    finalPolyline.Layer = layerName; // 设置图层
                    finalPolyline.ColorIndex = 231;
                    var polylineId = tr.CurrentSpace.AddEntity(finalPolyline);// 将多段线添加到模型空间
                    if (layerName != null)
                        DrawArrows(finalPolyline, layerName);
                    Env.Editor.Redraw();// 强制刷新视图
                    Env.Editor.WriteMessage("\n多边形绘制完成并闭合，填充图案已添加。");
                    pointS.Clear();  // 清空点列表

                }
                else
                {
                    Env.Editor.WriteMessage("\n至少需要三个点才能绘制闭合多边形。");
                }
                if (VariableDictionary.dimString != null)
                    DDimLinear(VariableDictionary.dimString);
                tr.Commit();
                Env.Editor.Redraw();// 强制刷新视图

            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构、画多边形失败: {ex.Message}");
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 生成箭头
        /// </summary>
        /// <param name="existingPolygon">Polyline多边形</param>
        /// <param name="layerName"></param>
        public static void DrawArrows(Polyline existingPolygon, string layerName)
        {
            try
            {
                using var tr = new DBTrans();
                // 获取多边形的边界  
                Extents3d bounds = existingPolygon.GeometricExtents;
                double width = bounds.MaxPoint.X - bounds.MinPoint.X;
                double height = bounds.MaxPoint.Y - bounds.MinPoint.Y;
                // 计算箭头的基本尺寸  
                double arrowWidth = width * 0.8; // 箭头总长度占据多边形宽度的80%  
                double arrowHeight = height * 0.3; // 每个箭头高度占据多边形高度的30%  
                double arrowHeadWidth = width * 0.2; // 箭头底坐宽度  
                double margin = width * 0.1; // 左右边距  
                                             // 计算两个箭头的垂直位置  
                double firstArrowY = bounds.MinPoint.Y + height * 0.35; // 第一个箭头在25%高度位置  
                double secondArrowY = bounds.MinPoint.Y + height * 0.65; // 第二个箭头在75%高度位置 
                                                                         // 创建第一个箭头（向右）  
                Polyline arrow1 = new Polyline();
                double x1 = bounds.MinPoint.X + margin;
                arrow1.AddVertexAt(0, new Point2d(x1, firstArrowY), 0, 0, 0);  // 起点  
                arrow1.AddVertexAt(1, new Point2d(x1 + arrowWidth - arrowHeadWidth, firstArrowY), 0, 0, 0);  // 线条终点  
                arrow1.AddVertexAt(2, new Point2d(x1 + arrowWidth - arrowHeadWidth, firstArrowY - arrowHeight / 2), 0, 0, 0);  // 箭头底部  
                arrow1.AddVertexAt(3, new Point2d(x1 + arrowWidth, firstArrowY + arrowHeight / 8), 0, 0, 0); // 箭头尖端  
                arrow1.AddVertexAt(4, new Point2d(x1 + arrowWidth - arrowHeadWidth, firstArrowY + arrowHeight / 1.4), 0, 0, 0);  // 箭头顶部  
                arrow1.AddVertexAt(5, new Point2d(x1 + arrowWidth - arrowHeadWidth, firstArrowY + arrowHeight / 4), 0, 0, 0);  // 回到线条  
                arrow1.AddVertexAt(6, new Point2d(x1, firstArrowY + arrowHeight / 4), 0, 0, 0);  // 线条起点上边  
                arrow1.Closed = true;
                arrow1.Layer = layerName;
                arrow1.ColorIndex = 231;
                tr.CurrentSpace.AddEntity(arrow1);
                // 创建第二个箭头（向左）  
                Polyline arrow2 = new Polyline();
                double x2 = bounds.MaxPoint.X - margin;
                arrow2.AddVertexAt(0, new Point2d(x2, secondArrowY), 0, 0, 0);  // 起点  
                arrow2.AddVertexAt(1, new Point2d(x2 - arrowWidth + arrowHeadWidth, secondArrowY), 0, 0, 0);  // 线条终点  
                arrow2.AddVertexAt(2, new Point2d(x2 - arrowWidth + arrowHeadWidth, secondArrowY - arrowHeight / 2), 0, 0, 0);  // 箭头底部  
                arrow2.AddVertexAt(3, new Point2d(x2 - arrowWidth, secondArrowY + arrowHeight / 8), 0, 0, 0); // 箭头尖端  
                arrow2.AddVertexAt(4, new Point2d(x2 - arrowWidth + arrowHeadWidth, secondArrowY + arrowHeight / 1.4), 0, 0, 0);  // 箭头顶部  
                arrow2.AddVertexAt(5, new Point2d(x2 - arrowWidth + arrowHeadWidth, secondArrowY + arrowHeight / 4), 0, 0, 0);  // 回到线条  
                arrow2.AddVertexAt(6, new Point2d(x2, secondArrowY + arrowHeight / 4), 0, 0, 0);  // 线条起点上边  
                arrow2.Closed = true;
                arrow2.Layer = layerName;
                arrow2.ColorIndex = 231;
                tr.CurrentSpace.AddEntity(arrow2);
                // 为箭头添加填充  
                using (Hatch hatch1 = new Hatch())
                {
                    //hatch1.Pattern = HatchPattern.PreDefined;  // 设置使用预定义的填充图案  
                    //hatch1.PatternType = HatchPatternType.PreDefined; // 设置填充类型为预定义类型 
                    hatch1.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");// 设置填充图案为 SOLID
                    hatch1.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection { arrow1.ObjectId });
                    hatch1.PatternScale = 100; // 设置填充图案比例为 100
                    hatch1.Layer = layerName; // 设置图层
                    hatch1.ColorIndex = 231;//设置颜色
                    hatch1.EvaluateHatch(true); // 强制计算填充图案
                    tr.CurrentSpace.AddEntity(hatch1);//写入当前空间

                }
                using (Hatch hatch2 = new Hatch())
                {
                    //hatch2.Pattern = HatchPattern.PreDefined;  // 设置使用预定义的填充图案  
                    //hatch2.PatternType = HatchPatternType.PreDefined; // 设置填充类型为预定义类型 
                    hatch2.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");// 设置填充图案为 SOLID
                    hatch2.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection { arrow2.ObjectId });
                    hatch2.PatternScale = 100; // 设置填充图案比例为 100
                    hatch2.Layer = layerName; // 设置图层
                    hatch2.ColorIndex = 231;//设置颜色
                    hatch2.EvaluateHatch(true); // 强制计算填充图案
                    tr.CurrentSpace.AddEntity(hatch2);//写入当前空间

                }
                tr.Commit();// 提交事务
                Env.Editor.Redraw();  // 强制刷新视图
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n结构生成箭头失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 多边形PL线
        /// </summary>
        [CommandMethod(nameof(NLinePolyline_K))]
        public static void NLinePolyline_K()
        {
            try
            {
                pointS.Clear();
                var layerName = VariableDictionary.btnBlockLayer; // 设置图层名称
                using var tr = new DBTrans();

                // 检查图层是否存在，如果不存在则创建
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, 231); // 231 是图层颜色索引

                // 获取第一个点
                var userPoint1 = Env.Editor.GetPoint("\n指定多边形的第一个点：");
                if (userPoint1.Status != PromptStatus.OK) return;

                // 将第一个点转换为 UCS 坐标并存储
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                pointS.Add(UcsUserPoint1);
                while (true)
                {
                    // 使用 JigEx 动态绘制多段线
                    using var polyLine = new JigEx((mpw, queue) =>
                    {
                        var UcsUserPoint2 = mpw.Z20(); // 将当前鼠标点转换为 UCS 坐标
                        Polyline polyline1 = new Polyline() // 创建多段线
                        {
                            Layer = layerName,  // 设置图层
                            ColorIndex = 231
                        };
                        for (int i = 0; i < pointS.Count; i++)
                        {
                            polyline1.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        }
                        // 添加当前鼠标点作为临时顶点
                        polyline1.AddVertexAt(pointS.Count, new Point2d(UcsUserPoint2.X, UcsUserPoint2.Y), 0, 0, 0);
                        if (pointS.Count >= 2) // 如果点数大于 2，则闭合多边形
                        {
                            polyline1.Closed = true;
                        }
                        queue.Enqueue(polyline1); // 将多段线加入绘制队列
                    });
                    polyLine.SetOptions(UcsUserPoint1, msg: "\n指定多边形的下一个点（右键结束）：");  // 设置 JigEx 的起始点和提示信息
                    var userPoint2 = Env.Editor.Drag(polyLine);  // 获取用户输入的下一个点
                    if (userPoint2.Status != PromptStatus.OK) break;
                    var UcsUserPoint2 = polyLine.MousePointWcsLast; // 将用户输入的点转换为 UCS 坐标并存储
                    pointS.Add(UcsUserPoint2);
                    UcsUserPoint1 = UcsUserPoint2; // 更新起始点为当前点
                    Env.Editor.Redraw();// 刷新视图
                }

                if (pointS.Count >= 3)// 如果点数大于 2，则闭合多边形并添加到模型空间
                {
                    Polyline finalPolyline = new Polyline();  // 创建最终的多段线
                    for (int i = 0; i < pointS.Count; i++)
                    {
                        finalPolyline.AddVertexAt(i, new Point2d(pointS[i].X, pointS[i].Y), 0, 0, 0);
                        // 设置线宽为 30
                        finalPolyline.SetStartWidthAt(i, 30);
                        finalPolyline.SetEndWidthAt(i, 30);
                    }
                    finalPolyline.Closed = true; // 闭合多边形
                    finalPolyline.Layer = layerName; // 设置图层
                    finalPolyline.ColorIndex = 231;
                    var polylineId = tr.CurrentSpace.AddEntity(finalPolyline); // 将多段线添加到模型空间
                    Extents3d polygonBounds = finalPolyline.Bounds.Value; // 计算多边形的边界
                    double arrowHeight = 50; // 定义箭头的高度
                    double polygonWidth = polygonBounds.MaxPoint.X - polygonBounds.MinPoint.X; // 多边形的宽度
                                                                                               // 计算箭头的中心点
                    double centerYTop = polygonBounds.MaxPoint.Y - arrowHeight / 2; // 上箭头的中心点 Y 坐标
                    double centerYBottom = polygonBounds.MinPoint.Y + arrowHeight / 2; // 下箭头的中心点 Y 坐标
                    double centerX = (polygonBounds.MinPoint.X + polygonBounds.MaxPoint.X) / 2; // 箭头的中心点 X 坐标
                    if (layerName != null)
                    {
                        // 绘制上箭头（向左）
                        DrawArrow(tr, new Point3d(centerX, centerYTop, 0), -90, polygonWidth, arrowHeight, layerName);
                        // 绘制下箭头（向右）
                        DrawArrow(tr, new Point3d(centerX, centerYBottom, 0), 90, polygonWidth, arrowHeight, layerName);
                    }

                    if (VariableDictionary.dimString != null)
                        DDimLinear(VariableDictionary.dimString);
                    Env.Editor.Redraw(); // 强制刷新视图
                    Env.Editor.WriteMessage("\n多边形绘制完成并闭合，箭头已添加。");
                }
                else
                {
                    Env.Editor.WriteMessage("\n至少需要三个点才能绘制闭合多边形。");
                }
                pointS.Clear(); // 清空点列表
                                //if (VariableDictionary.dimString != null)
                                //    DDimLinear(VariableDictionary.dimString);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {

                // 记录错误日志  
                Env.Editor.WriteMessage($"\n结构生成箭头失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 绘制箭头
        /// </summary>
        /// <param name="tr">事务</param>
        /// <param name="center">中心</param>
        /// <param name="angle">角度</param>
        /// <param name="arrowLength">箭头长</param>
        /// <param name="arrowHeight">箭头高</param>
        /// <param name="layerName">图层名</param>
        private static void DrawArrow(DBTrans tr, Point3d center, double angle, double arrowLength, double arrowHeight, string layerName)
        {
            // 计算箭头的三个顶点
            Point3d tip = center.PolarPoint(angle, arrowLength / 2); // 箭头尖端
            Point3d left = center.PolarPoint(angle - 90, arrowHeight / 2); // 左端点
            Point3d right = center.PolarPoint(angle + 90, arrowHeight / 2); // 右端点

            // 创建箭头的多段线
            Polyline arrow = new Polyline();
            arrow.AddVertexAt(0, new Point2d(tip.X, tip.Y), 0, 0, 0);
            arrow.AddVertexAt(1, new Point2d(left.X, left.Y), 0, 0, 0);
            arrow.AddVertexAt(2, new Point2d(right.X, right.Y), 0, 0, 0);
            arrow.Closed = true; // 闭合箭头
            arrow.Layer = layerName; // 设置图层
            arrow.ColorIndex = 231;
            tr.CurrentSpace.AddEntity(arrow);// 将箭头添加到模型空间
        }



        #endregion

        #region 接口、实体、天正数据、标注等

        /// <summary>
        /// 获取天正数据宽度参数
        /// </summary>
        public static string hvacR4 = "0";
        /// <summary>
        /// 获取天正数据高度参数
        /// </summary>
        public static string hvacR3 = "0";
        /// <summary>
        /// 获取天正数据距地面参数
        /// </summary>
        public static string strHvacStart = "0";

        /// <summary>
        /// 给体id返回实体对像
        /// </summary>
        /// <param name="entityId">输入要返回实体对像的Object</param>
        /// <returns>返回实体对像</returns>
        public static Entity GetEntity(ObjectId entityId)
        {
            Entity? entity = null;
            try
            {
                using (Transaction tr = entityId.Database.TransactionManager.StartTransaction())
                {
                    entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                    tr.Commit();
                }

            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("给体id返回实体对像失败！");
                Env.Editor.WriteMessage(ex.Message);
            }
            return entity;
        }

        /// <summary>
        /// 获取引用实体名称
        /// </summary>
        /// <param name="tr">开启事务</param>
        /// <param name="objectId">objectId</param>
        /// <returns></returns>
        public static string getXrefName(DBTrans tr, ObjectId objectId)
        {
            string? xrefName;
            // 第三步：打开选中的外部参照
            BlockReference xrefEntity = tr.GetObject(objectId, OpenMode.ForRead) as BlockReference;

            if (xrefEntity == null)
            {
                Env.Editor.WriteMessage("\n错误：选中的对象不是外部参照。");
                return xrefName = "";
            }

            // 第四步：获取外部参照的块表记录
            BlockTableRecord btr = tr.GetObject(xrefEntity.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

            if (btr == null)
            {
                Env.Editor.WriteMessage("\n错误：无法获取块表记录。");
                return xrefName = "";
            }
            return xrefName = btr.Name;
        }

        /// <summary>
        /// 获取天正数据
        /// </summary>
        [CommandMethod(nameof(tzData))]
        public static void tzData()
        {
            var sEper = Env.Editor.GetEntity("\n选择要标注的实体");
            if (sEper.Status != PromptStatus.OK)
                return;
            using var Tr = new DBTrans();
            try
            {
                //判断是不是曲线实体
                if (Tr.GetObject(sEper.ObjectId) is not Curve sEperObi)
                    return;
                //获取曲线实体的AcadObject对象
                var aCadSeperOb = sEperObi.AcadObject;
                if (aCadSeperOb != null)
                {
                    //获取到宽
                    hvacR4 = AddMenus.GetProperty(aCadSeperOb, "Hvac_R4").ToString();
                    //获取到高（厚）
                    hvacR3 = AddMenus.GetProperty(aCadSeperOb, "Hvac_R3").ToString();
                    //获取距地值，返回的是object[]数组；
                    object HvacStart = AddMenus.GetProperty(aCadSeperOb, "Hvac_Start");
                    //var havcR4 = Convert.ToString(aCadSeperOb.GetType().InvokeMember("Hvac_R4", BindingFlags.GetProperty, null, aCadSeperOb, null));
                    double[] doubles = new double[3] { 0, 0, 0 };
                    doubles = (double[])HvacStart;
                    strHvacStart = Convert.ToString(doubles[2]);
                    Env.Editor.WriteMessage("\nhvacR4:" + hvacR4);
                    Env.Editor.WriteMessage("\nhvacR3:" + hvacR3);
                    Env.Editor.WriteMessage("\nhvacStart:" + strHvacStart);
                }
            }
            catch
            {
                //Env.Editor.WriteMessage("您选定的图无不为天正图无，不能读出宽厚参数！");//在下面的历史记录框里显示一样文字
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("您选定的图元没有天正元素，不能读出宽厚等参数！");//弹出一个带有声音的消息框；
                return;
            }
        }

        /// <summary>
        /// 设置文字样式与设置图层信息
        /// </summary>
        /// <param name="VariableDictionary.btnFileName">按键名称</param>
        /// <param name="VariableDictionary.layerColorIndex">图层颜色</param>
        /// <param name="tJText">查询与创建条件图文字样式</param>
        /// <param name="mt">字体样式</param>
        /// <param name="mld">标注样式</param>
        /// 
        public static void TextStyleAndLayerInfo(string layerName, Int16 textColor, string tJText, ref MText mt, ref MLeader mld)
        {
            using var tr = new DBTrans();

            #region 创建文字属性

            if (!tr.LayerTable.Has(layerName))
                tr.LayerTable.Add(layerName, textColor);
            if (!tr.TextStyleTable.Has(tJText))
            {
                //ifox自带的文字样式添加
                tr.TextStyleTable.Add(tJText, ttr =>
                {
                    ttr.FileName = "gbenor.shx";// 字体文件
                    ttr.BigFontFileName = "gbcbig.shx";// 大字体文件
                    ttr.XScale = 0.8; // 字体比例
                });
            }
            else
            {
                tr.TextStyleTable.Change("tJText", ttr =>
                {
                    ttr.FileName = "gbenor.shx";// 字体文件
                    ttr.BigFontFileName = "gbcbig.shx";// 大字体文件
                    ttr.XScale = 0.8; // 字体比例

                });
            }
            mt = new MText()
            {
                // 设置标注文字居中对齐 
                Attachment = AttachmentPoint.MiddleCenter,
                //文字宽度因子
                Width = 0.8,
                //文字高度
                Height = 300,
            };

            mld = new MLeader
            {
                //设置多重引线的图层
                Layer = layerName,
                //设置多重引线的标注文字下是不是有引线；
                TextAttachmentType = TextAttachmentType.AttachmentBottomOfTopLine,
                //内容类型
                ContentType = ContentType.MTextContent,
                // 例如索引3通常代表绿色
                LeaderLineColor = Color.FromColorIndex(ColorMethod.ByAci, textColor),
                // 使用默认线型
                LeaderLineTypeId = Env.Database.LinetypeTableId,
                // 设置引线线宽
                //LeaderLineWeight = LineWeight.LineWeight030,
                // 设置多重引线的比例 
                //Scale = 1.0,  
                //赋值标注文字样式
                MText = mt,
                //设置文字样式
                TextStyleId = tr.TextStyleTable["tJText"],
                //设置多重引线标注文字的高度
                TextHeight = 300,
                //设置箭头大小为 200  
                ArrowSize = 200
            };

            #endregion
        }

        /// <summary>
        /// 设置文字样式与设置图层信息
        /// </summary>
        /// <param name="VariableDictionary.btnFileName">按键名称</param>
        /// <param name="VariableDictionary.layerColorIndex">图层颜色</param>
        /// <param name="tJText">查询与创建条件图文字样式</param>
        /// <param name="mt">字体样式</param>
        /// <param name="mld">标注样式</param>
        /// 
        public static void TextStyleAndLayerInfo(string layerName, Int16 textColor, string tJText)
        {
            using var tr = new DBTrans();

            #region 创建文字属性

            if (!tr.LayerTable.Has(layerName))
                tr.LayerTable.Add(layerName, textColor);
            if (!tr.TextStyleTable.Has(tJText))
            {
                //ifox自带的文字样式添加
                tr.TextStyleTable.Add(tJText, ttr =>
                {
                    ttr.FileName = "gbenor.shx";// 字体文件
                    ttr.BigFontFileName = "gbcbig.shx";// 大字体文件
                    ttr.XScale = 0.8; // 字体比例

                });
            }
            else
            {
                tr.TextStyleTable.Change("tJText", ttr =>
                {
                    ttr.FileName = "gbenor.shx";// 字体文件
                    ttr.BigFontFileName = "gbcbig.shx";// 大字体文件
                    ttr.XScale = 0.8; // 字体比例
                });
            }

            #endregion
        }

        /// <summary>
        /// com接口(设置)对象属性值，类似VisualLisp的vlax-put-property函数
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="key">属性名称</param>
        /// <param name="value">属性值</param>
        /// <summary>
        /// 给出一点与图层名，做标注
        /// </summary>
        /// <param name="pt1">标注的另一点</param>
        /// <param name="layer">图层名</param>
        [CommandMethod("DDimLinearP")]
        public static void DDimLinearP()
        {
            try
            {
                using var tr = new DBTrans();
                #region 创建文字属性
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, Convert.ToInt16(VariableDictionary.layerColorIndex), "tJText");

                var mld = new MLeader
                {
                    Layer = VariableDictionary.btnBlockLayer,//设置多重引线的图层
                    //TextAttachmentType = TextAttachmentType.AttachmentBottomOfTopLine,//设置多重引线的标注文字下是不是有引线；
                    TextAttachmentType = TextAttachmentType.AttachmentBottomLine,//设置多重引线的标注文字下是不是有引线；
                    ContentType = ContentType.MTextContent,//内容类型
                    ColorIndex = Convert.ToInt32(VariableDictionary.layerColorIndex),
                    // 例如索引3通常代表绿色
                    LeaderLineColor = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(VariableDictionary.layerColorIndex)),
                    //LeaderLineTypeId = Env.Database.LinetypeTableId, // 使用默认线型
                    //LeaderLineWeight = LineWeight.LineWeight030, // 设置引线线宽
                    //Scale = 1.0,// 设置多重引线的比例   
                };
                var userPoint1 = Env.Editor.GetPoint("\n请指定标注第一点");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                //标注样式
                MText mt = new MText();
                //标注文字获取
                mt.Contents = VariableDictionary.btnFileName;
                mt.Attachment = AttachmentPoint.MiddleCenter; // 设置标注文字居中对齐  
                mld.MText = mt;//赋值标注文字样式
                mld.TextHeight = 300;//设置多重引线标注文字的高度
                mld.TextStyleId = tr.TextStyleTable["tJText"];
                // 设置引线颜色为 7  
                mld.ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                // 设置箭头大小为 300  
                mld.ArrowSize = 250;
                // 添加引线和引线段  
                int ldNum = mld.AddLeader();
                int lnNum = mld.AddLeaderLine(ldNum);
                mld.AddFirstVertex(lnNum, UcsUserPoint1);  // 引线起始点（UCS 坐标）  
                using var mleaderjig = new JigEx((mpw, Queue) =>
                {
                    var pt2Ucs = mpw.Z20();


                    mld.TextLocation = pt2Ucs;  // 标注文字显示位置  
                });
                var UcsUserPoint2 = mleaderjig.MousePointWcsLast;
                mld.AddLastVertex(lnNum, UcsUserPoint2);// 引线结束点（UCS 坐标）  

                mleaderjig.DatabaseEntityDraw(wb => wb.Geometry.Draw(mld));
                mleaderjig.SetOptions(UcsUserPoint1, msg: "点选标注第二点");
                var pt2 = mleaderjig.Drag();
                if (pt2.Status != PromptStatus.OK) return;

                tr.CurrentSpace.AddEntity(mld);
                #endregion

                tr.Commit();
                Env.Editor.Redraw();//重新刷新
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n做标注失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 创建标注线
        /// </summary>
        /// <param name="DimString">标注文字</param>
        /// <param name="VariableDictionary.layerColorIndex">图层颜色</param>
        /// <param name="layerName">图层名</param>
        /// <param name="userPoint">指定坐标</param>
        public static void DDimLinear(string DimString, Int16 layerColorIndex, string layerName, Point3d userPoint)
        {
            try
            {
                using var tr = new DBTrans();
                #region 创建文字属性

                TextStyleAndLayerInfo(layerName, Convert.ToInt16(VariableDictionary.layerColorIndex), "tJText");
                var mld = new MLeader
                {
                    Layer = layerName, // 设置多重引线的图层  
                    ContentType = ContentType.MTextContent, // 内容类型  
                    ColorIndex = Convert.ToInt32(VariableDictionary.layerColorIndex),
                    LeaderLineColor = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(VariableDictionary.layerColorIndex)), // 设置引线颜色  
                };

                //标注样式
                MText mt = new MText();
                //标注文字获取
                mt.Contents = "设备名称" + "\n" + $"{DimString}" + " ";
                mt.Attachment = AttachmentPoint.MiddleCenter; // 设置标注文字居中对齐  

                // 添加引线和引线段  
                int ldNum = mld.AddLeader();
                int lnNum = mld.AddLeaderLine(ldNum);
                mld.AddFirstVertex(lnNum, userPoint);  // 引线起始点（UCS 坐标）
                var mpwUcs = new Point3d(0, 0, 0);
                using var mleaderjig = new JigEx((mpw, _) =>
                {
                    // 引线结束点（UCS 坐标）
                    mpwUcs = mpw.Z20();
                    // 标注文字显示位置        
                    mld.TextLocation = mpwUcs;
                });
                var UcsUserPoint2 = mleaderjig.MousePointWcsLast;
                mld.AddLastVertex(lnNum, UcsUserPoint2);
                mld.MText = mt;
                mld.TextHeight = 300;//设置多重引线标注文字的高度
                mld.TextStyleId = tr.TextStyleTable["tJText"];
                // 设置引线颜色为 7  
                mld.ColorIndex = VariableDictionary.layerColorIndex;
                // 设置箭头大小为 300  
                mld.ArrowSize = 100;
                mld.TextAttachmentType = TextAttachmentType.AttachmentBottomOfTopLine;
                mleaderjig.DatabaseEntityDraw(wb => wb.Geometry.Draw(mld));
                mleaderjig.SetOptions(userPoint, msg: "\n点选标注第二点");
                var userPoint2 = Env.Editor.Drag(mleaderjig);
                if (userPoint2.Status != PromptStatus.OK) return;
                tr.CurrentSpace.AddEntity(mld);

                #endregion

                tr.Commit();
                Env.Editor.Redraw();//重新刷新
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n做标注失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>"IsFrozenLayer "
        /// 点线标注
        /// </summary>
        /// <param name="dimString">标注文字内容</param>
        public static void DDimLinear(string dimString)
        {
            try
            {
                Int16 textColor = Convert.ToInt16(VariableDictionary.layerColorIndex);
                using var tr = new DBTrans();
                if (VariableDictionary.btnFileName == "TJ(结构专业JG)")
                    textColor = 3;
                MText mt = new MText();
                mt.ColorIndex = textColor;
                var mld = new MLeader();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, textColor, "tJText", ref mt, ref mld);
                if (VariableDictionary.buttonText.Contains("框着地"))
                {
                    dimString = "总重:" + dimString + "kg" + "\n框着地";
                }
                else if (VariableDictionary.buttonText.Contains("面着地"))
                {
                    dimString = "总重:" + dimString + "kg" + "\n面着地";
                }
                else if (VariableDictionary.buttonText.Contains("点受力"))
                {
                    dimString = "总重:" + dimString + "kg" + "\n点受力";
                }
                else if (VariableDictionary.buttonText.Contains("水平荷载"))
                {
                    dimString = "总重:" + dimString + "kg" + "\n水平荷载";
                }
                var userPoint1 = Env.Editor.GetPoint("\n请指定标注第一点");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                mt.Contents = dimString;
                mt.ColorIndex = textColor;
                // 添加引线和引线段  
                int ldNum = mld.AddLeader();
                int lnNum = mld.AddLeaderLine(ldNum);
                mld.AddFirstVertex(lnNum, UcsUserPoint1);  // 引线起始点（UCS 坐标）
                var mpwUcs = new Point3d(0, 0, 0);
                using var mleaderjig = new JigEx((mpw, _) =>
                {
                    // 引线结束点（UCS 坐标）
                    mpwUcs = mpw.Z20();
                    // 标注文字显示位置        
                    mld.TextLocation = mpwUcs;
                });
                var UcsUserPoint2 = mleaderjig.MousePointWcsLast;
                mld.AddLastVertex(lnNum, UcsUserPoint2);
                mld.MText = mt;
                mld.TextHeight = 300;
                mld.TextStyleId = tr.TextStyleTable["tJText"];
                mleaderjig.DatabaseEntityDraw(wb => wb.Geometry.Draw(mld));
                mleaderjig.SetOptions(UcsUserPoint1, msg: "\n点选标注第二点");
                var userPoint2 = Env.Editor.Drag(mleaderjig);
                if (userPoint2.Status != PromptStatus.OK) return;
                tr.CurrentSpace.AddEntity(mld);
                tr.Commit();
                Env.Editor.Redraw();//重新刷新
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n做标注失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 线性标注
        /// </summary>
        /// <param name="dimString">标注文字1\宽</param>
        /// <param name="dimString2">标注文字2\高\受力点数</param>
        public static void DDimLinear(string dimString, string dimString2, Int16 layerColorIndex)
        {
            try
            {
                string buttonText = VariableDictionary.buttonText;
                Int16 textColorIntex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                MText mt = new MText();
                var mld = new MLeader();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, textColorIntex, "tJText", ref mt, ref mld);
                using var tr = new DBTrans();
                if (buttonText.Contains("受力点"))
                {
                    dimString = "总重:" + dimString + "kg" + "\n" + dimString2 + " 点受力";
                }
                else if (buttonText.Contains("矩形开洞"))
                {
                    dimString = "矩形洞口\n" + dimString + "x" + dimString2;
                }
                else if (buttonText.Contains("圆形开洞"))
                {
                    dimString = "圆形洞口\n" + "直径:" + dimString;
                }
                else if (buttonText.Contains("JZTJ"))
                {
                    dimString = "排水沟\n" + "宽:" + dimString2 + "m，" + "深:" + dimString + "m";
                    textColorIntex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                }
                var userPoint1 = Env.Editor.GetPoint("\n请指定标注第一点");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                mt.Contents = dimString;
                mt.ColorIndex = textColorIntex;

                // 添加引线和引线段  
                int ldNum = mld.AddLeader();
                int lnNum = mld.AddLeaderLine(ldNum);
                mld.AddFirstVertex(lnNum, UcsUserPoint1);  // 引线起始点（UCS 坐标）
                var mpwUcs = new Point3d(0, 0, 0);
                using var mleaderjig = new JigEx((mpw, _) =>
                {
                    // 引线结束点（UCS 坐标）
                    mpwUcs = mpw.Z20();
                    // 标注文字显示位置        
                    mld.TextLocation = mpwUcs;
                });
                var UcsUserPoint2 = mleaderjig.MousePointWcsLast;
                mld.AddLastVertex(lnNum, UcsUserPoint2);
                mld.MText = mt;
                mld.TextHeight = 300;
                mld.TextStyleId = tr.TextStyleTable["tJText"];
                mleaderjig.DatabaseEntityDraw(wb => wb.Geometry.Draw(mld));
                mleaderjig.SetOptions(UcsUserPoint1, msg: "\n点选标注第二点");
                var userPoint2 = Env.Editor.Drag(mleaderjig);
                if (userPoint2.Status != PromptStatus.OK) return;
                tr.CurrentSpace.AddEntity(mld);
                tr.Commit();
                Env.Editor.Redraw();//重新刷新
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n做标注失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 线性标注
        /// </summary>
        /// <param name="dimString">标注文字1</param>
        /// <param name="dimString2">标注文字2</param>
        /// <param name="VariableDictionary.layerColorIndex">图层颜色</param>
        /// <param name="point3D">指定坐标点</param>
        public static void DDimLinear(string dimString, string dimString2, Int16 layerColorIndex, Point3d point3D)
        {
            try
            {
                string buttonText = VariableDictionary.buttonText;
                Int16 textColorIntex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                MText mt = new MText();
                var mld = new MLeader();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, textColorIntex, "tJText", ref mt, ref mld);
                using var tr = new DBTrans();
                if (buttonText.Contains("受力点"))
                {
                    dimString = "总重:" + dimString + "kg" + "\n" + dimString2 + " 点受力";
                }
                else if (buttonText.Contains("矩形开洞"))
                {
                    dimString = "矩形洞口\n" + dimString + "x" + dimString2;
                }
                else if (buttonText.Contains("圆形开洞"))
                {
                    dimString = "圆形洞口\n" + "直径:" + dimString;
                }
                else if (buttonText.Contains("JZTJ"))
                {
                    dimString = "排水沟\n" + "宽:" + dimString2 + "m，" + "深:" + dimString + "m";
                    textColorIntex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                }
                //var userPoint1 = Env.Editor.GetPoint("\n请指定标注第一点");
                //if (userPoint1.Status != PromptStatus.OK) return;
                //var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();
                mt.Contents = dimString;
                mt.ColorIndex = textColorIntex;

                // 添加引线和引线段  
                int ldNum = mld.AddLeader();
                int lnNum = mld.AddLeaderLine(ldNum);
                mld.AddFirstVertex(lnNum, point3D);  // 引线起始点（UCS 坐标）
                var mpwUcs = new Point3d(0, 0, 0);
                using var mleaderjig = new JigEx((mpw, _) =>
                {
                    // 引线结束点（UCS 坐标）
                    mpwUcs = mpw.Z20();
                    // 标注文字显示位置        
                    mld.TextLocation = mpwUcs;
                });
                var UcsUserPoint2 = mleaderjig.MousePointWcsLast;
                mld.AddLastVertex(lnNum, UcsUserPoint2);
                mld.MText = mt;
                mld.TextHeight = 300;
                mld.TextStyleId = tr.TextStyleTable["tJText"];
                mleaderjig.DatabaseEntityDraw(wb => wb.Geometry.Draw(mld));
                mleaderjig.SetOptions(point3D, msg: "\n点选标注第二点");
                var userPoint2 = Env.Editor.Drag(mleaderjig);
                if (userPoint2.Status != PromptStatus.OK) return;
                tr.CurrentSpace.AddEntity(mld);
                tr.Commit();
                Env.Editor.Redraw();//重新刷新
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"\n做标注失败！错误信息: {ex.Message}");
                Env.Editor.WriteMessage($"\n错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 创建水平、垂直或任意角度的线性标注对象
        /// </summary>
        /// <param name="pt1">第一点</param>
        /// <param name="layer">图层名</param>
        /// <param name="VariableDictionary.layerColorIndex">图层颜色</param>
        public static void DDimLinear(string layerName, Int16 layerColorIndex, Point3d pt1)
        {
            try
            {
                //开启事务
                var tr = new DBTrans();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, Convert.ToInt16(VariableDictionary.layerColorIndex), "tJText");
                // 创建水平、垂直或任意角度的线性标注对象
                RotatedDimension rDim = new RotatedDimension()
                {
                    // 设置标注图层  
                    Layer = layerName,
                    // 设置标注颜色  
                    ColorIndex = VariableDictionary.layerColorIndex,
                    // 设置标注的第一点  
                    XLine1Point = pt1,
                    LinetypeScale = 0.8,
                };
                //把标注文字设置为初始值，也就是默认两点长度
                rDim.DimensionText = null;
                //Dimtxt 指定标注文字的高度，除非当前文字样式具有固定的高度
                //rDim.Dimtxt = 300;
                // 通过工厂方法生成一个红色的 Color 对象（1 = 红色索引）
                rDim.Dimclrt = Color.FromColorIndex(ColorMethod.ByColor, Convert.ToInt16(VariableDictionary.layerColorIndex));
                rDim.Dimlfac = 1;//设置线型标注的全局比例
                                 //rDim.Dimasz = 100;//控制尺寸线、引线箭头的大小
                rDim.Dimatfit = 100;//标注箭头大小
                rDim.ColorIndex = VariableDictionary.layerColorIndex;
                rDim.TextStyleId = tr.TextStyleTable["tJText"];
                // 打开正交模式
                Env.OrthoMode = true;
                // 关闭正交模式
                //Env.OrthoMode = false;
                //动态显示标注并提示指定第二点；
                using var dimPoint2 = new JigEx((mpw, Queue) =>
                {
                    var pt2 = mpw.Z20();
                    //标注文字
                    rDim.DimensionText = Convert.ToString(Convert.ToInt32(pt1.DistanceTo(pt2)));
                    rDim.XLine2Point = pt2;
                    rDim.DimLinePoint = pt2;
                });
                dimPoint2.DatabaseEntityDraw(WorldDraw => WorldDraw.Geometry.Draw(rDim));
                dimPoint2.SetOptions(pt1, msg: "\n请指定方形洞口第二点");
                var userPoint2 = dimPoint2.Drag();//拿到的第二点；
                if (userPoint2.Status != PromptStatus.OK) return;
                // 计算旋转角度
                rDim.Rotation = pt1.GetVectorTo(dimPoint2.MousePointWcsLast).GetAngleTo(Vector3d.XAxis);
                SetDimensionRotationToNearest90Degrees(rDim, dimPoint2.MousePointWcsLast);
                // 提示用户指定标注位置
                using var dimTextPoint = new JigEx((mpw, Queue) =>
                {
                    var pt3 = mpw.Z20();
                    rDim.DimLinePoint = pt3;//标注点
                });
                dimTextPoint.DatabaseEntityDraw(WorldDraw => WorldDraw.Geometry.Draw(rDim));
                dimTextPoint.SetOptions(dimPoint2.MousePointWcsLast, msg: "\n请选择标注文字位置");
                var userPoint3 = dimTextPoint.Drag();//拿到的标注文字点；
                if (userPoint3.Status != PromptStatus.OK) return;
                //double angle = pt1.GetVectorTo(dimPoint2.MousePointWcsLast).GetAngleTo(Vector3d.XAxis);
                //拿到鼠标位置
                rDim.DimLinePoint = dimTextPoint.MousePointWcsLast;

                // 添加标注对象到模型空间
                tr.CurrentSpace.AddEntity(rDim);
                //正交关闭
                Env.OrthoMode = false;
                // 提交事务
                tr.Commit();

                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("给出一点与图层名，做标注失败！");
                Env.Editor.WriteMessage($"{ex.Message}");
            }
        }

        /// <summary>
        /// 调用正交角度
        /// </summary>
        /// <param name="rDim">标注</param>
        /// <param name="dimPoint2">坐标点</param>
        public static void SetDimensionRotationToNearest90Degrees(RotatedDimension rDim, Point3d dimPoint2)
        {
            // 获取当前旋转角度
            double currentAngle = rDim.Rotation;

            // 计算向量
            //Vector3d vectorToDimPoint = rDim.Position.GetVectorTo(dimPoint2);

            var vectorToDimPoint = rDim.TextPosition.GetVectorTo(dimPoint2);

            // 计算向量与X轴的角度
            double angleToXAxis = vectorToDimPoint.GetAngleTo(Vector3d.XAxis);

            // 将角度转换为0-360度范围
            angleToXAxis = angleToXAxis * (180 / Math.PI);
            if (angleToXAxis < 0)
            {
                angleToXAxis += 360;
            }

            // 找到最接近的0、90、180、270度
            double nearestAngle = FindNearestAngle(angleToXAxis);

            // 设置旋转角度
            rDim.Rotation = nearestAngle * (Math.PI / 180);
        }

        /// <summary>
        /// 计算正交角度
        /// </summary>
        /// <param name="angle">任意角度</param>
        /// <returns></returns>
        private static double FindNearestAngle(double angle)
        {
            double[] targetAngles = { 0, 90, 180, 270 };
            double nearestAngle = targetAngles[0];
            double minDifference = Math.Abs(angle - targetAngles[0]);

            for (int i = 1; i < targetAngles.Length; i++)
            {
                double difference = Math.Abs(angle - targetAngles[i]);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    nearestAngle = targetAngles[i];
                }
            }

            return nearestAngle;
        }

        /// <summary>
        /// 标注
        /// </summary>
        /// <param name="pt1Ucs">第一点坐标</param>
        /// <param name="dimX">X坐标</param>
        /// <param name="dimY">Y坐标</param>
        /// <param name="dimFl">标注文字</param>
        /// <param name="layerName">图层名</param>
        /// <param name="VariableDictionary.layerColorIndex">图层颜色</param>
        [CommandMethod(nameof(PointDim))]
        public static void PointDim(Point3d UcsUserPoint1, string dimX, string dimY, string dimFl, string layerName, Int16 layerColorIndex)
        {
            try
            {
                using var tr = new DBTrans();
                TextStyleAndLayerInfo(layerName, Convert.ToInt16(VariableDictionary.layerColorIndex), "tJText");
                var mld = new MLeader
                {
                    Layer = layerName,//设置多重引线的图层
                    ColorIndex = VariableDictionary.layerColorIndex,
                    TextAttachmentType = TextAttachmentType.AttachmentBottomOfTopLine,//设置多重引线的标注文字下是不是有引线；
                    ContentType = ContentType.MTextContent,//内容类型
                    LeaderLineColor = Color.FromColorIndex(ColorMethod.ByAci, Convert.ToInt16(VariableDictionary.layerColorIndex)),// 例如索引3通常代表绿色
                };
                //标注样式
                MText mt = new MText();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, Convert.ToInt16(VariableDictionary.layerColorIndex), "tJText", ref mt, ref mld);
                mt.Attachment = AttachmentPoint.MiddleCenter; // 设置标注文字居中对齐  
                                                              // 添加引线和引线段  
                int ldNum = mld.AddLeader();
                int lnNum = mld.AddLeaderLine(ldNum);
                mld.AddFirstVertex(lnNum, UcsUserPoint1);  // 引线起始点（UCS 坐标）
                var mpwUcs = new Point3d(0, 0, 0);
                using var mleaderjig = new JigEx((mpw, _) =>
                {
                    // 引线结束点（UCS 坐标）
                    mpwUcs = mpw.Z20();
                    // 标注文字显示位置        
                    mld.TextLocation = mpwUcs;
                });
                var UcsUserPoint2 = mleaderjig.MousePointWcsLast;
                //标注文字
                mt.Contents = dimX + dimY + dimFl;
                //标注文字高度
                mt.Height = 300;
                mt.ColorIndex = VariableDictionary.layerColorIndex;
                mld.AddLastVertex(lnNum, UcsUserPoint2);
                mld.MText = mt;
                mld.TextHeight = 300;
                mld.TextStyleId = tr.TextStyleTable["tJText"];
                mleaderjig.DatabaseEntityDraw(wb => wb.Geometry.Draw(mld));
                mleaderjig.SetOptions(UcsUserPoint1, msg: "\n标注文字的位置");
                var userPoint2 = Env.Editor.Drag(mleaderjig);
                if (userPoint2.Status != PromptStatus.OK) return;

                tr.CurrentSpace.AddEntity(mld);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"标注失败！错误信息: {ex.Message}"); // 输出错误信息  
            }
        }

        public static sendText? sendSum;

        /// <summary>
        /// 选择实体
        /// </summary>
        [CommandMethod("SelectEntities")]
        public void SelectEntities()
        {
            try
            {
                var tr = new DBTrans();
                double kwSum = 0;
                //int i = 0;
                // 提示用户选择图元
                PromptSelectionResult selectionResult = tr.Editor.GetSelection();
                if (selectionResult.Status == PromptStatus.OK)
                {
                    SelectionSet selectionSet = selectionResult.Value;

                    foreach (SelectedObject selectedObject in selectionSet)
                    {
                        if (selectedObject.ObjectId.ObjectClass.DxfName == "TEXT")
                        {
                            var kwTextString = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead) as DBText;
                            if (kwTextString != null)
                            {

                                Match kwTextMatch = Regex.Match(kwTextString.TextString.ToLower(), @"\d+(\.\d+)?");
                                //i++;
                                if (kwTextMatch.Success)
                                {
                                    // 将匹配到的数字转换为double类型，并加到总和中
                                    double number = double.Parse(kwTextMatch.Value);
                                    kwSum += number;
                                }
                            }

                        }
                    }
                    var kwSumString = kwSum.ToString();
                    sendSum?.Invoke(kwSumString + "kw");//与下面的表达式相同，判断是不是为空，真时就调用传值；
                }
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("选择实体失败！");
            }
        }

        /// <summary>
        /// 获取外参实体的ObjectId的List
        /// </summary>
        /// <param name="xrefId">要获取的外参实体的ObjectId</param>
        /// <returns>返回外参实体的ObjectId的List</returns>
        public static List<ObjectId> GetXrefEntities(ObjectId xrefId)
        {
            List<ObjectId> entities = new List<ObjectId>();
            try
            {
                using (var tr = new DBTrans())
                {
                    BlockReference xref = tr.GetObject(xrefId, OpenMode.ForRead) as BlockReference;
                    if (xref != null)
                    {
                        BlockTableRecord xrefBtr = tr.GetObject(xref.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (xrefBtr is not null)
                            foreach (ObjectId entityId in xrefBtr)
                            {
                                entities.Add(entityId);
                            }
                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("插入图元失败！");
                Env.Editor.WriteMessage("错误信息: " + ex.Message);
            }
            return entities;
        }



        #endregion

        #region 建筑绘图

        /// <summary>
        /// 建筑、用户指定两点吊顶区Line2Polyline
        /// </summary>
        /// <param name="layerName"></param>
        [CommandMethod(nameof(Line2Polyline))]
        public static void Line2Polyline()
        {
            try
            {
                var layerName = VariableDictionary.btnBlockLayer;
                var layerColorIndex = VariableDictionary.layerColorIndex;
                using var tr = new DBTrans();
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, VariableDictionary.layerColorIndex);
                if (!tr.TextStyleTable.Has("tJText"))
                {
                    //ifox自带的文字样式添加  
                    tr.TextStyleTable.Add("tJText", ttr =>
                    {
                        ttr.FileName = "txt.shx";// 字体文件  
                        ttr.BigFontFileName = "hztxt.shx";// 大字体文件  
                        ttr.XScale = 1; // 字体比例  
                    });
                }
                var userPoint1 = Env.Editor.GetPoint("\n请指定第一点");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20();//转换为UCS坐标  

                using var polyLine = new JigEx((mpw, queue) =>
                {
                    var UcsUserPoint2 = mpw.Z20();
                    Polyline polyline1 = new Polyline();
                    polyline1.AddVertexAt(0, new Point2d(UcsUserPoint1.X, UcsUserPoint1.Y), 0, 0, 0);
                    polyline1.AddVertexAt(1, new Point2d(UcsUserPoint2.X, UcsUserPoint2.Y), 0, 0, 0);
                    polyline1.Closed = false;
                    polyline1.Layer = layerName;
                    polyline1.ColorIndex = VariableDictionary.layerColorIndex;
                    queue.Enqueue(polyline1);

                    Polyline polyline2 = new Polyline();
                    polyline2.AddVertexAt(0, new Point2d(UcsUserPoint2.X, UcsUserPoint1.Y), 0, 0, 0);
                    polyline2.AddVertexAt(1, new Point2d(UcsUserPoint1.X, UcsUserPoint2.Y), 0, 0, 0);
                    polyline2.Closed = false;
                    polyline2.Layer = layerName;
                    polyline2.ColorIndex = VariableDictionary.layerColorIndex;
                    // 计算线的角度（弧度）  
                    double angle = Math.Atan2(UcsUserPoint2.Y - UcsUserPoint1.Y, UcsUserPoint2.X - UcsUserPoint1.X);

                    // 计算线的中点  
                    Point3d midPoint = new Point3d(
                        (UcsUserPoint1.X + UcsUserPoint2.X) / 2,
                        (UcsUserPoint1.Y + UcsUserPoint2.Y) / 2,
                        0
                    );
                    // 文字偏移距离  
                    double offsetDistance = 200; // 可以根据需要调整  
                                                 // 计算垂直偏移向量  
                    Vector3d offsetVector = new Vector3d(-Math.Sin(angle), Math.Cos(angle), 0) * offsetDistance;
                    // 计算文字插入点（线上方）  
                    Point3d textPoint = midPoint + offsetVector;
                    // 将角度转换为度  
                    double angleDegrees = angle * 180 / Math.PI;
                    // 确保文字方向正确（不会上下颠倒）  
                    if (angleDegrees > 90 || angleDegrees < -90)
                    {
                        angleDegrees += 180;
                        angle += Math.PI;
                        textPoint = midPoint - offsetVector;
                    }

                    if (VariableDictionary.btnFileName == "JZTJ_不吊顶")
                    {
                        queue.Enqueue(polyline2);
                        DBText text = new DBText()
                        {
                            TextStyleId = tr.TextStyleTable["tJText"],
                            TextString = "不吊顶",
                            Height = 350,
                            WidthFactor = 0.7,
                            ColorIndex = VariableDictionary.layerColorIndex,
                            Layer = layerName,
                            Position = textPoint,
                            //Rotation = angle,
                            HorizontalMode = TextHorizontalMode.TextCenter,
                            VerticalMode = TextVerticalMode.TextVerticalMid,
                            AlignmentPoint = textPoint
                        };
                        queue.Enqueue(text);
                    }
                    else
                    {
                        string diaoDingHeight=VariableDictionary.wpfDiaoDingHeight==""
                        ?VariableDictionary.winFormDiaoDingHeight
                        :VariableDictionary.wpfDiaoDingHeight;

                        DBText text = new DBText()
                        {
                            TextStyleId = tr.TextStyleTable["tJText"],
                            TextString = "吊顶高度:" + diaoDingHeight + "米",
                            Height = 350,
                            WidthFactor = 0.7,
                            ColorIndex = VariableDictionary.layerColorIndex,
                            Layer = layerName,
                            Position = textPoint,
                            Rotation = angle,
                            HorizontalMode = TextHorizontalMode.TextCenter,
                            VerticalMode = TextVerticalMode.TextVerticalMid,
                            AlignmentPoint = textPoint
                        };
                        queue.Enqueue(text);
                    }
                });

                polyLine.SetOptions(UcsUserPoint1, msg: "\n请指定第二点");
                var userPoint2 = Env.Editor.Drag(polyLine);
                if (userPoint2.Status != PromptStatus.OK) return;
                var polyLineEntityObj = tr.CurrentSpace.AddEntity(polyLine.Entities);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("建筑、用户指定两点吊顶区Line2Polyline失败！");
                Env.Editor.WriteMessage(ex.Message);
            }
        }

        /// <summary>
        /// 建筑房间号文字
        /// </summary>
        /// <param name="layerName"></param>
        [CommandMethod(nameof(DBTextLabel_JZ))]
        public static void DBTextLabel_JZ()
        {
            try
            {
                using var tr = new DBTrans();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, Convert.ToInt16(VariableDictionary.layerColorIndex), "tJText");
                //创建文字与文字属性
                DBText text = new DBText()
                {
                    TextStyleId = tr.TextStyleTable["tJText"],
                    TextString = VariableDictionary.btnFileName,//字体的内容
                    Height = 350,//字体的高度
                    WidthFactor = 0.7,//字体的宽度因子
                    ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex),//字体的颜色
                    Layer = VariableDictionary.btnBlockLayer,//字体的图层
                };

                var dbTextEntityObj = tr.CurrentSpace.AddEntity(text);//写入当前空间
                var startPoint = new Point3d(0, 0, 0);
                double tempAngle = 0;//角度
                var entityBBText = new JigEx((mpw, _) =>
                {
                    text.Move(startPoint, mpw);
                    startPoint = mpw;
                    if (VariableDictionary.entityRotateAngle == tempAngle)
                    {
                        return;
                    }
                    else if (VariableDictionary.entityRotateAngle != tempAngle)
                    {
                        text.Rotation(center: mpw, 0);
                        tempAngle = VariableDictionary.entityRotateAngle;
                        text.Rotation(center: mpw, tempAngle);
                    }
                });
                entityBBText.DatabaseEntityDraw(wd => wd.Geometry.Draw(text));
                entityBBText.SetOptions(msg: "\n指定插入点");
                //entityBlock.SetOptions(startPoint, msg: "\n指定插入点");这个startpoint，是有个参考线在里面，用于托拽时的辅助；
                var endPoint = Env.Editor.Drag(entityBBText);
                if (endPoint.Status != PromptStatus.OK)
                    tr.Abort();
                Env.Editor.Redraw();//重新刷新
                tr.Commit();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("建筑房间号文字失败！");
                Env.Editor.WriteMessage(ex.Message);
            }
        }

        /// <summary>
        /// 建筑专业用鼠标画矩形
        /// </summary>
        /// <param name="layerName"></param>
        [CommandMethod(nameof(Rec2PolyLine_2))]
        public static void Rec2PolyLine_2()
        {
            try
            {
                var layerName = VariableDictionary.btnBlockLayer;
                var layerColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                using var tr = new DBTrans();

                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, layerColorIndex);

                // 获取矩形左下角点（起始点）  
                var userPoint1 = Env.Editor.GetPoint("\n指定矩形的左下角点：");
                if (userPoint1.Status != PromptStatus.OK)
                    return;
                var basePoint = userPoint1.Value.Wcs2Ucs().Z20();

                // 通过鼠标拖动确定矩形宽度（X 方向增量），高度固定为300  
                using var jig = new JigEx((mpw, queue) =>
                {
                    // 获取当前动态点  
                    var dynamicPoint = mpw.Z20();
                    // 计算宽度（可为负值，表示向左延伸）  
                    double width = dynamicPoint.X - basePoint.X;
                    // 构造矩形的多段线  
                    Polyline polyline1 = new Polyline()
                    {
                        Layer = layerName,
                        ColorIndex = layerColorIndex
                    };
                    // 四个顶点顺序：左下、右下、右上、左上  
                    polyline1.AddVertexAt(0, new Point2d(basePoint.X, basePoint.Y), 0, 0, 0);
                    polyline1.AddVertexAt(1, new Point2d(basePoint.X + width, basePoint.Y), 0, 0, 0);
                    polyline1.AddVertexAt(2, new Point2d(basePoint.X + width, basePoint.Y + 300), 0, 0, 0);
                    polyline1.AddVertexAt(3, new Point2d(basePoint.X, basePoint.Y + 300), 0, 0, 0);
                    polyline1.Closed = true;

                    queue.Enqueue(polyline1);
                });

                jig.SetOptions(basePoint, msg: "\n指定矩形宽度的第二个点（右键结束）：");
                var userPoint2 = Env.Editor.Drag(jig);
                if (userPoint2.Status != PromptStatus.OK)
                {
                    Env.Editor.WriteMessage("\n未指定矩形宽度，操作取消。");
                    return;
                }
                // 通过最后一个动态点确定最终宽度  
                double finalWidth = jig.MousePointWcsLast.X - basePoint.X;

                // 创建最终的矩形多段线  
                Polyline finalPolyline = new Polyline();
                finalPolyline.AddVertexAt(0, new Point2d(basePoint.X, basePoint.Y), 0, 0, 0);
                finalPolyline.AddVertexAt(1, new Point2d(basePoint.X + finalWidth, basePoint.Y), 0, 0, 0);
                finalPolyline.AddVertexAt(2, new Point2d(basePoint.X + finalWidth, basePoint.Y + 300), 0, 0, 0);
                finalPolyline.AddVertexAt(3, new Point2d(basePoint.X, basePoint.Y + 300), 0, 0, 0);
                finalPolyline.Closed = true;
                finalPolyline.Layer = layerName;
                finalPolyline.ColorIndex = 231;

                // 添加矩形并获取其ID  
                var polylineId = tr.CurrentSpace.AddEntity(finalPolyline);

                try
                {
                    // 创建填充图案  
                    Hatch hatch = new Hatch();
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31"); // 设置填充图案为 ANSI31
                    hatch.PatternScale = 200; // 设置填充图案比例为 200  
                    hatch.Layer = layerName; // 设置图层  
                    hatch.ColorIndex = 231; // 设置填充色号  
                    hatch.PatternAngle = 0; // 设置填充角度  
                    hatch.Normal = Vector3d.ZAxis;
                    ObjectIdCollection boundaryIds = new ObjectIdCollection(); // 创建边界集合  
                    boundaryIds.Add(polylineId);
                    hatch.AppendLoop(HatchLoopTypes.External, boundaryIds); // 添加外部环  
                    hatch.EvaluateHatch(true); // 强制计算填充图案  
                    var hatchId = tr.CurrentSpace.AddEntity(hatch); // 将填充添加到模型空间  

                    Env.Editor.Redraw();  // 强制刷新视图  
                    Env.Editor.WriteMessage("\n矩形绘制完成并闭合，填充图案已添加。");
                }
                catch (System.Exception ex)
                {
                    Env.Editor.WriteMessage($"\n创建填充时出错: {ex.Message}");
                }

                Env.Editor.Redraw(); // 强制刷新视图  
                if (VariableDictionary.dimString_JZ_宽 != null && VariableDictionary.dimString_JZ_深 != null)
                    DDimLinear(VariableDictionary.dimString_JZ_深, VariableDictionary.dimString_JZ_宽, Convert.ToInt16(VariableDictionary.layerColorIndex));
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n矩形绘制失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 建筑专业用鼠标画矩形
        /// </summary>
        /// <param name="layerName"></param>
        [CommandMethod(nameof(Rec2PolyLine_3))]
        public static void Rec2PolyLine_3()
        {
            try
            {
                var layerName = VariableDictionary.btnBlockLayer;
                var layerColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                // 计算矢量差（拖动时基于参考点的偏移量）  
                var delta = new Vector3d(0, 0, 0);
                using var tr = new DBTrans();

                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, VariableDictionary.layerColorIndex);

                // 获取参考点（可以视为左下角，但后续根据方向调整）  
                var userPoint1 = Env.Editor.GetPoint("\n指定矩形的参考点：");
                if (userPoint1.Status != PromptStatus.OK)
                    return;
                var basePoint = userPoint1.Value.Wcs2Ucs().Z20();

                double? userDistance = null; // 保存用户通过命令行输入的距离数值  

                // 使用 JigEx 动态预览矩形，同时允许用户输入数值作为距离值  
                using var jig = new JigEx((mpw, queue) =>
                {
                    // 获取当前动态点  
                    var dynamicPoint = mpw.Z20();
                    // 计算矢量差（拖动时基于参考点的偏移量）  
                    delta = dynamicPoint - basePoint;
                    // 判断以哪个方向为主：若 Y 差绝对值大于 X 差，视为竖直模式，否则为水平模式  
                    bool verticalMode = (System.Math.Abs(delta.Y) > System.Math.Abs(delta.X));

                    // 如果用户已输入数值，则采用该数值作为距离；  
                    // 否则直接以鼠标相对于参考点的偏移作为距离  
                    double distanceValue;
                    if (userDistance.HasValue)
                    {
                        if (verticalMode)
                        {
                            // 当处于竖直模式时，同时判断鼠标在参考点上方或下方  
                            distanceValue = (delta.Y >= 0 ? userDistance.Value : -userDistance.Value);
                        }
                        else
                        {
                            // 水平模式：判断鼠标是在参考点右侧还是左侧  
                            distanceValue = (delta.X >= 0 ? userDistance.Value : -userDistance.Value);
                        }
                    }
                    else
                    {
                        distanceValue = verticalMode ? delta.Y : delta.X;
                    }

                    // 构造预览矩形多段线  
                    Polyline polyline1 = new Polyline()
                    {
                        Layer = layerName,
                        ColorIndex = layerColorIndex
                    };

                    if (verticalMode)
                    {
                        // 竖直模式：宽固定 300，矩形高度由 distanceValue 决定，  
                        // 但这里还要判断鼠标在水平方向（相对于参考点）是否位于右侧或左侧，  
                        // 如果在左侧，则矩形宽度应反向（向左延伸）  
                        if (delta.X >= 0)
                        {
                            // 鼠标在右侧：宽向右，顶点顺序：参考点、向右延伸、上/下延伸、垂直延伸回到参考点的X  
                            polyline1.AddVertexAt(0, new Point2d(basePoint.X, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(1, new Point2d(basePoint.X + 300, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(2, new Point2d(basePoint.X + 300, basePoint.Y + distanceValue), 0, 0, 0);
                            polyline1.AddVertexAt(3, new Point2d(basePoint.X, basePoint.Y + distanceValue), 0, 0, 0);
                        }
                        else
                        {
                            // 鼠标在左侧：宽向左，顶点顺序：参考点、向左延伸、上/下延伸、垂直返回  
                            polyline1.AddVertexAt(0, new Point2d(basePoint.X, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(1, new Point2d(basePoint.X - 300, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(2, new Point2d(basePoint.X - 300, basePoint.Y + distanceValue), 0, 0, 0);
                            polyline1.AddVertexAt(3, new Point2d(basePoint.X, basePoint.Y + distanceValue), 0, 0, 0);
                        }
                    }
                    else
                    {
                        // 水平模式：高固定 300，由 distanceValue 决定矩形宽度方向，  
                        // 同时判断鼠标在垂直方向上是位于参考点的上方还是下方  
                        if (delta.Y >= 0)
                        {
                            // 鼠标在上方：高向上  
                            polyline1.AddVertexAt(0, new Point2d(basePoint.X, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(1, new Point2d(basePoint.X + distanceValue, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(2, new Point2d(basePoint.X + distanceValue, basePoint.Y + 300), 0, 0, 0);
                            polyline1.AddVertexAt(3, new Point2d(basePoint.X, basePoint.Y + 300), 0, 0, 0);
                        }
                        else
                        {
                            // 鼠标在下方：高向下，顶点顺序调整后确保矩形延伸方向正确  
                            polyline1.AddVertexAt(0, new Point2d(basePoint.X, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(1, new Point2d(basePoint.X + distanceValue, basePoint.Y), 0, 0, 0);
                            polyline1.AddVertexAt(2, new Point2d(basePoint.X + distanceValue, basePoint.Y - 300), 0, 0, 0);
                            polyline1.AddVertexAt(3, new Point2d(basePoint.X, basePoint.Y - 300), 0, 0, 0);
                        }
                    }
                    polyline1.Closed = true;
                    queue.Enqueue(polyline1);
                });

                // 提示信息：用户拖动或输入数值后直接回车  
                jig.SetOptions(basePoint, msg: "\n指定矩形第二点（或输入距离值后回车）：");
                var userResponse = Env.Editor.Drag(jig);
                if (userResponse.Status != PromptStatus.OK)
                {
                    Env.Editor.WriteMessage("\n未指定矩形尺寸，操作取消。");
                    return;
                }

                // 判断是否有用户输入的数值（假设 jig.DistanceEntered 属性可得输入距离，非 0 表示输入了数值）  
                //if (jig.DistanceEntered != 0)
                //    userDistance = jig.DistanceEntered;

                // 获取最终动态点（如果未输入数值则直接取鼠标位置）  
                var dynamicPointFinal = jig.MousePointWcsLast;
                var deltaFinal = dynamicPointFinal - basePoint;
                bool isVertical = (System.Math.Abs(deltaFinal.Y) > System.Math.Abs(deltaFinal.X));

                double finalDistance;
                if (userDistance.HasValue)
                {
                    finalDistance = isVertical ?
                                    (deltaFinal.Y >= 0 ? userDistance.Value : -userDistance.Value) :
                                    (deltaFinal.X >= 0 ? userDistance.Value : -userDistance.Value);
                }
                else
                {
                    finalDistance = isVertical ? deltaFinal.Y : deltaFinal.X;
                }

                // 根据鼠标最终位置判断方向，并计算矩形四个顶点  
                Point2d pt0, pt1, pt2, pt3;
                if (isVertical)
                {
                    if (delta.X >= 0)
                    {
                        // 鼠标在参考点右侧  
                        pt0 = new Point2d(basePoint.X, basePoint.Y);
                        pt1 = new Point2d(basePoint.X + 300, basePoint.Y);
                        pt2 = new Point2d(basePoint.X + 300, basePoint.Y + finalDistance);
                        pt3 = new Point2d(basePoint.X, basePoint.Y + finalDistance);
                    }
                    else
                    {
                        // 鼠标在参考点左侧  
                        pt0 = new Point2d(basePoint.X, basePoint.Y);
                        pt1 = new Point2d(basePoint.X - 300, basePoint.Y);
                        pt2 = new Point2d(basePoint.X - 300, basePoint.Y + finalDistance);
                        pt3 = new Point2d(basePoint.X, basePoint.Y + finalDistance);
                    }
                }
                else
                {
                    if (delta.Y >= 0)
                    {
                        // 鼠标在参考点上方  
                        pt0 = new Point2d(basePoint.X, basePoint.Y);
                        pt1 = new Point2d(basePoint.X + finalDistance, basePoint.Y);
                        pt2 = new Point2d(basePoint.X + finalDistance, basePoint.Y + 300);
                        pt3 = new Point2d(basePoint.X, basePoint.Y + 300);
                    }
                    else
                    {
                        // 鼠标在参考点下方  
                        pt0 = new Point2d(basePoint.X, basePoint.Y);
                        pt1 = new Point2d(basePoint.X + finalDistance, basePoint.Y);
                        pt2 = new Point2d(basePoint.X + finalDistance, basePoint.Y - 300);
                        pt3 = new Point2d(basePoint.X, basePoint.Y - 300);
                    }
                }

                // 创建最终矩形闭合多段线  
                Polyline finalPolyline = new Polyline();
                finalPolyline.AddVertexAt(0, new Point2d(pt0.X, pt0.Y), 0, 0, 0);
                finalPolyline.AddVertexAt(1, new Point2d(pt1.X, pt1.Y), 0, 0, 0);
                finalPolyline.AddVertexAt(2, new Point2d(pt2.X, pt2.Y), 0, 0, 0);
                finalPolyline.AddVertexAt(3, new Point2d(pt3.X, pt3.Y), 0, 0, 0);
                finalPolyline.Closed = true;
                finalPolyline.Layer = layerName;
                finalPolyline.ColorIndex = VariableDictionary.layerColorIndex;

                // 添加矩形并获取其 ID  
                var polylineId = tr.CurrentSpace.AddEntity(finalPolyline);

                try
                {
                    // 创建填充图案  
                    Hatch hatch = new Hatch();
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                    hatch.PatternScale = 100;
                    hatch.Layer = layerName;
                    hatch.ColorIndex = VariableDictionary.layerColorIndex;
                    hatch.PatternAngle = 0;
                    hatch.Normal = Vector3d.ZAxis;
                    ObjectIdCollection boundaryIds = new ObjectIdCollection();
                    boundaryIds.Add(polylineId);
                    hatch.AppendLoop(HatchLoopTypes.External, boundaryIds);
                    hatch.EvaluateHatch(true);
                    var hatchId = tr.CurrentSpace.AddEntity(hatch);

                    Env.Editor.Redraw();
                    Env.Editor.WriteMessage("\n矩形绘制完成并闭合，填充图案已添加。");
                }
                catch (System.Exception ex)
                {
                    Env.Editor.WriteMessage($"\n创建填充时出错: {ex.Message}");
                }
                if (VariableDictionary.dimString_JZ_宽 != null && VariableDictionary.dimString_JZ_深 != null)
                    DDimLinear(VariableDictionary.dimString_JZ_深, VariableDictionary.dimString_JZ_宽, Convert.ToInt16(VariableDictionary.layerColorIndex));

                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (System.Exception ex)
            {
                Env.Editor.WriteMessage($"\n矩形绘制失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 建筑画防撞板
        /// </summary>
        [CommandMethod(nameof(ParallelLines))]
        public static void ParallelLines()
        {
            try
            {
                var layerName = VariableDictionary.btnBlockLayer; // 图层
                var move = VariableDictionary.textbox_Gap; // 获取用户输入的移动距离

                using var tr = new DBTrans();
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, 64);

                // 获取用户输入的第一点和第二点
                var userPoint1 = Env.Editor.GetPoint("\n请指定第一点");
                if (userPoint1.Status != PromptStatus.OK) return;
                var UcsUserPoint1 = userPoint1.Value.Wcs2Ucs().Z20(); // 转换为UCS坐标

                var userPoint2 = Env.Editor.GetPoint("\n请指定第二点（确定平行线距离）");
                if (userPoint2.Status != PromptStatus.OK) return;
                var UcsUserPoint2 = userPoint2.Value.Wcs2Ucs().Z20(); // 转换为UCS坐标

                // 计算两条平行线之间的偏移向量
                var offset = UcsUserPoint2 - UcsUserPoint1;

                // 计算移动后的偏移量
                var offsetDirection = offset.GetNormal(); // 获取偏移向量的单位方向向量
                var moveOffset = offsetDirection * move; // 计算移动的距离向量

                using var parallelLine = new JigEx((mpw, queue) =>
                {
                    var UcsUserPointEnd = mpw.Z20();

                    // 绘制第一条线
                    Polyline polyline1 = new Polyline();
                    polyline1.AddVertexAt(0, new Point2d(UcsUserPoint1.X + moveOffset.Value.X, UcsUserPoint1.Y + moveOffset.Value.Y), 0, 0, 0); // 起点向第二条线移动
                    polyline1.AddVertexAt(1, new Point2d(UcsUserPointEnd.X + moveOffset.Value.X, UcsUserPointEnd.Y + moveOffset.Value.Y), 0, 0, 0); // 终点向第二条线移动
                    polyline1.Closed = false;
                    polyline1.Layer = layerName; // 设置线条图层
                    polyline1.ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                    polyline1.SetStartWidthAt(0, 60);
                    polyline1.SetEndWidthAt(0, 60);
                    queue.Enqueue(polyline1);

                    // 绘制第二条平行线
                    Polyline polyline2 = new Polyline();
                    polyline2.AddVertexAt(0, new Point2d(UcsUserPoint2.X - moveOffset.Value.X, UcsUserPoint2.Y - moveOffset.Value.Y), 0, 0, 0); // 起点向第一条线移动
                    polyline2.AddVertexAt(1, new Point2d(UcsUserPointEnd.X + offset.X - moveOffset.Value.X, UcsUserPointEnd.Y + offset.Y - moveOffset.Value.Y), 0, 0, 0); // 终点向第一条线移动
                    polyline2.Closed = false;
                    polyline2.Layer = layerName; // 设置线条图层
                    polyline2.ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);
                    polyline2.SetStartWidthAt(0, 60);
                    polyline2.SetEndWidthAt(0, 60);
                    queue.Enqueue(polyline2);
                });

                parallelLine.SetOptions(UcsUserPoint1, msg: "\n请指定终点");
                var userPointEnd = Env.Editor.Drag(parallelLine);
                if (userPointEnd.Status != PromptStatus.OK) return;
                var polyLineEntityObj = tr.CurrentSpace.AddEntity(parallelLine.Entities);
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("建筑画防撞板失败！");
                Env.Editor.WriteMessage(ex.Message);
            }
        }
        #endregion

        #region 计算面积方法
        /// <summary>
        /// 指定点计算点内面积
        /// </summary>
        [CommandMethod("AreaByPoints")]
        public void AreaByPoints()
        {
            //获取图层名称
            string? layerName = VariableDictionary.btnBlockLayer;//图层名
            Int16 layerColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex);//图层颜色

            // 用于存储用户选择的点
            List<Point2d> points = new List<Point2d>();
            //var whileTrueFalse = true;
            // 循环让用户选择点，至少3个点
            while (true)
            {
                PromptPointOptions ppo = new PromptPointOptions("\n请指定框着地内线第一点");
                ppo.AllowNone = true;
                // 获取第一个点（左键点击有效，右键取消则返回 None）  
                var userPoint1 = Env.Editor.GetPoint(ppo);
                if (userPoint1.Status != PromptStatus.OK) break;

                // 添加点到列表
                points.Add(new Point2d(userPoint1.Value.X, userPoint1.Value.Y));
            }

            // 如果点数不足3个，提示并继续
            if (points.Count < 3)
            {
                Env.Editor.WriteMessage("\n至少需要3个点来计算面积！");
                return;
            }
            // 用户回车结束输入

            // 计算多边形面积
            double area = CalculatePolygonArea(points);

            // 计算多边形质心
            Point2d centroid = CalculateCentroid(points);

            using (var tr = new DBTrans())
            {
                if (layerName != null && !tr.LayerTable.Has(layerName))
                    tr.LayerTable.Add(layerName, layerColorIndex);//添加图层；

                var text = new DBText();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, layerColorIndex, "tJText");
                text.Height = 300; // 文字高度
                text.TextString = $"面积: {area:F2} m²";
                text.TextStyleId = tr.TextStyleTable["tJText"];
                text.Layer = layerName;
                //text.Text = $"面积: {area:F2}";
                //拖动实现
                using var moveText = new JigEx((mpw, _) =>
                {
                    text.Position = mpw.Z20();//mpw为鼠标移动变量；
                });
                moveText.DatabaseEntityDraw(worldDraw => worldDraw.Geometry.Draw(text));//绘制文字；
                moveText.SetOptions(msg: "\n请指定圆形洞口的终点");//提示用户输入第二点；
                var r2 = Env.Editor.Drag(moveText);//拿到第二点
                if (r2.Status != PromptStatus.OK) tr.Abort();
                tr.CurrentSpace.AddEntity(text);
                tr.Commit();
                Env.Editor.Redraw();
            }
            Env.Editor.WriteMessage($"\n多边形面积为: {area:F2}");
        }

        /// <summary>
        /// 计算多边形面积（Shoelace公式）
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private double CalculatePolygonArea(List<Point2d> pts)
        {
            double area = 0;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Point2d p1 = pts[i];
                Point2d p2 = pts[(i + 1) % n];
                area += (p1.X * p2.Y - p2.X * p1.Y);
            }
            return System.Math.Abs(area) / 2.0 / 100 / 100 / 100;
        }

        /// <summary>
        /// 计算多边形质心
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private Point2d CalculateCentroid(List<Point2d> pts)
        {
            double cx = 0, cy = 0;
            double area = 0;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Point2d p0 = pts[i];
                Point2d p1 = pts[(i + 1) % n];
                double cross = p0.X * p1.Y - p1.X * p0.Y;
                cx += (p0.X + p1.X) * cross;
                cy += (p0.Y + p1.Y) * cross;
                area += cross;
            }
            area = area / 2.0;
            cx = cx / (6 * area);
            cy = cy / (6 * area);
            return new Point2d(cx, cy);
        }

        #endregion

        /// <summary>
        /// 工艺与暖通标注文字
        /// </summary>
        /// <param name="layerName"></param>
        [CommandMethod(nameof(DBTextLabel))]
        public static void DBTextLabel()
        {
            try
            {
                using var tr = new DBTrans();
                TextStyleAndLayerInfo(VariableDictionary.btnBlockLayer, Convert.ToInt16(VariableDictionary.layerColorIndex), "tJText");

                //创建文字与文字属性
                DBText text = new DBText()
                {
                    TextStyleId = tr.TextStyleTable["tJText"],
                    TextString = VariableDictionary.btnFileName,//字体的内容
                    Height = 350,//字体的高度
                    WidthFactor = 0.8,//字体的宽度因子
                    ColorIndex = Convert.ToInt16(VariableDictionary.layerColorIndex),//字体的颜色
                    Layer = VariableDictionary.btnBlockLayer,//字体的图层
                };
                if (VariableDictionary.btnBlockLayer.Contains("工艺专业"))
                {
                    text.Height = 250;
                }
                var dbTextEntityObj = tr.CurrentSpace.AddEntity(text);//写入当前空间
                var startPoint = new Point3d(0, 0, 0);
                double tempAngle = 0;//角度
                var entityBBText = new JigEx((mpw, _) =>
                {
                    text.Move(startPoint, mpw);
                    startPoint = mpw;
                    if (VariableDictionary.entityRotateAngle == tempAngle)
                    {
                        return;
                    }
                    else if (VariableDictionary.entityRotateAngle != tempAngle)
                    {
                        text.Rotation(center: mpw, 0);
                        tempAngle = VariableDictionary.entityRotateAngle;
                        text.Rotation(center: mpw, tempAngle);
                    }
                });
                entityBBText.DatabaseEntityDraw(wd => wd.Geometry.Draw(text));
                entityBBText.SetOptions(msg: "\n指定插入点");
                //entityBlock.SetOptions(startPoint, msg: "\n指定插入点");这个startpoint，是有个参考线在里面，用于托拽时的辅助；
                var endPoint = Env.Editor.Drag(entityBBText);
                if (endPoint.Status != PromptStatus.OK)
                    tr.Abort();
                tr.Commit();
                Env.Editor.Redraw();//重新刷新
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("工艺与暖通标注文字失败！");
                Env.Editor.WriteMessage($"\n错误信息：{ex.Message}");
            }
        }

        #region 生成外轮廓方法
        #region 基础方法
        //public class OuterContourCommands
        //{
        //    [CommandMethod("GenerateOuterContour")]
        //    public void GenerateOuterContour()
        //    {
        //        // 获取当前文档和数据库  
        //        Document doc = Application.DocumentManager.MdiActiveDocument;
        //        Database db = doc.Database;
        //        Editor ed = doc.Editor;

        //        try
        //        {
        //            // 提示用户选择对象  
        //            PromptSelectionResult selResult = ed.GetSelection();
        //            if (selResult.Status != PromptStatus.OK)
        //                return;

        //            // 获取选择集  
        //            SelectionSet selSet = selResult.Value;
        //            ObjectId[] objIds = selSet.GetObjectIds();

        //            // 如果没有对象被选中，退出  
        //            if (objIds.Length == 0)
        //            {
        //                ed.WriteMessage("\n没有对象被选中。");
        //                return;
        //            }

        //            ed.WriteMessage($"\n已选择 {objIds.Length} 个对象，开始处理...");

        //            using (Transaction tr = db.TransactionManager.StartTransaction())
        //            {
        //                // 创建图层（可选）  
        //                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
        //                string contourLayerName = "OuterContour";

        //                // 如果轮廓层不存在，则创建  
        //                if (!lt.Has(contourLayerName))
        //                {
        //                    lt.UpgradeOpen();
        //                    LayerTableRecord ltr = new LayerTableRecord();
        //                    ltr.Name = contourLayerName;
        //                    ltr.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0); // 红色  
        //                    lt.Add(ltr);
        //                    tr.AddNewlyCreatedDBObject(ltr, true);
        //                }

        //                // 处理选中对象们的外轮廓  
        //                ProcessOuterContours(doc, db, tr, objIds);

        //                tr.Commit();
        //            }

        //            ed.WriteMessage("\n外轮廓线生成完成！");
        //        }
        //        catch (System.Exception ex)
        //        {
        //            ed.WriteMessage($"\n错误: {ex.Message}");
        //        }
        //    }

        //    private void ProcessOuterContours(Document doc, Database db, Transaction tr, ObjectId[] objIds)
        //    {
        //        Editor ed = doc.Editor;
        //        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

        //        // 对每个选中的对象生成单独的外轮廓  
        //        foreach (ObjectId objId in objIds)
        //        {
        //            Entity entity = tr.GetObject(objId, OpenMode.ForRead) as Entity;
        //            if (entity == null) continue;

        //            // 创建一个新的选择集，只包含当前实体  
        //            ObjectIdCollection singleEntityCollection = new ObjectIdCollection();
        //            singleEntityCollection.Add(objId);

        //            // 生成边界（外轮廓）  
        //            Point3dCollection boundaryPoints = GenerateBoundary(doc, db, tr, singleEntityCollection);
        //            if (boundaryPoints != null && boundaryPoints.Count > 0)
        //            {
        //                // 对边界进行偏移处理  
        //                Point3dCollection offsetPoints = OffsetBoundary(boundaryPoints, 10.0);
        //                // 创建轮廓线  
        //                CreateContourPolyline(db, tr, btr, offsetPoints);
        //            }
        //        }
        //    }

        //    private Point3dCollection GenerateBoundary(Document doc, Database db, Transaction tr, ObjectIdCollection objIds)
        //    {
        //        Editor ed = doc.Editor;

        //        try
        //        {
        //            // 创建一个边界点集合  
        //            Point3dCollection points = new Point3dCollection();

        //            // 创建一个虚拟的填充对象来获取边界  
        //            using (Hatch hatch = new Hatch())
        //            {
        //                // 添加到当前空间以便计算边界  
        //                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
        //                btr.AppendEntity(hatch);
        //                tr.AddNewlyCreatedDBObject(hatch, true);

        //                // 设置填充图案 (只用来生成边界，不实际显示)  
        //                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        //                hatch.Associative = false;

        //                // 计算边界 (使用External类型，这会生成外部轮廓)  
        //                hatch.AppendLoop(HatchLoopTypes.External, objIds);

        //                // 获取边界对象  
        //                // 修复: 使用正确的API调用方式获取边界循环  
        //                int loopCount = hatch.NumberOfLoops;
        //                if (loopCount == 0)
        //                {
        //                    // 从图纸中删除临时填充对象  
        //                    hatch.Erase();
        //                    return null;
        //                }

        //                // 获取第一个循环的边界对象  
        //                // 遍历所有边界  
        //                for (int i = 0; i < loopCount; i++)
        //                {
        //                    // 只处理外部轮廓  
        //                    HatchLoopTypes loopType = hatch.GetLoopAt(i);
        //                    //HatchLoopTypes loopType1 = Autodesk.AutoCAD.DatabaseServices.HatchLoopTypes..hatch.GetLoopAt(i);
        //                    if (loopType != HatchLoopTypes.External) continue;

        //                    // 获取当前循环的曲线  
        //                    Curve2dCollection curves = new Curve2dCollection();
        //                    hatch.GetLoopAt(i, curves);

        //                    // 转换2D曲线到3D空间并提取点  
        //                    foreach (Curve2d curve2d in curves)
        //                    {
        //                        // 采样点数量 - 根据曲线类型和大小调整  
        //                        int numSamples = 20; // 可根据需要调整  

        //                        // 获取曲线参数范围  
        //                        ParameterInterval interval = curve2d.GetInterval();
        //                        double start = interval.LowerBound;
        //                        double end = interval.UpperBound;
        //                        double step = (end - start) / numSamples;

        //                        // 对曲线采样获取点  
        //                        for (int j = 0; j <= numSamples; j++)
        //                        {
        //                            double param = start + j * step;
        //                            Point2d pt2d = curve2d.EvaluatePoint(param);

        //                            // 将2D点转换回3D空间坐标  
        //                            Point3d pt3d = new Point3d(pt2d.X, pt2d.Y, 0);
        //                            pt3d = pt3d.TransformBy(hatch.ObjectTransform);

        //                            points.Add(pt3d);
        //                        }
        //                    }
        //                }

        //                // 从图纸中删除临时填充对象  
        //                hatch.Erase();

        //                return points;
        //            }
        //        }
        //        catch (System.Exception ex)
        //        {
        //            ed.WriteMessage($"\n生成边界时出错: {ex.Message}");
        //            return null;
        //        }
        //    }

        //    private Point3dCollection OffsetBoundary(Point3dCollection boundaryPoints, double offsetDistance)
        //    {
        //        // 如果边界点不足，不执行偏移  
        //        if (boundaryPoints == null || boundaryPoints.Count < 3)
        //            return boundaryPoints;

        //        // 创建一个临时多段线用于偏移计算  
        //        Polyline tempPoly = new Polyline();
        //        for (int i = 0; i < boundaryPoints.Count; i++)
        //        {
        //            Point3d pt = boundaryPoints[i];
        //            tempPoly.AddVertexAt(i, new Point2d(pt.X, pt.Y), 0, 0, 0);
        //        }

        //        // 闭合多段线  
        //        tempPoly.Closed = true;

        //        // 执行偏移操作 - 正值向外偏移  
        //        DBObjectCollection offsetCurves = tempPoly.GetOffsetCurves(offsetDistance);

        //        // 如果偏移失败或没有结果，返回原始点集  
        //        if (offsetCurves.Count == 0)
        //            return boundaryPoints;

        //        // 从偏移结果中获取点集合  
        //        Point3dCollection offsetPoints = new Point3dCollection();
        //        foreach (Entity ent in offsetCurves)
        //        {
        //            if (ent is Polyline offsetPoly)
        //            {
        //                // 提取偏移多段线的所有点  
        //                for (int i = 0; i < offsetPoly.NumberOfVertices; i++)
        //                {
        //                    Point2d pt2d = offsetPoly.GetPoint2dAt(i);
        //                    offsetPoints.Add(new Point3d(pt2d.X, pt2d.Y, 0));
        //                }
        //            }
        //        }

        //        // 释放临时对象  
        //        tempPoly.Dispose();
        //        foreach (DBObject obj in offsetCurves)
        //        {
        //            obj.Dispose();
        //        }

        //        return offsetPoints;
        //    }

        //    private void CreateContourPolyline(Database db, Transaction tr, BlockTableRecord btr, Point3dCollection points)
        //    {
        //        // 如果点集合为空，则不创建多段线  
        //        if (points == null || points.Count < 2)
        //            return;

        //        // 创建新的多段线对象  
        //        Polyline pline = new Polyline();
        //        pline.Layer = "OuterContour"; // 设置多段线的图层  

        //        // 添加顶点  
        //        for (int i = 0; i < points.Count; i++)
        //        {
        //            Point3d pt = points[i];
        //            pline.AddVertexAt(i, new Point2d(pt.X, pt.Y), 0, 0, 0);
        //        }

        //        // 闭合多段线  
        //        pline.Closed = true;

        //        // 设置多段线的属性  
        //        pline.ConstantWidth = 0.5; // 多段线宽度  
        //        pline.ColorIndex = 1;     // 红色  

        //        // 将多段线添加到当前空间  
        //        btr.AppendEntity(pline);
        //        tr.AddNewlyCreatedDBObject(pline, true);
        //    }
        //}

        #endregion

        #region 方法一：

        [CommandMethod("SMARTOUTLINE")]
        public void SmartOutline()
        {
            // 获取CAD当前文档、数据库和命令行  
            Document doc = Application.DocumentManager.MdiActiveDocument; // 当前文档  
            Database db = doc.Database;                                   // 当前数据库  
            Editor ed = doc.Editor;                                       // 获取命令行交互对象  

            // 让用户选择对象  
            PromptSelectionOptions selOpt = new PromptSelectionOptions(); // 定义选择框选参数  
            selOpt.MessageForAdding = "\n请选择要生成轮廓的图元：";    // 提示消息  
            PromptSelectionResult selRes = ed.GetSelection(selOpt);      // 获取用户选择的对象  
            if (selRes.Status != PromptStatus.OK) return;                // 如果用户没有选择，退出方法  

            using (Transaction tr = db.TransactionManager.StartTransaction()) // 开始数据库事务  
            {
                // 打开模型空间用于添加新对象  
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable; // 获取块表  
                BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord; // 获取模型空间记录  

                // 1. 收集用户所选的全部曲线并爆炸块参照  
                List<Curve> allCurves = new List<Curve>(); // 存储所有选择的曲线对象  
                foreach (SelectedObject selectItem in selRes.Value) // 遍历所有选中的对象  
                {
                    if (selectItem == null) continue; // 判断是否为空选择  
                    Entity entity = tr.GetObject(selectItem.ObjectId, OpenMode.ForRead) as Entity; // 获取实体对象  
                    if (entity == null) continue; // 如果无法获取实体，则跳过  
                    CollectAllCurves(entity, tr, allCurves, 0); // 递归收集当前实体中的所有曲线  
                }
                if (allCurves.Count == 0) // 如果没有找到有效曲线  
                {
                    ed.WriteMessage("\n未找到可以处理的曲线或块对象。"); // 提示消息  
                    return; // 退出  
                }

                // 2. 分组。不连续的图元单独分组（通过包围盒判断是否相交或几乎相交）  
                List<List<Curve>> curveGroups = GroupConnectedCurves(allCurves, 0.001); // 将曲线按连接性分组  

                // 依次为每组生成外轮廓  
                int groupIndex = 0; // 定义组索引  
                foreach (var curveGroup in curveGroups) // 遍历所有分组  
                {
                    groupIndex++; // 递增组索引  
                    if (curveGroup.Count == 0) continue; // 如果当前组没有曲线，跳过  

                    // 3. 该分组的联合包围盒最小点minP  
                    Extents3d curveGroupExt = GetBoundingExtentsFromCurves(curveGroup); // 获取分组的包围盒  
                    Point3d minP = curveGroupExt.MinPoint; // 获取包围盒的最小点  

                    // 4. 沿最小点顺时针扫描一圈，采样最外侧交点  
                    List<Point2d> edgePoints = new List<Point2d>(); // 存储外边缘点  
                    double step = 2 * Math.PI / 365; // 计算每个采样点的角度  
                    for (int i = 0; i < 365; i++) // 循环进行采样  
                    {
                        double ang = i * step; // 当前采样的角度  
                        Vector2d dir = new Vector2d(Math.Cos(ang), Math.Sin(ang)); // 计算此方向的单位向量  
                        Point2d rayStart = new Point2d(minP.X, minP.Y); // 射线起点为最小点  
                        Point2d rayEnd = new Point2d(minP.X + dir.X * 1e4, minP.Y + dir.Y * 1e4); // 射线终点非常远（假设足够长）  
                        Point2d? pt = FindOuterBoundaryPoint(curveGroup, rayStart, rayEnd); // 查找此方向的外边界点  
                        if (pt != null) edgePoints.Add(pt.Value); // 如果找到有效点，则将其添加到列表  
                    }

                    // 若有效点不足3 个不能生成多段线  
                    if (edgePoints.Count < 3) // 如果边缘点低于3个  
                    {
                        ed.WriteMessage($"\n分组{groupIndex}点数不足无法生成多段线。"); // 提示信息  
                        continue; // 跳过当前分组  
                    }

                    // 5. 用所有顺时针点组成闭合多段线  
                    Polyline pl = new Polyline(); // 创建多段线对象  
                    for (int j = 0; j < edgePoints.Count; j++) // 遍历所有的外边缘点  
                        pl.AddVertexAt(j, edgePoints[j], 0, 0, 0); // 将点添加为多段线的顶点  
                    pl.Closed = true; // 闭合多段线  
                    pl.ColorIndex = groupIndex % 7 + 1; // 给不同组不同颜色（循环使用1-7的颜色）  
                    ms.AppendEntity(pl); // 将多段线添加到模型空间  
                    tr.AddNewlyCreatedDBObject(pl, true); // 将新对象注册到当前事务  

                    ed.WriteMessage($"\n第{groupIndex}组轮廓已生成。"); // 输出生成信息  
                }
                // 释放克隆的曲线资源  
                foreach (var c in allCurves) c.Dispose(); // 释放内存  

                tr.Commit(); // 提交事务  
                ed.WriteMessage("\n全部轮廓生成完成！"); // 输出完成信息  
            }
        }

        //--------↓ 曲线分组、几何方法（每行详细注释） ------------  

        /// <summary>  
        /// 分组：把空间相连/重叠/紧挨的曲线归为一组  
        /// </summary>  
        /// <param name="curves">所有曲线列表</param>  
        /// <param name="threshold">连接阈值</param>  
        /// <returns>分组后的曲线列表</returns>  
        private List<List<Curve>> GroupConnectedCurves(List<Curve> curves, double threshold)
        {
            List<List<Curve>> groups = new List<List<Curve>>(); // 保存分组  
            HashSet<Curve> processed = new HashSet<Curve>(); // 跟踪已处理的曲线  
            foreach (Curve curve in curves) // 遍历所有曲线  
            {
                if (processed.Contains(curve)) continue; // 已处理则跳过  
                Queue<Curve> queue = new Queue<Curve>(); // 用队列进行分组  
                queue.Enqueue(curve); // 将当前曲线入队  
                processed.Add(curve); // 将当前曲线标记为已处理  
                List<Curve> group = new List<Curve>(); // 创建当前组的曲线列表  
                while (queue.Count > 0) // 处理队列中的曲线  
                {
                    Curve current = queue.Dequeue(); // 出队当前曲线  
                    group.Add(current); // 将曲线添加到当前组  
                    foreach (Curve other in curves) // 遍历所有曲线  
                    {
                        if (processed.Contains(other)) continue; // 已处理则跳过  
                        if (AreConnected(current, other, threshold)) // 判断是否连接  
                        {
                            queue.Enqueue(other); // 入队连接的曲线  
                            processed.Add(other); // 标记为已处理  
                        }
                    }
                }
                groups.Add(group); // 添加当前分组  
            }
            return groups; // 返回分组列表  
        }

        // 判定两曲线包围盒是否碰到或足够接近  
        private bool AreConnected(Curve c1, Curve c2, double threshold)
        {
            try
            {
                if (!c1.Bounds.HasValue || !c2.Bounds.HasValue) return false; // 如果无包围盒则返回false  

                Extents3d ext1 = c1.Bounds.Value; // 获取曲线1的包围盒  
                Extents3d ext2 = c2.Bounds.Value; // 获取曲线2的包围盒  

                // 扩展后的包围盒1（加上阈值）  
                Point3d ext1Min = new Point3d(
                    ext1.MinPoint.X - threshold, // 扩展最小点  
                    ext1.MinPoint.Y - threshold,
                    ext1.MinPoint.Z - threshold);
                Point3d ext1Max = new Point3d(
                    ext1.MaxPoint.X + threshold, // 扩展最大点  
                    ext1.MaxPoint.Y + threshold,
                    ext1.MaxPoint.Z + threshold);

                // 判断点是否在扩展包围盒内  
                bool contains1to2Min = IsPointInBox(ext2.MinPoint, ext1Min, ext1Max); // 判断曲线2最小点是否在扩展包围盒1中  
                bool contains1to2Max = IsPointInBox(ext2.MaxPoint, ext1Min, ext1Max); // 判断曲线2最大点是否在扩展包围盒1中  
                bool contains2to1Min = IsPointInBox(ext1Min, ext2.MinPoint, ext2.MaxPoint); // 判断扩展包围盒1最小点是否在包围盒2中  
                bool contains2to1Max = IsPointInBox(ext1Max, ext2.MinPoint, ext2.MaxPoint); // 判断扩展包围盒1最大点是否在包围盒2中  

                // 或者检查包围盒相交（任一极点被包含，或包围盒交叉）  
                return contains1to2Min || contains1to2Max || contains2to1Min || contains2to1Max ||
                       DoBoxesIntersect(ext1Min, ext1Max, ext2.MinPoint, ext2.MaxPoint); // 检查包围盒相交  
            }
            catch { return false; } // 出现异常时返回false  
        }

        // 判断点是否在包围盒内  
        private bool IsPointInBox(Point3d pt, Point3d boxMin, Point3d boxMax)
        {
            // 检查点的坐标是否在包围盒的范围内  
            return (pt.X >= boxMin.X && pt.X <= boxMax.X &&
                    pt.Y >= boxMin.Y && pt.Y <= boxMax.Y &&
                    pt.Z >= boxMin.Z && pt.Z <= boxMax.Z);
        }

        // 判断两个包围盒是否相交  
        private bool DoBoxesIntersect(Point3d box1Min, Point3d box1Max, Point3d box2Min, Point3d box2Max)
        {
            // 只要有一个轴向不相交，整体就不相交  
            return !(box1Max.X < box2Min.X || box1Min.X > box2Max.X ||
                     box1Max.Y < box2Min.Y || box1Min.Y > box2Max.Y ||
                     box1Max.Z < box2Min.Z || box1Min.Z > box2Max.Z);
        }

        /// <summary>  
        /// 收集实体（含块内容）所有曲线，递归爆炸，深度防止栈溢出  
        /// </summary>  
        /// <param name="entity">实体对象</param>  
        /// <param name="tr">开启事务</param>  
        /// <param name="curveList">曲线列队</param>  
        /// <param name="depth">递归次数</param>  
        private void CollectAllCurves(Entity entity, Transaction tr, List<Curve> curveList, int depth = 0)
        {
            if (depth > 10) return; // 最多递归10层，防止栈溢出  
            if (entity is Curve curve)
                curveList.Add((Curve)curve.Clone()); // 克隆当前曲线并添加到列表  
            else if (entity is BlockReference br)
            {
                using (var subObjs = new DBObjectCollection()) // 用于存储块爆炸后的对象  
                {
                    br.Explode(subObjs); // 爆炸块参照，获取内部对象  
                    foreach (DBObject dBObject in subObjs) // 遍历爆炸后的对象  
                    {
                        if (dBObject is Entity se)
                            CollectAllCurves(se, tr, curveList, depth + 1); // 递归收集曲线   
                        dBObject.Dispose(); // 释临时对象  
                    }
                }
            }
        }

        // 获得曲线列表包围盒，取全部包围  
        private Extents3d GetBoundingExtentsFromCurves(List<Curve> curves)
        {
            bool first = true; // 初始状态标记  
            Extents3d ext = new Extents3d(); // 初始化包围盒  
            foreach (Curve c in curves)
            {
                try
                {
                    if (!c.Bounds.HasValue) continue; // 如果无包围盒则跳过  
                    if (first)
                    {
                        ext = c.Bounds.Value; // 第一个曲线的包围盒赋值  
                        first = false; // 更新状态  
                    }
                    else
                    {
                        ext.AddExtents(c.Bounds.Value); // 将后续的包围盒与已有的包围盒合并  
                    }
                }
                catch { } // 捕获异常并继续  
            }
            return ext; // 返回组合后的包围盒  
        }

        // 在所有曲线上查找射线最近点  
        private Point2d? FindOuterBoundaryPoint(List<Curve> curves, Point2d rayStart, Point2d rayEnd)
        {
            Point2d? bestPt = null; // 存储找到的最佳点  
            double maxDist = double.MinValue; // 初始化最大距离  
            foreach (Curve cv in curves) // 遍历所有曲线  
            {
                try
                {
                    Point3dCollection pts = new Point3dCollection(); // 用于存储交点  
                    cv.IntersectWith( // 查找交点  
                        new Line(new Point3d(rayStart.X, rayStart.Y, 0), new Point3d(rayEnd.X, rayEnd.Y, 0)),
                        Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                    foreach (Point3d p in pts) // 遍历所有交点  
                    {
                        double dist = (p.X - rayStart.X) * (p.X - rayStart.X) + // 计算二次距离  
                                      (p.Y - rayStart.Y) * (p.Y - rayStart.Y);
                        if (dist > maxDist) // 找到了更远的点  
                        {
                            maxDist = dist; // 更新最大距离  
                            bestPt = new Point2d(p.X, p.Y); // 更新最佳点  
                        }
                    }
                }
                catch { } // 某些实体不支持交点会报错，直接忽略  
            }
            return bestPt; // 返回最佳点  
        }
        #endregion
        ///////////////////////////////////////////////////////////////////




        #endregion

        #region 模型空间检查图层


        /// <summary>
        /// 关闭图层
        /// </summary>
        [CommandMethod(nameof(CloseLayer))]
        public static void CloseLayer()
        {
            try
            {
                using var tr = new DBTrans();
                if (VariableDictionary.btnState)
                {
                    SaveAllTjLayerStates();//保存所有条件图层状态
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n已保存 {layerOriStates.Count} 个图层的初始状态");
                    // 打开图层表  
                    //var layerTable = tr.LayerTable;

                    // 遍历图层并进行一些修改（这里以关闭部分图层为例）  
                    foreach (var layerId in layerOriStates)
                    {
                        //块表记录
                        LayerTableRecord layerRecord = tr.GetObject(layerId.ObjectId, OpenMode.ForWrite) as LayerTableRecord;

                        if (VariableDictionary.allTjtLayer.Contains(layerRecord.Name))
                        {
                            layerRecord.IsOff = true;//关闭图层
                        }
                    }
                }
                else
                {
                    // 还原图层状态  
                    RestoreLayerStates(layerOriStates);
                }
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("关闭图层失败！");
                Env.Editor.WriteMessage($"\n错误信息：{ex.Message}");
            }
        }

        /// <summary>  
        /// 图层状态信息类，用于存储单个图层的详细状态  
        /// </summary>  
        public class LayerState
        {
            public ObjectId ObjectId { get; set; }
            public string? Name { get; set; }            // 图层名称  
            public bool IsOff { get; set; }             // 图层是否关闭  
            public bool IsFrozen { get; set; }          // 图层是否冻结  
            public bool IsLocked { get; set; }          // 图层是否锁定  
            public bool IsPlottable { get; set; }       // 图层是否可打印  
            public short ColorIndex { get; set; }       // 图层颜色索引  
            public LineWeight LineWeight { get; set; }  // 图层线宽  
            public string? PlotStyleName { get; set; }   // 图层打印样式名称
            public DrawStream? DrawStream { get; set; } // 图层绘制数据
            public bool IsHidden { get; set; } // 图层是否隐藏
            public bool IsReconciled { get; set; } // 图层是否同步
            public ObjectId LinetypeObjectId { get; set; } // 图层线型对象ID
            public ObjectId MaterialId { get; set; } // 图层材质ID
            public DuplicateRecordCloning MergeStyle { get; set; } // 图层合并样式
            public ObjectId OwnerId { get; set; } // 图层所有者ID
            public ObjectId PlotStyleNameId { get; set; } // 图层打印样式名称ID
            public Transparency Transparency { get; set; } // 图层透明度
            public bool ViewportVisibilityDefault { get; set; } // 图层视图可见性默认值
            public ResultBuffer? XData { get; set; } // 图层X数据
            public AnnotativeStates Annotative { get; set; } // 图层注释
            public string? Description { get; set; } // 图层描述
            public bool HasSaveVersionOverride { get; set; } // 图层保存版本覆盖
        }

        /// <summary>
        /// 创建用于存储图层状态的列表  
        /// </summary>
        public static List<LayerState> layerOriStates = new List<LayerState>();

        /// <summary>  
        /// 保存当前文档中所有图层的状态  
        /// </summary>  
        /// <returns>图层状态列表</returns>  
        public static void SaveLayerStates()
        {
            // 获取当前活动文档的数据库  
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            // 使用事务读取图层信息  
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 清空储存图层列表变量
                    layerOriStates.Clear();
                    // 打开图层表  
                    LayerTable layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    // 遍历所有图层  
                    foreach (ObjectId layerId in layerTable)
                    {
                        // 获取图层表记录  
                        LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                        // 创建并保存图层状态  
                        LayerState state = new LayerState
                        {
                            ObjectId = layerRecord.ObjectId,
                            Name = layerRecord.Name,//图层名称
                            IsOff = layerRecord.IsOff,//图层是否关闭
                            #region 图层其它属性
                            IsFrozen = layerRecord.IsFrozen, // 图层是否冻结
                            //IsLocked = layerRecord.IsLocked,// 图层是否锁定
                            //IsPlottable = layerRecord.IsPlottable,// 图层是否可打印  
                            ColorIndex = layerRecord.Color.ColorIndex,// 图层颜色索引  
                            //LineWeight = layerRecord.LineWeight, // 图层线宽  
                            //PlotStyleName = layerRecord.PlotStyleName,// 图层打印样式名称
                            //PlotStyleNameId = layerRecord.PlotStyleNameId,// 图层打印样式名称ID
                            //DrawStream = layerRecord.DrawStream,// 图层绘制数据
                            //IsHidden = layerRecord.IsHidden,//   图层是否隐藏
                            //IsReconciled = layerRecord.IsReconciled,//   图层是否同步
                            //LinetypeObjectId = layerRecord.LinetypeObjectId,// 图层线型对象ID
                            //MaterialId = layerRecord.MaterialId,// 图层材质ID
                            //MergeStyle = layerRecord.MergeStyle,// 图层合并样式
                            //OwnerId = layerRecord.OwnerId,// 图层所有者ID
                            //Transparency = layerRecord.Transparency,// 图层透明度
                            //ViewportVisibilityDefault = layerRecord.ViewportVisibilityDefault,// 图层视图可见性默认值
                            //XData = layerRecord.XData,// 图层X数据
                            //Annotative = layerRecord.Annotative,// 图层注释
                            //Description = layerRecord.Description,// 图层描述
                            //HasSaveVersionOverride = layerRecord.HasSaveVersionOverride,// 图层保存版本覆盖
                            #endregion
                        };
                        layerOriStates.Add(state);
                    }
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    // 处理可能的异常  
                    Application.ShowAlertDialog($"保存图层状态时发生错误：{ex.Message}");
                    tr.Abort();
                }
            }
        }

        /// <summary>  
        /// 保存当前文档中所有图层的状态  
        /// </summary>  
        /// <returns>图层状态列表</returns>  
        public static void SaveAllTjLayerStates()
        {
            // 使用事务读取图层信息  
            using (DBTrans tr = new())
            {
                try
                {
                    layerOriStates.Clear();
                    // 打开图层表  
                    var layerTable = tr.LayerTable;
                    // 遍历所有图层  
                    foreach (ObjectId layerId in layerTable)
                    {
                        // 获取图层表记录  
                        LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;

                        if (VariableDictionary.allTjtLayer.Contains(layerRecord.Name))
                        {
                            // 创建并保存图层状态  
                            LayerState state = new LayerState
                            {
                                ObjectId = layerRecord.ObjectId,
                                Name = layerRecord.Name,
                                IsOff = layerRecord.IsOff,
                                #region 图层其它属性
                                IsFrozen = layerRecord.IsFrozen, // 图层是否冻结
                                //IsLocked = layerRecord.IsLocked,// 图层是否锁定
                                //IsPlottable = layerRecord.IsPlottable,// 图层是否可打印  
                                //ColorIndex = layerRecord.Color.ColorIndex,// 图层颜色索引  
                                //LineWeight = layerRecord.LineWeight, // 图层线宽  
                                //PlotStyleName = layerRecord.PlotStyleName,// 图层打印样式名称
                                //PlotStyleNameId = layerRecord.PlotStyleNameId,// 图层打印样式名称ID
                                //DrawStream = layerRecord.DrawStream,// 图层绘制数据
                                //IsHidden = layerRecord.IsHidden,//   图层是否隐藏
                                //IsReconciled = layerRecord.IsReconciled,//   图层是否同步
                                //LinetypeObjectId = layerRecord.LinetypeObjectId,// 图层线型对象ID
                                //MaterialId = layerRecord.MaterialId,// 图层材质ID
                                //MergeStyle = layerRecord.MergeStyle,// 图层合并样式
                                //OwnerId = layerRecord.OwnerId,// 图层所有者ID
                                //Transparency = layerRecord.Transparency,// 图层透明度
                                //ViewportVisibilityDefault = layerRecord.ViewportVisibilityDefault,// 图层视图可见性默认值
                                //XData = layerRecord.XData,// 图层X数据
                                //Annotative = layerRecord.Annotative,// 图层注释
                                //Description = layerRecord.Description,// 图层描述
                                //HasSaveVersionOverride = layerRecord.HasSaveVersionOverride,// 图层保存版本覆盖
                                #endregion
                            };
                            layerOriStates.Add(state);
                        }
                    }
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    // 处理可能的异常  
                    Application.ShowAlertDialog($"保存图层状态时发生错误：{ex.Message}");
                    tr.Abort();
                }
            }
        }

        /// <summary>  
        /// 还原之前保存的图层状态  
        /// </summary>  
        /// <param name="savedLayerStates">之前保存的图层状态列表</param>  
        public static void RestoreLayerStates(List<LayerState> savedLayerStates)
        {
            // 获取当前活动文档的数据库  
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            // 使用事务还原图层状态  
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 打开图层表  
                    LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                    // 遍历保存的图层状态  
                    foreach (LayerState state in savedLayerStates)
                    {
                        // 检查图层是否存在  
                        if (layerTable.Has(state.Name))
                        {
                            // 获取图层表记录并打开写入模式  
                            ObjectId layerId = layerTable[state.Name];
                            LayerTableRecord layerRecord = trans.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                            // 还原图层状态  
                            layerRecord.IsOff = state.IsOff;
                            #region 图层的其它属性
                            layerRecord.IsFrozen = state.IsFrozen; // 图层是否冻结
                            layerRecord.IsLocked = state.IsLocked;// 图层是否锁定
                            //layerRecord.IsPlottable = state.IsPlottable;//图层是否可打印  
                            //layerRecord.Color = Color.FromColorIndex(ColorMethod.ByAci, state.ColorIndex);// 还原颜色和线宽  
                            //layerRecord.LineWeight = state.LineWeight;// 图层线宽 
                            #endregion
                            /*
                              ObjectId = layerRecord.ObjectId,
                                Name = layerRecord.Name,
                                IsOff = layerRecord.IsOff,
                                #region 图层其它属性
                                IsFrozen = layerRecord.IsFrozen, // 图层是否冻结
                                IsLocked = layerRecord.IsLocked,// 图层是否锁定
                                IsPlottable = layerRecord.IsPlottable,// 图层是否可打印  
                                ColorIndex = layerRecord.Color.ColorIndex,// 图层颜色索引  
                                LineWeight = layerRecord.LineWeight, // 图层线宽  
                                PlotStyleName = layerRecord.PlotStyleName,// 图层打印样式名称
                                PlotStyleNameId = layerRecord.PlotStyleNameId,// 图层打印样式名称ID
                                DrawStream = layerRecord.DrawStream,// 图层绘制数据
                                IsHidden = layerRecord.IsHidden,//   图层是否隐藏
                                IsReconciled = layerRecord.IsReconciled,//   图层是否同步
                                LinetypeObjectId = layerRecord.LinetypeObjectId,// 图层线型对象ID
                                MaterialId = layerRecord.MaterialId,// 图层材质ID
                                MergeStyle = layerRecord.MergeStyle,// 图层合并样式
                                OwnerId = layerRecord.OwnerId,// 图层所有者ID
                                Transparency = layerRecord.Transparency,// 图层透明度
                                ViewportVisibilityDefault = layerRecord.ViewportVisibilityDefault,// 图层视图可见性默认值
                                XData = layerRecord.XData,// 图层X数据
                                Annotative = layerRecord.Annotative,// 图层注释
                                Description = layerRecord.Description,// 图层描述
                                HasSaveVersionOverride = layerRecord.HasSaveVersionOverride,// 图层保存版本覆盖
                                #endregion
                             */
                        }
                    }

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    // 处理可能的异常  
                    Application.ShowAlertDialog($"还原图层状态时发生错误：{ex.Message}");
                    trans.Abort();
                }
            }
        }

        // 静态变量：记录是否已经删除图层
        private static bool _isLayersDeleted = false;

        // 静态变量：存储原始图层信息
        private static Dictionary<string, LayerState> _originalLayerInfos;

        /// <summary>
        /// 图层删除和恢复命令
        /// </summary>
        [CommandMethod("ToggleLayerDeletion")]
        public static void ToggleLayerDeletion()
        {
            // 获取当前文档和数据库
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 使用事务处理
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 如果尚未删除图层，执行删除操作
                    if (!_isLayersDeleted)
                    {
                        // 提示用户选择要保留的图层
                        PromptEntityResult selectedLayerResult = ed.GetEntity("请选择要保留的图层");
                        if (selectedLayerResult.Status != PromptStatus.OK)
                        {
                            ed.WriteMessage("\n未选择图层，操作取消。");
                            return;
                        }
                        // 打开图层表
                        LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                        // 获取选中的图层名称
                        Entity selectEntity = trans.GetObject(selectedLayerResult.ObjectId, OpenMode.ForRead) as Entity;
                        //LayerTable selectLayerTable = trans.GetObject(selectEntity.ObjectId, OpenMode.ForRead) as LayerTable;
                        //LayerTableRecord selectedLayer = trans.GetObject(selectLayerTable.ObjectId, OpenMode.ForRead) as LayerTableRecord;

                        //LayerTableRecord selectedLayer1 = trans.GetObject(selectedLayerResult.ObjectId, OpenMode.ForRead) as LayerTableRecord;
                        if (selectEntity == null)
                        {
                            ed.WriteMessage("\n未找到图层，操作取消。");
                            return;
                        }
                        //LayerTableRecord selectedLayer = trans.GetObject(selectLayerTable.ObjectId, OpenMode.ForRead) as LayerTableRecord;
                        string preserveLayerName = selectEntity.Layer;

                        // 初始化原始图层信息字典
                        _originalLayerInfos = new Dictionary<string, LayerState>();

                        // 准备删除图层的事务
                        trans.GetObject(db.LayerTableId, OpenMode.ForWrite);

                        // 遍历所有图层
                        foreach (ObjectId layerId in layerTable)
                        {
                            LayerTableRecord layer = trans.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;

                            // 跳过0图层和选中的图层
                            if (layer.Name == "0" || layer.Name == "Defpoints" || layer.Name == preserveLayerName)
                                continue;

                            // 记录原始图层信息
                            _originalLayerInfos[layer.Name] = new LayerState
                            {
                                Name = layer.Name,
                                ColorIndex = layer.Color.ColorIndex,
                                LinetypeObjectId = layer.LinetypeObjectId,
                                LineWeight = layer.LineWeight,
                                IsFrozen = layer.IsFrozen,
                                IsLocked = layer.IsLocked,
                                IsOff = layer.IsOff
                            };

                            // 删除图层（切换写入模式）
                            layer.UpgradeOpen();
                            layer.Erase();
                        }

                        // 提交事务
                        trans.Commit();
                        Env.Editor.Redraw();

                        // 设置删除状态
                        _isLayersDeleted = true;
                        ed.WriteMessage($"\n已删除除 {preserveLayerName} 和 0 图层外的所有图层。");
                    }
                    else
                    {
                        // 恢复图层
                        LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForWrite) as LayerTable;

                        // 重新创建之前删除的图层
                        foreach (var layerInfo in _originalLayerInfos)
                        {
                            // 创建新的图层表记录
                            LayerTableRecord newLayer = new LayerTableRecord
                            {
                                Name = layerInfo.Value.Name,
                                Color = Color.FromColorIndex(ColorMethod.ByAci, layerInfo.Value.ColorIndex),// 还原颜色和线宽
                                LineWeight = layerInfo.Value.LineWeight
                            };

                            // 设置线型
                            newLayer.LinetypeObjectId = layerInfo.Value.LinetypeObjectId;

                            // 设置图层状态
                            newLayer.IsLocked = layerInfo.Value.IsLocked;
                            newLayer.IsOff = layerInfo.Value.IsOff;

                            // 将图层添加到图层表
                            layerTable.Add(newLayer);
                            trans.AddNewlyCreatedDBObject(newLayer, true);
                        }

                        // 提交事务
                        trans.Commit();

                        // 重置状态
                        _isLayersDeleted = false;
                        ed.WriteMessage("\n已恢复所有删除的图层。");
                    }
                }
                catch (Exception ex)
                {
                    // 错误处理
                    ed.WriteMessage($"\n发生错误：{ex.Message}");
                    trans.Abort();
                }
            }
        }


        /// <summary>
        /// 关闭图层
        /// </summary>
        [CommandMethod(nameof(CloseAllLayer))]
        public static void CloseAllLayer()
        {
            // 使用事务修改图层状态  
            using (var tr = new DBTrans())
            {
                try
                {
                    //// 第三步：等待用户确认  
                    //PromptResult pr = Application.DocumentManager.MdiActiveDocument.Editor.GetString("\n已修改图层状态，是否还原? [是/否] <是>: ");

                    //// 判断是否还原  
                    //if (pr.Status == PromptStatus.OK &&
                    //    (string.IsNullOrEmpty(pr.StringResult) ||
                    //     pr.StringResult.Trim().ToLower() == "是" ||
                    //     pr.StringResult.Trim().ToLower() == "y"))
                    //{
                    //    // 还原图层状态  
                    //    RestoreLayerStates(originalLayerStates);
                    //    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n已还原所有图层的初始状态");
                    //}
                    // 第一步：保存当前所有图层的初始状态  

                    if (VariableDictionary.btnState)
                    {
                        SaveLayerStates();
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n已保存 {layerOriStates.Count} 个图层的初始状态");
                        // 打开图层表  
                        LayerTable layerTable = tr.GetObject(tr.Database.LayerTableId, OpenMode.ForRead) as LayerTable;

                        // 遍历图层并进行一些修改（这里以关闭部分图层为例）  
                        foreach (ObjectId layerId in layerTable)
                        {
                            LayerTableRecord layerRecord = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                            if (!VariableDictionary.selectTjtLayer.Contains(layerRecord.Name))
                            {
                                //layerRecord.IsOff = true;//关闭图层
                                layerRecord.IsFrozen = true;//冻结图层
                            }
                        }
                    }
                    else
                    {
                        // 还原图层状态  
                        RestoreLayerStates(layerOriStates);
                    }
                    tr.Commit();
                    Env.Editor.Redraw();
                }
                catch (Exception ex)
                {
                    // 处理可能的异常  
                    Application.ShowAlertDialog($"修改图层状态时发生错误：{ex.Message}");
                    tr.Abort();
                }
            }

        }

        /// <summary>
        /// 打开图层
        /// </summary>
        [CommandMethod(nameof(OpenLayer2))]
        public static void OpenLayer2()
        {
            try
            {
                if (!readLayerONOFFState)
                    layerOnOff();
                using var tr = new DBTrans();
                if (!VariableDictionary.btnState)
                {
                    foreach (var layerName in layerOnOffDic)
                    {
                        var layerLtr = tr.LayerTable.GetRecord(layerName.Key, OpenMode.ForWrite);
                        if (layerLtr != null)
                        {
                            foreach (var layer in VariableDictionary.tjtBtn)
                            {
                                if (layer == layerName.Key)
                                { layerLtr.IsOff = false; layerLtr.IsFrozen = false; }
                                else
                                {
                                    //layerLtr.IsOff = true;
                                    layerLtr.IsFrozen = true;
                                    break;
                                }
                            }
                        }
                    }
                    VariableDictionary.btnState = true;
                }

                foreach (var layerName in layerOnOffDic)
                {
                    var layerLtr = tr.LayerTable.GetRecord(layerName.Key, OpenMode.ForWrite);
                    if (layerLtr != null)
                    {
                        layerLtr.IsOff = layerName.Value;

                    }
                    VariableDictionary.btnState = false;
                }
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage($"打开图层失败:{ex}");
            }

        }
        private static Dictionary<string, bool> layerOnOffDic = new Dictionary<string, bool>();
        private static bool readLayerONOFFState = false;

        /// <summary>
        /// 图层开或关
        /// </summary>
        public static void layerOnOff()
        {
            try
            {
                using var tr = new DBTrans();
                //获取当前所有图层名称并循环委托
                tr.LayerTable.GetRecordNames().ForEach(action: (layerName) =>
                {
                    var layerLtr = tr.LayerTable.GetRecord(layerName, OpenMode.ForWrite);
                    if (layerLtr != null)
                        layerOnOffDic.Add(layerName, layerLtr.IsOff);
                });
                readLayerONOFFState = true;
                tr.Commit();
                Env.Editor.Redraw();

            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("图层开或关失败！");
                Env.Editor.WriteMessage(ex.Message);
            }

        }

        /// <summary>
        /// 打开图层
        /// </summary>
        [CommandMethod(nameof(OpenLayer))]
        public static void OpenLayer()
        {
            try
            {
                using var tr = new DBTrans();
                tr.LayerTable.GetRecordNames().ForEach(action: (layname) =>
                {
                    foreach (var layer in VariableDictionary.tjtBtn)
                    {
                        if (layer != null)
                        {
                            if ((layname.Contains(layer)))//判断layer图层里是不是有传进来的关键字
                            {
                                var ltr = tr.LayerTable.GetRecord(layname, OpenMode.ForWrite);
                                if (ltr.IsOff == true)
                                {
                                    //ltr.IsOff = false;
                                    ltr.IsFrozen = false;
                                }
                                else
                                {
                                    //ltr.IsOff = true;
                                    ltr.IsFrozen = true;
                                }
                                ; // 关闭图层
                                layname.Print();
                            }
                        }
                    }
                });
                tr.Commit();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("打开图层失败！");
            }
        }

        /// <summary>
        /// 冻结VariableDictionary.selectTjtLayer图层
        /// </summary>
        [CommandMethod(nameof(IsFrozenLayer))]
        public static void IsFrozenLayer()
        {
            try
            {
                using var tr = new DBTrans();

                tr.LayerTable.GetRecordNames().ForEach(action: (layname) =>
                {
                    if (layname.Contains("|"))
                    {
                        string newString = layname.Split('|')[1];
                        if ((VariableDictionary.selectTjtLayer.Contains(newString)))//判断layer图层里是不是有传进来的关键字
                        {
                            var ltr = tr.LayerTable.GetRecord(layname, OpenMode.ForWrite);
                            if (ltr.IsFrozen == true)
                            {
                                ltr.IsFrozen = false;
                            }
                            else
                            {
                                ltr.IsFrozen = true;
                            }
                            ; // 关闭图层
                            layname.Print();
                        }
                    }
                    else
                    {
                        if ((VariableDictionary.selectTjtLayer.Contains(layname)))//判断layer图层里是不是有传进来的关键字
                        {
                            var ltr = tr.LayerTable.GetRecord(layname, OpenMode.ForWrite);
                            if (ltr.IsFrozen == true)
                            {
                                ltr.IsFrozen = false;
                            }
                            else
                            {
                                ltr.IsFrozen = true;
                            }
                           ; // 关闭图层
                            layname.Print();
                        }
                    }
                });
                tr.Commit();
                Env.Editor.Regen();
                Env.Editor.Redraw();
            }
            catch (Exception ex)
            {
                // 记录错误日志  
                Env.Editor.WriteMessage("打开图层失败！");
                Env.Editor.WriteMessage(ex.Message);
            }
        }


        #endregion

        #region  布局空间检查图层

        [CommandMethod("ChangePlotSetting")]
        public static void ChangePlotSetting()
        {
            //开启事务
            DBTrans tr = new();
            // 引用布局管理器 LayoutManager
            var acLayoutMgr = LayoutManager.Current;
            // 读取当前布局，在命令行窗口显示布局名
            var acLayout = tr.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout), OpenMode.ForRead) as Layout;
            // 输出当前布局名和设备名
            Env.Editor.WriteMessage("\nCurrent layout: " + acLayout.LayoutName);
            Env.Editor.WriteMessage("\nCurrent device name: " + 516 + acLayout.PlotConfigurationName);
            // 从布局中获取 PlotInfo
            PlotInfo acPlInfo = new PlotInfo();
            acPlInfo.Layout = acLayout.ObjectId;
            // 复制布局中的 PlotSettings
            PlotSettings acPlSet = new PlotSettings(acLayout.ModelType);
            acPlSet.CopyFrom(acLayout);
            // 更新 PlotSettings 对象的 PlotConfigurationName 属性
            PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;
            acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWF6 ePlot.pc3", "ANSI_(8.50_x_11.00_Inches)");
            // 更新布局
            acLayout.UpgradeOpen();
            acLayout.CopyFrom(acPlSet);
            // 输出已更新的布局设备名
            Env.Editor.WriteMessage("\nNew device name: " + acLayout.PlotConfigurationName);
            // 将新对象保存到数据库
            tr.Commit();
        }


        /// <summary>
        /// 找到视口中的外部参照图层并冻结
        /// </summary>
        [CommandMethod("FindXrefLayersInViewport")]
        public void FindXrefLayersInViewport()
        {
            // 获取当前文档的编辑器对象
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // 提示用户选择一个视口
            PromptEntityOptions peo = new("\n请选择一个布局视口: ");
            peo.SetRejectMessage("\n请选择一个视口对象。\n");
            peo.AddAllowedClass(typeof(Viewport), true);
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n未选择视口，操作已取消。");
                return;
            }

            // 开始事务
            using DBTrans tr = new();
            try
            {
                // 获取选定的视口对象
                Viewport selectedViewport = tr.GetObject<Viewport>(per.ObjectId, OpenMode.ForWrite);
                if (selectedViewport == null)
                {
                    ed.WriteMessage("\n无法打开选定的视口。");
                    return;
                }
                // 获取当前文档的编辑器对象  
                //DBTrans tr = new();
                // 获取布局管理器单例  
                LayoutManager lm = LayoutManager.Current;
                // 获取当前布局的名称  
                string curLayoutName = lm.CurrentLayout;
                // 根据布局名获取对应的 ObjectId  
                ObjectId layoutId = lm.GetLayoutId(curLayoutName);
                // 打开布局对象以便读取  
                Layout layout = tr.GetObject(layoutId, OpenMode.ForRead) as Layout;
                // 获取 PaperSpace 的 BlockTableRecord  
                BlockTableRecord psBtr = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;

                // 打开 BlockTable  
                BlockTable bt = tr.GetObject(tr.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                // 获取模型空间的  
                BlockTableRecord msBtr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                // 获取图层表  
                var layerTable = tr.GetObject(tr.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                // 遍历模型空间中的所有实体  
                foreach (ObjectId layerTableId in layerTable)
                {
                    // 打开子实体  
                    var layerTableRecord = tr.GetObject(layerTableId, OpenMode.ForRead) as LayerTableRecord;
                    // 如果子实体为空则跳过  
                    if (layerTableRecord == null) continue;
                    // 获取子实体所在的图层名称  
                    string layerName = layerTableRecord.Name;
                    // 检查图层名是否包含“|”符号  
                    int separatorIndex = layerName.IndexOf('|');
                    if (separatorIndex >= 0)
                    {
                        // 获取“|”符号后面的部分  
                        string layerSuffix = layerName.Substring(separatorIndex + 1);
                        // 循环遍历选择的图层  
                        foreach (var selectLayer in VariableDictionary.selectTjtLayer)
                        {
                            // 如果图层后缀与选择的图层完全匹配  
                            if (string.Equals(layerSuffix, selectLayer, StringComparison.OrdinalIgnoreCase))
                            {
                                // 获取图层的 ObjectId  
                                //ObjectId layerId = layerTable[layerName];

                                tr.GetObject(layerTableId, OpenMode.ForWrite);
                                // 冻结图层到视口中  
                                ObjectIdCollection layerIds = new ObjectIdCollection(new[] { layerTableId });
                                // 冻结图层到视口中  
                                // 修复代码以确保 activeVp 对象已打开为写模式，并且 layerIds 不为空。
                                try
                                {
                                    // 确保 activeVp 已打开为写模式
                                    var viewport = tr.GetObject(selectedViewport.ObjectId, OpenMode.ForWrite) as Viewport;
                                    if (selectedViewport == null)
                                    {
                                        throw new InvalidOperationException("无法将视口对象打开为写模式。");
                                    }
                                    // 确保 layerIds 不为空
                                    if (layerIds == null)
                                    {
                                        throw new ArgumentException("图层 ID 列表为空或无效。");
                                    }
                                    // 冻结图层
                                    selectedViewport.FreezeLayersInViewport(layerIds.GetEnumerator());

                                }
                                catch (Exception ex)
                                {
                                    // 处理异常
                                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"错误: {ex.Message}");
                                }

                                Env.Editor.WriteMessage($"\n图层 '{layerName}' 已在视口中冻结。");
                            }
                        }
                    }
                }
                tr.Commit();
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"\n发生错误: {ex.Message}");
            }
        }
        /// <summary>
        /// 找到视口中的外部参照图层并冻结
        /// </summary>
        [CommandMethod("FindXrefLayersInViewport1")]
        public void FindXrefLayersInViewport1()
        {
            // 获取当前文档的编辑器对象  
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // 开始事务  
            using DBTrans tr = new();
            try
            {
                // 获取布局管理器单例  
                LayoutManager lm = LayoutManager.Current;
                // 获取当前布局的名称  
                string curLayoutName = lm.CurrentLayout;
                // 根据布局名获取对应的 ObjectId  
                ObjectId layoutId = lm.GetLayoutId(curLayoutName);
                // 打开布局对象以便读取  
                Layout layout = tr.GetObject(layoutId, OpenMode.ForRead) as Layout;

                // 检查是否在布局中  
                if (layout != null && !layout.ModelType)
                {
                    // 如果在布局中，直接获取当前视口  
                    Viewport selectedViewport = null;
                    foreach (ObjectId objId in layout.GetViewports())
                    {
                        selectedViewport = tr.GetObject(objId, OpenMode.ForWrite) as Viewport;
                        if (selectedViewport != null && selectedViewport.Number > 1)
                        {
                            break;
                        }
                    }

                    if (selectedViewport == null)
                    {
                        ed.WriteMessage("\n未找到有效的视口。");
                        return;
                    }

                    ProcessViewportLayers(tr, selectedViewport);
                }
                else
                {
                    // 提示用户选择一个视口  
                    PromptEntityOptions peo = new("\n请选择一个布局视口: ");
                    peo.SetRejectMessage("\n请选择一个视口对象。\n");
                    peo.AddAllowedClass(typeof(Viewport), true);
                    PromptEntityResult per = ed.GetEntity(peo);

                    if (per.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\n未选择视口，操作已取消。");
                        return;
                    }

                    // 获取选定的视口对象  
                    Viewport selectedViewport = tr.GetObject<Viewport>(per.ObjectId, OpenMode.ForWrite);
                    if (selectedViewport == null)
                    {
                        ed.WriteMessage("\n无法打开选定的视口。");
                        return;
                    }

                    ProcessViewportLayers(tr, selectedViewport);
                }

                tr.Commit();
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"\n发生错误: {ex.Message}");
            }
        }
        /// <summary>
        /// 处理视口中的图层进行开关
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="selectedViewport"></param>
        private void ProcessViewportLayers(DBTrans tr, Viewport selectedViewport)
        {
            // 获取图层表  
            var layerTable = tr.GetObject(tr.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
            // 遍历图层表中的所有图层  
            foreach (ObjectId layerTableId in layerTable)
            {
                var layerTableRecord = tr.GetObject(layerTableId, OpenMode.ForRead) as LayerTableRecord;
                if (layerTableRecord == null) continue;

                string layerName = layerTableRecord.Name;
                int separatorIndex = layerName.IndexOf('|');
                if (separatorIndex >= 0)
                {
                    string layerSuffix = layerName.Substring(separatorIndex + 1);
                    foreach (var selectLayer in VariableDictionary.selectTjtLayer)
                    {
                        if (string.Equals(layerSuffix, selectLayer, StringComparison.OrdinalIgnoreCase))
                        {
                            tr.GetObject(layerTableId, OpenMode.ForWrite);
                            ObjectIdCollection layerIds = new ObjectIdCollection(new[] { layerTableId });
                            try
                            {
                                selectedViewport.FreezeLayersInViewport(layerIds.GetEnumerator());
                            }
                            catch (Exception ex)
                            {
                                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"错误: {ex.Message}");
                            }

                            Env.Editor.WriteMessage($"\n图层 '{layerName}' 已在视口中冻结。");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 找到视口中的外部参照图层并冻结
        /// </summary>
        [CommandMethod("FindXrefLayersInViewportOpen")]
        public void FindXrefLayersInViewportOpen()
        {
            // 获取当前文档的编辑器对象
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // 提示用户选择一个视口
            PromptEntityOptions peo = new("\n请选择一个布局视口: ");
            peo.SetRejectMessage("\n请选择一个视口对象。\n");
            peo.AddAllowedClass(typeof(Viewport), true);
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\n未选择视口，操作已取消。");
                return;
            }

            // 开始事务
            using DBTrans tr = new();
            try
            {
                // 获取选定的视口对象
                Viewport selectedViewport = tr.GetObject<Viewport>(per.ObjectId, OpenMode.ForWrite);
                if (selectedViewport == null)
                {
                    ed.WriteMessage("\n无法打开选定的视口。");
                    return;
                }

                // 获取图层表
                var layerTable = tr.GetObject(tr.Database.LayerTableId, OpenMode.ForRead) as LayerTable;

                // 遍历图层表中的所有图层
                foreach (ObjectId layerTableId in layerTable)
                {
                    // 打开图层记录
                    var layerTableRecord = tr.GetObject(layerTableId, OpenMode.ForRead) as LayerTableRecord;
                    if (layerTableRecord == null) continue;

                    // 获取图层名称
                    string layerName = layerTableRecord.Name;

                    // 检查图层名是否包含“|”符号
                    int separatorIndex = layerName.IndexOf('|');
                    if (separatorIndex >= 0)
                    {
                        // 获取“|”符号后面的部分
                        string layerSuffix = layerName.Substring(separatorIndex + 1);

                        // 循环遍历选择的图层
                        foreach (var selectLayer in VariableDictionary.selectTjtLayer)
                        {
                            // 如果图层后缀与选择的图层完全匹配
                            if (string.Equals(layerSuffix, selectLayer, StringComparison.OrdinalIgnoreCase))
                            {
                                // 解冻图层
                                ObjectIdCollection layerIds = new ObjectIdCollection(new[] { layerTableId });
                                try
                                {
                                    // 确保 selectedViewport 已打开为写模式
                                    var viewport = tr.GetObject(selectedViewport.ObjectId, OpenMode.ForWrite) as Viewport;
                                    if (viewport == null)
                                    {
                                        throw new InvalidOperationException("无法将视口对象打开为写模式。");
                                    }

                                    // 解冻图层
                                    selectedViewport.ThawLayersInViewport(layerIds.GetEnumerator());
                                }
                                catch (Exception ex)
                                {
                                    // 处理异常
                                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"错误: {ex.Message}");
                                }

                                Env.Editor.WriteMessage($"\n图层 '{layerName}' 已在视口中解冻。");
                            }
                        }
                    }
                }
                tr.Commit();
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"\n发生错误: {ex.Message}");
            }
        }

        #endregion

        #region Excel相关


        /// <summary>
        /// 选择Excel文件对话框 DBTextLabel
        /// </summary>
        /// <returns></returns>
        private string SelectExcelFile()
        {
            using (var dlg = new System.Windows.Forms.OpenFileDialog())
            {
                dlg.Filter = "Excel文件 (*.xlsx)|*.xlsx";
                return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
            }
        }

        ////////////////////////////////////

        [CommandMethod("InsertExcelTableToCAD")] // 在CAD中执行命令：EXCELTABLE  
        public void InsertExcelTableToCAD()
        {
            try
            {
                // 创建并显示文件打开对话框，选择 Excel 文件  获取选择的文件路径 
                string excelPath = SelectExcelFile();
                // 定义目标字段名  
                string keyField = "分类号";
                // 要找的列名
                string[] targetFields = { "分类号", "设备位号", "设备名称", "主要技术规格型号", "电压", "功率", "数量", "单重" };
                // 用于保存筛选后的数据  
                List<List<string>> dataList = new List<List<string>>();
                // 打开Excel文件并读取内容  
                using (var package = new ExcelPackage(new FileInfo(excelPath)))
                {
                    // 取第一个工作表  
                    var worksheet = package.Workbook.Worksheets[1];
                    // 用储存表头，找到各列的索引 ，列名到索引映射
                    var columnIndexDict = new Dictionary<string, int>();
                    // 存储总列数
                    int colCount = worksheet.Dimension.End.Column;
                    // 循环5-7行
                    for (int rowItem = 5; rowItem <= 7; rowItem++)
                    {
                        // 循环所有列
                        for (int colItem = 1; colItem <= colCount; colItem++)
                        {
                            // 在第 colItem 列名
                            string colName = worksheet.Cells[rowItem, colItem].Text
                                .Replace("\r", "")  // 去除回车符 \r
                                .Replace("\n", "")  // 去除换行符 \n
                                .Trim();           // 去除前后空格
                            //如果找出的字符在指定的范围内就加入到数据表内
                            if (targetFields.Any(s => colName.Contains(s)))
                                columnIndexDict[colName] = colItem;
                            else
                                continue;
                        }
                    }

                    // 按值排序并转为新字典（需 LINQ）从小到大排序
                    var sortedByValueDict = columnIndexDict
                            .OrderBy(pair => pair.Value)
                            .ToDictionary(pair => pair.Key, pair => pair.Value);

                    // 记录第一行作为表头行  
                    //var header = new List<string>(targetFields);
                    var header = new List<string>();
                    foreach (var item in sortedByValueDict.Keys)
                    {
                        header.Add(item);
                    }
                    dataList.Add(header);
                    // 遍历每一行，过滤“分类号”为“X”的行  
                    int rowCount = worksheet.Dimension.End.Row;//行数
                    int classifyCol = sortedByValueDict[keyField];//找出分类号所在的列号
                    for (int row = 8; row <= rowCount; row++)
                    {
                        //记录这行对应的列的内容值
                        string classifyValue = worksheet.Cells[row, classifyCol].Text.Trim();
                        if (classifyValue != "") // 筛选“分类号”列为"X"的行  
                        {
                            //储存这行的数据
                            List<string> rowData = new List<string>();
                            //循环要找的各列名头
                            foreach (var fld in sortedByValueDict.Values)
                            {
                                //int colIdx = columnIndexDict[fld];
                                string value = worksheet.Cells[row, fld].Text.Trim();
                                rowData.Add(value);
                            }
                            dataList.Add(rowData);
                        }
                    }
                }

                if (dataList.Count <= 1)
                {
                    Application.ShowAlertDialog("没有找到‘分类号’为X的设备！");
                    return;
                }

                // CAD中插入Table表格  
                using (DBTrans tr = new())
                {
                    TextStyleAndLayerInfo("SB", 1, "tJText");

                    //行倒序
                    dataList.Reverse();
                    // 创建表格对象  
                    var table = new Autodesk.AutoCAD.DatabaseServices.Table();
                    // 设置表格插入点，选择鼠标点选  
                    PromptPointResult ppr = Env.Editor.GetPoint("\n请在CAD中指定表格插入点：");
                    if (ppr.Status != PromptStatus.OK)
                    {
                        return;
                    }
                    table.Position = ppr.Value;
                    // 设置行数量  
                    int rows = dataList.Count;
                    // 设置列数量 
                    int cols = dataList[0].Count;
                    //初始化表的行与列数
                    table.SetSize(rows, cols);
                    // 设置图层为SB
                    table.Layer = "SB";
                    tr.LayerTable.GetRecordNames().ForEach(action: (layname) => layname.Print());
                    //设置表格颜色
                    table.Color = tr.LayerTable.GetRecord(tr.LayerTable["SB"], OpenMode.ForRead).Color; // 设置表格颜色为白色
                    // 行高（可调整，单位：当前图纸单位）
                    table.SetRowHeight(35);
                    // 先填入数据，计算每列的最大宽度
                    for (int rowItem = 0; rowItem < rows; rowItem++)
                    {
                        for (int colItem = 0; colItem < cols; colItem++)
                        {
                            //填入每行每列数据
                            table.Cells[rowItem, colItem].TextString = dataList[rowItem][colItem];
                            //如果是最后一行
                            if (rowItem == rows - 1)
                            {
                                table.Cells[rowItem, colItem].TextHeight = 350; // 表头字体大小 
                                table.Cells[rowItem, colItem].TextStyleId = tr.TextStyleTable["tJText"];
                                table.Cells[rowItem, colItem].Alignment = CellAlignment.MiddleCenter;
                            }
                            else
                            {
                                table.Cells[rowItem, colItem].TextHeight = 300;
                                table.Cells[rowItem, colItem].TextStyleId = tr.TextStyleTable["tJText"];
                                table.Cells[rowItem, colItem].Alignment = CellAlignment.MiddleCenter;
                            }
                        }
                    }
                    //table.SetRowHeight(10);
                    // 计算每列的最大宽度
                    double[] columnWidths = new double[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        double maxWidth = 0;
                        for (int i = 0; i < rows; i++)
                        {
                            // 估算文本宽度（可根据实际情况调整系数）
                            var textWidth = dataList[i][j].Length * table.Cells[i, j].TextHeight * 0.8;
                            if (textWidth > maxWidth)
                            {
                                maxWidth = (double)textWidth;
                            }
                        }
                        // 设置最小列宽（避免太窄）
                        columnWidths[j] = Math.Max(maxWidth, 20); // 40是最小宽度
                    }


                    // 在填充单元格数据之后，应用列宽之前添加这段代码  
                    // 设置每行行高，确保文字正确显示  
                    for (int i = 0; i < rows; i++)
                    {
                        // 根据是否为表头行、第一行或普通行设置不同行高  
                        if (i == rows - 1)
                        {
                            // 表头行 (最后一行)  
                            table.Rows[i].Height = 500; // 表头行高  
                        }
                        else if (i == 0)
                        {
                            // 第一行 (在倒序后显示为最上面一行)  
                            table.Rows[i].Height = 450; // 稍微高一点避免文字溢出  
                        }
                        else
                        {
                            // 其他普通行  
                            table.Rows[i].Height = 450; // 普通行高  
                        }
                    }
                    // 应用计算后的列宽
                    for (int j = 0; j < cols; j++)
                    {
                        table.Columns[j].Width = columnWidths[j];
                    }
                    // 确保表格边框显示正确  
                    table.GenerateLayout();

                    // 在应用列宽后，添加以下代码确保表格正确显示  
                    table.RecomputeTableBlock(true);

                    // 可选：强制更新表格显示  
                    Env.Editor.UpdateScreen();

                    // 将Scale移到这里，在所有设置完成后再缩放  
                    table.Scale(ppr.Value, 1);

                    // 将表格添加到模型空间  
                    var tableObjectId = tr.CurrentSpace.AddEntity(table);

                    // 分解块引用
                    DBObjectCollection explodedObjects = new DBObjectCollection();
                    table.Explode(explodedObjects);

                    // 添加分解后的实体到图纸空间
                    foreach (DBObject obj in explodedObjects)
                    {
                        Entity ent = obj as Entity;
                        if (ent != null)
                        {
                            tr.CurrentSpace.AddEntity(ent);
                        }
                    }
                    // 删除临时块引用和块定义
                    table.Erase();

                    Env.Editor.Redraw();
                    tr.Commit(); // 提交事务
                }
                Env.Editor.WriteMessage("\n表格已经插入到CAD。");
            }
            catch (Exception ex)
            {
                Env.Editor.WriteMessage($"\n错误：{ex.Message}");
            }
        }

        #region 导出Excel方法一；


        #endregion

        #region 导出Excel方法二；

        /// 存储表格数据的类
        public class TableCell
        {
            /// <summary>
            /// 表文字
            /// </summary>
            public string Text { get; set; }
            /// <summary>
            /// 表坐标
            /// </summary>
            public Point3d Position { get; set; }
            /// <summary>
            /// 表宽
            /// </summary>
            public double Width { get; set; }
            /// <summary>
            /// 表高
            /// </summary>
            public double Height { get; set; }
            /// <summary>
            /// 合并格
            /// </summary>
            public bool IsMerged { get; set; } = false;
            /// <summary>
            /// 合并跨越
            /// </summary>
            public int MergeAcross { get; set; } = 0;
            /// <summary>
            /// 向下合并
            /// </summary>
            public int MergeDown { get; set; } = 0;
        }

        /// <summary>
        /// 导出CAD表格到Excel的命令
        /// </summary>
        [CommandMethod("ExportCADTable")]
        public void ExportCADTable()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 获取用户选择的对象
            PromptSelectionResult psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 收集选择集中的所有实体
                    SelectionSet selectionSet = psr.Value;
                    //保存全部实体变量
                    List<Entity> allEntities = new List<Entity>();
                    //循环所有选择实体
                    foreach (SelectedObject selectedObject in selectionSet)
                    {
                        if (selectedObject != null)
                        {
                            Entity ent = tr.GetObject(selectedObject.ObjectId, OpenMode.ForRead) as Entity;
                            if (ent != null)
                            {
                                // 处理块引用
                                if (ent is BlockReference blockRef)
                                {
                                    allEntities.AddRange(ExplodeBlockReference(blockRef, tr));
                                }
                                else
                                {
                                    allEntities.Add(ent);
                                }
                            }
                        }
                    }

                    // 分析表格结构并提取数据
                    var tableData = AnalyzeTable(allEntities);
                    if (tableData == null || tableData.Count == 0)
                    {
                        ed.WriteMessage("\n未检测到有效的表格结构！");
                        return;
                    }

                    // 获取保存文件路径
                    string filePath = GetSaveFilePathWithDialog();
                    //如果保存路径为空，返回
                    if (string.IsNullOrEmpty(filePath))
                        return;

                    // 导出到Excel
                    ExportToExcel(tableData, filePath);

                    ed.WriteMessage($"\n表格数据已成功导出到: {filePath}");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\n错误: {ex.Message}");
                }
                finally
                {
                    tr.Commit();
                }
            }
        }

        /// <summary>
        /// 处理块引用，提取其中的实体
        /// </summary>
        /// <param name="blockRef">返回块表记录</param>
        /// <param name="tr">进程</param>
        /// <returns></returns>
        private List<Entity> ExplodeBlockReference(BlockReference blockRef, Transaction tr)
        {
            //储存块引用的实体
            List<Entity> entities = new List<Entity>();
            // 获取块表记录
            BlockTableRecord btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

            // 添加属性值作为文本
            if (blockRef.AttributeCollection != null)
            {
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attRef != null && !attRef.IsConstant)
                    {
                        DBText text = new DBText();
                        text.TextString = attRef.TextString;
                        text.Position = attRef.Position;
                        text.Height = attRef.Height;
                        text.Rotation = attRef.Rotation;
                        text.AlignmentPoint = attRef.AlignmentPoint;
                        text.HorizontalMode = attRef.HorizontalMode;
                        text.VerticalMode = attRef.VerticalMode;
                        entities.Add(text);
                    }
                }
            }

            // 添加块中的实体
            foreach (ObjectId objId in btr)
            {
                Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                if (ent != null)
                {
                    // 递归处理嵌套块
                    if (ent is BlockReference nestedBlockRef)
                    {
                        entities.AddRange(ExplodeBlockReference(nestedBlockRef, tr));
                    }
                    else if (!(ent is AttributeDefinition)) // 排除属性定义
                    {
                        // 转换实体坐标
                        Entity transformedEnt = ent.GetTransformedCopy(blockRef.BlockTransform);
                        entities.Add(transformedEnt);
                    }
                }
            }

            return entities;
        }

        /// <summary>
        /// 分析表格结构并提取数据
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <returns>返回表数据</returns>
        private List<List<TableCell>> AnalyzeTable(List<Entity> entities)
        {
            /// 分离线条和文字
            List<Line> lines = entities.OfType<Line>().ToList();
            /// 分离DBText
            List<DBText> texts = entities.OfType<DBText>().ToList();
            /// 分离MText
            List<MText> mtexts = entities.OfType<MText>().ToList();

            /// 合并普通文本和多行文本
            List<TextEntity> allTexts = new List<TextEntity>();
            foreach (var text in texts)
            {
                allTexts.Add(new TextEntity
                {
                    Text = text.TextString,
                    Position = text.Position,
                    Height = text.Height
                });
            }

            foreach (var mtext in mtexts)
            {
                allTexts.Add(new TextEntity
                {
                    Text = mtext.Contents,
                    Position = mtext.Location,
                    Height = mtext.TextHeight
                });
            }

            // 提取表格的横线和竖线
            var horizontalLines = lines.Where(l => IsHorizontalLine(l)).ToList();
            var verticalLines = lines.Where(l => IsVerticalLine(l)).ToList();

            // 按Y坐标排序横线，按X坐标排序竖线
            horizontalLines = horizontalLines.OrderByDescending(l => l.StartPoint.Y).ToList();
            verticalLines = verticalLines.OrderBy(l => l.StartPoint.X).ToList();

            // 如果没有足够的线条来形成表格，返回null
            if (horizontalLines.Count < 2 || verticalLines.Count < 2)
                return null;

            // 创建表格结构
            int rowCount = horizontalLines.Count - 1;
            int colCount = verticalLines.Count - 1;
            List<List<TableCell>> tableData = new List<List<TableCell>>();

            // 初始化表格数据结构
            for (int i = 0; i < rowCount; i++)
            {
                tableData.Add(new List<TableCell>());
                for (int j = 0; j < colCount; j++)
                {
                    tableData[i].Add(null);
                }
            }

            // 分析合并单元格情况
            var mergedCells = AnalyzeMergedCells(horizontalLines, verticalLines);

            // 将文本分配到对应的单元格
            foreach (var text in allTexts)
            {
                // 查找文本所在的单元格
                int rowIndex = FindRowIndex(text.Position, horizontalLines);
                int colIndex = FindColumnIndex(text.Position, verticalLines);

                if (rowIndex >= 0 && rowIndex < rowCount && colIndex >= 0 && colIndex < colCount)
                {
                    // 检查是否属于合并单元格
                    var mergedCell = mergedCells.FirstOrDefault(mc =>
                        mc.Row == rowIndex && mc.Column == colIndex);

                    TableCell cell = new TableCell
                    {
                        Text = text.Text,
                        Position = text.Position,
                        Width = verticalLines[colIndex + 1].StartPoint.X - verticalLines[colIndex].StartPoint.X,
                        Height = horizontalLines[rowIndex].StartPoint.Y - horizontalLines[rowIndex + 1].StartPoint.Y
                    };

                    if (mergedCell != null)
                    {
                        cell.IsMerged = true;
                        cell.MergeAcross = mergedCell.ColumnSpan - 1;
                        cell.MergeDown = mergedCell.RowSpan - 1;
                    }

                    tableData[rowIndex][colIndex] = cell;
                }
            }

            return tableData;
        }

        /// <summary>
        /// 分析合并单元格
        /// </summary>
        /// <param name="horizontalLines"></param>
        /// <param name="verticalLines"></param>
        /// <returns></returns>
        private List<MergedCellInfo> AnalyzeMergedCells(List<Line> horizontalLines, List<Line> verticalLines)
        {
            var mergedCells = new List<MergedCellInfo>();
            double tolerance = 0.001;

            // 分析水平方向合并单元格
            for (int row = 0; row < horizontalLines.Count - 1; row++)
            {
                for (int col = 0; col < verticalLines.Count - 1; col++)
                {
                    // 检查当前单元格是否已被合并
                    if (mergedCells.Any(mc => mc.Row <= row && mc.Row + mc.RowSpan > row &&
                                              mc.Column <= col && mc.Column + mc.ColumnSpan > col))
                    {
                        continue;
                    }

                    // 计算当前单元格的边界
                    double left = verticalLines[col].StartPoint.X;
                    double right = verticalLines[col + 1].StartPoint.X;
                    double top = horizontalLines[row].StartPoint.Y;
                    double bottom = horizontalLines[row + 1].StartPoint.Y;

                    // 查找可能的水平合并
                    int colSpan = 1;
                    while (col + colSpan < verticalLines.Count - 1)
                    {
                        double nextRight = verticalLines[col + colSpan + 1].StartPoint.X;

                        // 检查是否缺少垂直分隔线
                        bool hasVerticalLine = false;
                        foreach (var line in verticalLines)
                        {
                            if (Math.Abs(line.StartPoint.X - verticalLines[col + colSpan].StartPoint.X) < tolerance &&
                                line.StartPoint.Y >= bottom - tolerance && line.EndPoint.Y <= top + tolerance)
                            {
                                hasVerticalLine = true;
                                break;
                            }
                        }

                        if (hasVerticalLine)
                            break;

                        colSpan++;
                    }

                    // 查找可能的垂直合并
                    int rowSpan = 1;
                    while (row + rowSpan < horizontalLines.Count - 1)
                    {
                        double nextBottom = horizontalLines[row + rowSpan + 1].StartPoint.Y;

                        // 检查是否缺少水平分隔线
                        bool hasHorizontalLine = false;
                        foreach (var line in horizontalLines)
                        {
                            if (Math.Abs(line.StartPoint.Y - horizontalLines[row + rowSpan].StartPoint.Y) < tolerance &&
                                line.StartPoint.X >= left - tolerance && line.EndPoint.X <= right - tolerance)
                            {
                                hasHorizontalLine = true;
                                break;
                            }
                        }

                        if (hasHorizontalLine)
                            break;

                        rowSpan++;
                    }

                    // 如果有合并，记录合并单元格信息
                    if (colSpan > 1 || rowSpan > 1)
                    {
                        mergedCells.Add(new MergedCellInfo
                        {
                            Row = row,
                            Column = col,
                            RowSpan = rowSpan,
                            ColumnSpan = colSpan
                        });
                    }
                }
            }

            return mergedCells;
        }

        /// <summary>
        /// 导出到Excel
        /// </summary>
        /// <param name="tableData"></param>
        /// <param name="filePath"></param>
        private void ExportToExcel(List<List<TableCell>> tableData, string filePath)
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("CAD表格数据");

                // 获取表格的行列数
                int rowCount = tableData.Count;
                int colCount = rowCount > 0 ? tableData[0].Count : 0;

                // 填充Excel表格并设置边框
                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        var cell = tableData[row][col];

                        if (cell != null && cell.IsMerged && (cell.MergeAcross > 0 || cell.MergeDown > 0))
                        {
                            // 合并单元格
                            worksheet.Cells[row + 1, col + 1,
                                           row + 1 + cell.MergeDown,
                                           col + 1 + cell.MergeAcross].Merge = true;
                        }

                        // 设置单元格值
                        if (cell != null)
                        {
                            worksheet.Cells[row + 1, col + 1].Value = cell.Text;
                        }

                        // 设置单元格边框
                        SetCellBorder(worksheet.Cells[row + 1, col + 1]);
                    }
                }

                // 自动调整列宽
                worksheet.Cells.AutoFitColumns();

                // 保存Excel文件
                package.Save();
            }
        }

        /// <summary>
        /// 设置单元格边框
        /// </summary>
        /// <param name="cell"></param>
        private void SetCellBorder(ExcelRange cell)
        {
            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        }

        /// <summary>
        /// 判断是否为横线
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsHorizontalLine(Line line)
        {
            double tolerance = 0.001;
            return Math.Abs(line.StartPoint.Y - line.EndPoint.Y) < tolerance;
        }

        /// <summary>
        /// 判断是否为竖线
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsVerticalLine(Line line)
        {
            double tolerance = 0.001;
            return Math.Abs(line.StartPoint.X - line.EndPoint.X) < tolerance;
        }

        /// <summary>
        /// 查找文本所在的行索引
        /// </summary>
        /// <param name="position"></param>
        /// <param name="horizontalLines"></param>
        /// <returns></returns>
        private int FindRowIndex(Point3d position, List<Line> horizontalLines)
        {
            for (int i = 0; i < horizontalLines.Count - 1; i++)
            {
                if (position.Y <= horizontalLines[i].StartPoint.Y &&
                    position.Y >= horizontalLines[i + 1].StartPoint.Y)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 查找文本所在的列索引
        /// </summary>
        /// <param name="position"></param>
        /// <param name="verticalLines"></param>
        /// <returns></returns>
        private int FindColumnIndex(Point3d position, List<Line> verticalLines)
        {
            for (int i = 0; i < verticalLines.Count - 1; i++)
            {
                if (position.X >= verticalLines[i].StartPoint.X &&
                    position.X <= verticalLines[i + 1].StartPoint.X)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 使用AutoCAD文件对话框获取保存文件路径
        /// </summary>
        /// <returns></returns>
        private string GetSaveFilePathWithDialog()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // 创建文件保存对话框
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Excel文件 (*.xlsx)|*.xlsx";
            sfd.Title = "保存表格数据到Excel文件";
            sfd.DefaultExt = "xlsx";

            // 获取当前CAD文档路径作为默认路径
            string currentDocPath = doc.Name;
            if (!string.IsNullOrEmpty(currentDocPath))
            {
                string currentDir = Path.GetDirectoryName(currentDocPath);
                sfd.InitialDirectory = currentDir;
            }

            // 显示对话框
            System.Windows.Forms.DialogResult result = sfd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return sfd.FileName;
            }

            return null;
        }

        /// <summary>
        /// 辅助类：用于统一处理DBText和MText
        /// </summary>
        private class TextEntity
        {
            public string Text { get; set; }
            public Point3d Position { get; set; }
            public double Height { get; set; }
        }

        /// <summary>
        /// 辅助类：用于表示合并单元格信息
        /// </summary>
        private class MergedCellInfo
        {
            public int Row { get; set; }
            public int Column { get; set; }
            public int RowSpan { get; set; }
            public int ColumnSpan { get; set; }
        }

        #endregion

        #endregion

        #region 清理、检查等命令
        /// <summary>
        /// 分解选定的块
        /// </summary>
        [CommandMethod("ExplodeBlockToNewBlock")]
        public void ExplodeBlockToNewBlock()
        {
            // 让用户选择一个块参照
            PromptEntityOptions peo = new PromptEntityOptions("\n请选择一个块参照: ");
            peo.SetRejectMessage("\n请选择块参照对象！");
            peo.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult per = Env.Editor.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            using (var tr = new DBTrans())
            {
                // 获取用户选择的块参照对象
                BlockReference blkRef = tr.GetObject(per.ObjectId, OpenMode.ForRead) as BlockReference;

                // 递归分解块，收集所有基本图元
                List<Entity> allEntities = new List<Entity>();
                ExplodeBlockRecursive(blkRef, tr, allEntities);

                if (allEntities.Count == 0)
                {
                    Env.Editor.WriteMessage("\n未找到可用于新块的图元。");
                    return;
                }

                // 创建新块定义
                BlockTable bt = (BlockTable)tr.GetObject(tr.Database.BlockTableId, OpenMode.ForWrite);
                string newBlockName = "SB_" + System.DateTime.Now.Ticks;
                BlockTableRecord newBtr = new BlockTableRecord();
                newBtr.Name = newBlockName;


                // 将所有分解后的图元加入新块
                foreach (var ent in allEntities)
                {
                    newBtr.AppendEntity(ent);
                }
                bt.Add(newBtr);

                // 在模型空间插入新块参照
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                BlockReference newBlkRef = new BlockReference(Point3d.Origin, newBtr.ObjectId);
                ms.AppendEntity(newBlkRef);

                Env.Editor.WriteMessage($"\n新块 \"{newBlockName}\" 已创建并插入。");

                tr.Commit();
            }
        }

        /// <summary>
        /// 递归分解块，将所有基本图元加入列表
        /// </summary>
        /// <param name="blkRef">块表记录</param>
        /// <param name="tr">事务</param>
        /// <param name="entityList">实体例表</param>
        private void ExplodeBlockRecursive(BlockReference blkRef, DBTrans tr, List<Entity> entityList)
        {
            // 分解当前块参照
            DBObjectCollection explodedObjs = new DBObjectCollection();
            blkRef.Explode(explodedObjs);

            foreach (DBObject obj in explodedObjs)
            {
                if (obj is BlockReference nestedBlkRef)
                {
                    // 如果是嵌套块，递归分解
                    ExplodeBlockRecursive(nestedBlkRef, tr, entityList);
                    obj.Dispose(); // 释放临时对象
                }
                else if (obj is Entity ent)
                {
                    // 复制实体，避免事务结束后被释放
                    Entity clone = ent.Clone() as Entity;
                    if (clone != null)
                        entityList.Add(clone);
                    ent.Dispose(); // 释放临时对象
                }
                else
                {
                    obj.Dispose(); // 释放非实体对象
                }
            }
        }


        /// <summary>  
        /// DICTS命令的实现  
        /// 功能：列出所有字典及其计数，并允许删除选定的字典  
        /// </summary>  
        [CommandMethod("DICTS")]
        public void ListAndManageDictionaries()
        {

            bool continueCommand = true;

            while (continueCommand)
            {
                try
                {
                    using var tr = new DBTrans();
                    // 获取命名对象字典  
                    DBDictionary nod = (DBDictionary)tr.GetObject(
                        tr.NamedObjectsDict.Id,
                        OpenMode.ForRead);

                    // 开始撤销标记  
                    //doc.TransactionManager.StartUndoMark();

                    // 初始化计数器和集合  
                    int index = 1;
                    var dictNames = new List<string>();
                    var dictIds = new List<ObjectId>();

                    // 遍历所有字典  
                    foreach (DBDictionaryEntry entry in nod)
                    {
                        string count = GetDictionaryCount(tr, entry.Value);
                        Env.Editor.WriteMessage($"\n{index}. \"{entry.Key}\"  {count}");

                        dictNames.Add(entry.Key);
                        dictIds.Add(entry.Value);
                        index++;
                    }

                    Env.Editor.WriteMessage($"\nActiveDocument.Dictionaries.Count={dictNames.Count}\n");

                    // 设置用户输入选项  
                    PromptIntegerOptions opts = new PromptIntegerOptions(
                        "\nWhich one to REMOVE by index above? <Enter to exit>: ")
                    {
                        AllowNegative = false,
                        AllowZero = false,
                        UpperLimit = dictNames.Count,
                        AllowNone = true // 允许直接按回车  
                    };

                    // 获取用户输入  
                    PromptIntegerResult result = Env.Editor.GetInteger(opts);

                    // 如果用户按回车，退出命令  
                    if (result.Status == PromptStatus.None)
                    {
                        continueCommand = false;
                        Env.Editor.WriteMessage("\nYou can type command DICTS to go again.");
                    }
                    // 如果用户输入了有效的索引号  
                    else if (result.Status == PromptStatus.OK)
                    {
                        int selectedIndex = result.Value - 1;
                        if (selectedIndex >= 0 && selectedIndex < dictIds.Count)
                        {
                            // 打开字典进行写操作并删除选定项  
                            DBDictionary dict = (DBDictionary)tr.GetObject(
                                nod.ObjectId,
                                OpenMode.ForWrite);
                            dict.Remove(dictNames[selectedIndex]);

                            // 提交事务  
                            tr.Commit();
                            Env.Editor.WriteMessage($"\nDictionary \"{dictNames[selectedIndex]}\" has been removed.");
                        }
                    }

                    // 结束撤销标记  
                    //doc.TransactionManager.EndUndoMark();

                }
                catch (System.Exception ex)
                {
                    Env.Editor.WriteMessage($"\nError: {ex.Message}");
                    continueCommand = false;
                }
            }
        }

        /// <summary>  
        /// 获取字典的计数  
        /// </summary>  
        private string GetDictionaryCount(Transaction trans, ObjectId dictId)
        {
            try
            {
                DBDictionary dict = (DBDictionary)trans.GetObject(dictId, OpenMode.ForRead);
                return dict.Count.ToString();
            }
            catch
            {
                return "#n/a";
            }
        }

        /// <summary>  
        /// 执行清理操作的命令  
        /// </summary>  
        [CommandMethod("CLEANUPDWG")]
        public void CleanupDrawing()
        {

        }
        #endregion
    }
}