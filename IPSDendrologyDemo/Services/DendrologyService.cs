using Autodesk.AutoCAD.DatabaseServices;
using IPSDendrologyDemo.Other;
using IPSDendrologyDemo.UIService;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace IPSDendrologyDemo.Services
{
    public class DendrologyService : ViewModelBase
    {
        // Модель объекта из документа 
        public Entity Model { get; set; }

        public DendrologyService() { }

        // Список для пользователя
        // "Тип"
        public ObservableCollection<string> TypesList
        {
            get
            {
                return new ObservableCollection<string> { "Дерево", "Куст", "Пень" };
            }
        }

        // Выбранное значение пользователем из списка 
        // "Тип"
        private string selectedType = "Дерево";
        public string SelectedType
        {
            get
            {
                return selectedType;
            }
            set
            {
                selectedType = value;
                SetSelectedConclusionInModel();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Индекс элемента "№ п/п"
        /// </summary>
        public bool isChangedDendrologyServiceNumber = false;
        public string oldDendrologyServiceNumber;
        public string dendrologyServiceNumber;
        public string DendrologyServiceNumber
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(DendrologyServiceNumber), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { dendrologyServiceNumber = getXDataValue; }

                return dendrologyServiceNumber;
            }
            set
            {
                try
                {
                    // Находим в строке первые числа (123afb) -> (123) и буквы после чисел
                    var tupleValue = FindLetterAndDigitInStr(value);
                    string firstDigit = tupleValue.Item1;
                    string findLetter = tupleValue.Item2;

                    // Если значение <= 0, то присваиваем 1, потому что списко начинается с 1 (По техническому заданию)
                    if (Convert.ToInt32(firstDigit) <= 0) firstDigit = "1";

                    // Если не целое число, то выходим и оставляем всё как есть
                    if (!int.TryParse(firstDigit, out int result)) { isChangedDendrologyServiceNumber = false; return; }                                              // Выставляем флаг, что значение НЕ было изменено! И выходим из свойства

                    oldDendrologyServiceNumber = dendrologyServiceNumber;                                                                                             // Запоминаем старое значение 
                    dendrologyServiceNumber = result + findLetter;                                                                                                    // Присваиваем индекс
                    isChangedDendrologyServiceNumber = true;                                                                                                          // Выставляем флаг, что значение было изменено точно!

                    XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(DendrologyServiceNumber), (int)DxfCode.ExtendedDataAsciiString, result + findLetter);       // Добавление в БД документа

                    ChangeTextOnMLeader();                                                                                                                            // Меняем Выноску, если она существует для этого объекта
                    OnPropertyChanged();                                                                                                                              // Принимаем изменения
                }
                catch { }
            }
        }

        public static Tuple<string, string> FindLetterAndDigitInStr(string inValue)
        {
            // Находим в строке первые числа (123afb) -> (123)
            string firstDigit = new String(inValue.TakeWhile(Char.IsDigit).ToArray());
            // Находим букву после числа
            string afterFirstDigit = inValue.Replace(firstDigit, "");
            string findLetter = string.IsNullOrEmpty(afterFirstDigit) == false ? afterFirstDigit.ToCharArray().GetValue(0).ToString() : string.Empty;
            return new Tuple<string, string>(firstDigit, findLetter);
        }

        // Список для пользователя
        // "Наименование пород"
        public ObservableCollection<string> NameOfBreedsList
        {
            get
            {
                return new ObservableCollection<string> {
                    "Акация белая", "акация желтая куст", "барбарис куст", "Бархат амурский", "Береза", "бересклет куст", "бирючина куст", "Боярышник", "боярышник куст", "бузина куст", "Вишня", "вишня куст", "Вяз", "Газон", "Груша", "дек.- лиственный кустарник", "дерен куст", "Дуб", "Ель", "жимолость куст", "Ива",
                    "ива(поросль)", "Ива белая", "ирга куст", "калина куст", "Каштан", "кизильник куст", "Клен", "клен куст", "Клен ясенелистный", "клен ясенелистный(поросль)", "крушина куст", "кустарник разный", "лещина куст", "Лжетсуга", "лиана куст", "Липа", "Лиственница", "можжевельник куст", "Ольха", "Орех", "Осина", "Остолоп", "Пень", "Пихта",
                    "пл.- ягодный кустарник", "Плодовое", "поросль", "пузыреплодник куст", "ракитник куст", "роза куст", "Рябина", "Саженцы", "Самосев до 8 см.", "сирень куст", "Слива", "слива куст", "смородина куст", "снежноягодник куст", "Сосна", "спирея куст", "Сухостой", "Тополь", "тополь(поросль)", "Тополь белый", "Тополь пирамидальный", 
                    "Травяной покров", "Туя", "туя куст", "хвойный кустарник", "Черемуха", "черемуха куст", "чубушник куст", "Экзот", "Яблоня", "Ясень", "* ****************** ко/ куст.", "* ******************ко / трава", "* ******************ко / дер."};
            }
        }

        // Выбранное значение пользователем из списка 
        // "Наименование пород"
        private string selectedNameOfBreed;
        public string SelectedNameOfBreed
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(SelectedNameOfBreed), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { selectedNameOfBreed = getXDataValue; }

                return selectedNameOfBreed;
            }
            set
            {
                selectedNameOfBreed = value;
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(SelectedNameOfBreed), (int)DxfCode.ExtendedDataAsciiString, value);  
                OnPropertyChanged();
            }
        }

        // "Деревьев (шт.)"
        private string countTrees;
        public string СountTrees
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(СountTrees), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { countTrees = getXDataValue; }

                return countTrees;
            }
            set
            {
                // Если не целое число, то выходим и оставляем всё как есть
                if (!int.TryParse(value, out int result)) { return; }                                                                       // Выходим из метода 
                countTrees = value;                                                                                                         // Присваиваем индекс
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(СountTrees), (int)DxfCode.ExtendedDataAsciiString, value);
                OnPropertyChanged();                                                                                                        // Принимаем изменения
            }
        }

        // "Кустарников (шт.)"
        private string countShrubs;
        public string CountShrubs
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(CountShrubs), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { countShrubs = getXDataValue; }

                return countShrubs;
            }
            set
            {
                // Если не целое число, то выходим и оставляем всё как есть
                if (!int.TryParse(value, out int result)) { return; }                                                                       // Выходим из метода 
                countShrubs = value;                                                                                                        // Присваиваем индекс
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(CountShrubs), (int)DxfCode.ExtendedDataAsciiString, value);
                OnPropertyChanged();                                                                                                        // Принимаем изменения
            }
        }

        // "Диаметр (см)"
        private string diameter;
        public string Diameter
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(Diameter), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { diameter = getXDataValue; }

                return diameter;
            }
            set
            {
                // Если не число, то выходим и оставляем всё как есть
                // if (!double.TryParse(value, out double result)) { return; }                                                                 // Выходим из метода 
                diameter = value;                                                                                                           // Присваиваем индекс
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(Diameter), (int)DxfCode.ExtendedDataAsciiString, value);
                OnPropertyChanged();                                                                                                        // Принимаем изменения
            }
        }

        // "Высота (м)"
        private string height;
        public string Height
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(Height), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { height = getXDataValue; }

                return height;
            }
            set
            {
                // Если не число, то выходим и оставляем всё как есть
                // if (!double.TryParse(value, out double result)) { return; }                                                                 // Выходим из метода 
                height = value;                                                                                                             // Присваиваем индекс
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(Height), (int)DxfCode.ExtendedDataAsciiString, value);
                OnPropertyChanged();                                                                                                        // Принимаем изменения
            }
        }

        // "Характеристика состояния зеленых насаждений"
        private string сonditionСharacteristics;
        public string ConditionСharacteristics
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(ConditionСharacteristics), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { сonditionСharacteristics = getXDataValue; }

                return сonditionСharacteristics;
            }
            set
            {
                сonditionСharacteristics = value;                                                                                           // Присваиваем индекс
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(ConditionСharacteristics), (int)DxfCode.ExtendedDataAsciiString, value);
                OnPropertyChanged();                                                                                                        // Принимаем изменения
            }
        }

        // Список для пользователя
        // "Заключение"
        public ObservableCollection<string> ConclusionList
        {
            get
            {
                return new ObservableCollection<string> {
                    "Пересадить", "Сохранить", "Вырубить"
                };
            }
        }

        // Выбранное значение пользователем из списка 
        // "Заключение"
        private string selectedConclusion;
        public string SelectedConclusion
        {
            get
            {
                return selectedConclusion;
            }
            set
            {
                selectedConclusion = value;

                SetSelectedConclusionInModel();
                OnPropertyChanged();
            }
        }

        // Список для пользователя
        // "Примечание"
        public ObservableCollection<string> NoteList
        {
            get
            {
                return new ObservableCollection<string> {
                    "охр. зона ком.", "аварийное", "сухостой", "неудовлетв.", "поросль", "самосев", "5-ти м зона", 
                    "учтено ранее*", "обрезка ветвей", "кронировать", "не входит в пятно", "удовлетв.", "усыхающее", "хорошее", "саженцы", 
                    "0,5х0,5х0,4", "0,8х0,8х0,6", "1,0х1,0х0,6", "1,3х1,3х0,6", "1,5х1,5х0,65", "1,7х1,7х0,65", "2,0х2,0х0,8", "2,4х2,4х0,95", "Ком земли",
                };
            }
        }

        // Выбранное значение пользователем из списка 
        // "Примечание"
        private string selectedNote;
        public string SelectedNote
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(SelectedNote), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { selectedNote = getXDataValue; }

                return selectedNote;
            }
            set
            {
                selectedConclusion = value;
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(SelectedNote), (int)DxfCode.ExtendedDataAsciiString, value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Видимость строки в таблице приложения
        /// </summary>
        private bool isVisibleInTable;
        public bool IsVisibleInTable
        {
            get
            {
                return isVisibleInTable;
            }
            set
            {
                isVisibleInTable = value;
                OnPropertyChanged();
            }
        }

        // Необходимо получать объект через Handle, потому что иначе вылетает AutoCAD (если через ObjectId) 
        /// <summary>
        /// Handle value выноски, которая привязана к блоку
        /// </summary>
        private string mLeaderHandle;
        public string MLeaderHandle
        {
            get
            {
                string getXDataValue = GetXDataModel(nameof(MLeaderHandle), (int)DxfCode.ExtendedDataAsciiString);
                if (!string.IsNullOrEmpty(getXDataValue)) { mLeaderHandle = getXDataValue; }

                return mLeaderHandle;
            }
            set
            {
                mLeaderHandle = value;
                XDataUtils.AddStringXDataToTheObject(Model.Id, nameof(MLeaderHandle), (int)DxfCode.ExtendedDataAsciiString, value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Назначаем на модель выбранное значение свойства
        /// </summary>
        public void SetSelectedConclusionInModel()
        {
            try
            {
                if (Model.IsBad()) { return; }
                if (Model as BlockReference == null) { return; }

                string _selectedConclusion = selectedConclusion;
                if (string.IsNullOrEmpty(_selectedConclusion)) { return; }

                string newSelectedConclusion = _selectedConclusion;
                if (SelectedType == "Куст") 
                {
                    if (_selectedConclusion.ToLower().Contains("пересадить")) { newSelectedConclusion = "Куст пересаживаемый"; }
                    if (_selectedConclusion.ToLower().Contains("сохранить")) { newSelectedConclusion = "Куст сохраняемый"; }
                    if (_selectedConclusion.ToLower().Contains("вырубить")) { newSelectedConclusion = "Куст вырубаемый"; }
                }

                if (SelectedType == "Пень")
                {
                    newSelectedConclusion = "Пень";
                }

                BlockUtils.SetDynamicPropertyValueOfABlock(Model as BlockReference, "Видимость", newSelectedConclusion);

                if (!Model.IsNotifying) { AppData.SetFocusToDwgView(); }
            }
            catch (Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// Получаем значение видимости для объекта
        /// </summary>
        /// <param name="oEntity"></param>
        /// <returns></returns>
        public string GetEntityConclusion()
        {
            string conclusionDefault = "Сохранить";
            try
            {
                if (Model.IsBad()) { return conclusionDefault; }
                if (Model as BlockReference == null) { return conclusionDefault; }

                string conclusionEntity = BlockUtils.GetDynamicPropertyOfABlock(Model as BlockReference, "Видимость", true);
                if (string.IsNullOrEmpty(conclusionEntity)) { return conclusionDefault; }

                if (conclusionEntity.ToLower().Contains("пересаж")) { SelectedConclusion = "Пересадить"; }
                else if (conclusionEntity.ToLower().Contains("сохр")) { SelectedConclusion = "Сохранить"; }
                else if (conclusionEntity.ToLower().Contains("выруб")) { SelectedConclusion = "Вырубить"; }

                return conclusionEntity;
            }
            catch (Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
            return conclusionDefault;
        }

        /// <summary>
        /// Получаем тип объекта 
        /// </summary>
        /// <param name="oEntity"></param>
        /// <returns></returns>
        public string GetEntityType()
        {
            string conclusionDefault = "Сохранить";
            try
            {
                if (Model.IsBad()) { return conclusionDefault; }
                if (Model as BlockReference == null) { return conclusionDefault; }

                string conclusionEntity = BlockUtils.GetDynamicPropertyOfABlock(Model as BlockReference, "Видимость", true);
                if (string.IsNullOrEmpty(conclusionEntity)) { return conclusionDefault; }

                if (conclusionEntity.ToLower().Contains("куст")) { SelectedType = "Куст"; IsVisibleInTable = true; }
                else if (conclusionEntity.ToLower().Contains("пень")) { SelectedType = "Пень"; IsVisibleInTable = false; }
                else { SelectedType = "Дерево"; IsVisibleInTable = true; }

                return conclusionEntity;
            }
            catch (Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
            return conclusionDefault;
        }

        public string GetXDataModel(string regAppName, short typeCode)
        {
            var selectXData = string.Empty;

            try
            {
                string pitOrTrenchNumber_XData_TR = XDataUtils.GetStringXDataFromTheObjectByTypeCode(Model.Id, regAppName, typeCode);
                if (pitOrTrenchNumber_XData_TR != string.Empty)
                {
                    selectXData = pitOrTrenchNumber_XData_TR;
                    return selectXData;
                }

                // Если из транзакции БД данных получить не получилось, пытаемся из существующих XData данной модели
                string pitOrTrenchNumber_XData = XDataUtils.GetStringXDataFromTheEntityByTypeCode(Model.Id, regAppName, typeCode, Model.XData);
                if (pitOrTrenchNumber_XData != string.Empty)
                {
                    selectXData = pitOrTrenchNumber_XData;
                }
            }
            catch (Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }

            return selectXData;
        }

        public void UpdateConclusion()
        {
            if (Model == null) { return; }

        }

        // Меняем Выноску для котлована, если она существует 
        public void ChangeTextOnMLeader()
        {
            try
            {
                //Извлекаем КОД выноски из xdata
                var mLeaderHandle = MLeaderHandle;
                if (mLeaderHandle != "0" && mLeaderHandle != "" && mLeaderHandle != null)
                {
                    Database db = AppData.Database;
                    using (var ts = db.TransactionManager.StartOpenCloseTransaction())
                    {
                        // Получаем объект выноски
                        MLeader MLeader_ = db.GetEntityByHandle(mLeaderHandle, ts) as MLeader;
                        if (MLeader_ is null) { return; }

                        // Формируем текст для выноски
                        string index_ = DendrologyServiceNumber.ToString();
                        string TextMLeader = $"{index_}\n";
                        if (string.IsNullOrEmpty(TextMLeader) || TextMLeader == "\n") { return; }

                        // Обновляем текст на выноске блока
                        DirectionalLeaderJigBlock.updateValueMLeaderBlock(ts, db, MLeader_, TextMLeader);

                        // Подтверждаем изменение
                        ts.Commit();
                        AppData.SetFocusToDwgView();
                    }
                }
            }
            catch (Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

    }
}