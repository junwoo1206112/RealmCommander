using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealmCommander.UI;
using RealmCommander.Core;

namespace RealmCommander.Network
{
    public class NetworkGameManager : NetworkBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private string gameSceneName = "MainScene";
        [SerializeField] private string lobbySceneName = "LobbyScene";

        [SyncVar(hook = nameof(OnGameStateChanged))]
        private GameState gameState = GameState.Idle;

        [SyncVar]
        private int playerCount = 0;

        public static NetworkGameManager Instance { get; private set; }
        public GameState State => gameState;
        public int PlayerCount => playerCount;

        public event Action<GameState> OnStateChanged;
        public event Action OnGameStarted;
        public event Action<int> OnPlayerWon;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            gameState = GameState.WaitingForPlayers;
            playerCount = NetworkServer.connections.Count;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isServer)
            {
                playerCount = NetworkServer.connections.Count;
            }
        }

        private void Update()
        {
            if (!isServer) return;

            if (gameState == GameState.WaitingForPlayers)
            {
                playerCount = NetworkServer.connections.Count;

                if (playerCount >= 2)
                {
                    bool allReady = true;
                    foreach (var conn in NetworkServer.connections.Values)
                    {
                        var player = conn.identity.GetComponent<NetworkPlayer>();
                        if (player != null && !player.isGameReady)
                        {
                            allReady = false;
                            break;
                        }
                    }

                    if (allReady && playerCount >= 2)
                    {
                        StartGame();
                    }
                }
            }

            if (gameState == GameState.Playing)
            {
                CheckWinCondition();
            }
        }

        [Server]
        public void StartGame()
        {
            gameState = GameState.Playing;
            OnGameStarted?.Invoke();

            NetworkManager.singleton.ServerChangeScene(gameSceneName);
        }

        [Server]
        private void CheckWinCondition()
        {
            bool friendlyAlive = false;
            bool enemyAlive = false;

            var units = FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit == null || !unit.IsAlive) continue;

                if (unit.IsEnemy)
                {
                    enemyAlive = true;
                }
                else
                {
                    friendlyAlive = true;
                }
            }

            if (!friendlyAlive && enemyAlive)
            {
                EndGame(1);
            }
            else if (friendlyAlive && !enemyAlive)
            {
                EndGame(0);
            }
        }

        [Server]
        public void EndGame(int winningTeam)
        {
            gameState = GameState.GameOver;
            OnPlayerWon?.Invoke(winningTeam);

            RpcShowResult(winningTeam);
        }

        [ClientRpc]
        private void RpcShowResult(int winningTeam)
        {
            var localPlayer = NetworkPlayer.Local;
            bool isVictory = localPlayer != null && localPlayer.teamId == winningTeam;

            GameResultUI.Show(isVictory);
        }

        [Server]
        public void OnPlayerDisconnected()
        {
            if (gameState == GameState.Playing)
            {
                int remainingTeam = -1;
                foreach (var conn in NetworkServer.connections.Values)
                {
                    var player = conn.identity.GetComponent<NetworkPlayer>();
                    if (player != null)
                    {
                        remainingTeam = player.teamId;
                        break;
                    }
                }

                EndGame(remainingTeam);
            }
        }

        public void ReturnToLobby()
        {
            if (isServer)
            {
                NetworkManager.singleton.ServerChangeScene(lobbySceneName);
            }
            else
            {
                NetworkManager.singleton.StopClient();
            }
        }

        private void OnGameStateChanged(GameState oldValue, GameState newValue)
        {
            OnStateChanged?.Invoke(newValue);
        }

        public enum GameState
        {
            Idle,
            WaitingForPlayers,
            Playing,
            GameOver
        }

    }
}
