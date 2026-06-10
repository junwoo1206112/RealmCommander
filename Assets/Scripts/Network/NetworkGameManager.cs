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
        [SerializeField] private string lobbySceneName = "LobbyScene";
        [SerializeField] private int minPlayers = 1;
        [SerializeField] private float autoStartDelay = 10f;

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
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (GetComponent<NetworkIdentity>() == null)
            {
                gameObject.AddComponent<NetworkIdentity>();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            gameState = GameState.WaitingForPlayers;
            playerCount = NetworkServer.connections.Count;
            _autoStartTimer = 0f;
        }

        private bool _playerPrefabSetupDone;

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isServer && NetworkServer.connections != null)
            {
                playerCount = NetworkServer.connections.Count;
            }
        }

        private void Update()
        {
            if (!_playerPrefabSetupDone && NetworkManager.singleton != null && NetworkServer.active)
            {
                _playerPrefabSetupDone = true;
                var nm = NetworkManager.singleton;
                if (nm.playerPrefab == null && NetworkBootstrap.CachedPlayerPrefab != null)
                {
                    nm.playerPrefab = NetworkBootstrap.CachedPlayerPrefab;
                    nm.autoCreatePlayer = true;
                }
            }

            if (!NetworkServer.active) return;

            if (gameState == GameState.WaitingForPlayers)
            {
                if (NetworkServer.connections == null) return;
                playerCount = NetworkServer.connections.Count;

                bool allReady = true;
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn.identity == null) continue;
                    
                    var player = conn.identity.GetComponent<NetworkPlayer>();
                    if (player != null && !player.isGameReady)
                    {
                        allReady = false;
                        break;
                    }
                }

                if (playerCount >= 1 && Input.GetKeyDown(KeyCode.Return))
                {
                    Debug.Log("[Game] Enter key pressed - starting game");
                    StartGame();
                    return;
                }

                if (allReady && playerCount >= 2)
                {
                    Debug.Log("[Game] All players ready - starting game");
                    StartGame();
                    return;
                }

                _autoStartTimer += Time.deltaTime;
                if (_autoStartTimer >= autoStartDelay && playerCount >= 1)
                {
                    StartGame();
                }
            }

            if (gameState == GameState.Playing)
            {
                _winCheckTimer -= Time.deltaTime;
                if (_winCheckTimer <= 0f)
                {
                    _winCheckTimer = 2f;
                    CheckWinCondition();
                }
            }
        }

        private float _winCheckTimer = 2f;
        private float _autoStartTimer;

        [Server]
        private void CheckWinCondition()
        {
            bool friendlyUnitAlive = false;
            bool enemyUnitAlive = false;

            var units = FindObjectsByType<RTS.Unit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit == null || !unit.IsAlive) continue;

                if (unit.IsEnemy)
                    enemyUnitAlive = true;
                else
                    friendlyUnitAlive = true;

                if (friendlyUnitAlive && enemyUnitAlive) break;
            }

            if (!friendlyUnitAlive || !enemyUnitAlive)
            {
                var buildings = FindObjectsByType<RTS.Building>(FindObjectsSortMode.None);
                foreach (var building in buildings)
                {
                    if (building == null || !building.IsAlive) continue;

                    bool isEnemyBase = building.BuildingType == RTS.BuildingType.Base;
                    if (isEnemyBase)
                    {
                        if (building.tag == "Enemy")
                            enemyUnitAlive = true;
                        else
                            friendlyUnitAlive = true;
                    }
                }
            }

            if (!friendlyUnitAlive && enemyUnitAlive)
            {
                EndGame(1);
            }
            else if (friendlyUnitAlive && !enemyUnitAlive)
            {
                EndGame(0);
            }
        }

        [Server]
        public void StartGame()
        {
            gameState = GameState.Playing;
            OnGameStarted?.Invoke();
            Debug.Log("[Game] Game Started - State: Playing");
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
                    if (conn.identity == null) continue;
                    
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
