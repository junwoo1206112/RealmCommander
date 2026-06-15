using Mirror;
using UnityEngine;

namespace RealmCommander.Network
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        private string playerName = "Player";

        [SyncVar(hook = nameof(OnPlayerTeamChanged))]
        [SerializeField, Range(0, 1)] private int teamId = 0;

        [SyncVar]
        private bool isGameReady = false;

        [SerializeField] private bool showDebugOverlay;

        public static NetworkPlayer Local { get; private set; }
        public int TeamId => teamId;
        public string PlayerName => playerName;
        public bool IsGameReady => isGameReady;

        [Server]
        public void ServerSetTeamId(int newTeamId)
        {
            teamId = Mathf.Clamp(newTeamId, 0, 1);
        }

        public override void OnStartLocalPlayer()
        {
            Local = this;
            CmdSetPlayerName(System.Environment.UserName);
        }

        [Command]
        private void CmdSetPlayerName(string name)
        {
            string safeName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
            safeName = System.Text.RegularExpressions.Regex.Replace(safeName, @"[<>]", "");
            safeName = safeName.Substring(0, Mathf.Min(24, safeName.Length));
            playerName = safeName;
        }

        public void SetReady(bool ready)
        {
            CmdSetReady(ready);
        }

        [Command]
        private void CmdSetReady(bool ready)
        {
            var gm = NetworkGameManager.Instance;
            if (gm != null && gm.State != NetworkGameManager.GameState.WaitingForPlayers
                && gm.State != NetworkGameManager.GameState.Idle)
                return;
            isGameReady = ready;
        }

        private bool _isDestroyed;
        private MaterialPropertyBlock teamColorBlock;

        private void OnDestroy()
        {
            _isDestroyed = true;
            if (isLocalPlayer)
                Local = null;
        }

        private void OnPlayerNameChanged(string oldValue, string newValue)
        {
            if (_isDestroyed || gameObject == null) return;
            gameObject.name = $"Player_{newValue}";
        }

        private void OnPlayerTeamChanged(int oldValue, int newValue)
        {
            if (_isDestroyed) return;
            if (teamColorBlock == null)
                teamColorBlock = new MaterialPropertyBlock();
            teamColorBlock.SetColor("_Color", newValue == 0 ? Color.blue : Color.red);
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer == null) continue;
                renderer.SetPropertyBlock(teamColorBlock);
            }

            if (isLocalPlayer)
                RTS.ResourceManager.Instance?.RefreshLocalDisplay();
        }

        private void OnGUI()
        {
            if (!showDebugOverlay) return;
            if (_isDestroyed || netIdentity == null || !isOwned) return;

            Vector2 size = new Vector2(200f, 25f);
            Vector2 position = new Vector2(10f, Screen.height - size.y - 10f);

            GUI.Box(new Rect(position, size), $"Player: {playerName} (Team {teamId + 1})");
        }
    }
}
