using System.Collections.Generic;
using CryptKnight.Content;
using UnityEngine;

namespace CryptKnight.Loot
{
    public static class LootItemVisuals
    {
        private const int CircleSpriteSize = 64;
        private const string KeyItemId = "key";
        private const string KeyAssetPath = "Art/Items/key";

        private static readonly Dictionary<string, string> defaultAssetPathsByItemId = new Dictionary<string, string>
        {
            { "heart_container", "Art/Items/heart_container" },
            { "damage_up", "Art/Items/damage_up" },
            { "speed_up", "Art/Items/speed_up" },
            { "attack_rate_up", "Art/Items/attack_rate_up" }
        };

        private static readonly Dictionary<string, Sprite> circleSpritesByItemId = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> itemSpritesByItemId = new Dictionary<string, Sprite>();
        private static Sprite squareSprite;
        private static Sprite keySprite;

        public static Color GetFallbackColor(string itemId)
        {
            switch (itemId)
            {
                case "heart_container":
                    return new Color(0.88f, 0.06f, 0.08f, 1f);
                case "damage_up":
                    return new Color(0.12f, 0.70f, 0.25f, 1f);
                case "speed_up":
                    return new Color(0.98f, 0.84f, 0.16f, 1f);
                case "attack_rate_up":
                    return new Color(0.95f, 0.42f, 0.10f, 1f);
                case "key":
                    return new Color(0.95f, 0.70f, 0.18f, 1f);
                default:
                    return new Color(0.78f, 0.78f, 0.78f, 1f);
            }
        }

        public static Sprite GetItemSprite(LootItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
            {
                return GetCircleSprite(string.Empty);
            }

            return GetItemSprite(itemDefinition.ItemId, itemDefinition.IconAssetPath);
        }

        public static Sprite GetItemSprite(string itemId)
        {
            string assetPath = defaultAssetPathsByItemId.TryGetValue(itemId ?? string.Empty, out string path) ? path : string.Empty;
            return GetItemSprite(itemId, assetPath);
        }

        private static Sprite GetItemSprite(string itemId, string iconAssetPath)
        {
            if (itemId == KeyItemId)
            {
                return GetKeySprite() ?? GetCircleSprite(itemId);
            }

            Sprite configuredSprite = GetConfiguredItemSprite(itemId, iconAssetPath);
            if (configuredSprite != null)
            {
                return configuredSprite;
            }

            return GetCircleSprite(itemId);
        }

        public static Sprite GetCircleSprite(LootItemDefinition itemDefinition)
        {
            return GetCircleSprite(itemDefinition?.ItemId);
        }

        public static Sprite GetCircleSprite(string itemId)
        {
            string safeItemId = itemId ?? string.Empty;
            if (circleSpritesByItemId.TryGetValue(safeItemId, out Sprite sprite))
            {
                return sprite;
            }

            sprite = CreateCircleSprite(GetFallbackColor(safeItemId));
            circleSpritesByItemId[safeItemId] = sprite;
            return sprite;
        }

        public static Sprite GetSquareSprite()
        {
            if (squareSprite != null)
            {
                return squareSprite;
            }

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return squareSprite;
        }

        private static Sprite GetKeySprite()
        {
            if (keySprite != null)
            {
                return keySprite;
            }

            keySprite = RuntimeAssetLoader.LoadSprite(KeyAssetPath, "key_0")
                ?? RuntimeAssetLoader.LoadSprite(KeyAssetPath, "key");
            return keySprite;
        }

        private static Sprite GetConfiguredItemSprite(string itemId, string iconAssetPath)
        {
            string safeItemId = itemId ?? string.Empty;
            // Missing or bad art falls back to generated circles, hope this doesn't happen but just in case.
            if (itemSpritesByItemId.TryGetValue(safeItemId, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            string resourcePath = iconAssetPath;
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                defaultAssetPathsByItemId.TryGetValue(safeItemId, out resourcePath);
            }

            Sprite sprite = RuntimeAssetLoader.LoadSprite(resourcePath, safeItemId)
                ?? RuntimeAssetLoader.LoadSprite(resourcePath);

            if (sprite != null)
            {
                itemSpritesByItemId[safeItemId] = sprite;
            }

            return sprite;
        }

        private static Sprite CreateCircleSprite(Color color)
        {
            Texture2D texture = new Texture2D(CircleSpriteSize, CircleSpriteSize);
            texture.filterMode = FilterMode.Bilinear;

            float center = (CircleSpriteSize - 1) * 0.5f;
            float radius = center;
            for (int y = 0; y < CircleSpriteSize; y++)
            {
                for (int x = 0; x < CircleSpriteSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, CircleSpriteSize, CircleSpriteSize), new Vector2(0.5f, 0.5f), CircleSpriteSize);
        }
    }
}
