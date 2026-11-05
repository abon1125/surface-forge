using UnityEngine;

namespace SurfaceForge
{
    public sealed class SimulatedPlayer : MonoBehaviour
    {
        [SerializeField] private Transform headAnchor;
        public Transform HeadAnchor => headAnchor;
    }
}
