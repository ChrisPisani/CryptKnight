using CryptKnight.Dungeon;
using CryptKnight.Player;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    public sealed class RoomDoorTrigger : MonoBehaviour
    {
        private GameplaySceneController controller;
        private RoomDirection direction;

        public void Initialize(GameplaySceneController sceneController, RoomDirection doorDirection)
        {
            controller = sceneController;
            direction = doorDirection;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (controller == null || other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            controller.TravelThroughDoor(direction);
        }
    }
}
