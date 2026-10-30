using System;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class FulgorIconsTests
    {
        [Test]
        public void EveryIconLoadsAsSprite()
        {
            foreach (FulgorIcon icon in Enum.GetValues(typeof(FulgorIcon)))
            {
                var sprite = FulgorIcons.Get(icon);
                Assert.That(sprite, Is.Not.Null, FulgorIcons.File(icon));
                Assert.That(sprite.rect.width, Is.EqualTo(128f), FulgorIcons.File(icon));
            }
        }

        [Test]
        public void FramesAreNineSliced()
        {
            Assert.That(FulgorIcons.PanelFrame.border, Is.EqualTo(new Vector4(24f, 24f, 24f, 24f)));
            Assert.That(FulgorIcons.ChipFrame.border.x, Is.GreaterThan(0f));
        }
    }
}
