using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSDendrologyDemo.Other
{
    public static class BlockUtils
    {
        public static string GetBlockRealName(this BlockReference oBlockRef)
        {
            if (oBlockRef == null)
                return string.Empty;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            BlockTableRecord block = null;
            using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
            {

                //get the real dynamic block name.
                block = ts.GetObject(oBlockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                if (block == null)
                {
                    block = ts.GetObject(oBlockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                }
                ts.Commit();
            }
            return block.Name;
        }

        public static bool IsBlockExist(string blockName)
        {
            Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = adoc.Database;

            bool isBlockExists = false;
            using (Transaction acTrans = db.TransactionManager.StartOpenCloseTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                isBlockExists = acBlkTbl.Has(blockName);
                acTrans.Commit();
            }
            return isBlockExists;
        }

        public static void TryLoadBlockFromAnotherFile(string filePath, string blockName)
        {
            DocumentCollection dm = Application.DocumentManager;
            Editor ed = dm.MdiActiveDocument.Editor;
            Database destDb = dm.MdiActiveDocument.Database;
            Database sourceDb = new Database(false, true);
            try
            {
                // Read the DWG into a side database
                sourceDb.ReadDwgFile(filePath, System.IO.FileShare.Read, true, "");

                // Create a variable to store the list of block identifiers
                ObjectIdCollection blockIds = new ObjectIdCollection();

                Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = sourceDb.TransactionManager;
                using (Transaction myT = tm.StartOpenCloseTransaction())
                {
                    // Open the block table
                    BlockTable bt = (BlockTable)myT.GetObject(sourceDb.BlockTableId, OpenMode.ForRead, false);

                    // Check each block in the block table
                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr = (BlockTableRecord)myT.GetObject(btrId, OpenMode.ForRead, false);
                        // Only add named & non-layout blocks to the copy list
                        if (!btr.IsLayout && btr.Name.Equals(blockName))
                            blockIds.Add(btrId);

                        btr.Dispose();
                    }
                }
                // Copy blocks from source to destination database
                IdMapping mapping = new IdMapping();
                sourceDb.WblockCloneObjects(blockIds, destDb.BlockTableId, mapping, DuplicateRecordCloning.Replace, false);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage("\nError during copy: " + ex.Message);
            }
            sourceDb.Dispose();
        }

        public static void SetDynamicPropertyValueOfABlock(BlockReference oBlockRef, string propertyName, object value)
        {
            Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = adoc.Database;
            using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
            {
                if (!oBlockRef.IsWriteEnabled)
                {
                    try
                    {
                        oBlockRef = ts.GetObject(oBlockRef.Id, OpenMode.ForWrite, false, true) as BlockReference;
                    }
                    catch
                    {
                        if (!oBlockRef.IsWriteEnabled) { return; }
                    }
                }

                DynamicBlockReferencePropertyCollection properties = oBlockRef.DynamicBlockReferencePropertyCollection;
                for (int i = 0; i < properties.Count; i++)
                {
                    DynamicBlockReferenceProperty property = properties[i];
                    if (property.PropertyName == propertyName)
                    {
                        property.Value = value;
                        break;
                    }
                }
                ts.Commit();
            }
        }

        /// <summary>
        /// Берется свойство динамического блока<br/>
        /// Например, расстояния и т.п.
        /// </summary>
        /// <param name="oBlockRef"></param>
        /// <param name="propertyName"></param>
        /// <param name="offIsNotifying"></param>
        /// <returns></returns>
        public static string GetDynamicPropertyOfABlock(BlockReference oBlockRef, string propertyName, bool offIsNotifying = false)
        {
            if (!oBlockRef.IsNotifying && !offIsNotifying)
                return string.Empty;

            Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = adoc.Database;
            ObjectId blkRecId = ObjectId.Null;
            Editor ed = adoc.Editor;
            string propertyValue = String.Empty;
            using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
            {
                if (!oBlockRef.IsReadEnabled)
                {
                    try
                    {
                        oBlockRef = ts.GetObject(oBlockRef.Id, OpenMode.ForRead, false, true) as BlockReference;
                    }
                    catch { }
                }

                DynamicBlockReferencePropertyCollection properties = oBlockRef.DynamicBlockReferencePropertyCollection;
                for (int i = 0; i < properties.Count; i++)
                {
                    DynamicBlockReferenceProperty property = properties[i];
                    if (property.PropertyName == propertyName)
                    {
                        propertyValue = property.Value.ToString();
                        break;
                    }
                }
                ts.Commit();
            }
            return propertyValue;
        }

        // Создаём новый блок (на самом деле копируем существующий блок)
        public static BlockReference CreateBlockReference(string blockName, Point3d insertPoint)
        {
            //insertPoint = calc_center_block(blockName, insertPoint);          // Пересчитываем центра координат, если есть смещение от центра
            ObjectId objId = InsertABlock(blockName, insertPoint);
            BlockReference blockRef = null;
            if (objId.IsNull)
                return blockRef;

            Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = adoc.Database;
            using (Transaction ts = db.TransactionManager.StartTransaction())
            {
                blockRef = ts.GetObject(objId, OpenMode.ForWrite, false, true) as BlockReference;
                ts.Commit();
            }

            return blockRef;
        }

        public static ObjectId InsertABlock(string blockName, Point3d insertionPointOfABlock)
        {
            Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = adoc.Database;
            ObjectId blkRecId = ObjectId.Null;
            Editor ed = adoc.Editor;
            BlockReference acBlkRef = null;
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (!acBlkTbl.Has(blockName))
                {
                    ed.WriteMessage("Нет такого блока");
                    return ObjectId.Null;
                }
                else
                {
                    blkRecId = acBlkTbl[blockName];
                }

                if (blkRecId != ObjectId.Null)
                {
                    acBlkRef = new BlockReference(insertionPointOfABlock, blkRecId);
                    BlockTableRecord acCurSpaceBlkTblRec;
                    acCurSpaceBlkTblRec = acTrans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, true, true) as BlockTableRecord;
                    acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                    acTrans.AddNewlyCreatedDBObject(acBlkRef, true);
                    // Save the new object to the database
                }
                acTrans.Commit();
            }
            return acBlkRef.Id;
        }

    }
}
