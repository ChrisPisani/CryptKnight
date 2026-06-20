using UnityEngine;

namespace CryptKnight.Audio
{
    public static class GameSfxPlayer
    {
        private const string ItemPowerupPickupPath = "Audio/SFX/crypt-knight-sfx-item-powerup-pickup";
        private const string LifeLostPath = "Audio/SFX/crypt-knight-sfx-life-lost";
        private const string SwordAttackPath = "Audio/SFX/crypt-knight-sfx-subtle-sword-attack";
        private const float ItemPowerupPickupVolume = 0.55f;

        private static AudioSource audioSource;
        private static AudioClip itemPowerupPickupClip;
        private static AudioClip lifeLostClip;
        private static AudioClip swordAttackClip;

        public static void PlayItemPowerupPickup()
        {
            PlayOneShot(ref itemPowerupPickupClip, ItemPowerupPickupPath, ItemPowerupPickupVolume);
        }

        public static void PlayLifeLost()
        {
            PlayOneShot(ref lifeLostClip, LifeLostPath);
        }

        public static void PlaySwordAttack()
        {
            PlayOneShot(ref swordAttackClip, SwordAttackPath);
        }

        private static void PlayOneShot(ref AudioClip cachedClip, string resourcePath, float volumeScale = 1f)
        {
            AudioClip clip = LoadClip(ref cachedClip, resourcePath);
            AudioSource source = GetAudioSource();
            if (clip == null || source == null)
            {
                return;
            }

            source.PlayOneShot(clip, volumeScale * GameAudioSettings.GameSoundsVolume);
        }

        private static AudioClip LoadClip(ref AudioClip cachedClip, string resourcePath)
        {
            if (cachedClip == null)
            {
                cachedClip = Resources.Load<AudioClip>(resourcePath);
                if (cachedClip == null)
                {
                    Debug.LogWarning($"SFX clip could not be loaded from Resources/{resourcePath}.");
                }
            }

            return cachedClip;
        }

        private static AudioSource GetAudioSource()
        {
            if (audioSource != null)
            {
                return audioSource;
            }

            GameObject audioObject = new GameObject("Crypt Knight SFX");
            Object.DontDestroyOnLoad(audioObject);
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            return audioSource;
        }
    }
}
