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
                    { $"{HirdmanOutpost.PrefabName}_name", "Hirdman outpost" },
                    {
                        $"{HirdmanOutpost.PrefabName}_description",
                        "A banner post where fighters looking for work gather. Press Use to " +
                        "hire one for coins, as often as you can pay for it."
                    },
                    { $"{HirdmanBell.PrefabName}_name", "Muster bell" },
                    {
                        $"{HirdmanBell.PrefabName}_description",
                        "Press Use to call every retainer in your service back to it, " +
                        "wherever you left them. Where it stands is where home is."
                    },
                    { $"{HirdmanRetainer.PrefabName}_name", "Hirdman" },
                });
        }
    }
}
