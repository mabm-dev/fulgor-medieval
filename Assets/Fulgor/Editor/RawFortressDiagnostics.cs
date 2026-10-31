using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class RawFortressDiagnostics
    {
        const string Request="Temp/FulgorFortressDiagnostic.request";
        static RawFortressDiagnostics(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
            File.Delete(Request);
            var camera=Camera.main;
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fulgor/Art/RawFortress/M_Fortaleza_completa_bruta.mat");
            if(camera==null||material==null)return;
            var position=camera.transform.position;var rotation=camera.transform.rotation;var fov=camera.fieldOfView;var aspect=camera.aspect;
            bool normal=material.IsKeywordEnabled("_NORMALMAP");
            try
            {
                Directory.CreateDirectory("Builds/RawFortressReview");camera.fieldOfView=30;camera.aspect=1.4f;
                foreach(bool enabled in new[]{true,false})
                {
                    if(enabled)material.EnableKeyword("_NORMALMAP");else material.DisableKeyword("_NORMALMAP");
                    camera.transform.position=new Vector3(17,19,-22);camera.transform.LookAt(new Vector3(0,11,0));Capture(camera,"Torre_"+(enabled?"normal":"sin_normal"));
                    camera.transform.position=new Vector3(25,12,-6);camera.transform.LookAt(new Vector3(6,4,0));Capture(camera,"Muro_"+(enabled?"normal":"sin_normal"));
                }
                File.WriteAllText("Builds/RawFortressReview/diagnostic-status.txt","OK");
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;camera.aspect=aspect;if(normal)material.EnableKeyword("_NORMALMAP");else material.DisableKeyword("_NORMALMAP");}
        }
        static void Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1400,1000,24);rt.Create();var previous=RenderTexture.active;var image=new Texture2D(1400,1000,TextureFormat.RGBA32,false);
            try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes("Builds/RawFortressReview/"+name+".png",image.EncodeToPNG());}
            finally{RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
    }
}
