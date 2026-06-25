using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CryptKnight.Data;
using CryptKnight.Dungeon;
using CryptKnight.Gameplay;
using CryptKnight.Loot;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class LootDistributionTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

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

            GameObject gameManager = GameObject.Find("Game Manager");
            if (gameManager != null)
            {
                UnityEngine.Object.DestroyImmediate(gameManager);
            }
        }

        [Test]
        public void EligibleRoomsCanGetChests()
        {
            LootDistributionRules rules = new LootDistributionRules(1f, 0f);

            Assert.That(rules.ShouldPlaceChest(RoomType.Enemy, 12345, Vector2Int.zero), Is.True);
            Assert.That(rules.ShouldPlaceChest(RoomType.Trap, 12345, Vector2Int.one), Is.True);
        }

        [Test]
        public void StarterAndFinalRoomsDoNotGetChests()
        {
            LootDistributionRules rules = new LootDistributionRules(1f, 0f);

            Assert.That(rules.ShouldPlaceChest(RoomType.Starter, 12345, Vector2Int.zero), Is.False);
            Assert.That(rules.ShouldPlaceChest(RoomType.Final, 12345, Vector2Int.one), Is.False);
        }

        [Test]
        public void EligibleRoomsCanGetKeys()
        {
            LootDistributionRules rules = new LootDistributionRules(0f, 0f, 1f);

            Assert.That(rules.ShouldPlaceKey(RoomType.Enemy, 12345, Vector2Int.zero), Is.True);
            Assert.That(rules.ShouldPlaceKey(RoomType.Trap, 12345, Vector2Int.one), Is.True);
            Assert.That(rules.KeySpawnChance, Is.EqualTo(1f));
        }

        [Test]
        public void StarterAndFinalRoomsDoNotGetKeys()
        {
            LootDistributionRules rules = new LootDistributionRules(0f, 0f, 1f);

            Assert.That(rules.ShouldPlaceKey(RoomType.Starter, 12345, Vector2Int.zero), Is.False);
            Assert.That(rules.ShouldPlaceKey(RoomType.Final, 12345, Vector2Int.one), Is.False);
        }

        [Test]
        public void EnemyDefeatCanDropLoot()
        {
            DungeonRoomRuntimeState roomState = CreateEnemyRoomState();
            LootDropResult enemyDrop = CreateAlwaysDropLootSystem().RollDrop(LootSourceType.Enemy, 0f, 0);

            roomState.AddLoot(enemyDrop.Item, Vector2.zero);

            Assert.That(enemyDrop.HasDrop, Is.True);
            Assert.That(roomState.Loot, Has.Count.EqualTo(1));
            Assert.That(roomState.Loot[0].ItemDefinition.ItemId, Is.EqualTo("key"));
        }

        [Test]
        public void RoomClearCanDropLoot()
        {
            DungeonRoomRuntimeState roomState = CreateEnemyRoomState();
            bool becameCleared = roomState.DefeatEnemy();
            LootDropResult roomClearDrop = CreateAlwaysDropLootSystem().RollDrop(LootSourceType.RoomClear, 0f, 1);

            roomState.AddLoot(roomClearDrop.Item, Vector2.one);

            Assert.That(becameCleared, Is.True);
            Assert.That(roomClearDrop.HasDrop, Is.True);
            Assert.That(roomState.IsCleared, Is.True);
            Assert.That(roomState.Loot.Single().ItemDefinition.ItemId, Is.EqualTo("damage_up"));
        }

        [Test]
        public void EnemyAndRoomClearCanBothDrop()
        {
            DungeonRoomRuntimeState roomState = CreateEnemyRoomState();
            LootSystem lootSystem = CreateAlwaysDropLootSystem();

            bool becameCleared = roomState.DefeatEnemy();
            LootDropResult enemyDrop = lootSystem.RollDrop(LootSourceType.Enemy, 0f, 0);
            LootDropResult roomClearDrop = lootSystem.RollDrop(LootSourceType.RoomClear, 0f, 1);
            roomState.AddLoot(enemyDrop.Item, Vector2.zero);
            roomState.AddLoot(roomClearDrop.Item, Vector2.one);

            Assert.That(becameCleared, Is.True);
            Assert.That(roomState.Loot, Has.Count.EqualTo(2));
        }

        [Test]
        public void PickedUpStarterLootStaysGone()
        {
            DungeonRoomRuntimeState roomState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Starter);
            RoomLootInstance loot = roomState.AddLoot(CreateItem("key", LootSourceType.Enemy), Vector2.zero);

            bool marked = roomState.MarkLootCollected(loot.Id);

            Assert.That(marked, Is.True);
            Assert.That(roomState.Loot.Count(item => !item.IsCollected), Is.EqualTo(0));
        }

        [Test]
        public void StarterRoomGetsKeyAndChest()
        {
            GameObject controllerObject = new GameObject("Gameplay Controller");
            createdObjects.Add(controllerObject);
            GameplaySceneController controller = controllerObject.AddComponent<GameplaySceneController>();
            DungeonRoomRuntimeState roomState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Starter);

            InvokePrivateMethod(controller, "AddStarterGiftToRoomState", roomState);

            Assert.That(roomState.Loot, Has.Count.EqualTo(1));
            Assert.That(roomState.Loot.Single().ItemDefinition.ItemId, Is.EqualTo("key"));
            Assert.That(roomState.Chests, Has.Count.EqualTo(1));
        }

        [Test]
        public void DefeatedEnemyStaysGone()
        {
            DungeonRoomRuntimeState roomState = CreateEnemyRoomState();

            bool becameCleared = roomState.DefeatEnemy();

            Assert.That(becameCleared, Is.True);
            Assert.That(roomState.RemainingEnemies, Is.EqualTo(0));
            Assert.That(roomState.DefeatedEnemies, Is.EqualTo(1));
            Assert.That(roomState.IsCleared, Is.True);
        }

        [Test]
        public void EnemyRoomLocksUntilCleared()
        {
            DungeonRoomRuntimeState roomState = CreateEnemyRoomState();

            Assert.That(roomState.IsLocked, Is.True);

            roomState.DefeatEnemy();

            Assert.That(roomState.IsLocked, Is.False);
        }

        [Test]
        public void StarterAndFinalRoomsStayUnlocked()
        {
            DungeonRoomRuntimeState starterState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Starter);
            DungeonRoomRuntimeState finalState = new DungeonRoomRuntimeState(Vector2Int.one, RoomType.Final);
            starterState.SetEnemyCount(1);
            finalState.SetEnemyCount(1);

            Assert.That(starterState.IsLocked, Is.False);
            Assert.That(finalState.IsLocked, Is.False);
        }

        [Test]
        public void LockedRoomsBlockTravel()
        {
            DungeonRoom startRoom = new DungeonRoom(Vector2Int.zero, RoomType.Enemy);
            DungeonRoom nextRoom = new DungeonRoom(Vector2Int.right, RoomType.Trap);
            startRoom.Connect(RoomDirection.East, Vector2Int.right);
            nextRoom.Connect(RoomDirection.West, Vector2Int.zero);
            DungeonLayout layout = new DungeonLayout(2, 1, new[] { startRoom, nextRoom }, Vector2Int.zero, Vector2Int.right);
            DungeonRoomNavigator navigator = new DungeonRoomNavigator(layout);
            DungeonRoomRuntimeState lockedState = CreateEnemyRoomState();

            GameObject controllerObject = new GameObject("Gameplay Controller");
            createdObjects.Add(controllerObject);
            GameplaySceneController controller = controllerObject.AddComponent<GameplaySceneController>();
            SetPrivateField(controller, "roomNavigator", navigator);
            GetRoomStates(controller)[Vector2Int.zero] = lockedState;

            bool moved = controller.TryTravelThroughDoor(RoomDirection.East);

            Assert.That(moved, Is.False);
            Assert.That(navigator.CurrentRoom.GridPosition, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void UncollectedDropStaysInRoom()
        {
            DungeonRoomRuntimeState roomState = CreateEnemyRoomState();

            RoomLootInstance loot = roomState.AddLoot(CreateItem("damage_up", LootSourceType.Enemy), new Vector2(2f, 1f));

            Assert.That(loot.IsCollected, Is.False);
            Assert.That(roomState.Loot.Single().Position, Is.EqualTo(new Vector2(2f, 1f)));
        }

        [Test]
        public void OpenedChestStaysGone()
        {
            DungeonRoomRuntimeState roomState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Trap);
            RoomChestInstance chest = roomState.AddChest(Vector2.zero);

            bool opened = roomState.MarkChestOpened(chest.Id);

            Assert.That(opened, Is.True);
            Assert.That(roomState.Chests.Single().IsOpened, Is.True);
        }

        [Test]
        public void DefaultRoomLootIsDisabled()
        {
            LootDistributionRules rules = LootDistributionRules.CreateDefault();

            Assert.That(rules.ShouldPlaceLooseRoomLoot(RoomType.Enemy, 12345, Vector2Int.zero), Is.False);
            Assert.That(rules.RoomLootChance, Is.EqualTo(0f));
            Assert.That(rules.KeySpawnChance, Is.EqualTo(0.10f));
        }

        private static DungeonRoomRuntimeState CreateEnemyRoomState()
        {
            DungeonRoomRuntimeState roomState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Enemy);
            roomState.SetEnemyCount(1);
            return roomState;
        }

        private static LootSystem CreateAlwaysDropLootSystem()
        {
            return new LootSystem(new LootTableConfiguration(
                new[]
                {
                    CreateItem("key", LootSourceType.Enemy, LootSourceType.RoomClear),
                    CreateItem("damage_up", LootSourceType.Enemy, LootSourceType.RoomClear)
                },
                new Dictionary<LootSourceType, float>
                {
                    { LootSourceType.Enemy, 1f },
                    { LootSourceType.RoomClear, 1f }
                }));
        }

        private static LootItemDefinition CreateItem(string itemId, params LootSourceType[] sources)
        {
            int keyAmount = itemId == "key" ? 1 : 0;
            return new LootItemDefinition(
                itemId,
                itemId,
                string.Empty,
                new PlayerStatModifier(damageBonus: itemId == "damage_up" ? 1 : 0),
                sources,
                keyAmount: keyAmount);
        }

        private static Dictionary<Vector2Int, DungeonRoomRuntimeState> GetRoomStates(GameplaySceneController controller)
        {
            FieldInfo field = typeof(GameplaySceneController).GetField("roomStates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Dictionary<Vector2Int, DungeonRoomRuntimeState>)field.GetValue(controller);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }
    }
}
