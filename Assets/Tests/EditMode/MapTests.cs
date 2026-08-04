using System;
using System.Collections.Generic;
using System.Linq;
using CryptKnight.Dungeon;
using CryptKnight.Enemies;
using CryptKnight.Loot;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class MapTests
    {
        private static readonly Vector2Int StarterPosition = new Vector2Int(0, 0);
        private static readonly Vector2Int EnemyPosition = new Vector2Int(1, 0);
        private static readonly Vector2Int TrapPosition = new Vector2Int(0, 1);
        private static readonly Vector2Int FinalPosition = new Vector2Int(1, 1);

        [Test]
        public void StarterBeginsVisited()
        {
            DungeonRunState dungeon = CreateDungeon();

            Assert.That(dungeon.GetRoomState(StarterPosition).IsVisited, Is.True);
        }

        [Test]
        public void MovingMarksRoomVisited()
        {
            DungeonRunState dungeon = CreateDungeon();

            Assert.That(dungeon.TryMove(RoomDirection.East), Is.True);
            Assert.That(dungeon.GetRoomState(EnemyPosition).IsVisited, Is.True);
        }

        [Test]
        public void BadMoveChangesNothing()
        {
            DungeonRunState dungeon = CreateDungeon();
            int changes = 0;
            dungeon.MapChanged += () => changes++;

            Assert.That(dungeon.TryMove(RoomDirection.West), Is.False);
            Assert.That(dungeon.Navigator.CurrentRoom.GridPosition, Is.EqualTo(StarterPosition));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void MapUsesGridPositions()
        {
            DungeonMapSnapshot map = DungeonMapSnapshot.Create(CreateDungeon());

            Assert.That(map.Width, Is.EqualTo(2));
            Assert.That(map.Height, Is.EqualTo(2));
            Assert.That(map.Rooms.Select(room => room.GridPosition), Is.EquivalentTo(new[]
            {
                StarterPosition,
                EnemyPosition,
                TrapPosition,
                FinalPosition
            }));
        }

        [Test]
        public void MapUsesRoomConnections()
        {
            DungeonMapSnapshot map = DungeonMapSnapshot.Create(CreateDungeon());

            Assert.That(map.Connections, Has.Count.EqualTo(4));
            Assert.That(map.Connections.Any(connection => Connects(connection, StarterPosition, EnemyPosition)), Is.True);
            Assert.That(map.Connections.Any(connection => Connects(connection, StarterPosition, TrapPosition)), Is.True);
            Assert.That(map.Connections.Any(connection => Connects(connection, EnemyPosition, FinalPosition)), Is.True);
            Assert.That(map.Connections.Any(connection => Connects(connection, TrapPosition, FinalPosition)), Is.True);
        }

        [Test]
        public void CurrentRoomIsHighlighted()
        {
            DungeonRunState dungeon = CreateDungeon();
            dungeon.TryMove(RoomDirection.East);

            DungeonMapRoomInfo current = GetRoom(DungeonMapSnapshot.Create(dungeon), EnemyPosition);
            DungeonMapRoomInfo starter = GetRoom(DungeonMapSnapshot.Create(dungeon), StarterPosition);

            Assert.That(current.IsCurrent, Is.True);
            Assert.That(starter.IsCurrent, Is.False);
        }

        [Test]
        public void UnknownRoomsUseQuestionMarks()
        {
            DungeonMapRoomInfo enemy = GetRoom(DungeonMapSnapshot.Create(CreateDungeon()), EnemyPosition);

            Assert.That(enemy.Marker, Is.EqualTo(DungeonMapMarker.Unknown));
            Assert.That(enemy.IsVisited, Is.False);
        }

        [Test]
        public void VisitedEnemyShowsCombat()
        {
            DungeonRunState dungeon = CreateDungeon();
            dungeon.TryMove(RoomDirection.East);

            DungeonMapRoomInfo enemy = GetRoom(DungeonMapSnapshot.Create(dungeon), EnemyPosition);

            Assert.That(enemy.Marker, Is.EqualTo(DungeonMapMarker.Combat));
            Assert.That(enemy.IsCleared, Is.False);
        }

        [Test]
        public void VisitedTrapShowsSpikes()
        {
            DungeonRunState dungeon = CreateDungeon();
            dungeon.TryMove(RoomDirection.North);

            Assert.That(
                GetRoom(DungeonMapSnapshot.Create(dungeon), TrapPosition).Marker,
                Is.EqualTo(DungeonMapMarker.Trap));
        }

        [Test]
        public void UnknownTypesStayHidden()
        {
            DungeonMapSnapshot map = DungeonMapSnapshot.Create(CreateDungeon());

            Assert.That(GetRoom(map, EnemyPosition).Marker, Is.EqualTo(DungeonMapMarker.Unknown));
            Assert.That(GetRoom(map, TrapPosition).Marker, Is.EqualTo(DungeonMapMarker.Unknown));
        }

        [Test]
        public void BossStartsHidden()
        {
            DungeonMapRoomInfo boss = GetRoom(DungeonMapSnapshot.Create(CreateDungeon()), FinalPosition);

            Assert.That(boss.Marker, Is.EqualTo(DungeonMapMarker.Unknown));
        }

        [Test]
        public void BossAppearsFromNeighbor()
        {
            DungeonRunState dungeon = CreateDungeon();
            dungeon.TryMove(RoomDirection.East);

            DungeonMapRoomInfo boss = GetRoom(DungeonMapSnapshot.Create(dungeon), FinalPosition);

            Assert.That(boss.Marker, Is.EqualTo(DungeonMapMarker.Boss));
            Assert.That(boss.IsVisited, Is.False);
        }

        [Test]
        public void VisitedBossStaysVisible()
        {
            DungeonRunState dungeon = CreateDungeon();
            dungeon.TryMove(RoomDirection.East);
            dungeon.TryMove(RoomDirection.North);
            dungeon.TryMove(RoomDirection.West);

            DungeonMapRoomInfo boss = GetRoom(DungeonMapSnapshot.Create(dungeon), FinalPosition);

            Assert.That(boss.Marker, Is.EqualTo(DungeonMapMarker.Boss));
            Assert.That(boss.IsVisited, Is.True);
        }

        [Test]
        public void VisitedChestAppears()
        {
            DungeonRunState dungeon = CreateDungeon(true);
            dungeon.TryMove(RoomDirection.East);

            Assert.That(GetRoom(DungeonMapSnapshot.Create(dungeon), EnemyPosition).HasUnopenedChest, Is.True);
        }

        [Test]
        public void ChestAndRoomTypeBothShow()
        {
            DungeonRunState dungeon = CreateDungeon(true);
            dungeon.TryMove(RoomDirection.East);

            DungeonMapRoomInfo enemy = GetRoom(DungeonMapSnapshot.Create(dungeon), EnemyPosition);

            Assert.That(enemy.Marker, Is.EqualTo(DungeonMapMarker.Combat));
            Assert.That(enemy.HasUnopenedChest, Is.True);
        }

        [Test]
        public void OpenedChestDisappears()
        {
            DungeonRunState dungeon = CreateDungeon(true);
            dungeon.TryMove(RoomDirection.East);
            int chestId = dungeon.GetRoomState(EnemyPosition).Chests[0].Id;

            Assert.That(dungeon.MarkChestOpened(EnemyPosition, chestId), Is.True);
            Assert.That(GetRoom(DungeonMapSnapshot.Create(dungeon), EnemyPosition).HasUnopenedChest, Is.False);
            Assert.That(dungeon.MarkChestOpened(EnemyPosition, chestId), Is.False);
        }

        [Test]
        public void UnvisitedChestStaysHidden()
        {
            DungeonMapRoomInfo enemy = GetRoom(DungeonMapSnapshot.Create(CreateDungeon(true)), EnemyPosition);

            Assert.That(enemy.HasUnopenedChest, Is.False);
        }

        [Test]
        public void StarterHasNoIcon()
        {
            DungeonMapRoomInfo starter = GetRoom(DungeonMapSnapshot.Create(CreateDungeon()), StarterPosition);

            Assert.That(starter.IsStarter, Is.True);
            Assert.That(starter.Marker, Is.EqualTo(DungeonMapMarker.None));
        }

        [Test]
        public void ReturningKeepsRoomsVisited()
        {
            DungeonRunState dungeon = CreateDungeon();
            dungeon.TryMove(RoomDirection.East);
            dungeon.TryMove(RoomDirection.West);

            DungeonMapSnapshot map = DungeonMapSnapshot.Create(dungeon);

            Assert.That(GetRoom(map, StarterPosition).IsVisited, Is.True);
            Assert.That(GetRoom(map, EnemyPosition).IsVisited, Is.True);
        }

        [Test]
        public void SameSeedCreatesSameMap()
        {
            DungeonMapSnapshot first = DungeonMapSnapshot.Create(DungeonRunStateFactory.Create(4, 4, 812345));
            DungeonMapSnapshot second = DungeonMapSnapshot.Create(DungeonRunStateFactory.Create(4, 4, 812345));

            string[] firstRooms = first.Rooms
                .OrderBy(room => room.GridPosition.x)
                .ThenBy(room => room.GridPosition.y)
                .Select(DescribeRoom)
                .ToArray();
            string[] secondRooms = second.Rooms
                .OrderBy(room => room.GridPosition.x)
                .ThenBy(room => room.GridPosition.y)
                .Select(DescribeRoom)
                .ToArray();

            Assert.That(secondRooms, Is.EqualTo(firstRooms));
            Assert.That(second.Connections.Count, Is.EqualTo(first.Connections.Count));
        }

        [Test]
        public void MapChangeFiresAfterTravel()
        {
            DungeonRunState dungeon = CreateDungeon();
            int changes = 0;
            dungeon.MapChanged += () => changes++;

            dungeon.TryMove(RoomDirection.East);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void MapChangeFiresAfterChestOpens()
        {
            DungeonRunState dungeon = CreateDungeon(true);
            int changes = 0;
            dungeon.MapChanged += () => changes++;
            int chestId = dungeon.GetRoomState(EnemyPosition).Chests[0].Id;

            dungeon.MarkChestOpened(EnemyPosition, chestId);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void MapChangeFiresAfterEnemyDefeat()
        {
            DungeonRunState dungeon = CreateDungeon();
            DungeonRoomRuntimeState enemyRoom = dungeon.GetRoomState(EnemyPosition);
            int enemyId = enemyRoom.Enemies[0].Id;
            int changes = 0;
            dungeon.MapChanged += () => changes++;

            Assert.That(dungeon.MarkEnemyDefeated(EnemyPosition, enemyId), Is.True);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(GetRoom(DungeonMapSnapshot.Create(dungeon), EnemyPosition).IsCleared, Is.True);
        }

        [Test]
        public void MissingRoomIsNotReturned()
        {
            DungeonMapSnapshot map = DungeonMapSnapshot.Create(CreateDungeon());

            Assert.That(map.TryGetRoom(new Vector2Int(5, 5), out _), Is.False);
            Assert.Throws<ArgumentNullException>(() => DungeonMapSnapshot.Create(null));
        }

        private static DungeonRunState CreateDungeon(bool addChest = false)
        {
            DungeonRoom starter = new DungeonRoom(StarterPosition, RoomType.Starter);
            DungeonRoom enemy = new DungeonRoom(EnemyPosition, RoomType.Enemy);
            DungeonRoom trap = new DungeonRoom(TrapPosition, RoomType.Trap);
            DungeonRoom final = new DungeonRoom(FinalPosition, RoomType.Final);

            Connect(starter, RoomDirection.East, enemy, RoomDirection.West);
            Connect(starter, RoomDirection.North, trap, RoomDirection.South);
            Connect(enemy, RoomDirection.North, final, RoomDirection.South);
            Connect(trap, RoomDirection.East, final, RoomDirection.West);

            DungeonRoomRuntimeState starterState = new DungeonRoomRuntimeState(StarterPosition, RoomType.Starter);
            DungeonRoomRuntimeState enemyState = new DungeonRoomRuntimeState(EnemyPosition, RoomType.Enemy);
            DungeonRoomRuntimeState trapState = new DungeonRoomRuntimeState(TrapPosition, RoomType.Trap);
            DungeonRoomRuntimeState finalState = new DungeonRoomRuntimeState(FinalPosition, RoomType.Final);
            enemyState.AddEnemy(EnemyKind.Zombie, Vector2.zero);
            if (addChest)
            {
                enemyState.AddChest(Vector2.zero, 123);
            }

            DungeonLayout layout = new DungeonLayout(
                2,
                2,
                new[] { starter, enemy, trap, final },
                StarterPosition,
                FinalPosition);
            Dictionary<Vector2Int, DungeonRoomRuntimeState> states = new Dictionary<Vector2Int, DungeonRoomRuntimeState>
            {
                [StarterPosition] = starterState,
                [EnemyPosition] = enemyState,
                [TrapPosition] = trapState,
                [FinalPosition] = finalState
            };
            LootTableConfiguration loot = new LootTableConfiguration(
                Array.Empty<LootItemDefinition>(),
                new Dictionary<LootSourceType, float>());
            return new DungeonRunState(layout, states, loot, 12345);
        }

        private static void Connect(
            DungeonRoom first,
            RoomDirection firstDirection,
            DungeonRoom second,
            RoomDirection secondDirection)
        {
            first.Connect(firstDirection, second.GridPosition);
            second.Connect(secondDirection, first.GridPosition);
        }

        private static DungeonMapRoomInfo GetRoom(DungeonMapSnapshot map, Vector2Int position)
        {
            Assert.That(map.TryGetRoom(position, out DungeonMapRoomInfo room), Is.True);
            return room;
        }

        private static bool Connects(DungeonMapConnection connection, Vector2Int first, Vector2Int second)
        {
            return connection.From == first && connection.To == second
                || connection.From == second && connection.To == first;
        }

        private static string DescribeRoom(DungeonMapRoomInfo room)
        {
            return $"{room.GridPosition.x},{room.GridPosition.y}:{room.Marker}:{room.IsStarter}";
        }
    }
}
