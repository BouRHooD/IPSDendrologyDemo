using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace IPSDendrologyDemo.Other
{
    public static class EntityExts
    {
        public static string HandleToString(this Entity oEntity)
        {
            if (oEntity != null) { return oEntity.Handle.Value.ToString(); }
            else { return ""; }
        }

        public static bool IsBad(this Entity oEntity)
        {
            try
            {
                if (oEntity == null || oEntity.IsDisposed || oEntity.IsErased || oEntity.IsTransactionResident) 
                { 
                    return true; 
                }

                return false;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
