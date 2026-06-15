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

        [Header("AI")]
        [SerializeField] private GameObject[] enemyUnitPrefabs;

        [Header("Debug")]
        [SerializeField] private bool showDebugOverlay;

        [SyncVar(hook = nameof(OnGameStateChanged))]
        private GameState gameState = GameState.Idle;

        [SyncVar]
        private int playerCount = 0;

        [SyncVar(hook = nameof(OnPauseChanged))]
        private bool isGamePaused = false;

        [SyncVar(hook = nameof(OnSpeedChanged))]
        private float syncedGameSpeed = 1f;

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
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
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
            if (Core.EntityRegistry.Instance == null)
            {
                var go = new GameObject("EntityRegistry");
                go.AddComponent<Core.EntityRegistry>();
                Debug.Log("[Game] EntityRegistry auto-created");
            }

            if (ResourceManager.Instance == null)
            {
                var go = new GameObject("ResourceManager");
                go.AddComponent<NetworkIdentity>();
                go.AddComponent<ResourceManager>();
                Debug.Log("[Game] ResourceManager auto-created");
            }

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

            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
                Debug.Log("[Game] GameManager auto-created");
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

            EnsureCameraController();
            EnsureBattlefieldPolisher();
            EnsureRTSRuntimeDirector();
            EnsureRTSGameplayLoop();
        }

        private static void EnsureCameraController()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null) mainCam = camObj.GetComponent<Camera>();
            }
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                mainCam.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
                Debug.Log("[Game] Main Camera created");
            }

            if (mainCam.GetComponent<RTS.MobileRTSCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<RTS.MobileRTSCameraController>();
                Debug.Log("[Game] MobileRTSCameraController added to Main Camera");
            }
        }

        private static void EnsureBattlefieldPolisher()
        {
            if (FindAnyObjectByType<RTS.BattlefieldPolisher>() != null)
                return;

            new GameObject("BattlefieldPolisher").AddComponent<RTS.BattlefieldPolisher>();
        }

        private static void EnsureRTSRuntimeDirector()
        {
            if (FindAnyObjectByType<Core.RTSRuntimeDirector>() != null)
                return;

            new GameObject("RTSRuntimeDirector").AddComponent<Core.RTSRuntimeDirector>();
        }

        private static void EnsureRTSGameplayLoop()
        {
            if (FindAnyObjectByType<Core.RTSGameplayLoop>() != null)
                return;

            new GameObject("RTSGameplayLoop").AddComponent<Core.RTSGameplayLoop>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            gameState = GameState.WaitingForPlayers;
            playerCount = CountCreatedPlayers();
            _autoStartTimer = 0f;

            EnsureCombatManager();
            EnsureUnitSpawner();
            EnsureEnemyAI();
        }

        [Server]
        private void EnsureCombatManager()
        {
            if (CombatManager.Instance == null)
            {
                var go = new GameObject("CombatManager");
                go.AddComponent<NetworkIdentity>();
                var manager = go.AddComponent<CombatManager>();
                NetworkServer.Spawn(go);
                Debug.Log("[Game] CombatManager created and spawned on server");
            }
        }

        [Server]
        private void EnsureUnitSpawner()
        {
            if (FindAnyObjectByType<UnitSpawner>() != null)
                return;

            var spawnerGo = new GameObject("UnitSpawner (Runtime)");
            var spawner = spawnerGo.AddComponent<UnitSpawner>();

            var unitPrefab = Resources.Load<GameObject>("Unit");
            spawner.Initialize(unitPrefab);

            if (unitPrefab == null)
                Debug.LogError("[Game] Resources/Unit.prefab is missing.");

            spawner.SpawnUnitsNow();
        }

        [Server]
        private void EnsureEnemyAI()
        {
            if (FindAnyObjectByType<AIController>() != null)
                return;

            var ai = new GameObject("Enemy AI (Runtime)").AddComponent<AIController>();
            ai.Initialize(GetEnemyUnitPrefabs());
        }

        private GameObject[] GetEnemyUnitPrefabs()
        {
            if (enemyUnitPrefabs != null && enemyUnitPrefabs.Length > 0)
                return enemyUnitPrefabs;

            GameObject unitPrefab = Resources.Load<GameObject>("Unit");
            return unitPrefab != null ? new[] { unitPrefab } : new GameObject[0];
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isServer && NetworkServer.connections != null)
            {
                playerCount = CountCreatedPlayers();
            }
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (gameState == GameState.WaitingForPlayers)
            {
                if (NetworkServer.connections == null) return;
                playerCount = CountCreatedPlayers();

                bool allReady = true;
                bool allPlayersCreated = playerCount > 0;
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn.identity == null)
                    {
                        allPlayersCreated = false;
                        allReady = false;
                        continue;
                    }
                    
                    var player = conn.identity.GetComponent<NetworkPlayer>();
                    if (player != null && !player.IsGameReady)
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

                if (allReady && playerCount >= minPlayers)
                {
                    Debug.Log("[Game] All players ready - starting game");
                    StartGame();
                    return;
                }

                float delay = playerCount >= 2 ? autoStartDelay : 2f;
                _autoStartTimer += Time.deltaTime;
                if (_autoStartTimer >= delay && playerCount >= minPlayers && allPlayersCreated)
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

        [Server]
        private static int CountCreatedPlayers()
        {
            if (NetworkServer.connections == null) return 0;
            int count = 0;
            foreach (var connection in NetworkServer.connections.Values)
                if (connection.identity != null) count++;
            return count;
        }

        private float _winCheckTimer = 2f;
        private float _autoStartTimer;

        [Server]
        private void CheckWinCondition()
        {
            bool friendlyUnitAlive = false;
            bool enemyUnitAlive = false;

            var registry = Core.EntityRegistry.Instance;
            if (registry != null)
            {
                foreach (var unit in registry.AllUnits)
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
                    foreach (var building in registry.AllBuildings)
                    {
                        if (building == null || !building.IsAlive) continue;

                        if (building.TeamId == 1)
                            enemyUnitAlive = true;
                        else
                            friendlyUnitAlive = true;

                        if (friendlyUnitAlive && enemyUnitAlive) break;
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
            syncedGameSpeed = 1f;
            isGamePaused = false;
            Core.TimeScaleManager.Reset();
            OnGameStarted?.Invoke();
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
            bool isVictory = localPlayer != null && localPlayer.TeamId == winningTeam;

            GameResultUI.Show(isVictory);

            if (isVictory)
                Audio.AudioManager.Instance?.PlayVictory();
            else
                Audio.AudioManager.Instance?.PlayDefeat();
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
                        remainingTeam = player.TeamId;
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

        public void RestartGame()
        {
            if (!isServer) return;

            gameState = GameState.WaitingForPlayers;
            Core.TimeScaleManager.Reset();
            StartGame();
        }

        private void OnGameStateChanged(GameState oldValue, GameState newValue)
        {
            OnStateChanged?.Invoke(newValue);
        }

        private void OnPauseChanged(bool oldValue, bool newValue)
        {
            if (!isServer)
                Core.TimeScaleManager.SetPaused(newValue);
        }

        private void OnSpeedChanged(float oldValue, float newValue)
        {
            if (!isServer && !isGamePaused)
                Core.TimeScaleManager.SetTimeScale(newValue);
        }

        [Server]
        public void ServerSetPaused(bool paused)
        {
            isGamePaused = paused;
            Core.TimeScaleManager.SetPaused(paused);
            if (!paused)
                Core.TimeScaleManager.SetTimeScale(syncedGameSpeed);
        }

        [Server]
        public void ServerSetGameSpeed(float speed)
        {
            syncedGameSpeed = Mathf.Clamp(speed, 0.5f, 3f);
            if (!isGamePaused)
                Core.TimeScaleManager.SetTimeScale(syncedGameSpeed);
        }

        private void OnGUI()
        {
            if (!showDebugOverlay) return;
            if (!NetworkClient.active) return;

            string stateText = gameState == GameState.Playing
                ? "Battle in progress"
                : "Waiting for players";
            GUI.Box(new Rect(12f, 12f, 300f, 74f), string.Empty);
            GUI.Label(new Rect(24f, 20f, 270f, 24f), $"Realm Commander - {stateText}");
            GUI.Label(new Rect(24f, 43f, 270f, 22f), "Select: click/drag | Move/attack: right click");
            GUI.Label(new Rect(24f, 63f, 270f, 20f), $"Connected players: {playerCount}");
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
