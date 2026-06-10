using UnityEngine;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public class BoxSelector : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RectTransform selectionBox;
        [SerializeField] private Canvas selectionCanvas;

        private Vector2 startPosition;
        private Vector2 endPosition;
        private bool isSelecting;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            HandleSelectionInput();
        }

        private void HandleSelectionInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    startPosition = Input.mousePosition;
                    isSelecting = true;

                    if (selectionBox != null)
                    {
                        selectionBox.gameObject.SetActive(true);
                        selectionBox.anchoredPosition = startPosition;
                        selectionBox.sizeDelta = Vector2.zero;
                    }
                }
            }

            if (Input.GetMouseButton(0) && isSelecting)
            {
                endPosition = Input.mousePosition;
                UpdateSelectionBox();
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (isSelecting)
                {
                    endPosition = Input.mousePosition;
                    CompleteSelection();
                    isSelecting = false;

                    if (selectionBox != null)
                    {
                        selectionBox.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void UpdateSelectionBox()
        {
            if (selectionBox == null) return;

            Vector2 size = endPosition - startPosition;
            selectionBox.anchoredPosition = startPosition + size / 2f;
            selectionBox.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private void CompleteSelection()
        {
            Vector2 min = Vector2.Min(startPosition, endPosition);
            Vector2 max = Vector2.Max(startPosition, endPosition);

            float width = max.x - min.x;
            float height = max.y - min.y;

            if (width < 5f && height < 5f)
            {
                HandleSingleClick();
                return;
            }

            Rect selectionRect = new Rect(min.x, min.y, width, height);
            SelectionManager.Instance?.SelectUnitsInBox(selectionRect);
        }

        private void HandleSingleClick()
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                var unit = hit.collider.GetComponent<Unit>();
                if (unit != null)
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (SelectionManager.Instance.IsUnitSelected(hit.collider.gameObject))
                        {
                            SelectionManager.Instance.RemoveFromSelection(hit.collider.gameObject);
                        }
                        else
                        {
                            SelectionManager.Instance.AddToSelection(hit.collider.gameObject);
                        }
                    }
                    else
                    {
                        SelectionManager.Instance.SelectUnit(hit.collider.gameObject);
                    }
                    return;
                }
            }

            if (!Input.GetKey(KeyCode.LeftShift))
            {
                SelectionManager.Instance?.ClearSelection();
            }
        }

        private void OnGUI()
        {
            if (!isSelecting) return;

            GUI.color = new Color(0f, 1f, 0f, 0.3f);
            Rect rect = GetScreenRect(startPosition, endPosition);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = Color.green;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax, rect.y, 1, rect.height), Texture2D.whiteTexture);
        }

        private Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
        }
    }
}
