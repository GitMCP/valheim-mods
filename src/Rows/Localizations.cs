using System.Collections.Generic;
using Jotunn.Managers;

namespace Rows
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
                    { "rows_use", "Row" },
                });
        }
    }
}
