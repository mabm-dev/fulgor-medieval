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
        private Color territoryTint=Color.white;
        public void SetTerritoryTint(Color tint){territoryTint=tint;ApplyColor();}
        private bool hovered;
        private bool selected;
        private bool selectionOutline;
        private Fulgor.Core.Campaign.CampaignVisibility visibility=Fulgor.Core.Campaign.CampaignVisibility.Visible;
        public void SetFogVisibility(Fulgor.Core.Campaign.CampaignVisibility value){if(visibility==value)return;visibility=value;ApplyColor();}
        /// <summary>Visibilidad de la casilla según la niebla, la misma con la que se oscurece su color.</summary>
        public Fulgor.Core.Campaign.CampaignVisibility FogVisibility=>visibility;
        public void UseSelectionOutline(bool value){selectionOutline=value;ApplyColor();}

        public MapCell Cell { get; private set; }

        /// <summary>
        /// Indica si la casilla lleva un color propio (territorio, niebla, hover o selección).
        /// un MaterialPropertyBlock saca al renderer del SRP Batcher y del GPU Resident Drawer.
        /// </summary>
        public bool HasColorOverride => meshRenderer != null && meshRenderer.HasPropertyBlock();

        public void Initialize(MapCell cell, CampaignBoardSelection selectionOwner, Color color)
        {
            Cell = cell;
            Owner = selectionOwner;
            meshRenderer = GetComponent<MeshRenderer>();
            var shared = meshRenderer != null ? meshRenderer.sharedMaterial : null;
            baseColor = color == Color.white && shared != null && shared.HasProperty(BaseColorId)
                ? shared.GetColor(BaseColorId) : color;
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
            if (territoryTint==Color.white&&visibility==Fulgor.Core.Campaign.CampaignVisibility.Visible&&((!selected && !hovered) || selected && selectionOutline))
            {
                // En reposo el color sale del material compartido del terreno.
                meshRenderer.SetPropertyBlock(null);
                return;
            }

            if (properties == null) properties = new MaterialPropertyBlock();
            var ground=baseColor*territoryTint;
            var color = selected&&!selectionOutline ? SelectionColor : hovered?Color.Lerp(ground, Color.white, 0.28f):ground;
            if(visibility!=Fulgor.Core.Campaign.CampaignVisibility.Visible)color*=.42f;
            properties.Clear();
            properties.SetColor(BaseColorId, color);
            properties.SetColor(ColorId, color);
            meshRenderer.SetPropertyBlock(properties);
        }
    }
}
