using UnityEngine;
using UnityEngine.UI;
namespace Fulgor.Presentation.Map
{
    // UI geometry feathering: source portrait files remain untouched.
    public sealed class KingdomPortraitGraphic : RawImage
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(texture==null)return;var rect=GetPixelAdjustedRect();const int columns=24,rows=36;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=(float)x/columns,v=(float)y/rows;
                float edge=Mathf.SmoothStep(0,1,Mathf.Min(u,1-u)/.13f)*Mathf.SmoothStep(0,1,v/.16f)*Mathf.SmoothStep(0,1,(1-v)/.075f);
                var tint=color;tint.a*=edge;
                mesh.AddVert(new Vector3(rect.xMin+u*rect.width,rect.yMin+v*rect.height),tint,new Vector2(uvRect.x+u*uvRect.width,uvRect.y+v*uvRect.height));
            }
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int i=y*(columns+1)+x;mesh.AddTriangle(i,i+columns+1,i+1);mesh.AddTriangle(i+1,i+columns+1,i+columns+2);}
        }
    }
}
