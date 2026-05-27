using CryptKnight.Data;
using NUnit.Framework;

namespace CryptKnight.Tests.EditMode
{
    public sealed class RunStateTests
    {
        [Test]
        public void NewRunHasDefaults()
        {
            GameRunState runState = GameRunState.CreateNewRun(2, 12345, 4, 4, 6);

            Assert.That(runState.Status, Is.EqualTo(GameRunStatus.Active));
            Assert.That(runState.RunNumber, Is.EqualTo(2));
            Assert.That(runState.Seed, Is.EqualTo(12345));
            Assert.That(runState.DungeonWidth, Is.EqualTo(4));
            Assert.That(runState.DungeonHeight, Is.EqualTo(4));
            Assert.That(runState.CurrentHealth, Is.EqualTo(6));
            Assert.That(runState.MaxHealth, Is.EqualTo(6));
            Assert.That(runState.PlayerStats.Damage, Is.EqualTo(1));
            Assert.That(runState.PlayerStats.MovementSpeed, Is.EqualTo(5f));
            Assert.That(runState.PlayerStats.AttackRate, Is.EqualTo(1f));
            Assert.That(runState.KeyCount, Is.EqualTo(0));
            Assert.That(runState.CollectedItems, Is.Empty);
            Assert.That(runState.IsActive, Is.True);
        }

        [Test]
        public void HealthUsesHalfHearts()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.ApplyDamage(1);
            Assert.That(runState.CurrentHealth, Is.EqualTo(5));

            runState.Heal(10);
            Assert.That(runState.CurrentHealth, Is.EqualTo(6));

            runState.ApplyDamage(99);
            Assert.That(runState.CurrentHealth, Is.EqualTo(0));
            Assert.That(runState.Status, Is.EqualTo(GameRunStatus.Failed));
            Assert.That(runState.IsActive, Is.False);
        }

        [Test]
        public void KeysCanBeSpent()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            Assert.That(runState.SpendKey(), Is.False);

            runState.AddKeys(2);

            Assert.That(runState.KeyCount, Is.EqualTo(2));
            Assert.That(runState.SpendKey(), Is.True);
            Assert.That(runState.KeyCount, Is.EqualTo(1));
        }

        [Test]
        public void ItemsStack()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.AddCollectedItem("damage_up", "Damage", 1);
            runState.AddCollectedItem("damage_up", "Damage", 4);
            runState.AddCollectedItem("speed_up", "Speed", 2);

            Assert.That(runState.CollectedItems, Has.Count.EqualTo(2));
            Assert.That(runState.CollectedItems[0].ItemId, Is.EqualTo("damage_up"));
            Assert.That(runState.CollectedItems[0].Quantity, Is.EqualTo(5));
            Assert.That(runState.CollectedItems[1].ItemId, Is.EqualTo("speed_up"));
            Assert.That(runState.CollectedItems[1].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void StatModifiersApply()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.AddStatModifier(new PlayerStatModifier(maxHealthBonus: 2, damageBonus: 1, movementSpeedBonus: 1.5f, attackRateBonus: 0.5f));

            Assert.That(runState.MaxHealth, Is.EqualTo(8));
            Assert.That(runState.CurrentHealth, Is.EqualTo(8));
            Assert.That(runState.PlayerStats.Damage, Is.EqualTo(2));
            Assert.That(runState.PlayerStats.MovementSpeed, Is.EqualTo(6.5f));
            Assert.That(runState.PlayerStats.AttackRate, Is.EqualTo(1.5f));
            Assert.That(runState.PlayerStats.AttackCooldownSeconds, Is.EqualTo(1f / 1.5f).Within(0.001f));
        }

        [Test]
        public void HealthStaysUnderMax()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.AddStatModifier(new PlayerStatModifier(maxHealthBonus: -2));

            Assert.That(runState.MaxHealth, Is.EqualTo(4));
            Assert.That(runState.CurrentHealth, Is.EqualTo(4));
        }

        [Test]
        public void StatSummaryShowsCurrentValues()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);
            runState.ApplyDamage(1);
            runState.AddKeys(2);
            runState.AddStatModifier(new PlayerStatModifier(damageBonus: 1, movementSpeedBonus: 0.5f, attackRateBonus: 0.2f));

            string summary = PlayerStatSummaryFormatter.Format(runState);

            Assert.That(summary, Does.Contain("Health: 2.5 / 3 hearts"));
            Assert.That(summary, Does.Contain("Damage: 2"));
            Assert.That(summary, Does.Contain("Movement Speed: 5.5"));
            Assert.That(summary, Does.Contain("Attack Speed: 1.2"));
            Assert.That(summary, Does.Contain("Keys: 2"));
        }

        [Test]
        public void StatSummaryCanHideHudValues()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);
            runState.AddKeys(2);
            runState.AddStatModifier(new PlayerStatModifier(damageBonus: 1, movementSpeedBonus: 0.5f, attackRateBonus: 0.2f));

            string summary = PlayerStatSummaryFormatter.FormatStatsOnly(runState);

            Assert.That(summary, Does.Not.Contain("Health"));
            Assert.That(summary, Does.Not.Contain("Keys"));
            Assert.That(summary, Does.Contain("Damage: 2"));
            Assert.That(summary, Does.Contain("Movement Speed: 5.5"));
            Assert.That(summary, Does.Contain("Attack Speed: 1.2"));
        }
    }
}
