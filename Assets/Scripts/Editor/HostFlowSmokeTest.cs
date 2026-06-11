using System;
using System.IO;
using Mirror;
using RealmCommander.Network;
using RealmCommander.RTS;
using RealmCommander.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmCommander.Editor
{
    [InitializeOnLoad]
    public static class HostFlowSmokeTest
    {
        private const string SessionKey = "RealmCommander.HostSmoke.Active";
        private const double TimeoutSeconds = 20d;

        private static double startedAt;
        private static bool hostRequested;
        private static string failure;

        static HostFlowSmokeTest()
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        [MenuItem("Tools/Realm Commander/Run Host Flow Smoke Test")]
        public static void Run()
        {
            SessionState.SetBool(SessionKey, true);
            startedAt = EditorApplication.timeSinceStartup;
            hostRequested = false;
            failure = null;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;

            EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;

            startedAt = EditorApplication.timeSinceStartup;
            LobbyUI lobby = UnityEngine.Object.FindFirstObjectByType<LobbyUI>();
            if (lobby == null)
            {
                Finish(false, "LobbyUI was not found in LobbyScene.");
                return;
            }

            lobby.OnHostGame();
            hostRequested = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(SessionKey, false) || !EditorApplication.isPlaying || !hostRequested)
                return;

            if (!string.IsNullOrEmpty(failure))
            {
                Finish(false, failure);
                return;
            }

            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                Finish(false, "Timed out waiting for the host game to become playable.");
                return;
            }

            if (SceneManager.GetActiveScene().name != "MainScene") return;
            if (!NetworkServer.active || !NetworkClient.active) return;
            if (NetworkClient.localPlayer == null) return;
            if (NetworkGameManager.Instance == null || NetworkGameManager.Instance.State != NetworkGameManager.GameState.Playing)
                return;

            Unit[] units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            int friendly = 0;
            int enemy = 0;
            foreach (Unit unit in units)
            {
                if (unit.IsEnemy) enemy++;
                else friendly++;
            }

            if (friendly == 0 || enemy == 0) return;

            Finish(true, $"Host flow passed. Scene=MainScene, friendly={friendly}, enemy={enemy}, player={NetworkClient.localPlayer.name}");
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            if (type == LogType.Exception || type == LogType.Assert)
                failure = condition + Environment.NewLine + stackTrace;
        }

        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(SessionKey, false);
            Debug.Log($"[HostFlowSmokeTest] {(success ? "PASS" : "FAIL")}: {message}");
            File.WriteAllText("Logs/HostFlowSmokeResult.txt", $"{(success ? "PASS" : "FAIL")}\n{message}\n");

            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;

            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();

            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
