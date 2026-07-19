using System;
using System.Collections.Generic;
using CryptKnight.Dungeon;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    public sealed class FinalEncounterController : MonoBehaviour
    {
        private readonly FinalEncounterSpawnRules spawnRules = new FinalEncounterSpawnRules();

        private FinalEncounterConfiguration configuration;
        private FinalEncounterState encounterState;
        private DungeonRoomRuntimeState roomState;
        private Action<RoomEnemyInstance> spawnEnemy;
        private Action completeRun;
        private int runSeed;
        private float intermissionRemaining;

        public void Initialize(
            DungeonRoomRuntimeState finalRoomState,
            FinalEncounterConfiguration encounterConfiguration,
            int seed,
            Action<RoomEnemyInstance> spawnEnemyAction,
            Action completeRunAction)
        {
            roomState = finalRoomState ?? throw new ArgumentNullException(nameof(finalRoomState));
            configuration = encounterConfiguration ?? throw new ArgumentNullException(nameof(encounterConfiguration));
            encounterState = roomState.FinalEncounter ?? throw new InvalidOperationException("Final room encounter state was not initialized.");
            spawnEnemy = spawnEnemyAction ?? throw new ArgumentNullException(nameof(spawnEnemyAction));
            completeRun = completeRunAction ?? throw new ArgumentNullException(nameof(completeRunAction));
            runSeed = seed;

            if (encounterState.IsComplete)
            {
                completeRun();
                return;
            }

            if (encounterState.Status == FinalEncounterStatus.NotStarted)
            {
                BeginNextIntermission();
            }
            else if (encounterState.Status == FinalEncounterStatus.Intermission)
            {
                ResetIntermissionTimer();
            }
        }

        private void Update()
        {
            AdvanceIntermission(Time.deltaTime);
        }

        public bool AdvanceIntermission(float elapsedSeconds)
        {
            if (encounterState == null || encounterState.Status != FinalEncounterStatus.Intermission)
            {
                return false;
            }

            intermissionRemaining -= Mathf.Max(0f, elapsedSeconds);
            if (intermissionRemaining > 0f)
            {
                return false;
            }

            return SpawnCurrentWave();
        }

        public bool NotifyEnemyDefeated()
        {
            if (encounterState == null || !encounterState.RecordEnemyDefeated())
            {
                return false;
            }

            if (encounterState.IsComplete)
            {
                completeRun();
            }
            else
            {
                BeginNextIntermission();
            }

            return true;
        }

        private void BeginNextIntermission()
        {
            if (encounterState.BeginNextIntermission())
            {
                ResetIntermissionTimer();
            }
        }

        private void ResetIntermissionTimer()
        {
            intermissionRemaining = configuration.IntermissionSeconds;
            if (intermissionRemaining <= 0f)
            {
                AdvanceIntermission(0f);
            }
        }

        private bool SpawnCurrentWave()
        {
            int enemyCount = encounterState.StartCurrentWave();
            if (enemyCount <= 0)
            {
                return false;
            }

            IReadOnlyList<RoomEnemySpawn> spawns = spawnRules.CreateWave(
                configuration,
                encounterState.CurrentWaveIndex,
                runSeed,
                roomState.GridPosition);
            for (int i = 0; i < spawns.Count; i++)
            {
                RoomEnemySpawn spawn = spawns[i];
                RoomEnemyInstance enemy = roomState.AddEnemy(spawn.Kind, spawn.Position, configuration.EnemyMaxHealth);
                spawnEnemy(enemy);
            }

            return true;
        }
    }
}
