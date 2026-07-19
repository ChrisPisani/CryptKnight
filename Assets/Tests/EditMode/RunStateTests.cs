using CryptKnight.Application;
using CryptKnight.Data;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void DefaultStatsMatchStartingPlayer()
        {
            PlayerBaseStats stats = PlayerBaseStats.CreateDefault();

            Assert.That(stats.MaxHealth, Is.EqualTo(6));
            Assert.That(stats.Damage, Is.EqualTo(1));
            Assert.That(stats.MovementSpeed, Is.EqualTo(5f));
            Assert.That(stats.AttackRate, Is.EqualTo(1f));
        }

        [Test]
        public void BadRunUpdatesAreIgnored()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.ApplyDamage(0);
            runState.Heal(-2);
            runState.AddKeys(-5);
            runState.AddCollectedItem(string.Empty, "Missing", 1);
            runState.AddCollectedItem("damage_up", "Damage", 0);
            runState.AddStatModifier(null);

            Assert.That(runState.CurrentHealth, Is.EqualTo(6));
            Assert.That(runState.KeyCount, Is.EqualTo(0));
            Assert.That(runState.CollectedItems, Is.Empty);
            Assert.That(runState.PlayerStats.Modifiers, Is.Empty);
        }

        [Test]
        public void DisplayNameFallsBackToItemId()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.AddCollectedItem("mystery_relic", string.Empty, 2);

            Assert.That(runState.CollectedItems, Has.Count.EqualTo(1));
            Assert.That(runState.CollectedItems[0].DisplayName, Is.EqualTo("mystery_relic"));
            Assert.That(runState.CollectedItems[0].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void RunCanOnlyQuitWhileActive()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.QuitRun();
            runState.ApplyDamage(99);
            runState.Heal(2);

            Assert.That(runState.Status, Is.EqualTo(GameRunStatus.Quit));
            Assert.That(runState.CurrentHealth, Is.EqualTo(6));
        }

        [Test]
        public void CompletedRunCannotChange()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.CompleteRun();
            runState.QuitRun();
            runState.ApplyDamage(99);

            Assert.That(runState.Status, Is.EqualTo(GameRunStatus.Completed));
            Assert.That(runState.CurrentHealth, Is.EqualTo(6));
            Assert.That(runState.IsActive, Is.False);
        }

        [Test]
        public void PlayerDeathEndsRun()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.ApplyDamage(6);

            Assert.That(runState.Status, Is.EqualTo(GameRunStatus.Failed));
            Assert.That(runState.CurrentHealth, Is.Zero);
        }

        [Test]
        public void ManagerEndsCompletedAndFailedRuns()
        {
            GameManager manager = GameManager.Instance;
            try
            {
                manager.StartNewRun();
                manager.CompleteCurrentRun();
                Assert.That(manager.CurrentRun.Status, Is.EqualTo(GameRunStatus.Completed));

                manager.StartNewRun();
                manager.DamagePlayer(99);
                Assert.That(manager.CurrentRun.Status, Is.EqualTo(GameRunStatus.Failed));
            }
            finally
            {
                if (manager != null)
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }
        }

        [Test]
        public void StatsHaveMinimumValues()
        {
            PlayerRuntimeStats stats = new PlayerRuntimeStats(new PlayerBaseStats(2, 1, 1f, 1f));

            stats.AddModifier(new PlayerStatModifier(maxHealthBonus: -99, damageBonus: -99, movementSpeedBonus: -99f, attackRateBonus: -99f));

            Assert.That(stats.MaxHealth, Is.EqualTo(1));
            Assert.That(stats.Damage, Is.EqualTo(0));
            Assert.That(stats.MovementSpeed, Is.EqualTo(0f));
            Assert.That(stats.AttackRate, Is.EqualTo(0.01f));
            Assert.That(stats.AttackCooldownSeconds, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void NullStatsDoNotCrashSummary()
        {
            Assert.That(PlayerStatSummaryFormatter.Format(null), Is.EqualTo("No active run"));
            Assert.That(PlayerStatSummaryFormatter.FormatStatsOnly(null), Is.EqualTo("No active run"));
        }
    }
}
