using CryptKnight.Application;
using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SpiderEnemyAI : MonoBehaviour
    {
        private const float DetectionRadius = 20f;
        private const float JumpIntervalSeconds = 1.5f;
        private const float AttackDelayAfterJumpSeconds = 1.5f;
        private const float JumpDistance = 1.875f;
        private const float JumpDurationSeconds = 0.60f;
        private const float MaxJumpStepSeconds = 0.05f;
        private const float ProjectileSpeed = 4.6f;
        private const float ProjectileRadius = 0.135f;
        private const float ProjectileLifetimeSeconds = 5f;
        private const int ProjectileDamage = 1;
        private const int ProjectileBounces = 1;

        private Transform player;
        private Transform projectileParent;
        private Rigidbody2D body;
        private EnemySpriteAnimator animator;
        private Rect roomBounds;
        private float nextJumpTime;
        private float pendingAttackTime;
        private Vector2 jumpStartPosition;
        private Vector2 jumpEndPosition;
        private Vector2 jumpShotDirection;
        private float jumpElapsed;
        private bool isJumping;
        private bool hasPendingAttack;

        public Vector2 LastJumpPosition { get; private set; }
        public Vector2 LastShotDirection { get; private set; }
        public bool IsJumping => isJumping;
        public bool HasPendingAttack => hasPendingAttack;

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

            if (isJumping)
            {
                AdvanceJump(Time.deltaTime, Time.time);
                return;
            }

            if (TryResolvePendingAttack(Time.time))
            {
                return;
            }

            TryAttack(Time.time);
        }

        public void Initialize(Transform playerTarget, Transform projectileRoot, Rect playableBounds, float phaseOffsetSeconds = 0f)
        {
            player = playerTarget;
            projectileParent = projectileRoot;
            roomBounds = playableBounds;
            body = body != null ? body : GetComponent<Rigidbody2D>();
            animator = animator != null ? animator : GetComponentInChildren<EnemySpriteAnimator>();
            ConfigureBody();
            nextJumpTime = Time.time + JumpIntervalSeconds + Mathf.Max(0f, phaseOffsetSeconds);
        }

        public bool TryAttack(float currentTime)
        {
            if (isJumping || hasPendingAttack || !CanDetectPlayer() || currentTime < nextJumpTime)
            {
                return false;
            }

            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 playerPosition = player.position;
            Vector2 shotDirection = GetDiagonalDirection(currentPosition, playerPosition);
            Vector2 jumpPosition = GetJumpPosition(currentPosition, playerPosition, JumpDistance, roomBounds);
            StartJump(currentPosition, jumpPosition, shotDirection);
            SchedulePendingAttack(currentTime);
            animator?.SetMovement(jumpPosition - currentPosition);
            LastJumpPosition = jumpPosition;
            return true;
        }

        public bool AdvanceJump(float deltaTime)
        {
            return AdvanceJump(deltaTime, Time.time);
        }

        public bool AdvanceJump(float deltaTime, float currentTime)
        {
            if (!isJumping)
            {
                return false;
            }

            // Cap each step so a single long editor/game frame does not make the spider appear to teleport.
            jumpElapsed += Mathf.Min(Mathf.Max(0f, deltaTime), MaxJumpStepSeconds);
            float progress = Mathf.Clamp01(jumpElapsed / JumpDurationSeconds);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetPosition(Vector2.Lerp(jumpStartPosition, jumpEndPosition, easedProgress));

            if (progress < 1f)
            {
                return true;
            }

            isJumping = false;
            SetPosition(jumpEndPosition);
            animator?.SetMovement(Vector2.zero);
            return true;
        }

        public bool TryResolvePendingAttack(float currentTime)
        {
            if (!hasPendingAttack || currentTime < pendingAttackTime)
            {
                return false;
            }

            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 shotDirection = player != null
                ? GetDiagonalDirection(currentPosition, player.position)
                : jumpShotDirection;

            hasPendingAttack = false;
            animator?.PlayAttack(shotDirection);
            FireProjectile(shotDirection);
            nextJumpTime = currentTime + JumpIntervalSeconds;
            return true;
        }

        public static Vector2 GetDiagonalDirection(Vector2 enemyPosition, Vector2 playerPosition)
        {
            Vector2 delta = playerPosition - enemyPosition;
            float x = delta.x < -0.001f ? -1f : 1f;
            float y = delta.y < -0.001f ? -1f : 1f;
            return new Vector2(x, y).normalized;
        }

        public static Vector2 GetJumpPosition(Vector2 enemyPosition, Vector2 playerPosition, float distance, Rect bounds)
        {
            Vector2 diagonal = GetDiagonalDirection(enemyPosition, playerPosition);
            return ClampToBounds(enemyPosition + diagonal * distance, bounds);
        }

        private void StartJump(Vector2 startPosition, Vector2 endPosition, Vector2 shotDirection)
        {
            jumpStartPosition = startPosition;
            jumpEndPosition = endPosition;
            jumpShotDirection = shotDirection;
            jumpElapsed = 0f;
            isJumping = true;
        }

        private void SchedulePendingAttack(float currentTime)
        {
            // Separate the jump and attack beats so the spider movement is readable before it shoots.
            hasPendingAttack = true;
            pendingAttackTime = currentTime + AttackDelayAfterJumpSeconds;
        }

        private bool CanDetectPlayer()
        {
            return player != null && ((Vector2)player.position - (Vector2)transform.position).sqrMagnitude <= DetectionRadius * DetectionRadius;
        }

        private void FireProjectile(Vector2 direction)
        {
            LastShotDirection = direction;
            ProjectileFactory.CreateCircleProjectile(
                "Spider Projectile",
                (Vector2)transform.position + direction * 0.65f,
                direction,
                DamageableTarget.Player,
                ProjectileDamage,
                ProjectileSpeed,
                ProjectileRadius,
                ProjectileLifetimeSeconds,
                new Color(0.85f, 0.25f, 1f, 1f),
                projectileParent != null ? projectileParent : transform.parent,
                roomBounds,
                ProjectileBounces,
                ProjectileVisualStyle.SpiderPurple);
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
