using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using RealmCommander.AI;
using RealmCommander.RTS;
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmCommander.Network
{
    public struct MultiplayerSmokeClientPassMessage : NetworkMessage
    {
        public uint netId;
        public Vector3 targetPosition;
        public Vector3 finalPosition;
    }

    public sealed class MultiplayerSmokeLauncher : MonoBehaviour
    {
        private const float DefaultTimeout = 45f;

        private bool isHost;
        private string address = "127.0.0.1";
        private float timeout = DefaultTimeout;
        private float startedAt;
        private float clientReadyAt = -1f;
        private Unit clientUnit;
        private Vector3 clientStartPosition;
        private Vector3 clientTargetPosition;
        private float clientAtTargetSince = -1f;
        private uint clientReportedPassNetId;
        private Vector3 clientReportedTarget;
        private Vector3 clientReportedFinal;
        private Unit hostUnit;
        private Vector3 hostTargetPosition;
        private float hostAtTargetSince = -1f;
        private bool hostMoveRequested;
        private bool hostMoveReached;
        private bool resourceIsolationValidated;
        private bool hostReadyLogged;
        private bool clientPassSent;
        private bool productionTestDone;
        private float productionTestStarted;
        private readonly Dictionary<uint, Vector3> hostEnemyStarts = new Dictionary<uint, Vector3>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool requested = HasArg(args, "--rc-smoke-host") || HasArg(args, "--rc-smoke-client");
            if (!requested) return;

            GameObject go = new GameObject("Multiplayer Smoke Launcher");
            DontDestroyOnLoad(go);
            go.AddComponent<MultiplayerSmokeLauncher>();
        }

        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            isHost = HasArg(args, "--rc-smoke-host");
            address = GetArgValue(args, "--rc-address", address);
            timeout = ParseFloat(GetArgValue(args, "--rc-timeout", DefaultTimeout.ToString()), DefaultTimeout);

            yield return null;

            NetworkManager manager = NetworkBootstrap.EnsureNetworkManager();
            if (manager == null)
            {
                Fail("NetworkManager creation failed.");
                yield break;
            }

            if (manager.transport is PortTransport portTransport)
                portTransport.Port = 7777;

            startedAt = Time.realtimeSinceStartup;
            if (isHost)
            {
                NetworkServer.RegisterHandler<MultiplayerSmokeClientPassMessage>(OnClientPassMessage);
                manager.networkAddress = "0.0.0.0";
                Debug.Log("[MultiplayerSmoke] HOST_START port=7777");
                manager.StartHost();
            }
            else
            {
                manager.networkAddress = address;
                Debug.Log($"[MultiplayerSmoke] CLIENT_START address={address}:7777");
                manager.StartClient();
            }
        }

        private void Update()
        {
            if (startedAt <= 0f) return;
            DisableEnemyAI();

            if (Time.realtimeSinceStartup - startedAt > timeout)
            {
                Fail($"Timeout. role={(isHost ? "host" : "client")}, scene={SceneManager.GetActiveScene().name}");
                return;
            }

            if (isHost)
                ValidateHost();
            else
                ValidateClient();
        }

        private void ValidateHost()
        {
            if (!NetworkServer.active || NetworkServer.connections.Count < 2) return;

            bool hasTeamZero = false;
            bool hasTeamOne = false;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                NetworkPlayer player = connection.identity != null
                    ? connection.identity.GetComponent<NetworkPlayer>()
                    : null;
                if (player == null) return;
                hasTeamZero |= player.TeamId == 0;
                hasTeamOne |= player.TeamId == 1;
            }

            if (!hasTeamZero || !hasTeamOne) return;
            if (!ValidateResourceIsolation()) return;

            if (!productionTestDone)
            {
                HostValidateProduction();
                return;
            }

            bool teamZeroOwned = false;
            bool teamOneOwned = false;
            Unit trackedFriendly = null;
            Unit trackedEnemy = null;
            foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            {
                NetworkIdentity unitIdentity = unit.netIdentity;
                if (unitIdentity == null || unitIdentity.connectionToClient == null) continue;
                NetworkPlayer owner = unitIdentity.connectionToClient.identity != null
                    ? unitIdentity.connectionToClient.identity.GetComponent<NetworkPlayer>()
                    : null;
                if (owner == null) continue;

                if (owner.TeamId == 0 && !unit.IsEnemy)
                {
                    teamZeroOwned = true;
                    if (trackedFriendly == null || unit.netId < trackedFriendly.netId)
                        trackedFriendly = unit;
                }
                if (owner.TeamId == 1 && unit.IsEnemy)
                {
                    teamOneOwned = true;
                    if (trackedEnemy == null || unit.netId < trackedEnemy.netId)
                        trackedEnemy = unit;
                }
            }

            foreach (Building building in FindObjectsByType<Building>(FindObjectsSortMode.None))
            {
                NetworkIdentity buildingIdentity = building.netIdentity;
                if (buildingIdentity == null) continue;
                NetworkConnectionToClient ownerConnection = buildingIdentity.connectionToClient;
                if (ownerConnection == null) continue;
                NetworkPlayer owner = ownerConnection.identity != null
                    ? ownerConnection.identity.GetComponent<NetworkPlayer>()
                    : null;
                if (owner == null || owner.TeamId != building.TeamId)
                {
                    Fail($"Building authority mismatch. building={building.name}, team={building.TeamId}");
                    return;
                }
            }

            if (!teamZeroOwned || !teamOneOwned || trackedFriendly == null || trackedEnemy == null) return;

            ValidateHostOwnedMovement(trackedFriendly);

            if (!hostEnemyStarts.TryGetValue(trackedEnemy.netId, out Vector3 startPosition))
                hostEnemyStarts[trackedEnemy.netId] = trackedEnemy.transform.position;
            else if (clientReportedPassNetId == trackedEnemy.netId && hostMoveReached)
            {
                float movedDistance = HorizontalDistance(startPosition, trackedEnemy.transform.position);
                float hostTargetError = HorizontalDistance(trackedEnemy.transform.position, clientReportedTarget);
                float replicationError = HorizontalDistance(trackedEnemy.transform.position, clientReportedFinal);
                if (movedDistance < 2f || hostTargetError > 1.25f || replicationError > 0.75f)
                {
                    Fail($"Movement quality mismatch. moved={movedDistance:F2}, hostTargetError={hostTargetError:F2}, replicationError={replicationError:F2}");
                    return;
                }

                Pass($"HOST_PASS players=2 teams=0,1 ownership=ok remoteMoveNetId={trackedEnemy.netId} moved={movedDistance:F2} targetError={hostTargetError:F2} replicationError={replicationError:F2}");
                return;
            }

            if (!hostReadyLogged)
            {
                hostReadyLogged = true;
                Debug.Log("[MultiplayerSmoke] HOST_READY ownership=ok waiting_for_remote_move");
            }
        }

        private bool ValidateResourceIsolation()
        {
            if (resourceIsolationValidated) return true;
            ResourceManager resources = ResourceManager.Instance;
            if (resources == null) return false;

            float team0Gold = resources.GetGold(0);
            float team1Gold = resources.GetGold(1);
            float team1Mana = resources.GetMana(1);
            const float goldCost = 7f;
            const float manaCost = 3f;
            if (!resources.TrySpend(1, goldCost, manaCost))
            {
                Fail("Team 1 resource spend failed during isolation test.");
                return false;
            }

            bool isolated = Mathf.Approximately(resources.GetGold(0), team0Gold) &&
                Mathf.Approximately(resources.GetGold(1), team1Gold - goldCost) &&
                Mathf.Approximately(resources.GetMana(1), team1Mana - manaCost);
            resources.AddGold(1, goldCost);
            resources.AddMana(1, manaCost);

            if (!isolated)
            {
                Fail("Team resources are not isolated.");
                return false;
            }

            resourceIsolationValidated = true;
            Debug.Log("[MultiplayerSmoke] RESOURCE_ISOLATION_PASS teams=0,1");
            return true;
        }

        private void HostValidateProduction()
        {
            if (productionTestStarted == 0f)
            {
                productionTestStarted = Time.realtimeSinceStartup;
                Core.RTSGameplayLoop.ExecuteBuildCommand(RTS.BuildingType.Barracks, 0);
                Debug.Log("[MultiplayerSmoke] PRODUCTION_TEST build Barracks");
                return;
            }

            // Check if ANY building exists for team 0 with a production queue
            bool hasBarracks = false;
            bool hasQueue = false;
            foreach (Building b in FindObjectsByType<Building>(FindObjectsSortMode.None))
            {
                if (b == null || !b.IsAlive || b.TeamId != 0) continue;
                if (b.BuildingType == RTS.BuildingType.Barracks) hasBarracks = true;
                var q = b.GetProductionQueue();
                if (q != null && q.Count > 0) hasQueue = true;
            }

            if (hasBarracks && hasQueue)
            {
                productionTestDone = true;
                Debug.Log("[MultiplayerSmoke] PRODUCTION_PASS barracks_built_and_queue_ready");
            }
        }

        private void ValidateHostOwnedMovement(Unit trackedFriendly)
        {
            if (hostUnit == null)
                hostUnit = trackedFriendly;

            if (hostUnit != trackedFriendly || hostMoveReached) return;

            if (!hostMoveRequested)
            {
                hostMoveRequested = true;
                hostTargetPosition = hostUnit.transform.position + new Vector3(4f, 0f, -2f);
                hostUnit.RequestMove(hostTargetPosition);
                Debug.Log($"[MultiplayerSmoke] HOST_MOVE_REQUESTED netId={hostUnit.netId} target={hostTargetPosition}");
                return;
            }

            float targetError = HorizontalDistance(hostUnit.transform.position, hostTargetPosition);
            if (targetError <= 1f)
            {
                if (hostAtTargetSince < 0f)
                    hostAtTargetSince = Time.realtimeSinceStartup;
                else if (Time.realtimeSinceStartup - hostAtTargetSince >= 0.75f)
                {
                    hostMoveReached = true;
                    Debug.Log($"[MultiplayerSmoke] HOST_MOVE_PASS netId={hostUnit.netId} targetError={targetError:F2}");
                }
            }
            else
            {
                hostAtTargetSince = -1f;
            }
        }

        private void ValidateClient()
        {
            if (!NetworkClient.active || NetworkClient.localPlayer == null) return;
            NetworkPlayer localPlayer = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
            if (localPlayer == null || localPlayer.TeamId != 1) return;

            if (clientUnit == null)
            {
                Unit candidate = null;
                foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
                {
                    if (!unit.isOwned || !unit.IsEnemy) continue;
                    if (candidate == null || unit.netId < candidate.netId)
                        candidate = unit;
                }

                if (candidate == null) return;
                clientUnit = candidate;
                clientStartPosition = candidate.transform.position;
                clientReadyAt = Time.realtimeSinceStartup;
                Debug.Log($"[MultiplayerSmoke] CLIENT_READY netId={candidate.netId} start={clientStartPosition}");
                return;
            }

            if (Time.realtimeSinceStartup - clientReadyAt > 1f &&
                Vector3.Distance(clientStartPosition, clientUnit.transform.position) < 0.25f)
            {
                clientTargetPosition = clientStartPosition + new Vector3(-4f, 0f, 2f);
                clientUnit.RequestMove(clientTargetPosition);
                clientReadyAt = float.PositiveInfinity;
                Debug.Log($"[MultiplayerSmoke] CLIENT_MOVE_REQUESTED target={clientTargetPosition}");
                return;
            }

            if (clientPassSent) return;

            float targetError = HorizontalDistance(clientUnit.transform.position, clientTargetPosition);
            if (targetError <= 1f)
            {
                if (clientAtTargetSince < 0f)
                    clientAtTargetSince = Time.realtimeSinceStartup;
            }
            else
            {
                clientAtTargetSince = -1f;
            }

            if (clientAtTargetSince >= 0f && Time.realtimeSinceStartup - clientAtTargetSince >= 0.75f)
            {
                clientPassSent = true;
                NetworkClient.Send(new MultiplayerSmokeClientPassMessage
                {
                    netId = clientUnit.netId,
                    targetPosition = clientTargetPosition,
                    finalPosition = clientUnit.transform.position
                });
                Debug.Log($"[MultiplayerSmoke] CLIENT_PASS team=1 ownedUnit={clientUnit.netId} movementRoundTrip=ok targetError={targetError:F2}");
                StartCoroutine(QuitClientAfterSend());
            }
        }

        private void OnClientPassMessage(NetworkConnectionToClient connection, MultiplayerSmokeClientPassMessage message)
        {
            NetworkPlayer player = connection.identity != null
                ? connection.identity.GetComponent<NetworkPlayer>()
                : null;
            if (player == null || player.TeamId != 1)
            {
                Fail("Client PASS message came from an invalid team owner.");
                return;
            }

            clientReportedPassNetId = message.netId;
            clientReportedTarget = message.targetPosition;
            clientReportedFinal = message.finalPosition;
            Debug.Log($"[MultiplayerSmoke] HOST_RECEIVED_CLIENT_PASS netId={message.netId} target={message.targetPosition} final={message.finalPosition}");
        }

        private static void DisableEnemyAI()
        {
            foreach (AIController controller in FindObjectsByType<AIController>(FindObjectsSortMode.None))
                controller.enabled = false;
        }

        private static IEnumerator QuitClientAfterSend()
        {
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit(0);
        }

        private static bool HasArg(string[] args, string key)
        {
            foreach (string arg in args)
                if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string GetArgValue(string[] args, string key, string fallback)
        {
            string prefix = key + "=";
            foreach (string arg in args)
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return arg.Substring(prefix.Length);
            return fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            return float.TryParse(value, out float result) ? result : fallback;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static void Pass(string message)
        {
            Debug.Log($"[MultiplayerSmoke] {message}");
            Application.Quit(0);
        }

        private static void Fail(string message)
        {
            Debug.LogError($"[MultiplayerSmoke] FAIL {message}");
            Application.Quit(2);
        }
    }
}
