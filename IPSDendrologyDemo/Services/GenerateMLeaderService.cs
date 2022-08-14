using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using IPSDendrologyDemo.UIService;
using System.Collections.Generic;

namespace IPSDendrologyDemo.Services
{
    public static class GenerateMLeaderService
    {
        /// <summary>
        /// Блок выноски
        /// </summary>
        /// <param name="textMLeader"></param>
        /// <param name="pitPoint"></param>
        /// <returns></returns>
        public static MLeader CreateMLeaderBlock(string textMLeader, Point3d pitPoint)
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return null;
                var ed = doc.Editor;
                var db = doc.Database;

                // Запускаем транзакцию, так как мы будем двигать (Jig) объект-резидента базы данных
                using (var tr = db.TransactionManager.StartOpenCloseTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);

                    // Если не смогли получить координаты, то выходим из обработчика
                    if (pitPoint == null) { return null; }

                    // Генерируем итоговую точку вставки
                    Point3d End_ = new Point3d(pitPoint.X, pitPoint.Y, pitPoint.Z);

                    // Создаём и делаем нивидимым выноску MLeader (Creates a multileader object)
                    // (Это помогает избежать мерцания, когда мы начинаем Jig)
                    var ml = new MLeader();
                    ml.Visible = false;

                    // Get the current value from a system variable
                    int val_Regenmode = System.Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("REGENMODE"));
                    // Set system variable to new value
                    Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("REGENMODE", 0);

                    // Создаём Jig
                    var jig = new DirectionalLeaderJigBlock(textMLeader, pitPoint, End_, ml, ed);
                    jig.tr = tr;
                    jig.db = db;

                    // Добавляем MLeader в рисование: это позволяет отображать его
                    btr.AppendEntity(ml);
                    tr.AddNewlyCreatedDBObject(ml, true);

                    // Устанавливаем конечную точку в Jig
                    var res = ed.Drag(jig);

                    // Если все хорошо, принимаем изменения и записываем выноску к объекту (для обновления данных в выноске)
                    if (res != null)
                    {
                        if (res.Status == PromptStatus.OK)
                        {
                            // Подтверждает транзакцию 
                            tr.Commit();
                        }
                    }

                    Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("REGENMODE", 1);
                    ed.Regen();                                                                                         // Выполняем регенерацию модели, помогает от пропажи отображения
                    return ml;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Обычная выноска для объектов
        /// </summary>
        /// <param name="textMLeader"></param>
        /// <param name="pitPoint"></param>
        /// <returns></returns>
        public static MLeader CreateMLeaderStandart(string textMLeader, Point3d pitPoint)
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return null;
                var ed = doc.Editor;
                var db = doc.Database;

                // Получаем ID arrowhead для выноски 
                ObjectId arrId = DirectionalLeaderJig.GetArrowObjectId();

                // Запускаем транзакцию, так как мы будем двигать (Jig) объект-резидента базы данных
                using (var tr = db.TransactionManager.StartOpenCloseTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                    var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);

                    // Если не смогли получить координаты, то выходим из обработчика
                    if (pitPoint == null)
                        return null;

                    // Генерируем итоговую точку вставки
                    Point3d End_ = new Point3d(pitPoint.X, pitPoint.Y, pitPoint.Z);

                    // Создаём и делаем нивидимым выноску MLeader (Creates a multileader object)
                    // (Это помогает избежать мерцания, когда мы начинаем Jig)
                    var ml = new MLeader();
                    ml.Visible = false;
                    ml.ArrowSymbolId = arrId;

                    // Get the current value from a system variable
                    int val_Regenmode = System.Convert.ToInt32(Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("REGENMODE"));
                    // Set system variable to new value
                    Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("REGENMODE", 0);

                    // Создаём Jig
                    var jig = new DirectionalLeaderJig(textMLeader, pitPoint, End_, ml, ed);

                    // Добавляем MLeader в рисование: это позволяет отображать его
                    btr.AppendEntity(ml);
                    tr.AddNewlyCreatedDBObject(ml, true);

                    // Устанавливаем конечную точку в Jig
                    var res = ed.Drag(jig);

                    // Если все хорошо, принимаем изменения и записываем выноску к объекту (для обновления данных в выноске)
                    if (res != null)
                    {
                        if (res.Status == PromptStatus.OK)
                        {
                            // Присваиваем выноску к объекту (для обновления данных в выноске)
                            if (ml != null)
                            {
                                // Пытаемся присвоить стандартный стиль
                                try
                                {
                                    DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);
                                    if (mlStyles.Contains("Standard"))
                                    {
                                        var mlStyleEntity = mlStyles.GetAt("Standard");
                                        ml.MLeaderStyle = mlStyleEntity;
                                        var selectcolor = ml.MText.BackgroundFillColor;
                                    }
                                }
                                catch { }

                            }

                            // Подтверждает транзакцию 
                            tr.Commit();
                        }
                    }

                    Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("REGENMODE", 1);
                    ed.Regen();                                                                                         // Выполняем регенерацию модели, помогает от пропажи отображения
                    return ml;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
            return null;
        }
    }
}
