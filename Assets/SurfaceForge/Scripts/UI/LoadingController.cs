using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    // 只存在於 Loading 場景。來源先卸載，才載入目標；不建立跨場景物件。
    public sealed class LoadingController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text percentLabel;
        [SerializeField, Min(0)] private float fadeInDuration = .25f;
        [SerializeField, Min(.01f)] private float minimumDisplayDuration = 1.5f;
        [SerializeField, Min(0)] private float fadeOutDuration = .25f;
        [SerializeField] private string[] fontNames = { "Microsoft JhengHei", "Noto Sans CJK TC", "Arial Unicode MS" };
        private Font font;
        private string failure;
        private SceneNavigation destination;
        private bool fatalOnly;
        public float Progress { get; private set; }
        public bool IsFadingOut { get; private set; }
        public float MinimumDisplayDuration => minimumDisplayDuration;
        public bool HasFailed => !string.IsNullOrEmpty(failure);

        private void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(fontNames, 28);
            if (statusLabel) statusLabel.font = font;
            if (percentLabel) percentLabel.font = font;
            if (overlay) overlay.alpha = 0;
            SetProgress(0);
        }
        private IEnumerator Start()
        {
            if (fatalOnly) yield break;
            var request = SceneNavigation.CurrentTransition;
            if (!overlay || !progressFill || !statusLabel || !percentLabel || request == null)
            {
                Fail("Loading 缺少場景參照或有效轉場請求。");
                if (overlay) overlay.alpha = 1;
                yield break;
            }
            yield return Fade(0, 1, fadeInDuration);
            double started = Time.realtimeSinceStartupAsDouble;
            var unload = TryUnload(request.Source);
            if (unload == null) Fail("來源場景無法卸載。");
            else
            {
                while (!unload.isDone)
                {
                    Advance(.1f * unload.progress, started, false);
                    yield return null;
                }
                if (request.Source.isLoaded) Fail("來源場景仍在載入清單中。");
            }
            if (!HasFailed) yield return LoadDestination(request.Target, started);
            if (HasFailed)
            {
                // 一次性復原；保留 Loading 的覆蓋與擁有權，不遞迴重新請求轉場。
                statusLabel.text = "正在返回場景選單…";
                bool cleaned = true;
                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene == gameObject.scene) continue;
                    var navigation = SceneNavigation.InScene(scene);
                    if (navigation) navigation.PrepareToLeave();
                    unload = TryUnload(scene);
                    if (unload == null) { cleaned = false; break; }
                    yield return unload;
                    if (scene.isLoaded) { cleaned = false; break; }
                }
                failure = null;
                if (cleaned) yield return LoadDestination(SceneNavigation.MenuPath, started);
                else Fail("復原時無法卸載操作場景。");
                if (HasFailed)
                {
                    statusLabel.text = "載入失敗，請停止 Play Mode 或重新啟動。";
                    yield break;
                }
            }
            while (Progress < 1)
            {
                Advance(1, started, true);
                yield return null;
            }
            yield return null; // 完成的 100% 至少呈現一個影格。
            IsFadingOut = true;
            yield return Fade(1, 0, fadeOutDuration);
            // 交接給目標場景，避免卸載自身後 coroutine 無法完成解鎖。
            destination.CompleteArrival(gameObject.scene);
        }
        private IEnumerator LoadDestination(string path, double started)
        {
            destination = null;
            var load = TryLoad(path);
            if (load == null) { Fail($"無法載入 {path}"); yield break; }
            while (!load.isDone)
            {
                Advance(.1f + Mathf.Clamp01(load.progress / .9f) * .8f, started, false);
                yield return null;
            }
            var scene = SceneManager.GetSceneByPath(path);
            destination = SceneNavigation.InScene(scene);
            if (!destination || !destination.isActiveAndEnabled || destination.IsInputReady || !SceneManager.SetActiveScene(scene))
                Fail($"目標缺少有效導航／輸入閘門或無法啟用：{path}");
        }
        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0;
            overlay.alpha = from;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Lerp(from, to, duration > 0 ? elapsed / duration : 1);
            }
            overlay.alpha = to;
        }
        // 顯示進度為估計值：實際載入、最低顯示時間與平滑速度共同限制。
        public static float NextProgress(float current, float actual, float elapsed, float minimum, float delta, bool ready)
        {
            minimum = Mathf.Max(.01f, minimum);
            float cap = Mathf.Min(Mathf.Clamp01(actual), Mathf.Clamp01(elapsed / minimum), ready ? 1 : .99f);
            return Mathf.Max(current, Mathf.MoveTowards(current, cap, Mathf.Max(0, delta) / minimum));
        }
        private void Advance(float actual, double started, bool ready) => SetProgress(NextProgress(Progress, actual,
            (float)(Time.realtimeSinceStartupAsDouble - started), minimumDisplayDuration, Time.unscaledDeltaTime, ready));
        private void SetProgress(float value)
        {
            Progress = value;
            if (progressFill) progressFill.rectTransform.anchorMax = new Vector2(value, 1);
            if (percentLabel) percentLabel.text = $"{Mathf.FloorToInt(value * 100)}%";
        }
        private void Fail(string message)
        {
            failure = message;
            Debug.LogError(message, this);
            if (statusLabel) statusLabel.text = message;
        }
        internal static LoadingController InScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<LoadingController>();
                if (controller && controller.isActiveAndEnabled && controller.overlay && controller.progressFill &&
                    controller.statusLabel && controller.percentLabel) return controller;
            }
            return null;
        }
        internal static AsyncOperation TryLoad(string path)
        {
            try { return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive); }
            catch (Exception e) { Debug.LogError($"場景載入失敗：{path}\n{e.Message}"); return null; }
        }
        internal static AsyncOperation TryUnload(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            try { return SceneManager.UnloadSceneAsync(scene); }
            catch (Exception e) { Debug.LogError($"場景卸載失敗：{scene.path}\n{e.Message}"); return null; }
        }
        internal static void ShowFatal(string message)
        {
            // Loading 本身失敗時的最後保底顯示；沒有任何互動或輸入模組。
            var root = new GameObject("Transition Error", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32767;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var rect = background.GetComponent<RectTransform>(); rect.SetParent(root.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(.055f, .075f, .1f, 1);
            var label = new GameObject("Error", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(background.transform, false); label.rectTransform.sizeDelta = new Vector2(1100, 180);
            label.text = message; label.fontSize = 28; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            var controller = root.AddComponent<LoadingController>();
            controller.fatalOnly = true; controller.statusLabel = label; label.font = controller.font;
            controller.failure = message;
        }
        private void OnDestroy() { if (font) Destroy(font); }
    }
}
