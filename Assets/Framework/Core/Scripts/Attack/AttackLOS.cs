using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackLOS : AttackSubComponent
    {
        #region Attributes
        [SerializeField, Tooltip("Can the attacker engage only if the target is in its line of sight?")]
        private bool enabled = true; 

        [SerializeField, Tooltip("Enable to use the weapon object's rotation for the line of sight calculations instead of the main attcker object.")]
        private bool useWeaponObject = false; 

        [SerializeField, Tooltip("How wide is the line of sight angle (in degrees) of the attacker? the less, the closer the attacker must face its target to engage it."), Min(0)]
        private float angle = 40.0f; 

        [SerializeField, Tooltip("Define layers for obstacles that block the line of sight.")]
        private LayerMask obstacleLayerMask = new LayerMask();

        // Ignore one or more axis while considering LOS?
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the X axis.")]
        private bool ignoreRotationX = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Y axis.")]
        private bool ignoreRotationY = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Z axis.")]
        private bool ignoreRotationZ = false;
        #endregion

        #region Handling Angle and Obstacle LOS
        public ErrorMessage IsInSight (TargetData<IFactionEntity> target, bool ignoreAngle = false, bool ignoreObstacle = false)
        {
            if (!enabled)
                return ErrorMessage.none;

            bool useWeaponObj = useWeaponObject && SourceAttackComp.WeaponTransform.IsValid();
            // Use the weapon object or the attacker's object as the reference for the line of sight:
            Vector3 sourcePosition = useWeaponObj
                ? SourceAttackComp.WeaponTransform.Position 
                : SourceAttackComp.Entity.transform.position;

            Quaternion sourceRotation = useWeaponObj 
                ? SourceAttackComp.WeaponTransform.Rotation 
                : SourceAttackComp.Entity.transform.rotation;

            Vector3 targetPosition = RTSHelper.GetAttackTargetPosition(target);

            if (!ignoreAngle && IsAngleBlocked(sourcePosition, sourceRotation, targetPosition))
                return ErrorMessage.LOSAngleBlocked;
            else if (!ignoreObstacle && IsObstacleBlocked(sourcePosition, targetPosition))
                return ErrorMessage.LOSObstacleBlocked;

            return ErrorMessage.none;
        }

        public bool IsAngleBlocked (Vector3 sourcePosition, Quaternion sourceRotation, Vector3 targetPosition)
        {
            if (!enabled) 
                return true;

            Vector3 lookAt = targetPosition - sourcePosition;

            // Which axis to ignore when checking for LOS?
            if (ignoreRotationX == true)
                lookAt.x = 0.0f;
            if (ignoreRotationY == true)
                lookAt.y = 0.0f;
            if (ignoreRotationZ == true)
                lookAt.z = 0.0f;

            // if the angle is below the allowed LOS Angle then the attacker is in line of sight of the target
            return Vector3.Angle(sourceRotation * Vector3.forward, lookAt) >= angle;
        }

        public bool IsObstacleBlocked (Vector3 sourcePosition, Vector3 targetPosition)
        {
            if (!enabled)
                return false;

            return Physics.Linecast(sourcePosition, targetPosition, obstacleLayerMask);
        }
        #endregion
    }
}
