using UnityEngine;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class CommandInput : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask unitLayer;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }
        }

        private void HandleRightClick()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f, groundLayer | unitLayer))
            {
                CommandManager.Instance?.ProcessRightClick(hit.point, hit);
                ShowMoveIndicator(hit.point);
            }
        }

        private void ShowMoveIndicator(Vector3 position)
        {
            Debug.Log($"Move command issued to: {position}");
        }
    }
}
