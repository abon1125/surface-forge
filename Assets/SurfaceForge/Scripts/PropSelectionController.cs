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
        private bool pressed, rejected, pending, clearForDrag, released, releaseRejected;
        private Vector2 pressPosition, releasePosition, eventReleasePosition;
        private int pressFrame, releaseFrame, pendingPressFrame, eventPressFrame;
        private int interruptedFrame = -1;
        private InputSettings inputSettings;
        private bool previousEventMerging;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            // Input System can merge a first press with later held-position events before
            // onEvent. Preserve the sequence while this scene's selection input is active.
            inputSettings = InputSystem.settings;
            previousEventMerging = inputSettings.disableRedundantEventsMerging;
            inputSettings.disableRedundantEventsMerging = true;
            InputSystem.onEvent += CapturePointerEvent;
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
            InputSystem.onEvent -= CapturePointerEvent;
            if (inputSettings) inputSettings.disableRedundantEventsMerging = previousEventMerging;
            inputSettings = null;
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
            pressed = pending = clearForDrag = released = false;
            ClearSelection();
        }

        private bool InViewport(Vector2 p) => sceneCamera && sceneCamera.pixelRect.Contains(p) &&
            p.x >= 0 && p.y >= 0 && p.x < Screen.width && p.y < Screen.height;

        private void CapturePointerEvent(InputEventPtr inputEvent, InputDevice device)
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic || device != Mouse.current ||
                !(device is Mouse mouse) || (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>())) return;
            // onEvent runs before device state changes: compare event button with prior state,
            // and read the position from that event, never the end-of-update mouse position.
            var position = mouse.position.ReadValueFromEvent(inputEvent, out var value) ? value : mouse.position.ReadValue();
            var hasButton = mouse.leftButton.ReadValueFromEvent(inputEvent, out var button);
            if (hasButton &&
                button >= mouse.leftButton.pressPointOrDefault && !mouse.leftButton.isPressed)
            {
                pressFrame = Time.frameCount;
                pressed = true;
                pressPosition = position;
                rejected = pressFrame <= interruptedFrame || !InViewport(position) || !placement ||
                    placement.IsOverUI(position) || placement.IsDragging;
            }
            var threshold = EventSystem.current ? EventSystem.current.pixelDragThreshold : 5;
            if (pressed && !rejected && (position - pressPosition).sqrMagnitude > threshold * threshold)
            {
                RejectSceneDrag(); // Latch even if later events return to the press point.
            }
            if (pressed && hasButton && button < mouse.leftButton.pressPointOrDefault && mouse.leftButton.isPressed)
            {
                released = true;
                releaseRejected = rejected || !InViewport(position) || !placement ||
                    placement.IsOverUI(position) || placement.IsDragging;
                eventReleasePosition = position;
                eventPressFrame = pressFrame;
                pressed = false;
            }
        }

        private void RejectSceneDrag()
        {
            rejected = clearForDrag = true;
            // A newer drag supersedes both a prior LateUpdate candidate and a prior
            // release captured earlier in the same Dynamic input batch.
            pending = released = false;
        }

        // Runs one frame after release: all UGUI Update and placement LateUpdate work from
        // the release frame has completed, regardless of component execution order.
        private void LateUpdate()
        {
            if (clearForDrag) { clearForDrag = false; pending = false; ClearSelection(); }
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
                if (!rejected && moved) { RejectSceneDrag(); ClearSelection(); clearForDrag = false; }
                rejected |= !InViewport(position) || placement.IsOverUI(position) || placement.IsDragging ||
                    placement.LastConsumedFrame >= pressFrame || moved;
                if (!mouse.leftButton.isPressed) pressed = false;
            }
            if (released)
            {
                released = false;
                pending = !releaseRejected;
                releasePosition = eventReleasePosition;
                pendingPressFrame = eventPressFrame;
                // Stamp here, after UI Update, rather than relying on input-update frame numbering.
                releaseFrame = Time.frameCount;
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
