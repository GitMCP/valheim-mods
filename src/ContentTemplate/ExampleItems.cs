using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace ContentTemplate
{
    internal static class ExampleItems
    {
        internal const string BladePrefab = "CT_ExampleBlade";

        /// <summary>
        /// Vanilla prefab the example item is cloned from. Cloning inherits the mesh,
        /// animations, and attack data, which is what makes a new weapon possible without
        /// opening Unity; swap this for an AssetBundle asset once there is a custom model.
        /// </summary>
        private const string BladeCloneSource = "SwordBronze";

        internal static void Register()
        {
            var config = new ItemConfig
            {
                Name = $"${BladePrefab}_name",
                Description = $"${BladePrefab}_description",
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 2,
                Requirements = new[]
                {
                    new RequirementConfig("Bronze", 8, 4, true),
                    new RequirementConfig("Wood", 4, 2, true),
                },
            };

            if (ExampleAssets.ItemIcon != null)
            {
                config.Icon = ExampleAssets.ItemIcon;
            }

            var blade = ExampleAssets.Bundle != null
                ? new CustomItem(ExampleAssets.Bundle, BladePrefab, fixReference: true, config)
                : new CustomItem(BladePrefab, BladeCloneSource, config);

            if (!ItemManager.Instance.AddItem(blade))
            {
                ContentTemplatePlugin.Log.LogError($"Failed to register item '{BladePrefab}'.");
            }
        }
    }
}
