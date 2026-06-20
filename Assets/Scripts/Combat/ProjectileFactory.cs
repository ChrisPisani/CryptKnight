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
            Transform parent)
        {
            Vector2 normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            GameObject projectileObject = new GameObject(objectName);
            projectileObject.transform.SetParent(parent, false);
            projectileObject.transform.position = position;

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(projectileObject.transform, false);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetProjectileSprite(targetType);
            renderer.color = renderer.sprite == circleSprite ? color : Color.white;
            renderer.sortingOrder = 8;
            SetProjectileVisualTransform(visualObject.transform, renderer, normalizedDirection, ProjectileVisualDiameter);

            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.radius = radius;

            projectileObject.AddComponent<Rigidbody2D>();
            ProjectileController projectile = projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(normalizedDirection, speed, damage, targetType, lifetimeSeconds);
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

        private static Sprite GetProjectileSprite(DamageableTarget targetType)
        {
            // targetType is who the projectile can damage
            if (targetType == DamageableTarget.Enemy)
            {
                return GetPlayerProjectileSprite();
            }

            if (targetType == DamageableTarget.Player)
            {
                return GetEnemyProjectileSprite();
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
