using System;
using System.Collections.Generic;
using System.Reflection;
using CryptKnight.Application;
using CryptKnight.Data;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
using CryptKnight.Gameplay;
using CryptKnight.Loot;
using CryptKnight.Player;
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
        public void HardFinaleMixesEnemyTypes()
        {
            FinalEncounterConfiguration configuration =
                FinalEncounterConfiguration.CreateDefault(EnemyDifficulty.Hard);
            IReadOnlyList<RoomEnemySpawn> first = new FinalEncounterSpawnRules().CreateWave(
                configuration,
                0,
                24680,
                Vector2Int.one);
            IReadOnlyList<RoomEnemySpawn> second = new FinalEncounterSpawnRules().CreateWave(
                configuration,
                0,
                24680,
                Vector2Int.one);

            Assert.That(first, Has.Count.EqualTo(4));
            Assert.That(first[0].Kind, Is.EqualTo(second[0].Kind));
            Assert.That(ContainsKind(first, EnemyKind.Zombie), Is.True);
            Assert.That(ContainsKind(first, EnemyKind.Spider), Is.True);
            Assert.That(configuration.GetEnemyMaxHealth(EnemyKind.Zombie), Is.EqualTo(10));
            Assert.That(configuration.GetEnemyMaxHealth(EnemyKind.Spider), Is.EqualTo(6));
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
        public void FinalRoomUnlocksAfterFight()
        {
            FinalEncounterConfiguration configuration = new FinalEncounterConfiguration(
                new[] { 1 },
                0f,
                EnemyKind.Zombie,
                5);
            DungeonRoom finalRoom = new DungeonRoom(Vector2Int.zero, RoomType.Final);
            DungeonRoom neighbor = new DungeonRoom(Vector2Int.right, RoomType.Enemy);
            finalRoom.Connect(RoomDirection.East, Vector2Int.right);
            neighbor.Connect(RoomDirection.West, Vector2Int.zero);
            DungeonLayout layout = new DungeonLayout(
                2,
                1,
                new[] { finalRoom, neighbor },
                Vector2Int.zero,
                Vector2Int.zero);
            DungeonRoomRuntimeState roomState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Final);
            roomState.InitializeFinalEncounter(configuration);
            Dictionary<Vector2Int, DungeonRoomRuntimeState> roomStates =
                new Dictionary<Vector2Int, DungeonRoomRuntimeState>
                {
                    { Vector2Int.zero, roomState },
                    { Vector2Int.right, new DungeonRoomRuntimeState(Vector2Int.right, RoomType.Enemy) }
                };
            DungeonRunState dungeon = new DungeonRunState(
                layout,
                roomStates,
                new LootTableConfiguration(
                    Array.Empty<LootItemDefinition>(),
                    new Dictionary<LootSourceType, float>()),
                12345,
                configuration);
            GameplaySceneController controller = CreateGameplayController();
            SetPrivateField(controller, "dungeonRun", dungeon);
            SetPrivateField(controller, "roomNavigator", dungeon.Navigator);

            Assert.That(roomState.IsLocked, Is.True);
            Assert.That(controller.CanTravelFromCurrentRoom(), Is.False);

            roomState.FinalEncounter.BeginNextIntermission();
            roomState.FinalEncounter.StartCurrentWave();
            roomState.FinalEncounter.RecordEnemyDefeated();

            Assert.That(roomState.IsLocked, Is.False);
            Assert.That(controller.CanTravelFromCurrentRoom(), Is.True);
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
        public void PortalOnlyWorksOnce()
        {
            GameObject portalObject = new GameObject("Portal");
            createdObjects.Add(portalObject);
            FloorPortal portal = portalObject.AddComponent<FloorPortal>();
            int activations = 0;
            portal.Initialize(() => activations++);

            Assert.That(portal.TryActivate(), Is.False);
            Assert.That(portal.AdvanceAnimation(2f), Is.True);
            Assert.That(portal.TryActivate(), Is.True);
            Assert.That(portal.TryActivate(), Is.False);
            Assert.That(portal.IsUsed, Is.True);
            Assert.That(activations, Is.EqualTo(1));
        }

        [Test]
        public void PortalUsesProvidedAssets()
        {
            GameObject portalObject = new GameObject("Portal");
            createdObjects.Add(portalObject);
            FloorPortal portal = portalObject.AddComponent<FloorPortal>();
            portal.Initialize(() => { });
            CircleCollider2D interaction = portalObject.GetComponent<CircleCollider2D>();
            AudioSource idleAudio = portalObject.GetComponent<AudioSource>();

            Assert.That(portal.AnimationFrameCount, Is.EqualTo(8));
            Assert.That(portal.IsInteractable, Is.False);
            Assert.That(interaction.enabled, Is.False);
            Assert.That(idleAudio.clip, Is.Not.Null);
            Assert.That(idleAudio.loop, Is.True);

            portal.AdvanceAnimation(2f);

            Assert.That(portal.IsInteractable, Is.True);
            Assert.That(interaction.enabled, Is.True);
            Assert.That(
                Resources.Load<AudioClip>("Audio/SFX/crypt-knight-sfx-portal-enter"),
                Is.Not.Null);
        }

        [Test]
        public void FloorOneFightOpensPortal()
        {
            GameManager manager = GameManager.Instance;
            createdObjects.Add(manager.gameObject);
            GameRunState run = manager.StartNewRun(12345);
            GameplaySceneController controller = CreateGameplayController();
            GameObject room = new GameObject("Final Room");
            createdObjects.Add(room);

            InvokePrivate(controller, "HandleFinalEncounterCompleted", room.transform);
            InvokePrivate(controller, "HandleFinalEncounterCompleted", room.transform);

            Assert.That(run.IsActive, Is.True);
            Assert.That(room.GetComponentsInChildren<FloorPortal>(), Has.Length.EqualTo(1));
        }

        [Test]
        public void FloorTwoFightCompletesRun()
        {
            GameManager manager = GameManager.Instance;
            createdObjects.Add(manager.gameObject);
            GameRunState run = manager.StartNewRun(54321);
            Assert.That(manager.AdvanceToNextFloor(), Is.True);
            GameplaySceneController controller = CreateGameplayController();
            GameObject room = new GameObject("Final Room");
            createdObjects.Add(room);

            InvokePrivate(controller, "HandleFinalEncounterCompleted", room.transform);

            Assert.That(run.Status, Is.EqualTo(GameRunStatus.Completed));
            Assert.That(room.GetComponentInChildren<FloorPortal>(), Is.Null);
        }

        [Test]
        public void PortalPromptTracksPlayerRange()
        {
            GameObject portalObject = new GameObject("Portal");
            createdObjects.Add(portalObject);
            FloorPortal portal = portalObject.AddComponent<FloorPortal>();
            portal.Initialize(() => { });
            portal.AdvanceAnimation(2f);
            GameObject prompt = portalObject.transform.Find("Portal Prompt").gameObject;

            GameObject obstacle = new GameObject("Obstacle");
            createdObjects.Add(obstacle);
            BoxCollider2D obstacleCollider = obstacle.AddComponent<BoxCollider2D>();
            InvokePrivate(portal, "OnTriggerEnter2D", obstacleCollider);
            Assert.That(prompt.activeSelf, Is.False);

            GameObject player = new GameObject("Player");
            createdObjects.Add(player);
            player.AddComponent<PlayerController>();
            CircleCollider2D playerCollider = player.AddComponent<CircleCollider2D>();
            InvokePrivate(portal, "OnTriggerEnter2D", playerCollider);
            Assert.That(prompt.activeSelf, Is.True);
            Assert.That(portal.ShouldPlayIdleAudio, Is.True);

            GameManager manager = GameManager.Instance;
            createdObjects.Add(manager.gameObject);
            InvokePrivate(portal, "Update");

            InvokePrivate(portal, "OnTriggerExit2D", obstacleCollider);
            Assert.That(prompt.activeSelf, Is.True);
            InvokePrivate(portal, "OnTriggerExit2D", playerCollider);
            Assert.That(prompt.activeSelf, Is.False);
            Assert.That(portal.ShouldPlayIdleAudio, Is.False);
        }

        [Test]
        public void PortalNeedsCallback()
        {
            GameObject portalObject = new GameObject("Portal");
            createdObjects.Add(portalObject);
            FloorPortal portal = portalObject.AddComponent<FloorPortal>();

            Assert.Throws<ArgumentNullException>(() => portal.Initialize(null));
            Assert.That(portal.TryActivate(), Is.False);
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

        private GameplaySceneController CreateGameplayController()
        {
            GameObject controllerObject = new GameObject("Gameplay Test");
            createdObjects.Add(controllerObject);
            return controllerObject.AddComponent<GameplaySceneController>();
        }

        private static bool ContainsKind(IReadOnlyList<RoomEnemySpawn> spawns, EnemyKind kind)
        {
            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
