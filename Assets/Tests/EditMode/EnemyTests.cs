using System.Collections.Generic;
using System.Reflection;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
using CryptKnight.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class EnemyTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

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
        public void EnemyRoomsCreateThreeToFiveEnemies()
        {
            EnemySpawnRules rules = EnemySpawnRules.CreateDefault();

            int count = rules.GetEnemyCount(RoomType.Enemy, 12345, Vector2Int.zero);

            Assert.That(count, Is.InRange(3, 5));
        }

        [Test]
        public void TrapRoomsCreateOneOrTwoEnemies()
        {
            EnemySpawnRules rules = EnemySpawnRules.CreateDefault();

            int count = rules.GetEnemyCount(RoomType.Trap, 12345, Vector2Int.one);

            Assert.That(count, Is.InRange(1, 2));
        }

        [Test]
        public void RoomsCanMixEnemyTypes()
        {
            EnemySpawnRules rules = new EnemySpawnRules(4, 4, 2, 2);

            IReadOnlyList<RoomEnemySpawn> spawns = rules.CreateSpawns(RoomType.Enemy, 12345, Vector2Int.zero);

            Assert.That(ContainsKind(spawns, EnemyKind.Zombie), Is.True);
            Assert.That(ContainsKind(spawns, EnemyKind.Spider), Is.True);
        }

        [Test]
        public void EnemySpawnsAvoidNorthAndSouthDoorLanes()
        {
            EnemySpawnRules rules = new EnemySpawnRules(6, 6, 1, 1);

            IReadOnlyList<RoomEnemySpawn> spawns = rules.CreateSpawns(RoomType.Enemy, 12345, Vector2Int.zero);

            for (int i = 0; i < spawns.Count; i++)
            {
                Vector2 position = spawns[i].Position;
                bool inDoorLane = Mathf.Abs(position.x) < 1.25f && Mathf.Abs(position.y) > 2f;
                Assert.That(inDoorLane, Is.False, position.ToString());
            }
        }

        [Test]
        public void EnemySpawnsAvoidEastAndWestDoorLanes()
        {
            EnemySpawnRules rules = new EnemySpawnRules(6, 6, 1, 1);

            IReadOnlyList<RoomEnemySpawn> spawns = rules.CreateSpawns(RoomType.Enemy, 12345, Vector2Int.zero);

            for (int i = 0; i < spawns.Count; i++)
            {
                Vector2 position = spawns[i].Position;
                bool inDoorLane = Mathf.Abs(position.y) < 1.25f && Mathf.Abs(position.x) > 4.5f;
                Assert.That(inDoorLane, Is.False, position.ToString());
            }
        }

        [Test]
        public void DefeatedEnemyStaysGone()
        {
            DungeonRoomRuntimeState roomState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Enemy);
            RoomEnemyInstance enemy = roomState.AddEnemy(EnemyKind.Zombie, Vector2.one);

            bool cleared = roomState.MarkEnemyDefeated(enemy.Id);

            Assert.That(cleared, Is.True);
            Assert.That(enemy.IsDefeated, Is.True);
            Assert.That(roomState.RemainingEnemies, Is.EqualTo(0));
        }

        [Test]
        public void EnemyHealthCanBeConfigured()
        {
            GameObject enemy = CreateObject("Enemy");
            EnemyHealth health = enemy.AddComponent<EnemyHealth>();

            health.Initialize(5);
            health.ApplyDamage(4);

            Assert.That(health.CurrentHealth, Is.EqualTo(1));
        }

        [Test]
        public void EnemyStateUpdatesWhenRoomCloses()
        {
            DungeonRoomRuntimeState roomState = new DungeonRoomRuntimeState(Vector2Int.zero, RoomType.Enemy);
            RoomEnemyInstance roomEnemy = roomState.AddEnemy(EnemyKind.Zombie, Vector2.zero, 5);
            GameObject enemy = CreateObject("Tracked Enemy");
            enemy.transform.position = new Vector2(2f, -1f);
            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            health.Initialize(5, 3);
            EnemyRoomStateTracker tracker = enemy.AddComponent<EnemyRoomStateTracker>();
            tracker.Initialize(roomEnemy, health);

            MethodInfo onDisable = typeof(EnemyRoomStateTracker).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDisable, Is.Not.Null);
            onDisable.Invoke(tracker, null);

            Assert.That(roomEnemy.Position, Is.EqualTo(new Vector2(2f, -1f)));
            Assert.That(roomEnemy.CurrentHealth, Is.EqualTo(3));
        }

        [Test]
        public void ZombieMovesTowardPlayer()
        {
            GameObject enemy = CreateEnemyObject("Zombie");
            GameObject player = CreateObject("Player");
            player.transform.position = new Vector2(3f, 0f);
            ZombieEnemyAI zombie = enemy.AddComponent<ZombieEnemyAI>();
            zombie.Initialize(player.transform, null, CreateRoomBounds());

            bool moved = zombie.MoveTowardPlayer(1f);

            Assert.That(moved, Is.True);
            Assert.That(enemy.GetComponent<Rigidbody2D>().position.x, Is.GreaterThan(0f));
        }

        [Test]
        public void ZombieShootsAfterLunge()
        {
            GameObject projectileRoot = CreateObject("Projectiles");
            GameObject enemy = CreateEnemyObject("Zombie");
            GameObject player = CreateObject("Player");
            player.transform.position = new Vector2(3f, 0f);
            ZombieEnemyAI zombie = enemy.AddComponent<ZombieEnemyAI>();
            zombie.Initialize(player.transform, projectileRoot.transform, CreateRoomBounds());

            bool attacked = zombie.TryAttack(1000f);

            Assert.That(attacked, Is.True);
            Assert.That(zombie.LastShotDirection.x, Is.GreaterThan(0f));
            Assert.That(projectileRoot.transform.childCount, Is.EqualTo(1));
        }

        [Test]
        public void ZombiePhaseOffsetDelaysAttack()
        {
            GameObject projectileRoot = CreateObject("Projectiles");
            GameObject enemy = CreateEnemyObject("Zombie");
            GameObject player = CreateObject("Player");
            player.transform.position = new Vector2(3f, 0f);
            ZombieEnemyAI zombie = enemy.AddComponent<ZombieEnemyAI>();
            float startTime = Time.time;
            zombie.Initialize(player.transform, projectileRoot.transform, CreateRoomBounds(), 1f);

            Assert.That(zombie.TryAttack(startTime + 2.9f), Is.False);
            Assert.That(projectileRoot.transform.childCount, Is.EqualTo(0));
            Assert.That(zombie.TryAttack(startTime + 3.1f), Is.True);
        }

        [Test]
        public void EnemiesUseKinematicBodies()
        {
            GameObject player = CreateObject("Player");
            GameObject zombieObject = CreateEnemyObject("Zombie");
            GameObject spiderObject = CreateEnemyObject("Spider");

            zombieObject.AddComponent<ZombieEnemyAI>().Initialize(player.transform, null, CreateRoomBounds());
            spiderObject.AddComponent<SpiderEnemyAI>().Initialize(player.transform, null, CreateRoomBounds());

            Assert.That(zombieObject.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(spiderObject.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
        }

        [Test]
        public void SpiderJumpsDiagonally()
        {
            GameObject projectileRoot = CreateObject("Projectiles");
            GameObject enemy = CreateEnemyObject("Spider");
            GameObject player = CreateObject("Player");
            player.transform.position = new Vector2(2f, 2f);
            SpiderEnemyAI spider = enemy.AddComponent<SpiderEnemyAI>();
            spider.Initialize(player.transform, projectileRoot.transform, CreateRoomBounds());
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            Vector2 startPosition = body.position;

            bool attacked = spider.TryAttack(1000f);

            Assert.That(attacked, Is.True);
            Assert.That(spider.IsJumping, Is.True);
            Assert.That(spider.HasPendingAttack, Is.True);
            Assert.That(projectileRoot.transform.childCount, Is.EqualTo(0));
            Assert.That(body.position.x, Is.EqualTo(startPosition.x).Within(0.001f));
            Assert.That(body.position.y, Is.EqualTo(startPosition.y).Within(0.001f));
            Assert.That(spider.LastJumpPosition.x, Is.GreaterThan(0f));
            Assert.That(spider.LastJumpPosition.y, Is.GreaterThan(0f));
            Assert.That(spider.LastJumpPosition.x, Is.LessThan(player.transform.position.x));
            Assert.That(spider.LastJumpPosition.y, Is.LessThan(player.transform.position.y));

            float currentTime = 1000f;
            for (int i = 0; i < 20; i++)
            {
                currentTime += 0.05f;
                spider.AdvanceJump(0.05f, currentTime);
            }

            Assert.That(spider.IsJumping, Is.False);
            Assert.That(spider.HasPendingAttack, Is.True);
            Assert.That(body.position.x, Is.EqualTo(spider.LastJumpPosition.x).Within(0.001f));
            Assert.That(body.position.y, Is.EqualTo(spider.LastJumpPosition.y).Within(0.001f));
            Assert.That(spider.TryResolvePendingAttack(1001.4f), Is.False);
            Assert.That(projectileRoot.transform.childCount, Is.EqualTo(0));

            Assert.That(spider.TryResolvePendingAttack(1001.5f), Is.True);
            Assert.That(spider.HasPendingAttack, Is.False);
            Assert.That(projectileRoot.transform.childCount, Is.EqualTo(1));
        }

        [Test]
        public void SpiderPhaseOffsetDelaysJump()
        {
            GameObject projectileRoot = CreateObject("Projectiles");
            GameObject enemy = CreateEnemyObject("Spider");
            GameObject player = CreateObject("Player");
            player.transform.position = new Vector2(2f, 2f);
            SpiderEnemyAI spider = enemy.AddComponent<SpiderEnemyAI>();
            float startTime = Time.time;
            spider.Initialize(player.transform, projectileRoot.transform, CreateRoomBounds(), 1f);

            Assert.That(spider.TryAttack(startTime + 2.4f), Is.False);
            Assert.That(spider.TryAttack(startTime + 2.6f), Is.True);
            Assert.That(spider.IsJumping, Is.True);
        }

        [Test]
        public void SpiderShotUsesDiagonalDirection()
        {
            Vector2 direction = SpiderEnemyAI.GetDiagonalDirection(Vector2.zero, new Vector2(-2f, 3f));

            Assert.That(direction.x, Is.LessThan(0f));
            Assert.That(direction.y, Is.GreaterThan(0f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        private GameObject CreateEnemyObject(string name)
        {
            GameObject enemy = CreateObject(name);
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<CircleCollider2D>();
            return enemy;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Rect CreateRoomBounds()
        {
            return Rect.MinMaxRect(-5f, -3f, 5f, 3f);
        }

        private static bool ContainsKind(IReadOnlyList<RoomEnemySpawn> spawns, EnemyKind kind)
        {
            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
