using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CryptKnight.Combat
{
    public static class ProjectileFactory
    {
        private const string PlayerProjectileAssetPath = "Assets/Art/Projectiles/projectile_player_blue.png";
        private const string EnemyProjectileAssetPath = "Assets/Art/Projectiles/projectile_enemy_red.png";
        private const float ProjectileVisualDiameter = 0.54f;
        private static Sprite circleSprite;
        private static Sprite playerProjectileSprite;
        private static Sprite enemyProjectileSprite;
        private static Sprite spiderPurpleProjectileSprite;

        // Projectiles are generated from code here, maybe it should be a prefab?
        public static ProjectileController CreateCircleProjectile(
            string objectName,
            Vector2 position,
            Vector2 direction,
            DamageableTarget targetType,
            int damage,
            float speed,
            float radius,
            float lifetimeSeconds,
            Color color,
            Transform parent,
            Rect? bounceBounds = null,
            int maxBounces = 0,
            ProjectileVisualStyle visualStyle = ProjectileVisualStyle.Default)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject projectileObject = new GameObject(objectName);
            projectileObject.transform.SetParent(parent, false);
            projectileObject.transform.position = position;

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(projectileObject.transform, false);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetProjectileSprite(targetType, visualStyle);
            renderer.color = GetRendererColor(renderer.sprite, color, visualStyle);
            renderer.sortingOrder = 8;
            SetProjectileVisualTransform(visualObject.transform, renderer, normalizedDirection, ProjectileVisualDiameter);

            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.radius = radius;

            projectileObject.AddComponent<Rigidbody2D>();
            ProjectileController projectile = projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(normalizedDirection, speed, damage, targetType, lifetimeSeconds);
            if (bounceBounds.HasValue && maxBounces > 0)
            {
                projectile.ConfigureBounce(bounceBounds.Value, maxBounces);
            }

            return projectile;
        }

        private static void SetProjectileVisualTransform(Transform visualTransform, SpriteRenderer renderer, Vector2 direction, float targetDiameter)
        {
            Sprite sprite = renderer.sprite;
            if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
            {
                visualTransform.localScale = Vector3.one * targetDiameter;
                return;
            }

            float longestSide = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float scale = targetDiameter / longestSide;
            visualTransform.localScale = new Vector3(scale, scale, 1f);

            // Imported projectile art points right, so rotate it onto the shot direction.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static Color GetRendererColor(Sprite sprite, Color fallbackColor, ProjectileVisualStyle visualStyle)
        {
            if (sprite == circleSprite)
            {
                return fallbackColor;
            }

            return visualStyle == ProjectileVisualStyle.SpiderPurple && sprite == enemyProjectileSprite
                ? new Color(0.85f, 0.25f, 1f, 1f)
                : Color.white;
        }

        private static Sprite GetProjectileSprite(DamageableTarget targetType, ProjectileVisualStyle visualStyle)
        {
            // targetType is who the projectile can damage
            if (targetType == DamageableTarget.Enemy)
            {
                return GetPlayerProjectileSprite();
            }

            if (targetType == DamageableTarget.Player)
            {
                return visualStyle == ProjectileVisualStyle.SpiderPurple
                    ? GetSpiderPurpleProjectileSprite()
                    : GetEnemyProjectileSprite();
            }

            return GetCircleSprite();
        }

        private static Sprite GetPlayerProjectileSprite()
        {
            if (playerProjectileSprite == null)
            {
                playerProjectileSprite = LoadEditorSprite(PlayerProjectileAssetPath);
            }

            return playerProjectileSprite != null ? playerProjectileSprite : GetCircleSprite();
        }

        private static Sprite GetEnemyProjectileSprite()
        {
            if (enemyProjectileSprite == null)
            {
                enemyProjectileSprite = LoadEditorSprite(EnemyProjectileAssetPath);
            }

            return enemyProjectileSprite != null ? enemyProjectileSprite : GetCircleSprite();
        }

        private static Sprite GetSpiderPurpleProjectileSprite()
        {
            if (spiderPurpleProjectileSprite != null)
            {
                return spiderPurpleProjectileSprite;
            }

            Sprite sourceSprite = GetEnemyProjectileSprite();
            spiderPurpleProjectileSprite = TryCreateRecoloredSprite(sourceSprite, new Color(0.85f, 0.25f, 1f, 1f));
            return spiderPurpleProjectileSprite != null ? spiderPurpleProjectileSprite : sourceSprite;
        }

        private static Sprite TryCreateRecoloredSprite(Sprite sourceSprite, Color targetColor)
        {
            if (sourceSprite == null || sourceSprite == circleSprite)
            {
                return null;
            }

            try
            {
                Rect sourceRect = sourceSprite.textureRect;
                Texture2D sourceTexture = sourceSprite.texture;
                Color[] sourcePixels = sourceTexture.GetPixels(
                    Mathf.RoundToInt(sourceRect.x),
                    Mathf.RoundToInt(sourceRect.y),
                    Mathf.RoundToInt(sourceRect.width),
                    Mathf.RoundToInt(sourceRect.height));
                Color[] recoloredPixels = new Color[sourcePixels.Length];

                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    Color pixel = sourcePixels[i];
                    float brightness = Mathf.Max(pixel.r, pixel.g, pixel.b);
                    recoloredPixels[i] = new Color(
                        targetColor.r * brightness,
                        targetColor.g * brightness,
                        targetColor.b * brightness,
                        pixel.a);
                }

                Texture2D recoloredTexture = new Texture2D(
                    Mathf.RoundToInt(sourceRect.width),
                    Mathf.RoundToInt(sourceRect.height),
                    TextureFormat.RGBA32,
                    false);
                recoloredTexture.filterMode = sourceTexture.filterMode;
                recoloredTexture.SetPixels(recoloredPixels);
                recoloredTexture.Apply();

                return Sprite.Create(
                    recoloredTexture,
                    new Rect(0f, 0f, recoloredTexture.width, recoloredTexture.height),
                    new Vector2(sourceSprite.pivot.x / sourceRect.width, sourceSprite.pivot.y / sourceRect.height),
                    sourceSprite.pixelsPerUnit);
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private static Sprite LoadEditorSprite(string assetPath)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            // A simple generated disk keeps combat testable before making art for projectiles
            const int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return circleSprite;
        }
    }
}
