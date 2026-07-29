using UnityEngine;

namespace CryptKnight.Player
{
    public static class PlayerProjectileSpread
    {
        private const float PreferredAngleStep = 10f;
        private const float MaximumSpread = 60f;

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

            float angleStep = Mathf.Min(PreferredAngleStep, MaximumSpread / (safeCount - 1));
            float firstAngle = -angleStep * (safeCount - 1) * 0.5f;
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
