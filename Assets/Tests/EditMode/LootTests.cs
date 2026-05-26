using System.Linq;
using CryptKnight.Loot;
using NUnit.Framework;

namespace CryptKnight.Tests.EditMode
{
    public sealed class LootTests
    {
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
    }
}
