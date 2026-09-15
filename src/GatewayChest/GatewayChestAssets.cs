using System;
using System.IO;
using System.Reflection;
using Jotunn.Utils;
using UnityEngine;

namespace GatewayChest
{
    internal static class GatewayChestAssets
    {
        private const string IconResourceName = "GatewayChest.Assets.gateway_chest_icon.png";

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
                    GatewayChestPlugin.Log.LogWarning(
                        $"Embedded sprite '{resourceName}' missing; the piece will use its clone source's icon.");
                    return null;
                }

                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    var texture = new Texture2D(2, 2);
                    if (!AssetUtils.LoadImage(texture, buffer.ToArray()))
                    {
                        GatewayChestPlugin.Log.LogWarning($"Could not decode '{resourceName}'.");
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
