using NUnit.Framework;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class PhysicsCollisionTests
    {
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
        public void PlayerStartsClearOfRoomWall()
        {
            Collider2D player = CreateCircleCollider("Player", Vector2.zero, 0.35f);
            Collider2D wall = CreateWallCollider("East Wall", new Vector2(9.375f, 0f), new Vector2(0.75f, 10f));

            ColliderDistance2D distance = player.Distance(wall);

            Assert.That(distance.isOverlapped, Is.False);
        }

        [Test]
        public void PlayerTouchesWallAtBoundary()
        {
            Collider2D player = CreateCircleCollider("Player", new Vector2(9.2f, 0f), 0.35f);
            Collider2D wall = CreateWallCollider("East Wall", new Vector2(9.375f, 0f), new Vector2(0.75f, 10f));

            Physics2D.SyncTransforms();

            ColliderDistance2D distance = player.Distance(wall);

            Assert.That(distance.isOverlapped, Is.True);
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
    }
}
