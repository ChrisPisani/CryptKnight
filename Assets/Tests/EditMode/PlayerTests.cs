using CryptKnight.Application;
using CryptKnight.Data;
using CryptKnight.Player;
using CryptKnight.Gameplay;
using CryptKnight.Combat;
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

            GameObject gameManager = GameObject.Find("Game Manager");
            if (gameManager != null)
            {
                Object.DestroyImmediate(gameManager);
            }
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

        [Test]
        public void SmallMovementInputIsUnchanged()
        {
            Vector2 input = new Vector2(0.25f, -0.5f);

            Assert.That(PlayerMovement.NormalizeInput(input), Is.EqualTo(input));
        }

        [Test]
        public void AnimationDirectionUsesStrongestAxis()
        {
            Assert.That(PlayerIdleAnimator.GetCardinalDirection(new Vector2(3f, 1f)), Is.EqualTo(CardinalDirection.Right));
            Assert.That(PlayerIdleAnimator.GetCardinalDirection(new Vector2(-3f, 1f)), Is.EqualTo(CardinalDirection.Left));
            Assert.That(PlayerIdleAnimator.GetCardinalDirection(new Vector2(1f, 3f)), Is.EqualTo(CardinalDirection.Up));
            Assert.That(PlayerIdleAnimator.GetCardinalDirection(new Vector2(1f, -3f)), Is.EqualTo(CardinalDirection.Down));
        }

        [Test]
        public void AnimatorStoresMovementDirection()
        {
            GameObject playerVisual = new GameObject("Player Visual");
            createdObjects.Add(playerVisual);

            playerVisual.AddComponent<SpriteRenderer>();
            PlayerIdleAnimator animator = playerVisual.AddComponent<PlayerIdleAnimator>();
            InvokeAwake(animator);

            animator.SetMovement(Vector2.left);

            FieldInfo facingField = typeof(PlayerIdleAnimator).GetField("facingDirection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(facingField, Is.Not.Null);
            Assert.That(facingField.GetValue(animator), Is.EqualTo(CardinalDirection.Left));
        }

        [Test]
        public void AttackDefaultsWorkWithoutRun()
        {
            GameObject player = new GameObject("Player");
            createdObjects.Add(player);
            PlayerAttackController attackController = player.AddComponent<PlayerAttackController>();

            Assert.That(InvokePrivate<float>(attackController, "GetDamage"), Is.EqualTo(1f));
            Assert.That(InvokePrivate<float>(attackController, "GetAttackCooldownSeconds"), Is.EqualTo(1f));
        }

        [Test]
        public void MultipleShotsUseCenteredFan()
        {
            Vector2[] singleDirection = PlayerProjectileSpread.CreateDirections(Vector2.up, 0);
            Vector2[] directions = PlayerProjectileSpread.CreateDirections(Vector2.right, 3);

            Assert.That(singleDirection, Has.Length.EqualTo(1));
            Assert.That(singleDirection[0], Is.EqualTo(Vector2.up));
            Assert.That(directions, Has.Length.EqualTo(3));
            Assert.That(Vector2.SignedAngle(Vector2.right, directions[0]), Is.EqualTo(-10f).Within(0.001f));
            Assert.That(Vector2.SignedAngle(Vector2.right, directions[1]), Is.EqualTo(0f).Within(0.001f));
            Assert.That(Vector2.SignedAngle(Vector2.right, directions[2]), Is.EqualTo(10f).Within(0.001f));

            Vector2[] cappedDirections = PlayerProjectileSpread.CreateDirections(Vector2.zero, 10);
            Assert.That(Vector2.SignedAngle(Vector2.right, cappedDirections[0]), Is.EqualTo(-30f).Within(0.001f));
            Assert.That(Vector2.SignedAngle(Vector2.right, cappedDirections[9]), Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void PlayerShotsUseRunStats()
        {
            GameManager.Instance.StartNewRun();
            GameManager.Instance.CurrentRun.AddStatModifier(new PlayerStatModifier(
                damageBonus: 1,
                projectileCountBonus: 2,
                projectileSpeedBonus: 2f,
                projectileBouncesBonus: 1,
                projectileSizeBonus: 0.5f));

            GameObject room = new GameObject("Room");
            GameObject player = new GameObject("Player");
            createdObjects.Add(room);
            player.transform.SetParent(room.transform, false);
            PlayerAttackController attackController = player.AddComponent<PlayerAttackController>();

            MethodInfo fireMethod = typeof(PlayerAttackController).GetMethod("Fire", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fireMethod, Is.Not.Null);
            fireMethod.Invoke(attackController, new object[] { Vector2.right });

            ProjectileController[] projectiles = room.GetComponentsInChildren<ProjectileController>();
            Assert.That(projectiles, Has.Length.EqualTo(3));
            foreach (ProjectileController projectile in projectiles)
            {
                Assert.That(projectile.Damage, Is.EqualTo(2f));
                Assert.That(projectile.BouncesRemaining, Is.EqualTo(1));
                Assert.That(projectile.GetComponent<Rigidbody2D>().linearVelocity.magnitude, Is.EqualTo(10f).Within(0.001f));
                Assert.That(projectile.GetComponent<CircleCollider2D>().radius, Is.EqualTo(0.2025f).Within(0.001f));
            }
        }

        [Test]
        public void AttackUpdateHandlesPauseAndMissingInput()
        {
            GameObject player = new GameObject("Player");
            createdObjects.Add(player);
            PlayerAttackController attackController = player.AddComponent<PlayerAttackController>();
            GameManager.Instance.StartNewRun();

            MethodInfo awakeMethod = typeof(PlayerAttackController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo updateMethod = typeof(PlayerAttackController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo aimMethod = typeof(PlayerAttackController).GetMethod("GetAimDirection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awakeMethod, Is.Not.Null);
            Assert.That(updateMethod, Is.Not.Null);
            Assert.That(aimMethod, Is.Not.Null);

            awakeMethod.Invoke(attackController, null);
            Vector2 initialAim = (Vector2)aimMethod.Invoke(attackController, null);
            Assert.That(float.IsNaN(initialAim.x), Is.False);
            Assert.That(float.IsNaN(initialAim.y), Is.False);

            GameManager.Instance.SetGameplayPaused(true);
            updateMethod.Invoke(attackController, null);
            GameManager.Instance.SetGameplayPaused(false);
            updateMethod.Invoke(attackController, null);

            GameObject cameraObject = new GameObject("Main Camera");
            createdObjects.Add(cameraObject);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);

            Vector2 cameraAim = (Vector2)aimMethod.Invoke(attackController, null);
            Assert.That(float.IsNaN(cameraAim.x), Is.False);
            Assert.That(float.IsNaN(cameraAim.y), Is.False);
        }

        [Test]
        public void DamageReceiverTargetsPlayer()
        {
            GameObject player = new GameObject("Player");
            createdObjects.Add(player);

            PlayerDamageReceiver receiver = player.AddComponent<PlayerDamageReceiver>();

            Assert.That(receiver.TargetType, Is.EqualTo(DamageableTarget.Player));
        }

        [Test]
        public void PlayerDamageRoundsUpToHalfHeart()
        {
            GameManager.Instance.StartNewRun();
            GameObject player = new GameObject("Player");
            createdObjects.Add(player);
            PlayerDamageReceiver receiver = player.AddComponent<PlayerDamageReceiver>();

            receiver.ApplyDamage(0.5f);

            Assert.That(GameManager.Instance.CurrentRun.CurrentHealth, Is.EqualTo(5));
        }

        [Test]
        public void RuntimePlayerVisualIsSmallerAndNearHitboxCenter()
        {
            GameObject parent = new GameObject("Runtime Parent");
            GameObject controllerObject = new GameObject("Gameplay Controller");
            createdObjects.Add(parent);
            createdObjects.Add(controllerObject);

            GameplaySceneController controller = controllerObject.AddComponent<GameplaySceneController>();
            MethodInfo createPlayerMethod = typeof(GameplaySceneController).GetMethod("CreatePlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(createPlayerMethod, Is.Not.Null);

            Transform player = (Transform)createPlayerMethod.Invoke(controller, new object[] { parent.transform });
            Transform visual = player.Find("Player Visual");
            CircleCollider2D hitbox = player.GetComponent<CircleCollider2D>();

            Assert.That(visual, Is.Not.Null);
            Assert.That(hitbox, Is.Not.Null);
            Assert.That(visual.localScale.x, Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(visual.localScale.y, Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(hitbox.radius, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(visual.localPosition.x, Is.EqualTo(hitbox.offset.x).Within(0.001f));
            Assert.That(Mathf.Abs(visual.localPosition.y - hitbox.offset.y), Is.LessThanOrEqualTo(hitbox.radius * 0.3f));
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

        private static void InvokeAwake(PlayerIdleAnimator animator)
        {
            MethodInfo awakeMethod = typeof(PlayerIdleAnimator).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awakeMethod, Is.Not.Null);
            awakeMethod.Invoke(animator, null);
        }

        private static T InvokePrivate<T>(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (T)method.Invoke(target, null);
        }
    }
}
