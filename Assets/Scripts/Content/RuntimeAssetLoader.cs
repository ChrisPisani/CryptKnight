using System;
using UnityEngine;

namespace CryptKnight.Content
{
    public static class RuntimeAssetLoader
    {
        public static Sprite LoadSprite(string resourcePath, string spriteName = null)
        {
            Sprite[] sprites = LoadSprites(resourcePath);
            if (sprites.Length == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return sprites[0];
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                if (string.Equals(sprites[i].name, spriteName, StringComparison.Ordinal))
                {
                    return sprites[i];
                }
            }

            return null;
        }

        public static Sprite[] LoadSprites(string resourcePath)
        {
            return string.IsNullOrWhiteSpace(resourcePath)
                ? Array.Empty<Sprite>()
                : Resources.LoadAll<Sprite>(resourcePath);
        }

        public static Font LoadFont(string resourcePath)
        {
            return string.IsNullOrWhiteSpace(resourcePath) ? null : Resources.Load<Font>(resourcePath);
        }
    }
}
