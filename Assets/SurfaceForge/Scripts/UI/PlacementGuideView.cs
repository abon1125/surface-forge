using UnityEngine;

namespace SurfaceForge.UI
{
    // 專用金色直線與落點圓環，不改動控制器操作射線。
    public sealed class PlacementGuideView : MonoBehaviour
    {
        [SerializeField] private LineRenderer guide;
        [SerializeField] private LineRenderer ring;
        [SerializeField, Min(0.01f)] private float radius = 0.22f;
        public bool IsVisible => guide && guide.enabled;

        public void Show(Transform preview, Pose pose)
        {
            var up = pose.rotation * Vector3.up;
            // 原有四種道具高 1.5 m，以 renderer bounds 取得預覽沿法線的頂部。
            float height = 0;
            foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
            {
                Bounds b = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    height = Mathf.Max(height, Vector3.Dot(renderer.transform.TransformPoint(corner) - pose.position, up));
                }
            }
            guide.enabled = ring.enabled = true;
            guide.SetPosition(0, pose.position + up * (height + 0.12f));
            guide.SetPosition(1, pose.position + up * 0.015f);
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, pose.position + pose.rotation * new Vector3(Mathf.Cos(angle) * radius, 0.015f, Mathf.Sin(angle) * radius));
            }
        }

        public void Hide()
        {
            if (guide) guide.enabled = false;
            if (ring) ring.enabled = false;
        }
        private void OnDisable() => Hide();
    }
}
