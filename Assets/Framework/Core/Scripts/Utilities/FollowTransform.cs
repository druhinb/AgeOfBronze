using System;

using UnityEngine;
using UnityEngine.Assertions;

using RTSEngine.Model;

namespace RTSEngine.Utilities
{
    /// <summary>
    /// Allows a 'source' transform to follow the position and/or rotation of a 'target' ModelCacheAwareTransformInput.
    /// </summary>
    public class FollowTransform
    {
        #region Attributes
        private readonly Transform source;
        private bool canFollowPosition;
        private bool canFollowRotation;

        // Callback called when the target is invalid to alter the source class implemeting this.
        private bool enableCallback;
        private readonly Action targetInvalidCallback;

        // Target that the source transform will be following
        private ModelCacheAwareTransformInput target = null;
        private Vector3 offset = Vector3.zero;
        private Action onFollowTargetInvalid;

        public bool HasTarget => target != null;
        #endregion

        #region Constructor
        public FollowTransform(Transform source, Action targetInvalidCallback)
        {
            this.source = source;
            Assert.IsNotNull(this.source,
                $"[{GetType()}] A valid 'source' Transform must be provided!");

            this.targetInvalidCallback = targetInvalidCallback;
        }
        #endregion

        #region Setting Target
        public void ResetTarget()
            => SetTarget(null, offset: Vector3.zero, enableCallback: false);

        public void SetTarget(ModelCacheAwareTransformInput target, bool enableCallback, bool followPosition = true, bool followRotation = false)
            => SetTarget(target, Vector3.zero, enableCallback, followPosition, followRotation);

        public void SetTarget(ModelCacheAwareTransformInput target, Vector3 offset, bool enableCallback, bool followPosition = true, bool followRotation = false)
        {
            this.target = target;
            this.offset = offset;

            this.enableCallback = enableCallback;

            this.canFollowPosition = followPosition;
            this.canFollowRotation = followRotation;
        }
        #endregion

        #region Updating Position/Rotation
        public void Update()
        {
            if (!target.IsValid())
            {
                if(enableCallback && targetInvalidCallback.IsValid())
                    targetInvalidCallback();

                return;
            }

            if(canFollowPosition)
                source.position = target.Position + offset;

            if (canFollowRotation)
                source.rotation = RTSHelper.GetLookRotation(source, target.Position);
        }
        #endregion
    }
}
