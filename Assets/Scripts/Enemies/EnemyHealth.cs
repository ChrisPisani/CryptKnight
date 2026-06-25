using System;
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
        // update room state and roll defeat loot before the enemy is destroyed
        public event Action<EnemyHealth> Died;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void Initialize(int maximumHealth)
        {
            maxHealth = Mathf.Max(1, maximumHealth);
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
                Died?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
