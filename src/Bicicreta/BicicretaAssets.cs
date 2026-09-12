using System;
using System.IO;
using System.Reflection;
using Jotunn.Utils;
using UnityEngine;

namespace Bicicreta
{
    /// <summary>
    /// Loads the mod's own art. Two sources are supported, because they have very
    /// different costs:
    ///
    /// - Sprites (icons) load straight from a PNG at runtime, so a new icon needs nothing
    ///   but an image file.
    /// - Meshes, materials, prefabs, and shaders have to come from a Unity AssetBundle,
    ///   which means building them in an editor matching the game's engine version.
    ///
    /// Both are embedded in the plugin dll, so the mod stays a single file to install.
    /// </summary>
    internal static class BicicretaAssets
    {
        private const string BundleResourceName = "bicicreta";
        private const string IconResourceName = "Bicicreta.Assets.bicicreta_icon.png";

        internal static AssetBundle Bundle { get; private set; }

        internal static Sprite Icon { get; private set; }

        internal static void Load()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // The bundle is optional: until there is a real bicycle model to ship, the mod
            // borrows vanilla ones, and everything else still works.
            if (HasEmbeddedResource(assembly, BundleResourceName))
            {
                Bundle = AssetUtils.LoadAssetBundleFromResources(BundleResourceName, assembly);
                BicicretaPlugin.Log.LogInfo($"Loaded asset bundle '{BundleResourceName}'.");
            }

            Icon = LoadEmbeddedSprite(assembly, IconResourceName);
        }

        private static bool HasEmbeddedResource(Assembly assembly, string name)
        {
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                if (resource.EndsWith(name, StringComparison.OrdinalIgnoreCase))
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
                    BicicretaPlugin.Log.LogWarning(
                        $"Embedded sprite '{resourceName}' missing; the piece will use its clone source's icon.");
                    return null;
                }

                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);

                    // LoadImage resizes the texture to the PNG's real dimensions.
                    var texture = new Texture2D(2, 2);
                    if (!AssetUtils.LoadImage(texture, buffer.ToArray()))
                    {
                        BicicretaPlugin.Log.LogWarning($"Could not decode '{resourceName}'.");
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
