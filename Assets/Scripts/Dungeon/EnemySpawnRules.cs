using System.Collections.Generic;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public sealed class EnemySpawnRules
    {
        private const int EnemyCountSalt = 0x454E4D59;
        private const int TrapCountSalt = 0x54524150;
        private const int PositionSalt = 0x504F5345;
        private const int KindSalt = 0x4B494E44;

        private static readonly Vector2[] SpawnPositions =
        {
            new Vector2(-5.7f, 2.6f),
            new Vector2(5.7f, 2.6f),
            new Vector2(-5.7f, -2.6f),
            new Vector2(5.7f, -2.6f),
            new Vector2(-2.2f, 0f),
            new Vector2(2.2f, 0f)
        };

        public EnemySpawnRules(int enemyRoomMin, int enemyRoomMax, int trapRoomMin, int trapRoomMax)
        {
            EnemyRoomMin = Mathf.Max(0, enemyRoomMin);
            EnemyRoomMax = Mathf.Max(EnemyRoomMin, enemyRoomMax);
            TrapRoomMin = Mathf.Max(0, trapRoomMin);
            TrapRoomMax = Mathf.Max(TrapRoomMin, trapRoomMax);
        }

        public int EnemyRoomMin { get; }
        public int EnemyRoomMax { get; }
        public int TrapRoomMin { get; }
        public int TrapRoomMax { get; }

        public static EnemySpawnRules CreateDefault()
        {
            return new EnemySpawnRules(3, 5, 1, 2);
        }

        public IReadOnlyList<RoomEnemySpawn> CreateSpawns(RoomType roomType, int runSeed, Vector2Int roomPosition)
        {
            int count = GetEnemyCount(roomType, runSeed, roomPosition);
            List<Vector2> positions = GetShuffledPositions(count, runSeed, roomPosition);
            List<EnemyKind> kinds = GetEnemyKinds(count, runSeed, roomPosition);
            List<RoomEnemySpawn> spawns = new List<RoomEnemySpawn>(count);

            for (int i = 0; i < count; i++)
            {
                spawns.Add(new RoomEnemySpawn(kinds[i], positions[i]));
            }

            return spawns;
        }

        public int GetEnemyCount(RoomType roomType, int runSeed, Vector2Int roomPosition)
        {
            if (roomType == RoomType.Enemy)
            {
                return GetStableRange(runSeed, roomPosition, EnemyCountSalt, EnemyRoomMin, EnemyRoomMax);
            }

            if (roomType == RoomType.Trap)
            {
                return GetStableRange(runSeed, roomPosition, TrapCountSalt, TrapRoomMin, TrapRoomMax);
            }

            return 0;
        }

        private static List<Vector2> GetShuffledPositions(int count, int runSeed, Vector2Int roomPosition)
        {
            List<Vector2> positions = new List<Vector2>(SpawnPositions);
            System.Random random = new System.Random(CreateStableSeed(runSeed, roomPosition, PositionSalt));
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (positions[i], positions[swapIndex]) = (positions[swapIndex], positions[i]);
            }

            return positions.GetRange(0, Mathf.Min(count, positions.Count));
        }

        private static List<EnemyKind> GetEnemyKinds(int count, int runSeed, Vector2Int roomPosition)
        {
            List<EnemyKind> kinds = new List<EnemyKind>(count);
            System.Random random = new System.Random(CreateStableSeed(runSeed, roomPosition, KindSalt));
            for (int i = 0; i < count; i++)
            {
                kinds.Add(random.Next(2) == 0 ? EnemyKind.Zombie : EnemyKind.Spider);
            }

            // When a room has enough enemies, force the first two slots to show both enemy types.
            if (count >= 2)
            {
                kinds[0] = EnemyKind.Zombie;
                kinds[1] = EnemyKind.Spider;
            }

            return kinds;
        }

        private static int GetStableRange(int runSeed, Vector2Int roomPosition, int salt, int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }

            return new System.Random(CreateStableSeed(runSeed, roomPosition, salt)).Next(minInclusive, maxInclusive + 1);
        }

        private static int CreateStableSeed(int runSeed, Vector2Int roomPosition, int salt)
        {
            unchecked
            {
                int hash = runSeed;
                hash = (hash * 397) ^ roomPosition.x;
                hash = (hash * 397) ^ roomPosition.y;
                hash = (hash * 397) ^ salt;
                return hash;
            }
        }
    }

    public readonly struct RoomEnemySpawn
    {
        public RoomEnemySpawn(EnemyKind kind, Vector2 position)
        {
            Kind = kind;
            Position = position;
        }

        public EnemyKind Kind { get; }
        public Vector2 Position { get; }
    }
}
