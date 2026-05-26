using UnityEngine;

namespace CryptKnight.Player
{
    public static class PlayerMovement
    {
        // Pure movement math stays separate from Unity input so it can be tested directly
        public static Vector2 NormalizeInput(Vector2 input)
        {
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        public static Vector2 CalculateNextPosition(Vector2 currentPosition, Vector2 moveInput, float moveSpeed, float deltaTime)
        {
            return currentPosition + NormalizeInput(moveInput) * moveSpeed * deltaTime;
        }
    }
}
