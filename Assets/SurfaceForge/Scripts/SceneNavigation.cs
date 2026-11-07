using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SurfaceForge.UI;
using UnityEngine.InputSystem;

namespace SurfaceForge
{
    // 每個場景各自擁有，不建立跨場景單例或保存道具。
    [DefaultExecutionOrder(1000)]
    public sealed class SceneNavigation : MonoBehaviour
    {
        public const string MenuPath = "Assets/SurfaceForge/Scenes/SceneMenu.unity";
        public const string DesktopPath = "Assets/SurfaceForge/Scenes/Main.unity";
        public const string SimulationPath = "Assets/SurfaceForge/Scenes/VRSimulation.unity";
        public const string LoadingPath = "Assets/SurfaceForge/Scenes/Loading.unity";
        internal sealed class Transition
        {
            public Scene Source;
            public string Target;
        }
        internal static Transition CurrentTransition { get; private set; }
        public static bool IsTransitioning => CurrentTransition != null;
        [SerializeField] private PlacementController placement;
        [SerializeField] private PropSelectionController desktopSelection;
        [SerializeField] private SimulationSelectionController simulationSelection;
        [SerializeField] private SimulationPlayer player;
        [SerializeField] private SimulationPropMenu simulationMenu;
        [SerializeField] private PropCatalogView desktopCatalog;
        [SerializeField] private PlacementGuideView guide;
        [SerializeField] private EventSystem sceneEvents;
        [SerializeField] private Text[] navigationLabels;
        [SerializeField] private string[] fontNames = { "Microsoft JhengHei", "Noto Sans CJK TC", "Arial Unicode MS" };
        private Font navigationFont;
        private string pendingPath;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        public bool IsInputReady { get; private set; } = true;
        public bool IsTransitionPending => IsTransitioning || !IsInputReady;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransition() => CurrentTransition = null;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            navigationFont = Font.CreateDynamicFontFromOSFont(fontNames, 28);
            if (navigationLabels != null)
                foreach (var label in navigationLabels) if (label) label.font = navigationFont;
            // 所有 Awake 都早於首個 Update；來源已卸載，新場景只暫停自身輸入。
            if (IsTransitioning) SuspendInput();
        }
        public void OpenDesktop() => Request(DesktopPath);
        public void OpenSimulation() => Request(SimulationPath);
        public void ReturnToMenu() => Request(MenuPath);
        private void Request(string path)
        {
            if (IsTransitionPending || !isActiveAndEnabled) return;
            if (!Application.CanStreamedLevelBeLoaded(path) || !Application.CanStreamedLevelBeLoaded(LoadingPath))
            {
                Debug.LogError($"目標或 Loading 場景未加入有效場景清單：{path} / {LoadingPath}", this);
                return;
            }
            CurrentTransition = new Transition { Source = gameObject.scene, Target = path };
            pendingPath = path;
        }
        private void LateUpdate()
        {
            if (string.IsNullOrEmpty(pendingPath)) return;
            // UI click 派送已返回後才停用模組，避免重入 pointerUp / click。
            pendingPath = null;
            PrepareToLeave();
            StartCoroutine(OpenLoading());
        }
        private IEnumerator OpenLoading()
        {
            var operation = LoadingController.TryLoad(LoadingPath);
            if (operation != null) yield return operation;
            var loading = SceneManager.GetSceneByPath(LoadingPath);
            if (loading.IsValid() && loading.isLoaded && LoadingController.InScene(loading)) yield break;
            // Loading 本身載入失敗時，來源仍存在：停用其視聽元件後嘗試一次選單。
            Debug.LogError("Loading 無法啟動，嘗試返回場景選單。", this);
            if (gameObject.scene.path == MenuPath)
            {
                if (loading.IsValid() && loading.isLoaded)
                {
                    var overlayUnload = LoadingController.TryUnload(loading);
                    if (overlayUnload == null) { LoadingController.ShowFatal("Loading 無法卸載，請重新啟動。"); yield break; }
                    yield return overlayUnload;
                    if (loading.isLoaded) { LoadingController.ShowFatal("Loading 無法卸載，請重新啟動。"); yield break; }
                }
                CurrentTransition = null;
                yield return ResumeWhenReleased();
                yield break;
            }
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (var audio in root.GetComponentsInChildren<AudioListener>(true)) audio.enabled = false;
            }
            if (loading.IsValid() && loading.isLoaded)
            {
                var unload = LoadingController.TryUnload(loading);
                if (unload == null) { LoadingController.ShowFatal("Loading 無法卸載，請重新啟動。"); yield break; }
                yield return unload;
                if (loading.isLoaded) { LoadingController.ShowFatal("Loading 無法卸載，請重新啟動。"); yield break; }
            }
            operation = LoadingController.TryLoad(MenuPath);
            if (operation != null) yield return operation;
            var menu = SceneManager.GetSceneByPath(MenuPath);
            var navigation = InScene(menu);
            if (!navigation || !navigation.isActiveAndEnabled || navigation.IsInputReady || !SceneManager.SetActiveScene(menu))
            {
                Debug.LogError("場景選單復原失敗；輸入維持停用，請停止 Play Mode 或重新啟動。", this);
                LoadingController.ShowFatal("載入失敗，請停止 Play Mode 或重新啟動。");
                yield break;
            }
            navigation.CompleteArrival(gameObject.scene);
        }
        public void PrepareToLeave()
        {
            SuspendInput();
            if (placement) placement.Cancel();
            if (guide) guide.Hide();
            if (desktopSelection) desktopSelection.ClearSelection();
            if (simulationSelection) simulationSelection.ClearSelection();
            if (desktopCatalog) desktopCatalog.SetExpanded(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        private void Suspend(Behaviour behaviour)
        {
            if (!behaviour || !behaviour.enabled) return;
            suspended.Add(behaviour);
            behaviour.enabled = false;
        }
        private void SuspendInput()
        {
            if (!IsInputReady) return;
            IsInputReady = false;
            if (sceneEvents)
            {
                foreach (var module in sceneEvents.GetComponents<BaseInputModule>()) Suspend(module);
                sceneEvents.SetSelectedGameObject(null);
                Suspend(sceneEvents);
            }
            Suspend(placement);
            Suspend(desktopSelection);
            Suspend(simulationSelection);
            Suspend(simulationMenu);
            Suspend(player);
        }
        internal static SceneNavigation InScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var navigation = root.GetComponentInChildren<SceneNavigation>(true);
                if (navigation) return navigation;
            }
            return null;
        }
        internal void CompleteArrival(Scene sceneToUnload) => StartCoroutine(FinishArrival(sceneToUnload));
        private IEnumerator FinishArrival(Scene sceneToUnload)
        {
            var unload = LoadingController.TryUnload(sceneToUnload);
            if (unload == null)
            {
                Debug.LogError("轉場場景無法卸載，輸入維持停用。", this);
                LoadingController.ShowFatal("轉場場景無法卸載，請重新啟動。");
                yield break;
            }
            yield return unload;
            if (sceneToUnload.isLoaded)
            {
                LoadingController.ShowFatal("轉場場景仍未卸載，請重新啟動。");
                yield break;
            }
            CurrentTransition = null;
            yield return ResumeWhenReleased();
        }
        private IEnumerator ResumeWhenReleased()
        {
            // 要求一個沒有按下／放開事件的影格，包含同影格短按。
            do { yield return null; } while (!TransitionButtonsReleased());
            foreach (var behaviour in suspended)
                if (behaviour && !(behaviour is BaseInputModule) && !(behaviour is EventSystem)) behaviour.enabled = true;
            foreach (var behaviour in suspended) if (behaviour is EventSystem) behaviour.enabled = true;
            foreach (var behaviour in suspended) if (behaviour is BaseInputModule) behaviour.enabled = true;
            suspended.Clear();
            IsInputReady = true;
        }
        public static bool TransitionButtonsReleased()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            return !(mouse != null && (Busy(mouse.leftButton) || Busy(mouse.rightButton))) &&
                !(keyboard != null && (Busy(keyboard.tabKey) || Busy(keyboard.escapeKey)));
        }
        private static bool Busy(UnityEngine.InputSystem.Controls.ButtonControl button) =>
            button.isPressed || button.wasPressedThisFrame || button.wasReleasedThisFrame;
        private void OnApplicationQuit() => CurrentTransition = null;
        private void OnDestroy()
        {
            if (navigationFont) Destroy(navigationFont);
        }
    }
}
