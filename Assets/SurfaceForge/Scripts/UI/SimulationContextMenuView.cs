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
        [SerializeField, Tooltip("繞行座標偏移：X 水平側移、Y 高度、Z 向觀看者，分別乘以包圍盒寬、高、深。")]
        private Vector3 offsetRatio = new Vector3(0, .6f, 0);
        [SerializeField, Min(.001f), Tooltip("選單寬度相對物件包圍盒最大邊長的比例。")]
        private float widthRatio = .5f;
        [SerializeField, Min(0), Tooltip("水平外接圓以外的間距，相對物件最大邊長。")]
        private float orbitGapRatio = .1f;
        private Vector3 lastOrbitDirection;
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
            if (Target != target) lastOrbitDirection = Vector3.zero;
            Target = target;
            if (targetName) targetName.text = target && target.Definition ? target.Definition.DisplayName : "";
            RefreshPosition();
        }
        public void Hide()
        {
            Target = null;
            lastOrbitDirection = Vector3.zero;
            if (targetName) targetName.text = "";
            if (canvas) canvas.gameObject.SetActive(false);
        }
        private void LateUpdate() => RefreshPosition();
        private void RefreshPosition()
        {
            if (!canvas) return;
            if (!PropSelectionState.IsValid(Target)) { Hide(); return; }
            if (!viewCamera || !isActiveAndEnabled) { canvas.gameObject.SetActive(false); return; }
            // 只以有效碰撞器計算，不將 prefab 的底部錨點額外納入包圍盒。
            var bounds = new Bounds(Target.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (var collider in Target.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                if (!hasBounds) { bounds = collider.bounds; hasBounds = true; }
                else bounds.Encapsulate(collider.bounds);
            }
            var rect = (RectTransform)canvas.transform;
            rect.sizeDelta = size;
            float extent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            // 零尺寸目標使用 1 mm 安全下限；不依觀看距離調整尺寸或位置。
            float width = Mathf.Max(.001f, extent * Mathf.Max(.001f, widthRatio));
            rect.localScale = Vector3.one * (width / Mathf.Max(1, size.x));
            var towardViewer = Vector3.ProjectOnPlane(viewCamera.transform.position - bounds.center, Vector3.up);
            if (towardViewer.sqrMagnitude > .000001f) lastOrbitDirection = towardViewer.normalized;
            else if (lastOrbitDirection.sqrMagnitude < .000001f)
            {
                towardViewer = Vector3.ProjectOnPlane(-viewCamera.transform.forward, Vector3.up);
                // 垂直觀看時 forward 沒有水平分量，改由 camera up 定義後方。
                if (towardViewer.sqrMagnitude < .000001f)
                    towardViewer = Vector3.ProjectOnPlane(viewCamera.transform.up, Vector3.up);
                lastOrbitDirection = towardViewer.sqrMagnitude > .000001f ? towardViewer.normalized : Vector3.back;
            }
            var side = Vector3.Cross(Vector3.up, lastOrbitDirection);
            float radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
            rect.position = bounds.center + Vector3.up * (bounds.size.y * offsetRatio.y)
                + side * (bounds.size.x * offsetRatio.x)
                + lastOrbitDirection * (radius + extent * Mathf.Max(0, orbitGapRatio) + bounds.size.z * offsetRatio.z);
            var forward = rect.position - viewCamera.transform.position;
            if (forward.sqrMagnitude > .000001f)
            {
                // 平面法線朝離開觀看者的方向，維持 UGUI 正面朝向觀看者。
                // 正上／下方改用攝影機 up，避免 LookRotation 共線。
                var up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > .999f
                    ? Vector3.forward : Vector3.up;
                rect.rotation = Quaternion.LookRotation(forward, up);
            }
            canvas.gameObject.SetActive(true);
        }
    }
}
