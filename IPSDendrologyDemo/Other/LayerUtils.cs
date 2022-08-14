using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSDendrologyDemo.Other
{
    public class LayerUtils
    {
        public static List<LayerTableRecord> GetAllThelayers()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            List<LayerTableRecord> oLayers = new List<LayerTableRecord>();
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // This example returns the layer table for the current database
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;

                // Step through the Layer table and print each layer name
                foreach (ObjectId acObjId in acLyrTbl)
                {
                    LayerTableRecord acLyrTblRec;
                    acLyrTblRec = acTrans.GetObject(acObjId, OpenMode.ForRead) as LayerTableRecord;
                    oLayers.Add(acLyrTblRec);
                }
                return oLayers;
                // Dispose of the transaction
            }
        }

        public static void CopyStylesTextFromDWG(string fileFullName)
        {
            DocumentCollection dm = Application.DocumentManager;
            Editor ed = dm.MdiActiveDocument.Editor;
            Database toDb = dm.MdiActiveDocument.Database;
            Database fromDb = new Database(false, true);
            try
            {
                // Считываем DWG в стороннюю базу данных
                fromDb.ReadDwgFile(fileFullName, System.IO.FileShare.Read, true, "");
                // Копируем слоя из исходной (от куда копируем) базы данных в базу данных назначения (куда копируем)
                ObjectIdCollection idsStyles = new ObjectIdCollection();
                // Копируем все слои из исходного (от куда копируем) чертежа
                using (var oStyle = fromDb.DimStyleTableId.Open(OpenMode.ForRead) as DimStyleTable)
                {
                    foreach (ObjectId idStyle in oStyle) idsStyles.Add(idStyle);
                }
                // Копируем все состояния слоёв
                IdMapping maps = new IdMapping();
                fromDb.WblockCloneObjects(idsStyles, toDb.DimStyleTableId, maps, DuplicateRecordCloning.Ignore, false);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage("\nError during copy: " + ex.Message);
            }
            toDb.Dispose();
            fromDb.Dispose();
        }

        public static void CopyStylesMLeaderFromDWG(string fileFullName)
        {
            DocumentCollection dm = Application.DocumentManager;
            Editor ed = dm.MdiActiveDocument.Editor;
            Database toDb = dm.MdiActiveDocument.Database;
            Database fromDb = new Database(false, true);
            try
            {
                // Считываем DWG в стороннюю базу данных
                fromDb.ReadDwgFile(fileFullName, System.IO.FileShare.Read, true, "");
                // Копируем слоя из исходной (от куда копируем) базы данных в базу данных назначения (куда копируем)
                ObjectIdCollection idsStyles = new ObjectIdCollection();
                // Копируем все слои из исходного (от куда копируем) чертежа
                using (var dbDictionaryStyle = fromDb.MLeaderStyleDictionaryId.Open(OpenMode.ForRead) as DBDictionary)
                {
                    foreach (DBDictionaryEntry oStyle in dbDictionaryStyle) idsStyles.Add(oStyle.Value);
                }
                // Копируем все состояния слоёв
                IdMapping maps = new IdMapping();
                fromDb.WblockCloneObjects(idsStyles, toDb.MLeaderStyleDictionaryId, maps, DuplicateRecordCloning.Ignore, false);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage("\nError during copy: " + ex.Message);
            }
            toDb.Dispose();
            fromDb.Dispose();
        }

        public static void CopyLayersFromDWG(string fileFullName)
        {
            DocumentCollection dm = Application.DocumentManager;
            Editor ed = dm.MdiActiveDocument.Editor;
            Database toDb = dm.MdiActiveDocument.Database;
            Database fromDb = new Database(false, true);
            try
            {
                // Считываем DWG в стороннюю базу данных
                fromDb.ReadDwgFile(fileFullName, System.IO.FileShare.Read, true, "");
                // Копируем слоя из исходной (от куда копируем) базы данных в базу данных назначения (куда копируем)
                ObjectIdCollection idsLayers = new ObjectIdCollection();
                // Копируем все слои из исходного (от куда копируем) чертежа
                using (LayerTable lt = fromDb.LayerTableId.Open(OpenMode.ForRead) as LayerTable)
                {
                    foreach (ObjectId idLayer in lt) idsLayers.Add(idLayer);
                }
                // Копируем все состояния слоёв
                IdMapping maps = new IdMapping();
                fromDb.WblockCloneObjects(idsLayers, toDb.LayerTableId, maps, DuplicateRecordCloning.Ignore, false);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage("\nError during copy: " + ex.Message);
            }
            toDb.Dispose();
            fromDb.Dispose();
        }
    }
}
