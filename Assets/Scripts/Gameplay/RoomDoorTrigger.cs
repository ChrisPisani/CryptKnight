using System;
using CryptKnight.Dungeon;
using CryptKnight.Player;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    public sealed class RoomDoorTrigger : MonoBehaviour
    {
        private const float MinimumEntryInputDot = 0.65f;

        private Action<RoomDirection> travel;
        private RoomDirection direction;

        public void Initialize(Action<RoomDirection> travelAction, RoomDirection doorDirection)
        {
            travel = travelAction;
            direction = doorDirection;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTravel(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryTravel(other);
        }

        private void TryTravel(Collider2D other)
        {
            if (travel == null || other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            // Require input toward the doorway so moving sideways through trigger doesn't cause room swap.
            if (!IsInputEnteringDoor(direction, playerController.MoveInput))
            {
                return;
            }

            travel(direction);
        }

        public static bool IsInputEnteringDoor(RoomDirection doorDirection, Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            return Vector2.Dot(moveInput.normalized, GetDoorEntryDirection(doorDirection)) >= MinimumEntryInputDot;
        }

        private static Vector2 GetDoorEntryDirection(RoomDirection doorDirection)
        {
            switch (doorDirection)
            {
                case RoomDirection.North:
                    return Vector2.up;
                case RoomDirection.East:
                    return Vector2.right;
                case RoomDirection.South:
                    return Vector2.down;
                case RoomDirection.West:
                    return Vector2.left;
                default:
                    return Vector2.zero;
            }
        }
    }
}
