using UnityEngine;

namespace CryptKnight.Traps
{
    public sealed class TrapDefinition
    {
        public TrapDefinition(
            TrapKind kind,
            int damage,
            float activationIntervalSeconds,
            float initialDelaySeconds = 0f,
            float projectileSpeed = 0f,
            float projectileRadius = 0f,
            float projectileLifetimeSeconds = 0f)
        {
            Kind = kind;
            Damage = Mathf.Max(1, damage);
            ActivationIntervalSeconds = Mathf.Max(0.01f, activationIntervalSeconds);
            InitialDelaySeconds = Mathf.Max(0f, initialDelaySeconds);
            ProjectileSpeed = Mathf.Max(0f, projectileSpeed);
            ProjectileRadius = Mathf.Max(0f, projectileRadius);
            ProjectileLifetimeSeconds = Mathf.Max(0f, projectileLifetimeSeconds);
        }

        public TrapKind Kind { get; }
        public int Damage { get; }
        public float ActivationIntervalSeconds { get; }
        public float InitialDelaySeconds { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileRadius { get; }
        public float ProjectileLifetimeSeconds { get; }
    }
}
