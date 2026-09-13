using System.IO;
using System.Reflection;
using Jotunn.Utils;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Loads the mod's own art. An icon is only a PNG, which loads at runtime and needs
    /// no Unity editor - unlike a mesh or a material, which would have to come from an
    /// AssetBundle built against the game's own engine version. It is embedded in the
    /// plugin dll, so the mod stays a single file to install.
    /// </summary>
    internal static class HirdmanAssets
    {
        private const string IconResourceName = "Hirdman.Assets.hirdman_icon.png";

        internal static Sprite Icon { get; private set; }

        internal static void Load()
        {
            Icon = LoadEmbeddedSprite(Assembly.GetExecutingAssembly(), IconResourceName);
        }

        private static Sprite LoadEmbeddedSprite(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    HirdmanPlugin.Log.LogWarning(
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
                        HirdmanPlugin.Log.LogWarning($"Could not decode '{resourceName}'.");
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
