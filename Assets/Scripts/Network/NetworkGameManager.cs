using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealmCommander.UI;
using RealmCommander.Core;
using RealmCommander.RTS;
using RealmCommander.AI;

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

            EnsureManagers();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private static void EnsureManagers()
        {
            if (SelectionManager.Instance == null)
            {
                var go = new GameObject("SelectionManager");
                go.AddComponent<SelectionManager>();
                Debug.Log("[Game] SelectionManager auto-created");
            }

            if (CommandManager.Instance == null)
            {
                var go = new GameObject("CommandManager");
                go.AddComponent<CommandManager>();
                Debug.Log("[Game] CommandManager auto-created");
            }

            if (GameObject.Find("CommandInput") == null)
            {
                var go = new GameObject("CommandInput");
                go.AddComponent<RTS.CommandInput>();
                Debug.Log("[Game] CommandInput auto-created");
            }

            if (GameObject.Find("BoxSelector") == null)
            {
                var go = new GameObject("BoxSelector");
                go.AddComponent<RTS.BoxSelector>();
                Debug.Log("[Game] BoxSelector auto-created");
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            gameState = GameState.WaitingForPlayers;
            playerCount = NetworkServer.connections.Count;
            _autoStartTimer = 0f;

            EnsureUnitSpawner();
            EnsureEnemyAI();
        }

        [Server]
        private void EnsureUnitSpawner()
        {
            if (FindAnyObjectByType<UnitSpawner>() != null)
                return;

            var spawnerGo = new GameObject("UnitSpawner (Runtime)");
            var spawner = spawnerGo.AddComponent<UnitSpawner>();

            var unitPrefab = Resources.Load<GameObject>("Unit");
            if (unitPrefab != null)
                spawner.Initialize(unitPrefab);
            else
                Debug.LogError("[Game] Resources/Unit.prefab is missing.");

            spawner.SpawnUnitsNow();
        }

        [Server]
        private void EnsureEnemyAI()
        {
            if (FindAnyObjectByType<AIController>() != null)
                return;

            new GameObject("Enemy AI (Runtime)").AddComponent<AIController>();
        }

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
            if (!NetworkServer.active) return;

            if (gameState == GameState.WaitingForPlayers)
            {
                if (NetworkServer.connections == null) return;
                playerCount = NetworkServer.connections.Count;

                bool allReady = true;
                bool allPlayersCreated = playerCount > 0;
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn.identity == null)
                    {
                        allPlayersCreated = false;
                        continue;
                    }
                    
                    var player = conn.identity.GetComponent<NetworkPlayer>();
                    if (player != null && !player.isGameReady)
                    {
                        allReady = false;
                        break;
                    }
                }

                if (playerCount >= minPlayers && allPlayersCreated)
                {
                    Debug.Log("[Game] Required players created - starting game");
                    StartGame();
                    return;
                }

                if (playerCount >= minPlayers && Input.GetKeyDown(KeyCode.Return))
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
                if (_autoStartTimer >= autoStartDelay && playerCount >= minPlayers)
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

                    if (building.BuildingType == RTS.BuildingType.Base)
                    {
                        if (building.TeamId == 1)
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
            else if (!friendlyUnitAlive && !enemyUnitAlive)
            {
                EndGame(-1);
            }
        }

        [Server]
        public void StartGame()
        {
            if (gameState == GameState.Playing || gameState == GameState.GameOver) return;
            gameState = GameState.Playing;
            OnGameStarted?.Invoke();
            if (GameManager.Instance != null)
                GameManager.Instance.StartGame();
            Debug.Log("[Game] Game Started - State: Playing");
        }

        [Server]
        public void EndGame(int winningTeam)
        {
            if (gameState == GameState.GameOver) return;
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
            NetworkManager manager = NetworkManager.singleton;
            if (manager == null)
            {
                SceneManager.LoadScene(lobbySceneName);
                return;
            }

            manager.offlineScene = $"Assets/Scenes/{lobbySceneName}.unity";
            if (NetworkServer.active) manager.StopHost();
            else if (NetworkClient.active) manager.StopClient();
            else SceneManager.LoadScene(lobbySceneName);
        }

        private void OnGameStateChanged(GameState oldValue, GameState newValue)
        {
            OnStateChanged?.Invoke(newValue);
        }

        private void OnGUI()
        {
            if (!NetworkClient.active) return;

            string stateText = gameState == GameState.Playing
                ? "전투 진행 중"
                : "플레이어 연결 대기 중";
            GUI.Box(new Rect(12f, 12f, 300f, 74f), string.Empty);
            GUI.Label(new Rect(24f, 20f, 270f, 24f), $"Realm Commander - {stateText}");
            GUI.Label(new Rect(24f, 43f, 270f, 22f), "유닛 선택: 좌클릭/드래그 | 이동·공격: 우클릭");
            GUI.Label(new Rect(24f, 63f, 270f, 20f), $"연결 플레이어: {playerCount}");
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
