using UnityEngine;
using UnityEngine.UI;

namespace SurfaceForge.UI
{
    public sealed class PropCatalogItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;

        // A future pointer handler can read the bound definition from this row.
        public PropDefinition Definition { get; private set; }

        public void Bind(PropDefinition definition)
        {
            Definition = definition;
            icon.sprite = definition.Icon;
            label.text = definition.DisplayName;
            gameObject.name = $"Prop Item ({definition.Id})";
        }
    }
}
