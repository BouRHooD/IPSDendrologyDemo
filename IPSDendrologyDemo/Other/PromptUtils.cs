using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using System;
using System.Collections.Generic;
using Civil = Autodesk.Civil.DatabaseServices;

namespace IPSDendrologyDemo.Other
{
    public class PromptUtils
    {
        public static Point3d promptAPoint(string promptMessage = "Pick a point", string rejectMessage = "Error")
        {
            Document adoc = Application.DocumentManager.MdiActiveDocument;
            Database db = adoc.Database;
            Editor ed = adoc.Editor;
            using (Transaction ts = db.TransactionManager.StartTransaction())
            {
                PromptPointOptions opt = new PromptPointOptions(promptMessage);
                opt.Message = promptMessage;
                Point3d point = ed.GetPoint(opt).Value;

                Matrix3d curUCSMatrix = ed.CurrentUserCoordinateSystem;
                CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;
                Matrix3d curWCSMatrix = Matrix3d.AlignCoordinateSystem(
                    Point3d.Origin,
                    Vector3d.XAxis,
                    Vector3d.YAxis,
                    Vector3d.ZAxis,
                    curUCS.Origin,
                    curUCS.Xaxis,
                    curUCS.Yaxis,
                    curUCS.Zaxis
                    );

                Point3d pointCoordWCS = point.TransformBy(curWCSMatrix);

                ts.Commit();
                return pointCoordWCS;
            }
        }

        public static List<Entity> promptEntities(string messageForAdding = "Выберите объекты")
        {
            Document adoc = Application.DocumentManager.MdiActiveDocument;
            Database db = adoc.Database;
            Editor ed = adoc.Editor;
            var entityList = new List<Entity>();
            PromptSelectionOptions opt = new PromptSelectionOptions();
            opt.MessageForAdding = messageForAdding;
            PromptSelectionResult pipesPrompt = ed.GetSelection(opt);

            // If the prompt status is OK, objects were selected
            if (pipesPrompt.Status == PromptStatus.OK)
            {
                SelectionSet entitiesSet = pipesPrompt.Value;
                // Step through the objects in the selection set
                using (Transaction ts = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject entityObj in entitiesSet)
                    {
                        // Check to make sure a valid SelectedObject object was returned
                        if (entityObj != null)
                        {
                            var oEntity = ts.GetObject(entityObj.ObjectId, OpenMode.ForWrite, true, true) as Entity;
                            if (oEntity != null)
                            {
                                entityList.Add(oEntity);
                            }
                        }
                    }
                    ts.Commit();
                }
            }
            return entityList;
        }


    }
}
