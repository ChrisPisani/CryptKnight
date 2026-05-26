using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField]
        private int maxHealth = 3;

        public DamageableTarget TargetType => DamageableTarget.Enemy;
        public int CurrentHealth { get; private set; }

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void ApplyDamage(int damage)
        {
            if (damage <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            if (CurrentHealth == 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
