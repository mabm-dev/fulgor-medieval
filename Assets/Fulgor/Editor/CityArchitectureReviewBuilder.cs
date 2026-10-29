using System;
using System.IO;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    public static class CityArchitectureReviewBuilder
    {
        public static void Batch(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);CityConstructionReviewBuilder.Batch();}
        public static void CaptureDetails(string output)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fulgor/Prefabs/City/PF_CastillaCity.prefab");
            var root=new GameObject("Detalle de arquitectura");root.layer=31;root.transform.position=new Vector3(15000,0,15000);
            var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.SetColor("_BaseColor",new Color(.25f,.235f,.20f));groundMaterial.SetFloat("_Smoothness",.03f);
            var target=new RenderTexture(1800,1000,24,RenderTextureFormat.ARGB32);target.Create();var previous=RenderTexture.active;
            try {
                string[] labels={"Vivienda 1","Vivienda 2","Mercado"};
                for(int i=0;i<labels.Length;i++) {
                    var building=source.GetComponentsInChildren<CityBuildingView>(true).First(b=>b.DisplayName==labels[i]);
                    var copy=Object.Instantiate(building.gameObject,root.transform);copy.name=labels[i];copy.SetActive(true);
                    copy.transform.localPosition=new Vector3((i-1)*1.55f,0,0);copy.transform.localRotation=Quaternion.identity;copy.transform.localScale=Vector3.one;
                }
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Suelo de estudio";floor.transform.SetParent(root.transform,false);floor.transform.localPosition=new Vector3(0,-.03f,0);floor.transform.localScale=new Vector3(12,.05f,10);floor.GetComponent<MeshRenderer>().sharedMaterial=groundMaterial;
                foreach(var item in root.GetComponentsInChildren<Transform>(true))item.gameObject.layer=31;
                foreach(var collider in root.GetComponentsInChildren<Collider>())collider.enabled=false;
                var finish=root.AddComponent<CityVisualFinish>();
                var finishData=new SerializedObject(finish);finishData.FindProperty("authoredMaterials").objectReferenceValue=AssetDatabase.LoadAssetAtPath<CityFinishMaterials>("Assets/Fulgor/Art/City/CastillaFinish/CastillaFinish.asset");finishData.ApplyModifiedPropertiesWithoutUndo();finish.Apply();
                var lightRoot=new GameObject("Luz de detalle");lightRoot.transform.SetParent(root.transform,false);var light=lightRoot.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(1,.95f,.87f);light.shadows=LightShadows.Soft;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(43,-32,0);
                var cameraRoot=new GameObject("Camara de detalle");cameraRoot.transform.SetParent(root.transform,false);var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=39;camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.aspect=1.8f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.20f,.22f,.20f);camera.allowHDR=false;
                for(int side=0;side<2;side++) {
                    camera.transform.position=root.transform.position+new Vector3(side==0?2.8f:-2.8f,2.15f,side==0?-5.3f:5.3f);camera.transform.LookAt(root.transform.position+new Vector3(0,.65f,0));
                    RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;
                    var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"Arquitectura_Detalle_"+side+".png"),image.EncodeToPNG());Object.DestroyImmediate(image);
                }
            }finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(root);Object.DestroyImmediate(groundMaterial);}
        }
    }
}
