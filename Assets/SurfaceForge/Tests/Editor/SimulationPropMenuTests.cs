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
using DG.Tweening;

namespace SurfaceForge.Tests
{
    public class SimulationPropMenuTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private SimulationPropMenu menu;
        private Keyboard keyboard;
        private bool background;
        private T Field<T>(string name)=>(T)typeof(SimulationPropMenu).GetField(name,Private).GetValue(menu);
        private void Call(string name,params object[] args)=>typeof(SimulationPropMenu).GetMethod(name,Private).Invoke(menu,args);
        private static IEnumerator Frames(int count=3)
        {
            for(int i=0;i<count;i++)
            {
                int frame=Time.frameCount;double deadline=EditorApplication.timeSinceStartup+10;
                while(frame==Time.frameCount)
                {
                    Assert.That(EditorApplication.timeSinceStartup,Is.LessThan(deadline));
                    EditorApplication.QueuePlayerLoopUpdate();yield return null;
                }
            }
        }
        private IEnumerator Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return Frames();
        }
        private static IEnumerator AnimateFor(float seconds)
        {
            float until=Time.realtimeSinceStartup+seconds;
            while(Time.realtimeSinceStartup<until)yield return Frames(1);
        }
        [UnityTest]
        public IEnumerator SameFramePressReleaseAndRepressKeepInputEdges()
        {
            if(!Application.isFocused)Assert.Ignore("需要 Editor 聚焦才能測試正式 Update 輸入事件");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();Assert.That(menu.IsExpanded,Is.True,"同影格 Tab 按放仍切換");
            yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.False);
            yield return Frames(10);Assert.That(menu.IsExpanded,Is.False,"持續按住不重複切換");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            yield return Frames();Assert.That(menu.IsExpanded,Is.True,"同影格放開再按下仍切換");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();Assert.That(menu.IsExpanded,Is.False,"同影格 Esc 按放仍關閉");
            Call("OnApplicationFocus",false);Call("OnApplicationFocus",true);
            yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.False,"回焦仍按住必須先放開");
            yield return Keys();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Frames();Assert.That(menu.IsExpanded,Is.True,"放開防護解除後短按有效");
        }
        [UnityTest]
        public IEnumerator AnimationReversalAndInterruptedCleanup()
        {
            var panel=Field<GameObject>("panel");var group=panel.GetComponent<CanvasGroup>();
            Call("SetExpanded",true);
            // 控制 Tween 時間，避免低幀率跨過短動畫的終點。
            Field<Tween>("transition").Pause().Goto(0.06f);
            Call("LateUpdate");
            float before=Field<float>("progress");Assert.That(before,Is.InRange(0.01f,0.99f));
            Assert.That(group.alpha,Is.InRange(0.01f,0.99f));
            Call("SetExpanded",false);Assert.That(Field<float>("progress"),Is.EqualTo(before));
            Assert.That(panel.activeSelf,Is.True,"關閉動畫完成前保持 Canvas 有效");
            Call("SetExpanded",true);yield return AnimateFor(0.35f);
            Assert.That(group.alpha,Is.EqualTo(1).Within(0.001f));Assert.That(Field<object>("transition"),Is.Null);
            Assert.That(panel.transform.localPosition.magnitude,Is.LessThan(0.001f));
            Call("SetExpanded",false);Call("OnApplicationFocus",false);
            Assert.That(panel.activeSelf,Is.False);Assert.That(Field<object>("transition"),Is.Null);
            Call("OnApplicationFocus",true);Call("SetExpanded",true);menu.enabled=false;
            Assert.That(menu.IsExpanded,Is.False);Assert.That(Field<object>("transition"),Is.Null);
            menu.enabled=true;Call("SetExpanded",true);yield return AnimateFor(0.35f);
            Call("SetExpanded",false);yield return AnimateFor(0.3f);
            Assert.That(panel.activeSelf,Is.False);Assert.That(group.alpha,Is.Zero);Assert.That(Field<object>("transition"),Is.Null);
        }
        [UnityTest]
        public IEnumerator AnimatedFollowAndSceneUnloadOwnOnlyItsTween()
        {
            Call("SetExpanded",true);
            var owned=Field<Tween>("transition");owned.Pause().Goto(0.08f);
            var player=Object.FindFirstObjectByType<SimulationPlayer>();
            player.enabled=false;
            var motor=player.GetComponent<CharacterController>();motor.enabled=false;
            player.transform.position+=new Vector3(2,0,1);player.transform.rotation=Quaternion.Euler(0,100,0);
            player.transform.Find("ViewPivot").localRotation=Quaternion.Euler(-50,0,0);
            Call("LateUpdate");
            var panel=Field<GameObject>("panel");float p=Field<float>("progress");
            var left=menu.transform.parent;
            Assert.That(Vector3.Distance(panel.transform.position,Vector3.Lerp(left.position,menu.transform.position,p)),Is.LessThan(0.001f));
            Assert.That(panel.transform.lossyScale.x,Is.EqualTo(0.001f*Mathf.Lerp(0.15f,1,p)).Within(0.00001f));
            Assert.That(panel.GetComponent<CanvasGroup>().alpha,Is.EqualTo(p).Within(0.001f));
            float unrelatedValue=0;
            var unrelated=DOTween.To(()=>unrelatedValue,v=>unrelatedValue=v,1,10).Pause();
            try
            {
                var scene=menu.gameObject.scene;
                var empty=UnityEngine.SceneManagement.SceneManager.CreateScene("S2AnimationExit");
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(empty);
                var unload=UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                while(!unload.isDone)yield return null;
                Assert.That(owned.IsActive(),Is.False,"場景卸載清理自己的 Tween");
                Assert.That(unrelated.IsActive(),Is.True,"不清除其他系統 Tween");
                Assert.That(Cursor.visible,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
            }
            finally {unrelated.Kill();}
        }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/SurfaceForge/Scenes/VRSimulation.unity");yield return new EnterPlayMode();
            background=Application.runInBackground;Application.runInBackground=true;
            menu=Object.FindFirstObjectByType<SimulationPropMenu>();Assert.That(menu,Is.Not.Null);
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();yield return Keys();
        }
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);Application.runInBackground=background;
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator KeyEdgesLifecycleAndNoDuplicateItems()
        {
            Assert.That(menu.IsExpanded,Is.False);
            if(!Application.isFocused)Assert.Ignore("需要 Editor 聚焦才能測試正式 Update 輸入路徑");
            yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.True);
            yield return Frames(12);Assert.That(menu.IsExpanded,Is.True,"按住不反覆切換");
            var panel=Field<GameObject>("panel");
            var items=panel.GetComponentsInChildren<PropCatalogItemView>(true);
            Assert.That(items.Length,Is.EqualTo(4));
            Assert.That(items.Select(i=>i.Definition.Id),Is.EquivalentTo(new[]{"cube","sphere","cylinder","cone"}));
            foreach(var item in items)
            {
                Assert.That(item.GetComponentInChildren<Text>().text,Is.EqualTo(item.Definition.DisplayName));
                Assert.That(item.GetComponentsInChildren<Image>().Any(image=>image.sprite==item.Definition.Icon),Is.True);
                Assert.That(item.Definition.Prefab,Is.Not.Null);
            }
            Assert.That(panel.GetComponentsInChildren<PropItemDragHandler>(true),Is.Empty);
            Assert.That(panel.GetComponentsInChildren<GraphicRaycaster>(true),Is.Empty);
            Assert.That(panel.GetComponentsInChildren<SimulationPropItem>(true).Length,Is.EqualTo(4));
            var ids=items.Select(i=>i.GetInstanceID()).ToArray();
            for(int i=0;i<12;i++){yield return Keys();yield return Keys(Key.Tab);}
            Assert.That(menu.IsExpanded,Is.True);
            Assert.That(panel.GetComponentsInChildren<PropCatalogItemView>(true).Select(i=>i.GetInstanceID()),Is.EqualTo(ids));
            yield return Keys(Key.Escape);Assert.That(menu.IsExpanded,Is.False);
            yield return Keys();yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.True);
            Call("OnApplicationFocus",false);Call("OnApplicationFocus",true);yield return Frames();
            Assert.That(menu.IsExpanded,Is.True,"回焦仍按住 Tab 不切換");
            yield return Keys();yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.False);
            yield return Keys();yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.True);
            menu.enabled=false;Assert.That(menu.IsExpanded,Is.False);menu.enabled=true;yield return Frames();
            Assert.That(menu.IsExpanded,Is.False,"重啟仍按住不開啟");
            yield return Keys();yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.True);
            Call("OnApplicationPause",true);Call("OnApplicationPause",false);yield return Frames();Assert.That(menu.IsExpanded,Is.True);
            var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
            try
            {
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width/2f,Screen.height/2f)});yield return Frames();
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width/2f,Screen.height/2f)}.WithButton(MouseButton.Right));yield return Frames();
                var player=Object.FindFirstObjectByType<SimulationPlayer>();Assert.That(player.IsLooking,Is.True);
                yield return Keys();yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.False);
                Assert.That(player.IsLooking,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.Locked));Assert.That(Cursor.visible,Is.False);
                yield return Keys();yield return Keys(Key.Tab);Assert.That(menu.IsExpanded,Is.True);
                InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(100,20)}.WithButton(MouseButton.Right));yield return Frames();
                Assert.That(player.IsLooking,Is.True);Assert.That(menu.IsExpanded,Is.True);
                InputSystem.QueueStateEvent(mouse,new MouseState());yield return Frames();
                Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));Assert.That(Cursor.visible,Is.True);
            }
            finally {InputSystem.RemoveDevice(mouse);}
        }
        [UnityTest]
        public IEnumerator FollowsAnchorAcrossMovementAndViewAndReload()
        {
            var player=Object.FindFirstObjectByType<SimulationPlayer>();var root=player.transform;
            var pivot=root.Find("ViewPivot");var left=pivot.Find("LeftController");
            foreach(float pitch in new[]{-75f,0f,75f})
            {
                root.rotation=Quaternion.Euler(0,123,0);pivot.localRotation=Quaternion.Euler(pitch,0,0);
                var motor=player.GetComponent<CharacterController>();motor.enabled=false;root.position+=new Vector3(1,0,1);motor.enabled=true;
                yield return Frames();
                Assert.That(Vector3.Distance(menu.transform.position,left.position+left.rotation*Field<Vector3>("anchorOffset")),Is.LessThan(0.001f));
                Assert.That(Vector3.Dot(menu.transform.forward,(menu.transform.position-Field<Transform>("viewer").position).normalized),Is.GreaterThan(0.999f));
                Assert.That(Field<GameObject>("panel").transform.lossyScale.x,Is.EqualTo(0.001f).Within(0.00001f));
            }
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled),Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            var terrain=Object.FindFirstObjectByType<Terrain>();Assert.That(terrain.terrainData,Is.SameAs(terrain.GetComponent<TerrainCollider>().terrainData));
            Assert.That(terrain.gameObject.layer,Is.EqualTo(LayerMask.NameToLayer("Surface")));
            var reload=EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/SurfaceForge/Scenes/VRSimulation.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            while(!reload.isDone)yield return null;
            yield return Frames();
            menu=Object.FindFirstObjectByType<SimulationPropMenu>();Assert.That(menu.IsExpanded,Is.False);
        }
    }
}
