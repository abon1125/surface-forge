using UnityEngine;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    public sealed class PropContextMenuView : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform sidebar;
        [SerializeField] private Text targetName;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Vector2 offset = new Vector2(24, 24);
        [SerializeField] private float padding = 12;
        public PlacedProp Target { get; private set; }
        public event System.Action DeleteRequested;
        public event System.Action Unavailable;
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable() { if (deleteButton) deleteButton.onClick.AddListener(RequestDelete); Hide(); }
        private void OnDisable()
        {
            if (deleteButton) deleteButton.onClick.RemoveListener(RequestDelete);
            Hide();
            Unavailable?.Invoke();
        }
        private void RequestDelete() { if (Target) DeleteRequested?.Invoke(); }
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
            if (panel) panel.gameObject.SetActive(false);
        }

        // Canvas rendering follows all camera LateUpdates, including viewport fitting.
        private void OnCanvasWillRender() => RefreshPosition();
        private void Awake() => Canvas.willRenderCanvases += OnCanvasWillRender;
        private void OnDestroy() => Canvas.willRenderCanvases -= OnCanvasWillRender;
        private void LateUpdate() => RefreshPosition();

        private void RefreshPosition()
        {
            if (!isActiveAndEnabled || !panel) return;
            if (!Target || !Target.isActiveAndEnabled) { Hide(); return; }
            if (!canvas || !sceneCamera) { panel.gameObject.SetActive(false); return; }
            var screen = sceneCamera.WorldToScreenPoint(Target.transform.position);
            if (screen.z < sceneCamera.nearClipPlane || screen.z > sceneCamera.farClipPlane ||
                !sceneCamera.pixelRect.Contains(new Vector2(screen.x, screen.y)))
            { panel.gameObject.SetActive(false); return; }
            var root = (RectTransform)panel.parent;
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCamera, out var point))
            { panel.gameObject.SetActive(false); return; }
            var available = root.rect;
            available.xMin += padding; available.yMin += padding;
            available.xMax -= padding; available.yMax -= padding;
            if (sidebar && sidebar.gameObject.activeInHierarchy)
            {
                sidebar.GetWorldCorners(corners);
                var left = float.PositiveInfinity;
                foreach (var corner in corners) left = Mathf.Min(left, root.InverseTransformPoint(corner).x);
                available.xMax = Mathf.Min(available.xMax, left - padding);
            }
            // A uniformly scaled panel keeps the full button reachable in small viewports.
            var scale = Mathf.Min(1, available.width / panel.rect.width, available.height / panel.rect.height);
            if (scale <= 0) { panel.gameObject.SetActive(false); return; }
            panel.localScale = Vector3.one * scale;
            var size = panel.rect.size * scale;
            var pivot = panel.pivot;
            var anchor = point;
            // Prefer beside the object; flip to its left before clamping at the sidebar.
            point += offset + Vector2.Scale(size, pivot);
            if (point.x + size.x * (1 - pivot.x) > available.xMax)
                point.x = anchor.x - offset.x - size.x * (1 - pivot.x);
            point.x = Mathf.Clamp(point.x, available.xMin + size.x * pivot.x, available.xMax - size.x * (1 - pivot.x));
            point.y = Mathf.Clamp(point.y, available.yMin + size.y * pivot.y, available.yMax - size.y * (1 - pivot.y));
            panel.localPosition = new Vector3(point.x, point.y, 0);
            panel.gameObject.SetActive(true);
        }
    }
}
