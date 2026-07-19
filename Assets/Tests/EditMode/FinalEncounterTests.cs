using System;
using System.Collections.Generic;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
using CryptKnight.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class FinalEncounterTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void FinalRoomHasThreeWaves()
        {
            FinalEncounterConfiguration configuration = FinalEncounterConfiguration.CreateDefault();

            Assert.That(configuration.WaveCount, Is.EqualTo(3));
            Assert.That(configuration.IntermissionSeconds, Is.EqualTo(2f));
        }

        [Test]
        public void WavesSpawnFourSixAndEight()
        {
            FinalEncounterConfiguration configuration = FinalEncounterConfiguration.CreateDefault();
            FinalEncounterSpawnRules rules = new FinalEncounterSpawnRules();

            Assert.That(rules.CreateWave(configuration, 0, 12345, Vector2Int.one), Has.Count.EqualTo(4));
            Assert.That(rules.CreateWave(configuration, 1, 12345, Vector2Int.one), Has.Count.EqualTo(6));
            Assert.That(rules.CreateWave(configuration, 2, 12345, Vector2Int.one), Has.Count.EqualTo(8));
        }

        [Test]
        public void FinalFightUsesOnlyZombies()
        {
            FinalEncounterConfiguration configuration = FinalEncounterConfiguration.CreateDefault();
            IReadOnlyList<RoomEnemySpawn> spawns = new FinalEncounterSpawnRules().CreateWave(
                configuration,
                2,
                12345,
                Vector2Int.zero);

            Assert.That(configuration.EnemyMaxHealth, Is.EqualTo(5));
            for (int i = 0; i < spawns.Count; i++)
            {
                Assert.That(spawns[i].Kind, Is.EqualTo(EnemyKind.Zombie));
            }
        }

        [Test]
        public void WaveWaitsUntilEnemiesAreGone()
        {
            FinalEncounterState encounter = new FinalEncounterState(FinalEncounterConfiguration.CreateDefault());

            Assert.That(encounter.BeginNextIntermission(), Is.True);
            Assert.That(encounter.Status, Is.EqualTo(FinalEncounterStatus.Intermission));
            Assert.That(encounter.StartCurrentWave(), Is.EqualTo(4));
            Assert.That(encounter.RecordEnemyDefeated(), Is.False);
            Assert.That(encounter.RecordEnemyDefeated(), Is.False);
            Assert.That(encounter.RecordEnemyDefeated(), Is.False);
            Assert.That(encounter.RemainingEnemies, Is.EqualTo(1));
            Assert.That(encounter.RecordEnemyDefeated(), Is.True);
            Assert.That(encounter.Status, Is.EqualTo(FinalEncounterStatus.NotStarted));
        }

        [Test]
        public void FinalFightCompletesRun()
        {
            FinalEncounterState encounter = new FinalEncounterState(FinalEncounterConfiguration.CreateDefault());

            for (int wave = 0; wave < encounter.TotalWaves; wave++)
            {
                Assert.That(encounter.BeginNextIntermission(), Is.True);
                int enemyCount = encounter.StartCurrentWave();
                for (int enemy = 0; enemy < enemyCount; enemy++)
                {
                    encounter.RecordEnemyDefeated();
                }
            }

            Assert.That(encounter.IsComplete, Is.True);
            Assert.That(encounter.Status, Is.EqualTo(FinalEncounterStatus.Completed));
            Assert.That(encounter.BeginNextIntermission(), Is.False);
            Assert.That(encounter.StartCurrentWave(), Is.EqualTo(0));
            Assert.That(encounter.RecordEnemyDefeated(), Is.False);
        }

        [Test]
        public void FinalRoomCannotBeLeft()
        {
            DungeonRoomRuntimeState room = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Final);

            Assert.That(room.IsLocked, Is.True);
            room.SetEnemyCount(0);
            Assert.That(room.IsLocked, Is.True);
        }

        [Test]
        public void FinalStateIsCreatedWithDungeon()
        {
            DungeonRunState dungeon = DungeonRunStateFactory.Create(4, 4, 12345);
            DungeonRoomRuntimeState finalRoom = dungeon.GetRoomState(dungeon.Layout.FinalPosition);

            Assert.That(finalRoom.FinalEncounter, Is.Not.Null);
            Assert.That(finalRoom.FinalEncounter.Status, Is.EqualTo(FinalEncounterStatus.NotStarted));
            Assert.That(dungeon.FinalEncounterConfig.WaveEnemyCounts, Is.EqualTo(new[] { 4, 6, 8 }));
        }

        [Test]
        public void InvalidWaveConfigIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new FinalEncounterConfiguration(null, 2f, EnemyKind.Zombie, 5));
            Assert.Throws<ArgumentException>(() => new FinalEncounterConfiguration(Array.Empty<int>(), 2f, EnemyKind.Zombie, 5));

            FinalEncounterConfiguration configuration = new FinalEncounterConfiguration(new[] { 0 }, -1f, EnemyKind.Spider, 0);
            Assert.That(configuration.GetEnemyCount(0), Is.EqualTo(1));
            Assert.That(configuration.IntermissionSeconds, Is.Zero);
            Assert.That(configuration.EnemyMaxHealth, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.GetEnemyCount(1));
        }

        [Test]
        public void FinalStateOnlyInitializesOnce()
        {
            DungeonRoomRuntimeState finalRoom = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Final);
            FinalEncounterConfiguration first = FinalEncounterConfiguration.CreateDefault();
            FinalEncounterConfiguration second = new FinalEncounterConfiguration(new[] { 1 }, 0f, EnemyKind.Spider, 1);

            finalRoom.InitializeFinalEncounter(first);
            FinalEncounterState originalState = finalRoom.FinalEncounter;
            finalRoom.InitializeFinalEncounter(second);

            Assert.That(finalRoom.FinalEncounter, Is.SameAs(originalState));
            DungeonRoomRuntimeState enemyRoom = new DungeonRoomRuntimeState(Vector2Int.one, RoomType.Enemy);
            enemyRoom.InitializeFinalEncounter(first);
            Assert.That(enemyRoom.FinalEncounter, Is.Null);
        }

        [Test]
        public void ControllerStartsWaveAfterDelay()
        {
            FinalEncounterConfiguration configuration = FinalEncounterConfiguration.CreateDefault();
            DungeonRoomRuntimeState room = CreateFinalRoom(configuration);
            FinalEncounterController controller = CreateController();
            int spawned = 0;

            controller.Initialize(room, configuration, 12345, _ => spawned++, () => { });

            Assert.That(room.FinalEncounter.Status, Is.EqualTo(FinalEncounterStatus.Intermission));
            Assert.That(controller.AdvanceIntermission(1f), Is.False);
            Assert.That(spawned, Is.Zero);
            Assert.That(controller.AdvanceIntermission(1f), Is.True);
            Assert.That(spawned, Is.EqualTo(4));
            Assert.That(controller.AdvanceIntermission(2f), Is.False);
        }

        [Test]
        public void ControllerRunsAllWaves()
        {
            FinalEncounterConfiguration configuration = new FinalEncounterConfiguration(
                new[] { 4, 6, 8 },
                0f,
                EnemyKind.Zombie,
                5);
            DungeonRoomRuntimeState room = CreateFinalRoom(configuration);
            FinalEncounterController controller = CreateController();
            int spawned = 0;
            bool completed = false;

            controller.Initialize(room, configuration, 54321, _ => spawned++, () => completed = true);
            Assert.That(spawned, Is.EqualTo(4));

            for (int wave = 0; wave < configuration.WaveCount; wave++)
            {
                int enemyCount = configuration.GetEnemyCount(wave);
                for (int enemy = 0; enemy < enemyCount - 1; enemy++)
                {
                    Assert.That(controller.NotifyEnemyDefeated(), Is.False);
                }

                Assert.That(controller.NotifyEnemyDefeated(), Is.True);
            }

            Assert.That(spawned, Is.EqualTo(18));
            Assert.That(completed, Is.True);
            Assert.That(controller.NotifyEnemyDefeated(), Is.False);
        }

        [Test]
        public void ControllerResumesIntermission()
        {
            FinalEncounterConfiguration configuration = FinalEncounterConfiguration.CreateDefault();
            DungeonRoomRuntimeState room = CreateFinalRoom(configuration);
            room.FinalEncounter.BeginNextIntermission();
            FinalEncounterController controller = CreateController();
            int spawned = 0;

            controller.Initialize(room, configuration, 12345, _ => spawned++, () => { });

            Assert.That(controller.AdvanceIntermission(2f), Is.True);
            Assert.That(spawned, Is.EqualTo(4));
        }

        [Test]
        public void CompletedControllerFinishesImmediately()
        {
            FinalEncounterConfiguration configuration = new FinalEncounterConfiguration(
                new[] { 1 },
                0f,
                EnemyKind.Zombie,
                5);
            DungeonRoomRuntimeState room = CreateFinalRoom(configuration);
            room.FinalEncounter.BeginNextIntermission();
            room.FinalEncounter.StartCurrentWave();
            room.FinalEncounter.RecordEnemyDefeated();
            FinalEncounterController controller = CreateController();
            bool completed = false;

            controller.Initialize(room, configuration, 12345, _ => { }, () => completed = true);

            Assert.That(completed, Is.True);
        }

        [Test]
        public void BadControllerSetupIsRejected()
        {
            FinalEncounterConfiguration configuration = FinalEncounterConfiguration.CreateDefault();
            FinalEncounterController controller = CreateController();

            Assert.Throws<ArgumentNullException>(() => controller.Initialize(null, configuration, 0, _ => { }, () => { }));
            DungeonRoomRuntimeState room = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Final);
            Assert.Throws<InvalidOperationException>(() => controller.Initialize(room, configuration, 0, _ => { }, () => { }));
        }

        private DungeonRoomRuntimeState CreateFinalRoom(FinalEncounterConfiguration configuration)
        {
            DungeonRoomRuntimeState room = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Final);
            room.InitializeFinalEncounter(configuration);
            return room;
        }

        private FinalEncounterController CreateController()
        {
            GameObject controllerObject = new GameObject("Final Encounter Test");
            createdObjects.Add(controllerObject);
            return controllerObject.AddComponent<FinalEncounterController>();
        }
    }
}
