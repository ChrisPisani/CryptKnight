using NUnit.Framework;
using CryptKnight.Dungeon;
using CryptKnight.Gameplay;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class CollisionTests
    {
        private const float RoomWidth = 13.5f;
        private const float RoomHeight = 7.5f;
        private const float WallThickness = 0.75f;
        private const float PlayerRadius = 0.35f;

        private readonly System.Collections.Generic.List<Object> createdObjects = new System.Collections.Generic.List<Object>();

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
        public void PlayerStartsClearOfWall()
        {
            Collider2D player = CreateCircleCollider("Player", Vector2.zero, PlayerRadius);
            Collider2D wall = CreateWallCollider("East Wall", GetEastWallPosition(), GetEastWallSize());

            ColliderDistance2D distance = player.Distance(wall);

            Assert.That(distance.isOverlapped, Is.False);
        }

        [Test]
        public void PlayerHitsWallAtBoundary()
        {
            float touchingPlayerX = RoomWidth * 0.5f + PlayerRadius - 0.01f;
            Collider2D player = CreateCircleCollider("Player", new Vector2(touchingPlayerX, 0f), PlayerRadius);
            Collider2D wall = CreateWallCollider("East Wall", GetEastWallPosition(), GetEastWallSize());

            Physics2D.SyncTransforms();

            ColliderDistance2D distance = player.Distance(wall);

            Assert.That(distance.isOverlapped, Is.True);
        }

        [Test]
        public void DoorEntryRequiresInputTowardDoor()
        {
            Assert.That(RoomDoorTrigger.IsInputEnteringDoor(RoomDirection.North, Vector2.up), Is.True);
            Assert.That(RoomDoorTrigger.IsInputEnteringDoor(RoomDirection.North, Vector2.right), Is.False);
            Assert.That(RoomDoorTrigger.IsInputEnteringDoor(RoomDirection.East, Vector2.right), Is.True);
            Assert.That(RoomDoorTrigger.IsInputEnteringDoor(RoomDirection.East, Vector2.up), Is.False);
        }

        private Collider2D CreateCircleCollider(string objectName, Vector2 position, float radius)
        {
            GameObject gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            gameObject.transform.position = position;

            CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = radius;
            return collider;
        }

        private Collider2D CreateWallCollider(string objectName, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            gameObject.transform.position = position;

            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = size;
            return collider;
        }

        private static Vector2 GetEastWallPosition()
        {
            return new Vector2(RoomWidth * 0.5f + WallThickness * 0.5f, 0f);
        }

        private static Vector2 GetEastWallSize()
        {
            return new Vector2(WallThickness, RoomHeight);
        }
    }
}
