using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CryptKnight.Combat;
using CryptKnight.Content;
using CryptKnight.Dungeon;
using CryptKnight.Gameplay;
using CryptKnight.Traps;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class TrapTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

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
        public void TrapRoomsCreateFourToEightTraps()
        {
            TrapGenerationRules rules = TrapGenerationRules.CreateDefault();

            for (int seed = 1; seed <= 20; seed++)
            {
                IReadOnlyList<RoomTrapSpawn> spawns = rules.CreateSpawns(RoomType.Trap, seed, Vector2Int.one);
                Assert.That(spawns.Count, Is.InRange(4, 8));
            }
        }

        [Test]
        public void OtherRoomsHaveNoTraps()
        {
            TrapGenerationRules rules = TrapGenerationRules.CreateDefault();

            Assert.That(rules.CreateSpawns(RoomType.Starter, 1, Vector2Int.zero), Is.Empty);
            Assert.That(rules.CreateSpawns(RoomType.Enemy, 1, Vector2Int.zero), Is.Empty);
            Assert.That(rules.CreateSpawns(RoomType.Final, 1, Vector2Int.zero), Is.Empty);
        }

        [Test]
        public void TrapMixIsAboutFortyPercentWall()
        {
            TrapGenerationRules rules = TrapGenerationRules.CreateDefault();
            int wallTrapCount = 0;
            int totalTrapCount = 0;

            for (int seed = 1; seed <= 500; seed++)
            {
                IReadOnlyList<RoomTrapSpawn> spawns = rules.CreateSpawns(RoomType.Trap, seed, Vector2Int.one);
                wallTrapCount += spawns.Count(spawn => spawn.Kind == TrapKind.WallProjectile);
                totalTrapCount += spawns.Count;
            }

            float wallRatio = (float)wallTrapCount / totalTrapCount;
            Assert.That(wallRatio, Is.InRange(0.35f, 0.45f));
        }

        [Test]
        public void WallChanceCanSelectOneKind()
        {
            TrapDefinition spike = new TrapDefinition(TrapKind.Spike, 1, 1f);
            TrapDefinition wall = new TrapDefinition(TrapKind.WallProjectile, 1, 2.5f);
            TrapGenerationRules spikesOnly = new TrapGenerationRules(new TrapRoomConfiguration(8, 8, spike, wall, 0f));
            TrapGenerationRules wallsOnly = new TrapGenerationRules(new TrapRoomConfiguration(8, 8, spike, wall, 1f));

            Assert.That(
                spikesOnly.CreateSpawns(RoomType.Trap, 1, Vector2Int.zero).All(spawn => spawn.Kind == TrapKind.Spike),
                Is.True);
            Assert.That(
                wallsOnly.CreateSpawns(RoomType.Trap, 1, Vector2Int.zero).All(spawn => spawn.Kind == TrapKind.WallProjectile),
                Is.True);
        }

        [Test]
        public void TrapGenerationIsStable()
        {
            TrapGenerationRules rules = TrapGenerationRules.CreateDefault();
            IReadOnlyList<RoomTrapSpawn> first = rules.CreateSpawns(RoomType.Trap, 24680, new Vector2Int(2, 3));
            IReadOnlyList<RoomTrapSpawn> second = rules.CreateSpawns(RoomType.Trap, 24680, new Vector2Int(2, 3));

            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Kind, Is.EqualTo(first[i].Kind));
                Assert.That(second[i].Position, Is.EqualTo(first[i].Position));
                Assert.That(second[i].FireDirection, Is.EqualTo(first[i].FireDirection));
                Assert.That(second[i].PhaseOffsetSeconds, Is.EqualTo(first[i].PhaseOffsetSeconds));
            }
        }

        [Test]
        public void TrapAnchorsAvoidDoors()
        {
            IReadOnlyList<RoomTrapSpawn> spawns = TrapGenerationRules.CreateDefault().CreateSpawns(
                RoomType.Trap,
                12345,
                Vector2Int.zero);
            RoomDirection[] directions =
            {
                RoomDirection.North,
                RoomDirection.East,
                RoomDirection.South,
                RoomDirection.West
            };

            for (int i = 0; i < spawns.Count; i++)
            {
                for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
                {
                    Vector2 doorPosition = DungeonRoomGeometry.GetDoorPosition(directions[directionIndex]);
                    Assert.That(Vector2.Distance(spawns[i].Position, doorPosition), Is.GreaterThan(1.2f));
                }
            }
        }

        [Test]
        public void WallTrapsUseEveryWall()
        {
            HashSet<Vector2> directions = new HashSet<Vector2>();
            TrapGenerationRules rules = TrapGenerationRules.CreateDefault();
            for (int seed = 1; seed <= 100; seed++)
            {
                IReadOnlyList<RoomTrapSpawn> spawns = rules.CreateSpawns(RoomType.Trap, seed, Vector2Int.one);
                foreach (RoomTrapSpawn spawn in spawns.Where(spawn => spawn.Kind == TrapKind.WallProjectile))
                {
                    directions.Add(spawn.FireDirection);
                }
            }

            Assert.That(directions, Does.Contain(Vector2.up));
            Assert.That(directions, Does.Contain(Vector2.down));
            Assert.That(directions, Does.Contain(Vector2.left));
            Assert.That(directions, Does.Contain(Vector2.right));
        }

        [Test]
        public void RoomStateKeepsTraps()
        {
            DungeonRunState dungeon = DungeonRunStateFactory.Create(4, 4, 12345);
            DungeonRoomRuntimeState trapRoom = dungeon.Rooms.Values.First(room => room.RoomType == RoomType.Trap);

            DungeonRoomRuntimeState sameRoom = dungeon.GetRoomState(trapRoom.GridPosition);

            Assert.That(sameRoom, Is.SameAs(trapRoom));
            Assert.That(sameRoom.Traps.Count, Is.InRange(4, 8));
            Assert.That(dungeon.TrapConfiguration.GetDefinition(TrapKind.Spike).Damage, Is.EqualTo(1));
        }

        [Test]
        public void TrapRoomsBuildHazards()
        {
            DungeonRunState dungeon = DungeonRunStateFactory.Create(4, 4, 12345);
            DungeonRoomRuntimeState trapRoom = dungeon.Rooms.Values.First(room => room.RoomType == RoomType.Trap);
            GameObject controllerObject = CreateObject("Gameplay Controller");
            GameplaySceneController controller = controllerObject.AddComponent<GameplaySceneController>();
            GameObject roomRoot = CreateObject("Trap Room Root");
            SetPrivateField(controller, "dungeonRun", dungeon);

            InvokePrivate(controller, "CreateRoomTraps", roomRoot.transform, trapRoom);

            Assert.That(
                roomRoot.GetComponentsInChildren<SpikeTrap>().Length + roomRoot.GetComponentsInChildren<WallProjectileTrap>().Length,
                Is.EqualTo(trapRoom.Traps.Count));
            Assert.That(roomRoot.transform.Find("Trap Room Marker"), Is.Null);
            Assert.That(roomRoot.GetComponentsInChildren<SpriteRenderer>().All(renderer => renderer.sprite != null), Is.True);
        }

        [Test]
        public void TrapSpritesLoad()
        {
            Assert.That(RuntimeAssetLoader.LoadSprite(TrapVisualFactory.SpikeAssetPath), Is.Not.Null);
            Assert.That(RuntimeAssetLoader.LoadSprite(TrapVisualFactory.NorthWallAssetPath), Is.Not.Null);
            Assert.That(RuntimeAssetLoader.LoadSprite(TrapVisualFactory.WestWallAssetPath), Is.Not.Null);
            Assert.That(RuntimeAssetLoader.LoadSprite("Art/Traps/trap_projectile"), Is.Not.Null);
        }

        [Test]
        public void WallSpritesFaceIntoRoom()
        {
            Transform parent = CreateObject("Trap Visuals").transform;
            GameObject north = Track(TrapVisualFactory.CreateWall("North", parent, new Vector2(-4.5f, 4.8f), Vector2.down, Color.yellow));
            GameObject south = Track(TrapVisualFactory.CreateWall("South", parent, new Vector2(4.5f, -4.8f), Vector2.up, Color.yellow));
            GameObject west = Track(TrapVisualFactory.CreateWall("West", parent, new Vector2(-9.68f, -2.5f), Vector2.right, Color.yellow));
            GameObject east = Track(TrapVisualFactory.CreateWall("East", parent, new Vector2(9.68f, 2.5f), Vector2.left, Color.yellow));

            Assert.That(GetVisual(north).sprite.name, Does.Contain("north"));
            Assert.That(GetVisual(south).transform.localScale.y, Is.LessThan(0f));
            Assert.That(GetVisual(west).sprite.name, Does.Contain("west"));
            Assert.That(GetVisual(east).transform.localScale.x, Is.LessThan(0f));
        }

        [Test]
        public void WallTrapsOverlapRoomWalls()
        {
            Transform parent = CreateObject("Trap Visuals").transform;
            SpriteRenderer west = GetVisual(Track(TrapVisualFactory.CreateWall("West", parent, new Vector2(-9.68f, -2.5f), Vector2.right, Color.yellow)));
            SpriteRenderer east = GetVisual(Track(TrapVisualFactory.CreateWall("East", parent, new Vector2(9.68f, 2.5f), Vector2.left, Color.yellow)));
            SpriteRenderer north = GetVisual(Track(TrapVisualFactory.CreateWall("North", parent, new Vector2(-4.5f, 4.8f), Vector2.down, Color.yellow)));
            SpriteRenderer south = GetVisual(Track(TrapVisualFactory.CreateWall("South", parent, new Vector2(4.5f, -4.8f), Vector2.up, Color.yellow)));

            Assert.That(west.bounds.min.x, Is.LessThanOrEqualTo(-11.1f));
            Assert.That(west.bounds.max.x, Is.LessThanOrEqualTo(-9.35f));
            Assert.That(east.bounds.max.x, Is.GreaterThanOrEqualTo(11.1f));
            Assert.That(east.bounds.min.x, Is.GreaterThanOrEqualTo(9.35f));
            Assert.That(north.bounds.max.y, Is.GreaterThanOrEqualTo(6.1f));
            Assert.That(north.bounds.min.y, Is.LessThanOrEqualTo(4.35f));
            Assert.That(south.bounds.min.y, Is.LessThanOrEqualTo(-6.1f));
            Assert.That(south.bounds.max.y, Is.GreaterThanOrEqualTo(-4.35f));
        }

        [Test]
        public void SpikeKeepsItsDamageArea()
        {
            DungeonRunState dungeon = DungeonRunStateFactory.Create(4, 4, 12345);
            GameplaySceneController controller = CreateObject("Gameplay Controller").AddComponent<GameplaySceneController>();
            GameObject roomRoot = CreateObject("Trap Room Root");
            SetPrivateField(controller, "dungeonRun", dungeon);
            RoomTrapInstance instance = new RoomTrapInstance(0, TrapKind.Spike, Vector2.zero, Vector2.zero, 0f);

            InvokePrivate(controller, "CreateSpikeTrap", roomRoot.transform, instance);

            SpikeTrap spike = roomRoot.GetComponentInChildren<SpikeTrap>();
            SpriteRenderer renderer = GetVisual(spike.gameObject);
            Assert.That(spike.GetComponent<BoxCollider2D>().size, Is.EqualTo(new Vector2(1.2f, 1.2f)));
            Assert.That(spike.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y), Is.EqualTo(1.4f).Within(0.01f));
            Assert.That(renderer.sortingOrder, Is.EqualTo(4));
        }

        [Test]
        public void SpikeDamagesPlayer()
        {
            SpikeTrap spike = CreateSpike();
            GameObject player = CreateObject("Trap Test Player");
            CircleCollider2D playerCollider = player.AddComponent<CircleCollider2D>();
            TrapTestDamageable damageable = player.AddComponent<TrapTestDamageable>();
            damageable.Configure(DamageableTarget.Player);

            InvokePrivate(spike, "OnTriggerEnter2D", playerCollider);

            Assert.That(damageable.DamageTaken, Is.EqualTo(1));
        }

        [Test]
        public void SpikeWaitsBeforeHittingAgain()
        {
            SpikeTrap spike = CreateSpike();
            TrapTestDamageable damageable = CreateObject("Trap Test Player").AddComponent<TrapTestDamageable>();
            damageable.Configure(DamageableTarget.Player);

            Assert.That(spike.TryDamage(damageable, 0f), Is.True);
            Assert.That(spike.TryDamage(damageable, 0.5f), Is.False);
            Assert.That(spike.TryDamage(damageable, 1f), Is.True);
            Assert.That(damageable.DamageTaken, Is.EqualTo(2));
        }

        [Test]
        public void SpikeIgnoresEnemies()
        {
            SpikeTrap spike = CreateSpike();
            TrapTestDamageable damageable = CreateObject("Trap Test Enemy").AddComponent<TrapTestDamageable>();
            damageable.Configure(DamageableTarget.Enemy);

            Assert.That(spike.TryDamage(damageable, 0f), Is.False);
            Assert.That(damageable.DamageTaken, Is.Zero);
        }

        [Test]
        public void WallTrapWaitsToFire()
        {
            Transform projectileRoot = CreateObject("Trap Projectiles").transform;
            WallProjectileTrap trap = CreateWallTrap(projectileRoot, Vector2.right);
            float firstShotTime = trap.NextFireTime;

            InvokePrivate(trap, "Update");

            Assert.That(trap.TryFire(firstShotTime - 0.01f), Is.Null);
            Assert.That(projectileRoot.childCount, Is.Zero);
        }

        [Test]
        public void WallTrapShootsAcrossRoom()
        {
            Transform projectileRoot = CreateObject("Trap Projectiles").transform;
            WallProjectileTrap trap = CreateWallTrap(projectileRoot, Vector2.left);
            float firstShotTime = trap.NextFireTime;

            ProjectileController projectile = trap.TryFire(firstShotTime);

            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.GetComponent<Rigidbody2D>().linearVelocity.x, Is.LessThan(0f));
            Assert.That(trap.NextFireTime, Is.EqualTo(firstShotTime + 2.5f).Within(0.001f));
        }

        [Test]
        public void TrapProjectilesTargetPlayer()
        {
            Transform projectileRoot = CreateObject("Trap Projectiles").transform;
            WallProjectileTrap trap = CreateWallTrap(projectileRoot, Vector2.up);

            ProjectileController projectile = trap.TryFire(trap.NextFireTime);

            Assert.That(projectile.TargetType, Is.EqualTo(DamageableTarget.Player));
            Assert.That(projectile.Damage, Is.EqualTo(1));
            SpriteRenderer renderer = projectile.transform.Find("Visual").GetComponent<SpriteRenderer>();
            Assert.That(renderer.sprite.name, Does.Contain("trap_projectile"));
            Assert.That(Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y), Is.EqualTo(0.75f).Within(0.01f));
            Assert.That(projectile.GetComponent<CircleCollider2D>().radius, Is.EqualTo(0.135f).Within(0.001f));
        }

        [Test]
        public void TrapProjectilePointsWhereItMoves()
        {
            Vector2[] directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
            for (int i = 0; i < directions.Length; i++)
            {
                Transform projectileRoot = CreateObject($"Trap Projectiles {i}").transform;
                WallProjectileTrap trap = CreateWallTrap(projectileRoot, directions[i]);
                ProjectileController projectile = trap.TryFire(trap.NextFireTime);
                Transform visual = projectile.transform.Find("Visual");
                Vector2 visualHeading = visual.TransformDirection(Vector3.right).normalized;

                Assert.That(Vector2.Dot(visualHeading, directions[i]), Is.GreaterThan(0.99f));
            }
        }

        private SpikeTrap CreateSpike()
        {
            GameObject spikeObject = CreateObject("Spike Trap");
            SpikeTrap spike = spikeObject.AddComponent<SpikeTrap>();
            spike.Initialize(TrapRoomConfiguration.CreateDefault().GetDefinition(TrapKind.Spike));
            return spike;
        }

        private WallProjectileTrap CreateWallTrap(Transform projectileRoot, Vector2 direction)
        {
            GameObject trapObject = CreateObject("Wall Trap");
            WallProjectileTrap trap = trapObject.AddComponent<WallProjectileTrap>();
            trap.Initialize(
                TrapRoomConfiguration.CreateDefault().GetDefinition(TrapKind.WallProjectile),
                direction,
                projectileRoot);
            return trap;
        }

        private GameObject CreateObject(string name)
        {
            GameObject value = new GameObject(name);
            createdObjects.Add(value);
            return value;
        }

        private GameObject Track(GameObject value)
        {
            createdObjects.Add(value);
            return value;
        }

        private static SpriteRenderer GetVisual(GameObject root)
        {
            return root.transform.Find("Visual").GetComponent<SpriteRenderer>();
        }

        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }

    public sealed class TrapTestDamageable : MonoBehaviour, IDamageable
    {
        public DamageableTarget TargetType { get; private set; }
        public int DamageTaken { get; private set; }

        public void Configure(DamageableTarget targetType)
        {
            TargetType = targetType;
        }

        public void ApplyDamage(int damage)
        {
            DamageTaken += damage;
        }
    }
}
