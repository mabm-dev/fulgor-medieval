using System.Linq;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class BoardMaterialFactoryTests
    {
        private const string BaseMaterialPath = "Assets/Fulgor/Art/Materials/M_TableroBase.mat";
        private const string ScenePath = "Assets/Fulgor/Scenes/CampaignPrototype.unity";
        private const string UrpLitShader = "Universal Render Pipeline/Lit";

        [Test]
        public void BaseMaterialUsesUrpLit()
        {
            var baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(BaseMaterialPath);
            Assert.That(baseMaterial, Is.Not.Null);
            Assert.That(baseMaterial.shader.name, Is.EqualTo(UrpLitShader));
        }

        [Test]
        public void CreatesIndependentCopyWithColorAndSmoothness()
        {
            var baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(BaseMaterialPath);
            var baseColor = baseMaterial.GetColor(BoardMaterialFactory.BaseColorId);
            var color = new Color(0.42f, 0.52f, 0.29f);

            var material = BoardMaterialFactory.Create(baseMaterial, "Plain", color, 0.35f);
            try
            {
                Assert.That(material, Is.Not.SameAs(baseMaterial));
                Assert.That(material.name, Is.EqualTo("Plain"));
                Assert.That(material.shader, Is.SameAs(baseMaterial.shader));
                AssertColorApproximately(material.GetColor(BoardMaterialFactory.BaseColorId), color);
                Assert.That(material.GetFloat(BoardMaterialFactory.SmoothnessId), Is.EqualTo(0.35f).Within(1e-5f));
                AssertColorApproximately(baseMaterial.GetColor(BoardMaterialFactory.BaseColorId), baseColor);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        private static void AssertColorApproximately(Color actual, Color expected)
        {
            const float tolerance = 1e-3f;
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance));
        }

        [Test]
        public void RejectsMissingBaseMaterial()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                BoardMaterialFactory.Create(null, "Plain", Color.white, 0.1f));
        }

        [Test]
        public void CampaignSceneAssignsUrpLitBaseMaterial()
        {
            var previousScene = EditorSceneManager.GetActiveScene().path;
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var board = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<CampaignBoardPreview>(true))
                    .Single();
                Assert.That(board.BoardBaseMaterial, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(board.BoardBaseMaterial), Is.EqualTo(BaseMaterialPath));
                Assert.That(board.BoardBaseMaterial.shader.name, Is.EqualTo(UrpLitShader));
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
