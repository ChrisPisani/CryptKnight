namespace CryptKnight.Combat
{
    public interface IDamageable
    {
        DamageableTarget TargetType { get; }
        void ApplyDamage(int damage);
    }
}
