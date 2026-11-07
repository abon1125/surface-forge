using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SurfaceForge.UI;

namespace SurfaceForge.Tests
{
    // 由使用者在 Test Runner 執行；不取代原生滑鼠／世界射線人工驗收。
    public class SceneNavigationTests
    {
        [UnityTest]
        public IEnumerator SingleLoadCleansAndReentryStartsFresh()
        {
            EditorSceneManager.OpenScene(SceneNavigation.MenuPath);
            yield return new EnterPlayMode();
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
        static IEnumerator UntilScene(string name)
        {
            double deadline = EditorApplication.timeSinceStartup + 20;
            do
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline), $"等待 {name} 超時");
                EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            } while (SceneManager.GetActiveScene().name != name);
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
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
