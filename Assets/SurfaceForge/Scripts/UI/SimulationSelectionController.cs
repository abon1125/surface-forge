using UnityEngine;

namespace SurfaceForge.UI
{
    public sealed class SimulationSelectionController : MonoBehaviour
    {
        [SerializeField] private SimulationContextMenuView menu;
        [SerializeField] private PlacementController placement;
        private readonly PropSelectionState state = new PropSelectionState();
        public PlacedProp Selected => state.Selected;
        public event System.Action Unavailable;
        private void OnEnable()
        {
            if (menu) { menu.DeleteRequested += DeleteSelected; menu.Unavailable += Interrupt; }
            if (placement) placement.GestureStarted += ClearSelection;
            ClearSelection();
        }
        private void OnDisable()
        {
            if (menu) { menu.DeleteRequested -= DeleteSelected; menu.Unavailable -= Interrupt; }
            if (placement) placement.GestureStarted -= ClearSelection;
            Interrupt();
        }
        public void Select(PlacedProp target)
        {
            if (!isActiveAndEnabled || !menu || !menu.isActiveAndEnabled) return;
            state.Select(target);
            if (Selected) menu.Show(Selected); else menu.Hide();
        }
        public void ClearSelection() { state.Clear(); if (menu) menu.Hide(); }
        private void Interrupt() { ClearSelection(); Unavailable?.Invoke(); }
        private void DeleteSelected()
        {
            // 清掉舊 UI 與路由的按下／放開參照後才停用／銷毀唯一目標。
            state.DeleteSelected(() =>
            {
                if (menu) menu.Hide();
                Unavailable?.Invoke();
            });
        }
        private void LateUpdate() { if (!PropSelectionState.IsValid(Selected)) ClearSelection(); }
        private void OnApplicationFocus(bool focused) { if (!focused) Interrupt(); }
        private void OnApplicationPause(bool paused) { if (paused) Interrupt(); }
    }
}
