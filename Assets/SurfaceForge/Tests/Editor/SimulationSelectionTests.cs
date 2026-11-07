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
    public class SimulationSelectionTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        SimulationSelectionController selection;
        SimulationContextMenuView context;
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
            selection = Object.FindFirstObjectByType<SimulationSelectionController>();
            context = Object.FindFirstObjectByType<SimulationContextMenuView>();
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

        PlacedProp Spawn(int index, float x = 0, float distance = 2.5f)
        {
            var definition = AssetDatabase.LoadAssetAtPath<PropDefinition>("Assets/SurfaceForge/Data/Props/" + new[] {"Cube","Sphere","Cylinder","Cone"}[index] + ".asset");
            var camera = raycaster.ViewCamera;
            var position = camera.transform.position + camera.transform.forward * distance + camera.transform.right * x - Vector3.up * .45f;
            var go = Object.Instantiate(definition.Prefab, position, Quaternion.identity);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("PlacedProp");
            var prop = go.AddComponent<PlacedProp>(); prop.Initialize(definition); Physics.SyncTransforms(); return prop;
        }
        Vector2 HitPoint(PlacedProp prop) => raycaster.ViewCamera.WorldToScreenPoint(prop.GetComponentInChildren<Collider>().bounds.center);
        Vector2 EmptyScreen()
        {
            for(float y=.2f;y<.95f;y+=.1f) for(float x=.1f;x<.95f;x+=.1f)
            {
                var p=new Vector2(Screen.width*x,Screen.height*y);var hit=raycaster.Query(raycaster.Aim(p));
                if(!hit.UI && !PropSelectionState.Resolve(hit.Collider)) return p;
            }
            Assert.Fail("需要未被道具／UI 遮住的空白點");return default;
        }
        IEnumerator Click(Vector2 p) { yield return MouseAt(p,true); yield return MouseAt(p); }
        void State(Vector2 p,bool down) => InputSystem.QueueStateEvent(mouse,new MouseState {position=p}.WithButton(MouseButton.Left,down));
        [UnityTest] public IEnumerator FourKindsSwitchClearDeleteSingleInstanceAndRepeatPlacement()
        {
            for(int index=0;index<4;index++)
            {
                var prop=Spawn(index);var other=Spawn(index,-1.8f);
                yield return Click(HitPoint(prop));
                Assert.That(selection.Selected,Is.SameAs(prop),"hit="+module.CurrentHit.Collider+" UI="+module.CurrentHit.UI+" ray="+module.CurrentRay+" p="+HitPoint(prop)+" camera="+raycaster.ViewCamera.transform.position);Assert.That(context.Target,Is.SameAs(prop));Assert.That(context.IsVisible,Is.True);
                Assert.That(Field<Text>(context,"targetName").text,Is.EqualTo(prop.Definition.DisplayName));
                yield return Click(HitPoint(other));Assert.That(selection.Selected,Is.SameAs(other));
                yield return Click(EmptyScreen());Assert.That(selection.Selected,Is.Null,"hit="+module.CurrentHit.Collider+" UI="+module.CurrentHit.UI);
                yield return Click(HitPoint(prop));
                var button=Field<Button>(context,"deleteButton");
                yield return Click(ScreenAt(button.transform));
                Assert.That(selection.Selected,Is.Null);Assert.That(context.Target,Is.Null);Assert.That(context.IsVisible,Is.False);
                yield return Frames();Assert.That(prop==null,Is.True);Assert.That(other,Is.Not.Null);
                button.onClick.Invoke();Assert.That(other.gameObject.activeSelf,Is.True);
                Object.Destroy(other.gameObject);yield return Frames();
            }
            yield return Open();yield return Drag();yield return MouseAt(GroundScreen());Clean();
            Assert.That(Count,Is.EqualTo(1));Assert.That(selection.Selected,Is.Null);
            var placed=Object.FindFirstObjectByType<PlacedProp>();yield return Click(HitPoint(placed));Assert.That(selection.Selected,Is.SameAs(placed));
        }
        [UnityTest] public IEnumerator OrderedClicksDragUIOriginAndInterruptGuards()
        {
            var prop=Spawn(0);var p=HitPoint(prop);
            State(p,true);State(p,false);yield return Frames();Assert.That(selection.Selected,Is.SameAs(prop));
            selection.ClearSelection();
            State(p,true);State(p+Vector2.right*30,true);State(p,true);State(p,false);
            yield return Frames();Assert.That(selection.Selected,Is.Null,"移出再返回仍為拖動");
            yield return MouseAt(p,true);yield return Frames(5);Assert.That(selection.Selected,Is.Null);
            yield return MouseAt(p);Assert.That(selection.Selected,Is.SameAs(prop));
            State(p,true);State(p,false);State(p,true);yield return Frames();
            Assert.That(selection.Selected,Is.SameAs(prop));yield return MouseAt(p);
            yield return Open();selection.ClearSelection();var ui=ScreenAt(items[0].transform);
            State(ui,true);State(p,true);State(p,false);yield return Frames();Assert.That(selection.Selected,Is.Null,"UI 起始不得轉成場景點擊");
            for(int mode=0;mode<8;mode++)
            {
                yield return Click(p);Assert.That(selection.Selected,Is.SameAs(prop));yield return MouseAt(p,true);
                if(mode==0){Call(module,"OnApplicationFocus",false);Call(module,"OnApplicationFocus",true);}
                if(mode==1){Call(module,"OnApplicationPause",true);Call(module,"OnApplicationPause",false);}
                if(mode==2){module.enabled=false;module.enabled=true;}
                if(mode==3){selection.enabled=false;selection.enabled=true;}
                if(mode==4){context.enabled=false;context.enabled=true;}
                if(mode==5){player.enabled=false;player.enabled=true;}
                if(mode==6){raycaster.enabled=false;raycaster.enabled=true;}
                if(mode==7){yield return MouseAt(new Vector2(-5,-5),true);}
                yield return MouseAt(p,true);Assert.That(selection.Selected,Is.Null,"中斷 "+mode);
                yield return MouseAt(p);Assert.That(selection.Selected,Is.Null,"中斷後放開 "+mode);
            }
            yield return Click(p);Object.Destroy(prop.gameObject);yield return Frames();
            Assert.That(selection.Selected,Is.Null);Assert.That(context.Target,Is.Null);Assert.That(context.IsVisible,Is.False);
        }
        [UnityTest] public IEnumerator RightLookPreservesSelectionAndCancelsPendingDelete()
        {
            var prop=Spawn(0);yield return Click(HitPoint(prop));
            var button=Field<Button>(context,"deleteButton");var point=ScreenAt(button.transform);
            yield return MouseAt(point,true);Assert.That(module.HasPress,Is.True);
            yield return MouseAt(point,true,true);
            Assert.That(selection.Selected,Is.SameAs(prop));Assert.That(context.IsVisible,Is.True);
            Assert.That(module.HasPress,Is.False);
            yield return MouseAt(point,true);yield return MouseAt(point);
            Assert.That(prop.gameObject.activeSelf,Is.True,"觀看前的按下不能在觀看後放開刪除");
            Assert.That(selection.Selected,Is.SameAs(prop));
            // 同一批次短按右鍵，也必須保留選取並取消這一批後續左鍵。
            InputSystem.QueueStateEvent(mouse,new MouseState {position=point}.WithButton(MouseButton.Right));
            InputSystem.QueueStateEvent(mouse,new MouseState {position=point});
            State(point,true);State(point,false);yield return Frames();
            Assert.That(selection.Selected,Is.SameAs(prop));Assert.That(prop.gameObject.activeSelf,Is.True);
            yield return MouseAt(point);yield return Click(ScreenAt(button.transform));
            Assert.That(selection.Selected,Is.Null);yield return Frames();Assert.That(prop==null,Is.True);
        }
        [UnityTest] public IEnumerator WorldRayForegroundAndMultipleCanvasOcclusion()
        {
            var prop=Spawn(1);var p=HitPoint(prop);
            var ray=raycaster.Aim(p);var hit=raycaster.Query(ray);
            Assert.That(PropSelectionState.Resolve(hit.Collider),Is.SameAs(prop),"hit="+hit.Collider+" UI="+hit.UI+" ray="+ray+" prop="+prop.transform.position+" camera="+raycaster.ViewCamera.transform.position+" screen="+p);
            Assert.That(hit.Collider.transform,Is.Not.SameAs(prop.transform));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=ray.GetPoint(.5f);wall.transform.localScale=Vector3.one*.08f;Physics.SyncTransforms();
            Assert.That(raycaster.Query(ray).Collider.gameObject,Is.SameAs(wall));
            yield return Click(p);Assert.That(selection.Selected,Is.Null);
            Object.Destroy(wall);yield return Frames();yield return Click(p);Assert.That(selection.Selected,Is.SameAs(prop));
            var canvas=Field<Canvas>(context,"canvas");var button=Field<Button>(context,"deleteButton");
            Vector3 buttonPoint=button.transform.position;
            var direct=new Ray(raycaster.RayOrigin.position,(buttonPoint-raycaster.RayOrigin.position).normalized);
            Assert.That(raycaster.Query(direct).UI,Is.EqualTo(button.gameObject));
            button.interactable=false;Assert.That(raycaster.Query(direct).UI,Is.Not.Null,"停用按鈕仍阻擋");button.interactable=true;
            Vector3 background=canvas.transform.TransformPoint(new Vector3(-122,48,0));
            Assert.That(raycaster.Query(new Ray(raycaster.RayOrigin.position,(background-raycaster.RayOrigin.position).normalized)).UI,Is.EqualTo(canvas.gameObject));
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=direct.GetPoint(.5f);wall.transform.localScale=Vector3.one*.08f;Physics.SyncTransforms();
            Assert.That(raycaster.Query(direct).UI,Is.Null);Object.Destroy(wall);yield return Frames();
            // 兩份正式 Canvas 置於同一直線的不同距離，測試統一最近命中。
            yield return Open();var left=Field<Canvas>(raycaster,"menuCanvas");
            var leftMenu=left.transform.parent.GetComponent<SimulationPropMenu>();leftMenu.enabled=false;
            context.enabled=false;module.enabled=false;selection.enabled=false;left.gameObject.SetActive(true);canvas.gameObject.SetActive(true);
            var contextGroup=canvas.GetComponent<CanvasGroup>();contextGroup.alpha=1;contextGroup.blocksRaycasts=true;contextGroup.interactable=true;
            var group=left.GetComponent<CanvasGroup>();group.blocksRaycasts=true;group.alpha=1;
            var camera=raycaster.ViewCamera;var origin=camera.transform.position;
            var direction=camera.transform.forward;
            left.transform.SetPositionAndRotation(origin+direction*1,camera.transform.rotation);left.transform.localScale=Vector3.one*.001f;
            canvas.transform.SetPositionAndRotation(origin+direction*2,camera.transform.rotation);canvas.transform.localScale=Vector3.one*.002f;
            Canvas.ForceUpdateCanvases();yield return Frames();Physics.SyncTransforms();var testRay=new Ray(origin,direction);
            Assert.That(raycaster.Query(testRay).UI.transform.IsChildOf(left.transform),Is.True);
            canvas.transform.position=origin+direction*.5f;Canvas.ForceUpdateCanvases();
            Assert.That(raycaster.Query(testRay).UI.transform.IsChildOf(canvas.transform),Is.True);
            canvas.gameObject.SetActive(false);left.gameObject.SetActive(false);
            Assert.That(raycaster.Query(testRay).UI,Is.Null,"隱藏後沒有幽靈 UI");
        }
        [UnityTest] public IEnumerator MenuVisibilityTargetInvalidationAndPlacementReleaseConsumption()
        {
            var prop=Spawn(0);yield return Click(HitPoint(prop));Assert.That(context.IsVisible,Is.True);
            var original=prop.transform.position;var camera=raycaster.ViewCamera;
            var deleteButton=Field<Button>(context,"deleteButton");var deletePoint=ScreenAt(deleteButton.transform);
            yield return MouseAt(deletePoint,true);Assert.That(module.HasPress,Is.True);
            context.gameObject.SetActive(false);
            yield return MouseAt(deletePoint,true);Assert.That(module.HasPress,Is.False,"選單隱藏取消舊按下");
            context.gameObject.SetActive(true);yield return Frames();yield return MouseAt(deletePoint);
            Assert.That(prop.gameObject.activeSelf,Is.True,"重新可見後不能沿用舊按下刪除");
            // 距離與視野不再停用選單；自然裁切不等於 Canvas 被停用。
            foreach(var position in new[]{camera.transform.position-camera.transform.forward*3,camera.transform.position+camera.transform.forward*20})
            {prop.transform.position=position;Physics.SyncTransforms();selection.Select(prop);Assert.That(context.IsVisible,Is.True);}
            prop.transform.position=original;Physics.SyncTransforms();selection.Select(prop);yield return Frames();
            prop.enabled=false;yield return Frames();Assert.That(selection.Selected,Is.Null);Assert.That(context.Target,Is.Null);prop.enabled=true;
            yield return Click(HitPoint(prop));yield return Open();yield return Drag();Assert.That(selection.Selected,Is.Null);
            yield return MouseAt(GroundScreen());Assert.That(selection.Selected,Is.Null);Clean();
            yield return Drag();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return Frames();
            yield return MouseAt(HitPoint(prop));Assert.That(selection.Selected,Is.Null);Clean();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames();
            yield return Click(HitPoint(prop));Assert.That(selection.Selected,Is.SameAs(prop));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return Frames();Assert.That(selection.Selected,Is.Null);
        }

        [UnityTest] public IEnumerator DeleteButtonOrderedBatchesAndNoReleaseThrough()
        {
            var prop=Spawn(0);var other=Spawn(0,-1.5f);
            yield return Click(HitPoint(prop));var button=Field<Button>(context,"deleteButton");
            Vector2 deletePoint=ScreenAt(button.transform);var empty=EmptyScreen();
            State(empty,true);State(deletePoint,true);State(deletePoint,false);yield return Frames();
            Assert.That(prop.gameObject.activeSelf,Is.True,"場景按下不會取得 DELETE");Assert.That(selection.Selected,Is.SameAs(prop));
            State(deletePoint,true);State(deletePoint+Vector2.right*30,true);State(deletePoint,true);State(deletePoint,false);yield return Frames();
            Assert.That(prop.gameObject.activeSelf,Is.True,"刪除按鈕拖動不算點擊");
            State(deletePoint,true);State(deletePoint,false);State(HitPoint(other),true);State(HitPoint(other),false);yield return Frames();
            Assert.That(prop==null,Is.True);Assert.That(other.gameObject.activeSelf,Is.True);Assert.That(selection.Selected,Is.Null);
            Assert.That(context.Target,Is.Null);Assert.That(module.HasPress,Is.False);
            yield return Click(HitPoint(other));Assert.That(selection.Selected,Is.SameAs(other));
            yield return MouseAt(HitPoint(other),true);other.enabled=false;other.enabled=true;
            yield return MouseAt(HitPoint(other));Assert.That(selection.Selected,Is.Null,"同影格失效／重啟清除舊按下");
            yield return Click(HitPoint(other));Assert.That(selection.Selected,Is.SameAs(other));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return Frames();
            Assert.That(selection.Selected,Is.SameAs(other),"Tab 保留完成的選取");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames();
            var oldScene=other.gameObject.scene;
            var emptyScene=UnityEngine.SceneManagement.SceneManager.CreateScene("S5Exit");UnityEngine.SceneManagement.SceneManager.SetActiveScene(emptyScene);
            var unload=UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(oldScene);while(!unload.isDone)yield return null;
            Assert.That(selection==null,Is.True);Assert.That(context==null,Is.True);Assert.That(module==null,Is.True);
            var load=EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/SurfaceForge/Scenes/VRSimulation.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while(!load.isDone)yield return null;yield return Frames();
            Assert.That(Object.FindFirstObjectByType<SimulationSelectionController>().Selected,Is.Null);
            Assert.That(Object.FindFirstObjectByType<SimulationContextMenuView>().Target,Is.Null);
        }
        [UnityTest] public IEnumerator TiltedPropMenuOrbitsItsLocalYAxis()
        {
            var prop = Spawn(0);
            prop.transform.rotation = Quaternion.Euler(25, 35, 20);
            foreach (var collider in prop.GetComponentsInChildren<Collider>()) collider.isTrigger = true;
            Physics.SyncTransforms();
            var camera = raycaster.ViewCamera;
            var original = camera.transform.position;
            var rect = (RectTransform)Field<Canvas>(context,"canvas").transform;
            var axis = prop.transform.up;
            var center = prop.transform.TransformPoint(new Vector3(0,.75f,0));
            float expectedRadius = Mathf.Sqrt(2 * .75f * .75f) + .15f;
            for (int angle = 0; angle < 360; angle += 45)
            {
                var direction = Quaternion.AngleAxis(angle, axis) * prop.transform.forward;
                camera.transform.position = center + direction * 5 + axis * 2;
                selection.Select(prop); Call(context,"RefreshPosition");
                var offset = rect.position - center;
                Assert.That(Vector3.Dot(offset, axis), Is.EqualTo(.45f).Within(.001f));
                Assert.That(Vector3.Distance(Vector3.ProjectOnPlane(offset, axis), direction * expectedRadius), Is.LessThan(.001f));
            }
            camera.transform.position = original; selection.ClearSelection(); Object.Destroy(prop.gameObject);
            yield return Frames();
        }

        [UnityTest] public IEnumerator OrbitMenuStaysOnViewerSideWithStableHeightAndSize()
        {
            var camera=raycaster.ViewCamera;var rect=(RectTransform)Field<Canvas>(context,"canvas").transform;
            var originalPosition=camera.transform.position;var originalRotation=camera.transform.rotation;
            float originalAspect=camera.aspect;
            for(int kind=0;kind<4;kind++)
            foreach(float scale in new[]{.5f,1f,2f})
            {
                var prop=Spawn(kind);prop.transform.localScale=Vector3.one*scale;
                foreach(var collider in prop.GetComponentsInChildren<Collider>())collider.isTrigger=true;
                Physics.SyncTransforms();var bounds=prop.GetComponentInChildren<Collider>().bounds;
                float extent=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                float radius=new Vector2(bounds.extents.x,bounds.extents.z).magnitude+extent*.1f;
                foreach(float aspect in new[]{16f/9,4f/3,1720f/720})
                foreach(float distance in new[]{.15f,3f,20f})
                for(int angle=0;angle<360;angle+=45)
                {
                    var toward=Quaternion.Euler(0,angle,0)*Vector3.forward;
                    camera.aspect=aspect;camera.transform.position=bounds.center+toward*distance+Vector3.up*2;
                    selection.Select(prop);Call(context,"RefreshPosition");
                    var expected=bounds.center+Vector3.up*(bounds.size.y*.3f)+toward*radius;
                    Assert.That(context.IsVisible,Is.True);
                    Assert.That(Vector3.Distance(rect.position,expected),Is.LessThan(.0001f));
                    Assert.That(rect.rect.width*rect.lossyScale.x,Is.EqualTo(extent*.5f).Within(.0001f));
                    Assert.That(Vector3.Dot(rect.forward,(rect.position-camera.transform.position).normalized),Is.GreaterThan(.999f));
                }
                // 正上方沿用最後有效方向；切換目標後不沿用另一物件的方向。
                var previous=rect.position;camera.transform.position=bounds.center+Vector3.up*5;
                Call(context,"RefreshPosition");Assert.That(Vector3.Distance(rect.position,previous),Is.LessThan(.0001f));
                context.Hide();camera.transform.rotation=Quaternion.identity;context.Show(prop);
                Assert.That(Vector3.Dot((rect.position-bounds.center).normalized,Vector3.back),Is.GreaterThan(0));
                typeof(SimulationContextMenuView).GetField("offsetRatio",Private).SetValue(context,new Vector3(.2f,1,.3f));
                typeof(SimulationContextMenuView).GetField("widthRatio",Private).SetValue(context,.75f);
                typeof(SimulationContextMenuView).GetField("orbitGapRatio",Private).SetValue(context,.2f);
                camera.transform.position=bounds.center+Vector3.forward*5;Call(context,"RefreshPosition");
                var custom=bounds.center+Vector3.up*bounds.size.y+Vector3.right*(bounds.size.x*.2f)
                    +Vector3.forward*(radius+extent*.1f+bounds.size.z*.3f);
                Assert.That(Vector3.Distance(rect.position,custom),Is.LessThan(.0001f));
                Assert.That(rect.rect.width*rect.lossyScale.x,Is.EqualTo(extent*.75f).Within(.0001f));
                typeof(SimulationContextMenuView).GetField("offsetRatio",Private).SetValue(context,new Vector3(0,.3f,0));
                typeof(SimulationContextMenuView).GetField("widthRatio",Private).SetValue(context,.5f);
                typeof(SimulationContextMenuView).GetField("orbitGapRatio",Private).SetValue(context,.1f);
                selection.ClearSelection();Object.DestroyImmediate(prop.gameObject);
            }
            camera.aspect=originalAspect;camera.transform.SetPositionAndRotation(originalPosition,originalRotation);
            yield return Frames();
        }
    }
}
