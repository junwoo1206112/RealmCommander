using UnityEngine;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class BoxSelector : MonoBehaviour
    {
        [SerializeField] private RectTransform selectionBox;
        [SerializeField] private Canvas selectionCanvas;
        [SerializeField] private LayerMask selectionMask = ~0;

        private Vector2 startPosition;
        private Vector2 endPosition;
        private bool isSelecting;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private Camera GetCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            return cam;
        }

        private void Update()
        {
            if (MobileRTSInput.TouchControlsActive) return;
            if (MobileRTSInput.EditorSimulationActive) return;
            HandleSelectionInput();
        }

        private void HandleSelectionInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                startPosition = Input.mousePosition;
                isSelecting = true;
            }

            if (Input.GetMouseButton(0) && isSelecting)
            {
                endPosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (isSelecting)
                {
                    endPosition = Input.mousePosition;
                    CompleteSelection();
                    isSelecting = false;
                }
            }
        }

        public static bool WasClickHandled { get; private set; }

        private void LateUpdate()
        {
            WasClickHandled = false;
        }

        private void CompleteSelection()
        {
            Vector2 min = Vector2.Min(startPosition, endPosition);
            Vector2 max = Vector2.Max(startPosition, endPosition);

            float width = max.x - min.x;
            float height = max.y - min.y;

            if (width < 20f && height < 20f)
            {
                WasClickHandled = true;
                HandleSingleClick();
                return;
            }

            Rect selectionRect = new Rect(min.x, min.y, width, height);

            bool additive = Input.GetKey(KeyCode.LeftShift) || MobileRTSInput.AdditiveSelectionActive;
            if (additive)
                SelectionManager.Instance?.AddUnitsInBoxToSelection(selectionRect);
            else
                SelectionManager.Instance?.SelectUnitsInBox(selectionRect);
        }

        private void HandleSingleClick()
        {
            Camera cam = GetCamera();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (SelectionManager.Instance == null) return;

            bool additive = Input.GetKey(KeyCode.LeftShift) || MobileRTSInput.AdditiveSelectionActive;

            if (Physics.Raycast(ray, out hit, 1000f, selectionMask))
            {
                var unit = hit.collider.GetComponentInParent<Unit>();
                if (unit != null)
                {
                    if (unit.CanIssueLocalCommands)
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
                    }
                    return;
                }

                Building building = hit.collider.GetComponentInParent<Building>();
                if (building != null && building.CanIssueLocalCommands)
                {
                    if (additive && SelectionManager.Instance.IsUnitSelected(building.gameObject))
                        SelectionManager.Instance.RemoveFromSelection(building.gameObject);
                    else if (additive)
                        SelectionManager.Instance.AddToSelection(building.gameObject);
                    else
                        SelectionManager.Instance.SelectUnit(building.gameObject);
                    return;
                }
            }

            if (!additive)
            {
                SelectionManager.Instance.ClearSelection();
            }
        }

        private void OnGUI()
        {
            if (!isSelecting) return;

            Vector2 min = Vector2.Min(startPosition, endPosition);
            Vector2 max = Vector2.Max(startPosition, endPosition);
            float width = max.x - min.x;
            float height = max.y - min.y;

            if (width < 2f && height < 2f) return;

            Rect rect = new Rect(min.x, Screen.height - max.y, width, height);

            GUI.color = new Color(0f, 1f, 0f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = Color.green;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax, rect.y, 1, rect.height), Texture2D.whiteTexture);
        }
    }
}
