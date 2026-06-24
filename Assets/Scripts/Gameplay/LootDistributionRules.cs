using System;
using CryptKnight.Dungeon;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    // placement rules keep generated loot stable when a room is rebuilt later.
    public sealed class LootDistributionRules
    {
        private const int ChestRollSalt = 0x43484553;
        private const int ChestPositionSalt = 0x504F5343;
        private const int KeyRollSalt = 0x4B455953;
        private const int KeyPositionSalt = 0x504F534B;
        private static readonly Vector2[] ChestSpawnPositions =
        {
            new Vector2(-4.4f, -2.15f),
            new Vector2(4.4f, -2.15f),
            new Vector2(-4.4f, 1.85f),
            new Vector2(4.4f, 1.85f),
            new Vector2(0f, -2.35f)
        };
        private static readonly Vector2[] KeySpawnPositions =
        {
            new Vector2(-5.0f, 2.25f),
            new Vector2(5.0f, 2.25f),
            new Vector2(-5.0f, -1.95f),
            new Vector2(5.0f, -1.95f),
            new Vector2(0f, 2.35f)
        };

        public LootDistributionRules(float chestSpawnChance, float roomLootChance, float keySpawnChance = 0.10f)
        {
            ChestSpawnChance = ClampChance(chestSpawnChance);
            RoomLootChance = ClampChance(roomLootChance);
            KeySpawnChance = ClampChance(keySpawnChance);
        }

        public float ChestSpawnChance { get; }
        public float RoomLootChance { get; }
        public float KeySpawnChance { get; }

        public static LootDistributionRules CreateDefault()
        {
            return new LootDistributionRules(0.15f, 0f, 0.10f);
        }

        public bool CanRoomContainChest(RoomType roomType)
        {
            return roomType != RoomType.Starter && roomType != RoomType.Final;
        }

        public bool ShouldPlaceChest(RoomType roomType, int runSeed, Vector2Int roomPosition)
        {
            if (!CanRoomContainChest(roomType))
            {
                return false;
            }

            return GetStableChance(runSeed, roomPosition, ChestRollSalt) < ChestSpawnChance;
        }

        public bool ShouldPlaceKey(RoomType roomType, int runSeed, Vector2Int roomPosition)
        {
            if (!CanRoomContainGeneratedLoot(roomType))
            {
                return false;
            }

            return GetStableChance(runSeed, roomPosition, KeyRollSalt) < KeySpawnChance;
        }

        public Vector2 GetChestSpawnPosition(int runSeed, Vector2Int roomPosition)
        {
            int index = GetStableIndex(runSeed, roomPosition, ChestPositionSalt, ChestSpawnPositions.Length);
            return ChestSpawnPositions[index];
        }

        public Vector2 GetKeySpawnPosition(int runSeed, Vector2Int roomPosition)
        {
            int index = GetStableIndex(runSeed, roomPosition, KeyPositionSalt, KeySpawnPositions.Length);
            return KeySpawnPositions[index];
        }

        public bool ShouldPlaceLooseRoomLoot(RoomType roomType, int runSeed, Vector2Int roomPosition)
        {
            if (!CanRoomContainGeneratedLoot(roomType))
            {
                return false;
            }

            return GetStableChance(runSeed, roomPosition, ChestPositionSalt ^ ChestRollSalt) < RoomLootChance;
        }

        private static bool CanRoomContainGeneratedLoot(RoomType roomType)
        {
            return roomType != RoomType.Starter && roomType != RoomType.Final;
        }

        public static float ClampChance(float chance)
        {
            return Math.Max(0f, Math.Min(1f, chance));
        }

        private static float GetStableChance(int runSeed, Vector2Int roomPosition, int salt)
        {
            return (float)new System.Random(CreateStableSeed(runSeed, roomPosition, salt)).NextDouble();
        }

        private static int GetStableIndex(int runSeed, Vector2Int roomPosition, int salt, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return new System.Random(CreateStableSeed(runSeed, roomPosition, salt)).Next(count);
        }

        private static int CreateStableSeed(int runSeed, Vector2Int roomPosition, int salt)
        {
            unchecked
            {
                // Avoid runtime hash helpers here so the same run seed always produces the same room rolls.
                int hash = runSeed;
                hash = (hash * 397) ^ roomPosition.x;
                hash = (hash * 397) ^ roomPosition.y;
                hash = (hash * 397) ^ salt;
                return hash;
            }
        }
    }
}
