using UnityEngine;

namespace SurfaceForge
{
    // 桌面與世界射線共用的實例選取及刪除規則，不讀取任何輸入。
    public sealed class PropSelectionState
    {
        public PlacedProp Selected { get; private set; }
        public static bool IsValid(PlacedProp target) => target && target.isActiveAndEnabled &&
            target.Definition && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded &&
            target.gameObject.layer == LayerMask.NameToLayer("PlacedProp");

        public static PlacedProp Resolve(Collider collider)
        {
            var target = collider ? collider.GetComponentInParent<PlacedProp>() : null;
            return IsValid(target) ? target : null;
        }
        public void Select(PlacedProp target)
        {
            Clear(); Selected = IsValid(target) ? target : null;
            if (Selected) Selected.Unavailable += Clear;
        }
        public void Clear()
        {
            if (Selected) Selected.Unavailable -= Clear;
            Selected = null;
        }
        public void DeleteSelected(System.Action beforeDestroy = null)
        {
            var target = Selected;
            Clear();
            beforeDestroy?.Invoke();
            if (!IsValid(target)) return;
            target.gameObject.SetActive(false);
            Object.Destroy(target.gameObject);
        }
    }
}
