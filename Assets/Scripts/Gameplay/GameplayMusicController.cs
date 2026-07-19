using System.Collections;
using CryptKnight.Audio;
using CryptKnight.Dungeon;
using UnityEngine;

namespace CryptKnight.Gameplay
{
    public sealed class GameplayMusicController : MonoBehaviour
    {
        private const float FadeDuration = 5f;
        private const string ExplorationMusicPath = "Audio/Game/crypt-knight-dungeon-exploration-loop";
        private const string BossMusicPath = "Audio/Game/crypt-knight-boss-room-heavy-fight-loop";

        private AudioClip explorationClip;
        private AudioClip bossClip;
        private AudioSource source;
        private AudioClip currentClip;
        private AudioClip targetClip;
        private Coroutine transition;

        public void Configure()
        {
            if (source != null)
            {
                return;
            }

            explorationClip = Resources.Load<AudioClip>(ExplorationMusicPath);
            bossClip = Resources.Load<AudioClip>(BossMusicPath);
            if (explorationClip == null && bossClip == null)
            {
                Debug.LogWarning("Gameplay music clips could not be loaded from Resources/Audio/Game.");
                return;
            }

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        public void TransitionTo(RoomType roomType)
        {
            AudioClip nextClip = GetClip(roomType);
            if (nextClip == null || source == null || targetClip == nextClip)
            {
                return;
            }

            StopTransition();
            if (currentClip == nextClip && source.isPlaying)
            {
                if (source.volume < TargetVolume)
                {
                    transition = StartCoroutine(FadeToFull(nextClip));
                }

                return;
            }

            transition = currentClip == null || !source.isPlaying
                ? StartCoroutine(FadeIn(nextClip))
                : StartCoroutine(FadeOutThenPlay(nextClip));
        }

        public void RefreshVolume()
        {
            if (source != null && source.isPlaying && transition == null)
            {
                source.volume = TargetVolume;
            }
        }

        public void StopMusic()
        {
            StopTransition();
            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }

            currentClip = null;
            targetClip = null;
        }

        private AudioClip GetClip(RoomType roomType)
        {
            return roomType == RoomType.Final ? bossClip : explorationClip;
        }

        private IEnumerator FadeIn(AudioClip nextClip)
        {
            targetClip = nextClip;
            source.clip = nextClip;
            source.volume = 0f;
            source.Play();
            currentClip = nextClip;
            yield return FadeVolume(0f, TargetVolume);
            CompleteTransition();
        }

        private IEnumerator FadeToFull(AudioClip nextClip)
        {
            targetClip = nextClip;
            yield return FadeVolume(source.volume, TargetVolume);
            currentClip = nextClip;
            CompleteTransition();
        }

        private IEnumerator FadeOutThenPlay(AudioClip nextClip)
        {
            targetClip = nextClip;
            yield return FadeVolume(source.volume, 0f);
            source.Stop();
            source.clip = nextClip;
            source.volume = TargetVolume;
            source.Play();
            currentClip = nextClip;
            CompleteTransition();
        }

        private IEnumerator FadeVolume(float start, float end)
        {
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / FadeDuration));
                yield return null;
            }

            source.volume = end;
        }

        private void StopTransition()
        {
            if (transition == null)
            {
                return;
            }

            StopCoroutine(transition);
            transition = null;
            targetClip = null;
        }

        private void CompleteTransition()
        {
            targetClip = null;
            transition = null;
        }

        private static float TargetVolume => GameAudioSettings.MusicVolume;
    }
}
