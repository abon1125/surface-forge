using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

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
        [Header("左球展開動畫")]
        [SerializeField, Min(0.01f)] private float openDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.18f;
        [SerializeField, Range(0.01f, 1)] private float collapsedScale = 0.15f;
        [SerializeField] private Ease openEase = Ease.OutCubic;
        [SerializeField] private Ease closeEase = Ease.InCubic;
        private Tween transition;
        private float progress;
        private CanvasGroup canvasGroup;
        private bool expanded;
        private bool focused = true;
        private bool requireTabRelease = true;
        // 代表目標狀態；收回期間 Canvas 仍保持有效，直到動畫完成。
        public bool IsExpanded => expanded;

        private void OnEnable() { requireTabRelease = true; Close(); }
        private void OnDisable() { requireTabRelease = true; Close(); }
        private void OnApplicationFocus(bool value) { focused = value; requireTabRelease = true; if (!value) Snap(expanded); }
        private void OnApplicationPause(bool value) { if (value) { requireTabRelease = true; Snap(expanded); } }
        private void OnDestroy() { KillTransition(); }
        private void Close() { Snap(false); }
        private void KillTransition() { transition?.Kill(); transition = null; }
        private void Snap(bool value)
        {
            KillTransition(); expanded = value; progress = value ? 1 : 0;
            if (!panel) return;
            panel.SetActive(value); ApplyVisual();
        }
        private void SetExpanded(bool value)
        {
            if (!panel || expanded == value) return;
            KillTransition(); expanded = value; panel.SetActive(true); ApplyVisual();
            float target = value ? 1 : 0;
            float duration = (value ? openDuration : closeDuration) * Mathf.Abs(target - progress);
            // 單一進度同時驅動位置、縮放及透明度；反向時不重設起點。
            transition = DOTween.To(() => progress, p => progress = p, target, Mathf.Max(0.001f, duration))
                .SetEase(value ? openEase : closeEase).SetUpdate(true)
                .SetTarget(this).SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => { transition = null; if (!expanded && panel) panel.SetActive(false); ApplyVisual(); });
        }
        private void ApplyVisual()
        {
            if (!panel) return;
            if (!canvasGroup) canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup) canvasGroup.alpha = progress;
            var rect = (RectTransform)panel.transform;
            rect.sizeDelta = panelSize;
            rect.localPosition = transform.InverseTransformPoint(transform.parent.position) * (1 - progress);
            rect.localScale = Vector3.one * metresPerPixel * (panel.activeSelf ? Mathf.Lerp(collapsedScale, 1, progress) : 1);
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!focused || !Application.isFocused || keyboard == null) { requireTabRelease = true; return; }
            // 只有中斷後使用持續狀態等待放開，該影格不處理可能重播的按下。
            if (requireTabRelease)
            {
                if (!keyboard.tabKey.isPressed) requireTabRelease = false;
            }
            // 保留兩次 Update 間的短按，以及放開後再次按下的事件。
            else if (keyboard.tabKey.wasPressedThisFrame) SetExpanded(!expanded);
            if (keyboard.escapeKey.wasPressedThisFrame) SetExpanded(false);
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
            ApplyVisual();
        }
    }
}
