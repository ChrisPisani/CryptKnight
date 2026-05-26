using System;
using CryptKnight.Data;
using UnityEngine;

namespace CryptKnight.Application
{
    public sealed class GameManager : MonoBehaviour
    {
        private const int DungeonWidth = 4;
        private const int DungeonHeight = 4;
        private static GameManager instance;
        private int runCounter;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject managerObject = new GameObject("Game Manager");
                    instance = managerObject.AddComponent<GameManager>();
                }

                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        public GameRunState CurrentRun { get; private set; }

        public event Action<GameRunState> RunStateChanged;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public GameRunState StartNewRun()
        {
            runCounter++;

            int seed = UnityEngine.Random.Range(100000, 999999);
            CurrentRun = GameRunState.CreateNewRun(
                runCounter,
                seed,
                DungeonWidth,
                DungeonHeight,
                PlayerBaseStats.CreateDefault());

            Debug.Log($"Started Crypt Knight run {CurrentRun.RunNumber} with seed {CurrentRun.Seed}.");
            RunStateChanged?.Invoke(CurrentRun);
            return CurrentRun;
        }

        public void QuitCurrentRun()
        {
            if (CurrentRun == null || !CurrentRun.IsActive)
            {
                return;
            }

            CurrentRun.QuitRun();
            Debug.Log($"Quit Crypt Knight run {CurrentRun.RunNumber}.");
            RunStateChanged?.Invoke(CurrentRun);
        }

        public void DamagePlayer(int halfHeartDamage)
        {
            if (CurrentRun == null)
            {
                return;
            }

            CurrentRun.ApplyDamage(halfHeartDamage);
            RunStateChanged?.Invoke(CurrentRun);
        }

        public void HealPlayer(int halfHeartAmount)
        {
            if (CurrentRun == null)
            {
                return;
            }

            CurrentRun.Heal(halfHeartAmount);
            RunStateChanged?.Invoke(CurrentRun);
        }

        public void AddKeys(int amount)
        {
            if (CurrentRun == null)
            {
                return;
            }

            CurrentRun.AddKeys(amount);
            RunStateChanged?.Invoke(CurrentRun);
        }

        public bool SpendKey()
        {
            if (CurrentRun == null)
            {
                return false;
            }

            bool spentKey = CurrentRun.SpendKey();
            RunStateChanged?.Invoke(CurrentRun);
            return spentKey;
        }

        public void AddCollectedItem(string itemId, string displayName, int quantity = 1)
        {
            if (CurrentRun == null)
            {
                return;
            }

            CurrentRun.AddCollectedItem(itemId, displayName, quantity);
            RunStateChanged?.Invoke(CurrentRun);
        }

        public void AddStatModifier(PlayerStatModifier modifier)
        {
            if (CurrentRun == null)
            {
                return;
            }

            CurrentRun.AddStatModifier(modifier);
            RunStateChanged?.Invoke(CurrentRun);
        }
    }
}
