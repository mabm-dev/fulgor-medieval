using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed class HexTileView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color SelectionColor = new(0.95f, 0.72f, 0.18f);

        private MaterialPropertyBlock properties;
        public CampaignBoardSelection Owner { get; private set; }
        private MeshRenderer meshRenderer;
        private Color baseColor;
        private bool hovered;
        private bool selected;

        public MapCell Cell { get; private set; }

        public void Initialize(MapCell cell, CampaignBoardSelection selectionOwner, Color color)
        {
            Cell = cell;
            Owner = selectionOwner;
            baseColor = color;
            meshRenderer = GetComponent<MeshRenderer>();
            ApplyColor();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyColor();
        }

        public void SetHovered(bool value)
        {
            if (hovered == value) return;
            hovered = value;
            ApplyColor();
        }
        private void ApplyColor()
        {
            if (meshRenderer == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            var color = selected
                ? SelectionColor
                : hovered ? Color.Lerp(baseColor, Color.white, 0.28f) : baseColor;
            properties.Clear();
            properties.SetColor(BaseColorId, color);
            properties.SetColor(ColorId, color);
            meshRenderer.SetPropertyBlock(properties);
        }
    }
}
