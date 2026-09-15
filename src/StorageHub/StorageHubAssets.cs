using System;
using System.IO;
using System.Reflection;
using Jotunn.Utils;
using UnityEngine;

namespace StorageHub
{
    internal static class StorageHubAssets
    {
        private const string IconResourceName = "StorageHub.Assets.storage_hub_icon.png";

        internal static Sprite Icon { get; private set; }

        internal static void Load()
        {
            Icon = LoadSprite(IconResourceName);
            UI.HubSprites.Load();
        }

        internal static Sprite LoadSprite(string resourceName)
        {
            return LoadEmbeddedSprite(Assembly.GetExecutingAssembly(), resourceName);
        }

        private static Sprite LoadEmbeddedSprite(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    StorageHubPlugin.Log.LogWarning(
                        $"Embedded sprite '{resourceName}' missing; the piece will use its clone source's icon.");
                    return null;
                }

                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    var texture = new Texture2D(2, 2);
                    if (!AssetUtils.LoadImage(texture, buffer.ToArray()))
                    {
                        StorageHubPlugin.Log.LogWarning($"Could not decode '{resourceName}'.");
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
