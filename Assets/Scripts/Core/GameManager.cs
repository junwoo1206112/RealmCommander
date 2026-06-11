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
                Instance = null;
            Time.timeScale = 1f;
        }

        public void StartGame()
        {
            IsPaused = false;
            Time.timeScale = gameSpeed;
            OnGameStarted?.Invoke();
        }

        public void PauseGame()
        {
            if (NetworkClient.active && !NetworkServer.active) return;
            IsPaused = true;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            if (NetworkClient.active && !NetworkServer.active) return;
            IsPaused = false;
            Time.timeScale = gameSpeed;
            OnGameResumed?.Invoke();
        }

        public void SetGameSpeed(float speed)
        {
            if (NetworkClient.active) return;
            gameSpeed = Mathf.Clamp(speed, 0.5f, 3f);
            if (!IsPaused)
            {
                Time.timeScale = gameSpeed;
            }
        }
    }
}
