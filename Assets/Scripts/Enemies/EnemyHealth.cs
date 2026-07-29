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
        public float CurrentHealth { get; private set; }
        // update room state and roll defeat loot before the enemy is destroyed
        public event Action<EnemyHealth> Died;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void Initialize(int maximumHealth)
        {
            Initialize(maximumHealth, maximumHealth);
        }

        public void Initialize(int maximumHealth, float currentHealth)
        {
            maxHealth = Mathf.Max(1, maximumHealth);
            CurrentHealth = Mathf.Clamp(currentHealth, Mathf.Epsilon, maxHealth);
        }

        public void ApplyDamage(float damage)
        {
            if (damage <= 0f || CurrentHealth <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            if (CurrentHealth <= 0f)
            {
                Died?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
