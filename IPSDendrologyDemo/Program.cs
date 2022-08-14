using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using IPSDendrologyDemo.Other;

namespace IPSDendrologyDemo
{
    public class Program
    {
        public static MainView form_;

        // MainProgram в Файле .dll, который подгружается в AutoCAD и вызывается командой Dendrology
        [CommandMethod("IPS", "IPSDendrologyDemo", CommandFlags.Modal)]
        public static void MainProgram()
        {
            if (AppData.IsOpenedWindow(form_)) { return; }                                                                               // Проверяем было ли окно приложения уже открыто
            Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;                          // Получаем открытый документ в AutoCAD
            if (adoc == null) { return; }                                                                                                // Если активный документ не получен, выход
            form_ = new MainView();                                                                                                      // Инициализируем окно (в нём Инициализируем ModelView)
            adoc.LockDocument();                                                                                                         // Блокируем документ
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessWindow(form_);                                                  // Отображаем окно
            AppData.WtiteMassageToAutocad("IPSDendrology готов к работе" + "\n");                                                        // Выводим сообщение в консоль AutoCAD
        }
    }
}
