using System.IO;
using System.Reflection;
using Jotunn.Utils;
using UnityEngine;

namespace ContentTemplate
{
    /// <summary>
    /// Loads the mod's own art. Two sources are supported, because they have very
    /// different costs:
    ///
    /// - Sprites (icons) load straight from a PNG at runtime, so new icons need nothing
    ///   but an image file.
    /// - Meshes, materials, prefabs, and shaders have to come from a Unity AssetBundle,
    ///   which means building them in a Unity editor matching the game's engine version.
    ///
    /// Both are embedded in the plugin dll, so a mod stays a single file to install.
    /// </summary>
    internal static class ExampleAssets
    {
        private const string BundleResourceName = "contenttemplate";

        internal static AssetBundle Bundle { get; private set; }

        internal static Sprite ItemIcon { get; private set; }

        internal static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // The bundle is optional: until there is custom art to ship, the mod clones
            // vanilla prefabs instead, and everything else still works.
            if (HasEmbeddedResource(assembly, BundleResourceName))
            {
                Bundle = AssetUtils.LoadAssetBundleFromResources(BundleResourceName, assembly);
                ContentTemplatePlugin.Log.LogInfo($"Loaded asset bundle '{BundleResourceName}'.");
            }
            else
            {
                ContentTemplatePlugin.Log.LogInfo(
                    $"No embedded asset bundle '{BundleResourceName}'; cloning vanilla prefabs instead.");
            }

            ItemIcon = LoadEmbeddedSprite(assembly, "ContentTemplate.Assets.example_item_icon.png");
        }

        private static bool HasEmbeddedResource(Assembly assembly, string name)
        {
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (resource.EndsWith(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Sprite LoadEmbeddedSprite(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    ContentTemplatePlugin.Log.LogWarning(
                        $"Embedded sprite '{resourceName}' missing; the item will use its clone source's icon.");
                    return null;
                }

                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);

                    // LoadImage resizes the texture to the PNG's real dimensions.
                    var texture = new Texture2D(2, 2);
                    if (!AssetUtils.LoadImage(texture, buffer.ToArray()))
                    {
                        ContentTemplatePlugin.Log.LogWarning($"Could not decode '{resourceName}'.");
                        return null;
                    }

                    return Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
                }
            }
        }
    }
}
