using CryptKnight.Application;
using CryptKnight.Audio;
using CryptKnight.Combat;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CryptKnight.Player
{
    public sealed class PlayerAttackController : MonoBehaviour
    {
        private const float ProjectileSpeed = 8f;
        private const float ProjectileRadius = 0.135f;
        private const float ProjectileLifetimeSeconds = 5f;

        private readonly AttackCooldown cooldown = new AttackCooldown();
        private PlayerIdleAnimator spriteAnimator;

        private void Awake()
        {
            spriteAnimator = GetComponentInChildren<PlayerIdleAnimator>();
        }

        private void Update()
        {
            if (GameManager.Instance.IsGameplayPaused)
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
            Vector2 spawnPosition = (Vector2)transform.position + direction * 0.75f;
            spriteAnimator?.PlayAttack(direction);
            GameSfxPlayer.PlaySwordAttack();

            ProjectileFactory.CreateCircleProjectile(
                "Player Projectile",
                spawnPosition,
                direction,
                DamageableTarget.Enemy,
                GetDamage(),
                ProjectileSpeed,
                ProjectileRadius,
                ProjectileLifetimeSeconds,
                new Color(0.25f, 0.72f, 1f, 1f),
                transform.parent);
        }

        private int GetDamage()
        {
            return GameManager.Instance.CurrentRun?.PlayerStats.Damage ?? 1;
        }

        private float GetAttackCooldownSeconds()
        {
            return GameManager.Instance.CurrentRun?.PlayerStats.AttackCooldownSeconds ?? 1f;
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
