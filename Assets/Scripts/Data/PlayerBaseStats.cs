using System;

namespace CryptKnight.Data
{
    [Serializable]
    public sealed class PlayerBaseStats
    {
        public PlayerBaseStats(int maxHealth, int damage, float movementSpeed, float attackRate)
        {
            MaxHealth = maxHealth;
            Damage = damage;
            MovementSpeed = movementSpeed;
            AttackRate = attackRate;
        }

        public int MaxHealth { get; }
        public int Damage { get; }
        public float MovementSpeed { get; }
        public float AttackRate { get; }

        public static PlayerBaseStats CreateDefault()
        {
            return new PlayerBaseStats(
                maxHealth: 6,
                damage: 1,
                movementSpeed: 5f,
                attackRate: 1f);
        }
    }
}
