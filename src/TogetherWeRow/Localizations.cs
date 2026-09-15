using System.Collections.Generic;
using Jotunn.Managers;

namespace TogetherWeRow
{
    internal static class Localizations
    {
        internal static void Register()
        {
            var localization = LocalizationManager.Instance.GetLocalization();
            localization.AddTranslation(
                "English",
                new Dictionary<string, string>
                {
                    { "togetherwerow_use", "Row" },
                });
        }
    }
}
