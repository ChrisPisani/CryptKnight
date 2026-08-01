using System;
using System.Collections.Generic;
using CryptKnight.Enemies;

namespace CryptKnight.Dungeon
{
    public enum FinalEncounterComposition
    {
        SingleKind,
        Mixed
    }

    public sealed class FinalEncounterConfiguration
    {
        private readonly int[] waveEnemyCounts;

        public FinalEncounterConfiguration(
            IReadOnlyList<int> enemyCounts,
            float intermissionSeconds,
            EnemyKind enemyKind,
            int enemyMaxHealth,
            EnemyDifficulty difficulty = EnemyDifficulty.Normal,
            FinalEncounterComposition composition = FinalEncounterComposition.SingleKind)
        {
            if (enemyCounts == null || enemyCounts.Count == 0)
            {
                throw new ArgumentException("A final encounter needs at least one wave.", nameof(enemyCounts));
            }

            waveEnemyCounts = new int[enemyCounts.Count];
            for (int i = 0; i < enemyCounts.Count; i++)
            {
                waveEnemyCounts[i] = Math.Max(1, enemyCounts[i]);
            }

            IntermissionSeconds = Math.Max(0f, intermissionSeconds);
            EnemyKind = enemyKind;
            EnemyMaxHealth = Math.Max(1, enemyMaxHealth);
            Difficulty = difficulty;
            Composition = composition;
        }

        public IReadOnlyList<int> WaveEnemyCounts => waveEnemyCounts;
        public int WaveCount => waveEnemyCounts.Length;
        public float IntermissionSeconds { get; }
        public EnemyKind EnemyKind { get; }
        public int EnemyMaxHealth { get; }
        public EnemyDifficulty Difficulty { get; }
        public FinalEncounterComposition Composition { get; }

        public static FinalEncounterConfiguration CreateDefault()
        {
            return CreateDefault(EnemyDifficulty.Normal);
        }

        public static FinalEncounterConfiguration CreateDefault(EnemyDifficulty difficulty)
        {
            EnemyDifficultyProfile zombieProfile = EnemyDifficultyProfile.Get(EnemyKind.Zombie, difficulty);
            return new FinalEncounterConfiguration(
                new[] { 4, 6, 8 },
                2f,
                EnemyKind.Zombie,
                zombieProfile.MaxHealth,
                difficulty,
                difficulty == EnemyDifficulty.Hard
                    ? FinalEncounterComposition.Mixed
                    : FinalEncounterComposition.SingleKind);
        }

        public int GetEnemyCount(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waveEnemyCounts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(waveIndex));
            }

            return waveEnemyCounts[waveIndex];
        }

        public int GetEnemyMaxHealth(EnemyKind kind)
        {
            return Composition == FinalEncounterComposition.Mixed
                ? EnemyDifficultyProfile.Get(kind, Difficulty).MaxHealth
                : EnemyMaxHealth;
        }
    }
}
