using System;
using System.Linq;
using SurfaceForge.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnitySkills;

namespace SurfaceForge.Editor
{
    public static class SceneMenuAuthoring
    {
        [UnitySkill("sf_author_scene_menu", "Author I1 navigation in the three scenes and set their standard build order. Refuses Play Mode, dirty scenes and an existing menu.",
            Category = SkillCategory.Scene, Operation = SkillOperation.Create | SkillOperation.Modify,
            MutatesScene = true, MutatesAssets = true, TracksWorkflow = false, RiskLevel = "high", Outputs = new[] { "paths" })]
        public static object Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("請先離開 Play Mode。");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("有未儲存場景，停止編輯。");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneNavigation.MenuPath)) throw new InvalidOperationException("SceneMenu 已存在，禁止覆寫。");
            try
            {
                EditorSceneManager.OpenScene(SceneNavigation.DesktopPath);
                var desktop = AddNavigation();
                var canvas = All<Canvas>().Single(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
                var back = Button(canvas.transform, "ReturnToMenu", "返回場景選單", new Vector2(280, 52), new Vector2(166, -38), new Vector2(0, 1));
                UnityEventTools.AddPersistentListener(back.onClick, desktop.ReturnToMenu);
                Labels(desktop, back.GetComponentInChildren<Text>());
                EditorSceneManager.SaveScene(desktop.gameObject.scene);

                EditorSceneManager.OpenScene(SceneNavigation.SimulationPath);
                var simulation = AddNavigation();
                var menu = All<SimulationPropMenu>().Single();
                var menuObject = new SerializedObject(menu).FindProperty("panel").objectReferenceValue as GameObject;
                var vrBack = Button(menuObject.transform, "ReturnToMenu", "返回場景選單", new Vector2(332, 44), new Vector2(0, -222), new Vector2(.5f, .5f));
                UnityEventTools.AddPersistentListener(vrBack.onClick, simulation.ReturnToMenu);
                Labels(simulation, vrBack.GetComponentInChildren<Text>());
                EditorSceneManager.SaveScene(simulation.gameObject.scene);

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Menu Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.tag = "MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.055f, .075f, .1f);
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                var root = new GameObject("Scene Menu UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                var navigation = AddNavigation();
                var title = Label(root.transform, "Title", "SurfaceForge", new Vector2(600, 80), new Vector2(0, 160));
                title.fontSize = 48;
                var main = Button(root.transform, "Desktop", "桌面版驗收", new Vector2(440, 80), new Vector2(0, 35), new Vector2(.5f, .5f));
                var vr = Button(root.transform, "Simulation", "VR 模擬驗收", new Vector2(440, 80), new Vector2(0, -75), new Vector2(.5f, .5f));
                UnityEventTools.AddPersistentListener(main.onClick, navigation.OpenDesktop);
                UnityEventTools.AddPersistentListener(vr.onClick, navigation.OpenSimulation);
                Labels(navigation, title, main.GetComponentInChildren<Text>(), vr.GetComponentInChildren<Text>());
                EditorSceneManager.SaveScene(scene, SceneNavigation.MenuPath);
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneNavigation.LoadingPath)) return LoadingSceneAuthoring.Author();
                return ConfigureSceneOrder();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [UnitySkill("sf_configure_scene_order", "Set global and active scene lists to SceneMenu, Main, VRSimulation, Loading. Does not build or enter Play Mode.",
            Category = SkillCategory.Scene, Operation = SkillOperation.Modify, MutatesAssets = true,
            TracksWorkflow = false, RiskLevel = "high", Outputs = new[] { "paths" })]
        public static object ConfigureSceneOrder()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("請先離開 Play Mode。");
            var paths = new[] { SceneNavigation.MenuPath, SceneNavigation.DesktopPath, SceneNavigation.SimulationPath, SceneNavigation.LoadingPath };
            if (paths.Any(p => !AssetDatabase.LoadAssetAtPath<SceneAsset>(p))) throw new InvalidOperationException("場景尚未建立完整。");
            EditorBuildSettings.globalScenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            EditorBuildSettings.scenes = EditorBuildSettings.globalScenes;
            AssetDatabase.SaveAssets();
            return new { paths = EditorBuildSettings.globalScenes.Select(s => new { s.path, s.enabled }).ToArray() };
        }
        static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static SceneNavigation AddNavigation()
        {
            if (All<SceneNavigation>().Length != 0) throw new InvalidOperationException("已有導航元件，停止重複建立。");
            var nav = new GameObject("SceneNavigation").AddComponent<SceneNavigation>();
            var so = new SerializedObject(nav);
            Reference<PlacementController>(so, "placement");
            Reference<PropSelectionController>(so, "desktopSelection");
            Reference<SimulationSelectionController>(so, "simulationSelection");
            Reference<SimulationPlayer>(so, "player");
            Reference<SimulationPropMenu>(so, "simulationMenu");
            Reference<PropCatalogView>(so, "desktopCatalog");
            Reference<PlacementGuideView>(so, "guide");
            Reference<EventSystem>(so, "sceneEvents");
            so.ApplyModifiedPropertiesWithoutUndo();
            return nav;
        }
        static void Reference<T>(SerializedObject so, string field) where T : UnityEngine.Object => so.FindProperty(field).objectReferenceValue = All<T>().SingleOrDefault();
        static void Labels(SceneNavigation nav, params Text[] texts)
        {
            var so = new SerializedObject(nav);
            var array = so.FindProperty("navigationLabels"); array.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position, Vector2 anchor)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        static Text Label(Transform parent, string name, string text, Vector2 size, Vector2 position)
        {
            var label = Rect(parent, name, size, position, new Vector2(.5f, .5f)).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = 28; label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white; label.raycastTarget = false; return label;
        }
        static Button Button(Transform parent, string name, string text, Vector2 size, Vector2 position, Vector2 anchor)
        {
            var rect = Rect(parent, name, size, position, anchor);
            rect.gameObject.AddComponent<Image>().color = new Color(.12f, .22f, .32f, 1);
            var button = rect.gameObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = Label(rect, "Label", text, size, Vector2.zero);
            label.fontSize = size.y < 60 ? 24 : 28;
            return button;
        }
    }
}
