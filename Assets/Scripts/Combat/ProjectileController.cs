using UnityEngine;

namespace CryptKnight.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ProjectileController : MonoBehaviour
    {
        private DamageableTarget targetType;
        private int damage;
        private float lifetimeSeconds;
        private float spawnTime;
        private bool isConfigured;
        private Rect? bounceBounds;
        private int bouncesRemaining;

        public DamageableTarget TargetType => targetType;
        public int Damage => damage;
        public int BouncesRemaining => bouncesRemaining;

        public void Configure(Vector2 direction, float speed, int damageAmount, DamageableTarget target, float lifetime)
        {
            targetType = target;
            damage = damageAmount;
            lifetimeSeconds = lifetime;
            spawnTime = Time.time;
            isConfigured = true;
            bounceBounds = null;
            bouncesRemaining = 0;

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = direction.normalized * speed;

            Collider2D projectileCollider = GetComponent<Collider2D>();
            projectileCollider.isTrigger = true;
        }

        public void ConfigureBounce(Rect bounds, int maxBounces)
        {
            bounceBounds = bounds;
            bouncesRemaining = Mathf.Max(0, maxBounces);
        }

        private void Update()
        {
            if (!isConfigured)
            {
                return;
            }

            ApplyBoundsBounce();

            if (isConfigured && Time.time >= spawnTime + lifetimeSeconds)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isConfigured)
            {
                return;
            }

            if (other.isTrigger)
            {
                return;
            }

            // Projectiles carry their intended target type so player and enemy shots can share this component
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.TargetType == targetType)
            {
                damageable.ApplyDamage(damage);
                Destroy(gameObject);
                return;
            }

            if (damageable != null)
            {
                // only the configured target type can be damaged.
                return;
            }

            // physics colliders are treated as blockers for now, mainly room walls, traps in future?
            if (TryBounceFromBlocker(other))
            {
                return;
            }

            Destroy(gameObject);
        }

        private void ApplyBoundsBounce()
        {
            if (!bounceBounds.HasValue)
            {
                return;
            }

            Rect bounds = bounceBounds.Value;
            Vector2 position = transform.position;
            bool outsideX = position.x < bounds.xMin || position.x > bounds.xMax;
            bool outsideY = position.y < bounds.yMin || position.y > bounds.yMax;
            if (!outsideX && !outsideY)
            {
                return;
            }

            if (bouncesRemaining <= 0)
            {
                Destroy(gameObject);
                return;
            }

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            Vector2 velocity = body.linearVelocity;
            if (outsideX)
            {
                velocity.x *= -1f;
            }

            if (outsideY)
            {
                velocity.y *= -1f;
            }

            bouncesRemaining--;
            Vector2 clampedPosition = new Vector2(
                Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(position.y, bounds.yMin, bounds.yMax));
            transform.position = clampedPosition;
            body.position = clampedPosition;
            body.linearVelocity = velocity;
        }

        private bool TryBounceFromBlocker(Collider2D blocker)
        {
            if (bouncesRemaining <= 0)
            {
                return false;
            }

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            Vector2 velocity = body.linearVelocity;
            Bounds bounds = blocker.bounds;
            if (bounds.size.y > bounds.size.x)
            {
                velocity.x *= -1f;
            }
            else if (bounds.size.x > bounds.size.y)
            {
                velocity.y *= -1f;
            }
            else if (Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y))
            {
                velocity.x *= -1f;
            }
            else
            {
                velocity.y *= -1f;
            }

            bouncesRemaining--;
            body.linearVelocity = velocity;
            return true;
        }
    }
}
