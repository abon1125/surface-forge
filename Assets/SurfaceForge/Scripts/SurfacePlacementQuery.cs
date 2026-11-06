using UnityEngine;

namespace SurfaceForge
{
    public sealed class SurfacePlacementQuery : MonoBehaviour
    {
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private TerrainCollider surface;
        [SerializeField] private LayerMask surfaceMask;
        [SerializeField, Min(1)] private float maxDistance = 500f;
        [SerializeField, Min(1)] private float invalidDepth = 50f;

        public Ray PointerRay(Vector2 position) => sceneCamera.ScreenPointToRay(position);
        public Vector3 InvalidPosition(Vector2 position) => PointerRay(position).GetPoint(invalidDepth);
        public Vector3 InvalidPosition(Ray ray) => ray.GetPoint(invalidDepth);

        public bool TryGetPose(Vector2 position, out Pose pose)
        {
            pose = default;
            return sceneCamera && TryGetPose(PointerRay(position), out pose);
        }

        public bool TryGetPose(Ray ray, out Pose pose)
        {
            pose = default;
            if (!surface || !surface.enabled || !surface.gameObject.activeInHierarchy ||
                (surfaceMask.value & (1 << surface.gameObject.layer)) == 0) return false;
            // Raycast the explicitly assigned collider, so props can never occlude the surface.
            if (!surface.Raycast(ray, out var hit, maxDistance)) return false;
            pose = PoseAt(hit.point, hit.normal);
            return true;
        }

        public static Pose PoseAt(Vector3 point, Vector3 normal) =>
            new Pose(point, Quaternion.FromToRotation(Vector3.up, normal.normalized));
    }
}
