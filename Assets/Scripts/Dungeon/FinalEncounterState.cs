namespace CryptKnight.Dungeon
{
    public enum FinalEncounterStatus
    {
        NotStarted,
        Intermission,
        WaveActive,
        Completed
    }

    public sealed class FinalEncounterState
    {
        private readonly FinalEncounterConfiguration configuration;

        public FinalEncounterState(FinalEncounterConfiguration encounterConfiguration)
        {
            configuration = encounterConfiguration ?? throw new System.ArgumentNullException(nameof(encounterConfiguration));
            CurrentWaveIndex = -1;
            Status = FinalEncounterStatus.NotStarted;
        }

        public FinalEncounterStatus Status { get; private set; }
        public int CurrentWaveIndex { get; private set; }
        public int CurrentWaveNumber => CurrentWaveIndex + 1;
        public int TotalWaves => configuration.WaveCount;
        public int RemainingEnemies { get; private set; }
        public bool IsComplete => Status == FinalEncounterStatus.Completed;

        public bool BeginNextIntermission()
        {
            if (Status != FinalEncounterStatus.NotStarted || CurrentWaveIndex + 1 >= TotalWaves)
            {
                return false;
            }

            CurrentWaveIndex++;
            Status = FinalEncounterStatus.Intermission;
            return true;
        }

        public int StartCurrentWave()
        {
            if (Status != FinalEncounterStatus.Intermission)
            {
                return 0;
            }

            RemainingEnemies = configuration.GetEnemyCount(CurrentWaveIndex);
            Status = FinalEncounterStatus.WaveActive;
            return RemainingEnemies;
        }

        public bool RecordEnemyDefeated()
        {
            if (Status != FinalEncounterStatus.WaveActive || RemainingEnemies <= 0)
            {
                return false;
            }

            RemainingEnemies--;
            if (RemainingEnemies > 0)
            {
                return false;
            }

            Status = CurrentWaveIndex == TotalWaves - 1
                ? FinalEncounterStatus.Completed
                : FinalEncounterStatus.NotStarted;
            return true;
        }
    }
}
