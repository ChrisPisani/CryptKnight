namespace CryptKnight.Combat
{
    public sealed class AttackCooldown
    {
        private readonly float cooldownSeconds;
        private float nextAllowedAttackTime;

        public AttackCooldown(float cooldownSeconds)
        {
            this.cooldownSeconds = cooldownSeconds;
        }

        public bool CanAttack(float currentTime)
        {
            return currentTime >= nextAllowedAttackTime;
        }

        public void MarkAttackUsed(float currentTime)
        {
            nextAllowedAttackTime = currentTime + cooldownSeconds;
        }
    }
}
