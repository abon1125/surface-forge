using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using SurfaceForge.UI;

namespace SurfaceForge.Tests
{
    // 由使用者在 Test Runner 執行；不取代原生滑鼠／世界射線人工驗收。
    public class SceneNavigationTests
    {
        private Mouse mouse;
        private Keyboard keyboard;
        private InputDevice[] nativeDevices = new InputDevice[0];
        private bool background, merging;
        private float timeScale;
        [UnitySetUp]
        public IEnumerator EnterRuntime()
        {
            EditorSceneManager.OpenScene(SceneNavigation.MenuPath);
            yield return new EnterPlayMode();
            timeScale = Time.timeScale;
            background = Application.runInBackground; Application.runInBackground = true;
            merging = InputSystem.settings.disableRedundantEventsMerging;
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            nativeDevices = InputSystem.devices.Where(d => d.enabled && (d is Mouse || d is Keyboard) && d != mouse && d != keyboard).ToArray();
            foreach (var device in nativeDevices) InputSystem.DisableDevice(device);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Frames(2);
        }
        [UnityTest]
        public IEnumerator AdditiveLoadingCleansAndReentryStartsFresh()
        {
            foreach (bool simulation in new[] { false, true })
            {
                var navigation = Object.FindFirstObjectByType<SceneNavigation>();
                if (simulation) navigation.OpenSimulation(); else navigation.OpenDesktop();
                yield return UntilScene(simulation ? "VRSimulation" : "Main");
                AssertSingletons();
                var placement = Object.FindFirstObjectByType<PlacementController>();
                var definition = AssetDatabase.LoadAssetAtPath<PropDefinition>("Assets/SurfaceForge/Data/Props/Cube.asset");
                var prop = Object.Instantiate(definition.Prefab);
                foreach (var child in prop.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = LayerMask.NameToLayer("PlacedProp");
                var placed = prop.AddComponent<PlacedProp>();
                placed.Initialize(definition);
                var terrain = Object.FindFirstObjectByType<Terrain>();
                var point = terrain.transform.position + terrain.terrainData.size * .5f;
                Assert.That(placement.BeginWorld(placement, definition, new Ray(point + Vector3.up * 100, Vector3.down), false), Is.True);
                var preview = placement.Preview;
                navigation = Object.FindFirstObjectByType<SceneNavigation>();
                if (simulation)
                {
                    var selection = Object.FindFirstObjectByType<SimulationSelectionController>();
                    selection.Select(placed);
                    Assert.That(selection.Selected, Is.SameAs(placed));
                }
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                navigation.ReturnToMenu();
                // 重複請求不能覆蓋第一個切換目的地。
                navigation.OpenDesktop();
                yield return UntilScene("SceneMenu");
                Assert.That(!preview && !prop, Is.True, "舊場景實例和預覽應已卸載");
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Cursor.visible, Is.True);
                Assert.That(InputSystem.settings.disableRedundantEventsMerging, Is.EqualTo(merging), "舊輸入訂閱停用後應恢復事件合併設定");
                AssertSingletons();
                navigation = Object.FindFirstObjectByType<SceneNavigation>();
                if (simulation) navigation.OpenSimulation(); else navigation.OpenDesktop();
                yield return UntilScene(simulation ? "VRSimulation" : "Main");
                Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None), Is.Empty);
                Assert.That(Object.FindFirstObjectByType<PlacementController>().IsDragging, Is.False);
                if (simulation)
                {
                    Assert.That(Object.FindFirstObjectByType<SimulationPropMenu>().IsExpanded, Is.False);
                    Assert.That(Object.FindFirstObjectByType<SimulationSelectionController>().Selected, Is.Null);
                }
                Object.FindFirstObjectByType<SceneNavigation>().ReturnToMenu();
                yield return UntilScene("SceneMenu");
            }
        }
        [UnityTest]
        public IEnumerator PausedTransitionWaitsForAllTriggerButtonsToRelease()
        {
            Time.timeScale = 0;
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab, Key.Escape));
            Object.FindFirstObjectByType<SceneNavigation>().OpenSimulation();
            double deadline = EditorApplication.timeSinceStartup + 30;
            while (SceneNavigation.IsTransitioning)
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline));
                EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            }
            var navigation = Object.FindFirstObjectByType<SceneNavigation>();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("VRSimulation"));
            Assert.That(navigation.IsInputReady, Is.False);
            Assert.That(Object.FindFirstObjectByType<SimulationPlayer>().enabled, Is.False);
            Assert.That(Object.FindFirstObjectByType<SimulationRayInputModule>().enabled, Is.False);
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return Frames(3);
            Assert.That(navigation.IsInputReady, Is.False, "Tab/Esc 仍按住不能恢復輸入");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return UntilScene("VRSimulation", false);
            Assert.That(Object.FindFirstObjectByType<SimulationPropMenu>().IsExpanded, Is.False);
            Assert.That(Object.FindFirstObjectByType<PlacementController>().IsDragging, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            AssertSingletons();
        }
        [UnityTest]
        public IEnumerator MissingDestinationIsRejectedBeforeCleanup()
        {
            var navigation = Object.FindFirstObjectByType<SceneNavigation>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("目標或 Loading 場景未加入有效場景清單"));
            typeof(SceneNavigation).GetMethod("Request", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(navigation, new object[] { "Assets/SurfaceForge/Scenes/DoesNotExist.unity" });
            yield return Frames(2);
            Assert.That(SceneNavigation.IsTransitioning, Is.False);
            Assert.That(navigation.IsInputReady, Is.True);
            AssertSingletons();
        }
        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int frame = Time.frameCount;
                double deadline = EditorApplication.timeSinceStartup + 10;
                while (frame == Time.frameCount)
                {
                    Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline));
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
            }
        }
        static IEnumerator UntilScene(string name, bool expectLoading = true)
        {
            double deadline = EditorApplication.timeSinceStartup + 30;
            bool sawLoading = false;
            float previous = 0;
            double firstLoading = 0;
            do
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline), $"等待 {name} 超時");
                var loading = Object.FindFirstObjectByType<LoadingController>();
                if (loading)
                {
                    if (!sawLoading) firstLoading = Time.realtimeSinceStartupAsDouble;
                    sawLoading = true;
                    Assert.That(loading.Progress, Is.GreaterThanOrEqualTo(previous));
                    previous = loading.Progress;
                    if (loading.Progress >= 1)
                        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name), "完成前須已啟用正確目標");
                    if (loading.IsFadingOut)
                    {
                        Assert.That(Time.realtimeSinceStartupAsDouble - firstLoading, Is.GreaterThanOrEqualTo(loading.MinimumDisplayDuration));
                        var target = Object.FindFirstObjectByType<SceneNavigation>();
                        Assert.That(target.IsInputReady, Is.False, "淡出期間不能操作目標");
                    }
                }
                Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.LessThanOrEqualTo(1));
                Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.LessThanOrEqualTo(1));
                Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.LessThanOrEqualTo(1));
                Assert.That(Object.FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.LessThanOrEqualTo(1));
                Assert.That(Object.FindObjectsByType<SceneNavigation>(FindObjectsSortMode.None).Length, Is.LessThanOrEqualTo(1), "操作場景不並存");
                EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            } while (SceneNavigation.IsTransitioning || SceneManager.GetActiveScene().name != name ||
                !Object.FindFirstObjectByType<SceneNavigation>().IsInputReady);
            if (expectLoading) Assert.That(sawLoading, Is.True);
            Assert.That(Object.FindFirstObjectByType<LoadingController>(), Is.Null);
            EditorApplication.QueuePlayerLoopUpdate(); yield return null;
        }
        static void AssertSingletons()
        {
            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<BaseInputModule>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled), Is.EqualTo(1));
        }
        [UnityTearDown]
        public IEnumerator ExitRuntime()
        {
            Time.timeScale = timeScale;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            foreach (var device in nativeDevices) if (device != null && device.added) InputSystem.EnableDevice(device);
            Application.runInBackground = background;
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }

    public class LoadingProgressTests
    {
        [Test]
        public void ProgressIsMonotonicTimeLimitedAndRequiresReadiness()
        {
            float progress = 0;
            for (int i = 0; i < 30; i++)
            {
                float elapsed = (i + 1) * .1f;
                float next = LoadingController.NextProgress(progress, i < 4 ? .2f : 1, elapsed, 1.5f, .1f, false);
                Assert.That(next, Is.GreaterThanOrEqualTo(progress));
                Assert.That(next, Is.LessThan(1));
                if (elapsed < 1.5f) Assert.That(next, Is.LessThanOrEqualTo(elapsed / 1.5f + .0001f));
                progress = next;
            }
            Assert.That(LoadingController.NextProgress(progress, .1f, 4, 1.5f, .1f, false), Is.EqualTo(progress), "實際進度降低也不倒退");
            Assert.That(LoadingController.NextProgress(progress, 1, 4, 1.5f, .1f, true), Is.EqualTo(1));
        }
    }
}
