using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    [RequireComponent(typeof(PropCatalogItemView))]
    public sealed class SimulationPropItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Text feedback;
        private bool hovered, pressed;
        private float flashUntil;
        public PropDefinition LastClickedDefinition { get; private set; }
        public int ClickCount { get; private set; }
        public bool IsHovered => hovered;
        public bool IsPressed => pressed;
        public void OnPointerEnter(PointerEventData data) { hovered = true; Paint(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; Paint(); }
        public void OnPointerDown(PointerEventData data) { pressed = true; Paint(); }
        public void OnPointerUp(PointerEventData data) { pressed = false; Paint(); }
        public void OnPointerClick(PointerEventData data)
        {
            LastClickedDefinition = GetComponent<PropCatalogItemView>().Definition;
            ClickCount++;
            if (feedback) feedback.text = "CLICK: " + LastClickedDefinition.Id;
            flashUntil = Time.unscaledTime + 0.3f;
            Paint();
        }
        private void Update() { Paint(); }
        private void OnDisable() { hovered = pressed = false; flashUntil = 0; Paint(); }
        private void Paint()
        {
            if (background) background.color = pressed ? new Color(1, 0.65f, 0.15f) :
                Time.unscaledTime < flashUntil ? new Color(0.2f, 0.85f, 0.45f) :
                hovered ? new Color(0.15f, 0.65f, 0.9f) : new Color(0.10f, 0.17f, 0.23f, 0.98f);
        }
    }
}
