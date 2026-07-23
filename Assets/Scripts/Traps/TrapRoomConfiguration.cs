using System;
using UnityEngine;

namespace CryptKnight.Traps
{
    public sealed class TrapRoomConfiguration
    {
        private const int MaximumSupportedTraps = 8;

        public TrapRoomConfiguration(
            int minimumTraps,
            int maximumTraps,
            TrapDefinition spikeDefinition,
            TrapDefinition projectileDefinition,
            float wallProjectileChance = 0.4f)
        {
            Spike = spikeDefinition ?? throw new ArgumentNullException(nameof(spikeDefinition));
            Projectile = projectileDefinition ?? throw new ArgumentNullException(nameof(projectileDefinition));
            if (Spike.Kind != TrapKind.Spike || Projectile.Kind != TrapKind.WallProjectile)
            {
                throw new ArgumentException("Trap definitions must match their configured trap kinds.");
            }

            MinTrapsPerRoom = Mathf.Clamp(minimumTraps, 0, MaximumSupportedTraps);
            MaxTrapsPerRoom = Mathf.Clamp(maximumTraps, MinTrapsPerRoom, MaximumSupportedTraps);
            WallProjectileChance = Mathf.Clamp01(wallProjectileChance);
        }

        public int MinTrapsPerRoom { get; }
        public int MaxTrapsPerRoom { get; }
        public float WallProjectileChance { get; }
        public TrapDefinition Spike { get; }
        public TrapDefinition Projectile { get; }

        public static TrapRoomConfiguration CreateDefault()
        {
            return new TrapRoomConfiguration(
                4,
                8,
                new TrapDefinition(TrapKind.Spike, 1, 1f),
                new TrapDefinition(
                    TrapKind.WallProjectile,
                    1,
                    2.5f,
                    1f,
                    4.8f,
                    0.135f,
                    5f),
                0.4f);
        }

        public TrapDefinition GetDefinition(TrapKind kind)
        {
            switch (kind)
            {
                case TrapKind.Spike:
                    return Spike;
                case TrapKind.WallProjectile:
                    return Projectile;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
