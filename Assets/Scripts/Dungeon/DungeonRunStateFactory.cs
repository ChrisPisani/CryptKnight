using System.Collections.Generic;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public static class DungeonRunStateFactory
    {
        private const string KeyItemId = "key";
        private const int ZombieMaxHealth = 5;
        private const int SpiderMaxHealth = 3;
        private static readonly Vector2 StarterGiftKeyPosition = new Vector2(-1.15f, 1.15f);
        private static readonly Vector2 StarterGiftChestPosition = new Vector2(1.15f, 1.15f);

        public static DungeonRunState Create(int width, int height, int runSeed)
        {
            LootTableConfiguration lootConfiguration = LootTableConfiguration.CreateDefault();
            LootDistributionRules lootRules = LootDistributionRules.CreateDefault();
            EnemySpawnRules enemyRules = EnemySpawnRules.CreateDefault();
            FinalEncounterConfiguration finalEncounterConfiguration = FinalEncounterConfiguration.CreateDefault();
            DungeonLayout layout = DungeonLayoutGenerator.Generate(width, height, runSeed);
            Dictionary<Vector2Int, DungeonRoomRuntimeState> roomStates = new Dictionary<Vector2Int, DungeonRoomRuntimeState>();

            foreach (DungeonRoom room in layout.Rooms)
            {
                roomStates[room.GridPosition] = CreateRoomState(
                    room,
                    runSeed,
                    lootConfiguration,
                    lootRules,
                    enemyRules,
                    finalEncounterConfiguration);
            }

            return new DungeonRunState(layout, roomStates, lootConfiguration, runSeed, finalEncounterConfiguration);
        }

        private static DungeonRoomRuntimeState CreateRoomState(
            DungeonRoom room,
            int runSeed,
            LootTableConfiguration lootConfiguration,
            LootDistributionRules lootRules,
            EnemySpawnRules enemyRules,
            FinalEncounterConfiguration finalEncounterConfiguration)
        {
            DungeonRoomRuntimeState state = new DungeonRoomRuntimeState(room.GridPosition, room.RoomType);
            if (room.RoomType == RoomType.Final)
            {
                state.InitializeFinalEncounter(finalEncounterConfiguration);
            }

            IReadOnlyList<RoomEnemySpawn> enemySpawns = enemyRules.CreateSpawns(room.RoomType, runSeed, room.GridPosition);
            for (int i = 0; i < enemySpawns.Count; i++)
            {
                RoomEnemySpawn spawn = enemySpawns[i];
                state.AddEnemy(spawn.Kind, spawn.Position, GetEnemyMaxHealth(spawn.Kind));
            }

            if (room.RoomType == RoomType.Starter)
            {
                AddStarterGift(state, lootConfiguration, runSeed);
            }

            if (lootRules.ShouldPlaceChest(room.RoomType, runSeed, room.GridPosition))
            {
                AddChest(state, lootRules.GetChestSpawnPosition(runSeed, room.GridPosition), runSeed);
            }

            if (lootRules.ShouldPlaceKey(room.RoomType, runSeed, room.GridPosition))
            {
                LootItemDefinition keyItem = GetKeyItemDefinition(lootConfiguration);
                if (keyItem != null)
                {
                    state.AddLoot(keyItem, lootRules.GetKeySpawnPosition(runSeed, room.GridPosition));
                }
            }

            state.MarkContentsInitialized();
            return state;
        }

        private static void AddStarterGift(DungeonRoomRuntimeState state, LootTableConfiguration configuration, int runSeed)
        {
            LootItemDefinition keyItem = GetKeyItemDefinition(configuration);
            if (keyItem != null)
            {
                state.AddLoot(keyItem, StarterGiftKeyPosition);
            }

            AddChest(state, StarterGiftChestPosition, runSeed);
        }

        private static void AddChest(DungeonRoomRuntimeState state, Vector2 position, int runSeed)
        {
            int rewardSeed = CreateChestRewardSeed(runSeed, state.GridPosition, state.Chests.Count);
            state.AddChest(position, rewardSeed);
        }

        private static LootItemDefinition GetKeyItemDefinition(LootTableConfiguration configuration)
        {
            for (int i = 0; i < configuration.Items.Count; i++)
            {
                if (configuration.Items[i].ItemId == KeyItemId)
                {
                    return configuration.Items[i];
                }
            }

            return null;
        }

        private static int GetEnemyMaxHealth(EnemyKind enemyKind)
        {
            return enemyKind == EnemyKind.Zombie ? ZombieMaxHealth : SpiderMaxHealth;
        }

        private static int CreateChestRewardSeed(int runSeed, Vector2Int roomPosition, int chestIndex)
        {
            unchecked
            {
                int hash = runSeed;
                hash = (hash * 397) ^ roomPosition.x;
                hash = (hash * 397) ^ roomPosition.y;
                hash = (hash * 397) ^ chestIndex;
                return hash;
            }
        }
    }
}
