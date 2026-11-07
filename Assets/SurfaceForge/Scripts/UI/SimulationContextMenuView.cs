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
        private Vector3 offsetRatio = new Vector3(0, .3f, 0);
        [SerializeField, Min(.001f), Tooltip("選單寬度相對物件包圍盒最大邊長的比例。")]
        private float widthRatio = .5f;
        [SerializeField, Min(0), Tooltip("水平外接圓以外的間距，相對物件最大邊長。")]
        private float orbitGapRatio = .1f;
        [SerializeField] private ContextMenuFade fade = new ContextMenuFade();
        private Vector3 lastOrbitDirection;
        public PlacedProp Target { get; private set; }
        public bool IsVisible => fade.IsOpen && canvas && canvas.gameObject.activeInHierarchy;
        public event System.Action DeleteRequested;
        public event System.Action Unavailable;
        private void OnEnable() { fade.Initialize(gameObject, canvas ? canvas.gameObject : null); if (deleteButton) deleteButton.onClick.AddListener(RequestDelete); Hide(); }
        private void OnDisable()
        {
            if (deleteButton) deleteButton.onClick.RemoveListener(RequestDelete);
            Hide(); fade.SnapClosed(); Unavailable?.Invoke();
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
            fade.SetVisible(false);
        }
        private void OnDestroy() => fade.SnapClosed();
        private void OnApplicationFocus(bool focused) { if (!focused) { Hide(); fade.SnapClosed(); } }
        private void OnApplicationPause(bool paused) { if (paused) { Hide(); fade.SnapClosed(); } }
        private void LateUpdate() => RefreshPosition();
        private void RefreshPosition()
        {
            if (!canvas) return;
            if (!PropSelectionState.IsValid(Target)) { Hide(); return; }
            if (!viewCamera || !isActiveAndEnabled) { fade.SnapClosed(); return; }
            // 只以有效碰撞器計算，不將 prefab 的底部錨點額外納入包圍盒。
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;
            foreach (var collider in Target.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                Bounds local;
                if (collider is BoxCollider box) local = new Bounds(box.center, box.size);
                else if (collider is SphereCollider sphere) local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2);
                else if (collider is MeshCollider mesh && mesh.sharedMesh) local = mesh.sharedMesh.bounds;
                else
                {
                    // 其他 Collider 保守使用世界包圍盒，轉回物件座標。
                    local = collider.bounds;
                }
                for (int i = 0; i < 8; i++)
                {
                    var point = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    bool supported = collider is BoxCollider || collider is SphereCollider || (collider is MeshCollider m && m.sharedMesh);
                    point = Target.transform.InverseTransformPoint(supported ? collider.transform.TransformPoint(point) : point);
                    if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                    else bounds.Encapsulate(point);
                }
            }
            var rect = (RectTransform)canvas.transform;
            rect.sizeDelta = size;
            var dimensions = Vector3.Scale(bounds.size, new Vector3(Target.transform.TransformVector(Vector3.right).magnitude,
                Target.transform.TransformVector(Vector3.up).magnitude, Target.transform.TransformVector(Vector3.forward).magnitude));
            float extent = Mathf.Max(dimensions.x, dimensions.y, dimensions.z);
            // 零尺寸目標使用 1 mm 安全下限；不依觀看距離調整尺寸或位置。
            float width = Mathf.Max(.001f, extent * Mathf.Max(.001f, widthRatio));
            rect.localScale = Vector3.one * (width / Mathf.Max(1, size.x));
            // 在物件本地 XZ 平面繞行，Y 軸隨斜坡上的物件傾斜。
            var center = Target.transform.TransformPoint(bounds.center);
            var axis = Target.transform.up;
            var towardViewer = Vector3.ProjectOnPlane(viewCamera.transform.position - center, axis);
            lastOrbitDirection = Vector3.ProjectOnPlane(lastOrbitDirection, axis).normalized;
            if (towardViewer.sqrMagnitude > .000001f) lastOrbitDirection = towardViewer.normalized;
            else if (lastOrbitDirection.sqrMagnitude < .000001f)
            {
                towardViewer = Vector3.ProjectOnPlane(-viewCamera.transform.forward, axis);
                // 垂直觀看時 forward 沒有水平分量，改由 camera up 定義後方。
                if (towardViewer.sqrMagnitude < .000001f)
                    towardViewer = Vector3.ProjectOnPlane(viewCamera.transform.up, axis);
                lastOrbitDirection = towardViewer.sqrMagnitude > .000001f ? towardViewer.normalized : -Target.transform.forward;
            }
            var side = Vector3.Cross(axis, lastOrbitDirection);
            float radius = new Vector2(dimensions.x, dimensions.z).magnitude * .5f;
            rect.position = center + axis * (dimensions.y * offsetRatio.y)
                + side * (dimensions.x * offsetRatio.x)
                + lastOrbitDirection * (radius + extent * Mathf.Max(0, orbitGapRatio) + dimensions.z * offsetRatio.z);
            var forward = rect.position - viewCamera.transform.position;
            if (forward.sqrMagnitude > .000001f)
            {
                // 平面法線朝離開觀看者的方向，維持 UGUI 正面朝向觀看者。
                // 正上／下方改用攝影機 up，避免 LookRotation 共線。
                var up = Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > .999f
                    ? Vector3.forward : Vector3.up;
                rect.rotation = Quaternion.LookRotation(forward, up);
            }
            fade.Show(gameObject, canvas.gameObject);
        }
    }
}
