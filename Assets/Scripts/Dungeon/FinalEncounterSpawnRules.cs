using System.Collections.Generic;
using CryptKnight.Enemies;
using UnityEngine;

namespace CryptKnight.Dungeon
{
    public sealed class FinalEncounterSpawnRules
    {
        private const int WaveSalt = 0x57415645;

        private static readonly Vector2[] ArenaPositions =
        {
            new Vector2(-5.7f, 2.8f),
            new Vector2(0f, 3.0f),
            new Vector2(5.7f, 2.8f),
            new Vector2(-6.2f, 0f),
            new Vector2(6.2f, 0f),
            new Vector2(-5.7f, -2.8f),
            new Vector2(0f, -3.0f),
            new Vector2(5.7f, -2.8f)
        };

        public IReadOnlyList<RoomEnemySpawn> CreateWave(
            FinalEncounterConfiguration configuration,
            int waveIndex,
            int runSeed,
            Vector2Int roomPosition)
        {
            int count = configuration.GetEnemyCount(waveIndex);
            List<Vector2> positions = new List<Vector2>(ArenaPositions);
            System.Random random = new System.Random(CreateStableSeed(runSeed, roomPosition, waveIndex));
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (positions[i], positions[swapIndex]) = (positions[swapIndex], positions[i]);
            }

            List<RoomEnemySpawn> spawns = new List<RoomEnemySpawn>(count);
            List<EnemyKind> kinds = CreateEnemyKinds(configuration, count, random);
            for (int i = 0; i < count; i++)
            {
                spawns.Add(new RoomEnemySpawn(kinds[i], positions[i]));
            }

            return spawns;
        }

        private static List<EnemyKind> CreateEnemyKinds(
            FinalEncounterConfiguration configuration,
            int count,
            System.Random random)
        {
            List<EnemyKind> kinds = new List<EnemyKind>(count);
            if (configuration.Composition == FinalEncounterComposition.SingleKind)
            {
                for (int i = 0; i < count; i++)
                {
                    kinds.Add(configuration.EnemyKind);
                }

                return kinds;
            }

            kinds.Add(EnemyKind.Zombie);
            if (count > 1)
            {
                kinds.Add(EnemyKind.Spider);
            }

            while (kinds.Count < count)
            {
                kinds.Add(random.Next(2) == 0 ? EnemyKind.Zombie : EnemyKind.Spider);
            }

            for (int i = kinds.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (kinds[i], kinds[swapIndex]) = (kinds[swapIndex], kinds[i]);
            }

            return kinds;
        }

        private static int CreateStableSeed(int runSeed, Vector2Int roomPosition, int waveIndex)
        {
            unchecked
            {
                int hash = runSeed;
                hash = (hash * 397) ^ roomPosition.x;
                hash = (hash * 397) ^ roomPosition.y;
                hash = (hash * 397) ^ waveIndex;
                return hash ^ WaveSalt;
            }
        }
    }
}
