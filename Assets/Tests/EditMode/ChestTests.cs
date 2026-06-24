using System.Collections.Generic;
using System.Reflection;
using CryptKnight.Application;
using CryptKnight.Data;
using CryptKnight.Gameplay;
using CryptKnight.Loot;
using CryptKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class ChestTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            GameManager.Instance.StartNewRun();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();

            GameObject gameManager = GameObject.Find("Game Manager");
            if (gameManager != null)
            {
                Object.DestroyImmediate(gameManager);
            }

            GameObject sfxObject = GameObject.Find("Crypt Knight SFX");
            if (sfxObject != null)
            {
                Object.DestroyImmediate(sfxObject);
            }
        }

        [Test]
        public void ChestNeedsAKey()
        {
            ChestFixture fixture = CreateChest();

            bool opened = fixture.Chest.TryOpen();

            Assert.That(opened, Is.False);
            Assert.That(fixture.Chest.IsOpened, Is.False);
            Assert.That(GameManager.Instance.CurrentRun.KeyCount, Is.EqualTo(0));
            Assert.That(fixture.RewardCount, Is.EqualTo(0));
        }

        [Test]
        public void ChestSpendsOneKey()
        {
            GameManager.Instance.AddKeys(1);
            ChestFixture fixture = CreateChest();

            bool opened = fixture.Chest.TryOpen();

            Assert.That(opened, Is.True);
            Assert.That(fixture.Chest.IsOpened, Is.True);
            Assert.That(GameManager.Instance.CurrentRun.KeyCount, Is.EqualTo(0));
        }

        [Test]
        public void ChestOnlyOpensOnce()
        {
            GameManager.Instance.AddKeys(2);
            ChestFixture fixture = CreateChest();

            Assert.That(fixture.Chest.TryOpen(), Is.True);
            Assert.That(fixture.Chest.TryOpen(), Is.False);

            Assert.That(GameManager.Instance.CurrentRun.KeyCount, Is.EqualTo(1));
            Assert.That(fixture.RewardCount, Is.EqualTo(1));
        }

        [Test]
        public void ChestSpawnsReward()
        {
            GameManager.Instance.AddKeys(1);
            ChestFixture fixture = CreateChest();

            bool opened = fixture.Chest.TryOpen();

            Assert.That(opened, Is.True);
            Assert.That(fixture.SpawnedItem.ItemId, Is.EqualTo("test_reward"));
            Assert.That(fixture.RewardPosition.y, Is.GreaterThan(fixture.Chest.transform.position.y));
        }

        [Test]
        public void ChestSkipsKeyRewards()
        {
            GameManager.Instance.AddKeys(1);
            ChestFixture fixture = CreateChest();

            fixture.Chest.TryOpen();

            Assert.That(fixture.SpawnedItem.ItemId, Is.Not.EqualTo("key"));
            Assert.That(fixture.SpawnedItem.ItemId, Is.EqualTo("test_reward"));
        }

        [Test]
        public void ChestRewardStaysInRoom()
        {
            Vector2 clampedPosition = InvokeClampToPlayableRoom(new Vector2(100f, 100f));

            Assert.That(clampedPosition.x, Is.InRange(-6f, 6f));
            Assert.That(clampedPosition.y, Is.InRange(-3f, 3f));
        }

        [Test]
        public void ChestPromptShowsNeedKey()
        {
            LockedChest chest = CreateChest().Chest;
            Collider2D playerCollider = CreatePlayerCollider();

            InvokeTrigger(chest, "OnTriggerEnter2D", playerCollider);
            Assert.That(chest.IsPromptVisible, Is.True);
            Assert.That(chest.PromptMessage, Is.EqualTo("Press E to open chest"));

            chest.TryOpen();

            Assert.That(chest.IsPromptVisible, Is.True);
            Assert.That(chest.PromptMessage, Is.EqualTo("You do not have any keys!"));
        }

        [Test]
        public void ChestIgnoresNonPlayers()
        {
            LockedChest chest = CreateChest().Chest;
            GameObject otherObject = new GameObject("Other");
            createdObjects.Add(otherObject);
            Collider2D otherCollider = otherObject.AddComponent<CircleCollider2D>();

            InvokeTrigger(chest, "OnTriggerEnter2D", otherCollider);

            Assert.That(chest.IsPlayerInRange, Is.False);
            Assert.That(chest.IsPromptVisible, Is.False);
        }

        private ChestFixture CreateChest()
        {
            GameObject chestObject = new GameObject("Locked Chest");
            createdObjects.Add(chestObject);

            chestObject.AddComponent<SpriteRenderer>();
            chestObject.AddComponent<CircleCollider2D>();
            LockedChest chest = chestObject.AddComponent<LockedChest>();

            ChestFixture fixture = new ChestFixture(chest);

            chest.Initialize(CreateChestLootTable(), (item, position) =>
            {
                fixture.RewardCount++;
                fixture.SpawnedItem = item;
                fixture.RewardPosition = position;
            });

            return fixture;
        }

        private static LootTableConfiguration CreateChestLootTable()
        {
            LootItemDefinition key = new LootItemDefinition(
                "key",
                "Key",
                "Gain 1 key.",
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest },
                keyAmount: 1);
            LootItemDefinition item = new LootItemDefinition(
                "test_reward",
                "Test Reward",
                "Used by chest tests.",
                new PlayerStatModifier(damageBonus: 1),
                new[] { LootSourceType.Chest });

            return new LootTableConfiguration(
                new[] { key, item },
                new Dictionary<LootSourceType, float>
                {
                    { LootSourceType.Chest, 1f }
                });
        }

        private Collider2D CreatePlayerCollider()
        {
            GameObject playerObject = new GameObject("Player");
            createdObjects.Add(playerObject);

            playerObject.AddComponent<Rigidbody2D>();
            Collider2D playerCollider = playerObject.AddComponent<CircleCollider2D>();
            playerObject.AddComponent<PlayerController>();
            return playerCollider;
        }

        private static void InvokeTrigger(LockedChest chest, string methodName, Collider2D other)
        {
            MethodInfo method = typeof(LockedChest).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(chest, new object[] { other });
        }

        private static Vector2 InvokeClampToPlayableRoom(Vector2 position)
        {
            MethodInfo method = typeof(GameplaySceneController).GetMethod("ClampToPlayableRoom", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Vector2)method.Invoke(null, new object[] { position });
        }

        private sealed class ChestFixture
        {
            public ChestFixture(LockedChest chest)
            {
                Chest = chest;
            }

            public LockedChest Chest { get; }
            public int RewardCount { get; set; }
            public LootItemDefinition SpawnedItem { get; set; }
            public Vector2 RewardPosition { get; set; }
        }
    }
}
