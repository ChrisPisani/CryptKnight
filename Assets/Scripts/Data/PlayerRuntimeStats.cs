using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptKnight.Data
{
    [Serializable]
    public sealed class PlayerRuntimeStats
    {
        private readonly PlayerBaseStats baseStats;
        private readonly List<PlayerStatModifier> modifiers = new List<PlayerStatModifier>();

        public PlayerRuntimeStats(PlayerBaseStats baseStats)
        {
            this.baseStats = baseStats ?? throw new ArgumentNullException(nameof(baseStats));
        }

        public IReadOnlyList<PlayerStatModifier> Modifiers => modifiers;

        // Runtime stats are recalculated from base stats plus active modifiers so item buffs stack correctly
        public int MaxHealth => Math.Max(1, baseStats.MaxHealth + modifiers.Sum(modifier => modifier.MaxHealthBonus));
        public int Damage => Math.Max(0, baseStats.Damage + modifiers.Sum(modifier => modifier.DamageBonus));
        public float MovementSpeed => Math.Max(0f, baseStats.MovementSpeed + modifiers.Sum(modifier => modifier.MovementSpeedBonus));
        public float AttackRate => Math.Max(0.01f, baseStats.AttackRate + modifiers.Sum(modifier => modifier.AttackRateBonus));
        public float AttackCooldownSeconds => 1f / AttackRate;

        public void AddModifier(PlayerStatModifier modifier)
        {
            if (modifier == null)
            {
                return;
            }

            modifiers.Add(modifier);
        }
    }
}
