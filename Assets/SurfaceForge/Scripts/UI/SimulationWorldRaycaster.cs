using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    // 不註冊螢幕滑鼠 GraphicRaycaster；所有平面／範圍／遮擋來自傳入的世界 Ray。
    public sealed class SimulationWorldRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Transform ignoredPlayerRoot;
        [SerializeField] private Canvas menuCanvas;
        [SerializeField] private LayerMask sceneMask = ~4;
        [SerializeField, Min(0.1f)] private float maxDistance = 100;
        public Camera ViewCamera => viewCamera;
        public Transform RayOrigin => rayOrigin;
        public float MaxDistance => maxDistance;
        public struct Hit
        {
            public bool HasHit;
            public GameObject UI;
            public Vector3 Point, Normal;
            public float Distance;
        }
        public Ray Aim(Vector2 screenPosition)
        {
            Ray reference = viewCamera.ScreenPointToRay(screenPosition);
            Hit hit = Query(reference);
            Vector3 point = hit.HasHit ? hit.Point : reference.GetPoint(maxDistance);
            return new Ray(rayOrigin.position, (point - rayOrigin.position).normalized);
        }
        public Hit Query(Ray ray)
        {
            var result = new Hit { Distance = maxDistance, Point = ray.GetPoint(maxDistance), Normal = -ray.direction };
            foreach (var hit in Physics.RaycastAll(ray, maxDistance, sceneMask, QueryTriggerInteraction.Ignore))
            {
                if (ignoredPlayerRoot && hit.transform.IsChildOf(ignoredPlayerRoot)) continue;
                if (hit.distance >= result.Distance) continue;
                result = new Hit { HasHit = true, Distance = hit.distance, Point = hit.point, Normal = hit.normal };
            }
            if (!menuCanvas || !menuCanvas.isActiveAndEnabled) return result;
            int depth = -1;
            // 同一 Canvas 的 graphic depth 對應最上層顯示順序。
            var graphics = GraphicRegistry.GetGraphicsForCanvas(menuCanvas);
            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                if (!graphic || !graphic.isActiveAndEnabled || !graphic.raycastTarget || graphic.depth < 0 || graphic.canvasRenderer.cull) continue;
                Transform plane = graphic.rectTransform;
                if (Vector3.Dot(plane.forward, ray.direction) <= 0) continue;
                if (!new Plane(plane.forward, plane.position).Raycast(ray, out float distance) || distance > maxDistance) continue;
                // 物理阻擋物優先於同距離 UI；UI 只以自身深度解決同平面排序。
                if (distance > result.Distance + 0.00001f || (!result.UI && result.HasHit && distance >= result.Distance - 0.00001f)) continue;
                if (result.UI && Mathf.Abs(distance - result.Distance) < 0.00001f && graphic.depth <= depth) continue;
                Vector3 point = ray.GetPoint(distance);
                if (!graphic.rectTransform.rect.Contains(graphic.rectTransform.InverseTransformPoint(point))) continue;
                // 已確認世界交點後，投影只供既有 Mask／CanvasGroup 的 UGUI filter 使用。
                Vector3 screen = viewCamera.WorldToScreenPoint(point);
                if (screen.z <= 0 || !graphic.Raycast(screen, viewCamera)) continue;
                depth = graphic.depth;
                result = new Hit { HasHit = true, UI = graphic.gameObject, Distance = distance, Point = point, Normal = -plane.forward };
            }
            return result;
        }
    }
}
