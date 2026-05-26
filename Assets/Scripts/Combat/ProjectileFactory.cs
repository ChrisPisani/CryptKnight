using UnityEngine;

namespace CryptKnight.Combat
{
    public static class ProjectileFactory
    {
        private static Sprite circleSprite;

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
            GameObject projectileObject = new GameObject(objectName);
            projectileObject.transform.SetParent(parent, false);
            projectileObject.transform.position = position;

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(projectileObject.transform, false);
            visualObject.transform.localScale = Vector3.one * (radius * 2f);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetCircleSprite();
            renderer.color = color;
            renderer.sortingOrder = 8;

            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.radius = radius;

            projectileObject.AddComponent<Rigidbody2D>();
            ProjectileController projectile = projectileObject.AddComponent<ProjectileController>();
            projectile.Configure(direction, speed, damage, targetType, lifetimeSeconds);
            return projectile;
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
