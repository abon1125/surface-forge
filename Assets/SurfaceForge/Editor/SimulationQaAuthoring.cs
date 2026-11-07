using System;
using System.Linq;
using SurfaceForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnitySkills;

namespace SurfaceForge.Editor
{
    public static class SimulationQaAuthoring
    {
        [UnitySkill("sf_author_simulation_qa", "Add non-interactive screen-space controls and lower the orbit menu to the upper front of props. Refuses Play Mode, dirty scenes and duplicate UI.",
            Category = SkillCategory.Scene, Operation = SkillOperation.Modify,
            MutatesScene = true, MutatesAssets = true, TracksWorkflow = false, RiskLevel = "high")]
        public static object Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("請先離開 Play Mode。");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("有未儲存場景，停止編輯。");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(SceneNavigation.SimulationPath);
                var roots = scene.GetRootGameObjects();
                if (roots.Any(r => r.name == "Simulation Controls UI")) throw new InvalidOperationException("操作說明已存在，禁止重複建立。");
                var context = roots.SelectMany(r => r.GetComponentsInChildren<SimulationContextMenuView>(true)).Single();
                var nav = roots.SelectMany(r => r.GetComponentsInChildren<SceneNavigation>(true)).Single();
                var root = new GameObject("Simulation Controls UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                var group = root.GetComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;
                var panel = new GameObject("Controls", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                panel.SetParent(root.transform, false);
                panel.anchorMin = new Vector2(0, 1); panel.anchorMax = Vector2.one;
                panel.pivot = new Vector2(.5f, 1);
                panel.anchoredPosition = new Vector2(0, -16);
                panel.sizeDelta = new Vector2(-32, 112);
                var background = panel.GetComponent<Image>();
                background.color = new Color(.035f, .055f, .08f, .82f);
                background.raycastTarget = false;
                var label = new GameObject("Instructions", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                var rect = (RectTransform)label.transform;
                rect.SetParent(panel, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(20, 8); rect.offsetMax = new Vector2(-20, -8);
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 26; label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white; label.raycastTarget = false;
                label.text = "WASD 移動　｜　按住右鍵＋移動滑鼠：觀看　｜　Tab：開關左手面板\n"
                    + "左鍵：射線點選／按住項目拖出，放開放置　｜　滾輪：捲動指向的清單\n"
                    + "Esc：取消拖曳；無拖曳時清除選取並關閉面板　｜　返回：左手面板下方按鈕";
                var navigation = new SerializedObject(nav);
                var labels = navigation.FindProperty("navigationLabels");
                labels.InsertArrayElementAtIndex(labels.arraySize);
                labels.GetArrayElementAtIndex(labels.arraySize - 1).objectReferenceValue = label;
                navigation.ApplyModifiedPropertiesWithoutUndo();
                var settings = new SerializedObject(context);
                settings.FindProperty("offsetRatio").vector3Value = new Vector3(0, .3f, 0);
                settings.ApplyModifiedPropertiesWithoutUndo();
                Canvas.ForceUpdateCanvases();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("場景儲存失敗。");
                return new { path = scene.path, renderMode = canvas.renderMode.ToString(),
                    raycasterCount = root.GetComponents<GraphicRaycaster>().Length,
                    blocksRaycasts = group.blocksRaycasts, labelCount = labels.arraySize,
                    offsetRatio = new[] { 0f, .3f, 0f }, text = label.text };
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
