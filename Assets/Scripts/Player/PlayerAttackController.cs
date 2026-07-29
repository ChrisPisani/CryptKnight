using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Combat;
using CryptKnight.Data;
using CryptKnight.Gameplay;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.Player
{
    public sealed class PlayerAttackController : MonoBehaviour
    {
        private const float ProjectileRadius = 0.135f;
        private const float ProjectileLifetimeSeconds = 5f;
        private const float LifetimePerBounceSeconds = 2f;

        private readonly AttackCooldown cooldown = new AttackCooldown();
        private PlayerIdleAnimator spriteAnimator;

        private void Awake()
        {
            spriteAnimator = GetComponentInChildren<PlayerIdleAnimator>();
        }

        private void Update()
        {
            if (GameManager.Instance.IsGameplayPaused || GameplayInputGate.IsBlocked)
            {
                return;
            }

            float cooldownSeconds = GetAttackCooldownSeconds();
            if (!IsAttackHeld() || !cooldown.CanAttack(Time.time))
            {
                return;
            }

            Vector2 aimDirection = GetAimDirection();
            if (aimDirection.sqrMagnitude <= 0.001f)
            {
                aimDirection = Vector2.right;
            }

            Fire(aimDirection.normalized);
            cooldown.MarkAttackUsed(Time.time, cooldownSeconds);
        }

        private void Fire(Vector2 direction)
        {
            spriteAnimator?.PlayAttack(direction);
            GameSfxPlayer.PlaySwordAttack();

            PlayerRuntimeStats stats = GetPlayerStats();
            Vector2[] shotDirections = PlayerProjectileSpread.CreateDirections(direction, stats.ProjectileCount);
            float radius = ProjectileRadius * stats.ProjectileSizeMultiplier;
            Rect bounceBounds = InsetBounds(DungeonRoomGeometry.PlayableBounds, radius);
            float lifetimeSeconds = ProjectileLifetimeSeconds + stats.ProjectileBounces * LifetimePerBounceSeconds;

            for (int i = 0; i < shotDirections.Length; i++)
            {
                Vector2 shotDirection = shotDirections[i];
                Vector2 spawnPosition = (Vector2)transform.position + shotDirection * 0.75f;
                ProjectileFactory.CreateCircleProjectile(
                    "Player Projectile",
                    spawnPosition,
                    shotDirection,
                    DamageableTarget.Enemy,
                    stats.Damage,
                    stats.ProjectileSpeed,
                    radius,
                    lifetimeSeconds,
                    new Color(0.25f, 0.72f, 1f, 1f),
                    transform.parent,
                    stats.ProjectileBounces > 0 ? bounceBounds : null,
                    stats.ProjectileBounces,
                    ProjectileVisualStyle.Default,
                    stats.ProjectileSizeMultiplier);
            }
        }

        private float GetDamage()
        {
            return GetPlayerStats().Damage;
        }

        private float GetAttackCooldownSeconds()
        {
            return GetPlayerStats().AttackCooldownSeconds;
        }

        private static PlayerRuntimeStats GetPlayerStats()
        {
            return GameManager.Instance.CurrentRun?.PlayerStats
                ?? new PlayerRuntimeStats(PlayerBaseStats.CreateDefault());
        }

        private static Rect InsetBounds(Rect bounds, float padding)
        {
            float safePadding = Mathf.Max(0f, padding);
            return Rect.MinMaxRect(
                bounds.xMin + safePadding,
                bounds.yMin + safePadding,
                bounds.xMax - safePadding,
                bounds.yMax - safePadding);
        }

        private Vector2 GetAimDirection()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return Vector2.right;
            }

            // Aim through the active camera so mouse position lines up with the room.
            Vector2 pointerPosition = GetPointerScreenPosition();
            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, -camera.transform.position.z));
            return (Vector2)(worldPosition - transform.position);
        }

        private static bool IsAttackHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        private static Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse == null ? Vector2.zero : mouse.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        }
    }
}
