using CryptKnight.Player;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace CryptKnight.Tests.EditMode
{
    public sealed class PlayerTests
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
        public void RigidbodyIsTopDown()
        {
            GameObject player = CreatePlayer();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();

            InvokeAwake(player.GetComponent<PlayerController>());

            Assert.That(body.gravityScale, Is.EqualTo(0f));
            Assert.That(body.freezeRotation, Is.True);
            Assert.That(body.collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
        }

        [Test]
        public void MovementUsesInput()
        {
            Vector2 nextPosition = PlayerMovement.CalculateNextPosition(Vector2.zero, Vector2.right, 5f, Time.fixedDeltaTime);

            Assert.That(nextPosition.x, Is.GreaterThan(0f));
            Assert.That(nextPosition.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DiagonalMovementIsClamped()
        {
            Vector2 nextPosition = PlayerMovement.CalculateNextPosition(Vector2.zero, new Vector2(1f, 1f), 5f, Time.fixedDeltaTime);

            float distance = nextPosition.magnitude;
            float expectedDistance = 5f * Time.fixedDeltaTime;

            Assert.That(distance, Is.EqualTo(expectedDistance).Within(0.001f));
        }

        private GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Test Player");
            createdObjects.Add(player);

            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CircleCollider2D>();
            player.AddComponent<PlayerController>();

            return player;
        }

        private static void InvokeAwake(PlayerController controller)
        {
            MethodInfo awakeMethod = typeof(PlayerController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awakeMethod, Is.Not.Null);
            awakeMethod.Invoke(controller, null);
        }
    }
}
