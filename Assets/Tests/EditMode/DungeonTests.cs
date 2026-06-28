using System;
using System.Linq;
using CryptKnight.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class DungeonTests
    {
        [Test]
        public void GeneratedRoomsHaveTypes()
        {
            DungeonLayout layout = DungeonLayoutGenerator.Generate(4, 4, 12345);

            Assert.That(layout.Rooms, Has.Count.EqualTo(16));
            Assert.That(layout.StartRoom.RoomType, Is.EqualTo(RoomType.Starter));
            Assert.That(layout.FinalRoom.RoomType, Is.EqualTo(RoomType.Final));
            Assert.That(layout.Rooms.Any(room => room.RoomType == RoomType.Enemy), Is.True);
            Assert.That(layout.Rooms.Any(room => room.RoomType == RoomType.Trap), Is.True);

            foreach (DungeonRoom room in layout.Rooms)
            {
                Assert.That(Enum.IsDefined(typeof(RoomType), room.RoomType), Is.True, room.GridPosition.ToString());
            }
        }

        [Test]
        public void GeneratedRoomsAreConnected()
        {
            DungeonLayout layout = DungeonLayoutGenerator.Generate(4, 4, 12345);

            Assert.That(layout.AreAllRoomsConnected(), Is.True);
        }

        [Test]
        public void DoorConnectionsAreTwoWay()
        {
            DungeonLayout layout = DungeonLayoutGenerator.Generate(4, 4, 12345);

            foreach (DungeonRoom room in layout.Rooms)
            {
                foreach (Vector2Int connectedPosition in room.Connections.Values)
                {
                    Assert.That(layout.TryGetRoom(connectedPosition, out DungeonRoom connectedRoom), Is.True);
                    Assert.That(connectedRoom.IsConnectedTo(room.GridPosition), Is.True);
                }
            }
        }

        [Test]
        public void NavigatorMovesThroughConnectedDoors()
        {
            DungeonLayout layout = DungeonLayoutGenerator.Generate(4, 4, 12345);
            DungeonRoomNavigator navigator = new DungeonRoomNavigator(layout);
            RoomDirection direction = navigator.CurrentRoom.Connections.Keys.First();
            Vector2Int expectedRoomPosition = navigator.CurrentRoom.Connections[direction];

            bool moved = navigator.TryMove(direction);

            Assert.That(moved, Is.True);
            Assert.That(navigator.CurrentRoom.GridPosition, Is.EqualTo(expectedRoomPosition));
        }

        [Test]
        public void NavigatorBlocksMissingDoors()
        {
            DungeonLayout layout = DungeonLayoutGenerator.Generate(1, 1, 12345);
            DungeonRoomNavigator navigator = new DungeonRoomNavigator(layout);

            bool moved = navigator.TryMove(RoomDirection.North);

            Assert.That(moved, Is.False);
            Assert.That(navigator.CurrentRoom.GridPosition, Is.EqualTo(layout.StartPosition));
        }

        [Test]
        public void StartAndFinalRoomsVaryBySeed()
        {
            DungeonLayout firstLayout = DungeonLayoutGenerator.Generate(4, 4, 12345);
            DungeonLayout secondLayout = DungeonLayoutGenerator.Generate(4, 4, 54321);

            Assert.That(firstLayout.StartPosition, Is.Not.EqualTo(secondLayout.StartPosition));
            Assert.That(firstLayout.FinalPosition, Is.Not.EqualTo(secondLayout.FinalPosition));
        }

        [Test]
        public void FinalRoomIsAwayFromStart()
        {
            DungeonLayout layout = DungeonLayoutGenerator.Generate(4, 4, 12345);
            int distance = Mathf.Abs(layout.FinalPosition.x - layout.StartPosition.x) + Mathf.Abs(layout.FinalPosition.y - layout.StartPosition.y);

            Assert.That(distance, Is.GreaterThanOrEqualTo(3));
        }
    }
}
