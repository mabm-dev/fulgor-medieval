using System.Linq;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CityAuthoredMaterialImportTests
    {
        const string Art="Assets/Fulgor/Art/City/CastillaFinish/";
        [TestCase("CasaCal")][TestCase("CasaAlta")][TestCase("Mercado")]
        public void ArchitecturalOcclusionHasMatchingUvAndLinearTexture(string model)
        {
            var mesh=AssetDatabase.LoadAllAssetsAtPath("Assets/Fulgor/Art/City/CastillaView/SM_Villa_"+model+".fbx").OfType<Mesh>().First();
            Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
            Assert.That(mesh.uv.Distinct().Count(),Is.GreaterThan(100));
            Assert.That(mesh.colors.Length,Is.EqualTo(mesh.vertexCount));
            string path=Art+"SM_Villa_"+model+"_AO.png";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);Assert.That(texture,Is.Not.Null);
            Assert.That(texture.width,Is.EqualTo(1024));Assert.That(((TextureImporter)AssetImporter.GetAtPath(path)).sRGBTexture,Is.False);
        }
        [Test]public void SubstanceDataMapsHaveCorrectImportColourSpaces()
        {
            var library=AssetDatabase.LoadAssetAtPath<CityFinishMaterials>(Art+"CastillaFinish.asset");Assert.That(library,Is.Not.Null);
            foreach(var material in new[]{library.Limestone,library.Plaster,library.Oak,library.Terracotta}){
                Assert.That(material,Is.Not.Null);
                foreach(string property in new[]{"_DetailMap","_DetailNormal","_RoughnessMap"}){
                    var texture=material.GetTexture(property);Assert.That(texture,Is.Not.Null,material.name+property);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                    Assert.That(importer.sRGBTexture,Is.EqualTo(property=="_DetailMap"));
                    Assert.That(importer.textureType,Is.EqualTo(property=="_DetailNormal"?TextureImporterType.NormalMap:TextureImporterType.Default));
                }
            }
        }
    }
}
