using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmCommander.Core
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        private List<GameObject> selectedUnits = new List<GameObject>();
        private HashSet<GameObject> selectedLookup = new HashSet<GameObject>();
        private HashSet<GameObject> selectableUnits = new HashSet<GameObject>();

        public IReadOnlyList<GameObject> SelectedUnits => selectedUnits;
        public event Action<List<GameObject>> OnSelectionChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            selectedUnits.RemoveAll(unit => unit == null);
            selectableUnits.RemoveWhere(unit => unit == null);
            ClearSelection();
        }

        public void RegisterSelectableUnit(GameObject unit)
        {
            if (unit == null) return;
            selectableUnits.Add(unit);
        }

        public void UnregisterSelectableUnit(GameObject unit)
        {
            selectableUnits.Remove(unit);
            if (selectedLookup.Remove(unit))
            {
                selectedUnits.Remove(unit);
                OnSelectionChanged?.Invoke(selectedUnits);
            }
        }

        public void SelectUnit(GameObject unit)
        {
            if (unit == null) return;
            if (!selectableUnits.Contains(unit))
                selectableUnits.Add(unit);

            ClearSelection();
            selectedUnits.Add(unit);
            selectedLookup.Add(unit);
            UpdateUnitSelectionVisual(unit, true);
            OnSelectionChanged?.Invoke(selectedUnits);
            Audio.AudioManager.Instance?.PlayUnitSelect();
        }

        public void AddToSelection(GameObject unit)
        {
            if (unit == null) return;
            if (!selectableUnits.Contains(unit))
                selectableUnits.Add(unit);

            if (selectedLookup.Add(unit))
            {
                selectedUnits.Add(unit);
                UpdateUnitSelectionVisual(unit, true);
                OnSelectionChanged?.Invoke(selectedUnits);
            }
        }

        public void RemoveFromSelection(GameObject unit)
        {
            if (selectedLookup.Remove(unit))
            {
                selectedUnits.Remove(unit);
                UpdateUnitSelectionVisual(unit, false);
                OnSelectionChanged?.Invoke(selectedUnits);
            }
        }

        public void AddUnitsInBoxToSelection(Rect selectionBox)
        {
            var cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;

            var registry = EntityRegistry.Instance;
            if (registry != null)
            {
                foreach (var unit in registry.AllUnits)
                {
                    if (unit == null || !unit.IsAlive || !unit.CanIssueLocalCommands) continue;
                    if (selectedLookup.Contains(unit.gameObject)) continue;

                    if (!selectableUnits.Contains(unit.gameObject))
                        selectableUnits.Add(unit.gameObject);

                    if (IsUnitInSelectionRect(unit, selectionBox, cam))
                    {
                        selectedUnits.Add(unit.gameObject);
                        selectedLookup.Add(unit.gameObject);
                        UpdateUnitSelectionVisual(unit.gameObject, true);
                    }
                }
            }

            OnSelectionChanged?.Invoke(selectedUnits);
        }

        public void ClearSelection()
        {
            foreach (var unit in selectedUnits)
            {
                if (unit != null)
                {
                    UpdateUnitSelectionVisual(unit, false);
                }
            }
            selectedUnits.Clear();
            selectedLookup.Clear();
            OnSelectionChanged?.Invoke(selectedUnits);
        }

        public void SelectUnitsInBox(Rect selectionBox)
        {
            ClearSelection();

            var cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;

            var registry = EntityRegistry.Instance;
            if (registry != null)
            {
                foreach (var unit in registry.AllUnits)
                {
                    if (unit == null || !unit.IsAlive || !unit.CanIssueLocalCommands) continue;
                    if (!selectableUnits.Contains(unit.gameObject))
                        selectableUnits.Add(unit.gameObject);

                    if (IsUnitInSelectionRect(unit, selectionBox, cam))
                    {
                        selectedUnits.Add(unit.gameObject);
                        selectedLookup.Add(unit.gameObject);
                        UpdateUnitSelectionVisual(unit.gameObject, true);
                    }
                }
            }

            OnSelectionChanged?.Invoke(selectedUnits);
        }

        private static bool IsUnitInSelectionRect(RTS.Unit unit, Rect selectionBox, Camera cam)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);
            if (screenPos.z <= 0) return false;

            return selectionBox.Contains(new Vector2(screenPos.x, screenPos.y));
        }

        private void UpdateUnitSelectionVisual(GameObject unit, bool isSelected)
        {
            var unitComponent = unit.GetComponent<RTS.Unit>();
            if (unitComponent != null)
            {
                unitComponent.SetSelected(isSelected);
                return;
            }

            unit.GetComponent<RTS.Building>()?.SetSelected(isSelected);
        }

        public bool IsUnitSelected(GameObject unit)
        {
            return unit != null && selectedLookup.Contains(unit);
        }

        public int SelectedCount => selectedUnits.Count;

        public int GetUnitIndex(GameObject unit)
        {
            if (unit == null || !selectedLookup.Contains(unit)) return -1;
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                if (selectedUnits[i] == unit) return i;
            }
            return -1;
        }

    }
}
