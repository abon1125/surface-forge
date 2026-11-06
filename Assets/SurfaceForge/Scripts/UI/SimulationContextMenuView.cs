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
        private readonly Vector3[] corners = new Vector3[4];
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
            if (screen.z <= viewCamera.nearClipPlane || screen.z >= viewCamera.farClipPlane ||
                screen.x < 0 || screen.x > 1 || screen.y < 0 || screen.y > 1 ||
                distance < readableDistance.x || distance > readableDistance.y)
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
            rect.rotation = Quaternion.LookRotation(rect.position - viewer.position, viewer.up);
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var p = viewCamera.WorldToViewportPoint(corner);
                if (p.z <= viewCamera.nearClipPlane || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1)
                { canvas.gameObject.SetActive(false); return; }
            }
            canvas.gameObject.SetActive(true);
        }
    }
}
