using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace SurfaceForge
{
    // Runs before UGUI/placement so a V press cancels the entire old gesture first.
    [DefaultExecutionOrder(-100)]
    public sealed class ViewModeController : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private TerrainOverviewCamera overview;
        [SerializeField] private SimulatedPlayer playerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private RectTransform sidebar;
        [SerializeField] private RectTransform canvas;
        [SerializeField] private CanvasGroup sidebarGroup;
        [SerializeField] private PlacementController placement;
        [SerializeField] private PropSelectionController selection;
        [SerializeField] private InputSystemUIInputModule uiInput;
        [SerializeField] private ScrollRect catalogScroll;
        [SerializeField, Min(0.01f)] private float transitionSeconds = 0.25f;
        [SerializeField, Range(30, 100)] private float simulatedFieldOfView = 65;
        public bool IsSimulating { get; private set; }
        public bool IsTransitioning { get; private set; }
        public SimulatedPlayer Player { get; private set; }
        private bool captured, awaitingRelease;
        private bool oldOverview, oldPlacement, oldSelection, oldUI, oldInteractable, oldBlocks;
        private Transform cameraParent;
        private Vector3 cameraPosition, cameraScale;
        private Quaternion cameraRotation;
        private bool orthographic;
        private float fieldOfView, orthoSize, nearClip, farClip;
        private Rect viewport;
        private Vector2 sidebarPosition;
        private float progress, from, to, elapsed;
        private readonly Vector3[] corners = new Vector3[4];

        private void Update()
        {
            if (Keyboard.current?.vKey.wasPressedThisFrame ?? false) TryToggle();
            if (awaitingRelease && !IsTransitioning && !IsSimulating && PointerReleased()) RestoreInput();
            if (!IsTransitioning) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionSeconds);
            progress = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
            PositionSidebar();
            if (t < 1) return;
            IsTransitioning = false;
            if (!IsSimulating) FinishOverview();
        }

        public bool TryToggle()
        {
            if (!isActiveAndEnabled || IsTransitioning || awaitingRelease) return false;
            if (!IsSimulating)
            {
                if (!sceneCamera || !overview || !playerPrefab || !playerPrefab.HeadAnchor || !spawnPoint ||
                    !sidebar || !canvas || !sidebarGroup || !placement || !selection || !uiInput || !catalogScroll)
                    return false;
                Capture();
                SuspendInput();
                overview.enabled = false;
                Player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                sceneCamera.transform.SetParent(Player.HeadAnchor, false);
                sceneCamera.transform.localPosition = Vector3.zero;
                sceneCamera.transform.localRotation = Quaternion.identity;
                sceneCamera.transform.localScale = Vector3.one;
                sceneCamera.orthographic = false;
                sceneCamera.fieldOfView = simulatedFieldOfView;
                sceneCamera.nearClipPlane = 0.05f;
                sceneCamera.rect = new Rect(0, 0, 1, 1);
                IsSimulating = true;
                AnimateTo(1);
            }
            else
            {
                RestoreCamera(); // Detach before destroying the player's hierarchy.
                RemovePlayer();
                IsSimulating = false;
                AnimateTo(0);
            }
            return true;
        }

        private void Capture()
        {
            var tr = sceneCamera.transform;
            cameraParent = tr.parent; cameraPosition = tr.localPosition;
            cameraRotation = tr.localRotation; cameraScale = tr.localScale;
            orthographic = sceneCamera.orthographic; fieldOfView = sceneCamera.fieldOfView;
            orthoSize = sceneCamera.orthographicSize; viewport = sceneCamera.rect;
            nearClip = sceneCamera.nearClipPlane; farClip = sceneCamera.farClipPlane;
            oldOverview = overview.enabled; oldPlacement = placement.enabled;
            oldSelection = selection.enabled; oldUI = uiInput.enabled;
            oldInteractable = sidebarGroup.interactable; oldBlocks = sidebarGroup.blocksRaycasts;
            sidebarPosition = sidebar.anchoredPosition;
            captured = true;
        }
        private void SuspendInput()
        {
            sidebarGroup.interactable = sidebarGroup.blocksRaycasts = false;
            selection.enabled = false;
            foreach (var row in sidebar.GetComponentsInChildren<UI.PropItemDragHandler>(true)) row.CancelGesture();
            catalogScroll.StopMovement();
            placement.Cancel(); placement.enabled = false;
            uiInput.enabled = false; // Clears UGUI pointer/drag state, including a held old press.
            var events = uiInput.GetComponent<EventSystem>();
            if (events) events.SetSelectedGameObject(null);
        }
        private void AnimateTo(float target)
        { from = progress; to = target; elapsed = 0; IsTransitioning = true; }
        private void LateUpdate()
        {
            if (captured) PositionSidebar(); // Recompute with the current canvas size, including mid-transition resize.
        }
        private void PositionSidebar()
        {
            if (!sidebar || !canvas) return;
            sidebar.anchoredPosition = sidebarPosition;
            sidebar.GetWorldCorners(corners);
            float left = canvas.InverseTransformPoint(corners[0]).x;
            float distance = Mathf.Max(0, canvas.rect.xMax - left) + 2;
            Vector3 offset = sidebar.parent.InverseTransformVector(canvas.TransformVector(Vector3.right * distance));
            sidebar.anchoredPosition = sidebarPosition + (Vector2)offset * progress;
        }
        private void RestoreCamera()
        {
            if (!captured || !sceneCamera) return;
            var tr = sceneCamera.transform;
            tr.SetParent(cameraParent, false); tr.localPosition = cameraPosition;
            tr.localRotation = cameraRotation; tr.localScale = cameraScale;
            sceneCamera.orthographic = orthographic; sceneCamera.fieldOfView = fieldOfView;
            sceneCamera.orthographicSize = orthoSize; sceneCamera.rect = viewport;
            sceneCamera.nearClipPlane = nearClip; sceneCamera.farClipPlane = farClip;
        }
        private void RemovePlayer()
        {
            if (Player) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            Player = null;
        }
        private void FinishOverview()
        {
            progress = 0; PositionSidebar();
            if (overview) overview.enabled = oldOverview;
            // Let TerrainOverviewCamera fit the current dimensions on its next LateUpdate.
            awaitingRelease = true;
            InputSystem.onAfterUpdate -= ResumeAfterRelease;
            InputSystem.onAfterUpdate += ResumeAfterRelease;
            if (PointerReleased()) RestoreInput();
        }
        private static bool PointerReleased() => Mouse.current == null ||
            (!Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed && !Mouse.current.middleButton.isPressed);
        private void ResumeAfterRelease()
        {
            if (InputState.currentUpdateType == InputUpdateType.Dynamic && awaitingRelease &&
                !IsSimulating && !IsTransitioning && PointerReleased()) RestoreInput();
        }
        private void RestoreInput()
        {
            awaitingRelease = false;
            InputSystem.onAfterUpdate -= ResumeAfterRelease;
            if (placement) placement.enabled = oldPlacement;
            if (selection) selection.enabled = oldSelection;
            if (uiInput) uiInput.enabled = oldUI;
            if (sidebarGroup) { sidebarGroup.interactable = oldInteractable; sidebarGroup.blocksRaycasts = oldBlocks; }
            captured = false;
        }
        private void ReturnImmediately()
        {
            if (!captured) return;
            IsTransitioning = IsSimulating = false;
            RestoreCamera(); RemovePlayer(); FinishOverview();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) ReturnImmediately(); }
        private void OnApplicationPause(bool paused) { if (paused) ReturnImmediately(); }
        // Input System's neutral-button callback also runs while this component is disabled.
        // That keeps old UGUI presses suppressed until release without leaving input suspended.
        private void OnDisable() => ReturnImmediately();
        private void OnDestroy()
        {
            ReturnImmediately();
            if (captured) RestoreInput();
            InputSystem.onAfterUpdate -= ResumeAfterRelease;
        }
    }
}
