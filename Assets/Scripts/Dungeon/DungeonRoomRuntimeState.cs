using System.Collections.Generic;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    // Lightweight room memory that saves game states of a dungeon room after leaving
    public sealed class DungeonRoomRuntimeState
    {
        private readonly List<RoomLootInstance> loot = new List<RoomLootInstance>();
        private readonly List<RoomChestInstance> chests = new List<RoomChestInstance>();
        private readonly List<RoomEnemyInstance> enemies = new List<RoomEnemyInstance>();
        private int nextLootId;
        private int nextChestId;
        private int nextEnemyId;

        public DungeonRoomRuntimeState(Vector2Int gridPosition, RoomType roomType)
        {
            GridPosition = gridPosition;
            RoomType = roomType;
        }

        public Vector2Int GridPosition { get; }
        public RoomType RoomType { get; }
        public bool ContentsInitialized { get; private set; }
        public int TotalEnemies { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int RemainingEnemies { get; private set; }
        public bool IsCleared { get; private set; }
        // The final room stays sealed through intermissions, when no enemies are present.
        public bool IsLocked => RoomType == RoomType.Final || (RoomType != RoomType.Starter && RemainingEnemies > 0);
        public FinalEncounterState FinalEncounter { get; private set; }
        public IReadOnlyList<RoomLootInstance> Loot => loot;
        public IReadOnlyList<RoomChestInstance> Chests => chests;
        public IReadOnlyList<RoomEnemyInstance> Enemies => enemies;

        public void InitializeFinalEncounter(FinalEncounterConfiguration configuration)
        {
            if (RoomType != RoomType.Final || FinalEncounter != null)
            {
                return;
            }

            FinalEncounter = new FinalEncounterState(configuration);
        }

        public void MarkContentsInitialized()
        {
            ContentsInitialized = true;
        }

        public void SetEnemyCount(int enemyCount)
        {
            enemies.Clear();
            nextEnemyId = 0;
            TotalEnemies = Mathf.Max(0, enemyCount);
            DefeatedEnemies = 0;
            RemainingEnemies = TotalEnemies;
            IsCleared = TotalEnemies == 0 ? IsCleared : false;
        }

        public RoomEnemyInstance AddEnemy(EnemyKind kind, Vector2 position, int maximumHealth = 1)
        {
            RoomEnemyInstance instance = new RoomEnemyInstance(nextEnemyId++, kind, position, maximumHealth);
            enemies.Add(instance);
            TotalEnemies++;
            RemainingEnemies++;
            IsCleared = false;
            return instance;
        }

        public bool MarkEnemyDefeated(int enemyId)
        {
            RoomEnemyInstance instance = FindEnemy(enemyId);
            if (instance == null || instance.IsDefeated)
            {
                return false;
            }

            instance.MarkDefeated();
            return DefeatEnemy();
        }

        public bool DefeatEnemy()
        {
            if (RemainingEnemies <= 0)
            {
                return false;
            }

            RemainingEnemies--;
            DefeatedEnemies++;
            // Return true when final enemy defeated
            if (RemainingEnemies == 0 && TotalEnemies > 0 && !IsCleared)
            {
                IsCleared = true;
                return true;
            }

            return false;
        }

        public RoomLootInstance AddLoot(LootItemDefinition itemDefinition, Vector2 position)
        {
            RoomLootInstance instance = new RoomLootInstance(nextLootId++, itemDefinition, position);
            loot.Add(instance);
            return instance;
        }

        public bool MarkLootCollected(int lootId)
        {
            RoomLootInstance instance = FindLoot(lootId);
            if (instance == null || instance.IsCollected)
            {
                return false;
            }

            instance.MarkCollected();
            return true;
        }

        public RoomChestInstance AddChest(Vector2 position, int rewardSeed = 0)
        {
            RoomChestInstance instance = new RoomChestInstance(nextChestId++, position, rewardSeed);
            chests.Add(instance);
            return instance;
        }

        public bool MarkChestOpened(int chestId)
        {
            RoomChestInstance instance = FindChest(chestId);
            if (instance == null || instance.IsOpened)
            {
                return false;
            }

            instance.MarkOpened();
            return true;
        }

        private RoomLootInstance FindLoot(int lootId)
        {
            for (int i = 0; i < loot.Count; i++)
            {
                if (loot[i].Id == lootId)
                {
                    return loot[i];
                }
            }

            return null;
        }

        private RoomChestInstance FindChest(int chestId)
        {
            for (int i = 0; i < chests.Count; i++)
            {
                if (chests[i].Id == chestId)
                {
                    return chests[i];
                }
            }

            return null;
        }

        private RoomEnemyInstance FindEnemy(int enemyId)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Id == enemyId)
                {
                    return enemies[i];
                }
            }

            return null;
        }
    }

    public sealed class RoomLootInstance
    {
        public RoomLootInstance(int id, LootItemDefinition itemDefinition, Vector2 position)
        {
            Id = id;
            ItemDefinition = itemDefinition;
            Position = position;
        }

        public int Id { get; }
        public LootItemDefinition ItemDefinition { get; }
        public Vector2 Position { get; }
        public bool IsCollected { get; private set; }

        public void MarkCollected()
        {
            IsCollected = true;
        }
    }

    public sealed class RoomChestInstance
    {
        public RoomChestInstance(int id, Vector2 position, int rewardSeed)
        {
            Id = id;
            Position = position;
            RewardSeed = rewardSeed;
        }

        public int Id { get; }
        public Vector2 Position { get; }
        public int RewardSeed { get; }
        public bool IsOpened { get; private set; }

        public void MarkOpened()
        {
            IsOpened = true;
        }
    }

    public sealed class RoomEnemyInstance
    {
        public RoomEnemyInstance(int id, EnemyKind kind, Vector2 position, int maximumHealth)
        {
            Id = id;
            Kind = kind;
            Position = position;
            MaxHealth = Mathf.Max(1, maximumHealth);
            CurrentHealth = MaxHealth;
        }

        public int Id { get; }
        public EnemyKind Kind { get; }
        public Vector2 Position { get; private set; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsDefeated { get; private set; }

        public void UpdateRuntime(Vector2 position, int currentHealth)
        {
            if (IsDefeated)
            {
                return;
            }

            Position = position;
            CurrentHealth = Mathf.Clamp(currentHealth, 1, MaxHealth);
        }

        public void MarkDefeated()
        {
            CurrentHealth = 0;
            IsDefeated = true;
        }
    }
}
