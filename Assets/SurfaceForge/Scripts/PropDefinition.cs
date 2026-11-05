using UnityEngine;

namespace SurfaceForge
{
    [CreateAssetMenu(menuName = "SurfaceForge/Prop Definition")]
    public sealed class PropDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
    }
}
