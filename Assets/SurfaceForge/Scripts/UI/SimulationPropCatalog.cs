using UnityEngine;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    public sealed class SimulationPropCatalog : MonoBehaviour
    {
        [SerializeField] private PropDefinition[] definitions;
        [SerializeField] private PropCatalogItemView[] items;
        private void Awake()
        {
            // 項目由場景配置一次；重新開關不 Instantiate，也不綁定桌面手勢。
            if (definitions.Length != items.Length)
            {
                Debug.LogError("VR 清單項目與道具資料數量不一致。", this);
                return;
            }
            for (int i = 0; i < items.Length; i++) items[i].Bind(definitions[i]);
            // S3 射線目標由場景明確配置：項目、viewport 與背景；不再覆寫。
        }
    }
}
