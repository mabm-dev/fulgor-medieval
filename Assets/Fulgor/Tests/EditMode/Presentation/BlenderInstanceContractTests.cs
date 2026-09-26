using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class BlenderInstanceContractTests
    {
        private const string AssetPath = "Assets/Fulgor/Art/Validation/SM_VisualScaleValidation.fbx";
        private const float Tolerance = 0.001f;

        [Test]
        public void UsesStaticModelImportPolicy()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(AssetPath);
            Assert.That(importer.globalScale, Is.EqualTo(1f));
            Assert.That(importer.useFileScale, Is.True);
            Assert.That(importer.bakeAxisConversion, Is.True);
            Assert.That(importer.importAnimation, Is.False);
            Assert.That(importer.importCameras, Is.False);
            Assert.That(importer.importLights, Is.False);
            Assert.That(importer.addCollider, Is.False);
            Assert.That(importer.meshCompression, Is.EqualTo(ModelImporterMeshCompression.Off));
        }

        [TestCase("SM_Hex_Referencia", 1.7320508f, 0f, 2f)]
        [TestCase("REF_Fortaleza_1.18x0.82", 1.18f, 0.82f, 1.18f)]
        [TestCase("REF_Hueste_0.45x0.55", 0.45f, 0.55f, 0.45f)]
        public void TemporaryInstanceHasCorrectDimensionsAndBottomPivot(
            string meshName, float width, float height, float depth)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath));
            try
            {
                Assert.That(instance.transform.localScale, Is.EqualTo(Vector3.one));
                var filter = instance.GetComponentsInChildren<MeshFilter>()
                    .Single(candidate => candidate.sharedMesh.name == meshName);
                var bounds = filter.GetComponent<MeshRenderer>().bounds;
                TestContext.WriteLine($"{meshName}: rotation={filter.transform.rotation.eulerAngles}, scale={filter.transform.lossyScale}, mesh={filter.sharedMesh.bounds}");
                Assert.That(bounds.size.x, Is.EqualTo(width).Within(Tolerance));
                Assert.That(bounds.size.y, Is.EqualTo(height).Within(Tolerance));
                Assert.That(bounds.size.z, Is.EqualTo(depth).Within(Tolerance));
                Assert.That(bounds.min.y, Is.EqualTo(filter.transform.position.y).Within(Tolerance));
                Assert.That(bounds.center.x, Is.EqualTo(filter.transform.position.x).Within(Tolerance));
                Assert.That(bounds.center.z, Is.EqualTo(filter.transform.position.z).Within(Tolerance));
                Assert.That(filter.transform.lossyScale.x, Is.EqualTo(1f).Within(Tolerance));
                Assert.That(filter.transform.lossyScale.y, Is.EqualTo(1f).Within(Tolerance));
                Assert.That(filter.transform.lossyScale.z, Is.EqualTo(1f).Within(Tolerance));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}