using ACADAPP = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Windows;
using System.Collections.Generic;
using IPSDendrologyDemo.Services;
using System.Linq;
using System.IO;
using IPSDendrologyDemo.ViewModels;

namespace IPSDendrologyDemo.Other
{
    public static class AppData
    {
        public static Document ActiveDocument
        {
            get
            {
                return ACADAPP.DocumentManager.MdiActiveDocument;
            }
        }
        public static Database Database
        {
            get
            {
                return ActiveDocument.Database;
            }
        }

        public static Editor Editor
        {
            get
            {
                if (ActiveDocument != null)
                {
                    return ActiveDocument.Editor;
                }
                else
                {
                    return null;
                }
            }
        }

        public static DocumentCollection DocumentManager
        {
            get
            {
                return ACADAPP.DocumentManager;
            }
        }

        public static void AddEntityHandleToDwgDatabase(Entity oEntity)
        {
            try
            {
                if (oEntity == null || oEntity.Id.IsNull) { return; }
                if (IsEntityExistsInDwgDatabase(oEntity)) { return; }
                if (oEntity.IsBad()) { return; }

                AppData.Database.SetCustomProperty(oEntity.Handle.Value.ToString(), oEntity.Handle.Value.ToString());
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

        public static bool IsEntityExistsInDwgDatabase(Entity oEntity)
        {
            try
            {
                if (oEntity == null || oEntity.Id.IsNull) { return false; }
                if (oEntity.IsBad()) { return false; }

                var handleVal = oEntity.Handle.Value;
                string entityPropValue = AppData.Database.GetCustomProperty(handleVal.ToString());
                if (string.IsNullOrEmpty(entityPropValue)) { return false; }

                return true;
            }
            catch (Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
                return false;
            }
        }

        /// <summary>
        /// Получаем индекс для последнего элемента 
        /// </summary>
        /// <param name="pitTrenchServiceList"></param>
        /// <returns></returns>
        public static int GetNextAvailableNumber(List<DendrologyService> DendrologyServiceList)
        {
            // Если таблица пустая, то возвращаем индекс 1 (начинаем с 1)
            if (DendrologyServiceList == null || DendrologyServiceList.Count == 0) { return 1; }

            // Составляем список из уже существующих в таблице индексов И находим MAX
            List<int> pitTrechNumbers = DendrologyServiceList.Select(p => NumbersUtils.ParseStringToInt(p.DendrologyServiceNumber)).ToList();
            int maxPitNumber = pitTrechNumbers.Max();

            // Если вдруг индекс равен 0, то возвращаем индекс 1 (начинаем с 1)
            if (maxPitNumber == 0) { return 1; }

            // Возвращаем максимальный индекс + 1
            return maxPitNumber + 1;
        }

        /// <summary>
        /// Перевести фокус с приложения на документ AutoCAD (*.dwg)
        /// </summary>
        public static void SetFocusToDwgView()
        {
            try
            {
                // Set focus to AutoCAD
                Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// Выводим сообщение в AutoCAD Console
        /// </summary>
        /// <param name="msg"></param>
        public static void WtiteMassageToAutocad(string msg)
        {
            try
            {
                var ed = Editor;
                if (ed != null)
                {
                    ed.WriteMessage(msg);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Выводим сообщение об ошибке в AutoCAD Console
        /// </summary>
        /// <param name="msg"></param>
        public static void WtiteMassageToAutocad(System.Exception inEx)
        {
            try
            {
                var ed = Editor;
                if (ed != null) 
                {
                    ed.WriteMessage("IPSDendrology Error:\n" + inEx.Message + "|\n" + inEx.StackTrace + "|\n" + inEx.Data + "|\n");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Для отображения текстовой строки в окне AutoCAD
        /// </summary>
        public static void WriteInfoBarInAutocad(string msg = "")
        {
            try
            {
                Autodesk.AutoCAD.ApplicationServices.Application.SetSystemVariable("MODEMACRO", msg);
                System.Windows.Forms.Application.DoEvents();
            }
            catch (System.Exception ex) { AppData.WtiteMassageToAutocad(ex); }
        }

        /// <summary>
        /// Проверяем открыто ли окно приложения
        /// </summary>
        /// <param name="form_"> WPF Форма </param>
        /// <param name="ToActiveState"> Переводить окно на передний план, если оно уже открыто </param>
        /// <returns></returns>
        public static bool IsOpenedWindow(MainView form_, bool ToActiveState = true)
        {
            try
            {
                if (MainView.IsMainViewOpened)
                {
                    if (form_ != null)
                    {
                        form_.Activate();
                        if (form_.WindowState == System.Windows.WindowState.Minimized)
                        {
                            form_.WindowState = System.Windows.WindowState.Normal;
                        }
                    }

                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Сохраняем свойства приложения (размер окна, положение)
        /// </summary>
        /// <param name="form_"></param>
        public static void SaveAppPropertys(MainView form_)
        {
            try
            {
                if (form_.WindowState == WindowState.Maximized)
                {
                    // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                    Properties.Settings.Default.Top = form_.RestoreBounds.Top;
                    Properties.Settings.Default.Left = form_.RestoreBounds.Left;
                    Properties.Settings.Default.Height = form_.RestoreBounds.Height;
                    Properties.Settings.Default.Width = form_.RestoreBounds.Width;
                    Properties.Settings.Default.Maximized = true;
                }
                else
                {
                    Properties.Settings.Default.Top = form_.Top;
                    Properties.Settings.Default.Left = form_.Left;
                    Properties.Settings.Default.Height = form_.Height;
                    Properties.Settings.Default.Width = form_.Width;
                    Properties.Settings.Default.Maximized = false;
                }

                Properties.Settings.Default.Save();
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// Загружаем сохраненные ранее свойства приложения (размер окна, положение)
        /// </summary>
        /// <param name="form_"></param>
        public static void LoadAppPropertys(MainView form_)
        {
            try
            {
                // Min значения
                double min_height = form_.MinHeight;
                double min_width = form_.MinWidth;

                form_.Top = Properties.Settings.Default.Top;
                form_.Left = Properties.Settings.Default.Left;

                if (Properties.Settings.Default.Height > min_height) { form_.Height = Properties.Settings.Default.Height; }
                else { form_.Height = min_height; }

                if (Properties.Settings.Default.Width > min_width) { form_.Width = Properties.Settings.Default.Width; }
                else { form_.Width = min_width; }

                if (Properties.Settings.Default.Maximized)
                {
                    form_.WindowState = WindowState.Maximized;
                }
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// Загружаем слои из файла, чтобы использовать в новых документах, где их НЕ существует (nameFile = "name.dwg")
        /// </summary>
        public static void PreloadLayers()
        {
            try
            {
                string assemblyDir = FileUtils.GetAssemblyDirectory();
                string pitLayersPath = Path.Combine(assemblyDir, Blocks.fileName);
                LayerUtils.CopyLayersFromDWG(pitLayersPath);
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
            }
        }

        /// <summary>
        /// Загружаем блок котлована из файла, чтобы использовать в новых документах, где его НЕ существует (nameFile = "name.dwg")
        /// </summary>
        public static void PreloadBlocks()
        {
            try
            {
                string assemblyDir = FileUtils.GetAssemblyDirectory();
                string pitBlockPath = Path.Combine(assemblyDir, Blocks.fileName);

                if (!BlockUtils.IsBlockExist(Blocks.pointBlockReferenceName))
                {
                    BlockUtils.TryLoadBlockFromAnotherFile(pitBlockPath, Blocks.pointBlockReferenceName);
                }

                if (!BlockUtils.IsBlockExist(Blocks.mLeaderBlockReferenceName))
                {
                    BlockUtils.TryLoadBlockFromAnotherFile(pitBlockPath, Blocks.mLeaderBlockReferenceName);
                }
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");                                                  // Выводим сообщение об Ошибки в консоль AutoCAD
            }
        }


        /// <summary>
        /// Загружаем стили из файла, чтобы использовать в новых документах, где его НЕ существует
        /// </summary>
        public static void PreloadStyles()
        {
            try
            {
                string assemblyDir = FileUtils.GetAssemblyDirectory();
                string pitBlockPath = Path.Combine(assemblyDir, Blocks.fileName);
                LayerUtils.CopyStylesTextFromDWG(pitBlockPath);
                LayerUtils.CopyStylesMLeaderFromDWG(pitBlockPath);
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "| " + ex.StackTrace + "\n");                                                  // Выводим сообщение об Ошибки в консоль AutoCAD
            }
        }


    }
}
