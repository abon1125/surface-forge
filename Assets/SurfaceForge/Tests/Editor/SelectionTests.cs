using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurfaceForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SurfaceForge.Tests
{
    // Main-scene integration, using an isolated Input System test mouse (not desktop input).
    public class SelectionTests
    {
        private Mouse mouse;
        private bool oldBackground;
        private PropSelectionController selection;
        private PropContextMenuView menu;
        private PlacementController placement;
        private Camera camera;
        private Vector2 point;
        private static readonly string[] Names = { "Cube", "Sphere", "Cylinder", "Cone" };

        private static T Field<T>(Object target, string name) where T : Object =>
            (T)new SerializedObject(target).FindProperty(name).objectReferenceValue;

        private static IEnumerator Frames(int count = 3)
        {
            for (var i = 0; i < count; i++)
            {
                var frame = Time.frameCount;
                var deadline = EditorApplication.timeSinceStartup + 10;
                while (Time.frameCount == frame)
                {
                    Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline), "No real PlayerLoop frame");
                    EditorApplication.QueuePlayerLoopUpdate();
                    yield return null;
                }
            }
        }
        private void State(Vector2 position, bool down)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down));
        }
        private IEnumerator Click(Vector2 position)
        {
            State(position, true); yield return Frames();
            State(position, false); yield return Frames();
        }
        private PlacedProp Spawn(string name, Vector2 at)
        {
            var definition = AssetDatabase.LoadAssetAtPath<PropDefinition>($"Assets/SurfaceForge/Data/Props/{name}.asset");
            var query = placement.GetComponent<SurfacePlacementQuery>();
            Assert.That(query.TryGetPose(at, out var pose), Is.True);
            var go = Object.Instantiate(definition.Prefab, pose.position, pose.rotation);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("PlacedProp");
            var prop = go.AddComponent<PlacedProp>(); prop.Initialize(definition);
            Physics.SyncTransforms();
            return prop;
        }
        private Vector2 HitPoint(PlacedProp prop) => camera.WorldToScreenPoint(prop.GetComponentInChildren<Collider>().bounds.center);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/SurfaceForge/Scenes/Main.unity");
            yield return new EnterPlayMode();
            oldBackground = Application.runInBackground; Application.runInBackground = true;
            selection = Object.FindFirstObjectByType<PropSelectionController>();
            placement = Object.FindFirstObjectByType<PlacementController>();
            menu = Object.FindFirstObjectByType<PropContextMenuView>();
            camera = Field<Camera>(selection, "sceneCamera");
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            State(Vector2.zero, false);
            yield return Frames();
            point = camera.pixelRect.center;
        }
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            Application.runInBackground = oldBackground;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AllChildCollidersResolveAndFiftySwitchClearDeleteCycles()
        {
            for (var i = 0; i < 50; i++)
            {
                var first = Spawn(Names[i % 4], point + Vector2.right * 65);
                var second = Spawn(Names[(i + 1) % 4], point + Vector2.left * 65);
                yield return Click(HitPoint(first));
                Assert.That(selection.Selected, Is.SameAs(first));
                Assert.That(menu.Target, Is.SameAs(first));
                Assert.That(Field<Text>(menu,"targetName").text, Is.EqualTo(first.Definition.DisplayName));
                yield return Click(HitPoint(second));
                Assert.That(selection.Selected, Is.SameAs(second));
                yield return Click(camera.pixelRect.min + Vector2.one * 10);
                Assert.That(selection.Selected, Is.Null);
                Assert.That(Field<RectTransform>(menu,"panel").gameObject.activeSelf, Is.False);
                yield return Click(HitPoint(first));
                var button = Field<Button>(menu,"deleteButton");
                // Two UGUI invocations before Destroy is flushed must still delete only the target.
                button.onClick.Invoke(); button.onClick.Invoke();
                Assert.That(selection.Selected, Is.Null);
                yield return Frames();
                Assert.That(first == null, Is.True);
                Assert.That(second != null, Is.True);
                Object.Destroy(second.gameObject);
                yield return Frames();
                Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None), Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator UIAndPhysicalButtonPipelineDoNotPassThroughDeleteOrSidebar()
        {
            var prop = Spawn("Cube",point);
            yield return Click(HitPoint(prop));
            Assert.That(selection.Selected, Is.SameAs(prop));
            var sidebar = Field<RectTransform>(menu,"sidebar");
            yield return Click(RectTransformUtility.WorldToScreenPoint(null,sidebar.position));
            Assert.That(selection.Selected, Is.SameAs(prop));
            var button = Field<Button>(menu,"deleteButton");
            var position = RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            // Put a second collider directly behind the UI button to catch release-through.
            var behind = Spawn("Sphere",position);
            yield return Click(position);
            Assert.That(prop == null, Is.True);
            Assert.That(behind != null, Is.True);
            Assert.That(selection.Selected, Is.Null);
            Assert.That(menu.Target, Is.Null);
        }

        [UnityTest]
        public IEnumerator SameFrameUIDisappearanceCannotSelectBehindIt()
        {
            var prop=Spawn("Cube",point);
            yield return Click(HitPoint(prop));
            var button=Field<Button>(menu,"deleteButton");
            var position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            var behind=Spawn("Sphere",position);
            State(position,true); State(position,false);
            InputSystem.Update(); // Both transitions in one Dynamic input update, before UI hides.
            button.onClick.Invoke();
            yield return Frames();
            Assert.That(prop == null,Is.True);
            Assert.That(behind != null,Is.True);
            Assert.That(selection.Selected,Is.Null);
            Assert.That(menu.Target,Is.Null);
        }

        [UnityTest]
        public IEnumerator BatchedPressMovementReleasePreservesUIOriginAndDragDistance()
        {
            var prop=Spawn("Cube",point); var hit=HitPoint(prop);
            var sidebar=Field<RectTransform>(menu,"sidebar");
            var ui=RectTransformUtility.WorldToScreenPoint(null,sidebar.position);
            State(ui,false); yield return Frames();
            State(ui,true); State(hit,true); State(hit,false);
            yield return Frames();
            Assert.That(selection.Selected,Is.Null,"Batched UI-origin drag selected a prop");
            State(hit,false); yield return Frames();
            State(hit,true); State(hit+Vector2.right*30,true); State(hit,false);
            yield return Frames();
            Assert.That(selection.Selected,Is.Null,"Batched out-and-back movement became a click");
            State(hit,true); State(hit,false);
            yield return Frames();
            Assert.That(selection.Selected,Is.SameAs(prop),"A genuine batched click must still work");
            selection.ClearSelection();
            State(hit,true); State(hit,false); State(hit+Vector2.right*40,false);
            yield return Frames();
            Assert.That(selection.Selected,Is.SameAs(prop),"Movement after release must not move the click");
        }

        [UnityTest]
        public IEnumerator NewSceneDragCancelsOlderDeferredClick()
        {
            var prop=Spawn("Cube",point); var hit=HitPoint(prop);
            State(hit,true); yield return Frames();
            var pendingField=typeof(PropSelectionController).GetField("pending",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var queued=false;
            Canvas.WillRenderCanvases nextGesture=()=>
            {
                // Queue the next gesture after the release LateUpdate, before its deferred
                // click can resolve. Rendering follows LateUpdate; input runs next real frame.
                if(queued || !(bool)pendingField.GetValue(selection)) return;
                queued=true;
                State(hit,true); State(hit+Vector2.right*30,true);
            };
            Canvas.willRenderCanvases+=nextGesture;
            try
            {
                State(hit,false); yield return Frames(6);
                Assert.That(queued,Is.True,"Release candidate was not observed before next frame");
                Assert.That(selection.Selected,Is.Null,"Old deferred click reopened selection during a newer drag");
                Assert.That(menu.Target,Is.Null);
                State(hit+Vector2.right*30,false); yield return Frames();
                Assert.That(selection.Selected,Is.Null); Assert.That(menu.Target,Is.Null);
                State(hit,false); yield return Frames();
                // Also supersede a prior release before it has reached LateUpdate at all.
                State(hit,true); State(hit,false); State(hit,true); State(hit+Vector2.right*30,true);
                yield return Frames();
                Assert.That(selection.Selected,Is.Null,"Same-batch older release survived the newer drag");
                State(hit+Vector2.right*30,false); yield return Frames();
                Assert.That(selection.Selected,Is.Null); Assert.That(menu.Target,Is.Null);
                yield return Click(hit); Assert.That(selection.Selected,Is.SameAs(prop));
            }
            finally { Canvas.willRenderCanvases-=nextGesture; }
        }

        [UnityTest]
        public IEnumerator PlacementReleaseIsSuppressedAndFreshClickWorks()
        {
            var prop = Spawn("Cone",point);
            var hit = HitPoint(prop);
            yield return Click(hit);
            Assert.That(selection.Selected, Is.SameAs(prop));
            State(hit,true); yield return Frames();
            var row = Object.FindObjectsByType<PropItemDragHandler>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
            var definition = prop.Definition;
            Assert.That(placement.Begin(row,definition,hit), Is.True);
            Assert.That(selection.Selected, Is.Null);
            State(hit,false);
            // End before selection LateUpdate; its fallback may run before or after selection.
            placement.End(row,hit);
            yield return Frames();
            Assert.That(selection.Selected, Is.Null);
            Assert.That(menu.Target, Is.Null);
            Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            yield return Click(hit);
            Assert.That(selection.Selected != null, Is.True);
            // Cancelled drag release must also leave no selection.
            State(hit,true); yield return Frames();
            Assert.That(placement.Begin(row,definition,hit), Is.True);
            placement.Cancel(); State(hit,false); yield return Frames();
            Assert.That(selection.Selected, Is.Null);
        }

        [Test]
        public void EventMergingSettingIsScopedAndRestoredAcrossEnableCycles()
        {
            selection.enabled=false;
            var settings=InputSystem.settings;
            var original=settings.disableRedundantEventsMerging;
            try
            {
                settings.disableRedundantEventsMerging=false;
                for(var i=0;i<20;i++)
                {
                    selection.enabled=true;
                    Assert.That(settings.disableRedundantEventsMerging,Is.True);
                    selection.enabled=false;
                    Assert.That(settings.disableRedundantEventsMerging,Is.False);
                }
                settings.disableRedundantEventsMerging=true;
                selection.enabled=true; selection.enabled=false;
                Assert.That(settings.disableRedundantEventsMerging,Is.True);
            }
            finally { settings.disableRedundantEventsMerging=original; selection.enabled=true; }
        }

        [UnityTest]
        public IEnumerator InvalidTargetsFocusAndListenerLifetimeCleanUp()
        {
            var prop = Spawn("Cylinder",point); var hit = HitPoint(prop);
            for (var i=0;i<10;i++)
            {
                yield return Click(hit); Assert.That(selection.Selected, Is.SameAs(prop));
                if (i%4==0) { prop.gameObject.SetActive(false); yield return Frames(); prop.gameObject.SetActive(true); }
                else if (i%4==1) { menu.enabled=false; menu.enabled=true; }
                else if (i%4==2) { selection.enabled=false; selection.enabled=true; }
                else selection.SendMessage("OnApplicationFocus",false);
                Assert.That(selection.Selected, Is.Null); Assert.That(menu.Target, Is.Null);
            }
            var requests=0; System.Action count = () => requests++;
            menu.DeleteRequested += count;
            for(var i=0;i<20;i++) { menu.enabled=false; menu.enabled=true; }
            yield return Click(hit);
            Field<Button>(menu,"deleteButton").onClick.Invoke();
            Assert.That(requests, Is.EqualTo(1));
            menu.DeleteRequested -= count;
            yield return Frames();
            Assert.That(prop == null, Is.True);
            prop=Spawn("Cube",point); yield return Click(HitPoint(prop));
            Object.Destroy(prop.gameObject); yield return Frames();
            Assert.That(selection.Selected, Is.Null); Assert.That(menu.Target, Is.Null);
        }

        [UnityTest]
        public IEnumerator SceneDragAndOtherButtonsCannotSelect()
        {
            var prop=Spawn("Cube",point); var hit=HitPoint(prop);
            yield return Click(hit); Assert.That(selection.Selected,Is.SameAs(prop));
            State(hit,true); yield return Frames(); State(hit+Vector2.right*30,true); yield return Frames();
            Assert.That(selection.Selected,Is.Null);
            State(hit,false); yield return Frames(); Assert.That(selection.Selected, Is.Null);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=hit }.WithButton(MouseButton.Right));
            yield return Frames(); State(hit,false); yield return Frames();
            Assert.That(selection.Selected, Is.Null);
            yield return Click(hit); Assert.That(selection.Selected, Is.SameAs(prop));
        }

        [UnityTest]
        public IEnumerator MenuFollowsClampsAndHidesBehindCamera()
        {
            var prop=Spawn("Sphere",point); yield return Click(HitPoint(prop));
            var panel=Field<RectTransform>(menu,"panel");
            foreach(var uv in new[]{Vector2.zero,Vector2.one,new Vector2(0,1),new Vector2(1,0),new Vector2(.5f,.5f)})
            {
                var p=camera.pixelRect.min+Vector2.Scale(camera.pixelRect.size,new Vector2(Mathf.Lerp(.02f,.98f,uv.x),Mathf.Lerp(.02f,.98f,uv.y)));
                prop.transform.position=camera.ScreenToWorldPoint(new Vector3(p.x,p.y,70));
                yield return Frames();
                Assert.That(panel.gameObject.activeSelf, Is.True);
                var corners=new Vector3[4]; panel.GetWorldCorners(corners);
                var sidebar=Field<RectTransform>(menu,"sidebar"); var sideCorners=new Vector3[4]; sidebar.GetWorldCorners(sideCorners);
                foreach(var corner in corners)
                { Assert.That(corner.x,Is.InRange(0,sideCorners[0].x)); Assert.That(corner.y,Is.InRange(0,Screen.height)); }
            }
            prop.transform.position=camera.transform.position-camera.transform.forward*5;
            yield return Frames(); Assert.That(panel.gameObject.activeSelf,Is.False);
            prop.transform.position=camera.ScreenToWorldPoint(new Vector3(point.x,point.y,70));
            yield return Frames(); Assert.That(panel.gameObject.activeSelf,Is.True);
        }
    }
}
