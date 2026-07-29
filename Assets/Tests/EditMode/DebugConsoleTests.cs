#if UNITY_EDITOR
using CryptKnight.Application;
using CryptKnight.Loot;
using CryptKnight.UI;
using NUnit.Framework;

namespace CryptKnight.Tests.EditMode
{
    public sealed class DebugConsoleTests
    {
        [TearDown]
        public void TearDown()
        {
            GameplayInputGate.SetBlocked(false);
        }

        [Test]
        public void DebugConsoleFindsItemIds()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            LootItemDefinition item = EditorItemDebugConsole.ResolveItem(configuration.Items, "bloody_knife");

            Assert.That(item, Is.Not.Null);
            Assert.That(item.DisplayName, Is.EqualTo("Bloody Knife"));
        }

        [Test]
        public void DebugConsoleFindsItemNames()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            LootItemDefinition item = EditorItemDebugConsole.ResolveItem(configuration.Items, "watchers eye");

            Assert.That(item, Is.Not.Null);
            Assert.That(item.ItemId, Is.EqualTo("watchers_eye"));
        }

        [Test]
        public void DebugConsoleRejectsUnknownItems()
        {
            LootTableConfiguration configuration = LootTableConfiguration.CreateDefault();

            Assert.That(EditorItemDebugConsole.ResolveItem(configuration.Items, "missing item"), Is.Null);
            Assert.That(EditorItemDebugConsole.ResolveItem(configuration.Items, " "), Is.Null);
            Assert.That(EditorItemDebugConsole.ResolveItem(null, "key"), Is.Null);
        }

        [Test]
        public void RandomCommandNeedsPositiveNumber()
        {
            Assert.That(EditorItemDebugConsole.TryParseRandomCount("5", out int count), Is.True);
            Assert.That(count, Is.EqualTo(5));
            Assert.That(EditorItemDebugConsole.TryParseRandomCount("0", out _), Is.False);
            Assert.That(EditorItemDebugConsole.TryParseRandomCount("-2", out _), Is.False);
            Assert.That(EditorItemDebugConsole.TryParseRandomCount("many", out _), Is.False);
        }

        [Test]
        public void SpawnCommandReadsQuantity()
        {
            bool parsed = EditorItemDebugConsole.TryParseSpawnRequest(
                "Bloody Knife 5",
                out string itemQuery,
                out int quantity);

            Assert.That(parsed, Is.True);
            Assert.That(itemQuery, Is.EqualTo("Bloody Knife"));
            Assert.That(quantity, Is.EqualTo(5));
        }

        [Test]
        public void SpawnCommandDefaultsToOne()
        {
            bool parsed = EditorItemDebugConsole.TryParseSpawnRequest(
                "watchers eye",
                out string itemQuery,
                out int quantity);

            Assert.That(parsed, Is.True);
            Assert.That(itemQuery, Is.EqualTo("watchers eye"));
            Assert.That(quantity, Is.EqualTo(1));
        }

        [Test]
        public void SpawnCommandNeedsPositiveQuantity()
        {
            Assert.That(EditorItemDebugConsole.TryParseSpawnRequest("Bloody Knife 0", out _, out _), Is.False);
            Assert.That(EditorItemDebugConsole.TryParseSpawnRequest("Bloody Knife -2", out _, out _), Is.False);
            Assert.That(EditorItemDebugConsole.TryParseSpawnRequest(" ", out _, out _), Is.False);
        }

        [Test]
        public void DebugConsoleCanBlockGameplayInput()
        {
            GameplayInputGate.SetBlocked(true);

            Assert.That(GameplayInputGate.IsBlocked, Is.True);

            GameplayInputGate.SetBlocked(false);

            Assert.That(GameplayInputGate.IsBlocked, Is.False);
        }

        [Test]
        public void HudItemsWrapIntoRows()
        {
            Assert.That(RunHudItemLayout.GetItemPosition(8).x, Is.EqualTo(RunHudItemLayout.GetItemPosition(0).x));
            Assert.That(RunHudItemLayout.GetItemPosition(8).y, Is.LessThan(RunHudItemLayout.GetItemPosition(0).y));
        }

        [Test]
        public void HudPanelGrowsForItems()
        {
            Assert.That(RunHudItemLayout.GetPanelHeight(0), Is.EqualTo(108f));
            Assert.That(RunHudItemLayout.GetPanelHeight(8), Is.EqualTo(108f));
            Assert.That(RunHudItemLayout.GetPanelHeight(9), Is.EqualTo(176f));
            Assert.That(RunHudItemLayout.GetPanelHeight(26), Is.EqualTo(312f));
        }
    }
}
#endif
