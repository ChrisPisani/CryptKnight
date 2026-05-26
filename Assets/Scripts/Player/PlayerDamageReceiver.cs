using CryptKnight.Application;
using CryptKnight.Combat;
using UnityEngine;

namespace CryptKnight.Player
{
    public sealed class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        public DamageableTarget TargetType => DamageableTarget.Player;

        public void ApplyDamage(int damage)
        {
            GameManager.Instance.DamagePlayer(damage);
        }
    }
}
