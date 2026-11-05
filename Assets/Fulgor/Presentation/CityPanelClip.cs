using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Stencil silhouette for the panel artwork; decorative corners remain transparent.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CityPanelClip : MaskableGraphic
    {
        public float Corner=16f;
        public float LeftSlant;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();float l=r.xMin+2,rr=r.xMax-2,b=r.yMin+2,h=r.yMax-2;
            float c=Mathf.Min(Corner,Mathf.Min(r.width,r.height)*.25f);
            var points=LeftSlant>0?new[]{new Vector2(l,h),new Vector2(rr-c,h),new Vector2(rr,h-c),new Vector2(rr,b+c),new Vector2(rr-c,b),new Vector2(l+LeftSlant,b),new Vector2(l+LeftSlant*.45f,b+r.height*.5f)}:
                new[]{new Vector2(l+c,h),new Vector2(rr-c,h),new Vector2(rr,h-c),new Vector2(rr,b+c),new Vector2(rr-c,b),new Vector2(l+c,b),new Vector2(l,b+c),new Vector2(l,h-c)};
            mesh.AddVert(r.center,Color.white,Vector2.zero);
            foreach(var point in points)mesh.AddVert(point,Color.white,Vector2.zero);
            for(int i=0;i<points.Length;i++)mesh.AddTriangle(0,1+i,1+(i+1)%points.Length);
        }
    }
}
