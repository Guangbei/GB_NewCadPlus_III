using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace GB_NewCadPlus_III
{
    /// <summary>
    /// AutoCAD操作工具类
    /// </summary>
    public static class AutoCadHelper
    {
        /// <summary>
        /// 把二进制 DWG 写入到临时文件并返回路径，调用者负责删除（如果需要）
        /// 中文注释：用于把从服务器下载到内存的 DWG（二进制）写入磁盘，供 AutoCAD API 读取。
        /// </summary>
        public static string SaveBytesToTempDwg(byte[] dwgBytes, string? hintName = null)
        {
            // 使用用户临时目录，避免与程序资源目录冲突
            string tempDir = Path.Combine(Path.GetTempPath(), "GB_CADTools", "TempDwg");
            Directory.CreateDirectory(tempDir);
            // 安全文件名
            string fileName = string.IsNullOrWhiteSpace(hintName) ? $"tmp_{DateTime.Now:yyyyMMddHHmmssfff}.dwg" : $"{SanitizeFileName(hintName)}_{DateTime.Now:yyyyMMddHHmmssfff}.dwg";
            string fullPath = Path.Combine(tempDir, fileName);
            File.WriteAllBytes(fullPath, dwgBytes);
            return fullPath;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        /// <summary>
        /// 从字节数组导入块定义到当前数据库（通过临时文件实现）。
        /// 返回导入后的块定义 ObjectId 或 ObjectId.Null（失败）。
        /// 注：使用时需要在外层事务/DBTrans 中调用以避免重复导入/线程问题。
        /// </summary>
        public static ObjectId ImportBlockDefinitionFromBytes(byte[] dwgBytes, string blockName, DBTrans dbTr)
        {
            // 写临时文件
            string tmpFile = SaveBytesToTempDwg(dwgBytes, blockName);
            try
            {
                return ImportBlockDefinitionToCurrentDatabase(tmpFile, blockName, dbTr);
            }
            finally
            {
                // 尝试删除临时文件（失败不抛）
                try { if (File.Exists(tmpFile)) File.Delete(tmpFile); } catch { }
            }
        }

        /// <summary>
        /// 将 byte[] DWG 临时写盘后在当前文档中插入一个 BlockReference（包含属性），并返回插入的 ObjectId。
        /// 这个方法是对已有 InsertBlockFromExternalDwg 的补充：支持从内存直接插入。
        /// </summary>
        public static ObjectId InsertBlockFromExternalDwg(byte[] dwgBytes, string blockName, Point3d insertPoint)
        {
            string tmpFile = SaveBytesToTempDwg(dwgBytes, blockName);
            try
            {
                return InsertBlockFromExternalDwg(tmpFile, blockName, insertPoint);
            }
            finally
            {
                try { if (File.Exists(tmpFile)) File.Delete(tmpFile); } catch { }
            }
        }

        /// <summary>
        /// 从外部 DWG 导入指定块定义到当前文档并在目标点插入一个 BlockReference（包含属性）
        /// 返回插入的 BlockReference 的 ObjectId，失败返回 ObjectId.Null。
        /// （保留原有实现，已做健壮性和注释增强）
        /// </summary>
        public static ObjectId InsertBlockFromExternalDwg(string dwgPath, string blockName, Point3d insertPoint)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return ObjectId.Null;

            // 保证在 AutoCAD 主线程并加文档锁
            using (doc.LockDocument())
            {
                try
                {
                    // 读取外部 DWG 到临时 Database
                    using (var sourceDb = new Database(false, true))
                    {
                        sourceDb.ReadDwgFile(dwgPath, System.IO.FileShare.Read, true, null);

                        using (var sourceTr = sourceDb.TransactionManager.StartTransaction())
                        {
                            var sourceBt = (BlockTable)sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                            if (!sourceBt.Has(blockName))
                                return ObjectId.Null;

                            ObjectId sourceBtrId = sourceBt[blockName];

                            // 克隆到当前文档数据库（一次性把块定义导入目标 DB）
                            IdMapping mapping = new IdMapping();
                            sourceDb.WblockCloneObjects(new ObjectIdCollection { sourceBtrId },
                                                        doc.Database.BlockTableId,
                                                        mapping,
                                                        DuplicateRecordCloning.Replace,
                                                        false);

                            sourceTr.Commit();

                            if (!mapping.Contains(sourceBtrId))
                                return ObjectId.Null;

                            ObjectId newBtrId = mapping[sourceBtrId].Value;

                            // 在当前文档开启事务并插入 BlockReference（并正确处理属性）
                            using (var tr = doc.Database.TransactionManager.StartTransaction())
                            {
                                // 获取目标模型空间（写模式）
                                var ms = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                                // 创建 BlockReference 引用新块定义
                                var blockRef = new BlockReference(insertPoint, newBtrId);

                                // 把 BlockReference 加入模型空间并注册
                                ms.AppendEntity(blockRef);
                                tr.AddNewlyCreatedDBObject(blockRef, true);

                                // 读取目标数据库中新克隆的块表记录（以只读方式）
                                var btr = (BlockTableRecord)tr.GetObject(newBtrId, OpenMode.ForRead);

                                // 如果块定义包含属性定义，逐一创建 AttributeReference 并追加到 blockRef
                                if (btr.HasAttributeDefinitions)
                                {
                                    foreach (ObjectId id in btr)
                                    {
                                        var dbObj = tr.GetObject(id, OpenMode.ForRead);
                                        if (dbObj is AttributeDefinition attDef && !attDef.Constant)
                                        {
                                            // 创建属性引用，并从定义设置默认值（相对于块）
                                            var attRef = new AttributeReference();
                                            attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);

                                            // 必须在把属性附加到 BlockReference 后调用 AddNewlyCreatedDBObject
                                            blockRef.AttributeCollection.AppendAttribute(attRef);
                                            tr.AddNewlyCreatedDBObject(attRef, true);
                                        }
                                    }
                                }

                                tr.Commit();
                                return blockRef.ObjectId;
                            }
                        }
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n插入块失败: {ex.Message}");
                    return ObjectId.Null;
                }
                catch (Exception ex)
                {
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n未知错误: {ex.Message}");
                    return ObjectId.Null;
                }
            }
        }

        // 其余已存在的方法不变...
        /// <summary>
        /// 从源数据库中导入指定块
        /// </summary>
        /// <param name="dwgPath">DWG 文件路径</param>
        /// <param name="blockName">块名称</param>
        /// <param name="dbTr">目标数据库事务</param>
        /// <returns>目标数据库中新块记录的 ObjectId（失败返回 ObjectId.Null）</returns>
        public static ObjectId ImportBlockDefinitionToCurrentDatabase(string dwgPath, string blockName, DBTrans dbTr)
        {
            if (!File.Exists(dwgPath)) return ObjectId.Null;// 文件不存在 

            using (var sourceDb = new Database(false, true))// 创建源数据库 只读打开
            {
                sourceDb.ReadDwgFile(dwgPath, FileShare.ReadWrite, true, null);// 打开源数据库 读取 DWG 文件

                using (var sourceTr = sourceDb.TransactionManager.StartTransaction())// 开始事务 启动源数据库事务
                {
                    var sourceBt = (BlockTable)sourceTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);// 获取块表 获取源数据库块表
                    if (!sourceBt.Has(blockName)) return ObjectId.Null;// 源数据库没有该块 块定义不存在

                    ObjectId sourceBtrId = sourceBt[blockName];// 获取块记录 获取源块记录 ID

                    // 直接从 sourceDb 克隆到 targetTr.Database（目标数据库）
                    var ids = new ObjectIdCollection { sourceBtrId };// 创建 ObjectId 集合 包含源块记录 ID
                    var mapping = new IdMapping();
                    sourceDb.WblockCloneObjects(ids, dbTr.Database.BlockTableId, mapping, DuplicateRecordCloning.Replace, false);// 用 WblockCloneObjects 克隆块记录 到目标数据库

                    sourceTr.Commit();// 提交源数据库事务

                    return mapping.Contains(sourceBtrId) ? mapping[sourceBtrId].Value : ObjectId.Null;// 返回目标数据库中新块记录的 ObjectId
                }
            }
        }

        /// <summary>
        /// 把一个来自其它数据库的实体（已从源读取）以安全方式复制到当前事务所在数据库并返回新实体的 ObjectId。
        /// 适用于单个实体：先在源上用 WblockCloneObjects 克隆其所属块记录，或直接创建 GetTransformedCopy 然后在 targetTr 中 Add。
        /// </summary>
        public static ObjectId CloneEntityIntoCurrentDatabase(Entity sourceEntity, DBTrans dbTr)
        {
            // 更稳妥的方式是使用 GetTransformedCopy（会返回一个新的实体实例）
            var clone = sourceEntity.GetTransformedCopy(Matrix3d.Identity);// 获取实体的变换副本（无变换）
            var id = dbTr.CurrentSpace.AddEntity(clone);// 在当前空间中添加实体 将克隆实体添加到当前空间 并获取新实体的 ObjectId
            return id;
        }

        public static void ExecuteInDocumentTransaction(Action<Document, Transaction> action)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) throw new InvalidOperationException("当前没有活动文档。");
            using (doc.LockDocument())
            {
                var db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    action(doc, tr);
                    tr.Commit();
                }
            }
        }
        /// <summary>
        /// 在活动文档中执行事务
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <param name="func"> </param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static T ExecuteInDocumentTransaction<T>(Func<Document, Transaction, T> func)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) throw new InvalidOperationException("当前没有活动文档。");
            using (doc.LockDocument())
            {
                var db = doc.Database;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    T result = func(doc, tr);
                    tr.Commit();
                    return result;
                }
            }
        }
    }
}