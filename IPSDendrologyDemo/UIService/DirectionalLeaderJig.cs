using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace IPSDendrologyDemo.UIService
{

    class DirectionalLeaderJig : EntityJig
    {
        private Point3d _start, _end;
        private string _contents;
        private int _index;
        private int _lineIndex;
        private bool _started;
        private Editor _ed;

        public DirectionalLeaderJig(string txt, Point3d start, Point3d end, MLeader ld, Editor ed) : base(ld)
        {
            // Храним информацию, которая будет добавлена в "Выноску", но здесь нельзя инициализировать MLeader
            _contents = txt;
            _start = start;
            _end = end;
            _ed = ed;
            _started = false;
        }

        // Фунция пробоотборника
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var po = new JigPromptPointOptions();
            po.UserInputControls = (UserInputControls.Accept3dCoordinates | UserInputControls.NoNegativeResponseAccepted);
            po.Message = "\nКонечная точка и направление";

            var res = prompts.AcquirePoint(po);

            if (_end == res.Value)
            {
                return SamplerStatus.NoChange;
            }
            else if (res.Status == PromptStatus.OK)
            {
                _end = res.Value;
                return SamplerStatus.OK;
            }

            return SamplerStatus.Cancel;
        }

        protected override bool Update()
        {
            var ml = (MLeader)Entity;
            if (!_started)
            {
                if (_start.DistanceTo(_end) > Tolerance.Global.EqualPoint)
                {
                    // Когда Jig начинается и есть движение мыши
                    // Мы создаём текст и инициализируем MLeader
                    ml.ContentType = ContentType.MTextContent;
                    var mt = new MText();
                    mt.Contents = _contents;                                                    // Присваиваем текст МултиТексту
                    mt.TextHeight = 1.7500;
                    mt.Rotation = 0;

                    // Ставим непрозрачную маску на фон текста на выноске
                    try
                    {
                        mt.BackgroundFill = true;
                        mt.BackgroundScaleFactor = 1.19;
                        mt.UseBackgroundColor = true;
                        mt.BackgroundFillColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, 1);
                        mt.BackgroundTransparency = new Autodesk.AutoCAD.Colors.Transparency(255);
                    }
                    catch { }

                    // Рамка текста
                    try
                    {
                        mt.ShowBorders = true;
                    }
                    catch { }

                    ml.MText = mt;                                                              // Присваиваем МултиТекст MLeaderу
                    ml.LandingGap = 1;                                                          // Отступ от полки
                    ml.ExtendLeaderToText = true;                                               // Автоматическое расширение выноски
                    ml.LineWeight = LineWeight.LineWeight030;                                   // Вес линии (В стройгенплане 0,30)
                    ml.LeaderLineType = LeaderType.StraightLeader;                              // Тип выноски (Spline - кривая)
                    // ml.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0);               // Цвет Линии
                    ml.TextAlignmentType = TextAlignmentType.CenterAlignment;                   // Расположение текста по середине 
                    ml.TextAttachmentDirection = TextAttachmentDirection.AttachmentHorizontal;  // Направление расположения текста 
                    ml.TextAngleType = TextAngleType.AlwaysRightReadingAngle;
                    ml.EnableLanding = true;
                    ml.HasSaveVersionOverride = true;
                    ml.TextHeight = 1.7500;
                    ml.DoglegLength = 0.3600;
                    ml.EnableLanding = true;
                    

                    // Так как не ставилось на некоторые значения, то ставим на все
                    // AttachmentBottomLine - Подчёркивание последней строки; AttachmentBottomOfTopLine - Подчёркивание первой строки;
                    ml.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.UnknownLeader);
                    ml.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.TopLeader);
                    ml.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.RightLeader);
                    ml.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.LeftLeader);
                    ml.SetTextAttachmentType(TextAttachmentType.AttachmentBottomOfTopLine, LeaderDirectionType.BottomLeader);

                    // Создаём кластер MLeader и добавляем линию в него
                    _index = ml.AddLeader();
                    _lineIndex = ml.AddLeaderLine(_index);

                    // Устанавливаем вершины на линии
                    ml.AddFirstVertex(_lineIndex, _start);
                    ml.AddLastVertex(_lineIndex, _end);

                    // Убеждаемся, что мы не будет делать эту часть кода снова
                    _started = true;
                }
            }
            else
            {
                // Делаем видимым MLeader только после второго захода в код
                // (это также помогает избежать некоторого странного мерцания геометрии)
                ml.Visible = true;
                // У нас уже есть линия, поэтому просто установим ее последнюю вершину
                ml.SetLastVertex(_lineIndex, _end);
                //_ed.Regen();
            }

            if (_started)
            {
                // Установите направление текста в зависимости от X конечной точки
                // (т.е. находится ли он слева или справа от начальной точки?)
                var dl = new Vector3d((_end.X >= _start.X ? 1 : -1), 0, 0);
                ml.SetDogleg(_index, dl);
            }
            return true;
        }

        // Получаем ID arrowhead для выноски 
        public static ObjectId GetArrowObjectId(string newArrName = "_None")
        {
            // _Dot - Точка
            // _DotBlank - Указатель начала
            // _None - Без arrowhead of MLeader
            ObjectId arrObjId = ObjectId.Null;
            try
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

                Database db = doc.Database;

                // Get the current value of DIMBLK
                string oldArrName = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("DIMBLK") as string;

                // Set DIMBLK to the new style
                // (this action may create a new block)
                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("DIMBLK", newArrName);

                // Reset the previous value of DIMBLK
                if (oldArrName.Length != 0)
                    Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("DIMBLK", oldArrName);

                // Now get the objectId of the block
                Transaction tr = db.TransactionManager.StartTransaction();
                using (tr)
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    arrObjId = bt[newArrName];
                    tr.Commit();
                }
            }
            catch { }
            return arrObjId;
        }

    }

    class DirectionalLeaderJigBlock : EntityJig
    {
        private Point3d _start, _end;
        private string _contents;
        private int _index;
        private int _lineIndex;
        private bool _started;
        private Editor _ed;
        public Autodesk.AutoCAD.DatabaseServices.OpenCloseTransaction tr;
        public Database db;

        public DirectionalLeaderJigBlock(string txt, Point3d start, Point3d end, MLeader ld, Editor ed) : base(ld)
        {
            // Храним информацию, которая будет добавлена в "Выноску", но здесь нельзя инициализировать MLeader
            _contents = txt;
            _start = start;
            _end = end;
            _ed = ed;
            _started = false;
        }

        // Фунция пробоотборника
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var po = new JigPromptPointOptions();
            po.UserInputControls = (UserInputControls.Accept3dCoordinates | UserInputControls.NoNegativeResponseAccepted);
            po.Message = "\nКонечная точка и направление";

            var res = prompts.AcquirePoint(po);

            if (_end == res.Value)
            {
                return SamplerStatus.NoChange;
            }
            else if (res.Status == PromptStatus.OK)
            {
                _end = res.Value;
                return SamplerStatus.OK;
            }

            return SamplerStatus.Cancel;
        }

        protected override bool Update()
        {
            var ml = (MLeader)Entity;
            if (!_started)
            {
                if (_start.DistanceTo(_end) > Tolerance.Global.EqualPoint)
                {
                    // Когда Jig начинается и есть движение мыши
                    // Обновляем параметры блока
                    updateValueMLeaderBlock(tr, db, ml, _contents);

                    // Создаём кластер MLeader и добавляем линию в него
                    _index = ml.AddLeader();
                    _lineIndex = ml.AddLeaderLine(_index);

                    // Устанавливаем вершины на линии
                    ml.AddFirstVertex(_lineIndex, _start);
                    ml.AddLastVertex(_lineIndex, _end);

                    // Убеждаемся, что мы не будет делать эту часть кода снова
                    _started = true;
                }
            }
            else
            {
                // Делаем видимым MLeader только после второго захода в код
                // (это также помогает избежать некоторого странного мерцания геометрии)
                ml.Visible = true;
                // У нас уже есть линия, поэтому просто установим ее последнюю вершину
                ml.SetLastVertex(_lineIndex, _end);
                //_ed.Regen();
            }

            if (_started)
            {
                // Установите направление текста в зависимости от X конечной точки
                // (т.е. находится ли он слева или справа от начальной точки?)
                var dl = new Vector3d((_end.X >= _start.X ? 1 : -1), 0, 0);
                ml.SetDogleg(_index, dl);
            }
            return true;
        }

        public static void updateValueMLeaderBlock(Autodesk.AutoCAD.DatabaseServices.OpenCloseTransaction tr, Database db, MLeader ml, string _contents)
        {
            // Пытаемся присвоить стиль с блоком
            DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);
            ObjectId mlStyleEntity = new ObjectId();
            if (mlStyles.Contains("_Пр ДЕНДРО СТИЛЬ выноски")) { mlStyleEntity = mlStyles.GetAt("_Пр ДЕНДРО СТИЛЬ выноски"); }
            if (mlStyleEntity.IsValid && !mlStyleEntity.IsNull) { ml.MLeaderStyle = mlStyleEntity; }

            // ----- Контент 
            // Получаем список атрибутов
            if (ml.BlockContentId.IsNull) { return; }
            BlockTableRecord brML = (BlockTableRecord)tr.GetObject(ml.BlockContentId, OpenMode.ForRead);
            List<ObjectId> atributeMleaderIdList = new List<ObjectId>();
            foreach (ObjectId id in brML)
            {
                AttributeDefinition adef = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                if (adef != null) { atributeMleaderIdList.Add(id); }
            }

            // Присваиваем атрибут с текстом (1-й это номер выноски)
            foreach (var atrId in atributeMleaderIdList)
            {
                AttributeReference aref = ml.GetBlockAttribute(atrId);
                if (aref == null) { continue; }
                if (aref.Tag.Equals("№_ПЕРЕЧЕТКА"))
                {
                    aref.TextString = _contents;
                    aref.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0);
                    ml.SetBlockAttribute(atrId, aref);
                }
            }

            // Присваиваем цвет атрибуту 
            brML = (BlockTableRecord)tr.GetObject(ml.BlockContentId, OpenMode.ForRead);
            foreach (ObjectId id in brML)
            {
                AttributeDefinition adef = tr.GetObject(id, OpenMode.ForWrite) as AttributeDefinition;
                if (adef == null) { continue; }
                if (adef.Tag.Equals("№_ПЕРЕЧЕТКА"))
                {
                    adef.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0);
                }
            }
        }

        // Получаем ID arrowhead для выноски 
        public static ObjectId GetArrowObjectId(string newArrName = "_None")
        {
            // _Dot - Точка
            // _DotBlank - Указатель начала
            // _None - Без arrowhead of MLeader
            ObjectId arrObjId = ObjectId.Null;
            try
            {
                Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

                Database db = doc.Database;

                // Get the current value of DIMBLK
                string oldArrName = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("DIMBLK") as string;

                // Set DIMBLK to the new style
                // (this action may create a new block)
                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("DIMBLK", newArrName);

                // Reset the previous value of DIMBLK
                if (oldArrName.Length != 0)
                    Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("DIMBLK", oldArrName);

                // Now get the objectId of the block
                Transaction tr = db.TransactionManager.StartTransaction();
                using (tr)
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    arrObjId = bt[newArrName];
                    tr.Commit();
                }
            }
            catch { }
            return arrObjId;
        }

    }
}
