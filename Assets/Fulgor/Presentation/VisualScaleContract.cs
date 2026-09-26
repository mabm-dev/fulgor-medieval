using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Contrato compartido por Unity, Blender y los recursos del mapa.</summary>
    public static class VisualScaleContract
    {
        public const float HexOuterRadius = 1f;
        public const float HexInnerRadius = 0.8660254f;
        public const float HexWidth = HexInnerRadius * 2f;
        public const float HexDepth = HexOuterRadius * 2f;
        public const float HexHorizontalSpacing = HexWidth;
        public const float HexVerticalSpacing = HexOuterRadius * 1.5f;

        public const float PropSafeRadius = 0.68f;
        public const float SettlementMaximumFootprint = 1.18f;
        public const float SettlementReferenceHeight = 0.82f;
        public const float ArmyReferenceHeight = 0.55f;
        public const float ResourceMarkerHeight = 0.08f;

        public static readonly Vector3 AssetPivot = Vector3.zero;

        public static bool FitsInsideTile(float footprintDiameter) =>
            footprintDiameter > 0f && footprintDiameter <= PropSafeRadius * 2f;
    }
}
