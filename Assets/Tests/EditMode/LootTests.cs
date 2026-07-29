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

            Assert.That(configuration.Items, Has.Count.EqualTo(26));

            LootItemDefinition heartItem = configuration.Items.Single(item => item.ItemId == "heart_container");
            LootItemDefinition damageItem = configuration.Items.Single(item => item.ItemId == "damage_up");
            LootItemDefinition speedItem = configuration.Items.Single(item => item.ItemId == "speed_up");
            LootItemDefinition attackRateItem = configuration.Items.Single(item => item.ItemId == "attack_rate_up");
            LootItemDefinition keyItem = configuration.Items.Single(item => item.ItemId == "key");

            Assert.That(heartItem.StatModifier.MaxHealthBonus, Is.EqualTo(2));
            Assert.That(heartItem.Description, Is.EqualTo("It still beats with stubborn, impossible life."));
            Assert.That(heartItem.IconAssetPath, Is.EqualTo("Art/Items/heart_container"));
            Assert.That(damageItem.StatModifier.DamageBonus, Is.EqualTo(1f));
            Assert.That(speedItem.StatModifier.MovementSpeedBonus, Is.EqualTo(1f));
            Assert.That(attackRateItem.StatModifier.AttackRateBonus, Is.EqualTo(0.2f));
            Assert.That(keyItem.KeyAmount, Is.EqualTo(1));
            Assert.That(keyItem.IconAssetPath, Is.EqualTo("Art/Items/key"));
        }

        [Test]
        public void NewItemsLoad()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            foreach (string itemId in GetNewItemIds())
            {
                LootItemDefinition item = configuration.Items.Single(definition => definition.ItemId == itemId);
                Assert.That(item.Description, Is.Not.Empty, itemId);
                Assert.That(item.IconAssetPath, Is.EqualTo("Art/Items/items_sheet_no_numbers_alpha"), itemId);
            }
        }

        [Test]
        public void NewItemsHaveConfiguredEffects()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();
            LootItemDefinition knife = configuration.Items.Single(item => item.ItemId == "bloody_knife");
            LootItemDefinition skull = configuration.Items.Single(item => item.ItemId == "forgotten_skull");
            LootItemDefinition book = configuration.Items.Single(item => item.ItemId == "book_of_the_crypt");
            LootItemDefinition amethyst = configuration.Items.Single(item => item.ItemId == "cracked_amethyst");
            LootItemDefinition skeletonKey = configuration.Items.Single(item => item.ItemId == "skeleton_key");
            LootItemDefinition bloodChalice = configuration.Items.Single(item => item.ItemId == "blood_chalice");
            LootItemDefinition suspiciousPile = configuration.Items.Single(item => item.ItemId == "suspicious_pile");
            LootItemDefinition pinCushion = configuration.Items.Single(item => item.ItemId == "pin_cushion");
            LootItemDefinition borrowedTime = configuration.Items.Single(item => item.ItemId == "borrowed_time");

            Assert.That(knife.StatModifier.DamageBonus, Is.EqualTo(2f));
            Assert.That(knife.StatModifier.AttackRateBonus, Is.EqualTo(-0.3f));
            Assert.That(skull.StatModifier.ProjectileBouncesBonus, Is.EqualTo(1));
            Assert.That(skull.StatModifier.ProjectileSpeedBonus, Is.EqualTo(-1f));
            Assert.That(book.StatModifier.ProjectileCountBonus, Is.EqualTo(1));
            Assert.That(amethyst.StatModifier.ProjectileSizeBonus, Is.EqualTo(0.35f));
            Assert.That(skeletonKey.KeyAmount, Is.EqualTo(3));
            Assert.That(skeletonKey.StatModifier.ProjectileBouncesBonus, Is.EqualTo(0));
            Assert.That(bloodChalice.StatModifier.DamageBonus, Is.EqualTo(0.5f));
            Assert.That(suspiciousPile.StatModifier.DamageBonus, Is.EqualTo(-0.5f));
            Assert.That(suspiciousPile.StatModifier.MaxHealthBonus, Is.EqualTo(4));
            Assert.That(pinCushion.StatModifier.ProjectileCountBonus, Is.EqualTo(2));
            Assert.That(borrowedTime.StatModifier.MaxHealthBonus, Is.EqualTo(-4));
            Assert.That(borrowedTime.StatModifier.DamageBonus, Is.EqualTo(2f));
            Assert.That(borrowedTime.StatModifier.AttackRateBonus, Is.EqualTo(0.5f));
        }

        [Test]
        public void GarlicAndCandleMatchConfig()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();
            LootItemDefinition garlic = configuration.Items.Single(item => item.ItemId == "garlic");
            LootItemDefinition candle = configuration.Items.Single(item => item.ItemId == "eternal_candle");

            Assert.That(garlic.StatModifier.MaxHealthBonus, Is.EqualTo(4));
            Assert.That(garlic.StatModifier.MovementSpeedBonus, Is.EqualTo(-0.5f));
            Assert.That(candle.StatModifier.AttackRateBonus, Is.EqualTo(0.35f));
            Assert.That(candle.StatModifier.ProjectileSizeBonus, Is.EqualTo(-0.10f));
        }

        [Test]
        public void DescriptionsUseFlavorText()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            foreach (LootItemDefinition item in configuration.Items)
            {
                Assert.That(item.Description, Is.Not.Empty, item.ItemId);
                Assert.That(item.Description, Does.Not.Match(@"\d"), item.ItemId);
                Assert.That(
                    item.Description,
                    Does.Not.Match(@"(?i)\b(increase|decrease|gain|add|lose)\b"),
                    item.ItemId);
            }
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
        public void EnemyDropRateIsEightPercent()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(0.08f));
            Assert.That(configuration.GetDropRate(LootSourceType.Chest), Is.EqualTo(1f));
            Assert.That(configuration.GetDropRate(LootSourceType.RoomClear), Is.EqualTo(0.20f));
            Assert.That(configuration.GetDropRate(LootSourceType.Shop), Is.EqualTo(1f));
        }

        [Test]
        public void ItemsHaveExpectedRarity()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            AssertRarity(
                configuration,
                LootRarity.Common,
                "heart_container",
                "damage_up",
                "speed_up",
                "attack_rate_up",
                "key");
            AssertRarity(
                configuration,
                LootRarity.Uncommon,
                "forgotten_skull",
                "cracked_amethyst",
                "eternal_candle",
                "treasure_map",
                "garlic",
                "boo_beans",
                "brine_brain",
                "dread_knight_helm",
                "suspicious_pile");
            AssertRarity(
                configuration,
                LootRarity.Rare,
                "skeleton_key",
                "bloody_knife",
                "book_of_the_crypt",
                "dead_mans_coin",
                "blood_chalice",
                "cloak_of_shadows",
                "chimkin",
                "blooshroom",
                "watchers_eye");
            AssertRarity(
                configuration,
                LootRarity.Legendary,
                "lucky_dice",
                "pin_cushion",
                "borrowed_time");
        }

        [Test]
        public void RarityTiersAreBalanced()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            Assert.That(configuration.Items.Count(item => item.Rarity == LootRarity.Common), Is.EqualTo(5));
            Assert.That(configuration.Items.Count(item => item.Rarity == LootRarity.Uncommon), Is.EqualTo(9));
            Assert.That(configuration.Items.Count(item => item.Rarity == LootRarity.Rare), Is.EqualTo(9));
            Assert.That(configuration.Items.Count(item => item.Rarity == LootRarity.Legendary), Is.EqualTo(3));
        }

        [Test]
        public void DefaultRarityWeightsAreSet()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();
            LootRarityWeights enemy = configuration.GetRarityWeights(LootSourceType.Enemy);
            LootRarityWeights chest = configuration.GetRarityWeights(LootSourceType.Chest);

            Assert.That(enemy.Common, Is.EqualTo(0.60f));
            Assert.That(enemy.Uncommon, Is.EqualTo(0.27f));
            Assert.That(enemy.Rare, Is.EqualTo(0.10f));
            Assert.That(enemy.Legendary, Is.EqualTo(0.03f));
            Assert.That(chest.Common, Is.EqualTo(0.40f));
            Assert.That(chest.Uncommon, Is.EqualTo(0.35f));
            Assert.That(chest.Rare, Is.EqualTo(0.20f));
            Assert.That(chest.Legendary, Is.EqualTo(0.05f));
        }

        [Test]
        public void RarityRollChoosesTier()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            Assert.That(lootSystem.RollDrop(LootSourceType.Enemy, 0f, 0.20f, 0).Item.Rarity, Is.EqualTo(LootRarity.Common));
            Assert.That(lootSystem.RollDrop(LootSourceType.Enemy, 0f, 0.70f, 0).Item.Rarity, Is.EqualTo(LootRarity.Uncommon));
            Assert.That(lootSystem.RollDrop(LootSourceType.Enemy, 0f, 0.90f, 0).Item.Rarity, Is.EqualTo(LootRarity.Rare));
            Assert.That(lootSystem.RollDrop(LootSourceType.Enemy, 0f, 0.99f, 0).Item.Rarity, Is.EqualTo(LootRarity.Legendary));
        }

        [Test]
        public void ChestUsesBetterRarityOdds()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult enemy = lootSystem.RollDrop(LootSourceType.Enemy, 0f, 0.50f, 0);
            LootDropResult chest = lootSystem.RollDrop(LootSourceType.Chest, 0f, 0.50f, 0);

            Assert.That(enemy.Item.Rarity, Is.EqualTo(LootRarity.Common));
            Assert.That(chest.Item.Rarity, Is.EqualTo(LootRarity.Uncommon));
        }

        [Test]
        public void ItemsAreEvenWithinTier()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult first = lootSystem.RollDrop(LootSourceType.Chest, 0f, 0.90f, 0);
            LootDropResult wrapped = lootSystem.RollDrop(LootSourceType.Chest, 0f, 0.90f, 9);

            Assert.That(first.Item.Rarity, Is.EqualTo(LootRarity.Rare));
            Assert.That(wrapped.Item.ItemId, Is.EqualTo(first.Item.ItemId));
        }

        [Test]
        public void EmptyTiersAreSkipped()
        {
            LootItemDefinition common = CreateDefinition("common", LootRarity.Common);
            LootItemDefinition rare = CreateDefinition("rare", LootRarity.Rare);
            LootTableConfiguration configuration = new LootTableConfiguration(
                new[] { common, rare },
                new System.Collections.Generic.Dictionary<LootSourceType, float>
                {
                    { LootSourceType.Chest, 1f }
                },
                new System.Collections.Generic.Dictionary<LootSourceType, LootRarityWeights>
                {
                    { LootSourceType.Chest, new LootRarityWeights(0.6f, 0.3f, 0.1f, 0f) }
                });
            LootSystem lootSystem = new LootSystem(configuration);

            LootDropResult result = lootSystem.RollDrop(
                LootSourceType.Chest,
                0f,
                0f,
                0,
                item => item.Rarity == LootRarity.Rare);

            Assert.That(result.Item, Is.SameAs(rare));
        }

        [Test]
        public void MissingWeightsStillDrop()
        {
            LootItemDefinition rare = CreateDefinition("rare", LootRarity.Rare);
            LootSystem lootSystem = new LootSystem(new LootTableConfiguration(
                new[] { rare },
                new System.Collections.Generic.Dictionary<LootSourceType, float>
                {
                    { LootSourceType.Chest, 1f }
                }));

            LootDropResult result = lootSystem.RollDrop(LootSourceType.Chest, 0f, 0.5f, 0);

            Assert.That(result.Item, Is.SameAs(rare));
        }

        [Test]
        public void BadRarityRollUsesCommon()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult notANumber = lootSystem.RollDrop(LootSourceType.Chest, 0f, float.NaN, 0);
            LootDropResult infinity = lootSystem.RollDrop(LootSourceType.Chest, 0f, float.PositiveInfinity, 0);
            LootDropResult negative = lootSystem.RollDrop(LootSourceType.Chest, 0f, -0.10f, 0);

            Assert.That(notANumber.Item.Rarity, Is.EqualTo(LootRarity.Common));
            Assert.That(infinity.Item.Rarity, Is.EqualTo(LootRarity.Common));
            Assert.That(negative.Item.Rarity, Is.EqualTo(LootRarity.Rare));
        }

        [Test]
        public void RarityPresentationMatchesTier()
        {
            Assert.That(LootRarityPresentation.GetLabel(LootRarity.Common), Is.EqualTo("Common"));
            Assert.That(LootRarityPresentation.GetLabel(LootRarity.Legendary), Is.EqualTo("Legendary"));
            Assert.That(LootRarityPresentation.GetColor(LootRarity.Common), Is.Not.EqualTo(LootRarityPresentation.GetColor(LootRarity.Uncommon)));
            Assert.That(LootRarityPresentation.GetColor(LootRarity.Uncommon), Is.Not.EqualTo(LootRarityPresentation.GetColor(LootRarity.Rare)));
            Assert.That(LootRarityPresentation.GetColor(LootRarity.Rare), Is.Not.EqualTo(LootRarityPresentation.GetColor(LootRarity.Legendary)));
        }

        [Test]
        public void EnemyDropChanceWorks()
        {
            LootSystem lootSystem = new LootSystem(LootTableConfiguration.CreateDefault());

            LootDropResult failedRoll = lootSystem.RollDrop(LootSourceType.Enemy, 0.08f, 0);
            LootDropResult successfulRoll = lootSystem.RollDrop(LootSourceType.Enemy, 0.079f, 0);

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
  ""sourceRarityWeights"": [
    { ""source"": ""Enemy"", ""common"": 0.4, ""uncommon"": 0.3, ""rare"": 0.2, ""legendary"": 0.1 }
  ],
  ""items"": [
    {
      ""itemId"": ""test_item"",
      ""displayName"": ""Test Item"",
      ""description"": ""Used by tests to prove config parsing works."",
      ""iconAssetPath"": ""Art/Items/test_item"",
      ""keyAmount"": 3,
      ""rarity"": ""Rare"",
      ""allowedSources"": [""Enemy""],
      ""statModifier"": {
        ""maxHealthBonus"": 0,
        ""damageBonus"": 2,
        ""movementSpeedBonus"": 0.5,
        ""attackRateBonus"": 0.1,
        ""projectileCountBonus"": 2,
        ""projectileSpeedBonus"": 1.5,
        ""projectileBouncesBonus"": 1,
        ""projectileSizeBonus"": 0.25
      }
    }
  ]
}";

            LootTableConfiguration configuration = LootTableConfiguration.FromJson(json);

            Assert.That(configuration.Items, Has.Count.EqualTo(1));
            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(0.25f));
            Assert.That(configuration.GetDropRate(LootSourceType.Chest), Is.EqualTo(0f));
            Assert.That(configuration.GetRarityWeights(LootSourceType.Enemy).Rare, Is.EqualTo(0.2f));
            Assert.That(configuration.GetRarityWeights(LootSourceType.Chest).Common, Is.EqualTo(1f));

            LootItemDefinition item = configuration.Items[0];
            Assert.That(item.ItemId, Is.EqualTo("test_item"));
            Assert.That(item.DisplayName, Is.EqualTo("Test Item"));
            Assert.That(item.Description, Is.EqualTo("Used by tests to prove config parsing works."));
            Assert.That(item.IconAssetPath, Is.EqualTo("Art/Items/test_item"));
            Assert.That(item.KeyAmount, Is.EqualTo(3));
            Assert.That(item.Rarity, Is.EqualTo(LootRarity.Rare));
            Assert.That(item.CanAppearFrom(LootSourceType.Enemy), Is.True);
            Assert.That(item.CanAppearFrom(LootSourceType.Chest), Is.False);
            Assert.That(item.StatModifier.DamageBonus, Is.EqualTo(2f));
            Assert.That(item.StatModifier.MovementSpeedBonus, Is.EqualTo(0.5f));
            Assert.That(item.StatModifier.AttackRateBonus, Is.EqualTo(0.1f));
            Assert.That(item.StatModifier.ProjectileCountBonus, Is.EqualTo(2));
            Assert.That(item.StatModifier.ProjectileSpeedBonus, Is.EqualTo(1.5f));
            Assert.That(item.StatModifier.ProjectileBouncesBonus, Is.EqualTo(1));
            Assert.That(item.StatModifier.ProjectileSizeBonus, Is.EqualTo(0.25f));
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
  ""sourceRarityWeights"": [
    { ""source"": ""Enemy"", ""common"": -1, ""uncommon"": 2, ""rare"": -3, ""legendary"": 4 }
  ],
  ""items"": [
    { ""itemId"": """", ""displayName"": ""Bad Item"", ""allowedSources"": [""Enemy""] },
    { ""itemId"": ""valid_item"", ""displayName"": """", ""keyAmount"": -3, ""rarity"": ""Missing"", ""allowedSources"": [""Shop"", ""Unknown""] }
  ]
}";

            LootTableConfiguration configuration = LootTableConfiguration.FromJson(json);
            LootItemDefinition item = configuration.Items.Single();

            Assert.That(configuration.GetDropRate(LootSourceType.Enemy), Is.EqualTo(1f));
            Assert.That(configuration.GetDropRate(LootSourceType.Chest), Is.EqualTo(0f));
            Assert.That(configuration.GetRarityWeights(LootSourceType.Enemy).Common, Is.Zero);
            Assert.That(configuration.GetRarityWeights(LootSourceType.Enemy).Uncommon, Is.EqualTo(2f));
            Assert.That(configuration.GetRarityWeights(LootSourceType.Enemy).Rare, Is.Zero);
            Assert.That(configuration.GetRarityWeights(LootSourceType.Enemy).Legendary, Is.EqualTo(4f));
            Assert.That(item.DisplayName, Is.EqualTo("valid_item"));
            Assert.That(item.Description, Is.EqualTo(string.Empty));
            Assert.That(item.KeyAmount, Is.EqualTo(0));
            Assert.That(item.Rarity, Is.EqualTo(LootRarity.Common));
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
            Assert.That(item.Rarity, Is.EqualTo(LootRarity.Common));
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
                new PlayerStatModifier(
                    maxHealthBonus: 2,
                    damageBonus: 3,
                    projectileCountBonus: 1,
                    projectileSpeedBonus: 2f,
                    projectileBouncesBonus: 1,
                    projectileSizeBonus: 0.25f),
                new[] { LootSourceType.Chest },
                keyAmount: 2);

            bool applied = LootItemCollector.ApplyToRun(runState, item);

            Assert.That(applied, Is.True);
            Assert.That(runState.MaxHealth, Is.EqualTo(8));
            Assert.That(runState.CurrentHealth, Is.EqualTo(8));
            Assert.That(runState.PlayerStats.Damage, Is.EqualTo(4f));
            Assert.That(runState.PlayerStats.ProjectileCount, Is.EqualTo(2));
            Assert.That(runState.PlayerStats.ProjectileSpeed, Is.EqualTo(10f));
            Assert.That(runState.PlayerStats.ProjectileBounces, Is.EqualTo(1));
            Assert.That(runState.PlayerStats.ProjectileSizeMultiplier, Is.EqualTo(1.25f));
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
        public void ItemSpritesLoad()
        {
            foreach (string itemId in GetNewItemIds())
            {
                Sprite sprite = LootItemVisuals.GetItemSprite(itemId);
                Assert.That(sprite, Is.Not.Null, itemId);
                Assert.That(sprite.name, Is.EqualTo(itemId));
                Assert.That(sprite.rect.width, Is.EqualTo(260f), itemId);
                Assert.That(sprite.rect.height, Is.EqualTo(250f), itemId);
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
        public void NewEffectsAreFormatted()
        {
            LootItemDefinition item = new LootItemDefinition(
                "projectile_relic",
                "Projectile Relic",
                string.Empty,
                new PlayerStatModifier(
                    damageBonus: 0.5f,
                    projectileCountBonus: 2,
                    projectileSpeedBonus: 1.5f,
                    projectileBouncesBonus: 1,
                    projectileSizeBonus: 0.25f),
                new[] { LootSourceType.Chest });

            string effectText = LootItemEffectFormatter.FormatEffects(item);

            Assert.That(effectText, Does.Contain("+0.5 damage"));
            Assert.That(effectText, Does.Contain("+2 projectiles"));
            Assert.That(effectText, Does.Contain("+1.5 projectile speed"));
            Assert.That(effectText, Does.Contain("+1 projectile bounce"));
            Assert.That(effectText, Does.Contain("+25% projectile size"));
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
                new PlayerStatModifier(
                    maxHealthBonus: -2,
                    damageBonus: -1,
                    movementSpeedBonus: -0.5f,
                    attackRateBonus: -0.1f,
                    projectileCountBonus: -1,
                    projectileSpeedBonus: -1f,
                    projectileBouncesBonus: -2,
                    projectileSizeBonus: -0.2f),
                new[] { LootSourceType.Chest });

            string effectText = LootItemEffectFormatter.FormatEffects(item);

            Assert.That(effectText, Does.Contain("-1 max heart"));
            Assert.That(effectText, Does.Contain("-1 damage"));
            Assert.That(effectText, Does.Contain("-0.5 movement speed"));
            Assert.That(effectText, Does.Contain("-0.1 attack speed"));
            Assert.That(effectText, Does.Contain("-1 projectile"));
            Assert.That(effectText, Does.Contain("-1 projectile speed"));
            Assert.That(effectText, Does.Contain("-2 projectile bounces"));
            Assert.That(effectText, Does.Contain("-20% projectile size"));
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
        public void PickupDrawsAboveEnvironmentAndUnderPlayer()
        {
            LootItemDefinition item = new LootItemDefinition(
                "test_relic",
                "Test Relic",
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest });

            LootPickup pickup = CreatePickup(item);
            SpriteRenderer pickupRenderer = pickup.GetComponent<SpriteRenderer>();

            Assert.That(pickupRenderer.sortingOrder, Is.GreaterThan(8));
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
        public void PickupPromptUsesRarityColor()
        {
            LootItemDefinition item = CreateDefinition("rare_relic", LootRarity.Rare);
            LootPickup pickup = CreatePickup(item);
            TextMesh prompt = pickup.GetComponentInChildren<TextMesh>(true);
            Color expectedColor = LootRarityPresentation.GetColor(LootRarity.Rare);

            Assert.That(prompt, Is.Not.Null);
            Assert.That(prompt.color.r, Is.EqualTo(expectedColor.r).Within(0.005f));
            Assert.That(prompt.color.g, Is.EqualTo(expectedColor.g).Within(0.005f));
            Assert.That(prompt.color.b, Is.EqualTo(expectedColor.b).Within(0.005f));
            Assert.That(prompt.color.a, Is.EqualTo(expectedColor.a).Within(0.005f));
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

        private static void AssertRarity(
            LootTableConfiguration configuration,
            LootRarity rarity,
            params string[] expectedItemIds)
        {
            string[] actualItemIds = configuration.Items
                .Where(item => item.Rarity == rarity)
                .Select(item => item.ItemId)
                .ToArray();
            CollectionAssert.AreEquivalent(expectedItemIds, actualItemIds);
        }

        private static LootItemDefinition CreateDefinition(string itemId, LootRarity rarity)
        {
            return new LootItemDefinition(
                itemId,
                itemId,
                string.Empty,
                new PlayerStatModifier(),
                new[] { LootSourceType.Chest },
                rarity: rarity);
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

        private static string[] GetNewItemIds()
        {
            return new[]
            {
                "bloody_knife",
                "forgotten_skull",
                "book_of_the_crypt",
                "cracked_amethyst",
                "skeleton_key",
                "eternal_candle",
                "dead_mans_coin",
                "treasure_map",
                "lucky_dice",
                "blood_chalice",
                "pin_cushion",
                "dread_knight_helm",
                "garlic",
                "boo_beans",
                "brine_brain",
                "suspicious_pile",
                "cloak_of_shadows",
                "chimkin",
                "blooshroom",
                "watchers_eye",
                "borrowed_time"
            };
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
