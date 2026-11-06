using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Rasterizes dynamic glyphs at higher density while preserving the UI layout in canvas units.</summary>
    public sealed class CampaignSharpText : Text
    {
        private readonly UIVertex[] quad = new UIVertex[4];

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            if (font == null || !font.dynamic)
            {
                base.OnPopulateMesh(mesh);
                return;
            }

            var settings = GetGenerationSettings(rectTransform.rect.size);
            settings.scaleFactor = Mathf.Max(2f, settings.scaleFactor * 2f);
            m_DisableFontTextureRebuiltCallback = true;
            try
            {
                cachedTextGenerator.Populate(text, settings);
                var vertices = cachedTextGenerator.verts;
                mesh.Clear();
                if (vertices.Count == 0) return;
                Vector2 first = vertices[0].position / settings.scaleFactor;
                Vector3 offset = PixelAdjustPoint(first) - first;
                for (int i = 0; i < vertices.Count; i++)
                {
                    int corner = i & 3;
                    quad[corner] = vertices[i];
                    quad[corner].position = vertices[i].position / settings.scaleFactor + offset;
                    if (corner == 3) mesh.AddUIVertexQuad(quad);
                }
            }
            finally
            {
                m_DisableFontTextureRebuiltCallback = false;
            }
        }
    }
}
