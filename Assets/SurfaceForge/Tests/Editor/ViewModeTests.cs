using System.Collections;
using NUnit.Framework;
using SurfaceForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SurfaceForge.Tests
{
    public class ViewModeTests
    {
        private ViewModeController mode;
        private Camera camera;
        private Mouse mouse;
        private Keyboard keyboard;
        private bool background;
        private static T Field<T>(Object target, string name) where T : Object =>
            (T)new SerializedObject(target).FindProperty(name).objectReferenceValue;
        private static IEnumerator Frames(int count = 2)
        {
            for (int i = 0; i < count; i++)
            {
                int frame = Time.frameCount;
                double deadline = EditorApplication.timeSinceStartup + 10;
                while (Time.frameCount == frame)
                {
                    Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline), "Real PlayerLoop stalled");
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
            }
        }
        private IEnumerator Settled()
        {
            double deadline = EditorApplication.timeSinceStartup + 10;
            while (mode.IsTransitioning)
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline));
                yield return Frames(1);
            }
            yield return Frames();
        }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/SurfaceForge/Scenes/Main.unity");
            yield return new EnterPlayMode();
            background = Application.runInBackground; Application.runInBackground = true;
            mode = Object.FindFirstObjectByType<ViewModeController>();
            camera = Field<Camera>(mode, "sceneCamera");
            var so = new SerializedObject(mode); so.FindProperty("transitionSeconds").floatValue = 0.02f; so.ApplyModifiedPropertiesWithoutUndo();
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return Frames();
        }
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            Application.runInBackground = background;
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator FiftyRoundTripsRejectReentryAndPreservePropsAndCatalog()
        {
            var catalog = Object.FindFirstObjectByType<PropCatalogView>(); catalog.SetExpanded(true);
            int rows = Object.FindObjectsByType<PropCatalogItemView>(FindObjectsSortMode.None).Length;
            var definition = AssetDatabase.LoadAssetAtPath<PropDefinition>("Assets/SurfaceForge/Data/Props/Cube.asset");
            var prop = Object.Instantiate(definition.Prefab, new Vector3(40,20,30), Quaternion.Euler(5,17,8));
            prop.AddComponent<PlacedProp>().Initialize(definition);
            var position = prop.transform.position; var rotation = prop.transform.rotation;
            var parent = camera.transform.parent; var pose = camera.transform.localPosition;
            var group = Field<CanvasGroup>(mode,"sidebarGroup");
            var merging = InputSystem.settings.disableRedundantEventsMerging;
            for (int i = 0; i < 50; i++)
            {
                Assert.That(mode.TryToggle(), Is.True); Assert.That(mode.TryToggle(), Is.False);
                Assert.That(camera.transform.parent, Is.SameAs(mode.Player.HeadAnchor));
                Assert.That(camera.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(camera.orthographic, Is.False); Assert.That(camera.rect, Is.EqualTo(new Rect(0,0,1,1)));
                Assert.That(group.blocksRaycasts || group.interactable, Is.False);
                yield return Settled();
                Assert.That(Object.FindObjectsByType<SimulatedPlayer>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                var sidebar = Field<RectTransform>(mode,"sidebar");
                var corners = new Vector3[4]; sidebar.GetWorldCorners(corners);
                Assert.That(corners[0].x, Is.GreaterThan(Screen.width));
                Assert.That(mode.TryToggle(), Is.True); Assert.That(mode.TryToggle(), Is.False);
                Assert.That(camera.transform.parent, Is.SameAs(parent));
                Assert.That(camera.transform.localPosition, Is.EqualTo(pose));
                Assert.That(camera.gameObject.activeInHierarchy, Is.True);
                yield return Settled();
                Assert.That(Object.FindObjectsByType<SimulatedPlayer>(FindObjectsSortMode.None), Is.Empty);
                Assert.That(camera.orthographic, Is.True); Assert.That(group.blocksRaycasts && group.interactable, Is.True);
                Assert.That(prop.transform.position, Is.EqualTo(position)); Assert.That(prop.transform.rotation, Is.EqualTo(rotation));
                Assert.That(prop.GetComponent<PlacedProp>().Definition, Is.SameAs(definition));
                Assert.That(prop.GetComponentInChildren<Collider>().enabled, Is.True);
                Assert.That(catalog.IsExpanded, Is.True);
                Assert.That(Object.FindObjectsByType<PropCatalogItemView>(FindObjectsSortMode.None).Length, Is.EqualTo(rows));
                Assert.That(InputSystem.settings.disableRedundantEventsMerging, Is.EqualTo(merging));
                Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            }
        }
        [UnityTest]
        public IEnumerator VKeyDeviceSwitchesAndHeldGestureCannotResumeOnReturn()
        {
            var placement = Field<PlacementController>(mode,"placement");
            var catalog = Object.FindFirstObjectByType<PropCatalogView>(); catalog.SetExpanded(true); yield return Frames();
            var row = Object.FindFirstObjectByType<PropCatalogItemView>();
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left)); yield return Frames();
            Assert.That(placement.Begin(row.GetComponent<PropItemDragHandler>(),row.Definition,camera.pixelRect.center), Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.V)); yield return Frames();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Settled();
            Assert.That(mode.IsSimulating, Is.True); Assert.That(placement.IsDragging, Is.False);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.V)); yield return Frames();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Settled();
            Assert.That(mode.IsSimulating, Is.False); Assert.That(placement.enabled, Is.False);
            InputSystem.QueueStateEvent(mouse,new MouseState()); yield return Frames(4);
            Assert.That(placement.enabled, Is.True);
            Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Field<PropSelectionController>(mode,"selection").Selected, Is.Null);
            Assert.That(placement.Begin(row.GetComponent<PropItemDragHandler>(),row.Definition,camera.pixelRect.center), Is.True);
            placement.Cancel();
        }
        [UnityTest]
        public IEnumerator FocusAndDisableInterruptBothTransitionsAndRestoreCameraBeforeDestruction()
        {
            for (int i = 0; i < 8; i++)
            {
                Assert.That(mode.TryToggle(), Is.True);
                if (i % 2 == 0) { yield return Settled(); Assert.That(mode.TryToggle(), Is.True); }
                if (i % 4 < 2) mode.SendMessage("OnApplicationFocus",false);
                else { mode.enabled = false; mode.enabled = true; }
                yield return Frames(3);
                Assert.That(mode.IsSimulating || mode.IsTransitioning, Is.False);
                Assert.That(mode.Player == null, Is.True);
                Assert.That(camera && camera.gameObject.activeInHierarchy, Is.True);
                Assert.That(camera.transform.parent, Is.Null);
                Assert.That(camera.orthographic, Is.True);
                Assert.That(Field<TerrainOverviewCamera>(mode,"overview").enabled, Is.True);
                Assert.That(Field<InputSystemUIInputModule>(mode,"uiInput").enabled, Is.True);
                Assert.That(Object.FindObjectsByType<SimulatedPlayer>(FindObjectsSortMode.None), Is.Empty);
            }
        }
        [UnityTest]
        public IEnumerator FourPropsRecoverPlacementSelectionAndDeletionAfterSelectedSwitch()
        {
            var placement = Field<PlacementController>(mode,"placement");
            var selection = Field<PropSelectionController>(mode,"selection");
            var menu = Object.FindFirstObjectByType<PropContextMenuView>();
            foreach (var name in new[] { "Cube", "Sphere", "Cylinder", "Cone" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<PropDefinition>($"Assets/SurfaceForge/Data/Props/{name}.asset");
                var point = camera.pixelRect.center;
                Assert.That(placement.Begin(null,definition,point),Is.True);
                placement.End(null,point); yield return Frames();
                var props = Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None);
                Assert.That(props.Length,Is.EqualTo(1));
                var prop = props[0];
                Vector2 hit = camera.WorldToScreenPoint(prop.GetComponentInChildren<Collider>().bounds.center);
                InputSystem.QueueStateEvent(mouse,new MouseState { position=hit }.WithButton(MouseButton.Left)); yield return Frames();
                InputSystem.QueueStateEvent(mouse,new MouseState { position=hit }); yield return Frames(4);
                Assert.That(selection.Selected,Is.SameAs(prop));
                Assert.That(mode.TryToggle(),Is.True);
                Assert.That(selection.Selected,Is.Null); Assert.That(menu.Target,Is.Null);
                yield return Settled();
                Assert.That(mode.TryToggle(),Is.True); yield return Settled();
                // A new cancelled placement must not delete the retained prop or resurrect selection.
                Assert.That(placement.Begin(null,definition,point),Is.True); placement.Cancel(); yield return Frames();
                Assert.That(selection.Selected,Is.Null);
                hit = camera.WorldToScreenPoint(prop.GetComponentInChildren<Collider>().bounds.center);
                InputSystem.QueueStateEvent(mouse,new MouseState { position=hit }.WithButton(MouseButton.Left)); yield return Frames();
                InputSystem.QueueStateEvent(mouse,new MouseState { position=hit }); yield return Frames(4);
                Assert.That(selection.Selected,Is.SameAs(prop));
                Field<Button>(menu,"deleteButton").onClick.Invoke(); yield return Frames();
                Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None),Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator DisabledControllerWaitsForHeldPointerReleaseWithoutSubscriptionLeak()
        {
            var placement = Field<PlacementController>(mode,"placement");
            var ui = Field<InputSystemUIInputModule>(mode,"uiInput");
            for (int i=0;i<5;i++)
            {
                bool ran = false, held = false, accepted = false;
                System.Action callback = null;
                callback = () =>
                {
                    if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
                    InputSystem.onAfterUpdate -= callback;
                    ran = true; held = mouse.leftButton.isPressed;
                    accepted = mode.TryToggle();
                    mode.enabled = false;
                };
                InputSystem.onAfterUpdate += callback;
                InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
                yield return Frames();
                InputSystem.onAfterUpdate -= callback;
                Assert.That(ran && held && accepted, Is.True, "Switch inside real Dynamic input, not Editor state buffers");
                Assert.That(placement.enabled || ui.enabled,Is.False);
                Assert.That(camera.orthographic,Is.True);
                InputSystem.QueueStateEvent(mouse,new MouseState()); yield return Frames(4);
                Assert.That(placement.enabled && ui.enabled,Is.True);
                Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None),Is.Empty);
                mode.enabled=true;
            }
        }
    }
}
