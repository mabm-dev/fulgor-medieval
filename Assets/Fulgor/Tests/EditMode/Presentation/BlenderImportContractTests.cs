using System.Linq;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class BlenderImportContractTests
    {
        private const string AssetPath =
            "Assets/Fulgor/Art/Validation/SM_VisualScaleValidation.fbx";
        private const float Tolerance = 0.001f;

        [Test]
        public void ImportsValidationFbx()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath), Is.Not.Null);
        }

        [Test]
        public void ImportsHexAtContractSizeAndOrientation()
        {
            var bounds = LoadBounds("SM_Hex_Referencia");
            Assert.That(bounds.size.x, Is.EqualTo(VisualScaleContract.HexWidth).Within(Tolerance));
            Assert.That(bounds.size.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(bounds.size.z, Is.EqualTo(VisualScaleContract.HexDepth).Within(Tolerance));
        }

        [Test]
        public void ImportsFortressAtReferenceSize()
        {
            var size = LoadBounds("REF_Fortaleza_1.18x0.82").size;
            Assert.That(size.x, Is.EqualTo(1.18f).Within(Tolerance));
            Assert.That(size.y, Is.EqualTo(0.82f).Within(Tolerance));
            Assert.That(size.z, Is.EqualTo(1.18f).Within(Tolerance));
        }

        [Test]
        public void ImportsArmyAtReferenceSize()
        {
            var size = LoadBounds("REF_Hueste_0.45x0.55").size;
            Assert.That(size.x, Is.EqualTo(0.45f).Within(Tolerance));
            Assert.That(size.y, Is.EqualTo(0.55f).Within(Tolerance));
            Assert.That(size.z, Is.EqualTo(0.45f).Within(Tolerance));
        }

        // The FBX may keep axis conversion on a node. The contract describes
        // the instantiated resource in Unity axes, not raw mesh-local bounds.
        private static Bounds LoadBounds(string meshName)
        {
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath));
            try
            {
                var filter = instance.GetComponentsInChildren<MeshFilter>()
                    .FirstOrDefault(candidate => candidate.sharedMesh.name == meshName);
                Assert.That(filter, Is.Not.Null, $"No se importó la malla {meshName}");
                return filter.GetComponent<MeshRenderer>().bounds;
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}