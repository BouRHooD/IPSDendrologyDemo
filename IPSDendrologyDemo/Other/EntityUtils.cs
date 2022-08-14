using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using MathNet.Spatial.Euclidean;
using Autocad = Autodesk.AutoCAD.DatabaseServices;

namespace IPSDendrologyDemo.Other
{
    public class EntityUtils
    {
        public static void Erase(Entity oEntity, bool notifyForDelete = true)
        {
            try
            {
                if (oEntity == null || oEntity.IsErased) { return; }

                if (notifyForDelete)
                {
                    var dialogResult = System.Windows.Forms.MessageBox.Show("Вы действительно хотите удалить объкт " + oEntity.Handle + "?\nInfo: dataGrid_SelectedRowChange IsErased", "Подтверждение для удаления", System.Windows.Forms.MessageBoxButtons.YesNo);
                    if (dialogResult != System.Windows.Forms.DialogResult.Yes)
                    {
                        return;
                    }
                }

                // Get the current document and database
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;

                // Start a transaction
                using (Transaction ts = acCurDb.TransactionManager.StartOpenCloseTransaction())
                {
                    if (!oEntity.IsWriteEnabled)
                        oEntity = ts.GetObject(oEntity.Id, OpenMode.ForWrite, false, true) as Entity;
                    oEntity.Erase(true);
                    ts.Commit();
                }
            }
            catch { }
        }

        public static void Erase(string inHandleValue, bool notifyForDelete = true)
        {
            try
            {
                if (string.IsNullOrEmpty(inHandleValue)) { return; }

                if (notifyForDelete)
                {
                    var dialogResult = System.Windows.Forms.MessageBox.Show("Вы действительно хотите удалить объкт " + inHandleValue + "?\nInfo: dataGrid_SelectedRowChange IsErased", "Подтверждение для удаления", System.Windows.Forms.MessageBoxButtons.YesNo);
                    if (dialogResult != System.Windows.Forms.DialogResult.Yes)
                    {
                        return;
                    }
                }

                // Get the current document and database
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                Database acCurDb = acDoc.Database;

                Entity oEntity = acCurDb.GetEntityByHandle(inHandleValue);

                // Start a transaction
                using (Transaction ts = acCurDb.TransactionManager.StartOpenCloseTransaction())
                {
                    try
                    {
                        if (oEntity == null || oEntity.IsErased)
                            return;

                        if (!oEntity.IsWriteEnabled)
                            oEntity = ts.GetObject(oEntity.Id, OpenMode.ForWrite, false, true) as Entity;

                        oEntity.Erase(true);
                        ts.Commit();
                    }
                    catch
                    {

                    }
                }
            }
            catch { }

        }
    }
}
