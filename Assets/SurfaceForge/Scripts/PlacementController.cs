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
        private Object source;
        private bool worldGesture;
        public bool IsDragging => preview != null;
        public Transform Preview => preview ? preview.transform : null;
        public bool HasValidPose { get; private set; }
        public Pose PreviewPose { get; private set; }
        public event System.Action Finished;
        public event System.Action GestureStarted;
        public int LastConsumedFrame { get; private set; } = -1;

        // Both scrolling and placement own their entire left-button gesture.
        public void ConsumeGesture()
        {
            LastConsumedFrame = Time.frameCount;
            GestureStarted?.Invoke();
        }

        public bool Begin(UI.PropItemDragHandler owner, PropDefinition value, Vector2 position)
        {
            if (!BeginCore(owner, value, false)) return false;
            UpdatePreview(position);
            return true;
        }

        public bool BeginWorld(Object owner, PropDefinition value, Ray ray, bool overUI)
        {
            if (!owner || !BeginCore(owner, value, true)) return false;
            UpdateWorld(owner, ray, overUI);
            return true;
        }

        private bool BeginCore(Object owner, PropDefinition value, bool world)
        {
            if (!isActiveAndEnabled || IsDragging || !value || !value.Prefab || !surfaceQuery) return false;
            ConsumeGesture();
            source = owner;
            worldGesture = world;
            definition = value;
            preview = Instantiate(value.Prefab);
            preview.name = "Placement Preview";
            foreach (var t in preview.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
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
            if (!IsDragging || worldGesture) return;
            var overUI = IsOverUI(position);
            var valid = TryPose(position, out var pose);
            ApplyPreview(overUI, valid, valid ? pose : new Pose(surfaceQuery.InvalidPosition(position), Quaternion.identity));
        }

        public void UpdateWorld(Object owner, Ray ray, bool overUI)
        {
            if (source != owner || !worldGesture || !IsDragging) return;
            var valid = surfaceQuery.TryGetPose(ray, out var pose) && !overUI;
            ApplyPreview(overUI, valid, valid ? pose : new Pose(surfaceQuery.InvalidPosition(ray), Quaternion.identity));
        }

        private void ApplyPreview(bool overUI, bool valid, Pose pose)
        {
            HasValidPose = valid;
            PreviewPose = pose;
            preview.SetActive(!overUI);
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
            if (source != owner || worldGesture || !IsDragging) return;
            // Re-query release coordinates and all UI; never commit a cached preview pose.
            Commit(TryPose(releasePosition, out var pose), pose);
        }

        public void EndWorld(Object owner, Ray releaseRay, bool overUI)
        {
            if (source != owner || !worldGesture || !IsDragging) return;
            var valid = surfaceQuery.TryGetPose(releaseRay, out var pose);
            Commit(!overUI && valid, pose);
        }

        private void Commit(bool valid, Pose pose)
        {
            if (valid)
            {
                var placed = Instantiate(definition.Prefab, pose.position, pose.rotation);
                foreach (var t in placed.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("PlacedProp");
                placed.AddComponent<PlacedProp>().Initialize(definition);
            }
            Cancel();
        }

        public void Cancel(Object owner)
        {
            if (source == owner) Cancel();
        }

        public void Cancel()
        {
            bool wasDragging = IsDragging;
            if (wasDragging) LastConsumedFrame = Time.frameCount;
            if (preview)
            {
                preview.SetActive(false);
                if (Application.isPlaying) Destroy(preview);
                else DestroyImmediate(preview);
            }
            preview = null;
            definition = null;
            HasValidPose = false;
            PreviewPose = default;
            if (source is UI.PropItemDragHandler desktop && desktop) desktop.PlacementCancelled();
            source = null;
            worldGesture = false;
            if (wasDragging) Finished?.Invoke();
        }

        private void Update()
        {
            if (!IsDragging || worldGesture) return;
            var mouse = Mouse.current;
            if ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || mouse == null ||
                mouse.position.ReadValue().x < 0 || mouse.position.ReadValue().y < 0 ||
                mouse.position.ReadValue().x >= Screen.width || mouse.position.ReadValue().y >= Screen.height)
                Cancel();
        }
        private void LateUpdate()
        {
            if (worldGesture) return;
            // UI EndDrag runs during Update. Only then cancel a release that UI did not deliver.
            if (IsDragging && !(Mouse.current?.leftButton.isPressed ?? false)) Cancel();
            else if (IsDragging) UpdatePreview(Mouse.current.position.ReadValue());
        }
        private void OnApplicationFocus(bool focused) { if (!focused) Cancel(); }
        private void OnApplicationPause(bool paused) { if (paused) Cancel(); }
        private void OnDisable() => Cancel();
    }
}
