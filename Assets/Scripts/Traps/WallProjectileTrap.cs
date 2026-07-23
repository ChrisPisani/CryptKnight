using System;
using CryptKnight.Application;
using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Traps
{
    public sealed class WallProjectileTrap : MonoBehaviour
    {
        private const float ProjectileSpawnOffset = 0.65f;
        private static readonly Color ProjectileColor = new Color(1f, 0.65f, 0.10f, 1f);

        private TrapDefinition definition;
        private Vector2 fireDirection;
        private Transform projectileParent;
        private float nextFireTime;

        public Vector2 FireDirection => fireDirection;
        public float NextFireTime => nextFireTime;

        public void Initialize(
            TrapDefinition trapDefinition,
            Vector2 direction,
            Transform projectileRoot,
            float phaseOffsetSeconds = 0f)
        {
            definition = trapDefinition ?? throw new ArgumentNullException(nameof(trapDefinition));
            if (definition.Kind != TrapKind.WallProjectile)
            {
                throw new ArgumentException("WallProjectileTrap requires a projectile definition.", nameof(trapDefinition));
            }

            fireDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            projectileParent = projectileRoot;
            nextFireTime = Time.time + definition.InitialDelaySeconds + Mathf.Max(0f, phaseOffsetSeconds);
        }

        private void Update()
        {
            if (GameManager.HasInstance && GameManager.Instance.IsGameplayPaused)
            {
                return;
            }

            TryFire(Time.time);
        }

        public ProjectileController TryFire(float currentTime)
        {
            if (definition == null || currentTime < nextFireTime)
            {
                return null;
            }

            nextFireTime = currentTime + definition.ActivationIntervalSeconds;
            Vector2 spawnPosition = (Vector2)transform.position + fireDirection * ProjectileSpawnOffset;
            return ProjectileFactory.CreateCircleProjectile(
                "Trap Projectile",
                spawnPosition,
                fireDirection,
                DamageableTarget.Player,
                definition.Damage,
                definition.ProjectileSpeed,
                definition.ProjectileRadius,
                definition.ProjectileLifetimeSeconds,
                ProjectileColor,
                projectileParent != null ? projectileParent : transform.parent,
                visualStyle: ProjectileVisualStyle.TrapProjectile);
        }
    }
}
