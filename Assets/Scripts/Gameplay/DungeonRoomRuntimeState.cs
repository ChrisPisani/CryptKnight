using System.Collections.Generic;
using CryptKnight.Dungeon;
using CryptKnight.Loot;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    // Lightweight room memory that saves game states of a dungeon room after leaving
    public sealed class DungeonRoomRuntimeState
    {
        private readonly List<RoomLootInstance> loot = new List<RoomLootInstance>();
        private readonly List<RoomChestInstance> chests = new List<RoomChestInstance>();
        private int nextLootId;
        private int nextChestId;

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
        // starter and final rooms are not locked for now
        public bool IsLocked => RoomType != RoomType.Starter && RoomType != RoomType.Final && RemainingEnemies > 0;
        public IReadOnlyList<RoomLootInstance> Loot => loot;
        public IReadOnlyList<RoomChestInstance> Chests => chests;

        public void MarkContentsInitialized()
        {
            ContentsInitialized = true;
        }

        public void SetEnemyCount(int enemyCount)
        {
            TotalEnemies = Mathf.Max(0, enemyCount);
            DefeatedEnemies = 0;
            RemainingEnemies = TotalEnemies;
            IsCleared = TotalEnemies == 0 ? IsCleared : false;
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

        public RoomChestInstance AddChest(Vector2 position)
        {
            RoomChestInstance instance = new RoomChestInstance(nextChestId++, position);
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
        public RoomChestInstance(int id, Vector2 position)
        {
            Id = id;
            Position = position;
        }

        public int Id { get; }
        public Vector2 Position { get; }
        public bool IsOpened { get; private set; }

        public void MarkOpened()
        {
            IsOpened = true;
        }
    }
}
