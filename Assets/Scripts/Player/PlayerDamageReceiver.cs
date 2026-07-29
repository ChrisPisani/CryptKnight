using CryptKnight.Application;
using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Player
{
    public sealed class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        public DamageableTarget TargetType => DamageableTarget.Player;

        public void ApplyDamage(float damage)
        {
            // Player health is stored in half-hearts, so any positive fractional hit costs at least one unit.
            GameManager.Instance.DamagePlayer(Mathf.CeilToInt(Mathf.Max(0f, damage)));
        }
    }
}
