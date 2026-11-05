using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    [RequireComponent(typeof(PropCatalogItemView))]
    public sealed class PropItemDragHandler : MonoBehaviour, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PlacementController controller;
        private ScrollRect scroll;
        private bool scrolling;
        private bool placing;
        private int pointerId;
        private PointerEventData scrollEvent;

        public void Initialize(PlacementController value, ScrollRect scrollRect)
        { controller = value; scroll = scrollRect; }

        public void OnInitializePotentialDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            e.useDragThreshold = true;
            if (scroll) scroll.OnInitializePotentialDrag(e);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || scrolling || placing) return;
            pointerId = e.pointerId;
            if (controller) controller.ConsumeGesture();
            var delta = e.position - e.pressPosition;
            scrolling = Mathf.Abs(delta.y) > Mathf.Abs(delta.x);
            if (scrolling)
            {
                scrollEvent = e;
                if (scroll) scroll.OnBeginDrag(e);
            }
            else if (controller)
            {
                if (scroll) scroll.StopMovement();
                placing = controller.Begin(this, GetComponent<PropCatalogItemView>().Definition, e.position);
            }
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || e.pointerId != pointerId) return;
            if (scrolling && scroll) scroll.OnDrag(e);
            else if (placing && controller) controller.UpdatePreview(e.position);
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || e.pointerId != pointerId) return;
            if ((scrolling || placing) && controller) controller.ConsumeGesture();
            if (scrolling && scroll) scroll.OnEndDrag(e);
            else if (placing && controller) controller.End(this, e.position);
            ResetGesture();
        }
        private void ResetGesture() { scrolling = placing = false; scrollEvent = null; }
        public void PlacementCancelled() => ResetGesture();
        private void OnDisable()
        {
            if (scrolling && scroll && scrollEvent != null) scroll.OnEndDrag(scrollEvent);
            if (controller) controller.Cancel(this);
            ResetGesture();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
        private void OnApplicationPause(bool paused) { if (paused) OnDisable(); }
    }
}
