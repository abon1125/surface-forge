using DG.Tweening;
using System.Collections;
using System.Linq;
using System.Reflection;
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
    public class SimulationRayTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        SimulationRayInputModule module;SimulationWorldRaycaster raycaster;SimulationPropMenu menu;SimulationPropItem[] items;ScrollRect scroll;Mouse mouse;Keyboard keyboard;bool background;
        static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        static IEnumerator Frames(int count=3)
        {
            for(int i=0;i<count;i++){int frame=Time.frameCount;double deadline=EditorApplication.timeSinceStartup+10;while(frame==Time.frameCount){Assert.That(EditorApplication.timeSinceStartup,Is.LessThan(deadline));EditorApplication.QueuePlayerLoopUpdate();yield return null;}}
        }
        IEnumerator MouseAt(Vector2 point,bool left=false,bool right=false,float wheel=0)
        {
            var state=new MouseState{position=point,scroll=new Vector2(0,wheel)};if(left)state=state.WithButton(MouseButton.Left);if(right)state=state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(mouse,state);yield return Frames();
        }
        Vector2 ScreenAt(Transform t)=>raycaster.ViewCamera.WorldToScreenPoint(t.position);
        IEnumerator Open(){Call(menu,"SetExpanded",true);Call(menu,"OnApplicationFocus",false);Call(menu,"OnApplicationFocus",true);yield return Frames();Assert.That(menu.IsInteractive,Is.True);yield return MouseAt(ScreenAt(items[0].transform));}
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/SurfaceForge/Scenes/VRSimulation.unity");yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;
            var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(type).Focus();
            module=Object.FindFirstObjectByType<SimulationRayInputModule>();raycaster=Object.FindFirstObjectByType<SimulationWorldRaycaster>();menu=Object.FindFirstObjectByType<SimulationPropMenu>();
            items=Object.FindObjectsByType<SimulationPropItem>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderByDescending(i=>((RectTransform)i.transform).anchoredPosition.y).ToArray();scroll=Object.FindFirstObjectByType<ScrollRect>(FindObjectsInactive.Include);
            mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();yield return MouseAt(new Vector2(Screen.width/2f,Screen.height/2f));
        }
        [UnityTearDown] public IEnumerator Teardown(){if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);Application.runInBackground=background;yield return new ExitPlayMode();}
        [UnityTest] public IEnumerator FourDefinitionsAndSinglePressReleaseClick()
        {
            yield return Open();Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<EventSystem>().GetComponents<BaseInputModule>().Count(m=>m.isActiveAndEnabled),Is.EqualTo(1));
            foreach(var item in items)
            {
                var point=ScreenAt(item.transform);yield return MouseAt(point);Assert.That(item.IsHovered,Is.True,"實際路由必須 hover "+item.name);
                yield return MouseAt(point,true);Assert.That(item.IsPressed,Is.True);yield return Frames(8);Assert.That(item.ClickCount,Is.Zero);
                yield return MouseAt(point);Assert.That(item.IsPressed,Is.False);Assert.That(item.ClickCount,Is.EqualTo(1));Assert.That(item.LastClickedDefinition,Is.SameAs(item.GetComponent<PropCatalogItemView>().Definition));yield return Frames(5);Assert.That(item.ClickCount,Is.EqualTo(1));
            }
            Assert.That(items.Select(i=>i.LastClickedDefinition.Id),Is.EqualTo(new[]{"cube","sphere","cylinder","cone"}));
            var p=ScreenAt(items[0].transform);yield return MouseAt(p);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));InputSystem.QueueStateEvent(mouse,new MouseState{position=p});yield return Frames();Assert.That(items[0].ClickCount,Is.EqualTo(2),"同影格按放");
            yield return MouseAt(p,true);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});InputSystem.QueueStateEvent(mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return Frames();Assert.That(items[0].ClickCount,Is.EqualTo(3));Assert.That(module.HasPress,Is.True);yield return MouseAt(p);Assert.That(items[0].ClickCount,Is.EqualTo(4));
            yield return MouseAt(p,true);yield return MouseAt(ScreenAt(items[1].transform),true);yield return MouseAt(ScreenAt(items[1].transform));Assert.That(items[0].ClickCount,Is.EqualTo(4));Assert.That(items[1].ClickCount,Is.EqualTo(1),"移出放開不得點另一項");
            Assert.That(Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None),Is.Empty);Assert.That(Object.FindObjectsByType<PropItemDragHandler>(FindObjectsSortMode.None),Is.Empty);
        }
        [UnityTest] public IEnumerator WorldDistanceOcclusionBackgroundAndNoHit()
        {
            yield return Open();Canvas.ForceUpdateCanvases();
            Vector2 screen=ScreenAt(items[0].transform);Ray reference=raycaster.ViewCamera.ScreenPointToRay(screen);Ray actual=raycaster.Aim(screen);
            Assert.That(Vector3.Distance(actual.origin,reference.origin),Is.GreaterThan(0.2f));var ui=raycaster.Query(actual);Assert.That(ui.UI,Is.EqualTo(items[0].gameObject));
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="S3 runtime occluder";blocker.transform.position=actual.GetPoint(ui.Distance*0.45f);blocker.transform.localScale=Vector3.one*0.025f;Physics.SyncTransforms();
            Assert.That(raycaster.Query(reference).UI,Is.EqualTo(items[0].gameObject),"攝影機參考仍命中 UI");
            var hit=raycaster.Query(actual);Assert.That(hit.HasHit,Is.True);Assert.That(hit.UI,Is.Null);Assert.That(hit.Distance,Is.LessThan(ui.Distance));
            yield return MouseAt(screen);Assert.That(module.CurrentHit.UI,Is.Null);Assert.That(items[0].IsHovered,Is.False);Assert.That(Vector3.Distance(Field<Transform>(module,"hitMarker").position,module.CurrentHit.Point),Is.LessThan(0.001f));
            Object.Destroy(blocker);yield return Frames();
            var canvas=items[0].GetComponentInParent<Canvas>();Vector3 blank=canvas.transform.TransformPoint(new Vector3(178,155,0));var bg=raycaster.Query(new Ray(raycaster.RayOrigin.position,(blank-raycaster.RayOrigin.position).normalized));Assert.That(bg.UI,Is.EqualTo(canvas.gameObject),"空白背景仍阻擋");
            var none=raycaster.Query(new Ray(raycaster.RayOrigin.position,Vector3.up));Assert.That(none.HasHit,Is.False);Assert.That(none.Distance,Is.EqualTo(raycaster.MaxDistance));
            yield return MouseAt(new Vector2(Screen.width*0.95f,Screen.height*0.95f));Assert.That(module.CurrentHit.HasHit,Is.False);Assert.That(Field<Transform>(module,"hitMarker").gameObject.activeSelf,Is.False);
            Assert.That(Vector3.Distance(Field<LineRenderer>(module,"rayLine").GetPosition(1),module.CurrentHit.Point),Is.LessThan(0.001f));
        }
        [UnityTest] public IEnumerator WheelOnlyVisibleUnblockedViewportAndAnimationGate()
        {
            yield return Open();var viewport=scroll.viewport;viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,144);Canvas.ForceUpdateCanvases();yield return Frames();
            var point=ScreenAt(items[0].transform);yield return MouseAt(point);float initial=scroll.content.anchoredPosition.y;yield return MouseAt(point,wheel:-120);Assert.That(scroll.content.anchoredPosition.y,Is.GreaterThan(initial));
            yield return MouseAt(point);float after=scroll.content.anchoredPosition.y;var canvas=viewport.GetComponentInParent<Canvas>();var title=ScreenAt(canvas.transform.Find("Title"));yield return MouseAt(title,wheel:-120);Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(after).Within(0.01f));
            yield return MouseAt(point,right:true,wheel:-120);Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(after).Within(0.01f));yield return MouseAt(point);
            Ray ray=raycaster.Aim(point);var hit=raycaster.Query(ray);var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=ray.GetPoint(hit.Distance*0.5f);blocker.transform.localScale=Vector3.one*0.1f;Physics.SyncTransforms();yield return MouseAt(point,wheel:-120);Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(after).Within(0.01f));Object.Destroy(blocker);yield return Frames();
            Call(menu,"SetExpanded",false);Assert.That(menu.IsInteractive,Is.False);yield return MouseAt(point,wheel:-120);Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(after).Within(0.01f));
            Call(menu,"OnApplicationFocus",false);Call(menu,"OnApplicationFocus",true);Call(menu,"SetExpanded",true);var tween=Field<DG.Tweening.Tween>(menu,"transition");tween.Pause().Goto(0.04f);yield return Frames();Assert.That(menu.IsInteractive,Is.False);yield return MouseAt(point,true,wheel:-120);Assert.That(module.HasPress,Is.False);Assert.That(scroll.content.anchoredPosition.y,Is.EqualTo(after).Within(0.01f));
            Call(menu,"OnApplicationFocus",false);Call(menu,"OnApplicationFocus",true);yield return Frames();Assert.That(menu.IsInteractive,Is.True);yield return MouseAt(point,true);Assert.That(module.HasPress,Is.False,"動畫中按住不能接管");yield return MouseAt(point);yield return MouseAt(point,true);Assert.That(module.HasPress,Is.True);
        }
        [UnityTest] public IEnumerator InterruptionsReleaseGuardFollowAndReload()
        {
            yield return Open();var point=ScreenAt(items[0].transform);
            for(int mode=0;mode<6;mode++)
            {
                yield return MouseAt(point);yield return MouseAt(point,true);Assert.That(module.HasPress,Is.True,"before interruption "+mode);
                if(mode==0){Call(module,"OnApplicationFocus",false);Call(module,"OnApplicationFocus",true);}
                if(mode==1){Call(module,"OnApplicationPause",true);Call(module,"OnApplicationPause",false);}
                if(mode==2){module.enabled=false;module.enabled=true;}
                if(mode==3){yield return MouseAt(new Vector2(-5,-5),true);}
                if(mode==4){Call(menu,"SetExpanded",false);yield return Frames();Call(menu,"SetExpanded",true);Call(menu,"OnApplicationFocus",false);Call(menu,"OnApplicationFocus",true);}
                if(mode==5){yield return MouseAt(point,true,true);yield return MouseAt(point,true);}
                Assert.That(module.HasPress,Is.False,"interrupt clears press "+mode);yield return MouseAt(point,true);Assert.That(module.HasPress,Is.False,"held cannot reacquire "+mode);yield return MouseAt(point);Assert.That(items[0].ClickCount,Is.Zero);
            }
            var player=Object.FindFirstObjectByType<SimulationPlayer>();player.transform.position+=new Vector3(1,0,1);player.transform.rotation=Quaternion.Euler(0,40,0);player.transform.Find("ViewPivot").localRotation=Quaternion.Euler(-30,0,0);yield return Frames();point=ScreenAt(items[0].transform);yield return MouseAt(point);Assert.That(Vector3.Distance(module.CurrentRay.origin,raycaster.RayOrigin.position),Is.LessThan(0.0001f));Assert.That(module.CurrentHit.UI,Is.EqualTo(items[0].gameObject));
            yield return MouseAt(point,true);var scene=menu.gameObject.scene;var empty=UnityEngine.SceneManagement.SceneManager.CreateScene("S3Exit");UnityEngine.SceneManagement.SceneManager.SetActiveScene(empty);var unload=UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);while(!unload.isDone)yield return null;Assert.That(module==null,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
            var load=EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/SurfaceForge/Scenes/VRSimulation.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));while(!load.isDone)yield return null;yield return Frames();Assert.That(Object.FindFirstObjectByType<SimulationPropMenu>().IsExpanded,Is.False);Assert.That(Object.FindFirstObjectByType<SimulationRayInputModule>().HasPress,Is.False);
        }
    }
}
