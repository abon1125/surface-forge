using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurfaceForge.Tests
{
    public class SimulationPlayerTests
    {
        private SimulationPlayer player;
        private CharacterController motor;
        private Mouse mouse;
        private Keyboard keyboard;
        private bool oldBackground;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private T Field<T>(string name) => (T)typeof(SimulationPlayer).GetField(name, Private).GetValue(player);
        private void Call(string method, params object[] args) => typeof(SimulationPlayer).GetMethod(method, Private).Invoke(player, args);
        private static IEnumerator Frames(int count = 3)
        {
            for (int i=0;i<count;i++)
            {
                int frame=Time.frameCount; double deadline=EditorApplication.timeSinceStartup+10;
                while(frame==Time.frameCount)
                {
                    Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline),"等待真正 PlayerLoop 影格逾時");
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
            }
        }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/SurfaceForge/Scenes/VRSimulation.unity");
            yield return new EnterPlayMode();
            oldBackground=Application.runInBackground; Application.runInBackground=true;
            player=Object.FindFirstObjectByType<SimulationPlayer>(); motor=player.GetComponent<CharacterController>();
            mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent(); keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(mouse,new MouseState {position=new Vector2(Screen.width/2f,Screen.height/2f)});
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Frames();
        }
        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            Application.runInBackground=oldBackground;
            yield return new ExitPlayMode();
        }
        private void Teleport(Vector3 point)
        {
            motor.enabled=false;player.transform.position=point;motor.enabled=true;Physics.SyncTransforms();
        }
        [UnityTest]
        public IEnumerator GroundMovementCollisionBoundaryAndPitch()
        {
            // Isolate movement stepping from live keyboard input; invoke the production motor path.
            player.enabled=false;
            var terrain=Field<Terrain>("terrain");
            float initialY=player.transform.position.y;
            for(int i=0;i<50;i++)Call("Move",Vector2.zero,0.02f);
            Assert.That(player.transform.position.y,Is.InRange(initialY-0.2f,initialY+0.1f),"靜止貼地");
            Call("Look",new Vector2(750,100000)); // 90 degree yaw, clamped upward pitch.
            Assert.That(Mathf.DeltaAngle(player.transform.eulerAngles.y,90),Is.EqualTo(0).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(0,Field<Transform>("viewPivot").localEulerAngles.x),Is.EqualTo(-75).Within(0.01f));
            var start=player.transform.position;
            for(int i=0;i<20;i++)Call("Move",Vector2.up,0.02f);
            Assert.That(player.transform.position.x-start.x,Is.GreaterThan(1));
            Assert.That(Mathf.Abs(player.transform.position.z-start.z),Is.LessThan(0.05f),"仰望時仍水平向前");
            Call("Look",new Vector2(0,-100000));
            Assert.That(Mathf.DeltaAngle(0,Field<Transform>("viewPivot").localEulerAngles.x),Is.EqualTo(75).Within(0.01f));
            // Full-height wall stops the capsule, rather than testing only mathematical clamping.
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=player.transform.position+Vector3.right*2+Vector3.up*2;
            wall.transform.localScale=new Vector3(0.5f,8,8); Physics.SyncTransforms();
            float wallX=wall.transform.position.x;
            for(int i=0;i<100;i++)Call("Move",Vector2.up,0.02f);
            Assert.That(player.transform.position.x,Is.LessThan(wallX-0.4f),"膠囊受到實際 Collider 阻擋");
            Object.DestroyImmediate(wall);
            // Four corners: outward diagonal motion and recovery from below/outside terrain.
            var origin=terrain.transform.position;var size=terrain.terrainData.size;
            foreach(var corner in new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)})
            {
                Teleport(origin+new Vector3(corner.x*size.x+(corner.x==0?-10:10),-100,corner.y*size.z+(corner.y==0?-10:10)));
                Call("Move",Vector2.zero,0.02f);
                Vector3 point=player.transform.position;
                Assert.That(point.x,Is.InRange(origin.x+0.37f,origin.x+size.x-0.37f));
                Assert.That(point.z,Is.InRange(origin.z+0.37f,origin.z+size.z-0.37f));
                Assert.That(point.y,Is.GreaterThanOrEqualTo(terrain.SampleHeight(point)+origin.y-0.15f));
                var direction=new Vector3(corner.x==0?-1:1,0,corner.y==0?-1:1);
                player.transform.rotation=Quaternion.LookRotation(direction);
                for(int i=0;i<50;i++)Call("Move",Vector2.up,0.02f);
                Assert.That(Vector2.Distance(new Vector2(point.x,point.z),new Vector2(player.transform.position.x,player.transform.position.z)),Is.LessThan(0.1f),"邊界停止");
            }
            // A dedicated transient height field tests uphill rejection beyond the accepted slope.
            var original=terrain.terrainData;
            var steep=new TerrainData {heightmapResolution=33,size=new Vector3(80,160,64)};
            var heights=new float[33,33];for(int z=0;z<33;z++)for(int x=0;x<33;x++)heights[z,x]=x/32f;
            steep.SetHeights(0,0,heights);terrain.terrainData=steep;terrain.GetComponent<TerrainCollider>().terrainData=steep;
            // The same transient ramp first exercises gentle ascent/descent and a fall under gravity.
            steep.size=new Vector3(80,20,64);
            Teleport(origin+new Vector3(30,15,32));
            for(int i=0;i<100;i++)Call("Move",Vector2.zero,0.02f);
            Assert.That(player.transform.position.y,Is.EqualTo(origin.y+7.5f).Within(0.15f),"重力落到緩坡地面");
            player.transform.rotation=Quaternion.Euler(0,90,0);
            float lowY=player.transform.position.y;
            for(int i=0;i<30;i++)Call("Move",Vector2.up,0.02f);
            Assert.That(player.transform.position.y,Is.GreaterThan(lowY+0.3f),"緩坡上行");
            float highY=player.transform.position.y;
            for(int i=0;i<30;i++)Call("Move",Vector2.down,0.02f);
            Assert.That(player.transform.position.y,Is.LessThan(highY-0.3f),"緩坡下行");
            Assert.That(player.transform.position.y,Is.EqualTo(terrain.SampleHeight(player.transform.position)+origin.y).Within(0.15f),"下坡持續貼地");
            steep.size=new Vector3(80,160,64);
            Teleport(origin+new Vector3(30,65,32));Call("RecoverToTerrain",true);player.transform.rotation=Quaternion.Euler(0,90,0);
            float beforeX=player.transform.position.x;
            for(int i=0;i<50;i++)Call("Move",Vector2.up,0.02f);
            Assert.That(player.transform.position.x,Is.LessThanOrEqualTo(beforeX+0.05f),"陡坡不能向上爬");
            terrain.terrainData=original;terrain.GetComponent<TerrainCollider>().terrainData=original;Object.DestroyImmediate(steep);
            yield return null;
        }
        private void MouseState(bool held, Vector2 delta=default)
        {
            var state=new MouseState {position=new Vector2(Screen.width/2f,Screen.height/2f),delta=delta}.WithButton(MouseButton.Right,held);
            InputSystem.QueueStateEvent(mouse,state);
        }
        [UnityTest]
        public IEnumerator InputAndCursorLifecycle()
        {
            // Focus callbacks are invoked explicitly; this does not claim native OS focus testing.
            // Runtime still requires the real application to be focused before accepting input.
            if(!Application.isFocused) Assert.Ignore("Editor 未聚焦；無法驗證右鍵實際 Update 路徑");
            Call("OnApplicationFocus",true);
            var start=player.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return Frames(10);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames();
            Assert.That(player.transform.position.z-start.z,Is.GreaterThan(0.1f),"W 由 Input System 驅動");
            foreach(var key in new[]{Key.A,Key.S,Key.D})
            {
                start=player.transform.position;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return Frames(8);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames();
                var change=player.transform.position-start;
                Assert.That(key==Key.A ? -change.x : key==Key.S ? -change.z : change.x,Is.GreaterThan(0.05f),key+" 方向");
            }
            MouseState(false);yield return Frames();MouseState(true);yield return Frames();
            Assert.That(player.IsLooking,Is.True);Assert.That(Cursor.visible,Is.False);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.Locked));
            var yaw=player.transform.eulerAngles.y;
            MouseState(true,new Vector2(100,0));yield return Frames();Assert.That(Mathf.DeltaAngle(yaw,player.transform.eulerAngles.y),Is.GreaterThan(0));
            MouseState(false);yield return Frames();AssertCursor();
            MouseState(true);yield return Frames();Call("OnApplicationFocus",false);AssertCursor();
            Call("OnApplicationFocus",true);yield return Frames();Assert.That(player.IsLooking,Is.False,"回焦仍按住不重新鎖定");
            MouseState(false);yield return Frames();MouseState(true);yield return Frames();Assert.That(player.IsLooking,Is.True);
            player.enabled=false;AssertCursor();player.enabled=true;yield return Frames();Assert.That(player.IsLooking,Is.False,"重啟仍按住不重新鎖定");
            MouseState(false);yield return Frames();MouseState(true);yield return Frames();Assert.That(player.IsLooking,Is.True);
            // Actual scene replacement exercises disable/destroy during unloading, no scene menu added.
            var empty=SceneManager.CreateScene("S1TemporaryExit");SceneManager.SetActiveScene(empty);
            var op=SceneManager.UnloadSceneAsync(player.gameObject.scene);while(!op.isDone)yield return null;
            Assert.That(Cursor.visible,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
        }
        private void AssertCursor()
        {
            Assert.That(player.IsLooking,Is.False);Assert.That(Cursor.visible,Is.True);Assert.That(Cursor.lockState,Is.EqualTo(CursorLockMode.None));
        }
    }
}
