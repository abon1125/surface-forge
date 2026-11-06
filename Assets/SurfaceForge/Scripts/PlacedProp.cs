using UnityEngine;

namespace SurfaceForge
{
    public sealed class PlacedProp : MonoBehaviour
    {
        [SerializeField] private PropDefinition definition;
        public PropDefinition Definition => definition;
        public event System.Action Unavailable;
        private void OnDisable() => Unavailable?.Invoke();
        public void Initialize(PropDefinition value) => definition = value;
    }
}
