using UnityEngine;
using UnityEngine.EventSystems;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class MobileRTSInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;

        [Header("Touch Settings")]
        [SerializeField, Min(4f)] private float dragThreshold = 18f;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private bool simulateTouchInEditor;

        private Vector2 touchStartPosition;
        private Vector2 touchCurrentPosition;
        private int activeFingerId = -1;
        private bool isDragging;
        private bool startedOverUi;
        private float touchStartTime;

        public static bool TouchControlsActive => Application.isMobilePlatform && Input.touchSupported;
        public static bool EditorSimulationActive { get; private set; }
        public static bool AdditiveSelectionActive { get; private set; }

        private const float LongPressThreshold = 0.4f;

        private void Awake()
        {
            if (mainCamera == null || mainCamera.orthographic)
                mainCamera = ResolveGameplayCamera();
            EditorSimulationActive = simulateTouchInEditor && Application.isEditor;
        }

        private static Camera ResolveGameplayCamera()
        {
            Camera main = Camera.main;
            if (main != null && main.isActiveAndEnabled && !main.orthographic)
                return main;

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].isActiveAndEnabled && !cameras[i].orthographic)
                    return cameras[i];
            }

            return null;
        }

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                HandleTouches();
            }
            else if (simulateTouchInEditor && Application.isEditor)
            {
                HandleEditorSimulation();
            }
        }

        private void HandleTouches()
        {
            if (Input.touchCount != 1)
            {
                CancelGesture();
                return;
            }

            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginGesture(touch.fingerId, touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (touch.fingerId == activeFingerId) UpdateGesture(touch.position);
                    break;
                case TouchPhase.Ended:
                    if (touch.fingerId == activeFingerId) EndGesture(touch.position);
                    break;
                case TouchPhase.Canceled:
                    CancelGesture();
                    break;
            }
        }

        private void HandleEditorSimulation()
        {
            if (Input.GetMouseButtonDown(0)) BeginGesture(0, Input.mousePosition);
            if (Input.GetMouseButton(0) && activeFingerId == 0) UpdateGesture(Input.mousePosition);
            if (Input.GetMouseButtonUp(0) && activeFingerId == 0) EndGesture(Input.mousePosition);
        }

        private void BeginGesture(int fingerId, Vector2 position)
        {
            activeFingerId = fingerId;
            touchStartPosition = position;
            touchCurrentPosition = position;
            touchStartTime = Time.time;
            isDragging = false;
            startedOverUi = IsPointerOverUi(fingerId);
        }

        private void UpdateGesture(Vector2 position)
        {
            touchCurrentPosition = position;
            float distance = Vector2.Distance(touchStartPosition, touchCurrentPosition);
            isDragging = distance >= dragThreshold;

            if (!isDragging && Time.time - touchStartTime >= LongPressThreshold)
            {
                AdditiveSelectionActive = true;
            }
        }

        private void EndGesture(Vector2 position)
        {
            touchCurrentPosition = position;
            if (!startedOverUi)
            {
                if (isDragging)
                {
                    SelectUnitsInDragArea();
                }
                else
                {
                    HandleTap(position);
                }
            }

            CancelGesture();
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (mainCamera == null || SelectionManager.Instance == null) return;

            bool additive = Input.GetKey(KeyCode.LeftShift) || AdditiveSelectionActive;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, raycastMask))
            {
                Unit unit = hit.collider.GetComponentInParent<Unit>();
                if (unit != null && unit.CanIssueLocalCommands)
                {
                    if (additive)
                    {
                        if (SelectionManager.Instance.IsUnitSelected(unit.gameObject))
                            SelectionManager.Instance.RemoveFromSelection(unit.gameObject);
                        else
                            SelectionManager.Instance.AddToSelection(unit.gameObject);
                    }
                    else
                    {
                        SelectionManager.Instance.SelectUnit(unit.gameObject);
                    }
                    return;
                }

                if (SelectionManager.Instance.SelectedCount > 0)
                {
                    CommandManager.Instance?.ProcessRightClick(hit.point, hit);
                    return;
                }
            }

            if (!additive)
            {
                SelectionManager.Instance.ClearSelection();
            }
        }

        private void SelectUnitsInDragArea()
        {
            Vector2 min = Vector2.Min(touchStartPosition, touchCurrentPosition);
            Vector2 max = Vector2.Max(touchStartPosition, touchCurrentPosition);
            SelectionManager.Instance?.SelectUnitsInBox(new Rect(min, max - min));
        }

        private static bool IsPointerOverUi(int fingerId)
        {
            if (EventSystem.current == null) return false;
            return fingerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private void CancelGesture()
        {
            activeFingerId = -1;
            isDragging = false;
            startedOverUi = false;
            AdditiveSelectionActive = false;
        }

        private void OnGUI()
        {
            if (!isDragging || startedOverUi) return;

            Rect rect = ToGuiRect(touchStartPosition, touchCurrentPosition);
            GUI.color = new Color(0.1f, 0.8f, 1f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Rect ToGuiRect(Vector2 start, Vector2 end)
        {
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
        }
    }
}
