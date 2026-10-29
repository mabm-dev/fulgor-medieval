using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CityArchitectureImportTests
    {
        [TestCase("CasaCal",1.18f,1.40f,1.07f)]
        [TestCase("CasaAlta",1.01f,1.58f,1.14f)]
        [TestCase("Mercado",1.48f,1.32f,1.52f)]
        public void RefinedArchitectureFitsItsExistingEnvelopeAndUsesExpectedMaterialSlots(string model,float width,float height,float depth)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fulgor/Art/City/CastillaView/SM_Villa_"+model+".fbx");Assert.That(root,Is.Not.Null);
            var renderer=root.GetComponentsInChildren<MeshRenderer>().Single();var bounds=renderer.bounds;
            Assert.That(bounds.size.x,Is.InRange(width*.94f,width+.003f));Assert.That(bounds.size.y,Is.InRange(height*.94f,height+.003f));Assert.That(bounds.size.z,Is.InRange(depth*.94f,depth+.003f));
            string[] materialNames={"M_Villa_Piedra","M_Villa_Cal","M_Villa_Madera","M_Villa_Oscuro","M_Villa_Hierro","M_Villa_Teja"};
            if(model=="Mercado")materialNames=materialNames.Concat(new[]{"M_Villa_Tela"}).ToArray();
            Assert.That(renderer.sharedMaterials.Select(m=>m.name),Is.EqualTo(materialNames));
            var mesh=root.GetComponentInChildren<MeshFilter>().sharedMesh;
            long triangles=0;for(int i=0;i<mesh.subMeshCount;i++)triangles+=(long)mesh.GetIndexCount(i)/3;
            Assert.That(triangles,Is.InRange(3000,8000));
        }
    }
}
