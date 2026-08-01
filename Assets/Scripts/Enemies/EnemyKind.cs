using UnityEngine;

namespace CryptKnight.Enemies
{
    public enum EnemyKind
    {
        Zombie,
        Spider
    }

    public enum EnemyDifficulty
    {
        Normal,
        Hard
    }

    public sealed class EnemyDifficultyProfile
    {
        private static readonly Color HardTint = new Color(1f, 0.78f, 0.84f, 1f);

        private EnemyDifficultyProfile(
            int maxHealth,
            float movementSpeedMultiplier,
            float attackSpeedMultiplier,
            float projectileSpeedMultiplier,
            int damageMultiplier,
            int projectileCount,
            float animationSpeedMultiplier,
            Color tint)
        {
            MaxHealth = maxHealth;
            MovementSpeedMultiplier = movementSpeedMultiplier;
            AttackSpeedMultiplier = attackSpeedMultiplier;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
            DamageMultiplier = damageMultiplier;
            ProjectileCount = projectileCount;
            AnimationSpeedMultiplier = animationSpeedMultiplier;
            Tint = tint;
        }

        public int MaxHealth { get; }
        public float MovementSpeedMultiplier { get; }
        public float AttackSpeedMultiplier { get; }
        public float ProjectileSpeedMultiplier { get; }
        public int DamageMultiplier { get; }
        public int ProjectileCount { get; }
        public float AnimationSpeedMultiplier { get; }
        public Color Tint { get; }

        public static EnemyDifficultyProfile Get(EnemyKind kind, EnemyDifficulty difficulty)
        {
            int baseHealth = kind == EnemyKind.Zombie ? 5 : 3;
            if (difficulty == EnemyDifficulty.Hard)
            {
                // Hard variants share health and pacing boosts, while projectile tuning stays enemy-specific.
                float projectileSpeedMultiplier = kind == EnemyKind.Spider ? 1f : 1.5f;
                int damageMultiplier = kind == EnemyKind.Zombie ? 1 : 2;
                int projectileCount = kind == EnemyKind.Spider ? 2 : 1;
                return new EnemyDifficultyProfile(
                    baseHealth * 2,
                    2f,
                    2f,
                    projectileSpeedMultiplier,
                    damageMultiplier,
                    projectileCount,
                    2f,
                    HardTint);
            }

            return new EnemyDifficultyProfile(baseHealth, 1f, 1f, 1f, 1, 1, 1f, Color.white);
        }
    }

    public static class EnemyProjectileSpread
    {
        private const float TotalSpreadDegrees = 15f;

        public static Vector2[] CreateDirections(Vector2 aimDirection, int projectileCount)
        {
            int safeCount = Mathf.Max(1, projectileCount);
            Vector2 centerDirection = aimDirection.sqrMagnitude > 0.001f
                ? aimDirection.normalized
                : Vector2.right;
            Vector2[] directions = new Vector2[safeCount];

            if (safeCount == 1)
            {
                directions[0] = centerDirection;
                return directions;
            }

            float angleStep = TotalSpreadDegrees / (safeCount - 1);
            float firstAngle = -TotalSpreadDegrees * 0.5f;
            for (int i = 0; i < safeCount; i++)
            {
                directions[i] = Rotate(centerDirection, firstAngle + angleStep * i);
            }

            return directions;
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            return new Vector2(
                direction.x * cosine - direction.y * sine,
                direction.x * sine + direction.y * cosine).normalized;
        }
    }
}
