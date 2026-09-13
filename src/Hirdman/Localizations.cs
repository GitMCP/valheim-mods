using System.Collections.Generic;
using Jotunn.Managers;

namespace Hirdman
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
            var localization = LocalizationManager.Instance.GetLocalization();

            localization.AddTranslation(
                "English",
                new Dictionary<string, string>
                {
                    { $"{HirdmanMuster.PrefabName}_name", "Muster post" },
                    {
                        $"{HirdmanMuster.PrefabName}_description",
                        "Plant it, and someone comes to stand by it. Press Use to send them " +
                        "with you or leave them; type 'hird' in the console to say more."
                    },
                    { $"{HirdmanRetainer.PrefabName}_name", "Hirdman" },
                });
        }
    }
}
