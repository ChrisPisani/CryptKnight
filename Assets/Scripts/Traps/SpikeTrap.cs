using System;
using CryptKnight.Application;
using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Traps
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SpikeTrap : MonoBehaviour
    {
        private TrapDefinition definition;
        private float nextDamageTime = float.NegativeInfinity;

        public void Initialize(TrapDefinition trapDefinition)
        {
            definition = trapDefinition ?? throw new ArgumentNullException(nameof(trapDefinition));
            if (definition.Kind != TrapKind.Spike)
            {
                throw new ArgumentException("SpikeTrap requires a spike definition.", nameof(trapDefinition));
            }

            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        public bool TryDamage(IDamageable target, float currentTime)
        {
            if (definition == null || target == null || target.TargetType != DamageableTarget.Player)
            {
                return false;
            }

            if (currentTime < nextDamageTime)
            {
                return false;
            }

            target.ApplyDamage(definition.Damage);
            nextDamageTime = currentTime + definition.ActivationIntervalSeconds;
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamageCollider(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamageCollider(other);
        }

        private void TryDamageCollider(Collider2D other)
        {
            if (GameManager.HasInstance && GameManager.Instance.IsGameplayPaused)
            {
                return;
            }

            IDamageable target = other.GetComponentInParent<IDamageable>();
            TryDamage(target, Time.time);
        }
    }
}
