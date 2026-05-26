using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptKnight.Data
{
    public enum GameRunStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed,
        Quit
    }

    [Serializable]
    public sealed class CollectedItemStack
    {
        public CollectedItemStack(string itemId, string displayName, int quantity)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Quantity = quantity;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public int Quantity { get; private set; }

        public void Add(int amount)
        {
            Quantity += Math.Max(0, amount);
        }
    }

    [Serializable]
    public sealed class GameRunState
    {
        private readonly List<CollectedItemStack> collectedItems = new List<CollectedItemStack>();

        public GameRunStatus Status { get; private set; }
        public int RunNumber { get; private set; }
        public int Seed { get; private set; }
        public int DungeonWidth { get; private set; }
        public int DungeonHeight { get; private set; }
        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }
        public int KeyCount { get; private set; }
        public DateTime StartedAt { get; private set; }

        public bool IsActive => Status == GameRunStatus.Active;
        public IReadOnlyList<CollectedItemStack> CollectedItems => collectedItems;

        public static GameRunState CreateNewRun(int runNumber, int seed, int dungeonWidth, int dungeonHeight, int maxHealth)
        {
            return new GameRunState
            {
                Status = GameRunStatus.Active,
                RunNumber = runNumber,
                Seed = seed,
                DungeonWidth = dungeonWidth,
                DungeonHeight = dungeonHeight,
                MaxHealth = maxHealth,
                CurrentHealth = maxHealth,
                KeyCount = 0,
                StartedAt = DateTime.UtcNow
            };
        }

        public void QuitRun()
        {
            if (!IsActive)
            {
                return;
            }

            Status = GameRunStatus.Quit;
        }

        public void ApplyDamage(int halfHeartDamage)
        {
            if (!IsActive || halfHeartDamage <= 0)
            {
                return;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - halfHeartDamage);
            if (CurrentHealth == 0)
            {
                Status = GameRunStatus.Failed;
            }
        }

        public void Heal(int halfHeartAmount)
        {
            if (!IsActive || halfHeartAmount <= 0)
            {
                return;
            }

            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + halfHeartAmount);
        }

        public void AddKeys(int amount)
        {
            KeyCount += Math.Max(0, amount);
        }

        public bool SpendKey()
        {
            if (KeyCount <= 0)
            {
                return false;
            }

            KeyCount--;
            return true;
        }

        public void AddCollectedItem(string itemId, string displayName, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return;
            }

            string safeDisplayName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
            CollectedItemStack existingStack = collectedItems.FirstOrDefault(item => item.ItemId == itemId);
            if (existingStack != null)
            {
                existingStack.Add(quantity);
                return;
            }

            collectedItems.Add(new CollectedItemStack(itemId, safeDisplayName, quantity));
        }
    }
}
