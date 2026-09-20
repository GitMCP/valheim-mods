using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Jotunn.Managers;
using Newtonsoft.Json;

namespace NjordWarehouseKeeper
{
    /// <summary>
    /// Loads every embedded JSON catalog whose file name matches a Valheim language
    /// (for example, Localization/English.json or Localization/Russian.json).
    /// Adding another language only requires adding another catalog file.
    /// </summary>
    internal static class Localizations
    {
        private const string ResourceMarker = ".Localization.";
        private const string ResourceExtension = ".json";
        private static readonly Regex Placeholder = new Regex(@"\{\d+\}", RegexOptions.Compiled);

        internal static void Register()
        {
            try
            {
                var catalogs = LoadCatalogs(typeof(Localizations).Assembly);
                Validate(catalogs);

                var localization = LocalizationManager.Instance.GetLocalization();
                foreach (var catalog in catalogs.OrderBy(c => c.Language == "English" ? 0 : 1))
                {
                    localization.AddTranslation(catalog.Language, catalog.Translations);
                }
            }
            catch (Exception ex)
            {
                NjordWarehouseKeeperPlugin.Log.LogError(
                    "Could not register Njord localization catalogs: " + ex);
            }
        }

        private static List<Catalog> LoadCatalogs(Assembly assembly)
        {
            var catalogs = new List<Catalog>();
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.EndsWith(ResourceExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var marker = resourceName.LastIndexOf(ResourceMarker, StringComparison.OrdinalIgnoreCase);
                if (marker < 0)
                {
                    continue;
                }

                var languageStart = marker + ResourceMarker.Length;
                var languageLength = resourceName.Length - languageStart - ResourceExtension.Length;
                if (languageLength <= 0)
                {
                    continue;
                }

                var language = resourceName.Substring(languageStart, languageLength);
                if (language.IndexOf('.') >= 0)
                {
                    continue;
                }

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException(
                            "Embedded localization resource is unavailable: " + resourceName);
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                            reader.ReadToEnd());
                        if (translations == null || translations.Count == 0)
                        {
                            throw new InvalidDataException(
                                "Localization catalog is empty: " + resourceName);
                        }

                        catalogs.Add(new Catalog(language, translations));
                    }
                }
            }

            return catalogs;
        }

        private static void Validate(IReadOnlyList<Catalog> catalogs)
        {
            var english = catalogs.FirstOrDefault(
                c => string.Equals(c.Language, "English", StringComparison.OrdinalIgnoreCase));
            if (english == null)
            {
                throw new InvalidDataException("English localization catalog is required.");
            }

            var duplicateLanguages = catalogs
                .GroupBy(c => c.Language, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateLanguages.Length > 0)
            {
                throw new InvalidDataException(
                    "Duplicate localization languages: " + string.Join(", ", duplicateLanguages));
            }

            var referenceKeys = new HashSet<string>(english.Translations.Keys, StringComparer.Ordinal);
            var errors = new List<string>();
            foreach (var catalog in catalogs)
            {
                var keys = new HashSet<string>(catalog.Translations.Keys, StringComparer.Ordinal);
                var missing = referenceKeys.Except(keys).OrderBy(key => key).ToArray();
                var extra = keys.Except(referenceKeys).OrderBy(key => key).ToArray();
                if (missing.Length > 0)
                {
                    errors.Add(catalog.Language + " is missing: " + string.Join(", ", missing));
                }

                if (extra.Length > 0)
                {
                    errors.Add(catalog.Language + " has unexpected keys: " + string.Join(", ", extra));
                }

                foreach (var key in referenceKeys.Intersect(keys).OrderBy(key => key))
                {
                    var expected = Placeholders(english.Translations[key]);
                    var actual = Placeholders(catalog.Translations[key]);
                    if (!expected.SequenceEqual(actual))
                    {
                        errors.Add(
                            catalog.Language + " changes placeholders for " + key +
                            ": expected " + string.Join(", ", expected) +
                            ", got " + string.Join(", ", actual));
                    }
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidDataException(string.Join("; ", errors));
            }
        }

        private static string[] Placeholders(string value)
        {
            return Placeholder.Matches(value ?? string.Empty)
                .Cast<Match>()
                .Select(match => match.Value)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private sealed class Catalog
        {
            internal Catalog(string language, Dictionary<string, string> translations)
            {
                Language = language;
                Translations = translations;
            }

            internal string Language { get; }
            internal Dictionary<string, string> Translations { get; }
        }
    }
}
