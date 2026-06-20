using System;
using UnityEngine;

namespace CryptKnight.Audio
{
    public static class GameAudioSettings
    {
        private const string MasterVolumePrefsKey = "CryptKnight.MasterVolume";
        private const string MusicVolumePrefsKey = "CryptKnight.MusicVolume";
        private const string GameSoundsVolumePrefsKey = "CryptKnight.GameSoundsVolume";

        private static bool isLoaded;
        private static float masterVolume = 1f;
        private static float musicVolume = 1f;
        private static float gameSoundsVolume = 1f;

        public static event Action VolumesChanged;

        public static float MasterVolume
        {
            get
            {
                EnsureLoaded();
                return masterVolume;
            }
        }

        public static float MusicVolume
        {
            get
            {
                EnsureLoaded();
                return musicVolume;
            }
        }

        public static float GameSoundsVolume
        {
            get
            {
                EnsureLoaded();
                return gameSoundsVolume;
            }
        }

        public static void Initialize()
        {
            EnsureLoaded();
            AudioListener.volume = masterVolume;
        }

        public static void SetMasterVolume(float volume)
        {
            EnsureLoaded();
            masterVolume = ClampVolume(volume);
            AudioListener.volume = masterVolume;
            SaveVolume(MasterVolumePrefsKey, masterVolume);
            VolumesChanged?.Invoke();
        }

        public static void SetMusicVolume(float volume)
        {
            EnsureLoaded();
            musicVolume = ClampVolume(volume);
            SaveVolume(MusicVolumePrefsKey, musicVolume);
            VolumesChanged?.Invoke();
        }

        public static void SetGameSoundsVolume(float volume)
        {
            EnsureLoaded();
            gameSoundsVolume = ClampVolume(volume);
            SaveVolume(GameSoundsVolumePrefsKey, gameSoundsVolume);
            VolumesChanged?.Invoke();
        }

        public static float ClampVolume(float volume)
        {
            return Mathf.Clamp01(volume);
        }

        private static void EnsureLoaded()
        {
            if (isLoaded)
            {
                return;
            }

            masterVolume = ClampVolume(PlayerPrefs.GetFloat(MasterVolumePrefsKey, 1f));
            musicVolume = ClampVolume(PlayerPrefs.GetFloat(MusicVolumePrefsKey, 1f));
            gameSoundsVolume = ClampVolume(PlayerPrefs.GetFloat(GameSoundsVolumePrefsKey, 1f));
            isLoaded = true;
        }

        private static void SaveVolume(string key, float volume)
        {
            PlayerPrefs.SetFloat(key, volume);
            PlayerPrefs.Save();
        }
    }
}
