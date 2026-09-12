using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ContentTemplate
{
    internal static class ExamplePieces
    {
        internal const string LanternPrefab = "CT_ExampleLantern";

        private const string LanternCloneSource = "piece_groundtorch_green";

        internal static void Register()
        {
            var config = new PieceConfig
            {
                Name = $"${LanternPrefab}_name",
                Description = $"${LanternPrefab}_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Furniture,
                CraftingStation = CraftingStations.Workbench,
                Requirements = new[]
                {
                    new RequirementConfig("Wood", 3, 0, true),
                    new RequirementConfig("Resin", 2, 0, true),
                },
            };

            var lantern = ExampleAssets.Bundle != null
                ? new CustomPiece(ExampleAssets.Bundle, LanternPrefab, fixReference: true, config)
                : new CustomPiece(LanternPrefab, LanternCloneSource, config);

            if (!PieceManager.Instance.AddPiece(lantern))
            {
                ContentTemplatePlugin.Log.LogError($"Failed to register piece '{LanternPrefab}'.");
            }
        }
    }
}
