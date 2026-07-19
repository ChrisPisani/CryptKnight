using CryptKnight.Dungeon;
using UnityEngine;

namespace CryptKnight.Enemies
{
    public sealed class EnemyRoomStateTracker : MonoBehaviour
    {
        private RoomEnemyInstance roomEnemy;
        private EnemyHealth enemyHealth;

        public void Initialize(RoomEnemyInstance enemyInstance, EnemyHealth health)
        {
            roomEnemy = enemyInstance;
            enemyHealth = health;
        }

        private void OnDisable()
        {
            if (roomEnemy == null || enemyHealth == null || roomEnemy.IsDefeated)
            {
                return;
            }

            roomEnemy.UpdateRuntime(transform.position, enemyHealth.CurrentHealth);
        }
    }
}
