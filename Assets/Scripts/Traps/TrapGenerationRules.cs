using System;
using System.Collections.Generic;
using CryptKnight.Dungeon;
using UnityEngine;

namespace CryptKnight.Traps
{
    public sealed class TrapGenerationRules
    {
        private const int CountSalt = 0x5452434E;
        private const int KindSalt = 0x4B494E44;
        private const int SpikePositionSalt = 0x53504B50;
        private const int WallPositionSalt = 0x57414C50;
        private const int PhaseSalt = 0x50484153;
        private const float MaximumPhaseOffsetSeconds = 0.75f;

        private static readonly Vector2[] SpikeAnchors =
        {
            new Vector2(-4.5f, 1.7f),
            new Vector2(4.5f, 1.7f),
            new Vector2(-4.5f, -1.7f),
            new Vector2(4.5f, -1.7f),
            new Vector2(-1.5f, 2.2f),
            new Vector2(1.5f, 2.2f),
            new Vector2(-1.5f, -2.2f),
            new Vector2(1.5f, -2.2f)
        };

        private static readonly WallAnchor[] WallAnchors =
        {
            new WallAnchor(new Vector2(-9.68f, -2.5f), Vector2.right),
            new WallAnchor(new Vector2(-9.68f, 2.5f), Vector2.right),
            new WallAnchor(new Vector2(9.68f, -2.5f), Vector2.left),
            new WallAnchor(new Vector2(9.68f, 2.5f), Vector2.left),
            new WallAnchor(new Vector2(-4.5f, 4.8f), Vector2.down),
            new WallAnchor(new Vector2(4.5f, 4.8f), Vector2.down),
            new WallAnchor(new Vector2(-4.5f, -4.8f), Vector2.up),
            new WallAnchor(new Vector2(4.5f, -4.8f), Vector2.up)
        };

        private readonly TrapRoomConfiguration configuration;

        public TrapGenerationRules(TrapRoomConfiguration trapConfiguration)
        {
            configuration = trapConfiguration ?? throw new ArgumentNullException(nameof(trapConfiguration));
        }

        public static TrapGenerationRules CreateDefault()
        {
            return new TrapGenerationRules(TrapRoomConfiguration.CreateDefault());
        }

        public IReadOnlyList<RoomTrapSpawn> CreateSpawns(RoomType roomType, int runSeed, Vector2Int roomPosition)
        {
            if (roomType != RoomType.Trap || configuration.MaxTrapsPerRoom == 0)
            {
                return Array.Empty<RoomTrapSpawn>();
            }

            int count = GetStableRange(
                runSeed,
                roomPosition,
                CountSalt,
                configuration.MinTrapsPerRoom,
                configuration.MaxTrapsPerRoom);
            List<Vector2> spikePositions = CreateShuffledList(SpikeAnchors, runSeed, roomPosition, SpikePositionSalt);
            List<WallAnchor> wallPositions = CreateShuffledList(WallAnchors, runSeed, roomPosition, WallPositionSalt);
            List<RoomTrapSpawn> spawns = new List<RoomTrapSpawn>(count);
            int spikeIndex = 0;
            int wallIndex = 0;

            for (int i = 0; i < count; i++)
            {
                // Each slot gets its own stable roll so rooms vary while a run seed remains reproducible.
                bool createsWallTrap = GetStableChance(runSeed, roomPosition, KindSalt ^ i) <
                    configuration.WallProjectileChance;
                if (!createsWallTrap)
                {
                    spawns.Add(new RoomTrapSpawn(TrapKind.Spike, spikePositions[spikeIndex++], Vector2.zero, 0f));
                    continue;
                }

                WallAnchor anchor = wallPositions[wallIndex++];
                float phaseOffset = GetStableChance(runSeed, roomPosition, PhaseSalt ^ i) * MaximumPhaseOffsetSeconds;
                spawns.Add(new RoomTrapSpawn(
                    TrapKind.WallProjectile,
                    anchor.Position,
                    anchor.FireDirection,
                    phaseOffset));
            }

            return spawns;
        }

        private static List<T> CreateShuffledList<T>(IReadOnlyList<T> source, int runSeed, Vector2Int roomPosition, int salt)
        {
            List<T> values = new List<T>(source);
            System.Random random = new System.Random(CreateStableSeed(runSeed, roomPosition, salt));
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }

            return values;
        }

        private static int GetStableRange(
            int runSeed,
            Vector2Int roomPosition,
            int salt,
            int minInclusive,
            int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }

            return new System.Random(CreateStableSeed(runSeed, roomPosition, salt)).Next(minInclusive, maxInclusive + 1);
        }

        private static float GetStableChance(int runSeed, Vector2Int roomPosition, int salt)
        {
            return (float)new System.Random(CreateStableSeed(runSeed, roomPosition, salt)).NextDouble();
        }

        private static int CreateStableSeed(int runSeed, Vector2Int roomPosition, int salt)
        {
            unchecked
            {
                int hash = runSeed;
                hash = (hash * 397) ^ roomPosition.x;
                hash = (hash * 397) ^ roomPosition.y;
                return (hash * 397) ^ salt;
            }
        }

        private readonly struct WallAnchor
        {
            public WallAnchor(Vector2 position, Vector2 fireDirection)
            {
                Position = position;
                FireDirection = fireDirection;
            }

            public Vector2 Position { get; }
            public Vector2 FireDirection { get; }
        }
    }

    public readonly struct RoomTrapSpawn
    {
        public RoomTrapSpawn(TrapKind kind, Vector2 position, Vector2 fireDirection, float phaseOffsetSeconds)
        {
            Kind = kind;
            Position = position;
            FireDirection = fireDirection;
            PhaseOffsetSeconds = phaseOffsetSeconds;
        }

        public TrapKind Kind { get; }
        public Vector2 Position { get; }
        public Vector2 FireDirection { get; }
        public float PhaseOffsetSeconds { get; }
    }
}
