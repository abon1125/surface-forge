using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SurfaceForge.UI;

namespace SurfaceForge
{
    // 每個場景各自擁有，不建立跨場景單例或保存道具。
    [DefaultExecutionOrder(1000)]
    public sealed class SceneNavigation : MonoBehaviour
    {
        public const string MenuPath = "Assets/SurfaceForge/Scenes/SceneMenu.unity";
        public const string DesktopPath = "Assets/SurfaceForge/Scenes/Main.unity";
        public const string SimulationPath = "Assets/SurfaceForge/Scenes/VRSimulation.unity";
        [SerializeField] private PlacementController placement;
        [SerializeField] private PropSelectionController desktopSelection;
        [SerializeField] private SimulationSelectionController simulationSelection;
        [SerializeField] private SimulationPlayer player;
        [SerializeField] private SimulationPropMenu simulationMenu;
        [SerializeField] private PropCatalogView desktopCatalog;
        [SerializeField] private PlacementGuideView guide;
        [SerializeField] private EventSystem sceneEvents;
        [SerializeField] private Text[] navigationLabels;
        [SerializeField] private string[] fontNames = { "Microsoft JhengHei", "Noto Sans CJK TC", "Arial Unicode MS" };
        private Font navigationFont;
        private string pendingPath;
        public bool IsTransitionPending => !string.IsNullOrEmpty(pendingPath);

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            navigationFont = Font.CreateDynamicFontFromOSFont(fontNames, 28);
            if (navigationLabels != null)
                foreach (var label in navigationLabels) if (label) label.font = navigationFont;
        }
        public void OpenDesktop() => Request(DesktopPath);
        public void OpenSimulation() => Request(SimulationPath);
        public void ReturnToMenu() => Request(MenuPath);
        private void Request(string path)
        {
            if (IsTransitionPending || !isActiveAndEnabled) return;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError($"場景未加入有效場景清單：{path}", this);
                return;
            }
            pendingPath = path;
        }
        private void LateUpdate()
        {
            if (!IsTransitionPending) return;
            // UI click 派送已返回後才停用模組，避免重入 pointerUp / click。
            string path = pendingPath;
            PrepareToLeave();
            SceneManager.LoadScene(path, LoadSceneMode.Single);
        }
        public void PrepareToLeave()
        {
            if (sceneEvents)
            {
                foreach (var module in sceneEvents.GetComponents<BaseInputModule>()) module.enabled = false;
                sceneEvents.SetSelectedGameObject(null);
                sceneEvents.enabled = false;
            }
            if (placement) { placement.Cancel(); placement.enabled = false; }
            if (guide) guide.Hide();
            if (desktopSelection) { desktopSelection.ClearSelection(); desktopSelection.enabled = false; }
            if (simulationSelection) { simulationSelection.ClearSelection(); simulationSelection.enabled = false; }
            if (simulationMenu) simulationMenu.enabled = false;
            if (desktopCatalog) desktopCatalog.SetExpanded(false);
            if (player) player.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        private void OnDestroy()
        {
            if (navigationFont) Destroy(navigationFont);
        }
    }
}
