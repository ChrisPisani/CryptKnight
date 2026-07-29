namespace CryptKnight.Application
{
    public static class GameplayInputGate
    {
        public static bool IsBlocked { get; private set; }

        public static void SetBlocked(bool blocked)
        {
            IsBlocked = blocked;
        }
    }
}
