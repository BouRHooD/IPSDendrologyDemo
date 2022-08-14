using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using IPSDendrologyDemo.Other;
using IPSDendrologyDemo.ViewModels;
using System.IO;


namespace IPSDendrologyDemo.Services
{
    public static class GenerateDendrologyService
    {
        public static BlockReference CreateBlockReferencePoint()
        {
            string assemblyDir = FileUtils.GetAssemblyDirectory();
            string pitBlockPath = Path.Combine(assemblyDir, Blocks.fileName);
            BlockUtils.TryLoadBlockFromAnotherFile(pitBlockPath, Blocks.pointBlockReferenceName);

            // Добавляем в чертеж 
            if (BlockUtils.IsBlockExist(Blocks.pointBlockReferenceName))
            {
                Point3d pitPoint = PromptUtils.promptAPoint("Выберите точку для вставки блока:");

                Matrix3d pWCS = AppData.Editor.CurrentUserCoordinateSystem;
                if (pWCS.CoordinateSystem3d.Origin.X == pitPoint.X && pWCS.CoordinateSystem3d.Origin.Y == pitPoint.Y)
                {
                    return null;
                }

                var blockRef = BlockUtils.CreateBlockReference(Blocks.pointBlockReferenceName, pitPoint);
                return blockRef;
            }

            // Если зашли сюда, то не получилось добавить блок котлована
            System.Windows.MessageBox.Show("Блок отсутствует в чертеже");
            return null;
        }

        public static BlockReference CreateBlockReferenceMLeaderPoint(Point3d pitPoint)
        {
            string blockName = Blocks.mLeaderBlockReferenceName;
            string assemblyDir = FileUtils.GetAssemblyDirectory();
            string pitBlockPath = Path.Combine(assemblyDir, Blocks.fileName);
            BlockUtils.TryLoadBlockFromAnotherFile(pitBlockPath, blockName);

            // Добавляем в чертеж 
            if (BlockUtils.IsBlockExist(blockName))
            {
                Matrix3d pWCS = AppData.Editor.CurrentUserCoordinateSystem;
                if (pWCS.CoordinateSystem3d.Origin.X == pitPoint.X && pWCS.CoordinateSystem3d.Origin.Y == pitPoint.Y) { return null; }
                var blockRef = BlockUtils.CreateBlockReference(Blocks.pointBlockReferenceName, pitPoint);
                return blockRef;
            }

            // Если зашли сюда, то не получилось добавить блок котлована
            System.Windows.MessageBox.Show("Блок отсутствует в чертеже");
            return null;
        }

        private static DendrologyService GetServiceFromBlockReference(BlockReference blockReference)
        {
            try
            {
                if (blockReference == null || blockReference.Id.IsNull) { return null; }
                if (!blockReference.GetBlockRealName().Equals(Blocks.pointBlockReferenceName)) { return null; }

                AppData.AddEntityHandleToDwgDatabase(blockReference);

                DendrologyService oDendrologyService = new DendrologyService { Model = blockReference };
                oDendrologyService.DendrologyServiceNumber = XDataUtils.GetStringXDataFromTheEntityByTypeCode(blockReference.Id, "DendrologyServiceNumber", (int)DxfCode.ExtendedDataAsciiString, blockReference.XData);
                oDendrologyService.SelectedConclusion = oDendrologyService.GetEntityConclusion();
                return oDendrologyService;
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
                return null;
            }
        }

        public static DendrologyService CreatePitOrTrench(Entity oEntity)
        {
            try
            {
                if (oEntity is BlockReference)
                {
                    BlockReference blockReferencePit = oEntity as BlockReference;
                    return GetServiceFromBlockReference(blockReferencePit);
                }

                return null;
            }
            catch (System.Exception ex)
            {
                AppData.WtiteMassageToAutocad("IPSDendrology Error: " + ex.Message + "\n");
                return null;
            }
        }
    }
}
