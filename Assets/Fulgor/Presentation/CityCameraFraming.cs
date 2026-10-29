using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public static class CityCameraFraming
    {
        /// <summary>Closest perspective distance keeping every point inside the padded viewport.</summary>
        public static float RequiredDistance(IReadOnlyList<Vector3> points,Vector3 focus,Quaternion rotation,float fieldOfView,float aspect,float padding=.06f)
        {
            if(points==null)throw new ArgumentNullException(nameof(points));
            if(fieldOfView<=0||fieldOfView>=179||aspect<=0||padding<0||padding>=.5f)throw new ArgumentOutOfRangeException(nameof(fieldOfView));
            float vertical=Mathf.Tan(fieldOfView*.5f*Mathf.Deg2Rad)*(1-2*padding),horizontal=vertical*aspect;
            var inverse=Quaternion.Inverse(rotation);float required=.2f;
            foreach(var point in points) {
                var local=inverse*(point-focus);
                required=Mathf.Max(required,Mathf.Abs(local.x)/horizontal-local.z);
                required=Mathf.Max(required,Mathf.Abs(local.y)/vertical-local.z);
                required=Mathf.Max(required,.2f-local.z);
            }
            return required;
        }
    }
}
