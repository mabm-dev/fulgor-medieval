using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Maps a castle hit back to its logical tile and shows its selected footprint.</summary>
    public sealed class CampaignSettlementView : MonoBehaviour
    {
        private LineRenderer outline;
        private Material material;
        private BoxCollider pickCollider;
        private LineRenderer control;
        private Material controlMaterial;
        public HexTileView Tile { get; private set; }
        public bool Selected=>outline!=null&&outline.enabled;
        public void Initialize(HexTileView tile)
        {
            Tile=tile;Tile.UseSelectionOutline(true);
            var bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())bounds.Encapsulate(renderer.bounds);
            pickCollider=gameObject.AddComponent<BoxCollider>();
            pickCollider.center=transform.InverseTransformPoint(bounds.center);
            pickCollider.size=new Vector3(bounds.size.x/transform.lossyScale.x,bounds.size.y/transform.lossyScale.y,bounds.size.z/transform.lossyScale.z);
            var root=new GameObject("Asentamiento seleccionado");root.transform.SetParent(transform,false);
            outline=root.AddComponent<LineRenderer>();outline.useWorldSpace=true;outline.loop=true;outline.positionCount=48;
            outline.startWidth=outline.endWidth=.018f;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(1,.72f,.12f));outline.sharedMaterial=material;
            outline.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;outline.receiveShadows=false;
            for(int i=0;i<48;i++) {
                float angle=i*Mathf.PI*2/48;outline.SetPosition(i,new Vector3(tile.transform.position.x+Mathf.Cos(angle)*.71f,transform.position.y+.008f,tile.transform.position.z+Mathf.Sin(angle)*.71f));
            }
            outline.enabled=false;
        }
        public void SetSelected(bool selected){if(outline!=null)outline.enabled=selected;}
        public void SetOwner(bool own)
        {
            if(control==null)
            {
                var root=new GameObject("Control de la fortaleza");root.transform.SetParent(transform,false);
                control=root.AddComponent<LineRenderer>();control.useWorldSpace=true;control.loop=true;control.positionCount=48;
                control.startWidth=control.endWidth=.012f;
                controlMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));control.sharedMaterial=controlMaterial;
                control.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;control.receiveShadows=false;
                for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;control.SetPosition(i,new Vector3(Tile.transform.position.x+Mathf.Cos(a)*.66f,transform.position.y+.012f,Tile.transform.position.z+Mathf.Sin(a)*.66f));}
            }
            controlMaterial.SetColor("_BaseColor",own?new Color(.15f,.48f,.92f):new Color(.82f,.23f,.17f));
        }
        private static void Release(Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        private void OnDestroy(){if(Tile!=null)Tile.UseSelectionOutline(false);Release(material);Release(controlMaterial);if(outline!=null)Release(outline.gameObject);if(control!=null)Release(control.gameObject);Release(pickCollider);}
    }
}
