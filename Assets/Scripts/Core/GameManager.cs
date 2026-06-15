using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace RealmCommander.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private float gameSpeed = 1f;

        public event Action OnGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<float> OnGameSpeedChanged;

        public bool IsPaused { get; private set; }
        public float GameSpeed => gameSpeed;

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

            Application.targetFrameRate = 60;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                if (Network.NetworkGameManager.Instance == null)
                    TimeScaleManager.Reset();
            }
        }

        public void StartGame()
        {
            IsPaused = false;
            if (Network.NetworkGameManager.Instance == null)
                TimeScaleManager.SetTimeScale(gameSpeed);

            Audio.AudioManager.Instance?.PlayBattleMusic();

            OnGameStarted?.Invoke();
        }

        public void PauseGame()
        {
            if (NetworkClient.active && !NetworkServer.active) return;
            IsPaused = true;
            if (Network.NetworkGameManager.Instance != null)
                Network.NetworkGameManager.Instance.ServerSetPaused(true);
            else
                TimeScaleManager.SetPaused(true);
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (NetworkClient.active && !NetworkServer.active) return;
            IsPaused = false;
            if (Network.NetworkGameManager.Instance != null)
                Network.NetworkGameManager.Instance.ServerSetPaused(false);
            else
            {
                TimeScaleManager.SetPaused(false);
                TimeScaleManager.SetTimeScale(gameSpeed);
            }
            OnGameResumed?.Invoke();
        }

        public void SetGameSpeed(float speed)
        {
            if (NetworkClient.active && !NetworkServer.active) return;
            gameSpeed = Mathf.Clamp(speed, 0.5f, 3f);
            if (!IsPaused)
            {
                if (Network.NetworkGameManager.Instance != null && NetworkServer.active)
                    Network.NetworkGameManager.Instance.ServerSetGameSpeed(gameSpeed);
                else
                    TimeScaleManager.SetTimeScale(gameSpeed);
            }
            OnGameSpeedChanged?.Invoke(gameSpeed);
        }
    }
}
