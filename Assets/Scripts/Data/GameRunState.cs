using System;

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
    public sealed class GameRunState
    {
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
    }
}
