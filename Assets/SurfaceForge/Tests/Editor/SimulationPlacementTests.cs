using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SurfaceForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SurfaceForge.Tests
{
    public class SimulationPlacementTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        SimulationRayInputModule module;
        SimulationWorldRaycaster raycaster;
        SimulationPropMenu menu;
        PlacementController placement;
        PlacementGuideView guide;
        SurfacePlacementQuery query;
        SimulationPlayer player;
        SimulationPropItem[] items;
        Mouse mouse;
        Keyboard keyboard;
        bool background;
        InputDevice[] nativeDevices;
        static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
        static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        int Count => Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None).Length;
        Vector2 ScreenAt(Transform t) => raycaster.ViewCamera.WorldToScreenPoint(t.position);
        static IEnumerator Frames(int count = 3)
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
        IEnumerator MouseAt(Vector2 point, bool left = false, bool right = false, float wheel = 0)
        {
            var state = new MouseState { position = point, scroll = new Vector2(0, wheel) };
            if (left) state = state.WithButton(MouseButton.Left);
            if (right) state = state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(mouse, state); yield return Frames();
        }
        IEnumerator Open()
        {
            Call(menu, "SetExpanded", true);
            Call(menu, "OnApplicationFocus", false); Call(menu, "OnApplicationFocus", true);
            yield return MouseAt(ScreenAt(items[0].transform));
            Assert.That(menu.IsInteractive, Is.True);
        }
        Vector2 GroundScreen()
        {
            for (float y = .45f; y > .15f; y -= .05f)
                for (float x = .75f; x < .95f; x += .05f)
                {
                    Vector2 p = new Vector2(Screen.width * x, Screen.height * y);
                    Ray ray = raycaster.Aim(p);
                    if (!raycaster.Query(ray).UI && query.TryGetPose(ray, out _)) return p;
                }
            Assert.Fail("目前視野必須有合法地形"); return default;
        }
        IEnumerator Drag(int index = 0)
        {
            yield return MouseAt(ScreenAt(items[index].transform));
            yield return MouseAt(ScreenAt(items[index].transform), true);
            Assert.That(module.HasPress, Is.True, "press: UI=" + module.CurrentHit.UI + " interactive=" + menu.IsInteractive + " looking=" + player.IsLooking);
            yield return MouseAt(GroundScreen(), true);
            Assert.That(module.OwnsDrag, Is.True);
            Assert.That(placement.IsDragging, Is.True);
        }
        void Clean()
        {
            Assert.That(placement.IsDragging, Is.False);
            Assert.That(module.OwnsDrag, Is.False);
            Assert.That(module.HasPress, Is.False);
            Assert.That(guide.IsVisible, Is.False);
            Assert.That(player.PlacementOwnsInput, Is.False);
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/SurfaceForge/Scenes/VRSimulation.unity");
            yield return new EnterPlayMode();
            background = Application.runInBackground; Application.runInBackground = true;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            module = Object.FindFirstObjectByType<SimulationRayInputModule>();
            raycaster = Object.FindFirstObjectByType<SimulationWorldRaycaster>();
            menu = Object.FindFirstObjectByType<SimulationPropMenu>();
            placement = Object.FindFirstObjectByType<PlacementController>();
            query = Object.FindFirstObjectByType<SurfacePlacementQuery>();
            guide = Object.FindFirstObjectByType<PlacementGuideView>();
            player = Object.FindFirstObjectByType<SimulationPlayer>();
            items = Object.FindObjectsByType<SimulationPropItem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderByDescending(i => ((RectTransform)i.transform).anchoredPosition.y).ToArray();
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            nativeDevices = InputSystem.devices.Where(d => d.enabled && (d is Mouse || d is Keyboard) && d != mouse && d != keyboard).ToArray();
            foreach (var device in nativeDevices) InputSystem.DisableDevice(device);
            yield return MouseAt(new Vector2(Screen.width / 2f, Screen.height / 2f));
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if (placement) placement.Cancel();
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            foreach (var device in nativeDevices) InputSystem.EnableDevice(device);
            Application.runInBackground = background;
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator WorldQueryFourPropsTerrainRegionsAndReleaseRecheck()
        {
            yield return Open();
            var terrain = Object.FindFirstObjectByType<Terrain>();
            var data = terrain.terrainData;
            Assert.That(terrain.GetComponent<TerrainCollider>().terrainData, Is.SameAs(data));
            Assert.That(terrain.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Surface")));
            // 查詢真實高度場的高／低／緩坡與邊界樣本，而非只驗算 PoseAt。
            float low = float.MaxValue, high = float.MinValue;
            Vector2 lowUV = default, highUV = default, slopeUV = default;
            float slope = 0;
            for (int y = 1; y < 20; y++) for (int x = 1; x < 20; x++)
            {
                var uv = new Vector2(x / 20f, y / 20f);
                float height = data.GetInterpolatedHeight(uv.x, uv.y);
                if (height < low) { low = height; lowUV = uv; }
                if (height > high) { high = height; highUV = uv; }
                float angle = Vector3.Angle(data.GetInterpolatedNormal(uv.x, uv.y), Vector3.up);
                if (angle > slope && angle < 35) { slope = angle; slopeUV = uv; }
            }
            Assert.That(high - low, Is.GreaterThan(1)); Assert.That(slope, Is.GreaterThan(1));
            foreach (var item in items)
            {
                var definition = item.GetComponent<PropCatalogItemView>().Definition;
                foreach (var uv in new[] { lowUV, highUV, slopeUV, new Vector2(.001f, .999f) })
                {
                    var origin = terrain.transform.position + new Vector3(uv.x * data.size.x, data.size.y + 5, uv.y * data.size.z);
                    var ray = new Ray(origin, Vector3.down);
                    Assert.That(query.TryGetPose(ray, out var pose), Is.True);
                    Assert.That(placement.BeginWorld(thisOwner, definition, ray, false), Is.True);
                    Assert.That(placement.BeginWorld(module, definition, ray, false), Is.False);
                    var preview = placement.Preview;
                    Assert.That(Vector3.Distance(preview.position, pose.position), Is.LessThan(.001f));
                    Assert.That(Quaternion.Angle(preview.rotation, pose.rotation), Is.LessThan(.01f));
                    Assert.That(Vector3.Angle(preview.up, pose.rotation * Vector3.up), Is.LessThan(.01f));
                    Assert.That(preview.Find("Model").localPosition.y, Is.EqualTo(.75f).Within(.001f));
                    foreach (var c in preview.GetComponentsInChildren<Collider>()) Assert.That(c.enabled, Is.False);
                    int before = Count;
                    placement.EndWorld(module, ray, false); Assert.That(placement.IsDragging, Is.True, "非擁有者不能提交");
                    placement.EndWorld(thisOwner, ray, false); placement.EndWorld(thisOwner, ray, false);
                    Assert.That(Count, Is.EqualTo(before + 1));
                    var placed = Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None).Single(p => Vector3.Distance(p.transform.position, pose.position) < .001f && p.Definition == definition);
                    Assert.That(Quaternion.Angle(placed.transform.rotation, pose.rotation), Is.LessThan(.01f));
                    Assert.That(placed.GetComponentsInChildren<Transform>().All(t => t.gameObject.layer == LayerMask.NameToLayer("PlacedProp")), Is.True);
                }
            }
            var validRay = new Ray(terrain.transform.position + new Vector3(5, data.size.y + 5, 5), Vector3.down);
            var outside = new Ray(terrain.transform.position + new Vector3(-1, data.size.y + 5, -1), Vector3.down);
            Assert.That(query.TryGetPose(outside, out _), Is.False);
            int total = Count;
            foreach (int mode in new[] { 0, 1, 2 })
            {
                Assert.That(placement.BeginWorld(thisOwner, items[0].GetComponent<PropCatalogItemView>().Definition, validRay, false), Is.True);
                if (mode == 0) placement.EndWorld(thisOwner, outside, false);
                if (mode == 1) placement.EndWorld(thisOwner, validRay, true);
                if (mode == 2)
                {
                    terrain.GetComponent<TerrainCollider>().enabled = false;
                    placement.EndWorld(thisOwner, validRay, false);
                    terrain.GetComponent<TerrainCollider>().enabled = true;
                }
                Assert.That(Count, Is.EqualTo(total)); Assert.That(placement.IsDragging, Is.False);
            }
            yield return Frames();
        }
        Object thisOwner => query;

        [UnityTest] public IEnumerator RoutedRepeatOwnershipGuideUIAndInvalidRelease()
        {
            yield return Open();
            var ids = items.Select(i => i.GetInstanceID()).ToArray();
            for (int index = 0; index < 8; index++)
            {
                yield return Drag(index % 4);
                Assert.That(guide.IsVisible, Is.True, "guide: valid=" + placement.HasValidPose + " UI=" + module.CurrentHit.UI + " ref=" + Field<PlacementGuideView>(module,"placementGuide") + " pose=" + placement.PreviewPose.position);
                Assert.That(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "Placement Preview"), Is.EqualTo(1));
                var pose = placement.PreviewPose;
                var line = Field<LineRenderer>(guide, "guide");
                Assert.That(Vector3.Distance(line.GetPosition(1), pose.position + pose.rotation * Vector3.up * .015f), Is.LessThan(.001f));
                Assert.That(Field<LineRenderer>(guide, "ring").enabled, Is.True);
                int before = Count;
                yield return MouseAt(GroundScreen());
                Assert.That(Count, Is.EqualTo(before + 1)); Clean();
                Assert.That(placement.LastConsumedFrame, Is.GreaterThanOrEqualTo(0));
                Assert.That(Field<LineRenderer>(module, "rayLine").enabled, Is.True);
            }
            Assert.That(items.Select(i => i.GetInstanceID()), Is.EqualTo(ids));
            Assert.That(items.All(i => i.ClickCount == 0), Is.True);
            yield return Drag();
            var other = ScreenAt(items[1].transform);
            yield return MouseAt(other, true, wheel: -120);
            Assert.That(module.OwnsDrag, Is.True); Assert.That(module.HasPress, Is.False);
            Assert.That(placement.Preview.gameObject.activeSelf, Is.False); Assert.That(guide.IsVisible, Is.False);
            int count = Count;
            yield return MouseAt(other); Clean(); Assert.That(Count, Is.EqualTo(count));
            yield return Drag();
            var sky = new Vector2(Screen.width * .95f, Screen.height * .95f);
            yield return MouseAt(sky, true);
            Assert.That(placement.HasValidPose, Is.False); Assert.That(guide.IsVisible, Is.False);
            Assert.That(placement.Preview.gameObject.activeSelf, Is.True);
            Assert.That(placement.Preview.GetComponentInChildren<Renderer>().sharedMaterial.name, Does.Contain("Invalid"));
            yield return MouseAt(sky); Clean(); Assert.That(Count, Is.EqualTo(count));
        }

        [UnityTest] public IEnumerator ThresholdClickWheelAndLookExclusion()
        {
            yield return Open();
            Vector2 point = ScreenAt(items[0].transform);
            yield return MouseAt(point, true);
            yield return MouseAt(point + Vector2.right * 2, true);
            Assert.That(placement.IsDragging, Is.False);
            yield return MouseAt(point); Assert.That(items[0].ClickCount, Is.EqualTo(1), "UI=" + module.CurrentHit.UI + " drag=" + module.OwnsDrag + " interactive=" + menu.IsInteractive);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return Frames(); Assert.That(items[0].ClickCount, Is.EqualTo(2)); Assert.That(Count, Is.Zero);
            yield return Drag();
            var scroll = Object.FindFirstObjectByType<ScrollRect>();
            scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 144);
            Canvas.ForceUpdateCanvases(); yield return Frames();
            float before = scroll.content.anchoredPosition.y;
            yield return MouseAt(ScreenAt(items[0].transform), true, true, -120);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(before).Within(.01f));
            Assert.That(player.IsLooking, Is.False); Assert.That(module.OwnsDrag, Is.True);
            var ground = GroundScreen();
            yield return MouseAt(ground, true, true); yield return MouseAt(ground, false, true);
            Clean(); Assert.That(Count, Is.EqualTo(1)); Assert.That(player.IsLooking, Is.False);
            yield return MouseAt(ground, right: true); Assert.That(player.IsLooking, Is.False, "結束後仍按住右鍵不能接管");
            yield return MouseAt(ground); yield return MouseAt(ground, right: true); Assert.That(player.IsLooking, Is.True);
            yield return MouseAt(ground); Assert.That(player.IsLooking, Is.False);
        }

        [UnityTest] public IEnumerator CancellationGuardsAndSceneUnload()
        {
            yield return Open();
            for (int mode = 0; mode < 11; mode++)
            {
                yield return Drag(); var ground = GroundScreen();
                if (mode == 0) { Call(module, "OnApplicationFocus", false); Call(module, "OnApplicationFocus", true); }
                if (mode == 1) { Call(module, "OnApplicationPause", true); Call(module, "OnApplicationPause", false); }
                if (mode == 2) { module.enabled = false; module.enabled = true; }
                if (mode == 3) { items[0].enabled = false; items[0].enabled = true; }
                if (mode == 4) { placement.enabled = false; placement.enabled = true; }
                if (mode == 5) { yield return MouseAt(new Vector2(-5, -5), true); }
                if (mode == 6)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return Frames();
                    Assert.That(menu.IsExpanded, Is.True, "Esc 優先取消拖曳");
                }
                if (mode == 7)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab)); yield return Frames();
                    Assert.That(menu.IsExpanded, Is.False);
                }
                if (mode == 8) { player.enabled = false; yield return Frames(); player.enabled = true; }
                if (mode == 9) { raycaster.enabled = false; yield return Frames(); raycaster.enabled = true; }
                if (mode == 10) { menu.enabled = false; menu.enabled = true; }
                yield return Frames(); Clean(); Assert.That(Count, Is.Zero, "取消模式 " + mode);
                yield return MouseAt(ground, true); Assert.That(module.OwnsDrag, Is.False);
                yield return MouseAt(ground); Assert.That(Count, Is.Zero);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return Frames(); yield return Open();
            }
            yield return Drag();
            var oldScene = menu.gameObject.scene;
            var empty = UnityEngine.SceneManagement.SceneManager.CreateScene("S4Exit");
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(empty);
            var unload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(oldScene);
            while (!unload.isDone) yield return null;
            Assert.That(module == null, Is.True); Assert.That(guide == null, Is.True);
            Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/SurfaceForge/Scenes/VRSimulation.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return Frames();
            Assert.That(Object.FindFirstObjectByType<SimulationPropMenu>().IsExpanded, Is.False);
            Assert.That(Object.FindFirstObjectByType<PlacementController>().IsDragging, Is.False);
            Assert.That(Object.FindFirstObjectByType<PlacementGuideView>().IsVisible, Is.False);
        }
    }
}
