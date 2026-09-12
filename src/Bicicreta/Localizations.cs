using System.Collections.Generic;
using Jotunn.Managers;

namespace Bicicreta
{
    /// <summary>
    /// Names and descriptions are registered as tokens rather than literal strings, so
    /// the mod can be translated without touching the prefab definitions. Configs
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
                    { $"{BicicretaStand.PrefabName}_name", "Bicicreta" },
                    {
                        $"{BicicretaStand.PrefabName}_description",
                        "A two-wheeled contraption. Build it, then ride it."
                    },
                    { $"{BicicretaMount.PrefabName}_name", "Bicicreta" },
                    { $"{BicicretaMount.PrefabName}_ride", "Ride" },
                });
        }
    }
}
