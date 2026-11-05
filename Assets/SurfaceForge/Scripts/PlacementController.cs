using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SurfaceForge
{
    public sealed class PlacementController : MonoBehaviour
    {
        [SerializeField] private SurfacePlacementQuery surfaceQuery;
        [SerializeField] private Material validPreviewMaterial;
        [SerializeField] private Material invalidPreviewMaterial;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private GameObject preview;
        private PropDefinition definition;
        private UI.PropItemDragHandler source;
        public bool IsDragging => preview != null;

        public bool Begin(UI.PropItemDragHandler owner, PropDefinition value, Vector2 position)
        {
            if (!isActiveAndEnabled || IsDragging || !value || !value.Prefab || !surfaceQuery) return false;
            source = owner;
            definition = value;
            preview = Instantiate(value.Prefab);
            preview.name = "Placement Preview";
            foreach (var t in preview.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            UpdatePreview(position);
            return true;
        }

        public bool IsOverUI(Vector2 position)
        {
            if (!EventSystem.current) return true; // Missing UI input must never allow accidental placement.
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            foreach (var hit in uiHits) if (hit.module is GraphicRaycaster) return true;
            return false;
        }

        private bool TryPose(Vector2 position, out Pose pose)
        {
            pose = default;
            return position.x >= 0 && position.y >= 0 && position.x < Screen.width && position.y < Screen.height &&
                !IsOverUI(position) && surfaceQuery.TryGetPose(position, out pose);
        }

        public void UpdatePreview(Vector2 position)
        {
            if (!IsDragging) return;
            var overUI = IsOverUI(position);
            var valid = TryPose(position, out var pose);
            preview.SetActive(!overUI);
            if (!valid) pose = new Pose(surfaceQuery.InvalidPosition(position), Quaternion.identity);
            preview.transform.SetPositionAndRotation(pose.position, pose.rotation);
            foreach (var renderer in preview.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++) materials[i] = valid ? validPreviewMaterial : invalidPreviewMaterial;
                renderer.sharedMaterials = materials;
            }
        }

        public void End(UI.PropItemDragHandler owner, Vector2 releasePosition)
        {
            if (source != owner || !IsDragging) return;
            // Re-query release coordinates and all UI; never commit a cached preview pose.
            if (TryPose(releasePosition, out var pose))
            {
                var placed = Instantiate(definition.Prefab, pose.position, pose.rotation);
                foreach (var t in placed.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
                placed.AddComponent<PlacedProp>().Initialize(definition);
            }
            Cancel();
        }

        public void Cancel(UI.PropItemDragHandler owner)
        {
            if (source == owner) Cancel();
        }

        public void Cancel()
        {
            if (preview)
            {
                preview.SetActive(false);
                if (Application.isPlaying) Destroy(preview);
                else DestroyImmediate(preview);
            }
            preview = null;
            definition = null;
            if (source) source.PlacementCancelled();
            source = null;
        }

        private void Update()
        {
            if (!IsDragging) return;
            var mouse = Mouse.current;
            if ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || mouse == null ||
                mouse.position.ReadValue().x < 0 || mouse.position.ReadValue().y < 0 ||
                mouse.position.ReadValue().x >= Screen.width || mouse.position.ReadValue().y >= Screen.height)
                Cancel();
        }
        private void LateUpdate()
        {
            // UI EndDrag runs during Update. Only then cancel a release that UI did not deliver.
            if (IsDragging && !(Mouse.current?.leftButton.isPressed ?? false)) Cancel();
            else if (IsDragging) UpdatePreview(Mouse.current.position.ReadValue());
        }
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
        private void OnDisable() => Cancel();
    }
}
