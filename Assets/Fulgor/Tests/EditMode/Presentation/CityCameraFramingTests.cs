using System;
using System.Collections.Generic;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CityCameraFramingTests
    {
        [TestCase(0,1.44f)][TestCase(60,1.44f)][TestCase(120,1.44f)][TestCase(180,1.44f)][TestCase(240,1.44f)][TestCase(300,1.44f)][TestCase(45,.75f)][TestCase(225,2.2f)]
        public void PerspectiveFitPreservesMarginAtDifferentAnglesAndAspectRatios(float yaw,float aspect)
        {
            var points=new List<Vector3>();
            for(int x=-1;x<=1;x+=2)for(int y=0;y<=1;y++)for(int z=-1;z<=1;z+=2)points.Add(new Vector3(x*4,y*3,z*4));
            var offset=Quaternion.Euler(0,yaw,0)*new Vector3(0,Mathf.Sin(31*Mathf.Deg2Rad),-Mathf.Cos(31*Mathf.Deg2Rad));var rotation=Quaternion.LookRotation(-offset);
            var focus=new Vector3(0,.55f,0);float distance=CityCameraFraming.RequiredDistance(points,focus,rotation,42,aspect);
            foreach(var point in points) {
                var p=Quaternion.Inverse(rotation)*(point-focus);p.z+=distance;
                Assert.That(p.z,Is.GreaterThan(0));
                Assert.That(Mathf.Abs(p.x)/(p.z*Mathf.Tan(21*Mathf.Deg2Rad)*aspect),Is.LessThanOrEqualTo(.8801f));
                Assert.That(Mathf.Abs(p.y)/(p.z*Mathf.Tan(21*Mathf.Deg2Rad)),Is.LessThanOrEqualTo(.8801f));
            }
        }
        [Test]public void InvalidProjectionIsRejected()
        {
            Assert.Throws<ArgumentNullException>(()=>CityCameraFraming.RequiredDistance(null,Vector3.zero,Quaternion.identity,42,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CityCameraFraming.RequiredDistance(Array.Empty<Vector3>(),Vector3.zero,Quaternion.identity,0,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CityCameraFraming.RequiredDistance(Array.Empty<Vector3>(),Vector3.zero,Quaternion.identity,42,0));
        }
    }
}
