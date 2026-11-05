using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SurfaceForge
{
    public sealed class PropSelectionController : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private PlacementController placement;
        [SerializeField] private UI.PropContextMenuView menu;
        [SerializeField] private LayerMask placedPropMask;
        [SerializeField, Min(1)] private float maxRayDistance = 500;
        public PlacedProp Selected { get; private set; }
        private bool pressed, rejected, pending;
        private Vector2 pressPosition, releasePosition;
        private int pressFrame, releaseFrame, pendingPressFrame;
        private int interruptedFrame = -1, capturedPressFrame = -1;

        private void OnEnable()
        {
            InputSystem.onAfterUpdate += CapturePress;
            if (placement) placement.GestureStarted += Interrupt;
            if (menu)
            {
                menu.DeleteRequested += DeleteSelected;
                menu.Unavailable += Interrupt;
            }
            Interrupt();
        }

        private void OnDisable()
        {
            InputSystem.onAfterUpdate -= CapturePress;
            if (placement) placement.GestureStarted -= Interrupt;
            if (menu)
            {
                menu.DeleteRequested -= DeleteSelected;
                menu.Unavailable -= Interrupt;
            }
            Interrupt();
        }

        public void ClearSelection()
        {
            Selected = null;
            if (menu) menu.Hide();
        }

        private void Interrupt()
        {
            interruptedFrame = Time.frameCount;
            pressed = pending = false;
            ClearSelection();
        }

        private bool InViewport(Vector2 p) => sceneCamera && sceneCamera.pixelRect.Contains(p) &&
            p.x >= 0 && p.y >= 0 && p.x < Screen.width && p.y < Screen.height;

        private void CapturePress()
        {
            // Snapshot UI before UGUI can hide a clicked panel, including down/up in one frame.
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || capturedPressFrame == Time.frameCount) return;
            capturedPressFrame = pressFrame = Time.frameCount;
            pressed = true;
            pressPosition = mouse.position.ReadValue();
            rejected = pressFrame <= interruptedFrame || !InViewport(pressPosition) || !placement ||
                placement.IsOverUI(pressPosition) || placement.IsDragging;
        }

        // Runs one frame after release: all UGUI Update and placement LateUpdate work from
        // the release frame has completed, regardless of component execution order.
        private void LateUpdate()
        {
            if (Selected && !Selected.isActiveAndEnabled) ClearSelection();
            else if (!Selected) ClearSelection();
            if (!placement || !menu || !menu.isActiveAndEnabled || !sceneCamera)
            { Interrupt(); return; }
            if (pending && Time.frameCount > releaseFrame)
            {
                pending = false;
                if (interruptedFrame < pendingPressFrame && !placement.IsDragging && placement.LastConsumedFrame < pendingPressFrame &&
                    !placement.IsOverUI(releasePosition) && InViewport(releasePosition))
                    SelectAt(releasePosition);
            }
            var mouse = Mouse.current;
            if (mouse == null) { Interrupt(); return; }
            var position = mouse.position.ReadValue();
            if (pressed)
            {
                var threshold = EventSystem.current ? EventSystem.current.pixelDragThreshold : 5;
                var moved = (position - pressPosition).sqrMagnitude > threshold * threshold;
                if (!rejected && moved) ClearSelection();
                rejected |= !InViewport(position) || placement.IsOverUI(position) || placement.IsDragging ||
                    placement.LastConsumedFrame >= pressFrame || moved;
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    pressed = false;
                    pending = !rejected;
                    releasePosition = position;
                    releaseFrame = Time.frameCount;
                    pendingPressFrame = pressFrame;
                }
                else if (!mouse.leftButton.isPressed) pressed = false;
            }
        }

        private void SelectAt(Vector2 position)
        {
            PlacedProp target = null;
            if (Physics.Raycast(sceneCamera.ScreenPointToRay(position), out var hit,
                maxRayDistance, placedPropMask, QueryTriggerInteraction.Ignore))
                target = hit.collider.GetComponentInParent<PlacedProp>();
            if (!target || !target.isActiveAndEnabled || !target.Definition) { ClearSelection(); return; }
            Selected = target;
            menu.Show(target);
        }

        private void DeleteSelected()
        {
            var target = Selected;
            Interrupt(); // Clear references before destruction and before another button invocation.
            if (target) { target.gameObject.SetActive(false); Destroy(target.gameObject); }
        }

        private void OnApplicationFocus(bool focused) { if (!focused) Interrupt(); }
        private void OnApplicationPause(bool paused) { if (paused) Interrupt(); }
    }
}
