using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmCommander.Core
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        private List<GameObject> selectedUnits = new List<GameObject>();
        private HashSet<GameObject> selectableUnits = new HashSet<GameObject>();

        public IReadOnlyList<GameObject> SelectedUnits => selectedUnits;
        public event Action<List<GameObject>> OnSelectionChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterSelectableUnit(GameObject unit)
        {
            selectableUnits.Add(unit);
        }

        public void UnregisterSelectableUnit(GameObject unit)
        {
            selectableUnits.Remove(unit);
            selectedUnits.Remove(unit);
        }

        public void SelectUnit(GameObject unit)
        {
            if (unit == null || !selectableUnits.Contains(unit)) return;

            ClearSelection();
            selectedUnits.Add(unit);
            UpdateUnitSelectionVisual(unit, true);
            OnSelectionChanged?.Invoke(selectedUnits);
        }

        public void AddToSelection(GameObject unit)
        {
            if (unit == null || !selectableUnits.Contains(unit)) return;

            if (!selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
                UpdateUnitSelectionVisual(unit, true);
                OnSelectionChanged?.Invoke(selectedUnits);
            }
        }

        public void RemoveFromSelection(GameObject unit)
        {
            if (selectedUnits.Contains(unit))
            {
                selectedUnits.Remove(unit);
                UpdateUnitSelectionVisual(unit, false);
                OnSelectionChanged?.Invoke(selectedUnits);
            }
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
            OnSelectionChanged?.Invoke(selectedUnits);
        }

        public void SelectUnitsInBox(Rect selectionBox)
        {
            ClearSelection();

            foreach (var unit in selectableUnits)
            {
                if (unit == null) continue;

                var cam = Camera.main;
                if (cam == null) continue;

                Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);
                if (screenPos.z > 0 && selectionBox.Contains(new Vector2(screenPos.x, screenPos.y)))
                {
                    selectedUnits.Add(unit);
                    UpdateUnitSelectionVisual(unit, true);
                }
            }

            OnSelectionChanged?.Invoke(selectedUnits);
        }

        private void UpdateUnitSelectionVisual(GameObject unit, bool isSelected)
        {
            var unitComponent = unit.GetComponent<RTS.Unit>();
            if (unitComponent != null)
            {
                unitComponent.SetSelected(isSelected);
            }
        }

        public bool IsUnitSelected(GameObject unit)
        {
            return selectedUnits.Contains(unit);
        }

        public int SelectedCount => selectedUnits.Count;
    }
}
