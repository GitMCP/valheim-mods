using System.Collections.Generic;
using Jotunn.Managers;

namespace ContentTemplate
{
    /// <summary>
    /// Names and descriptions are registered as tokens rather than literal strings, so
    /// the same content can be translated without touching the item definitions. Configs
    /// reference a token by prefixing it with '$'.
    /// </summary>
    internal static class Localizations
    {
        internal static void Register()
        {
            // Returns this mod's own localization, already registered with Jotunn.
            var localization = LocalizationManager.Instance.GetLocalization();

            localization.AddTranslation(
                "English",
                new Dictionary<string, string>
                {
                    { $"{ExampleItems.BladePrefab}_name", "Example Blade" },
                    { $"{ExampleItems.BladePrefab}_description", "A bronze sword, reforged as a worked example." },
                    { $"{ExamplePieces.LanternPrefab}_name", "Example Lantern" },
                    { $"{ExamplePieces.LanternPrefab}_description", "A standing lamp that burns with a worked example." },
                });
        }
    }
}
