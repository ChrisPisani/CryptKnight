using System.Collections.Generic;
using CryptKnight.Content;
using CryptKnight.Dungeon;
using CryptKnight.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class EnvironmentTests
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
        public void AssetsLoadByPathAndName()
        {
            const string archwayPath = "Art/Environment/door_archway_environment_half_row";

            Sprite[] sprites = RuntimeAssetLoader.LoadSprites(archwayPath);

            Assert.That(RuntimeAssetLoader.LoadSprites(string.Empty), Is.Empty);
            Assert.That(sprites, Has.Length.EqualTo(2));
            Assert.That(RuntimeAssetLoader.LoadSprite(archwayPath), Is.SameAs(sprites[0]));
            Assert.That(RuntimeAssetLoader.LoadSprite(archwayPath, "door_archway_environment_north"), Is.Not.Null);
            Assert.That(RuntimeAssetLoader.LoadSprite(archwayPath, "missing_sprite"), Is.Null);
            Assert.That(RuntimeAssetLoader.LoadSprite(string.Empty), Is.Null);
            Assert.That(RuntimeAssetLoader.LoadFont("Art/UI/Bloodlines/Fonts/MedievalSharp-Regular"), Is.Not.Null);
            Assert.That(RuntimeAssetLoader.LoadFont(" "), Is.Null);
        }

        [Test]
        public void RoomGeometryHandlesEveryDirection()
        {
            RoomDirection[] directions =
            {
                RoomDirection.North,
                RoomDirection.East,
                RoomDirection.South,
                RoomDirection.West
            };

            for (int i = 0; i < directions.Length; i++)
            {
                RoomDirection direction = directions[i];
                Assert.That(DungeonRoomGeometry.GetDoorPosition(direction), Is.Not.EqualTo(Vector2.zero));
                Assert.That(DungeonRoomGeometry.GetSpawnPositionForEntry(direction), Is.Not.EqualTo(Vector2.zero));
            }

            Assert.That(DungeonRoomGeometry.IsHorizontalDoor(RoomDirection.North), Is.True);
            Assert.That(DungeonRoomGeometry.IsHorizontalDoor(RoomDirection.South), Is.True);
            Assert.That(DungeonRoomGeometry.IsHorizontalDoor(RoomDirection.East), Is.False);
            Assert.That(DungeonRoomGeometry.IsHorizontalDoor(RoomDirection.West), Is.False);
            Assert.That(DungeonRoomGeometry.GetDoorPosition((RoomDirection)999), Is.EqualTo(Vector2.zero));
            Assert.That(DungeonRoomGeometry.GetSpawnPositionForEntry((RoomDirection)999), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void RewardsStayInsideRoomBounds()
        {
            Rect bounds = DungeonRoomGeometry.PlayableBounds;

            Vector2 clamped = DungeonRoomGeometry.ClampToPlayableBounds(new Vector2(100f, -100f));

            Assert.That(clamped.x, Is.EqualTo(bounds.xMax));
            Assert.That(clamped.y, Is.EqualTo(bounds.yMin));
        }

        [Test]
        public void EnvironmentBuildsWallsAndDoors()
        {
            GameObject root = CreateObject("Room Environment Test");
            DungeonRoom room = new DungeonRoom(Vector2Int.zero, RoomType.Starter);
            DungeonRoom north = new DungeonRoom(Vector2Int.up, RoomType.Final);
            DungeonRoom east = new DungeonRoom(Vector2Int.right, RoomType.Enemy);
            DungeonRoom south = new DungeonRoom(Vector2Int.down, RoomType.Trap);
            DungeonRoom west = new DungeonRoom(Vector2Int.left, RoomType.Enemy);
            room.Connect(RoomDirection.North, north.GridPosition);
            room.Connect(RoomDirection.East, east.GridPosition);
            room.Connect(RoomDirection.South, south.GridPosition);
            room.Connect(RoomDirection.West, west.GridPosition);
            DungeonLayout layout = new DungeonLayout(
                3,
                3,
                new[] { room, north, east, south, west },
                room.GridPosition,
                north.GridPosition);

            new DungeonRoomEnvironmentBuilder().Build(root.transform, room, layout, _ => { });

            Assert.That(root.transform.Find("Room Floor"), Is.Not.Null);
            Assert.That(root.transform.Find("Dungeon Wall Frame"), Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<RoomDoorTrigger>(), Has.Length.EqualTo(4));
            Assert.That(root.transform.Find("Door Frame North"), Is.Not.Null);
            Assert.That(root.transform.Find("Door Frame East"), Is.Not.Null);
            Assert.That(root.transform.Find("Door Frame South"), Is.Not.Null);
            Assert.That(root.transform.Find("Door Frame West"), Is.Not.Null);
            Assert.That(root.transform.Find("Door Archway North"), Is.Not.Null);
            Assert.That(root.transform.Find("Door Archway East"), Is.Null);
        }

        private GameObject CreateObject(string name)
        {
            GameObject value = new GameObject(name);
            createdObjects.Add(value);
            return value;
        }
    }
}
