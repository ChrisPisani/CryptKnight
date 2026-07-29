using System;

namespace CryptKnight.Data
{
    [Serializable]
    public sealed class PlayerStatModifier
    {
        public PlayerStatModifier(
            int maxHealthBonus = 0,
            float damageBonus = 0f,
            float movementSpeedBonus = 0f,
            float attackRateBonus = 0f,
            int projectileCountBonus = 0,
            float projectileSpeedBonus = 0f,
            int projectileBouncesBonus = 0,
            float projectileSizeBonus = 0f)
        {
            MaxHealthBonus = maxHealthBonus;
            DamageBonus = damageBonus;
            MovementSpeedBonus = movementSpeedBonus;
            AttackRateBonus = attackRateBonus;
            ProjectileCountBonus = projectileCountBonus;
            ProjectileSpeedBonus = projectileSpeedBonus;
            ProjectileBouncesBonus = projectileBouncesBonus;
            ProjectileSizeBonus = projectileSizeBonus;
        }

        public int MaxHealthBonus { get; }
        public float DamageBonus { get; }
        public float MovementSpeedBonus { get; }
        public float AttackRateBonus { get; }
        public int ProjectileCountBonus { get; }
        public float ProjectileSpeedBonus { get; }
        public int ProjectileBouncesBonus { get; }
        public float ProjectileSizeBonus { get; }
    }
}
