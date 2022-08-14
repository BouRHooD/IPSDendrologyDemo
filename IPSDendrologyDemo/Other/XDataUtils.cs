using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSDendrologyDemo.Other
{
    public class XDataUtils
    {
        /// <summary>
        /// Add xdata to a objectId
        /// First you need to register your xdata with AddRegAppXDataToTheObject method
        /// </summary>
        /// <param name="oEntityId"></param>
        /// <param name="regAppName"></param>
        /// <param name="xDataTypeCode"></param>
        /// <param name="xDataValue"></param>
        public static void AddStringXDataToTheObject(ObjectId oEntityId, string regAppName, short xDataTypeCode, string xDataValue)
        {
            try
            {
                Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database db = adoc.Database;
                using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
                {
                    try
                    {
                        RegAppTable regTable = (RegAppTable)ts.GetObject(db.RegAppTableId, OpenMode.ForRead, true, true);
                        if (!regTable.Has(regAppName))
                        {
                            if (!regTable.IsWriteEnabled)
                            {
                                // Переключиться на режим Write
                                regTable.UpgradeOpen();
                            }

                            // Add the application names that would be used to add Xdata
                            RegAppTableRecord app = new RegAppTableRecord();
                            app.Name = regAppName;
                            regTable.Add(app);
                            ts.AddNewlyCreatedDBObject(app, true);
                        }
                        // Append the Xdata to the entity - two different applications added.
                        if (oEntityId == ObjectId.Null)
                        {
                            return;
                        }

                        DBObject obj = ts.GetObject(oEntityId, OpenMode.ForWrite, true, true);
                        obj.XData = new ResultBuffer(new TypedValue(1001, regAppName), new TypedValue(xDataTypeCode, xDataValue));
                    }
                    catch { }
                    finally { ts.Commit(); }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        public static string GetStringXDataFromTheObjectByTypeCode(ObjectId oEntityId, string regAppName, short typeCode, bool ifContains = false)
        {
            try
            {
                Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (adoc == null) { return string.Empty; }
                Database db = adoc.Database;
                string xDataResult = string.Empty;

                using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
                {
                    if (oEntityId.IsNull) { return string.Empty; }
                    DBObject obj = ts.GetObject(oEntityId, OpenMode.ForRead, true, true);
                    ResultBuffer rb = obj.XData;
                    if (rb == null)
                    {
                        return xDataResult;
                    }
                    else
                    {
                        bool isRegAppNameMatch = false;
                        foreach (TypedValue tv in rb)
                        {
                            if (tv.TypeCode == 1001 && tv.Value.Equals(regAppName) && !ifContains) { isRegAppNameMatch = true; }
                            else if (tv.TypeCode == 1001 && !tv.Value.Equals(regAppName) && !ifContains) { isRegAppNameMatch = false; }

                            // Для объектов, когда необходимо найти значение по частичному совпадению названия параметра
                            if (ifContains)
                            {
                                if (tv.TypeCode == 1001 && tv.Value.ToString().Contains(regAppName)) { isRegAppNameMatch = true; }
                                else if (tv.TypeCode == 1001 && !tv.Value.ToString().Contains(regAppName)) { isRegAppNameMatch = false; }
                            }

                            if (isRegAppNameMatch)
                            {
                                if (tv.TypeCode == typeCode)
                                {
                                    xDataResult = tv.Value.ToString();
                                    rb.Dispose();
                                    ts.Commit();
                                    return xDataResult;
                                }
                            }
                        }

                        rb.Dispose();
                    }
                    ts.Commit();
                }
                return xDataResult;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                return string.Empty;
            }
        }

        public static string GetStringXDataFromTheEntityByTypeCode(ObjectId oEntityId, string regAppName, short typeCode, ResultBuffer rb, bool ifContains = false)
        {
            try
            {
                string xDataResult = string.Empty;

                if (oEntityId.IsNull)
                    return string.Empty;

                if (rb == null)
                {
                    return xDataResult;
                }
                else
                {
                    bool isRegAppNameMatch = false;
                    foreach (TypedValue tv in rb)
                    {
                        if (tv.TypeCode == 1001 && tv.Value.Equals(regAppName)) { isRegAppNameMatch = true; }
                        else if (tv.TypeCode == 1001 && !tv.Value.Equals(regAppName)) { isRegAppNameMatch = false; }

                        // Для объектов, когда необходимо найти значение по частичному совпадению названия параметра
                        if (ifContains)
                        {
                            if (tv.TypeCode == 1001 && tv.Value.ToString().Contains(regAppName)) { isRegAppNameMatch = true; }
                            else if (tv.TypeCode == 1001 && !tv.Value.ToString().Contains(regAppName)) { isRegAppNameMatch = false; }
                        }

                        if (isRegAppNameMatch)
                        {
                            if (tv.TypeCode == typeCode)
                            {
                                xDataResult = tv.Value.ToString();
                                return xDataResult;
                            }
                        }
                    }

                    rb.Dispose();
                }
                return xDataResult;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                return string.Empty;
            }
        }

        public static void CleanXData(ObjectId oEntityId)
        {
            try
            {
                Document adoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                Database db = adoc.Database;

                using (Transaction ts = db.TransactionManager.StartOpenCloseTransaction())
                {
                    if (oEntityId.IsNull) { return; }

                    DBObject obj = ts.GetObject(oEntityId, OpenMode.ForWrite, false, true);
                    if (obj.XData == null) { return; }

                    var regAppCode = (int)DxfCode.ExtendedDataRegAppName;
                    var appNames = obj.XData.AsArray().Where(x => x.TypeCode == regAppCode).Select(x => x.Value.ToString());

                    foreach (var appName in appNames)
                    {
                        obj.XData = new ResultBuffer(new TypedValue(1001, appName));
                    }

                    ts.Commit();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }


    }
}
