using IPSDendrologyDemo.Other;
using IPSDendrologyDemo.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace IPSDendrologyDemo
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public static bool IsMainViewOpened = false;
        MainViewViewModel MVVM;
        public MainView()
        {
            try
            {
                InitializeComponent();                                                                                                       // Инициализируем компоненты формы
                AppData.WtiteMassageToAutocad("IPSDendrology загружает данные документа" + "\n");                                            // Выводим сообщение в консоль AutoCAD
                MVVM = new MainViewViewModel();                                                                                              // Инициализируем ModelView
                MVVM.OnRequestClose += (s, e) => this.Close();                                                                               // Привязываем событие для закрытия формы
                DataContext = MVVM;                                                                                                          // Привязываем ModelView к форме
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");                                                  // Выводим сообщение об Ошибки в консоль AutoCAD
            }
        }

        private void cb_InStackPanelControl_1_DropDownOpened(object sender, EventArgs e)
        {
            ComboBox selectComboBox = sender as ComboBox;
            MVVM.OnUpdateLayersCommand();
            if (selectComboBox == null) { return; }
            selectComboBox.ItemsSource = MainViewViewModel.AllLayers;
            selectComboBox.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
        }

        // При открытии MainWindows
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            AppData.LoadAppPropertys(this);                                                                                                  // Загружаем сохраненные ранее свойства приложения (размер окна, положение)
            IsMainViewOpened = true;                                                                                                         // Ставим флаг, что окно открылось
        }

        // При закрытии MainWindows
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppData.SaveAppPropertys(this);                                                                                                  // Сохраняем свойства приложения (размер окна, положение)
            IsMainViewOpened = false;                                                                                                        // Ставим флаг, что окно закрылось
        }

        // Сначала вызывается метод myDG_CellEditEnding, потом событие изменение значения столбца, потом только myDG_RowEditEnding 
        // Запоминаем номер колонки редактирования, после обновления значения в таблице, обновляем значения остальных элементов и сортируем
        int selectedCell = -1;
        private void MainDataGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            try
            {
                selectedCell = e.Column.DisplayIndex;                                                                                       // Запоминаем индекс столбца
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

        // Вызываем сортировку и изменение индексов в таблице (сначала происходит событие изменение значения столбца)
        private void MainDataGrid_RowEditEnding(object sender, System.Windows.Controls.DataGridRowEditEndingEventArgs e)
        {
            // Если выбранная колонка равна номеру индекса
            if (selectedCell == 1)
            {
                var selectedRow = e.Row.GetIndex();
                var selectedRowData = e.Row.DataContext;
                MainViewViewModel MVVM = DataContext as MainViewViewModel;
                if (MVVM != null)
                {
                    MVVM.DataGridSelectedCellsChanged(selectedRowData);
                }
            }

            // Сбрасываем значение
            selectedCell = -1;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            if (MainDataGrid == null)
                return;

            // Отнимаем от высоты страницы высоты элементов расположенных до таблицы, ОСТАТОК - максимальная высота таблицы 
            var newMaxHeightDG = this.ActualHeight - 45 - 5 - stackPanelActions.ActualHeight - 35;
            MainDataGrid.MaxHeight = newMaxHeightDG;
            MainDataGrid.Height = newMaxHeightDG;
        }

        private void MainDataGrid_PreparingCellForEdit(object sender, System.Windows.Controls.DataGridPreparingCellForEditEventArgs e)
        {
            if ((string)e.Column.Header == "Характеристика состояния зеленых насаждений")
            {
                var tb = e.EditingElement as TextBox;
                tb.TextWrapping = TextWrapping.Wrap;
            }
        }
    }
}
