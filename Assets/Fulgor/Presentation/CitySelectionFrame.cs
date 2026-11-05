using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Fine gilded inner frame with clipped corners matching the building slots.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CitySelectionFrame : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Stroke(vertices,2f,1.4f,new Color(.64f,.38f,.10f,1f));
            Stroke(vertices,4.5f,1.2f,new Color(1f,.84f,.44f,1f));
        }
        private void Stroke(VertexHelper vertices,float inset,float width,Color tint)
        {
            var r=GetPixelAdjustedRect();
            if(r.width<inset*2+14||r.height<inset*2+14)return;
            var outer=Corners(r,inset);var inner=Corners(r,inset+width);
            int start=vertices.currentVertCount;
            for(int i=0;i<8;i++)
            {
                vertices.AddVert(outer[i],tint,Vector2.zero);
                vertices.AddVert(inner[i],tint,Vector2.zero);
            }
            for(int i=0;i<8;i++)
            {
                int a=start+i*2,b=start+((i+1)%8)*2;
                vertices.AddTriangle(a,b,a+1);vertices.AddTriangle(a+1,b,b+1);
            }
        }
        private static Vector2[] Corners(Rect r,float inset)
        {
            float l=r.xMin+inset,h=r.yMax-inset,rr=r.xMax-inset,b=r.yMin+inset,c=7f;
            return new[]{new Vector2(l+c,h),new Vector2(rr-c,h),new Vector2(rr,h-c),new Vector2(rr,b+c),new Vector2(rr-c,b),new Vector2(l+c,b),new Vector2(l,b+c),new Vector2(l,h-c)};
        }
    }
}
