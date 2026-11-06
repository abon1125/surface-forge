using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfaceForge.UI
{
    // 留在常駐 MenuAnchor，不能放在會收合的 Canvas 上。
    public sealed class SimulationPropMenu : MonoBehaviour
    {
        [Header("明確場景參照")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform viewer;
        [Header("相對左球的世界公尺與畫布尺寸")]
        [SerializeField] private Vector3 anchorOffset = new Vector3(0, 0.29f, 0.15f);
        [SerializeField] private Vector2 panelSize = new Vector2(380, 380);
        [SerializeField, Min(0.0001f)] private float metresPerPixel = 0.001f;
        [SerializeField] private Vector3 facingOffset;
        private bool focused = true;
        private bool tabHeld = true;
        public bool IsExpanded => panel && panel.activeSelf;

        private void OnEnable() { tabHeld = true; Close(); }
        private void OnDisable() { tabHeld = true; Close(); }
        private void OnApplicationFocus(bool value) { focused = value; tabHeld = true; }
        private void OnApplicationPause(bool value) { if (value) tabHeld = true; }
        private void Close() { if (panel) panel.SetActive(false); }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!focused || !Application.isFocused || keyboard == null) { tabHeld = true; return; }
            bool held = keyboard.tabKey.isPressed;
            // 以持續狀態偵測邊緣；中斷後必須先觀察放開，避免回焦重播按鍵。
            if (held && !tabHeld && panel) panel.SetActive(!panel.activeSelf);
            tabHeld = held;
            if (keyboard.escapeKey.isPressed) Close();
        }
        private void LateUpdate()
        {
            if (!panel || !viewer) return;
            // 左球有 S1 視覺縮放，抵銷繼承縮放，偏移仍以公尺表達。
            var parent = transform.parent;
            var scale = parent.lossyScale;
            transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            transform.position = parent.position + parent.rotation * anchorOffset;
            transform.rotation = Quaternion.LookRotation(transform.position - viewer.position, viewer.up)
                * Quaternion.Euler(facingOffset);
            var rect = (RectTransform)panel.transform;
            rect.sizeDelta = panelSize;
            rect.localScale = Vector3.one * metresPerPixel;
        }
    }
}
