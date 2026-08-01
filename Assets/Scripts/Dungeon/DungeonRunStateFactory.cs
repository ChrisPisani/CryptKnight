using System.Collections.Generic;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using CryptKnight.Traps;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public static class DungeonRunStateFactory
    {
        private const string KeyItemId = "key";
        private const int StarterGiftSalt = 0x53544746;
        private static readonly Vector2 StarterGiftPosition = new Vector2(-1.15f, 1.15f);
        private static readonly string[] StarterGiftItemIds =
        {
            "heart_container",
            "damage_up",
            "speed_up",
            "attack_rate_up"
        };

        public static DungeonRunState Create(
            int width,
            int height,
            int runSeed,
            int floorNumber = 1,
            EnemyDifficulty difficulty = EnemyDifficulty.Normal,
            bool includeStarterGift = true)
        {
            LootTableConfiguration lootConfiguration = LootTableConfiguration.CreateDefault();
            LootDistributionRules lootRules = LootDistributionRules.CreateDefault();
            EnemySpawnRules enemyRules = EnemySpawnRules.CreateDefault();
            FinalEncounterConfiguration finalEncounterConfiguration = FinalEncounterConfiguration.CreateDefault(difficulty);
            TrapRoomConfiguration trapConfiguration = TrapRoomConfiguration.CreateDefault();
            TrapGenerationRules trapRules = new TrapGenerationRules(trapConfiguration);
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
                    finalEncounterConfiguration,
                    trapRules,
                    difficulty,
                    includeStarterGift);
            }

            return new DungeonRunState(
                layout,
                roomStates,
                lootConfiguration,
                runSeed,
                finalEncounterConfiguration,
                trapConfiguration,
                floorNumber,
                difficulty);
        }

        private static DungeonRoomRuntimeState CreateRoomState(
            DungeonRoom room,
            int runSeed,
            LootTableConfiguration lootConfiguration,
            LootDistributionRules lootRules,
            EnemySpawnRules enemyRules,
            FinalEncounterConfiguration finalEncounterConfiguration,
            TrapGenerationRules trapRules,
            EnemyDifficulty difficulty,
            bool includeStarterGift)
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
                state.AddEnemy(
                    spawn.Kind,
                    spawn.Position,
                    EnemyDifficultyProfile.Get(spawn.Kind, difficulty).MaxHealth,
                    difficulty);
            }

            IReadOnlyList<RoomTrapSpawn> trapSpawns = trapRules.CreateSpawns(room.RoomType, runSeed, room.GridPosition);
            for (int i = 0; i < trapSpawns.Count; i++)
            {
                RoomTrapSpawn spawn = trapSpawns[i];
                state.AddTrap(spawn.Kind, spawn.Position, spawn.FireDirection, spawn.PhaseOffsetSeconds);
            }

            if (room.RoomType == RoomType.Starter && includeStarterGift)
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
            List<LootItemDefinition> commonItems = new List<LootItemDefinition>();
            for (int giftIndex = 0; giftIndex < StarterGiftItemIds.Length; giftIndex++)
            {
                for (int itemIndex = 0; itemIndex < configuration.Items.Count; itemIndex++)
                {
                    LootItemDefinition item = configuration.Items[itemIndex];
                    if (item.ItemId == StarterGiftItemIds[giftIndex]
                        && item.Rarity == LootRarity.Common
                        && item.KeyAmount == 0)
                    {
                        commonItems.Add(item);
                        break;
                    }
                }
            }

            if (commonItems.Count == 0)
            {
                return;
            }

            // Seed the gift independently so changing room generation does not change the starter reward.
            System.Random random = new System.Random(runSeed ^ StarterGiftSalt);
            state.AddLoot(commonItems[random.Next(commonItems.Count)], StarterGiftPosition);
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
