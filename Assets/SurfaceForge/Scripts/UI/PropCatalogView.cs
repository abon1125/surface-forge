using UnityEngine;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    public sealed class PropCatalogView : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject listRoot;
        [SerializeField] private Text toggleLabel;
        [SerializeField] private RectTransform content;
        [SerializeField] private PropCatalogItemView itemPrefab;
        [SerializeField] private PropDefinition[] definitions;

        public bool IsExpanded => listRoot.activeSelf;

        private void Awake()
        {
            foreach (var definition in definitions)
            {
                Instantiate(itemPrefab, content).Bind(definition);
            }
            SetExpanded(false);
            toggleButton.onClick.AddListener(Toggle);
        }

        private void OnDestroy() => toggleButton.onClick.RemoveListener(Toggle);

        private void Toggle() => SetExpanded(!IsExpanded);

        public void SetExpanded(bool expanded)
        {
            listRoot.SetActive(expanded);
            toggleLabel.text = expanded ? "PROPS   /   CLOSE -" : "PROPS   /   OPEN +";
        }
    }
}
