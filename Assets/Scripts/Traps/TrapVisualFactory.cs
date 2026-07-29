using CryptKnight.Content;
using CryptKnight.Dungeon;
using CryptKnight.Loot;
using UnityEngine;

namespace CryptKnight.Traps
{
    public static class TrapVisualFactory
    {
        public const string SpikeAssetPath = "Art/Traps/spike_trap";
        public const string NorthWallAssetPath = "Art/Traps/wall_projectile_north";
        public const string WestWallAssetPath = "Art/Traps/wall_projectile_west";

        private const float SpikeVisualSize = 1.4f;
        private const float HorizontalWallVisualWidth = 6.4f;
        private const float VerticalWallVisualHeight = 4.8f;

        public static GameObject CreateSpike(string objectName, Transform parent, Vector2 position, Color fallbackColor)
        {
            GameObject root = CreateRoot(objectName, parent, position);
            Sprite sprite = RuntimeAssetLoader.LoadSprite(SpikeAssetPath);
            CreateVisual(
                root.transform,
                sprite,
                fallbackColor,
                GetUniformScale(sprite, SpikeVisualSize, true),
                4);
            return root;
        }

        public static GameObject CreateWall(
            string objectName,
            Transform parent,
            Vector2 position,
            Vector2 fireDirection,
            Color fallbackColor)
        {
            GameObject root = CreateRoot(objectName, parent, position);
            bool firesVertically = Mathf.Abs(fireDirection.y) > Mathf.Abs(fireDirection.x);
            Sprite sprite = RuntimeAssetLoader.LoadSprite(firesVertically ? NorthWallAssetPath : WestWallAssetPath);
            Vector3 scale;
            if (sprite == null)
            {
                scale = firesVertically ? new Vector3(1.1f, 0.55f, 1f) : new Vector3(0.55f, 1.1f, 1f);
            }
            else
            {
                scale = firesVertically
                    ? GetUniformScale(sprite, HorizontalWallVisualWidth, true)
                    : GetUniformScale(sprite, VerticalWallVisualHeight, false);
            }

            // The north and west art are authored facing into the room; opposite walls mirror them.
            if (firesVertically && fireDirection.y > 0f)
            {
                scale.y *= -1f;
            }
            else if (!firesVertically && fireDirection.x < 0f)
            {
                scale.x *= -1f;
            }

            CreateVisual(root.transform, sprite, fallbackColor, scale, DungeonRenderLayers.WallTrap);
            return root;
        }

        private static GameObject CreateRoot(string objectName, Transform parent, Vector2 position)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            return root;
        }

        private static void CreateVisual(
            Transform parent,
            Sprite importedSprite,
            Color fallbackColor,
            Vector3 scale,
            int sortingOrder)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = scale;
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = importedSprite != null ? importedSprite : LootItemVisuals.GetSquareSprite();
            renderer.color = importedSprite != null ? Color.white : fallbackColor;
            renderer.sortingOrder = sortingOrder;
        }

        private static Vector3 GetUniformScale(Sprite sprite, float targetSize, bool useWidth)
        {
            if (sprite == null)
            {
                return new Vector3(targetSize, targetSize, 1f);
            }

            float sourceSize = useWidth ? sprite.bounds.size.x : sprite.bounds.size.y;
            float scale = sourceSize > 0f ? targetSize / sourceSize : 1f;
            return new Vector3(scale, scale, 1f);
        }
    }
}
