using System;

namespace CryptKnight.Data
{
    [Serializable]
    public sealed class PlayerStatModifier
    {
        public PlayerStatModifier(int maxHealthBonus = 0, int damageBonus = 0, float movementSpeedBonus = 0f, float attackRateBonus = 0f)
        {
            MaxHealthBonus = maxHealthBonus;
            DamageBonus = damageBonus;
            MovementSpeedBonus = movementSpeedBonus;
            AttackRateBonus = attackRateBonus;
        }

        public int MaxHealthBonus { get; }
        public int DamageBonus { get; }
        public float MovementSpeedBonus { get; }
        public float AttackRateBonus { get; }
    }
}
