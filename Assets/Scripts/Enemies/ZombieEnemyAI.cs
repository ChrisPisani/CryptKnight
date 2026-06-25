using CryptKnight.Application;
using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ZombieEnemyAI : MonoBehaviour
    {
        private const float DetectionRadius = 20f;
        private const float MoveSpeed = 1.15f;
        private const float AttackIntervalSeconds = 2f;
        private const float LungeDistance = 0.45f;
        private const float ProjectileSpeed = 4.8f;
        private const float ProjectileRadius = 0.135f;
        private const float ProjectileLifetimeSeconds = 5f;
        private const int ProjectileDamage = 1;

        private Transform player;
        private Transform projectileParent;
        private Rigidbody2D body;
        private EnemySpriteAnimator animator;
        private Rect roomBounds;
        private float nextAttackTime;

        public Vector2 LastShotDirection { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<EnemySpriteAnimator>();
            ConfigureBody();
        }

        private void Update()
        {
            if (GameManager.Instance.IsGameplayPaused)
            {
                return;
            }

            TryAttack(Time.time);
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.IsGameplayPaused)
            {
                return;
            }

            MoveTowardPlayer(Time.fixedDeltaTime);
        }

        public void Initialize(Transform playerTarget, Transform projectileRoot, Rect playableBounds, float phaseOffsetSeconds = 0f)
        {
            player = playerTarget;
            projectileParent = projectileRoot;
            roomBounds = playableBounds;
            body = body != null ? body : GetComponent<Rigidbody2D>();
            animator = animator != null ? animator : GetComponentInChildren<EnemySpriteAnimator>();
            ConfigureBody();
            nextAttackTime = Time.time + AttackIntervalSeconds + Mathf.Max(0f, phaseOffsetSeconds);
        }

        public bool MoveTowardPlayer(float deltaTime)
        {
            if (!CanDetectPlayer())
            {
                animator?.SetMovement(Vector2.zero);
                return false;
            }

            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 direction = GetAimDirection(currentPosition, player.position);
            Vector2 nextPosition = Vector2.MoveTowards(currentPosition, player.position, MoveSpeed * deltaTime);
            nextPosition = ClampToBounds(nextPosition, roomBounds);

            if (body != null)
            {
                body.MovePosition(nextPosition);
                if (!UnityEngine.Application.isPlaying)
                {
                    body.position = nextPosition;
                }
            }
            else
            {
                transform.position = nextPosition;
            }

            animator?.SetMovement(direction);
            return true;
        }

        public bool TryAttack(float currentTime)
        {
            if (!CanDetectPlayer() || currentTime < nextAttackTime)
            {
                return false;
            }

            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 targetPosition = player.position;
            Vector2 direction = GetAimDirection(currentPosition, targetPosition);
            Vector2 lungePosition = GetLungePosition(currentPosition, targetPosition, LungeDistance, roomBounds);
            SetPosition(lungePosition);
            animator?.PlayAttack(direction);
            FireProjectile(direction);
            nextAttackTime = currentTime + AttackIntervalSeconds;
            return true;
        }

        public static Vector2 GetAimDirection(Vector2 enemyPosition, Vector2 playerPosition)
        {
            Vector2 direction = playerPosition - enemyPosition;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.left;
        }

        public static Vector2 GetLungePosition(Vector2 enemyPosition, Vector2 playerPosition, float distance, Rect bounds)
        {
            Vector2 direction = GetAimDirection(enemyPosition, playerPosition);
            return ClampToBounds(enemyPosition + direction * distance, bounds);
        }

        private bool CanDetectPlayer()
        {
            return player != null && ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude <= DetectionRadius * DetectionRadius;
        }

        private void FireProjectile(Vector2 direction)
        {
            LastShotDirection = direction;
            ProjectileFactory.CreateCircleProjectile(
                "Zombie Projectile",
                (Vector2)transform.position + direction * 0.65f,
                direction,
                DamageableTarget.Player,
                ProjectileDamage,
                ProjectileSpeed,
                ProjectileRadius,
                ProjectileLifetimeSeconds,
                new Color(1f, 0.25f, 0.2f, 1f),
                projectileParent != null ? projectileParent : transform.parent);
        }

        private void SetPosition(Vector2 position)
        {
            transform.position = position;
            if (body != null)
            {
                body.position = position;
            }
        }

        private void ConfigureBody()
        {
            if (body == null)
            {
                return;
            }

            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private static Vector2 ClampToBounds(Vector2 position, Rect bounds)
        {
            return new Vector2(
                Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(position.y, bounds.yMin, bounds.yMax));
        }
    }
}
