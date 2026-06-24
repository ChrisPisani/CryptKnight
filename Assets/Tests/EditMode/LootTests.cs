using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CryptKnight.Data;
using CryptKnight.Loot;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CryptKnight.Tests.EditMode
{
    public sealed class LootTests
    {
        private readonly System.Collections.Generic.List<UnityEngine.Object> createdObjects = new System.Collections.Generic.List<UnityEngine.Object>();

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

            GameObject sfxObject = GameObject.Find("Crypt Knight SFX");
            if (sfxObject != null)
            {
                UnityEngine.Object.DestroyImmediate(sfxObject);
            }
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
            Assert.That(heartItem.Description, Is.EqualTo("Gain 1 max heart."));
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
                Assert.That(item.CanAppearFrom(LootSourceType.RoomClear), Is.True, item.ItemId);
                Assert.That(item.CanAppearFrom(LootSourceType.Shop), Is.True, item.ItemId);
            }
        }

        [Test]
        public void DefaultDropRatesAreSet()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(0.10f));
            Assert.That(configuration.GetDropRate(LootSourceType.Chest), Is.EqualTo(1f));
            Assert.That(configuration.GetDropRate(LootSourceType.RoomClear), Is.EqualTo(0.20f));
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
        public void ItemRollWrapsAround()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult negativeRoll = lootSystem.RollDrop(LootSourceType.Chest, 0f, -1);
            LootDropResult largeRoll = lootSystem.RollDrop(LootSourceType.Chest, 0f, 99);

            Assert.That(negativeRoll.HasDrop, Is.True);
            Assert.That(largeRoll.HasDrop, Is.True);
        }

        [Test]
        public void FilteredDropsSkipItems()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult result = lootSystem.RollDrop(LootSourceType.Chest, 0f, 99, item => item.ItemId != "key");

            Assert.That(result.HasDrop, Is.True);
            Assert.That(result.Item.ItemId, Is.Not.EqualTo("key"));
        }

        [Test]
        public void FilteredDropsCanReturnNothing()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult result = lootSystem.RollDrop(LootSourceType.Chest, 0f, 0, _ => false);

            Assert.That(result.HasDrop, Is.False);
            Assert.That(result.Item, Is.Null);
        }

        [Test]
        public void MissingSourceDropsNothing()
        {
            LootSystem lootSystem = new LootSystem(new LootTableConfiguration(
                Array.Empty<LootItemDefinition>(),
                new System.Collections.Generic.Dictionary<LootSourceType, float>
                {
                    { LootSourceType.Enemy, 1f }
                }));

            LootDropResult result = lootSystem.RollDrop(LootSourceType.Enemy, new System.Random(12345));

            Assert.That(result.HasDrop, Is.False);
        }

        [Test]
        public void RandomRollNeedsRandom()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            Assert.Throws<ArgumentNullException>(() => lootSystem.RollDrop(LootSourceType.Enemy, null));
        }

        [Test]
        public void NoDropHasNoItem()
        {
            LootDropResult result = LootDropResult.NoDrop();

            Assert.That(result.HasDrop, Is.False);
            Assert.That(result.Item, Is.Null);
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
        public void EmptyJsonCreatesEmptyTable()
        {
            LootTableConfiguration configuration = LootTableConfiguration.FromJson(string.Empty);

            Assert.That(configuration.Items, Is.Empty);
            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(0f));
        }

        [Test]
        public void InvalidConfigValuesAreIgnored()
        {
            string json = @"{
  ""sourceDropRates"": [
    { ""source"": ""Enemy"", ""dropRate"": 2.5 },
    { ""source"": ""Chest"", ""dropRate"": -1 },
    { ""source"": ""Missing"", ""dropRate"": 1 }
  ],
  ""items"": [
    { ""itemId"": """", ""displayName"": ""Bad Item"", ""allowedSources"": [""Enemy""] },
    { ""itemId"": ""valid_item"", ""displayName"": """", ""keyAmount"": -3, ""allowedSources"": [""Shop"", ""Unknown""] }
  ]
}";

            LootTableConfiguration configuration = LootTableConfiguration.FromJson(json);
            LootItemDefinition item = configuration.Items.Single();

            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(1f));
            Assert.That(configuration.GetDropRate(LootSourceType.Chest), Is.EqualTo(0f));
            Assert.That(item.DisplayName, Is.EqualTo("valid_item"));
            Assert.That(item.Description, Is.EqualTo(string.Empty));
            Assert.That(item.KeyAmount, Is.EqualTo(0));
            Assert.That(item.CanAppearFrom(LootSourceType.Shop), Is.True);
            Assert.That(item.CanAppearFrom(LootSourceType.Enemy), Is.False);
        }

        [Test]
        public void ItemDefinitionNormalizesFields()
        {
            LootItemDefinition item = new LootItemDefinition(
                "test_item",
                string.Empty,
                null,
                null,
                null,
                null,
                keyAmount: -2);

            Assert.That(item.ItemId, Is.EqualTo("test_item"));
            Assert.That(item.DisplayName, Is.EqualTo("test_item"));
            Assert.That(item.Description, Is.EqualTo(string.Empty));
            Assert.That(item.IconAssetPath, Is.EqualTo(string.Empty));
            Assert.That(item.KeyAmount, Is.EqualTo(0));
            Assert.That(item.StatModifier, Is.Not.Null);
            Assert.That(item.AllowedSources, Is.Empty);
        }

        [Test]
        public void ItemDefinitionNeedsId()
        {
            Assert.Throws<ArgumentException>(() => new LootItemDefinition(string.Empty, "Bad", string.Empty, new PlayerStatModifier(), Array.Empty<LootSourceType>()));
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
        public void CollectorRejectsMissingInput()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, new PlayerBaseStats(6, 1, 5f, 1f));
            LootItemDefinition item = new LootItemDefinition("test_item", "Test", string.Empty, new PlayerStatModifier(), new[] { LootSourceType.Chest });

            Assert.That(LootItemCollector.ApplyToRun(null, item), Is.False);
            Assert.That(LootItemCollector.ApplyToRun(runState, null), Is.False);
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
        public void DefaultStatItemsUseConfiguredArtwork()
        {
            string[] itemIds =
            {
                "heart_container",
                "damage_up",
                "speed_up",
                "attack_rate_up"
            };

            foreach (string itemId in itemIds)
            {
                Sprite sprite = LootItemVisuals.GetItemSprite(itemId);
                Assert.That(sprite, Is.Not.Null, itemId);
                Assert.That(sprite.rect.width, Is.GreaterThan(64f), itemId);
                Assert.That(sprite.rect.height, Is.GreaterThan(64f), itemId);
            }
        }

        [Test]
        public void EffectTextShowsStatBonuses()
        {
            LootItemDefinition item = new LootItemDefinition(
                "mixed_relic",
                "Mixed Relic",
                string.Empty,
                new PlayerStatModifier(maxHealthBonus: 2, damageBonus: 1, movementSpeedBonus: 1f, attackRateBonus: 0.2f),
                new[] { LootSourceType.Chest });

            string effectText = LootItemEffectFormatter.FormatEffects(item);

            Assert.That(effectText, Does.Contain("+1 max heart"));
            Assert.That(effectText, Does.Contain("+1 damage"));
            Assert.That(effectText, Does.Contain("+1 movement speed"));
            Assert.That(effectText, Does.Contain("+0.2 attack speed"));
        }

        [Test]
        public void EffectTextUsesStackQuantity()
        {
            LootItemDefinition item = new LootItemDefinition(
                "speed_up",
                "Speed Up",
                string.Empty,
                new PlayerStatModifier(movementSpeedBonus: 1f),
                new[] { LootSourceType.Chest });

            string effectText = LootItemEffectFormatter.FormatEffects(item, 3);

            Assert.That(effectText, Is.EqualTo("+3 movement speed"));
        }

        [Test]
        public void EffectTextClampsBadQuantity()
        {
            LootItemDefinition item = new LootItemDefinition(
                "key",
                "Key",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest },
                keyAmount: 1);

            string effectText = LootItemEffectFormatter.FormatEffects(item, 0);

            Assert.That(effectText, Is.EqualTo("+1 key"));
        }

        [Test]
        public void EffectTextHandlesEmptyItems()
        {
            LootItemDefinition item = new LootItemDefinition(
                "plain_rock",
                "Plain Rock",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });

            Assert.That(LootItemEffectFormatter.FormatEffects(null), Is.EqualTo("No effect"));
            Assert.That(LootItemEffectFormatter.FormatEffects(item), Is.EqualTo("No effect"));
        }

        [Test]
        public void EffectTextShowsNegativeBonuses()
        {
            LootItemDefinition item = new LootItemDefinition(
                "cursed_relic",
                "Cursed Relic",
                string.Empty,
                new PlayerStatModifier(maxHealthBonus: -2, damageBonus: -1, movementSpeedBonus: -0.5f, attackRateBonus: -0.1f),
                new[] { LootSourceType.Chest });

            string effectText = LootItemEffectFormatter.FormatEffects(item);

            Assert.That(effectText, Does.Contain("-1 max heart"));
            Assert.That(effectText, Does.Contain("-1 damage"));
            Assert.That(effectText, Does.Contain("-0.5 movement speed"));
            Assert.That(effectText, Does.Contain("-0.1 attack speed"));
        }

        [Test]
        public void GeneratedSpritesAreCached()
        {
            Assert.That(LootItemVisuals.GetSquareSprite(), Is.SameAs(LootItemVisuals.GetSquareSprite()));
            Assert.That(LootItemVisuals.GetCircleSprite("unknown_item"), Is.SameAs(LootItemVisuals.GetCircleSprite("unknown_item")));
        }

        [Test]
        public void PickupVisualIsScaledDownWithoutShrinkingInteractionRange()
        {
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });

            LootPickup pickup = CreatePickup(item);
            CircleCollider2D pickupCollider = pickup.GetComponent<CircleCollider2D>();

            Assert.That(pickup.transform.localScale.x, Is.EqualTo(0.28f).Within(0.001f));
            Assert.That(pickup.transform.localScale.y, Is.EqualTo(0.28f).Within(0.001f));
            Assert.That(pickupCollider.radius * pickup.transform.localScale.x, Is.EqualTo(1.05f).Within(0.001f));
        }

        [Test]
        public void KeyPickupVisualIsLargerWithoutShrinkingInteractionRange()
        {
            LootItemDefinition normalItem = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });
            LootItemDefinition keyItem = new LootItemDefinition(
                "key",
                "Key",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest },
                keyAmount: 1);

            LootPickup normalPickup = CreatePickup(normalItem);
            LootPickup keyPickup = CreatePickup(keyItem);
            CircleCollider2D keyCollider = keyPickup.GetComponent<CircleCollider2D>();

            Assert.That(keyPickup.transform.localScale.x, Is.GreaterThan(normalPickup.transform.localScale.x));
            Assert.That(keyPickup.transform.localScale.y, Is.GreaterThan(normalPickup.transform.localScale.y));
            Assert.That(keyCollider.radius * keyPickup.transform.localScale.x, Is.EqualTo(1.05f).Within(0.001f));
        }

        [Test]
        public void PickupDrawsUnderPlayer()
        {
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });

            LootPickup pickup = CreatePickup(item);
            SpriteRenderer pickupRenderer = pickup.GetComponent<SpriteRenderer>();

            Assert.That(pickupRenderer.sortingOrder, Is.LessThan(10));
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

        [Test]
        public void PickupCollectsOnce()
        {
            CryptKnight.Application.GameManager.Instance.StartNewRun();
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(damageBonus: 1),
                new[] { LootSourceType.Chest });
            int collectedCount = 0;
            LootPickup pickup = CreatePickup(item, _ => collectedCount++);

            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            bool firstPickup = pickup.TryPickUp();
            bool secondPickup = pickup.TryPickUp();

            Assert.That(firstPickup, Is.True);
            Assert.That(secondPickup, Is.False);
            Assert.That(collectedCount, Is.EqualTo(1));
            Assert.That(CryptKnight.Application.GameManager.Instance.CurrentRun.CollectedItems.Single().ItemId, Is.EqualTo("test_relic"));
        }

        [Test]
        public void OnePressPicksClosestItem()
        {
            CryptKnight.Application.GameManager.Instance.StartNewRun();
            LootItemDefinition nearItem = new LootItemDefinition(
                "near_relic",
                "Near Relic",
                string.Empty,
                new PlayerStatModifier(damageBonus: 1),
                new[] { LootSourceType.Chest });
            LootItemDefinition farItem = new LootItemDefinition(
                "far_relic",
                "Far Relic",
                string.Empty,
                new PlayerStatModifier(damageBonus: 1),
                new[] { LootSourceType.Chest });
            int nearCollected = 0;
            int farCollected = 0;
            LootPickup nearPickup = CreatePickup(nearItem, _ => nearCollected++);
            LootPickup farPickup = CreatePickup(farItem, _ => farCollected++);
            nearPickup.transform.position = new Vector2(0.2f, 0f);
            farPickup.transform.position = new Vector2(0.9f, 0f);
            Collider2D playerCollider = CreatePlayerCollider();
            playerCollider.transform.position = Vector2.zero;
            InvokeTrigger(nearPickup, "OnTriggerEnter2D", playerCollider);
            InvokeTrigger(farPickup, "OnTriggerEnter2D", playerCollider);

            bool farPicked = farPickup.TryPickUpForPlayer(playerCollider.transform);
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            bool nearPicked = nearPickup.TryPickUpForPlayer(playerCollider.transform);
            bool secondFarAttempt = farPickup.TryPickUpForPlayer(playerCollider.transform);

            Assert.That(farPicked, Is.False);
            Assert.That(nearPicked, Is.True);
            Assert.That(secondFarAttempt, Is.False);
            Assert.That(nearCollected, Is.EqualTo(1));
            Assert.That(farCollected, Is.EqualTo(0));
            Assert.That(CryptKnight.Application.GameManager.Instance.CurrentRun.CollectedItems.Single().ItemId, Is.EqualTo("near_relic"));
        }

        [Test]
        public void PickupDoesNotCollectWithoutRun()
        {
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });
            int collectedCount = 0;
            LootPickup pickup = CreatePickup(item, _ => collectedCount++);

            bool pickedUp = pickup.TryPickUp();

            Assert.That(pickedUp, Is.False);
            Assert.That(collectedCount, Is.EqualTo(0));
        }

        [Test]
        public void PickupRejectsMissingItem()
        {
            CryptKnight.Application.GameManager.Instance.StartNewRun();
            LootPickup pickup = CreatePickup(null);

            bool pickedUp = pickup.TryPickUp();

            Assert.That(pickedUp, Is.False);
        }

        private LootPickup CreatePickup(LootItemDefinition item, Action<LootPickup> onCollected = null)
        {
            GameObject pickupObject = new GameObject("Test Pickup");
            createdObjects.Add(pickupObject);

            pickupObject.AddComponent<SpriteRenderer>();
            pickupObject.AddComponent<CircleCollider2D>();
            LootPickup pickup = pickupObject.AddComponent<LootPickup>();
            pickup.Initialize(item, onCollected);
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
