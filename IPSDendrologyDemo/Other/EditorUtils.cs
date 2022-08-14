using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IPSDendrologyDemo.Other
{
    public class EditorUtils
    {
        /// <summary>
        /// Масштабируемся на объекте и помечаем его как выбранный в активном документе
        /// </summary>
        /// <param name="oEntity"> Сущность на котором масштабируемся </param>
        public static void ZoomAndSelect(Entity oEntity)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;
                // Extract its extents
                Extents3d entityExtents;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    entityExtents = oEntity.GeometricExtents;

                    Matrix3d pWCS = ed.CurrentUserCoordinateSystem;
                    var ucs = ed.CurrentUserCoordinateSystem.Inverse();
                    if ((pWCS.CoordinateSystem3d.Origin.X == 0 || pWCS.CoordinateSystem3d.Origin.X == 1) && (pWCS.CoordinateSystem3d.Origin.Y == 0 || pWCS.CoordinateSystem3d.Origin.Y == 1))
                    {
                        entityExtents.TransformBy(ed.CurrentUserCoordinateSystem.Inverse());
                    }
                    else
                    {
                        //entityExtents.TransformBy(ed.CurrentUserCoordinateSystem);
                    }
                    // Call our helper function
                    // [Change this to ZoomWin2 or WoomWin3 to
                    // use different zoom techniques]
                    Point2d min2d = new Point2d(entityExtents.MinPoint.X, entityExtents.MinPoint.Y);
                    Point2d max2d = new Point2d(entityExtents.MaxPoint.X, entityExtents.MaxPoint.Y);
                    ViewTableRecord view = new ViewTableRecord();
                    view.CenterPoint = min2d + ((max2d - min2d) / 2.0);
                    view.Height = max2d.Y - min2d.Y;
                    view.Width = max2d.X - min2d.X;
                    ed.SetCurrentView(view);
                    try
                    {
                        ed.SetImpliedSelection(new ObjectId[] { oEntity.Id });
                    }
                    catch
                    {

                    }
                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        public static void SetImpliedSelection_Objects(ObjectId[] objIDs)
        {
            try
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;
                using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
                {
                    try
                    {
                        ObjectId[] select_objects = objIDs;
                        ed.SetImpliedSelection(select_objects);
                    }
                    catch (Exception)
                    {
                        ts.Abort();
                        return;
                    }
                    ts.Commit();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }


    }
}
