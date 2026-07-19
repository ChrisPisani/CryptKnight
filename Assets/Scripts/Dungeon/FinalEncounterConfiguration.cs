using System;
using System.Collections.Generic;
using CryptKnight.Enemies;

namespace CryptKnight.Dungeon
{
    public sealed class FinalEncounterConfiguration
    {
        private readonly int[] waveEnemyCounts;

        public FinalEncounterConfiguration(
            IReadOnlyList<int> enemyCounts,
            float intermissionSeconds,
            EnemyKind enemyKind,
            int enemyMaxHealth)
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
        }

        public IReadOnlyList<int> WaveEnemyCounts => waveEnemyCounts;
        public int WaveCount => waveEnemyCounts.Length;
        public float IntermissionSeconds { get; }
        public EnemyKind EnemyKind { get; }
        public int EnemyMaxHealth { get; }

        public static FinalEncounterConfiguration CreateDefault()
        {
            return new FinalEncounterConfiguration(
                new[] { 4, 6, 8 },
                2f,
                EnemyKind.Zombie,
                5);
        }

        public int GetEnemyCount(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waveEnemyCounts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(waveIndex));
            }

            return waveEnemyCounts[waveIndex];
        }
    }
}
