using UnityEngine;

namespace SurfaceForge
{
    [RequireComponent(typeof(Camera))]
    public sealed class TerrainOverviewCamera : MonoBehaviour
    {
        [SerializeField] private RectTransform sidebar;
        [SerializeField] private Terrain terrain;
        private Camera overview;
        private readonly Vector3[] corners = new Vector3[4];

        private void Awake() => overview = GetComponent<Camera>();

        private void LateUpdate()
        {
            sidebar.GetWorldCorners(corners);
            float width = Mathf.Clamp(corners[0].x / Screen.width, 0.1f, 1f);
            overview.rect = new Rect(0, 0, width, 1);
            // Fit the finite heightfield bounds in camera space, including a margin.
            Vector3 size = terrain.terrainData.size;
            Vector3 center = terrain.transform.position + size * 0.5f;
            float halfWidth = 0, halfHeight = 0;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = terrain.transform.position + new Vector3(
                    (i & 1) == 0 ? 0 : size.x,
                    (i & 2) == 0 ? 0 : size.y,
                    (i & 4) == 0 ? 0 : size.z);
                Vector3 offset = transform.InverseTransformVector(point - center);
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(offset.x));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(offset.y));
            }
            float aspect = Mathf.Max(0.1f, Screen.width * width / Screen.height);
            overview.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect) * 1.12f;
        }
    }
}
