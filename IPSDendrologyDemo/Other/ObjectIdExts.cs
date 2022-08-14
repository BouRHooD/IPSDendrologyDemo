using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;

namespace IPSDendrologyDemo.Other
{
    public static class ObjectIdExts
    {
        public static bool IsBad(this ObjectId objId)
        {
            // Если файл стертый ИЛИ равен null ИЛИ Проверяет, является ли заданный объект допустимым ИЛИ объект удален (с ирархией пользователей)
            if (objId.IsErased || objId.IsNull || !objId.IsValid || objId.IsEffectivelyErased) { return true; }
            return false;
        }
    }
}
