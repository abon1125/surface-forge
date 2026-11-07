using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfaceForge
{
    [RequireComponent(typeof(Camera))]
    public sealed class TerrainOverviewCamera : MonoBehaviour
    {
        [SerializeField] private RectTransform sidebar;
        [SerializeField] private Terrain terrain;
        [SerializeField] private PlacementController placement;
        [SerializeField] private SceneNavigation navigation;
        [SerializeField, Min(1.01f)] private float zoomPerStep = 1.15f;
        [SerializeField, Range(.1f, 1)] private float minimumZoom = .35f;
        [SerializeField, Min(1)] private float maximumZoom = 1.5f;
        [SerializeField, Min(.01f)] private float wheelUnitsPerStep = 120f;
        private float zoom = 1;
        private bool focused = true, paused, requireWheelIdle = true;
        private Camera overview;
        private readonly Vector3[] corners = new Vector3[4];

        private void Awake() => overview = GetComponent<Camera>();
        private void OnEnable() { zoom = 1; requireWheelIdle = true; }
        private void OnDisable() { zoom = 1; requireWheelIdle = true; }
        private void OnApplicationFocus(bool value) { focused = value; requireWheelIdle = true; }
        private void OnApplicationPause(bool value) { paused = value; requireWheelIdle = true; }

        private bool CanZoom(Vector2 point)
        {
            if (!focused || paused || !Application.isFocused || !overview.pixelRect.Contains(point)
                || !placement || !navigation || navigation.IsTransitionPending
                || placement.IsDragging || Mouse.current.leftButton.isPressed || placement.IsOverUI(point)) return false;
#if UNITY_EDITOR
            var window = UnityEditor.EditorWindow.focusedWindow;
            var over = UnityEditor.EditorWindow.mouseOverWindow;
            if (!window || window.GetType().Name != "GameView" || !over || over.GetType().Name != "GameView") return false;
#endif
            return true;
        }

        // 中斷後先等一個無滾輪輸入的影格，避免沿用回焦或轉場中的 delta。
        private void ApplyWheel(float delta, bool allowed)
        {
            if (!allowed) { requireWheelIdle = true; return; }
            if (requireWheelIdle) { if (Mathf.Approximately(delta, 0)) requireWheelIdle = false; return; }
            float steps = Mathf.Clamp(delta / Mathf.Max(.01f, wheelUnitsPerStep), -10, 10);
            zoom = Mathf.Clamp(zoom * Mathf.Pow(Mathf.Max(1.01f, zoomPerStep), -steps),
                Mathf.Clamp(minimumZoom, .1f, 1), Mathf.Max(1, maximumZoom));
        }

        private void LateUpdate()
        {
            sidebar.GetWorldCorners(corners);
            float width = Mathf.Clamp(corners[0].x / Screen.width, 0.1f, 1f);
            overview.rect = new Rect(0, 0, width, 1);
            // Fit the finite heightfield bounds in camera space, including a margin.
            Vector3 size = terrain.terrainData.size;
            Vector3 center = terrain.transform.position + size * 0.5f;
            float halfWidth = 0, halfHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = terrain.transform.position + new Vector3(
                    (i & 1) == 0 ? 0 : size.x,
                    (i & 2) == 0 ? 0 : size.y,
                    (i & 4) == 0 ? 0 : size.z);
                Vector3 offset = transform.InverseTransformVector(point - center);
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(offset.x));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(offset.y));
            }
            float aspect = Mathf.Max(0.1f, Screen.width * width / Screen.height);
            var mouse = Mouse.current;
            ApplyWheel(mouse != null ? mouse.scroll.ReadValue().y : 0, mouse != null && CanZoom(mouse.position.ReadValue()));
            overview.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect) * 1.12f * zoom;
        }
    }
}
