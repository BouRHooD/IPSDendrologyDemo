using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSDendrologyDemo.Other
{
    public static class DatabaseExts
    {
        public static void RemoveCustomProperty(this Database db, string key)
        {
            try
            {
                if (db.GetCustomProperty(key) == null)
                    return;

                using (var ts = db.TransactionManager.StartTransaction())
                {
                    DatabaseSummaryInfoBuilder infoBuilder = new DatabaseSummaryInfoBuilder(db.SummaryInfo);
                    if (infoBuilder == null)
                        return;
                    IDictionary custProps = infoBuilder.CustomPropertyTable;
                    if (custProps == null)
                        return;
                    if (!custProps.Contains(key))
                    {
                        ts.Commit();
                        return;
                    }
                    custProps.Remove(key);
                    db.SummaryInfo = infoBuilder.ToDatabaseSummaryInfo();
                    ts.Commit();
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Возвращает пользовательские свойства чертежа.
        /// Gets the drawing custom properties.
        /// </summary>
        /// <param name="db">Database instance this method applies to.</param>
        /// <returns>A strongly typed dictionary containing the entries.</returns>
        public static Dictionary<string, string> GetCustomProperties(this Database db)
        {
            try
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                IDictionaryEnumerator dictEnum = db.SummaryInfo.CustomProperties;                       // Получаем данные из БАЗЫ ДАННЫХ
                while (dictEnum.MoveNext())
                {
                    DictionaryEntry entry = dictEnum.Entry;
                    result.Add((string)entry.Key, (string)entry.Value);
                }
                return result;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Устанавливаем в БД AutoCAD значение пользовательского свойства <br/>
        /// Sets a property value
        /// </summary>
        /// <param name="db">Экземпляр БД к которой будет применяться метод</param>
        /// <param name="key">Ключ свойства</param>
        /// <param name="value">Значение свойства</param>
        public static void SetCustomProperty(this Database db, string key, string value)
        {
            try
            {
                using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
                {
                    DatabaseSummaryInfoBuilder infoBuilder = new DatabaseSummaryInfoBuilder(db.SummaryInfo);
                    IDictionary custProps = infoBuilder.CustomPropertyTable;
                    if (custProps.Contains(key)) { custProps[key] = value; }
                    else { custProps.Add(key, value); }
                    db.SummaryInfo = infoBuilder.ToDatabaseSummaryInfo();
                    ts.Commit();
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Получаем из БД AutoCAD значение пользовательского свойства
        /// </summary>
        /// <param name="db">Экземпляр БД к которой будет применяться метод</param>
        /// <param name="key">Ключ свойства</param>
        /// <returns>Значение свойства или string.Empty если НЕ найдено</returns>
        public static string GetCustomProperty(this Database db, string key)
        {
            try
            {
                DatabaseSummaryInfoBuilder sumInfo = new DatabaseSummaryInfoBuilder(db.SummaryInfo);
                IDictionary custProps = sumInfo.CustomPropertyTable;
                return custProps[key] as string;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public static Entity GetEntityByHandle(this Database db, string handleValue, Transaction ts)
        {
            try
            {
                if (string.IsNullOrEmpty(handleValue)) { return null; }
                long handleLongValue = NumbersUtils.ParseStringToInt(handleValue);
                Handle oHandle = new Handle(handleLongValue);
                ObjectId foundedObjectId = ObjectId.Null;
                if (!db.TryGetObjectId(oHandle, out foundedObjectId))
                    return null;

                if (foundedObjectId.IsBad())
                    return null;

                Entity oEntity = null;
                oEntity = ts.GetObject(foundedObjectId, OpenMode.ForWrite, false, true) as Entity;

                return oEntity;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static Entity GetEntityByHandle(this Database db, string handleValue)
        {
            try
            {
                if (string.IsNullOrEmpty(handleValue))
                {
                    return null;
                }
                long handleLongValue = NumbersUtils.ParseStringToInt(handleValue);
                Handle oHandle = new Handle(handleLongValue);
                ObjectId foundedObjectId = ObjectId.Null;
                if (!db.TryGetObjectId(oHandle, out foundedObjectId))
                {
                    return null;
                }

                if (foundedObjectId.IsBad())
                    return null;

                Entity oEntity = null;
                if (db != null && db.TransactionManager != null)
                {
                    Transaction ts = db.TransactionManager.StartOpenCloseTransaction();
                    if (ts != null)
                    {
                        using (ts)
                        {
                            oEntity = ts.GetObject(foundedObjectId, OpenMode.ForWrite, false, true) as Entity;
                            ts.Commit();                                                                                                // Подтверждаем завершение работы с БД
                        }
                    }
                }

                return oEntity;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return null;
            }
        }


    }
}
