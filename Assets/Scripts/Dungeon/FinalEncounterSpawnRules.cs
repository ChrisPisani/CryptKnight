using System.Collections.Generic;
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
            for (int i = 0; i < count; i++)
            {
                spawns.Add(new RoomEnemySpawn(configuration.EnemyKind, positions[i]));
            }

            return spawns;
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
