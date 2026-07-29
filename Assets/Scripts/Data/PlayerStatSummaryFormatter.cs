namespace CryptKnight.Data
{
    public static class PlayerStatSummaryFormatter
    {
        public static string Format(GameRunState runState)
        {
            if (runState == null)
            {
                return "No active run";
            }

            return
                $"Health: {FormatHearts(runState.CurrentHealth)} / {FormatHearts(runState.MaxHealth)} hearts\n" +
                $"Damage: {FormatNumber(runState.PlayerStats.Damage)}\n" +
                $"Movement Speed: {FormatNumber(runState.PlayerStats.MovementSpeed)}\n" +
                $"Attack Speed: {FormatNumber(runState.PlayerStats.AttackRate)}\n" +
                $"Projectiles: {runState.PlayerStats.ProjectileCount}\n" +
                $"Projectile Speed: {FormatNumber(runState.PlayerStats.ProjectileSpeed)}\n" +
                $"Projectile Bounces: {runState.PlayerStats.ProjectileBounces}\n" +
                $"Projectile Size: {FormatPercent(runState.PlayerStats.ProjectileSizeMultiplier)}\n" +
                $"Keys: {runState.KeyCount}";
        }

        public static string FormatStatsOnly(GameRunState runState)
        {
            if (runState == null)
            {
                return "No active run";
            }

            return
                $"Damage: {FormatNumber(runState.PlayerStats.Damage)}\n" +
                $"Movement Speed: {FormatNumber(runState.PlayerStats.MovementSpeed)}\n" +
                $"Attack Speed: {FormatNumber(runState.PlayerStats.AttackRate)}\n" +
                $"Projectiles: {runState.PlayerStats.ProjectileCount}\n" +
                $"Projectile Speed: {FormatNumber(runState.PlayerStats.ProjectileSpeed)}\n" +
                $"Projectile Bounces: {runState.PlayerStats.ProjectileBounces}\n" +
                $"Projectile Size: {FormatPercent(runState.PlayerStats.ProjectileSizeMultiplier)}";
        }

        private static string FormatHearts(int halfHearts)
        {
            int fullHearts = halfHearts / 2;
            return halfHearts % 2 == 0 ? fullHearts.ToString() : $"{fullHearts}.5";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##");
        }

        private static string FormatPercent(float multiplier)
        {
            return $"{multiplier * 100f:0.##}%";
        }
    }
}
