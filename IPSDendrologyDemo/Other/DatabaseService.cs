using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;

namespace IPSDendrologyDemo.Other
{
    public class DatabaseService
    {
        public DatabaseService() { }

        // Получаем сущности (строки) из таблицы свойств документа
        public List<Entity> GetEntitiesFromCustomTable()
        {
            try
            {
                List<Entity> entityList = new List<Entity>();                                                       // Создаём новый экземпляр класса List из Entity
                Dictionary<string, string> dictWithProps = AppData.Database.GetCustomProperties();                  // Создаём словарь 
                foreach (KeyValuePair<string, string> dictItem in dictWithProps)
                {
                    Entity oEntity = AppData.Database.GetEntityByHandle(dictItem.Value);

                    if (oEntity.IsBad())
                        continue;

                    entityList.Add(oEntity);
                }

                return entityList;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return new List<Entity>();
            }
        }
    }
}
