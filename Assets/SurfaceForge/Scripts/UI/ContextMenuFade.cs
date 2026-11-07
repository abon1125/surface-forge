using System;
using DG.Tweening;
using UnityEngine;

namespace SurfaceForge.UI
{
    [Serializable]
    public sealed class ContextMenuFade
    {
        [SerializeField, Min(0)] private float openDuration = .18f;
        [SerializeField, Min(0)] private float closeDuration = .12f;
        [SerializeField] private Ease openEase = Ease.OutCubic;
        [SerializeField] private Ease closeEase = Ease.InCubic;
        private GameObject root, owner;
        private CanvasGroup group;
        private Tween tween;
        private bool open;
        public bool IsOpen => open;

        public void Initialize(GameObject host, GameObject panel)
        {
            owner = host; root = panel;
            if (root)
            {
                group = root.GetComponent<CanvasGroup>();
                // Unity 缺少元件時可能回傳原生端已失效的 wrapper；不可用 CLR 的 ?? 判斷。
                if (!group) group = root.AddComponent<CanvasGroup>();
            }
            SnapClosed();
        }
        public void Show(GameObject host, GameObject panel)
        {
            // 設定可被 Unity 重新反序列化；顯示時重新確認非序列化的執行期綁定。
            // 也支援顯示請求先於 OnEnable 初始化到達的情況。
            if (owner != host || root != panel || !group) Initialize(host, panel);
            SetVisible(true);
        }
        public void SetVisible(bool value)
        {
            if (!root || !group || open == value) return;
            open = value;
            tween?.Kill(); tween = null;
            // 關閉當下即撤銷互動；保留圖像完成淡出，不保留刪除目標。
            group.interactable = group.blocksRaycasts = value;
            if (value) root.SetActive(true);
            float target = value ? 1 : 0;
            float duration = Mathf.Max(0, value ? openDuration : closeDuration) * Mathf.Abs(target - group.alpha);
            if (!Application.isPlaying || duration <= 0)
            {
                group.alpha = target; root.SetActive(value); return;
            }
            tween = DOTween.To(() => group.alpha, a => group.alpha = a, target, duration)
                .SetEase(value ? openEase : closeEase).SetUpdate(true)
                .SetLink(owner, LinkBehaviour.KillOnDisable)
                .OnComplete(() => { tween = null; if (!open && root) root.SetActive(false); });
        }
        public void SnapClosed()
        {
            tween?.Kill(); tween = null; open = false;
            if (group) { group.alpha = 0; group.interactable = group.blocksRaycasts = false; }
            if (root) root.SetActive(false);
        }
    }
}
