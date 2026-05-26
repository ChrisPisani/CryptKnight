using CryptKnight.Application;
using UnityEngine;

namespace CryptKnight.UI
{
    public static class MainMenuBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateMainMenu()
        {
            // The current prototype builds UI at runtime so the default Unity scene can stay minimal
            _ = GameManager.Instance;

            if (Object.FindFirstObjectByType<MainMenuController>() != null)
            {
                return;
            }

            GameObject menuObject = new GameObject("Main Menu");
            menuObject.AddComponent<MainMenuController>();
        }
    }
}
