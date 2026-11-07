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
        [SerializeField, Tooltip("世界軸向偏移，分別乘以物件包圍盒的寬、高、深。")]
        private Vector3 offsetRatio = new Vector3(0, .6f, 0);
        [SerializeField, Min(.001f), Tooltip("選單寬度相對物件包圍盒最大邊長的比例。")]
        private float widthRatio = .5f;
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
            rect.position = bounds.center + Vector3.Scale(bounds.size, offsetRatio);
            var forward = rect.position - viewCamera.transform.position;
            if (forward.sqrMagnitude > .000001f)
            {
                // 平面法線朝離開觀看者的方向，維持 UGUI 正面朝向觀看者。
                // 正上／下方改用攝影機 up，避免 LookRotation 共線。
                var up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > .999f
                    ? viewCamera.transform.up : Vector3.up;
                rect.rotation = Quaternion.LookRotation(forward, up);
            }
            canvas.gameObject.SetActive(true);
        }
    }
}
