using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    // VR 場景唯一有效的輸入模組；不派送導航、submit、桌面滑鼠或拖曳。
    [DefaultExecutionOrder(100)]
    public sealed class SimulationRayInputModule : BaseInputModule
    {
        [SerializeField] private SimulationWorldRaycaster worldRaycaster;
        [SerializeField] private SimulationPlayer player;
        [SerializeField] private SimulationPropMenu menu;
        [SerializeField] private ScrollRect list;
        [SerializeField] private LineRenderer rayLine;
        [SerializeField] private Transform hitMarker;
        [SerializeField, Min(1)] private float wheelUnitsPerStep = 120;
        [SerializeField] private PlacementController placement;
        [SerializeField] private PlacementGuideView placementGuide;
        [SerializeField, Min(1)] private float dragThreshold = 18;
        private SimulationPropItem dragSource;
        private Vector2 pressLocal;
        public bool OwnsDrag { get; private set; }
        private PointerEventData pointer;
        private bool focused = true, paused, requireRelease = true;
        private GameObject pressed;
        public SimulationWorldRaycaster.Hit CurrentHit { get; private set; }
        public Ray CurrentRay { get; private set; }
        public bool HasPress => pressed;
        public override bool ShouldActivateModule() => base.ShouldActivateModule() && worldRaycaster && player && menu;
        // 在 LateUpdate 查詢：角色 Update 與面板跟隨已完成，避免前一影格 UI 平面。
        public override void Process() { }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (placement) placement.Finished += PlacementFinished;
            if (player) player.Unavailable += Interrupt;
            if (worldRaycaster) worldRaycaster.Unavailable += Interrupt;
        }
        private void LateUpdate()
        {
            if (eventSystem.currentInputModule != this) { Interrupt(); return; }
            var mouse = Mouse.current;
            if (mouse == null) { Interrupt(); return; }
            Vector2 position = mouse.position.ReadValue();
            bool valid = worldRaycaster && worldRaycaster.isActiveAndEnabled && player && player.isActiveAndEnabled &&
                focused && !paused && Application.isFocused && !player.IsLooking && (OwnsDrag || !mouse.rightButton.isPressed) && InGameView(position);
            if (!valid) { Interrupt(); return; }
            CurrentRay = worldRaycaster.Aim(position);
            CurrentHit = worldRaycaster.Query(CurrentRay);
            Draw();
            if (OwnsDrag)
            {
                if (!menu.IsInteractive || !dragSource || !dragSource.isActiveAndEnabled || !placement || !placement.isActiveAndEnabled || !placement.IsDragging)
                { Interrupt(); return; }
                placement.UpdateWorld(this, CurrentRay, CurrentHit.UI);
                UpdateGuide();
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    // 本影格重新 Aim/Query，EndWorld 再查指定 TerrainCollider；不使用快取姿態。
                    placement.EndWorld(this, CurrentRay, CurrentHit.UI);
                    return;
                }
                if (!mouse.leftButton.isPressed) Interrupt();
                return; // 擁有權期間不派送 hover、click 或滾輪。
            }
            if (dragSource && !dragSource.isActiveAndEnabled) { Interrupt(); return; }
            if (!menu.IsInteractive) { CancelPointer(); requireRelease = true; return; }
            if (pointer == null) pointer = new PointerEventData(eventSystem) { pointerId = -100, button = PointerEventData.InputButton.Left };
            pointer.position = worldRaycaster.ViewCamera.WorldToScreenPoint(CurrentHit.Point);
            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = CurrentHit.UI, worldPosition = CurrentHit.Point,
                worldNormal = CurrentHit.Normal, distance = CurrentHit.Distance, screenPosition = pointer.position };
            HandlePointerExitAndEnter(pointer, CurrentHit.UI);
            if (requireRelease)
            {
                if (!mouse.leftButton.isPressed) requireRelease = false;
                return;
            }
            bool down = mouse.leftButton.wasPressedThisFrame;
            bool up = mouse.leftButton.wasReleasedThisFrame;
            if (pressed && dragSource && mouse.leftButton.isPressed && !up && TryStartDrag()) return;
            // 同影格放開再按住：先結束舊操作，新操作等待下一次放開。
            if (down && up && mouse.leftButton.isPressed) { Release(); Press(); }
            else
            {
                if (down) Press();
                if (up) Release();
            }
            Vector2 wheel = mouse.scroll.ReadValue() / wheelUnitsPerStep;
            if (!pressed && wheel.sqrMagnitude > 0 && CurrentHit.UI && list && list.isActiveAndEnabled && list.viewport &&
                CurrentHit.UI.transform.IsChildOf(list.viewport) && InsideViewport(CurrentHit.Point))
            {
                pointer.scrollDelta = wheel;
                ExecuteEvents.Execute(list.gameObject, pointer, ExecuteEvents.scrollHandler);
            }
        }
        private bool InsideViewport(Vector3 point) => list.viewport.rect.Contains(list.viewport.InverseTransformPoint(point));
        private bool TryStartDrag()
        {
            var rect = (RectTransform)dragSource.transform;
            if (!new Plane(rect.forward, rect.position).Raycast(CurrentRay, out float distance)) return false;
            Vector2 local = rect.InverseTransformPoint(CurrentRay.GetPoint(distance));
            if ((local - pressLocal).sqrMagnitude < dragThreshold * dragThreshold || rect.rect.Contains(local)) return false;
            if (!placement || !placement.BeginWorld(this, dragSource.GetComponent<PropCatalogItemView>().Definition, CurrentRay, CurrentHit.UI)) return false;
            OwnsDrag = true;
            player.SetPlacementInput(true);
            if (list) list.StopMovement();
            Release(false, true);
            if (pointer != null) HandlePointerExitAndEnter(pointer, null);
            UpdateGuide();
            return true;
        }
        private void UpdateGuide()
        {
            if (!placementGuide) return;
            if (placement && placement.IsDragging && placement.HasValidPose) placementGuide.Show(placement.Preview, placement.PreviewPose);
            else placementGuide.Hide();
        }
        private void PlacementFinished()
        {
            OwnsDrag = false;
            ClearSource();
            if (player) player.SetPlacementInput(false);
            if (placementGuide) placementGuide.Hide();
            CancelPointer();
            requireRelease = true;
        }
        private bool InGameView(Vector2 point)
        {
            if (!worldRaycaster.ViewCamera.pixelRect.Contains(point) || point.x < 0 || point.y < 0 || point.x >= Screen.width || point.y >= Screen.height) return false;
#if UNITY_EDITOR
            // 測試裝置仍需明確聚焦 Game View；不把 Scene／Inspector 的滑鼠當成有效操作。
            var window = UnityEditor.EditorWindow.focusedWindow;
            var over = UnityEditor.EditorWindow.mouseOverWindow;
            if (!window || window.GetType().Name != "GameView" || !over || over.GetType().Name != "GameView") return false;
#endif
            return true;
        }
        private void Press()
        {
            if (pressed) Release(false);
            pointer.pressPosition = pointer.position;
            pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;
            pressed = ExecuteEvents.ExecuteHierarchy(CurrentHit.UI, pointer, ExecuteEvents.pointerDownHandler);
            if (!pressed) pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentHit.UI);
            pointer.pointerPress = pressed;
            pointer.rawPointerPress = CurrentHit.UI;
            pointer.eligibleForClick = pressed;
            dragSource = pressed ? pressed.GetComponent<SimulationPropItem>() : null;
            if (dragSource)
            {
                pressLocal = dragSource.transform.InverseTransformPoint(CurrentHit.Point);
                dragSource.Unavailable += Interrupt;
            }
        }
        private void ClearSource()
        {
            if (dragSource) dragSource.Unavailable -= Interrupt;
            dragSource = null;
        }
        private void Release(bool click = true, bool keepSource = false)
        {
            if (pressed)
            {
                ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);
                if (click && pointer.eligibleForClick && pressed == ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentHit.UI))
                    ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerClickHandler);
            }
            pressed = null;
            if (!keepSource) ClearSource();
            if (pointer != null) { pointer.pointerPress = null; pointer.rawPointerPress = null; pointer.eligibleForClick = false; }
        }
        private void CancelPointer()
        {
            Release(false);
            if (pointer != null) { HandlePointerExitAndEnter(pointer, null); pointer.pointerCurrentRaycast = default; pointer.pointerPressRaycast = default; }
        }
        private void Draw()
        {
            if (rayLine)
            {
                rayLine.enabled = true;
                rayLine.SetPosition(0, CurrentRay.origin);
                rayLine.SetPosition(1, CurrentHit.Point);
            }
            if (hitMarker)
            {
                hitMarker.gameObject.SetActive(CurrentHit.HasHit);
                if (CurrentHit.HasHit) hitMarker.position = CurrentHit.Point;
            }
        }
        private void Interrupt()
        {
            if (OwnsDrag && placement) placement.Cancel(this);
            PlacementFinished();
            CancelPointer(); requireRelease = true; CurrentHit = default; CurrentRay = default;
            if (rayLine) rayLine.enabled = false;
            if (hitMarker) hitMarker.gameObject.SetActive(false);
        }
        public override void DeactivateModule() { Interrupt(); base.DeactivateModule(); }
        protected override void OnDisable()
        {
            Interrupt();
            if (placement) placement.Finished -= PlacementFinished;
            if (player) player.Unavailable -= Interrupt;
            if (worldRaycaster) worldRaycaster.Unavailable -= Interrupt;
            base.OnDisable();
        }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Interrupt(); }
        private void OnApplicationPause(bool value) { paused = value; if (value) Interrupt(); }
    }
}
