using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using IPSDendrologyDemo.Other;
using IPSDendrologyDemo.Services;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace IPSDendrologyDemo.ViewModels
{
    public static class Blocks
    {
        public static string fileName = "блоки дендроплан.dwg";
        public static string pointBlockReferenceName = "Дерево";
        public static string mLeaderBlockReferenceName = "Номер перечетка";
    }

    public class MainViewViewModel : ViewModelBase
    {
        #region ##### Init DelegateCommand
        public DelegateCommand AddEntityCommand { get; }
        public DelegateCommand AddExistingEntityCommand { get; }
        public DelegateCommand СreateLeaderOnEntity { get; }
        public DelegateCommand ZoomAndSelectEntityCommand { get; }
        public DelegateCommand ExcelExportCommand { get; }
        public DelegateCommand RemoveRowCommand { get; }
        #endregion

        public MainViewViewModel()
        {
            LoadDendrologyServiceList();                                                                                                     // Загружаем таблицу в DendrologyServiceList из документа dwg (handle объектов хранится в таблице свойств dwg)

            AppData.PreloadBlocks();                                                                                                         // Загружаем блок котлована из файла, чтобы использовать в новых документах
            AppData.PreloadLayers();                                                                                                         // Загружаем слои из файла, чтобы использовать в новых документах
            AppData.PreloadStyles();                                                                                                         // Загружаем стили из файла, чтобы использовать в новых документах

            AddExistingEntityCommand = new DelegateCommand(OnAddExistingEntityCommand);                                                      // Косанда по добавлению существующих в документе объектов в таблицу
            AddEntityCommand = new DelegateCommand(OnAddEntityCommand);                                                                      // Команда по добавлению объекта в таблицу "GenPlanServiceList", простановке индексов на углах и выставления выноски
            СreateLeaderOnEntity = new DelegateCommand(OnСreateLeaderOnEntity);                                                              // Команда по созданию выносок на объекты таблицы
            ZoomAndSelectEntityCommand = new DelegateCommand(OnZoomAndSelectEntityCommand);                                                  // Команда по фокусировании на объекте в документе из таблицы приложения                    
            RemoveRowCommand = new DelegateCommand(OnRemoveRowCommand);                                                                      // Команда по удалению объекта из таблицы и из приложения

            AppData.Editor.SelectionAdded += OnSelectionAdded;                                                                               // Вешаем событие на отловку объектов, выбранных в документе
        }

        public void OnAddExistingEntityCommand()
        {
            try
            {
                List<Entity> entityPitList = PromptUtils.promptEntities("Выберите объекты для добавления в таблицу");
                if (entityPitList == null || entityPitList.Count <= 0) { return; }
                int countEntity = entityPitList.Count;
                AppUnlocked = false;
                foreach (Entity entity in entityPitList)
                {
                    int selectIndex = entityPitList.IndexOf(entity);

                    try
                    {
                        if (entity != null)
                        {
                            //Если объект уже в таблице, то не будем его обрабатывать
                            if (DendrologyServiceList.Where(p => p != null && p.Model != null).Any(pitService => pitService.Model.Id.Equals(entity.Id))) { continue; }
                            // Очищаем прошлую XData 
                            XDataUtils.CleanXData(entity.Id);
                            AddEntityToTable(entity);
                        }
                    }
                    catch (System.Exception ex) { AppData.WtiteMassageToAutocad(ex); }

                    AppData.WriteInfoBarInAutocad($"{selectIndex + 1}/{countEntity}");
                }
            }
            catch (System.Exception ex) { AppData.WtiteMassageToAutocad(ex); }
            finally 
            { 
                AppData.WriteInfoBarInAutocad(); 
                AppUnlocked = true; 
            }
        }

        // Событие CellEditEnding возникает когда пользователь завершил редактировать таблицу DataGrid
        public void DataGridSelectedCellsChanged(object newSelectedRowData)
        {
            try
            {
                // Если выбранная строка данных не соответсвтует выбранной строке пользователем в таблице, то изменяем выбранную строку данных
                var selectRow = SelectedDataTableRow;
                if (selectRow != newSelectedRowData)
                {
                    SelectedDataTableRow = (DendrologyService)newSelectedRowData;
                }

                // Сдвигаем индексы и сортируем
                SortRowsDataGrid((DendrologyService)newSelectedRowData);
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        // Сдвигаем индексы
        // Обрабатываем событие изменения номера строки, если порядковый номер строки был изменен, то сортируем так, чтобы элемент встал на своё место
        // ПРИМЕР: изменение порядка нумерации должно работать так – вписал в желаемую строчку цифру «2» и эта строчка встала между строчками 1 и 3, все остальные строчки перенумеровались со сдвигом вниз
        // Сортируем по порядковым номера значения в таблице
        public void SortRowsDataGrid(DendrologyService changedDendrologyService)
        {
            try
            {
                if (changedDendrologyService == null) { return; }

                // Сортируем в порядке возрастания
                OrderByDendrologyServiceList();

                // Выходим, если значение не было изменено
                if (!changedDendrologyService.isChangedDendrologyServiceNumber) { return; }

                // Получаем объекты DataGrid
                var tableRows = DendrologyServiceList;

                // Получаем старый (запомненный) индекс, новый индекс и модель, измененной строки 
                var oldDendrologyIndexWithLetter = getIndexWithLetter(changedDendrologyService.oldDendrologyServiceNumber);
                var DendrologyIndexWithLetter = getIndexWithLetter(changedDendrologyService.DendrologyServiceNumber);
                string oldDendrologyServiceNumber = oldDendrologyIndexWithLetter.Item1.ToString();
                string DendrologyServiceNumber = DendrologyIndexWithLetter.Item1.ToString();
                string oldDendrologyServiceLetter = oldDendrologyIndexWithLetter.Item2;
                string DendrologyServiceLetter = DendrologyIndexWithLetter.Item2;
                int oldIndex = -1; int newIndex = -1;
                if (!string.IsNullOrEmpty(oldDendrologyServiceNumber)) { oldIndex = Convert.ToInt32(oldDendrologyServiceNumber); }
                if (!string.IsNullOrEmpty(DendrologyServiceNumber)) { newIndex = Convert.ToInt32(DendrologyServiceNumber); }
                var changed_oEntityId = changedDendrologyService.Model.Id;

                // Выходим, если не требует сортировки 
                // Если в таблице одно значение, то выходим
                if (tableRows.Count <= 1 || oldIndex == -1 || newIndex == -1 || oldIndex == newIndex) { OrderByDendrologyServiceList(); return; }

                // Определяем направление изменения индекса
                // Если новый индекс больше старого, то отнимаем, если новый индекс меньше, то прибавляем
                int direction_index = 0;
                if (string.IsNullOrEmpty(DendrologyServiceLetter) && string.IsNullOrEmpty(oldDendrologyServiceLetter))
                {
                    if (newIndex < oldIndex) { direction_index = 1; }
                    else if (newIndex > oldIndex) { direction_index = -1; }
                }
                else if (!string.IsNullOrEmpty(DendrologyServiceLetter) || !string.IsNullOrEmpty(oldDendrologyServiceLetter))
                {
                    if (newIndex < oldIndex) { direction_index = -1; }
                    else if (newIndex > oldIndex) { direction_index = 1; }
                }

                // Изменяем порядковые номера остальных моделей (смещаем их)
                foreach (var oDendrologyService in tableRows)
                {
                    var selectOldDendrologyIndexWithLetter = getIndexWithLetter(oDendrologyService.oldDendrologyServiceNumber);
                    int selectOldIndex = selectOldDendrologyIndexWithLetter.Item1;
                    string selectOldLetter = selectOldDendrologyIndexWithLetter.Item2;

                    var selectDendrologyIndexWithLetter = getIndexWithLetter(oDendrologyService.dendrologyServiceNumber);
                    int selectIndex = selectDendrologyIndexWithLetter.Item1;
                    string selectLetter = selectDendrologyIndexWithLetter.Item2;

                    var select_oEntityId = oDendrologyService.Model.Id;
                    // Если измененный пользователь id не равен текущей модели И порядковый номер >= заменяемому
                    // И текущая строка <= заменяемому, то добавляем +1 к порядковому номеру объекту (смещаем) 
                    //if (string.IsNullOrEmpty(DendrologyServiceLetter))
                    //{
                        if (direction_index > 0)
                        {
                            if (string.IsNullOrEmpty(DendrologyServiceLetter) && string.IsNullOrEmpty(oldDendrologyServiceLetter))
                            {
                                if (changed_oEntityId != select_oEntityId && selectIndex >= newIndex && selectIndex <= oldIndex)
                                {
                                    string newDendrologyServiceNumber = (selectIndex + direction_index).ToString() + selectLetter;
                                    oDendrologyService.DendrologyServiceNumber = newDendrologyServiceNumber;
                                }
                            }

                            if (!string.IsNullOrEmpty(DendrologyServiceLetter) || !string.IsNullOrEmpty(oldDendrologyServiceLetter))
                            {
                                if (changed_oEntityId != select_oEntityId && selectIndex >= newIndex)
                                {
                                    string newDendrologyServiceNumber = (selectIndex + direction_index).ToString() + selectLetter;
                                    oDendrologyService.DendrologyServiceNumber = newDendrologyServiceNumber;
                                }
                            }
                        }
                        else if (direction_index < 0)
                        {
                            if (string.IsNullOrEmpty(DendrologyServiceLetter) && string.IsNullOrEmpty(oldDendrologyServiceLetter))
                            {
                                if (changed_oEntityId != select_oEntityId && selectIndex >= oldIndex && selectIndex <= newIndex)
                                {
                                    string newDendrologyServiceNumber = (selectIndex + direction_index).ToString() + selectLetter;
                                    oDendrologyService.DendrologyServiceNumber = newDendrologyServiceNumber;
                                }
                            }

                            if (!string.IsNullOrEmpty(DendrologyServiceLetter) || !string.IsNullOrEmpty(oldDendrologyServiceLetter))
                            {
                                if (changed_oEntityId != select_oEntityId && selectIndex > oldIndex)
                                {
                                    string newDendrologyServiceNumber = (selectIndex + direction_index).ToString() + selectLetter;
                                    oDendrologyService.DendrologyServiceNumber = newDendrologyServiceNumber;
                                }
                            }
                        }
                    //}
                    //else if (!string.IsNullOrEmpty(DendrologyServiceLetter))
                    //{
                    //    if (direction_index > 0)
                    //    {
                    //        if (changed_oEntityId != select_oEntityId && selectIndex >= oldIndex && selectIndex <= newIndex)
                    //        {
                    //            string newDendrologyServiceNumber = (selectIndex - direction_index).ToString();
                    //            oDendrologyService.DendrologyServiceNumber = newDendrologyServiceNumber;
                    //        }
                    //    }
                    //    else if (direction_index < 0)
                    //    {
                    //        if (changed_oEntityId != select_oEntityId && selectIndex >= newIndex && selectIndex <= oldIndex)
                    //        {
                    //            string newDendrologyServiceNumber = (selectIndex - direction_index).ToString();
                    //            oDendrologyService.DendrologyServiceNumber = newDendrologyServiceNumber;
                    //        }
                    //    }
                    //}
                }

                // Сортируем в порядке возрастания
                OrderByDendrologyServiceList();
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        /// <summary>
        /// Активируется, когда пользователь в AutoCAD выбирает котлован или когда программа устанавливает выделение на котлован<br/>
        /// Если мы это делаем программно, то выходим, иначе обрабатываем выделение пользователя 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static bool IsSelectedFromProgramm = false;
        private void OnSelectionAdded(object sender, SelectionAddedEventArgs e)
        {
            // Получаем список добавленных выбранных объектов из AutoCAD
            List<SelectedObject> selectedEntityList = new List<SelectedObject>();
            foreach (SelectedObject obj in e.AddedObjects)
            {
                try
                {
                    // Проверяем, чтобы быть уверенными, что SelectedObject был возвращён
                    if (obj == null || obj.ObjectId.IsBad()) { continue; }

                    var obj_in_List = selectedEntityList.Find(p => p.ObjectId.Equals(obj.ObjectId));
                    if (obj_in_List == null) { selectedEntityList.Add(obj); }
                }
                catch (System.Exception ex) { AppData.WtiteMassageToAutocad(ex); }
            }

            try
            {
                // Если не выбрано элементов или больше 1, то выходим (Для выбора только 1 элемента)
                if (selectedEntityList == null || selectedEntityList.Count != 1) { return; }

                // Последний выбранный элемент
                ObjectId oSelectedEntityId = selectedEntityList.Last().ObjectId;
                if (oSelectedEntityId.IsBad()) { return; }

                // Элемент в таблице приложения
                DendrologyService oDendrologyService = DendrologyServiceList.Where(p => p != null && p.Model != null && p.Model.IsReadEnabled == false).FirstOrDefault(p => p.Model.Id.Equals(oSelectedEntityId));
                if (oDendrologyService == null) { return; }

                // Если текущий выбранный элемент НЕ равен новому выбранному, то новый присваиваем в текущий элемент (Подсветится выбор в таблице приложения, а не в AutoCAD)
                // oDendrologyService - найденное значение, которое выбранно в AutoCAD, SelectedDataTableRow - текущий выбранный в таблице приложения элемент
                if (SelectedDataTableRow != null && SelectedDataTableRow.Model.Id == oDendrologyService.Model.Id) { return; }

                // Присваиваем значеине
                SelectedDataTableRow = oDendrologyService;
            }
            catch (System.Exception ex) { AppData.WtiteMassageToAutocad(ex); }
        }

        public static Tuple<int, string> getIndexWithLetter(string inIndex)
        {
            Tuple<int, string> tupleIndex = new Tuple<int, string>(-1, string.Empty);

            try
            {
                // Измененный индекс
                string selectIndexString = new String(inIndex.TakeWhile(Char.IsDigit).ToArray());
                int selectIndex = -1;

                // Находим букву после числа
                string afterFirstDigit = string.Empty;
                string findLetter = string.Empty;
                if (!string.IsNullOrEmpty(selectIndexString))
                {
                    afterFirstDigit = inIndex.Replace(selectIndexString, "");
                    findLetter = string.IsNullOrEmpty(afterFirstDigit) == false ? afterFirstDigit.ToCharArray().GetValue(0).ToString() : string.Empty;

                    selectIndex = Convert.ToInt32(selectIndexString);
                }

                tupleIndex = new Tuple<int, string>(selectIndex, findLetter);
                return tupleIndex;
            }
            catch { }

            return tupleIndex;
        }

        public void OnRemoveRowCommand()
        {
            try
            {
                if (SelectedDataTableRow == null || SelectedDataTableRow.Model == null)
                    return;

                string info_Pit = "№" + SelectedDataTableRow.DendrologyServiceNumber + " | " + SelectedDataTableRow.SelectedNameOfBreed;
                var dialogResult = MessageBox.Show("Вы действительно хотите удалить объкт " + info_Pit + "?", "Подтверждение для удаления", MessageBoxButton.YesNo);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    RemoveEntityFromDatabase(SelectedDataTableRow, notifyForDelete: false);
                }
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        private void RemoveEntityFromDatabase(DendrologyService oDendrologyService, bool notifyForDelete = true)
        {
            try
            {
                if (oDendrologyService == null || oDendrologyService.Model == null)
                    return;

                // ---- Удалдяем выноску, если есть у объекта
                // deleteMLeader(oDendrologyService);

                // ---- Удлаяем объект из автокада и списка datagrid
                AppData.Database.RemoveCustomProperty(oDendrologyService.Model.Handle.Value.ToString());
                EntityUtils.Erase(oDendrologyService.Model, notifyForDelete);
                DendrologyServiceList.Remove(oDendrologyService);
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        private void deleteMLeader(DendrologyService oDendrologyService)
        {
            try
            {
                if (oDendrologyService == null || oDendrologyService.Model == null)
                    return;

                //Извлекаем КОД выноски из xdata
                var trenchMLeaderHandle = XDataUtils.GetStringXDataFromTheObjectByTypeCode(oDendrologyService.Model.Id, "", (int)DxfCode.ExtendedDataAsciiString);
                if (trenchMLeaderHandle != "0" && trenchMLeaderHandle != "" && trenchMLeaderHandle != null)
                {
                    using (Transaction ts = AppData.ActiveDocument.TransactionManager.StartOpenCloseTransaction())
                    {
                        // Получаем объект выноски
                        MLeader MLeader_ = AppData.Database.GetEntityByHandle(trenchMLeaderHandle, ts) as MLeader;
                        if (MLeader_ is null)
                            return;
                        MLeader_.Erase(true);

                        // Подтверждаем изменение
                        ts.Commit();
                    }
                }
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        /// <summary>
        /// Создание
        /// </summary>
        public void OnСreateLeaderOnEntity()
        {
            try
            {
                // Выбираем объекты на чертеже
                var selectedEntities = PromptUtils.promptEntities("Выберите объекты на которых необходимо выставить выноску");
                if (selectedEntities == null || selectedEntities.Count <= 0) { return; }

                AppUnlocked = false;
                int countEntity = selectedEntities.Count;
                foreach (Entity selectedEntity in selectedEntities)
                {
                    int selectIndex = selectedEntities.IndexOf(selectedEntity);

                    try
                    {
                        DendrologyService oDendrologyService = DendrologyServiceList.FirstOrDefault(p => p.Model.Handle.Equals(selectedEntity.Handle));
                        if (oDendrologyService == null) { continue; }

                        // Формируем текст для выноски
                        string index_ = oDendrologyService.DendrologyServiceNumber.ToString();
                        string TextMLeader = $"{index_}\n";

                        // Если BlockReference котлован
                        Point3d startPointMLeader = new Point3d(0, 0, 0);
                        BlockReference asBlockReference = oDendrologyService.Model as BlockReference;
                        if (asBlockReference != null) { startPointMLeader = asBlockReference.Position; }
                        if (startPointMLeader == new Point3d(0, 0, 0)) { continue; }

                        MLeader createdMLeaderBlock = GenerateMLeaderService.CreateMLeaderBlock(TextMLeader, startPointMLeader);
                        string HandleValueOldMLeader = oDendrologyService.MLeaderHandle;
                        if (createdMLeaderBlock != null) { oDendrologyService.MLeaderHandle = createdMLeaderBlock.HandleToString(); }
                        if (!string.IsNullOrEmpty(HandleValueOldMLeader)) { EntityUtils.Erase(HandleValueOldMLeader, notifyForDelete: false); }
                    }
                    catch (System.Exception ex) { AppData.WtiteMassageToAutocad(ex); }

                    AppData.WriteInfoBarInAutocad($"{selectIndex + 1}/{countEntity}");
                }

                AppData.SetFocusToDwgView();
            }
            catch (System.Exception ex) { AppData.WtiteMassageToAutocad(ex); }
            finally 
            { 
                AppData.WriteInfoBarInAutocad(); 
                AppUnlocked = true; 
            }

        }

        /// <summary>
        /// Фокусируемся на объкте, который выбрали в таблице приложения
        /// </summary>
        public void OnZoomAndSelectEntityCommand()
        {
            try
            {
                if (SelectedDataTableRow == null) { return; }
                EditorUtils.ZoomAndSelect(SelectedDataTableRow.Model);
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        /// <summary>
        /// Добавляем блок в документ
        /// </summary>
        public void OnAddEntityCommand()
        {
            try
            {
                AppData.SetFocusToDwgView();
                BlockReference pointBlockRef = GenerateDendrologyService.CreateBlockReferencePoint();
                AddEntityToTable(pointBlockRef);
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");                                                  
            }
        }

        /// <summary>
        /// Загружаем список DendrologyService из сохранённых handle в таблице свойств документа
        /// </summary>
        public void LoadDendrologyServiceList()
        {
            try
            {
                DatabaseService entityService = new DatabaseService();
                List<Entity> foundedEntities = entityService.GetEntitiesFromCustomTable();

                if (foundedEntities != null && foundedEntities.Count > 0)
                {
                    //Добавляем объект в таблицу приложения
                    AddDendrologyServiceToTable(foundedEntities);

                    // Добавляем к объекту события обновления данных 
                    DendrologyServiceAssignHandleOnModify(foundedEntities);

                    // Сортируем в порядке возрастания
                    OrderByDendrologyServiceList();
                }
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        public void DendrologyServiceAssignHandleOnModify(List<Entity> foundedEntities)
        {
            try
            {
                using (Transaction ts = AppData.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (Entity oEntity in foundedEntities)
                    {
                        try
                        {
                            if (oEntity.IsBad())
                                continue;

                            Entity oEntityItem = oEntity;
                            if (!oEntity.IsWriteEnabled)
                                oEntityItem = ts.GetObject(oEntity.Id, OpenMode.ForRead, false, true) as Entity;

                            // Сначала ловим OEntity_OpenedForModify -> OEntity_Modified -> OEntity_ObjectClosed
                            oEntity.OpenedForModify += OEntity_OpenedForModify;
                            oEntity.Modified += OEntity_Modified;
                            oEntity.ObjectClosed += OEntity_ObjectClosed;
                        }
                        catch (System.Exception ex)
                        {
                             AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
                        }
                    }
                    ts.Commit();

                }
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        // Присваиваем номер индекса, если не существует в таблице
        private void AssignNumberIfNotExists(DendrologyService oDendrologyService)
        {
            try
            {
                // Если сущность восстановлена (была удалена), то записываем старый ID
                if (oDendrologyService.Model.IsEraseStatusToggled)
                {
                    // Получаем PitOrTrenchNumber из XData модели объекта
                    oDendrologyService.DendrologyServiceNumber = oDendrologyService.DendrologyServiceNumber;
                }

                // Если элемент индекса пустой, то начинаем с максимального значения + 1
                if (string.IsNullOrEmpty(oDendrologyService.DendrologyServiceNumber))
                {
                    int pitTrenchNextNumber = AppData.GetNextAvailableNumber(DendrologyServiceList.ToList());
                    oDendrologyService.DendrologyServiceNumber = (pitTrenchNextNumber).ToString();
                }
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        public void AddDendrologyServiceToTable(List<Entity> foundedEntities)
        {
            try
            {
                AppData.SetFocusToDwgView();
                foreach (Entity oEntity in foundedEntities)
                {
                    try
                    {
                        if (oEntity.IsBad())
                            return;

                        //Если объект уже в таблице, то не будем их снова добавлять
                        if (DendrologyServiceList.Any(pitService => oEntity.Id.Equals(pitService.Model.Id)))
                            return;

                        var oDendrologyService = GenerateDendrologyService.CreatePitOrTrench(oEntity);
                        if (oDendrologyService == null) { continue; }

                        // Присваиваем индекс для отображения в таблице, если НЕ существует 
                        AssignNumberIfNotExists(oDendrologyService);

                        oDendrologyService.GetEntityType();
                        oDendrologyService.GetEntityConclusion();

                        DendrologyServiceList.Add(oDendrologyService);
                    }
                    catch (System.Exception ex)
                    {
                        AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
                    }
                }

            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        #region ##### Events DWG OEntity_OpenedForModify -> OEntity_Modified -> OEntity_ObjectClosed
        // Сначала ловим OEntity_OpenedForModify -> OEntity_Modified -> OEntity_ObjectClosed
        private void OEntity_OpenedForModify(object sender, EventArgs e)
        {
            try
            {
                Entity oEntity = sender as Entity;
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }
        private void OEntity_Modified(object sender, EventArgs e)
        {
            try
            {
                Entity oEntity = sender as Entity;
                if (oEntity == null || oEntity.Id.IsNull) { return; }

                if (oEntity.IsModified)
                {
                    DendrologyService oDendrologyService = DendrologyServiceList.FirstOrDefault(p => p.Model.Id.Equals(oEntity.Id));
                    if (oDendrologyService == null) { return; }

                    oDendrologyService.GetEntityType();
                    oDendrologyService.GetEntityConclusion();
                }
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }
        private void OEntity_ObjectClosed(object sender, ObjectClosedEventArgs e)
        {
            try
            {
                if (e.Id.IsBad())
                    return;

            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }
        #endregion

        /// <summary>
        /// Сортируем список в порядке возрастания
        /// </summary>
        public void OrderByDendrologyServiceList()
        {
            try
            {
                // Убираем ненужные значения
                var deleteNULLValues = DendrologyServiceList.OfType<DendrologyService>();
                // Сортируем по индексам
                // Находим в строке первые числа (123afb) -> (123) и переводим в Int32 формат
                // var afterSortNumbers = deleteNULLValues.OrderBy(s => Convert.ToInt32(new String(s.DendrologyServiceNumber.TakeWhile(Char.IsDigit).ToArray())));
                var toList = deleteNULLValues.ToList();
                toList.Sort(new MyComparerDendrologyService());

                DendrologyServiceList = new ObservableCollection<DendrologyService>(toList);
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
            }
        }

        /// <summary>
        /// Добавляем объект в таблицу
        /// </summary>
        /// <param name="oEntity"></param>
        public void AddEntityToTable(Entity oEntity)
        {
            try 
            {
                if (oEntity == null) { return; }
                if (DendrologyServiceList.Any(p => p.Model.Id.Equals(oEntity.Id))) { return; }
                var oDendrologyService = new DendrologyService { Model = oEntity };
                if (oDendrologyService == null) { return; }

                AssignNumberIfNotExists(oDendrologyService);
                oDendrologyService.SelectedConclusion = oDendrologyService.GetEntityConclusion();

                DendrologyServiceList.Add(oDendrologyService);
                OnPropertyChanged("DendrologyServiceList");
                AppData.AddEntityHandleToDwgDatabase(oEntity);
                DendrologyServiceAssignHandleOnModify(new List<Entity>() { oEntity });
            }
            catch (System.Exception ex)
            {
                 AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");                                                
            }
        }

        public int GetIndexList()
        {
            if (DendrologyServiceList == null || DendrologyServiceList.Count <= 0)
                return 1;

            return DendrologyServiceList.Count + 1;
        }

        public LayerTableRecord getLayerFromName(string NameLayer)
        {
            LayerTableRecord layerTableRecord = AllLayers.FirstOrDefault(l => l.Name.Equals(NameLayer));
            if (layerTableRecord == null) { layerTableRecord = AllLayers.FirstOrDefault(l => l.Name.Equals("0")); }
            return layerTableRecord;
        }

        public void OnUpdateLayersCommand()
        {
            var layer = AllLayers;
            OnPropertyChanged(nameof(AllLayers));
        }

        public static List<LayerTableRecord> AllLayers
        {
            get
            {
                try { return LayerUtils.GetAllThelayers(); }
                catch { return new List<LayerTableRecord>(); }
            }
        }

        private LayerTableRecord selectedLayerForNum_1;
        public LayerTableRecord SelectedLayerForNum_1
        {
            get { return selectedLayerForNum_1; }
            set { selectedLayerForNum_1 = value; }
        }

        private LayerTableRecord selectedLayerForNum_2;
        public LayerTableRecord SelectedLayerForNum_2
        {
            get { return selectedLayerForNum_2; }
            set { selectedLayerForNum_2 = value; }
        }

        private LayerTableRecord selectedLayerForNum_3;
        public LayerTableRecord SelectedLayerForNum_3
        {
            get { return selectedLayerForNum_3; }
            set { selectedLayerForNum_3 = value; }
        }

        private LayerTableRecord selectedLayerForNum_4;
        public LayerTableRecord SelectedLayerForNum_4
        {
            get { return selectedLayerForNum_4; }
            set { selectedLayerForNum_4 = value; }
        }

        private LayerTableRecord selectedLayerForNum_5;
        public LayerTableRecord SelectedLayerForNum_5
        {
            get { return selectedLayerForNum_5; }
            set { selectedLayerForNum_5 = value; }
        }

        private LayerTableRecord selectedLayerForNum_6;
        public LayerTableRecord SelectedLayerForNum_6
        {
            get { return selectedLayerForNum_6; }
            set { selectedLayerForNum_6 = value; }
        }

        private LayerTableRecord selectedLayerForNum_7;
        public LayerTableRecord SelectedLayerForNum_7
        {
            get { return selectedLayerForNum_7; }
            set { selectedLayerForNum_7 = value; }
        }

        private LayerTableRecord selectedLayerForNum_8;
        public LayerTableRecord SelectedLayerForNum_8
        {
            get { return selectedLayerForNum_8; }
            set { selectedLayerForNum_8 = value; }
        }


        private ObservableCollection<DendrologyService> dendrologyServiceList = new ObservableCollection<DendrologyService>();
        public ObservableCollection<DendrologyService> DendrologyServiceList
        {
            get
            {
                return dendrologyServiceList;
            }
            set
            {
                dendrologyServiceList = value;
                OnPropertyChanged();
            }
        }

        public DendrologyService selectedDataTableRow;
        public DendrologyService SelectedDataTableRow
        {
            get
            {
                return selectedDataTableRow;
            }
            set
            {
                try
                {
                    if (value == null) { return; }

                    selectedDataTableRow = value;

                    // Пока что работает с 1 выделенным объектов, в будущем планируется для использования списка выбранных элементов в таблице
                    List<DendrologyService> selectedDataTableRowList = new List<DendrologyService>();
                    selectedDataTableRowList.Add(selectedDataTableRow);

                    List<ObjectId> objectIds = new List<ObjectId>();
                    foreach (DendrologyService oDendrologyService in selectedDataTableRowList)
                    {
                        if (oDendrologyService != null && oDendrologyService.Model != null && !oDendrologyService.Model.Id.IsNull)
                        {
                            objectIds.Add(oDendrologyService.Model.Id);
                        }
                    }

                    EditorUtils.SetImpliedSelection_Objects(objectIds.ToArray());
                }
                catch (System.Exception ex)
                {
                     AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");
                }
                finally
                {
                    AppData.SetFocusToDwgView();
                    OnPropertyChanged();
                }
            }
        }

        private bool appUnclocked = true;
        /// <summary>
        /// Флаг показывающий разблокиравно ли приложение<br/>
        /// is false - заблокировано, пользователь НЕ может взаимодействовать с приложением<br/>
        /// is true - разблокировано, пользователь может взаимодействовать с приложением
        /// </summary>
        public bool AppUnlocked
        {
            get { return appUnclocked; }
            set
            {
                if (appUnclocked == value) { return; }
                appUnclocked = value;
                OnPropertyChanged();
            }
        }

    }

    public class MyComparerDendrologyService : IComparer<DendrologyService>
    {
        private readonly Regex regex = new Regex(@"(\d+)([a-zA-Z0-9А-Яа-я]?)()");
        public int Compare(DendrologyService oDendrologyService1, DendrologyService oDendrologyService2)
        {
            string selectNumber1 = oDendrologyService1.dendrologyServiceNumber;
            string selectNumber2 = oDendrologyService2.dendrologyServiceNumber;
            Match m1 = regex.Match(selectNumber1);
            Match m2 = regex.Match(selectNumber2);
            string num1 = m1.Groups[1].Value;
            string num2 = m2.Groups[1].Value;
            if (num1.Length < num2.Length)
                return -1;
            if (num1.Length > num2.Length)
                return 1;
            int cmp = string.Compare(num1, num2);
            if (cmp != 0)
                return cmp;
            return string.Compare(m1.Groups[2].Value, m2.Groups[2].Value);
        }
    }
}
