using UnityEngine;

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
        [SerializeField] private AudioClip skillCastClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

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

        public void PlaySkillCast()
        {
            PlaySFX(skillCastClip);
        }

        public void PlayVictory()
        {
            PlaySFX(victoryClip);
        }

        public void PlayDefeat()
        {
            PlaySFX(defeatClip);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }
    }
}
