using UnityEngine;
using UnityEngine.UI;
namespace Fulgor.Presentation.Map
{
    // Illustrative heraldry for Legends, not a certified historical flag catalogue.
    public sealed class KingdomBannerGraphic : MaskableGraphic
    {
        private void Update(){SetVerticesDirty();}
        public string Kingdom;public void SetKingdom(string id){Kingdom=id;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var gold=new Color(.88f,.7f,.3f);var red=new Color(.55f,.08f,.1f);var blue=new Color(.12f,.25f,.48f);
            void Box(float x,float y,float w,float h,Color c)
            {
                var r=rectTransform.rect;int columns=Mathf.Max(1,Mathf.CeilToInt(w*20)),rows=Mathf.Max(1,Mathf.CeilToInt(h*30));
                for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
                {
                    float left=x+w*col/columns,bottom=y+h*row/rows,right=x+w*(col+1)/columns,top=y+h*(row+1)/rows;int n=v.currentVertCount;
                    v.AddVert(new Vector3(r.xMin+left*r.width,r.yMin+bottom*r.height),c,Vector2.zero);v.AddVert(new Vector3(r.xMin+right*r.width,r.yMin+bottom*r.height),c,Vector2.zero);
                    v.AddVert(new Vector3(r.xMin+right*r.width,r.yMin+top*r.height),c,Vector2.zero);v.AddVert(new Vector3(r.xMin+left*r.width,r.yMin+top*r.height),c,Vector2.zero);
                    v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);
                }
            }
            void Cross(float x,float y,float s,Color c){Box(x-s*.15f,y-s*.5f,s*.3f,s,c);Box(x-s*.5f,y-s*.12f,s,s*.24f,c);}
            Box(0,0,1,1,color);
            if(Kingdom=="castilla"){Box(.22f,.25f,.56f,.33f,gold);Box(.2f,.25f,.16f,.49f,gold);Box(.64f,.25f,.16f,.49f,gold);Box(.42f,.25f,.16f,.58f,gold);Box(.45f,.25f,.1f,.19f,red);for(int i=0;i<3;i++)Box(.21f+i*.22f,.71f,.08f,.08f,gold);}
            else if(Kingdom=="aragon"){Box(0,0,1,1,gold);for(int i=0;i<4;i++)Box(.13f+i*.22f,0,.1f,1,red);}
            else if(Kingdom=="asturias")Cross(.5f,.53f,.6f,gold);
            else if(Kingdom=="portugal"){Box(.06f,.04f,.88f,.92f,Color.white);foreach(var p in new[]{new Vector2(.5f,.7f),new Vector2(.5f,.5f),new Vector2(.5f,.3f),new Vector2(.28f,.5f),new Vector2(.72f,.5f)})Box(p.x-.06f,p.y-.06f,.12f,.12f,blue);}
            else if(Kingdom=="navarra"){Box(.17f,.18f,.66f,.035f,gold);Box(.17f,.78f,.66f,.035f,gold);Box(.17f,.18f,.035f,.63f,gold);Box(.8f,.18f,.035f,.63f,gold);Cross(.5f,.5f,.66f,gold);Box(.45f,.45f,.1f,.1f,new Color(.1f,.45f,.25f));}
            else if(Kingdom=="almohades"){Box(0,0,1,1,red);for(int x=0;x<5;x++)for(int y=0;y<5;y++)if((x+y)%2==0)Box(.2f+x*.12f,.3f+y*.08f,.12f,.08f,Color.white);}
            else if(Kingdom=="almoravides"){Box(0,0,1,1,new Color(.88f,.85f,.72f));Box(.15f,.45f,.7f,.08f,new Color(.13f,.15f,.17f));}
            else if(Kingdom=="cordoba"){Box(0,0,1,1,new Color(.9f,.87f,.76f));Box(.1f,.2f,.8f,.035f,gold);Box(.1f,.77f,.8f,.035f,gold);}
            else if(Kingdom=="granada"){Box(0,0,1,1,red);Box(.15f,.48f,.7f,.04f,gold);Box(.45f,.3f,.1f,.35f,gold);}
            else if(Kingdom=="leon"){Box(0,0,1,1,new Color(.9f,.86f,.75f));var purple=new Color(.45f,.2f,.5f);Box(.4f,.3f,.24f,.35f,purple);Box(.55f,.62f,.2f,.14f,purple);Box(.2f,.5f,.24f,.09f,purple);Box(.3f,.22f,.12f,.24f,purple);Box(.6f,.2f,.12f,.18f,purple);Box(.22f,.55f,.06f,.25f,purple);}
            else {Box(.25f,.3f,.5f,.035f,gold);Box(.25f,.67f,.5f,.035f,gold);Cross(.5f,.5f,.3f,gold);}            var bounds=rectTransform.rect;
            // A woven border, a forked hem and a slow wind deformation share the emblem mesh.
            Box(.025f,.025f,.025f,.95f,gold);Box(.95f,.025f,.025f,.95f,gold);Box(.025f,.95f,.95f,.025f,gold);Box(.025f,.025f,.95f,.025f,gold);
            for(int i=0;i<v.currentVertCount;i++)
            {
                UIVertex vertex=default;v.PopulateUIVertex(ref vertex,i);float u=(vertex.position.x-bounds.xMin)/bounds.width, t=(vertex.position.y-bounds.yMin)/bounds.height;
                float fold=Mathf.Cos(u*14-Time.unscaledTime*.8f);float weave=Mathf.Sin(u*600)*Mathf.Sin(t*850)*.025f;float shade=.82f+.16f*fold+weave;
                vertex.color=new Color32((byte)(vertex.color.r*shade),(byte)(vertex.color.g*shade),(byte)(vertex.color.b*shade),vertex.color.a);
                vertex.position.x+=Mathf.Sin(t*7+Time.unscaledTime*.8f)*5*u;
                vertex.position.y+=Mathf.Sin(u*8+Time.unscaledTime)*3*u+Mathf.Max(0,1-t*12)*Mathf.Max(0,1-Mathf.Abs(u-.5f)*2)*22;
                v.SetUIVertex(vertex,i);
            }
        }
    }
}


