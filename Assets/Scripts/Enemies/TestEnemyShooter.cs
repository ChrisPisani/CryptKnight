using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Enemies
{
    public sealed class TestEnemyShooter : MonoBehaviour
    {
        private const float ShotIntervalSeconds = 1.5f;
        private const float FirstShotDelaySeconds = 0.5f;
        private const float ProjectileSpeed = 5f;
        private const float ProjectileRadius = 0.12f;
        private const float ProjectileLifetimeSeconds = 5f;
        // Damage is in half hearts, so 1 damage is half a heart, 2 damage is a full heart
        private const int ProjectileDamage = 1;

        private Transform player;
        private float nextShotTime;

        public void Initialize(Transform playerTarget)
        {
            player = playerTarget;
            nextShotTime = Time.time + FirstShotDelaySeconds;
        }

        private void Update()
        {
            if (player == null || Time.time < nextShotTime)
            {
                return;
            }

            // it only fires straight at the player current position.
            Vector2 direction = player.position - transform.position;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.left;
            }

            ProjectileFactory.CreateCircleProjectile(
                "Enemy Projectile",
                (Vector2)transform.position + direction.normalized * 0.65f,
                direction.normalized,
                DamageableTarget.Player,
                ProjectileDamage,
                ProjectileSpeed,
                ProjectileRadius,
                ProjectileLifetimeSeconds,
                new Color(1f, 0.25f, 0.2f, 1f),
                transform.parent);

            nextShotTime = Time.time + ShotIntervalSeconds;
        }
    }
}
