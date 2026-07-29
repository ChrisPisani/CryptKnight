using System;

namespace CryptKnight.Data
{
    [Serializable]
    public sealed class PlayerBaseStats
    {
        public PlayerBaseStats(
            int maxHealth,
            float damage,
            float movementSpeed,
            float attackRate,
            int projectileCount = 1,
            float projectileSpeed = 8f,
            int projectileBounces = 0,
            float projectileSizeMultiplier = 1f)
        {
            MaxHealth = maxHealth;
            Damage = damage;
            MovementSpeed = movementSpeed;
            AttackRate = attackRate;
            ProjectileCount = projectileCount;
            ProjectileSpeed = projectileSpeed;
            ProjectileBounces = projectileBounces;
            ProjectileSizeMultiplier = projectileSizeMultiplier;
        }

        public int MaxHealth { get; }
        public float Damage { get; }
        public float MovementSpeed { get; }
        public float AttackRate { get; }
        public int ProjectileCount { get; }
        public float ProjectileSpeed { get; }
        public int ProjectileBounces { get; }
        public float ProjectileSizeMultiplier { get; }

        public static PlayerBaseStats CreateDefault()
        {
            return new PlayerBaseStats(
                maxHealth: 6,
                damage: 1,
                movementSpeed: 5f,
                attackRate: 1f,
                projectileCount: 1,
                projectileSpeed: 8f,
                projectileBounces: 0,
                projectileSizeMultiplier: 1f);
        }
    }
}
