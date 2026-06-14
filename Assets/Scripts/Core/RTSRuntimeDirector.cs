using RealmCommander.Network;
using RealmCommander.RTS;
using UnityEngine;

namespace RealmCommander.Core
{
    public class RTSRuntimeDirector : MonoBehaviour
    {
        private bool hasFocused;
        private float nextAttemptTime;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (hasFocused || Time.time < nextAttemptTime) return;
            nextAttemptTime = Time.time + 0.5f;

            Unit targetUnit = FindFirstFriendlyUnit();
            if (targetUnit == null) return;

            FocusCamera(targetUnit.transform.position);
            SelectionManager.Instance?.SelectUnit(targetUnit.gameObject);
            hasFocused = true;
        }

        private static Unit FindFirstFriendlyUnit()
        {
            int teamId = NetworkPlayer.Local != null ? NetworkPlayer.Local.TeamId : 0;
            var registry = EntityRegistry.Instance;
            if (registry != null)
            {
                foreach (Unit unit in registry.AllUnits)
                {
                    bool isFriendly = teamId == 0 ? !unit.IsEnemy : unit.IsEnemy;
                    if (unit != null && isFriendly && unit.IsAlive)
                        return unit;
                }
            }

            foreach (Unit unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
            {
                bool isFriendly = teamId == 0 ? !unit.IsEnemy : unit.IsEnemy;
                if (unit != null && isFriendly && unit.IsAlive)
                    return unit;
            }

            return null;
        }

        private static void FocusCamera(Vector3 position)
        {
            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null) return;

            MobileRTSCameraController controller = camera.GetComponent<MobileRTSCameraController>();
            if (controller != null)
            {
                controller.FocusOn(position);
                return;
            }

            camera.transform.position = position + new Vector3(0f, 22f, -7f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        }
    }
}
