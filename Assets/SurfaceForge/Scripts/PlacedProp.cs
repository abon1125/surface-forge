using UnityEngine;

namespace SurfaceForge
{
    public sealed class PlacedProp : MonoBehaviour
    {
        [SerializeField] private PropDefinition definition;
        public PropDefinition Definition => definition;
        public void Initialize(PropDefinition value) => definition = value;
    }
}
