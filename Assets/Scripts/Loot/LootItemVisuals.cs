using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CryptKnight.Loot
{
    public static class LootItemVisuals
    {
        private const int CircleSpriteSize = 64;
        private const string KeyItemId = "key";
        private const string KeyAssetPath = "Assets/Art/Items/key.png";

        private static readonly Dictionary<string, string> defaultAssetPathsByItemId = new Dictionary<string, string>
        {
            { "heart_container", "Assets/Art/Items/heart_container.png" },
            { "damage_up", "Assets/Art/Items/damage_up.png" },
            { "speed_up", "Assets/Art/Items/speed_up.png" },
            { "attack_rate_up", "Assets/Art/Items/attack_rate_up.png" }
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

#if UNITY_EDITOR
            keySprite = LoadSpriteAtPath(KeyAssetPath, "key_0") ?? LoadSpriteAtPath(KeyAssetPath, "key");
#endif
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

            Sprite sprite = null;
#if UNITY_EDITOR
            string assetPath = iconAssetPath;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                defaultAssetPathsByItemId.TryGetValue(safeItemId, out assetPath);
            }

            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) ?? LoadSpriteAtPath(assetPath, safeItemId);
            }
#endif

            if (sprite != null)
            {
                itemSpritesByItemId[safeItemId] = sprite;
            }

            return sprite;
        }

#if UNITY_EDITOR
        private static Sprite LoadSpriteAtPath(string assetPath, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return null;
        }
#endif

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
