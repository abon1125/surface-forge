using System;
using SurfaceForge.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnitySkills;

namespace SurfaceForge.Editor
{
    public static class LoadingSceneAuthoring
    {
        [UnitySkill("sf_author_loading", "Create the I1 Loading scene and configure four-scene build order. Refuses Play Mode, dirty scenes and overwriting Loading.",
            Category = SkillCategory.Scene, Operation = SkillOperation.Create | SkillOperation.Modify,
            MutatesScene = true, MutatesAssets = true, TracksWorkflow = false, RiskLevel = "high", Outputs = new[] { "paths" })]
        public static object Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("請先離開 Play Mode。");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("有未儲存場景，停止編輯。");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneNavigation.LoadingPath)) throw new InvalidOperationException("Loading 已存在，禁止覆寫。");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Loading UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
                var group = root.GetComponent<CanvasGroup>(); group.alpha = 0; group.interactable = false; group.blocksRaycasts = true;
                var background = Image(root.transform, "Background", new Color(.055f, .075f, .1f, 1));
                Stretch(background.rectTransform);
                var status = Label(root.transform, "Status", "載入中…", new Vector2(900, 70), new Vector2(0, 65), 32);
                var track = Image(root.transform, "Progress Track", new Color(.15f, .19f, .24f, 1));
                track.rectTransform.sizeDelta = new Vector2(600, 24);
                var area = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
                area.SetParent(track.transform, false); Stretch(area); area.offsetMin = new Vector2(3, 3); area.offsetMax = new Vector2(-3, -3);
                var fill = Image(area, "Progress Fill", new Color(.25f, .75f, .83f, 1));
                Stretch(fill.rectTransform); fill.rectTransform.anchorMax = new Vector2(0, 1);
                var percent = Label(root.transform, "Percent", "0%", new Vector2(600, 50), new Vector2(0, -54), 24);
                var controller = root.AddComponent<LoadingController>();
                var so = new SerializedObject(controller);
                so.FindProperty("overlay").objectReferenceValue = group;
                so.FindProperty("progressFill").objectReferenceValue = fill;
                so.FindProperty("statusLabel").objectReferenceValue = status;
                so.FindProperty("percentLabel").objectReferenceValue = percent;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, SceneNavigation.LoadingPath);
                return SceneMenuAuthoring.ConfigureSceneOrder();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static Image Image(Transform parent, string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false); image.color = color; image.raycastTarget = false;
            return image;
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        static Text Label(Transform parent, string name, string text, Vector2 size, Vector2 position, int fontSize)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false); label.rectTransform.sizeDelta = size; label.rectTransform.anchoredPosition = position;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = text; label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
            return label;
        }
    }
}
