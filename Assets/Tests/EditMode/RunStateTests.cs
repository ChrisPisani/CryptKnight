using CryptKnight.Application;
using CryptKnight.Data;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
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
            Assert.That(runState.CurrentFloorNumber, Is.EqualTo(1));
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
        public void ProjectileStatsStack()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);

            runState.AddStatModifier(new PlayerStatModifier(
                maxHealthBonus: 2,
                damageBonus: 1,
                movementSpeedBonus: 1.5f,
                attackRateBonus: 0.5f,
                projectileCountBonus: 2,
                projectileSpeedBonus: 1.5f,
                projectileBouncesBonus: 1,
                projectileSizeBonus: 0.25f));

            Assert.That(runState.MaxHealth, Is.EqualTo(8));
            Assert.That(runState.CurrentHealth, Is.EqualTo(8));
            Assert.That(runState.PlayerStats.Damage, Is.EqualTo(2f));
            Assert.That(runState.PlayerStats.MovementSpeed, Is.EqualTo(6.5f));
            Assert.That(runState.PlayerStats.AttackRate, Is.EqualTo(1.5f));
            Assert.That(runState.PlayerStats.ProjectileCount, Is.EqualTo(3));
            Assert.That(runState.PlayerStats.ProjectileSpeed, Is.EqualTo(9.5f));
            Assert.That(runState.PlayerStats.ProjectileBounces, Is.EqualTo(1));
            Assert.That(runState.PlayerStats.ProjectileSizeMultiplier, Is.EqualTo(1.25f));
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
            runState.AddStatModifier(new PlayerStatModifier(
                damageBonus: 1,
                movementSpeedBonus: 0.5f,
                attackRateBonus: 0.2f,
                projectileCountBonus: 1,
                projectileSpeedBonus: 2f,
                projectileBouncesBonus: 1,
                projectileSizeBonus: 0.25f));

            string summary = PlayerStatSummaryFormatter.Format(runState);

            Assert.That(summary, Does.Contain("Health: 2.5 / 3 hearts"));
            Assert.That(summary, Does.Contain("Damage: 2"));
            Assert.That(summary, Does.Contain("Movement Speed: 5.5"));
            Assert.That(summary, Does.Contain("Attack Speed: 1.2"));
            Assert.That(summary, Does.Contain("Projectiles: 2"));
            Assert.That(summary, Does.Contain("Projectile Speed: 10"));
            Assert.That(summary, Does.Contain("Projectile Bounces: 1"));
            Assert.That(summary, Does.Contain("Projectile Size: 125%"));
            Assert.That(summary, Does.Contain("Keys: 2"));
        }

        [Test]
        public void StatSummaryCanHideHudValues()
        {
            GameRunState runState = GameRunState.CreateNewRun(1, 12345, 4, 4, 6);
            runState.AddKeys(2);
            runState.AddStatModifier(new PlayerStatModifier(
                damageBonus: 1,
                movementSpeedBonus: 0.5f,
                attackRateBonus: 0.2f,
                projectileCountBonus: 1,
                projectileSpeedBonus: 2f,
                projectileBouncesBonus: 1,
                projectileSizeBonus: 0.25f));

            string summary = PlayerStatSummaryFormatter.FormatStatsOnly(runState);

            Assert.That(summary, Does.Not.Contain("Health"));
            Assert.That(summary, Does.Not.Contain("Keys"));
            Assert.That(summary, Does.Contain("Damage: 2"));
            Assert.That(summary, Does.Contain("Movement Speed: 5.5"));
            Assert.That(summary, Does.Contain("Attack Speed: 1.2"));
            Assert.That(summary, Does.Contain("Projectiles: 2"));
            Assert.That(summary, Does.Contain("Projectile Speed: 10"));
            Assert.That(summary, Does.Contain("Projectile Bounces: 1"));
            Assert.That(summary, Does.Contain("Projectile Size: 125%"));
        }

        [Test]
        public void DefaultProjectileStatsMatch()
        {
            PlayerBaseStats stats = PlayerBaseStats.CreateDefault();

            Assert.That(stats.MaxHealth, Is.EqualTo(6));
            Assert.That(stats.Damage, Is.EqualTo(1));
            Assert.That(stats.MovementSpeed, Is.EqualTo(5f));
            Assert.That(stats.AttackRate, Is.EqualTo(1f));
            Assert.That(stats.ProjectileCount, Is.EqualTo(1));
            Assert.That(stats.ProjectileSpeed, Is.EqualTo(8f));
            Assert.That(stats.ProjectileBounces, Is.EqualTo(0));
            Assert.That(stats.ProjectileSizeMultiplier, Is.EqualTo(1f));
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
        public void ProjectileStatsHaveMinimums()
        {
            PlayerRuntimeStats stats = new PlayerRuntimeStats(new PlayerBaseStats(2, 1, 1f, 1f, 1, 1f, 0, 1f));

            stats.AddModifier(new PlayerStatModifier(
                maxHealthBonus: -99,
                damageBonus: -99,
                movementSpeedBonus: -99f,
                attackRateBonus: -99f,
                projectileCountBonus: -99,
                projectileSpeedBonus: -99f,
                projectileBouncesBonus: -99,
                projectileSizeBonus: -99f));

            Assert.That(stats.MaxHealth, Is.EqualTo(1));
            Assert.That(stats.Damage, Is.EqualTo(0f));
            Assert.That(stats.MovementSpeed, Is.EqualTo(0f));
            Assert.That(stats.AttackRate, Is.EqualTo(0.01f));
            Assert.That(stats.ProjectileCount, Is.EqualTo(1));
            Assert.That(stats.ProjectileSpeed, Is.EqualTo(0.1f));
            Assert.That(stats.ProjectileBounces, Is.EqualTo(0));
            Assert.That(stats.ProjectileSizeMultiplier, Is.EqualTo(0.25f));
            Assert.That(stats.AttackCooldownSeconds, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void CustomSeedStartsRun()
        {
            GameManager manager = GameManager.Instance;
            try
            {
                GameRunState run = manager.StartNewRun(24680);

                Assert.That(run.Seed, Is.EqualTo(24680));
                Assert.That(run.Dungeon.FloorSeed, Is.EqualTo(24680));
                Assert.That(run.CurrentFloorNumber, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void RandomSeedStaysInRange()
        {
            GameManager manager = GameManager.Instance;
            try
            {
                GameRunState run = manager.StartNewRun();

                Assert.That(run.Seed, Is.InRange(100000, 999999));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void BadSeedIsRejected()
        {
            Assert.That(RunSeedUtility.TryParse("123456", out int seed), Is.True);
            Assert.That(seed, Is.EqualTo(123456));
            Assert.That(RunSeedUtility.TryParse("0", out _), Is.False);
            Assert.That(RunSeedUtility.TryParse("1000000000", out _), Is.False);
            Assert.That(RunSeedUtility.TryParse("crypt", out _), Is.False);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RunSeedUtility.GetFloorSeed(0, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RunSeedUtility.GetFloorSeed(12345, 3));
        }

        [Test]
        public void SeedDisplayUsesRunSeed()
        {
            Assert.That(RunSeedUtility.FormatForHud(24680), Is.EqualTo("Seed: 24680"));
        }

        [Test]
        public void SameSeedBuildsSameFloors()
        {
            int firstFloorSeed = RunSeedUtility.GetFloorSeed(13579, 1);
            int firstHardSeed = RunSeedUtility.GetFloorSeed(13579, 2);
            int secondHardSeed = RunSeedUtility.GetFloorSeed(13579, 2);
            DungeonRunState firstHardFloor = DungeonRunStateFactory.Create(
                4,
                4,
                firstHardSeed,
                2,
                EnemyDifficulty.Hard,
                false);
            DungeonRunState secondHardFloor = DungeonRunStateFactory.Create(
                4,
                4,
                secondHardSeed,
                2,
                EnemyDifficulty.Hard,
                false);

            Assert.That(firstFloorSeed, Is.EqualTo(13579));
            Assert.That(firstHardSeed, Is.EqualTo(secondHardSeed));
            Assert.That(firstHardFloor.Layout.StartPosition, Is.EqualTo(secondHardFloor.Layout.StartPosition));
            Assert.That(firstHardFloor.Layout.FinalPosition, Is.EqualTo(secondHardFloor.Layout.FinalPosition));
        }

        [Test]
        public void PlayerStateCarriesBetweenFloors()
        {
            GameRunState run = GameRunState.CreateNewRun(1, 24680, 4, 4, 6);
            run.InitializeDungeon(DungeonRunStateFactory.Create(4, 4, 24680));
            run.ApplyDamage(1);
            run.AddKeys(2);
            run.AddCollectedItem("damage_up", "Chili Pepper");
            run.AddStatModifier(new PlayerStatModifier(damageBonus: 1));
            DungeonRunState hardFloor = DungeonRunStateFactory.Create(
                4,
                4,
                RunSeedUtility.GetFloorSeed(24680, 2),
                2,
                EnemyDifficulty.Hard,
                false);

            Assert.That(run.AdvanceToDungeon(hardFloor), Is.True);

            Assert.That(run.CurrentFloorNumber, Is.EqualTo(2));
            Assert.That(run.CurrentHealth, Is.EqualTo(5));
            Assert.That(run.KeyCount, Is.EqualTo(2));
            Assert.That(run.CollectedItems, Has.Count.EqualTo(1));
            Assert.That(run.PlayerStats.Damage, Is.EqualTo(2));
            Assert.That(run.AdvanceToDungeon(hardFloor), Is.False);
        }

        [Test]
        public void SecondFloorHasNoStarterGift()
        {
            DungeonRunState hardFloor = DungeonRunStateFactory.Create(
                4,
                4,
                RunSeedUtility.GetFloorSeed(12345, 2),
                2,
                EnemyDifficulty.Hard,
                false);
            DungeonRoomRuntimeState starter = hardFloor.GetRoomState(hardFloor.Layout.StartPosition);

            Assert.That(starter.Loot, Is.Empty);
            Assert.That(hardFloor.FloorNumber, Is.EqualTo(2));
            Assert.That(hardFloor.Difficulty, Is.EqualTo(EnemyDifficulty.Hard));
        }

        [Test]
        public void PortalAdvancesToFloorTwo()
        {
            GameManager manager = GameManager.Instance;
            try
            {
                GameRunState run = manager.StartNewRun(97531);
                run.ApplyDamage(2);

                Assert.That(manager.AdvanceToNextFloor(), Is.True);
                Assert.That(run.CurrentFloorNumber, Is.EqualTo(2));
                Assert.That(run.Dungeon.Difficulty, Is.EqualTo(EnemyDifficulty.Hard));
                Assert.That(run.CurrentHealth, Is.EqualTo(run.MaxHealth));
                Assert.That(run.IsActive, Is.True);
                Assert.That(manager.AdvanceToNextFloor(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void FloorAdvanceRestoresFullHealth()
        {
            GameManager manager = GameManager.Instance;
            try
            {
                GameRunState run = manager.StartNewRun(13579);
                run.ApplyDamage(4);

                Assert.That(manager.AdvanceToNextFloor(), Is.True);
                Assert.That(run.CurrentHealth, Is.EqualTo(6));
                Assert.That(run.CurrentHealth, Is.EqualTo(run.MaxHealth));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void FloorHealUsesCurrentMaxHealth()
        {
            GameManager manager = GameManager.Instance;
            try
            {
                GameRunState run = manager.StartNewRun(24680);
                run.AddStatModifier(new PlayerStatModifier(maxHealthBonus: 4));
                run.ApplyDamage(5);

                Assert.That(manager.AdvanceToNextFloor(), Is.True);
                Assert.That(run.MaxHealth, Is.EqualTo(10));
                Assert.That(run.CurrentHealth, Is.EqualTo(10));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void FailedFloorAdvanceDoesNotHeal()
        {
            GameManager manager = GameManager.Instance;
            try
            {
                GameRunState run = manager.StartNewRun(86420);
                Assert.That(manager.AdvanceToNextFloor(), Is.True);
                run.ApplyDamage(2);
                int damagedHealth = run.CurrentHealth;

                Assert.That(manager.AdvanceToNextFloor(), Is.False);
                Assert.That(run.CurrentHealth, Is.EqualTo(damagedHealth));
            }
            finally
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        [Test]
        public void NullStatsDoNotCrashSummary()
        {
            Assert.That(PlayerStatSummaryFormatter.Format(null), Is.EqualTo("No active run"));
            Assert.That(PlayerStatSummaryFormatter.FormatStatsOnly(null), Is.EqualTo("No active run"));
        }
    }
}
