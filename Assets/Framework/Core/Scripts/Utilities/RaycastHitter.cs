using System;

using UnityEngine;

namespace RTSEngine.Utilities
{
    public class RaycastHitter
    {
        private LayerMask mask;

        public RaycastHitter(LayerMask mask)
        {
            this.mask = mask;
        }

        public bool Hit(Ray ray, out RaycastHit hit)
        {
            return Physics.Raycast(ray, out hit, Mathf.Infinity, mask);
        }

        public Vector3 Hit(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mask))
                return hit.point;

            RTSHelper.LoggingService.LogError($"[RaycastHitter] Unable to raycast hit target mask. If this is happening at or near the edge of the map with a zoomed out camera then this is usually caused by the camera borders not being able to cast a ray that hits the base terrain. Please follow error trace to see where this request is coming from.");
            return Vector3.zero;
        }
    }
}
