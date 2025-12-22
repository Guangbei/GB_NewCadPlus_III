using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace GB_NewCadPlus_III.Helpers
{
    /// <summary>
    /// 用于从CAD选择导入时传输数据的DTO
    /// </summary>
    public class ImportEntityDto
    {
        public FileStorage FileStorage { get; set; } = new FileStorage();
        public FileAttribute FileAttribute { get; set; } = new FileAttribute();
        public string PreviewImagePath { get; set; }
    }

    /// <summary>
    /// 负责从当前CAD图形中选择实体并提取信息的辅助类
    /// </summary>
    public static class SelectionImportHelper
    {
        /// <summary>
        /// 交互式地在CAD中选择一个实体，并返回包含其信息的DTO。
        /// 此方法应在后台线程或UI线程中调用，它内部会处理DocumentLock。
        /// </summary>
        public static ImportEntityDto PickAndReadEntity()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                throw new InvalidOperationException("没有活动的CAD文档。");
            }

            ImportEntityDto dto = null;

            // 必须在文档锁定的情况下与CAD数据库交互
            using (doc.LockDocument())
            {
                var ed = doc.Editor;
                var db = doc.Database;

                var peo = new PromptEntityOptions("\n请选择要导入的图元（块、属性块等）：");
                var per = ed.GetEntity(peo);

                if (per.Status != PromptStatus.OK)
                {
                    return null; // 用户取消
                }

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var entity = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;
                    if (entity == null)
                    {
                        tr.Commit();
                        return null;
                    }

                    dto = new ImportEntityDto();
                    var fs = dto.FileStorage;
                    var fa = dto.FileAttribute;

                    // 填充通用信息
                    fs.FilePath = doc.Name; // 当前DWG文件路径
                    fs.FileType = ".dwg";
                    fs.CreatedAt = DateTime.Now;
                    fs.UpdatedAt = DateTime.Now;
                    fs.IsActive = 1;
                    fs.IsPublic = 1;
                    fs.CreatedBy = Environment.UserName;
                    fa.CreatedAt = DateTime.Now;
                    fa.UpdatedAt = DateTime.Now;

                    // 针对不同实体类型提取信息
                    if (entity is BlockReference br)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                        fs.DisplayName = btr.Name;
                        fs.ElementBlockName = btr.Name;
                        fs.LayerName = br.Layer;
                        fs.ColorIndex = br.Color.ColorIndex;
                        fs.scale = br.ScaleFactors.X; // 简化，只取X向缩放

                        fa.Angle = (decimal)br.Rotation;
                        fa.BasePointX = (decimal)br.Position.X;
                        fa.BasePointY = (decimal)br.Position.Y;
                        fa.BasePointZ = (decimal)br.Position.Z;

                        // 提取属性
                        if (br.AttributeCollection.Count > 0)
                        {
                            var attributesText = new List<string>();
                            foreach (ObjectId attId in br.AttributeCollection)
                            {
                                var attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                if (attRef != null)
                                {
                                    attributesText.Add($"{attRef.Tag}: {attRef.TextString}");
                                }
                            }
                            fa.Remarks = string.Join("\n", attributesText);
                        }
                    }
                    else
                    {
                        fs.DisplayName = entity.GetRXClass().Name;
                        fs.LayerName = entity.Layer;
                        fs.ColorIndex = entity.Color.ColorIndex;
                    }

                    fs.FileName = fs.DisplayName; // 默认文件名等于显示名
                    fa.FileName = fs.DisplayName;

                    // 估算尺寸
                    try
                    {
                        var ext = entity.GeometricExtents;
                        fa.Length = (decimal)(ext.MaxPoint.X - ext.MinPoint.X);
                        fa.Width = (decimal)(ext.MaxPoint.Y - ext.MinPoint.Y);
                        fa.Height = (decimal)(ext.MaxPoint.Z - ext.MinPoint.Z);
                    }
                    catch { /* 忽略尺寸获取失败 */ }

                    // 生成预览图
                    //try
                    //{
                    //    string tempDir = Path.Combine(Path.GetTempPath(), "GB_NewCadPlus_III_Previews");
                    //    Directory.CreateDirectory(tempDir);
                    //    string previewPath = Path.Combine(tempDir, $"preview_{Guid.NewGuid()}.png");

                    //    var previewBmp = Autodesk.AutoCAD.ApplicationServices.DocumentExtension.CapturePreviewImage(doc, 400, 300);
                    //    if (previewBmp != null)
                    //    {
                    //        previewBmp.Save(previewPath, System.Drawing.Imaging.ImageFormat.Png);
                    //        dto.PreviewImagePath = previewPath;

                    //        // 同步到 FileStorage
                    //        fs.PreviewImagePath = previewPath;
                    //        fs.PreviewImageName = Path.GetFileName(previewPath);
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    LogManager.Instance.LogWarning($"生成预览图失败: {ex.Message}");
                    //}

                    try
                    {
                        string tempDir = Path.Combine(Path.GetTempPath(), "GB_NewCadPlus_III_Previews");
                        Directory.CreateDirectory(tempDir);
                        string previewPath = Path.Combine(tempDir, $"preview_{Guid.NewGuid()}.png");

                        // 1. 保存当前视图
                        //var docEd = doc.Editor;
                        //var docDb = doc.Database;
                        ViewTableRecord originalView = ed.GetCurrentView();

                        // 2. 计算选中对象的包围盒
                        Extents3d? ext = null;
                        try
                        {
                            ext = entity.GeometricExtents;
                        }
                        catch { }

                        if (ext != null)
                        {
                            // 3. 生成适合包围盒的视图
                            var min = ext.Value.MinPoint;
                            var max = ext.Value.MaxPoint;
                            var center = new Point2d((min.X + max.X) / 2, (min.Y + max.Y) / 2);
                            double width = Math.Max(max.X - min.X, 1e-4);
                            double height = Math.Max(max.Y - min.Y, 1e-4);

                            // 适当放大一点，避免裁切
                            double margin = 0.05;
                            width *= (1 + margin);
                            height *= (1 + margin);

                            // 4. 设置新视图
                            var newView = (ViewTableRecord)originalView.Clone();
                            newView.CenterPoint = center;
                            newView.Width = width;
                            newView.Height = height;
                            ed.SetCurrentView(newView);

                            // 5. 截图
                            System.Threading.Thread.Sleep(100); // 等待视图刷新
                            var previewBmp = Autodesk.AutoCAD.ApplicationServices.DocumentExtension.CapturePreviewImage(doc, 400, 300);
                            if (previewBmp != null)
                            {
                                previewBmp.Save(previewPath, System.Drawing.Imaging.ImageFormat.Png);
                                dto.PreviewImagePath = previewPath;
                                fs.PreviewImagePath = previewPath;
                                fs.PreviewImageName = Path.GetFileName(previewPath);
                            }

                            // 6. 还原原视图
                            ed.SetCurrentView(originalView);
                        }
                        else
                        {
                            // 如果没有包围盒，退回原有全图快照
                            var previewBmp = Autodesk.AutoCAD.ApplicationServices.DocumentExtension.CapturePreviewImage(doc, 400, 300);
                            if (previewBmp != null)
                            {
                                previewBmp.Save(previewPath, System.Drawing.Imaging.ImageFormat.Png);
                                dto.PreviewImagePath = previewPath;
                                fs.PreviewImagePath = previewPath;
                                fs.PreviewImageName = Path.GetFileName(previewPath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.Instance.LogWarning($"生成预览图失败: {ex.Message}");
                    }

                    tr.Commit();
                }
            }
            return dto;
        }
    }
}