using System.Collections;
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
    public class PlacementTests
    {
        private GameObject host, cameraObject, terrainObject, uiObject, sourceObject;
        private TerrainData terrainData;
        private PlacementController controller;
        private SurfacePlacementQuery query;
        private PropDefinition definition;
        private PropItemDragHandler source;
        private Material validMaterial;
        private Mouse testMouse;
        private bool previousRunInBackground;

        private static IEnumerator NextPlayerFrame()
        {
            var frame = Time.frameCount;
            var deadline = EditorApplication.timeSinceStartup + 10;
            while (Time.frameCount == frame)
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline), "Player loop did not advance");
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
        }

        private Vector2 Centre => new Vector2(Screen.width / 2f, Screen.height / 2f);
        private int Count => Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None).Length;

        private static void SetReference(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Listed under EditMode, but exercise actual runtime callbacks in an isolated scene.
        [UnitySetUp]
        public IEnumerator EnterRuntime()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            host = new GameObject("test placement");
            cameraObject = new GameObject("test camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(5, 20, 5), Quaternion.Euler(90, 0, 0));
            camera.orthographic = true;
            camera.orthographicSize = 8;
            terrainData = new TerrainData { heightmapResolution = 33, size = new Vector3(10, 3, 10) };
            terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.layer = LayerMask.NameToLayer("Surface");
            query = host.AddComponent<SurfacePlacementQuery>();
            SetReference(query, "sceneCamera", camera);
            SetReference(query, "surface", terrainObject.GetComponent<TerrainCollider>());
            var serialized = new SerializedObject(query);
            serialized.FindProperty("surfaceMask").intValue = 1 << terrainObject.layer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            controller = host.AddComponent<PlacementController>();
            SetReference(controller, "surfaceQuery", query);
            validMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SurfaceForge/Materials/PreviewValid.mat");
            SetReference(controller, "validPreviewMaterial", validMaterial);
            SetReference(controller, "invalidPreviewMaterial",
                AssetDatabase.LoadAssetAtPath<Material>("Assets/SurfaceForge/Materials/PreviewInvalid.mat"));
            uiObject = new GameObject("test events", typeof(EventSystem));
            sourceObject = new GameObject("test source", typeof(RectTransform),
                typeof(PropCatalogItemView), typeof(PropItemDragHandler));
            source = sourceObject.GetComponent<PropItemDragHandler>();
            source.Initialize(controller, null);
            definition = AssetDatabase.LoadAssetAtPath<PropDefinition>("Assets/SurfaceForge/Data/Props/Cube.asset");
            var icon = new GameObject("icon", typeof(RectTransform), typeof(Image));
            var label = new GameObject("label", typeof(RectTransform), typeof(Text));
            icon.transform.SetParent(sourceObject.transform, false);
            label.transform.SetParent(sourceObject.transform, false);
            var view = sourceObject.GetComponent<PropCatalogItemView>();
            SetReference(view, "icon", icon.GetComponent<Image>());
            SetReference(view, "label", label.GetComponent<Text>());
            view.Bind(definition);
            testMouse = InputSystem.AddDevice<Mouse>();
            testMouse.MakeCurrent();
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = Centre });
            InputSystem.Update();
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator LeaveRuntime()
        {
            if (controller) controller.Cancel();
            if (testMouse != null) InputSystem.RemoveDevice(testMouse);
            Application.runInBackground = previousRunInBackground;
            foreach (var placed in Object.FindObjectsByType<PlacedProp>(FindObjectsSortMode.None))
                Object.Destroy(placed.gameObject);
            foreach (var go in new[] { host, cameraObject, terrainObject, uiObject, sourceObject })
                Object.Destroy(go);
            Object.Destroy(terrainData);
            yield return null;
            yield return new ExitPlayMode();
        }

        [Test]
        public void PosePreservesAnchorAndAlignsUpOnSlope()
        {
            var point = new Vector3(7, 4, -2);
            var normal = new Vector3(.3f, 1, .6f).normalized;
            var pose = SurfacePlacementQuery.PoseAt(point, normal);
            Assert.That(pose.position, Is.EqualTo(point));
            Assert.That(Vector3.Angle(pose.rotation * Vector3.up, normal), Is.LessThan(.01f));
        }

        [Test]
        public void ReleaseRequeriesSurfaceAndNeverUsesOldValidPose()
        {
            Assert.That(query.TryGetPose(Centre, out _), Is.True);
            Assert.That(controller.Begin(source, definition, Centre), Is.True);
            terrainObject.SetActive(false);
            controller.End(source, Centre);
            Assert.That(Count, Is.Zero);
            Assert.That(controller.IsDragging, Is.False);
        }

        [Test]
        public void CommitExactlyOnceWithDataAndColliderThenCanRestart()
        {
            Assert.That(controller.Begin(source, definition, Centre), Is.True);
            var preview = GameObject.Find("Placement Preview");
            var previewPose = new Pose(preview.transform.position, preview.transform.rotation);
            foreach (var collider in preview.GetComponentsInChildren<Collider>())
                Assert.That(collider.enabled, Is.False);
            Assert.That(preview.GetComponentInChildren<Renderer>().sharedMaterial, Is.SameAs(validMaterial));
            controller.End(source, Centre);
            controller.End(source, Centre);
            Assert.That(Count, Is.EqualTo(1));
            var placed = Object.FindFirstObjectByType<PlacedProp>();
            Assert.That(placed.Definition, Is.SameAs(definition));
            Assert.That(placed.GetComponentInChildren<Collider>().enabled, Is.True);
            Assert.That(query.TryGetPose(Centre, out var pose), Is.True); // Placed collider cannot occlude.
            Assert.That(Vector3.Distance(placed.transform.position, pose.position), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(placed.transform.position, previewPose.position), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(placed.transform.rotation, previewPose.rotation), Is.LessThan(.01f));
            Assert.That(controller.Begin(source, definition, Centre), Is.True);
            controller.Cancel();
            Assert.That(Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UIAddedAtReleaseCancelsEvenAfterValidPreview()
        {
            var canvas = new GameObject("blocking UI", typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var panel = new GameObject("panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            // GraphicRaycaster requires a rendered graphic depth.
            yield return NextPlayerFrame();
            Canvas.ForceUpdateCanvases();
            try
            {
                canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
                Assert.That(controller.IsOverUI(Centre), Is.False);
                Assert.That(controller.Begin(source, definition, Centre), Is.True);
                canvas.GetComponent<CanvasGroup>().blocksRaycasts = true;
                Assert.That(controller.IsOverUI(Centre), Is.True);
                controller.End(source, Centre);
                Assert.That(Count, Is.Zero);
                Assert.That(controller.IsDragging, Is.False);
            }
            finally { Object.Destroy(canvas); }
        }

        [Test]
        public void InterruptionsClearStateAndAllowNextDrag()
        {
            for (var i = 0; i < 50; i++)
            {
                Assert.That(controller.Begin(source, definition, Centre), Is.True);
                Assert.That(controller.Begin(source, definition, Centre), Is.False);
                if (i % 3 == 0) host.SetActive(false);
                else if (i % 3 == 1) sourceObject.SetActive(false);
                else host.SendMessage("OnApplicationFocus", false);
                Assert.That(controller.IsDragging, Is.False);
                Assert.That(Count, Is.Zero);
                host.SetActive(true);
                sourceObject.SetActive(true);
            }
        }

        [Test]
        public void OtherMouseButtonWithSamePointerCannotMoveOrCommitLeftDrag()
        {
            var left = new PointerEventData(EventSystem.current)
            {
                pointerId = -1, button = PointerEventData.InputButton.Left,
                pressPosition = Centre + Vector2.right * 30, position = Centre
            };
            source.OnBeginDrag(left);
            Assert.That(controller.IsDragging, Is.True);
            var preview = GameObject.Find("Placement Preview");
            var position = preview.transform.position;
            var right = new PointerEventData(EventSystem.current)
            {
                pointerId = left.pointerId, button = PointerEventData.InputButton.Right,
                pressPosition = left.pressPosition, position = new Vector2(-10, -10)
            };
            source.OnBeginDrag(right);
            source.OnDrag(right);
            Assert.That(preview.transform.position, Is.EqualTo(position));
            right.position = Centre;
            source.OnEndDrag(right);
            Assert.That(controller.IsDragging, Is.True);
            Assert.That(Count, Is.Zero);
            source.OnEndDrag(left);
            Assert.That(controller.IsDragging, Is.False);
            Assert.That(Count, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseCoordinatesOutsideSurfaceCancelAfterValidPreview()
        {
            Assert.That(controller.Begin(source, definition, Centre), Is.True);
            controller.End(source, new Vector2(-10, -10));
            Assert.That(Count, Is.Zero);
            Assert.That(controller.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator UndeliveredReleaseClearsPreviewOnFollowingFrame()
        {
            // No mouse button is held: LateUpdate must cancel even without an EndDrag event.
            Assert.That(controller.Begin(source, definition, Centre), Is.True);
            yield return NextPlayerFrame();
            yield return NextPlayerFrame();
            Assert.That(controller.IsDragging, Is.False);
            Assert.That(GameObject.Find("Placement Preview"), Is.Null);
            Assert.That(Count, Is.Zero);
            Assert.That(controller.Begin(source, definition, Centre), Is.True);
            controller.Cancel();
        }
    }
}
