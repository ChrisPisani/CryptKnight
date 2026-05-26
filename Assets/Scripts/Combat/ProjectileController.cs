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

        public DamageableTarget TargetType => targetType;
        public int Damage => damage;

        public void Configure(Vector2 direction, float speed, int damageAmount, DamageableTarget target, float lifetime)
        {
            targetType = target;
            damage = damageAmount;
            lifetimeSeconds = lifetime;
            spawnTime = Time.time;
            isConfigured = true;

            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = direction.normalized * speed;

            Collider2D projectileCollider = GetComponent<Collider2D>();
            projectileCollider.isTrigger = true;
        }

        private void Update()
        {
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
            Destroy(gameObject);
        }
    }
}
