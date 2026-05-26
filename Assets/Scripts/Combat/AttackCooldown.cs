namespace CryptKnight.Combat
{
    public sealed class AttackCooldown
    {
        private float nextAllowedAttackTime;

        public bool CanAttack(float currentTime)
        {
            return currentTime >= nextAllowedAttackTime;
        }

        public void MarkAttackUsed(float currentTime, float cooldownSeconds)
        {
            nextAllowedAttackTime = currentTime + cooldownSeconds;
        }
    }
}
