using UnityEngine;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    // 常駐元件與可隱藏 Canvas 分開，隱藏後不留下可查詢的 Graphic。
    [DefaultExecutionOrder(50)]
    public sealed class SimulationContextMenuView : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text targetName;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Vector2 size = new Vector2(260, 120);
        [SerializeField, Min(.0001f)] private float metresPerPixel = .002f;
        [SerializeField, Min(.1f)] private float referenceDistance = 2;
        [SerializeField, Min(0)] private float gap = .15f;
        [SerializeField] private Vector2 readableDistance = new Vector2(.6f, 15);
        public PlacedProp Target { get; private set; }
        public bool IsVisible => canvas && canvas.gameObject.activeInHierarchy;
        public event System.Action DeleteRequested;
        public event System.Action Unavailable;
        private void OnEnable() { if (deleteButton) deleteButton.onClick.AddListener(RequestDelete); Hide(); }
        private void OnDisable()
        {
            if (deleteButton) deleteButton.onClick.RemoveListener(RequestDelete);
            Hide(); Unavailable?.Invoke();
        }
        private void RequestDelete() { if (PropSelectionState.IsValid(Target) && IsVisible) DeleteRequested?.Invoke(); }
        public void Show(PlacedProp target)
        {
            Target = target;
            if (targetName) targetName.text = target && target.Definition ? target.Definition.DisplayName : "";
            RefreshPosition();
        }
        public void Hide()
        {
            Target = null;
            if (targetName) targetName.text = "";
            if (canvas) canvas.gameObject.SetActive(false);
        }
        private void LateUpdate() => RefreshPosition();
        private void RefreshPosition()
        {
            if (!canvas) return;
            if (!PropSelectionState.IsValid(Target)) { Hide(); return; }
            if (!viewCamera || !isActiveAndEnabled) { canvas.gameObject.SetActive(false); return; }
            var bounds = new Bounds(Target.transform.position, Vector3.zero);
            foreach (var collider in Target.GetComponentsInChildren<Collider>())
                if (collider.enabled) bounds.Encapsulate(collider.bounds);
            var anchor = bounds.center;
            var screen = viewCamera.WorldToViewportPoint(anchor);
            float distance = Vector3.Distance(viewCamera.transform.position, anchor);
            if (screen.z <= 0 || distance > readableDistance.y)
            { canvas.gameObject.SetActive(false); return; }
            var rect = (RectTransform)canvas.transform;
            rect.sizeDelta = size;
            float pixelScale = metresPerPixel * Mathf.Max(1, distance / referenceDistance);
            rect.localScale = Vector3.one * pixelScale;
            var viewer = viewCamera.transform;
            float halfWidth = size.x * pixelScale * .5f;
            float side = screen.x > .5f ? -1 : 1;
            float sideExtent = Vector3.Dot(new Vector3(Mathf.Abs(viewer.right.x), Mathf.Abs(viewer.right.y), Mathf.Abs(viewer.right.z)), bounds.extents);
            Vector3 towardViewer = (viewer.position - anchor).normalized;
            float frontExtent = Vector3.Dot(new Vector3(Mathf.Abs(towardViewer.x), Mathf.Abs(towardViewer.y), Mathf.Abs(towardViewer.z)), bounds.extents);
            // 選單位於物件側邊並前移至包圍盒前方，右球低於眼睛時也不被所選道具擋住。
            rect.position = anchor + viewer.right * side * (sideExtent + gap + halfWidth) + towardViewer * (frontExtent + gap);
            // 保持世界空間平面，依攝影機投影把完整選單移回可視區域。
            // 近距離使用至少 1 m 的深度，避免裁切與過度放大；仍由世界射線判定遮擋。
            var local = viewer.InverseTransformPoint(rect.position);
            float depth = Mathf.Clamp(local.z, Mathf.Max(1, readableDistance.x, viewCamera.nearClipPlane + .1f), viewCamera.farClipPlane - .1f);
            var left = viewCamera.ViewportToWorldPoint(new Vector3(0, .5f, depth));
            var right = viewCamera.ViewportToWorldPoint(new Vector3(1, .5f, depth));
            var bottom = viewCamera.ViewportToWorldPoint(new Vector3(.5f, 0, depth));
            var top = viewCamera.ViewportToWorldPoint(new Vector3(.5f, 1, depth));
            float width = Vector3.Distance(left, right), height = Vector3.Distance(bottom, top);
            const float padding = .04f;
            float fit = Mathf.Min(1, width * (1 - padding * 2) / (size.x * pixelScale), height * (1 - padding * 2) / (size.y * pixelScale));
            pixelScale *= fit;
            rect.localScale = Vector3.one * pixelScale;
            rect.rotation = viewer.rotation;
            var point = viewCamera.WorldToViewportPoint(viewer.TransformPoint(new Vector3(local.x, local.y, depth)));
            float halfX = size.x * pixelScale / width * .5f;
            float halfY = size.y * pixelScale / height * .5f;
            point.x = Mathf.Clamp(point.x, padding + halfX, 1 - padding - halfX);
            point.y = Mathf.Clamp(point.y, padding + halfY, 1 - padding - halfY);
            rect.position = viewCamera.ViewportToWorldPoint(new Vector3(point.x, point.y, depth));
            canvas.gameObject.SetActive(true);
        }
    }
}
