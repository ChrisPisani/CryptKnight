using System.Linq;
using System.Reflection;
using CryptKnight.Data;
using CryptKnight.Loot;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class LootTests
    {
        private readonly System.Collections.Generic.List<Object> createdObjects = new System.Collections.Generic.List<Object>();

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
        }

        [Test]
        public void DefaultItemsExist()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            Assert.That(configuration.Items, Has.Count.EqualTo(5));

            LootItemDefinition heartItem = configuration.Items.Single(item => item.ItemId == "heart_container");
            LootItemDefinition damageItem = configuration.Items.Single(item => item.ItemId == "damage_up");
            LootItemDefinition speedItem = configuration.Items.Single(item => item.ItemId == "speed_up");
            LootItemDefinition attackRateItem = configuration.Items.Single(item => item.ItemId == "attack_rate_up");
            LootItemDefinition keyItem = configuration.Items.Single(item => item.ItemId == "key");

            Assert.That(heartItem.StatModifier.MaxHealthBonus, Is.EqualTo(2));
            Assert.That(heartItem.Description, Is.EqualTo("Gain one full heart container."));
            Assert.That(heartItem.IconAssetPath, Is.EqualTo("Assets/Art/Items/heart_container.png"));
            Assert.That(damageItem.StatModifier.DamageBonus, Is.EqualTo(1));
            Assert.That(speedItem.StatModifier.MovementSpeedBonus, Is.EqualTo(1f));
            Assert.That(attackRateItem.StatModifier.AttackRateBonus, Is.EqualTo(0.2f));
            Assert.That(keyItem.KeyAmount, Is.EqualTo(1));
            Assert.That(keyItem.IconAssetPath, Is.EqualTo("Assets/Art/Items/key.png"));
        }

        [Test]
        public void DefaultSourcesAllowAllItems()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            foreach (LootItemDefinition item in configuration.Items)
            {
                Assert.That(item.CanAppearFrom(LootSourceType.Enemy), Is.True, item.ItemId);
                Assert.That(item.CanAppearFrom(LootSourceType.Chest), Is.True, item.ItemId);
                Assert.That(item.CanAppearFrom(LootSourceType.Shop), Is.True, item.ItemId);
            }
        }

        [Test]
        public void DefaultDropRatesAreSet()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(0.10f));
            Assert.That(configuration.GetDropRate(LootSourceType.Chest), Is.EqualTo(1f));
            Assert.That(configuration.GetDropRate(LootSourceType.Shop), Is.EqualTo(1f));
        }

        [Test]
        public void EnemyDropChanceWorks()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult failedRoll = lootSystem.RollDrop(LootSourceType.Enemy, 0.10f, 0);
            LootDropResult successfulRoll = lootSystem.RollDrop(LootSourceType.Enemy, 0.09f, 0);

            Assert.That(failedRoll.HasDrop, Is.False);
            Assert.That(successfulRoll.HasDrop, Is.True);
        }

        [Test]
        public void ChestAlwaysDrops()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult result = lootSystem.RollDrop(LootSourceType.Chest, 0.99f, 2);

            Assert.That(result.HasDrop, Is.True);
            Assert.That(result.Item.ItemId, Is.EqualTo("speed_up"));
        }

        [Test]
        public void JsonConfigLoads()
        {
            string json = @"{
  ""sourceDropRates"": [
    { ""source"": ""Enemy"", ""dropRate"": 0.25 }
  ],
  ""items"": [
    {
      ""itemId"": ""test_item"",
      ""displayName"": ""Test Item"",
      ""description"": ""Used by tests to prove config parsing works."",
      ""iconAssetPath"": ""Assets/Art/Items/test_item.png"",
      ""keyAmount"": 3,
      ""allowedSources"": [""Enemy""],
      ""statModifier"": { ""maxHealthBonus"": 0, ""damageBonus"": 2, ""movementSpeedBonus"": 0.5, ""attackRateBonus"": 0.1 }
    }
  ]
}";

            LootTableConfiguration configuration = LootTableConfiguration.FromJson(json);

            Assert.That(configuration.Items, Has.Count.EqualTo(1));
            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(0.25f));
            Assert.That(configuration.GetDropRate(LootSourceType.Chest), Is.EqualTo(0f));

            LootItemDefinition item = configuration.Items[0];
            Assert.That(item.ItemId, Is.EqualTo("test_item"));
            Assert.That(item.DisplayName, Is.EqualTo("Test Item"));
            Assert.That(item.Description, Is.EqualTo("Used by tests to prove config parsing works."));
            Assert.That(item.IconAssetPath, Is.EqualTo("Assets/Art/Items/test_item.png"));
            Assert.That(item.KeyAmount, Is.EqualTo(3));
            Assert.That(item.CanAppearFrom(LootSourceType.Enemy), Is.True);
            Assert.That(item.CanAppearFrom(LootSourceType.Chest), Is.False);
            Assert.That(item.StatModifier.DamageBonus, Is.EqualTo(2));
            Assert.That(item.StatModifier.MovementSpeedBonus, Is.EqualTo(0.5f));
            Assert.That(item.StatModifier.AttackRateBonus, Is.EqualTo(0.1f));
        }

        [Test]
        public void CollectorAppliesItemBenefitsToRun()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, new PlayerBaseStats(6, 1, 5f, 1f));
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                "Used by tests to prove item pickup effects apply.",
                new PlayerStatModifier(maxHealthBonus: 2, damageBonus: 3),
                new[] { LootSourceType.Chest },
                keyAmount: 2);

            bool applied = LootItemCollector.ApplyToRun(runState, item);

            Assert.That(applied, Is.True);
            Assert.That(runState.MaxHealth, Is.EqualTo(8));
            Assert.That(runState.CurrentHealth, Is.EqualTo(8));
            Assert.That(runState.PlayerStats.Damage, Is.EqualTo(4));
            Assert.That(runState.KeyCount, Is.EqualTo(2));
            Assert.That(runState.CollectedItems, Has.Count.EqualTo(1));
            Assert.That(runState.CollectedItems[0].ItemId, Is.EqualTo("test_relic"));
        }

        [Test]
        public void CollectorAddsKeysOnlyToKeyCounter()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, new PlayerBaseStats(6, 1, 5f, 1f));
            LootItemDefinition key = new LootItemDefinition(
                "key",
                "Key",
                "Gain 1 key.",
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest },
                keyAmount: 1);

            bool applied = LootItemCollector.ApplyToRun(runState, key);

            Assert.That(applied, Is.True);
            Assert.That(runState.KeyCount, Is.EqualTo(1));
            Assert.That(runState.CollectedItems, Is.Empty);
        }

        [Test]
        public void CollectorIgnoresInactiveRuns()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, new PlayerBaseStats(6, 1, 5f, 1f));
            runState.QuitRun();

            LootItemDefinition item = new LootItemDefinition(
                "inactive_relic",
                "Inactive Relic",
                string.Empty,
                new PlayerStatModifier(damageBonus: 3),
                new[] { LootSourceType.Chest },
                keyAmount: 2);

            bool applied = LootItemCollector.ApplyToRun(runState, item);

            Assert.That(applied, Is.False);
            Assert.That(runState.PlayerStats.Damage, Is.EqualTo(1));
            Assert.That(runState.KeyCount, Is.EqualTo(0));
            Assert.That(runState.CollectedItems, Is.Empty);
        }

        [Test]
        public void FallbackColorsMatchDefaultItemRepresentations()
        {
            AssertColor(LootItemVisuals.GetFallbackColor("heart_container"), new Color(0.88f, 0.06f, 0.08f, 1f));
            AssertColor(LootItemVisuals.GetFallbackColor("damage_up"), new Color(0.12f, 0.70f, 0.25f, 1f));
            AssertColor(LootItemVisuals.GetFallbackColor("speed_up"), new Color(0.98f, 0.84f, 0.16f, 1f));
            AssertColor(LootItemVisuals.GetFallbackColor("attack_rate_up"), new Color(0.95f, 0.42f, 0.10f, 1f));
        }

        [Test]
        public void CollectedItemSpritesExistForDefaultItems()
        {
            string[] itemIds =
            {
                "heart_container",
                "damage_up",
                "speed_up",
                "attack_rate_up",
                "key"
            };

            foreach (string itemId in itemIds)
            {
                Assert.That(LootItemVisuals.GetItemSprite(itemId), Is.Not.Null, itemId);
            }
        }

        [Test]
        public void PickupVisualIsHalfSizedWithoutShrinkingInteractionRange()
        {
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });

            LootPickup pickup = CreatePickup(item);
            CircleCollider2D pickupCollider = pickup.GetComponent<CircleCollider2D>();

            Assert.That(pickup.transform.localScale.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(pickup.transform.localScale.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(pickupCollider.radius * pickup.transform.localScale.x, Is.EqualTo(1.05f).Within(0.001f));
        }

        [Test]
        public void PickupPromptOnlyShowsWhenPlayerIsInRange()
        {
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });

            LootPickup pickup = CreatePickup(item);
            Collider2D playerCollider = CreatePlayerCollider();

            Assert.That(pickup.IsPromptVisible, Is.False);

            InvokeTrigger(pickup, "OnTriggerEnter2D", playerCollider);

            Assert.That(pickup.IsPlayerInRange, Is.True);
            Assert.That(pickup.IsPromptVisible, Is.True);

            InvokeTrigger(pickup, "OnTriggerExit2D", playerCollider);

            Assert.That(pickup.IsPlayerInRange, Is.False);
            Assert.That(pickup.IsPromptVisible, Is.False);
        }

        private LootPickup CreatePickup(LootItemDefinition item)
        {
            GameObject pickupObject = new GameObject("Test Pickup");
            createdObjects.Add(pickupObject);

            pickupObject.AddComponent<SpriteRenderer>();
            pickupObject.AddComponent<CircleCollider2D>();
            LootPickup pickup = pickupObject.AddComponent<LootPickup>();
            pickup.Initialize(item);
            return pickup;
        }

        private Collider2D CreatePlayerCollider()
        {
            GameObject playerObject = new GameObject("Test Player");
            createdObjects.Add(playerObject);

            playerObject.AddComponent<Rigidbody2D>();
            CircleCollider2D playerCollider = playerObject.AddComponent<CircleCollider2D>();
            playerObject.AddComponent<CryptKnight.Player.PlayerController>();
            return playerCollider;
        }

        private static void InvokeTrigger(LootPickup pickup, string methodName, Collider2D other)
        {
            MethodInfo method = typeof(LootPickup).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(pickup, new object[] { other });
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }
    }
}
