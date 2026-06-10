using Mirror;
using UnityEngine;

namespace RealmCommander.Network
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        public string playerName = "Player";

        [SyncVar(hook = nameof(OnPlayerTeamChanged))]
        public int teamId = 0;

        [SyncVar]
        public bool isGameReady = false;

        public static NetworkPlayer Local { get; private set; }

        public override void OnStartLocalPlayer()
        {
            Local = this;
            CmdSetPlayerName(System.Environment.UserName);
        }

        [Command]
        private void CmdSetPlayerName(string name)
        {
            playerName = name;
        }

        public void SetReady(bool ready)
        {
            CmdSetReady(ready);
        }

        [Command]
        private void CmdSetReady(bool ready)
        {
            isGameReady = ready;
        }

        private bool _isDestroyed;

        private void OnDestroy()
        {
            _isDestroyed = true;
        }

        private void OnPlayerNameChanged(string oldValue, string newValue)
        {
            if (_isDestroyed || gameObject == null) return;
            gameObject.name = $"Player_{newValue}";
        }

        private void OnPlayerTeamChanged(int oldValue, int newValue)
        {
            if (_isDestroyed) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || renderer.material == null) continue;
                renderer.material.color = newValue == 0 ? Color.blue : Color.red;
            }
        }

        private void OnGUI()
        {
            if (_isDestroyed || netIdentity == null || !isOwned) return;

            Vector2 size = new Vector2(200f, 25f);
            Vector2 position = new Vector2(10f, Screen.height - size.y - 10f);

            GUI.Box(new Rect(position, size), $"Player: {playerName} (Team {teamId + 1})");
        }
    }
}
