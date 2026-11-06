using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SurfaceForge
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SimulationPlayer : MonoBehaviour
    {
        [Header("明確場景參照")]
        [SerializeField] private CharacterController motor;
        [SerializeField] private Terrain terrain;
        [SerializeField] private Transform viewPivot;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;
        [Header("移動與碰撞（公尺）")]
        [SerializeField, Min(0.1f)] private float speed = 4;
        [SerializeField, Min(0.5f)] private float height = 1.8f;
        [SerializeField, Min(0.05f)] private float radius = 0.3f;
        [SerializeField, Range(1, 60)] private float slopeLimit = 45;
        [SerializeField, Min(0)] private float stepOffset = 0.25f;
        [SerializeField, Min(0.01f)] private float skinWidth = 0.03f;
        [SerializeField, Min(0.01f)] private float boundaryMargin = 0.05f;
        [SerializeField, Min(1)] private float gravity = 20;
        [SerializeField, Min(0)] private float groundAdhesion = 0.3f;
        [Header("觀看（每滑鼠像素的角度）")]
        [SerializeField, Min(0.001f)] private float sensitivity = 0.12f;
        [SerializeField, Range(-89, 0)] private float minPitch = -75;
        [SerializeField, Range(0, 89)] private float maxPitch = 75;
        [Header("第一人稱構圖")]
        [SerializeField] private float eyeHeight = 1.65f;
        [SerializeField] private Vector3 leftOffset = new Vector3(-0.28f, -0.25f, 0.55f);
        [SerializeField] private Vector3 rightOffset = new Vector3(0.28f, -0.25f, 0.55f);
        [SerializeField, Min(0.01f)] private float controllerDiameter = 0.12f;
        [SerializeField] private bool showBody = false;
        private float pitch, verticalSpeed;
        private bool focused = true, requireRelease = true;
        public bool IsLooking { get; private set; }

        private void Awake()
        {
            if (!motor || !terrain || !terrain.terrainData || !viewPivot || !viewCamera ||
                !body || !leftController || !rightController)
            {
                Debug.LogError("SimulationPlayer 缺少 Inspector 場景參照。", this);
                enabled = false;
                return;
            }
            ApplySettings();
            RecoverToTerrain(true);
        }
        private void OnEnable() { requireRelease = true; }
        private void OnValidate()
        {
            height = Mathf.Max(height, radius * 2);
            eyeHeight = Mathf.Clamp(eyeHeight, radius, height);
            stepOffset = Mathf.Clamp(stepOffset, 0, height);
            if (motor && viewPivot && body && leftController && rightController) ApplySettings();
        }
        private void ApplySettings()
        {
            motor.height = height;
            motor.radius = radius;
            motor.center = Vector3.up * height * 0.5f;
            motor.slopeLimit = slopeLimit;
            motor.stepOffset = stepOffset;
            motor.skinWidth = skinWidth;
            motor.minMoveDistance = 0;
            viewPivot.localPosition = Vector3.up * eyeHeight;
            body.localPosition = Vector3.up * height * 0.5f;
            body.localScale = new Vector3(radius * 2, height * 0.5f, radius * 2);
            body.GetComponent<Renderer>().shadowCastingMode = showBody ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            leftController.localPosition = leftOffset;
            rightController.localPosition = rightOffset;
            leftController.localScale = rightController.localScale = Vector3.one * controllerDiameter;
        }
        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool held = mouse != null && mouse.rightButton.isPressed;
            if (!held) { requireRelease = false; EndLook(); }
            if (!focused || !Application.isFocused) { Interrupt(); return; }
            if (mouse != null && mouse.rightButton.wasPressedThisFrame && !requireRelease && InViewport(mouse.position.ReadValue()))
            {
                IsLooking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // Ignore the activation frame's delta (including cursor recentering).
            }
            else if (IsLooking && held) Look(mouse.delta.ReadValue());
            if (IsLooking && Cursor.lockState != CursorLockMode.Locked) Interrupt();
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            }
            Move(input, Mathf.Min(Time.deltaTime, 0.05f));
        }
        private bool InViewport(Vector2 point) => viewCamera.pixelRect.Contains(point) &&
            point.x >= 0 && point.y >= 0 && point.x < Screen.width && point.y < Screen.height;
        private void Look(Vector2 delta)
        {
            transform.Rotate(0, delta.x * sensitivity, 0, Space.World);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
            viewPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        private Vector3 ClampToBounds(Vector3 point)
        {
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            float inset = radius + boundaryMargin + skinWidth;
            point.x = Mathf.Clamp(point.x, origin.x + inset, origin.x + size.x - inset);
            point.z = Mathf.Clamp(point.z, origin.z + inset, origin.z + size.z - inset);
            return point;
        }
        private bool Ground(Vector3 point, out RaycastHit hit)
        {
            var origin = new Vector3(point.x, terrain.transform.position.y + terrain.terrainData.size.y + height, point.z);
            return terrain.GetComponent<TerrainCollider>().Raycast(new Ray(origin, Vector3.down), out hit,
                terrain.terrainData.size.y + height * 2);
        }
        private void RecoverToTerrain(bool initial)
        {
            Vector3 point = ClampToBounds(transform.position);
            bool outside = (new Vector2(point.x - transform.position.x, point.z - transform.position.z)).sqrMagnitude > 0.000001f;
            if (Ground(point, out var hit) && (initial || outside || point.y < hit.point.y - height))
            {
                point.y = hit.point.y + skinWidth;
                motor.enabled = false;
                transform.position = point;
                motor.enabled = true;
                verticalSpeed = 0;
            }
        }
        private void Move(Vector2 input, float dt)
        {
            RecoverToTerrain(false);
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            direction.y = 0;
            direction = Vector3.ClampMagnitude(direction, 1);
            Vector3 target = ClampToBounds(transform.position + direction * speed * dt);
            Vector3 horizontal = target - transform.position;
            horizontal.y = 0;
            // CharacterController handles contact; prevent climbing steep terrain even at low frame rates.
            if (Ground(target, out var hit) && Vector3.Angle(hit.normal, Vector3.up) > slopeLimit &&
                Vector3.Dot(horizontal, new Vector3(hit.normal.x, 0, hit.normal.z)) < 0)
                horizontal = Vector3.zero;
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(verticalSpeed - gravity * dt, -50);
            float down = verticalSpeed * dt;
            if (motor.isGrounded) down -= groundAdhesion;
            motor.Move(horizontal + Vector3.up * down);
            RecoverToTerrain(false);
        }
        private void EndLook()
        {
            if (!IsLooking) return;
            IsLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        private void Interrupt() { requireRelease = true; EndLook(); }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Interrupt(); }
        private void OnApplicationPause(bool value) { if (value) Interrupt(); }
        private void OnDisable() { Interrupt(); }
        private void OnDestroy() { Interrupt(); }
    }
}
