using UnityEngine;
using RealmCommander.Core;

namespace RealmCommander.RTS
{
    public static class SelectionHelper
    {
        public static void HandleSelection(GameObject obj, bool isCurrentlySelected)
        {
            if (SelectionManager.Instance == null) return;
            if (BoxSelector.WasClickHandled) return;

            bool additive = Input.GetKey(KeyCode.LeftShift) || MobileRTSInput.AdditiveSelectionActive;

            if (additive)
            {
                if (isCurrentlySelected)
                    SelectionManager.Instance.RemoveFromSelection(obj);
                else
                    SelectionManager.Instance.AddToSelection(obj);
            }
            else
            {
                SelectionManager.Instance.SelectUnit(obj);
            }
        }
    }
}
