using UnityEngine;
using System.Collections.Generic;

namespace RealmCommander.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip unitSelectClip;
        [SerializeField] private AudioClip unitAttackClip;
        [SerializeField] private AudioClip unitMoveClip;
        [SerializeField] private AudioClip buildingCompleteClip;
        [SerializeField] private AudioClip unitSpawnClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;
        [SerializeField] private AudioClip buildClip;
        [SerializeField] private AudioClip clickClip;

        [Header("Music")]
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField] private AudioClip battleMusicClip;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

        private const int MaxSfxSources = 8;
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private int nextSfxIndex;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            sfxSource.volume = sfxVolume;
            musicSource.volume = musicVolume;

            InitializeSfxPool();
        }

        private void InitializeSfxPool()
        {
            for (int i = 0; i < MaxSfxSources; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.volume = sfxVolume;
                sfxPool.Add(source);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void PlayUnitSelect()
        {
            PlaySFX(unitSelectClip);
        }

        public void PlayUnitAttack()
        {
            PlaySFX(unitAttackClip);
        }

        public void PlayUnitMove()
        {
            PlaySFX(unitMoveClip);
        }

        public void PlayBuildingComplete()
        {
            PlaySFX(buildingCompleteClip);
        }

        public void PlayUnitSpawn()
        {
            PlaySFX(unitSpawnClip);
        }

        public void PlayVictory()
        {
            PlaySFX(victoryClip);
        }

        public void PlayDefeat()
        {
            PlaySFX(defeatClip);
        }

        public void PlayBuild()
        {
            PlaySFX(buildClip);
        }

        public void PlayClick()
        {
            PlaySFX(clickClip);
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            var source = GetAvailableSfxSource();
            if (source == null) return;

            source.transform.position = position;
            source.clip = clip;
            source.volume = sfxVolume * volume;
            source.spatialBlend = 1f;
            source.Play();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            var source = GetAvailableSfxSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = sfxVolume;
            source.spatialBlend = 0f;
            source.Play();
        }

        private AudioSource GetAvailableSfxSource()
        {
            foreach (var source in sfxPool)
            {
                if (!source.isPlaying)
                    return source;
            }

            var fallback = sfxPool[nextSfxIndex];
            nextSfxIndex = (nextSfxIndex + 1) % sfxPool.Count;
            return fallback;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
            foreach (var source in sfxPool)
            {
                if (source != null && !source.isPlaying)
                    source.volume = sfxVolume;
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;
            if (musicSource.isPlaying && musicSource.clip == clip) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlayMenuMusic()
        {
            PlayMusic(menuMusicClip);
        }

        public void PlayBattleMusic()
        {
            PlayMusic(battleMusicClip);
        }
    }
}
