using RTSEngine.Model;
using UnityEngine;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public class AttackWeapon : AttackSubComponent
    {
        #region Attributes
        [SerializeField, Tooltip("Objects, other than the main weapon object, enabled when the attack type is activated and hidden otherwise.")]
        private ModelCacheAwareTransformInput[] toggableObjects = new ModelCacheAwareTransformInput[0];

        [SerializeField, Tooltip("Allow to update the weapon rotation?")]
        private bool updateRotation = true;

        [SerializeField, Tooltip("Only rotate the weapon when the target is inside the attacking range?")]
        private bool rotateInRangeOnly = false;

        //to the freeze the weapon's rotation on the Y axis then you should enable freezeRotationX and freezeRotationZ
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the X axis.")]
        private bool freezeRotationX = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Y axis.")]
        private bool freezeRotationY = false;
        [SerializeField, Tooltip("Freeze calculating rotation in the look at vector on the Z axis.")]
        private bool freezeRotationZ = false;

        [SerializeField, Tooltip("Is the weapon's object rotation smooth?")]
        private bool smoothRotation = true;
        [SerializeField, Tooltip("How smooth is the weapon's rotation? Only if smooth rotation is enabled!")]
        private float rotationDamping = 2.0f;

        [SerializeField, Tooltip("Force the weapon object to get back to an idle rotation when the attacker does not have an active target?")]
        private bool forceIdleRotation = true;
        [SerializeField, Tooltip("In case idle rotation is enabled, this represents the idle rotation euler angles.")]
        private Vector3 idleAngles = Vector3.zero;
        // Used to store the weapon's idle rotation so it is not calculated everytime through its euler angles
        private Quaternion idleRotation = Quaternion.identity;
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            if (SourceAttackComp.WeaponTransform.IsValid())
                idleRotation = Quaternion.Euler(idleAngles);

            for (int i = 0; i < toggableObjects.Length; i++)
                if (!toggableObjects[i].IsValid())
                    logger.LogError($"[AttackWeapon - {SourceAttackComp.Code} - {SourceAttackComp.Entity.Code}] The Toggable Objects array field in the Attack Weapon tab includes one or more unassigned or incorrectly assigned elements!", source: SourceAttackComp);
        }

        public void Toggle(bool enable)
        {
            for (int i = 0; i < toggableObjects.Length; i++)
                toggableObjects[i].IsActive = enable;

            if(SourceAttackComp.WeaponTransform.IsValid())
                SourceAttackComp.WeaponTransform.IsActive = enable;
        }
        #endregion

        #region Handling Active/Idle Rotation
        public void Update()
        {
            if (!updateRotation
                || !SourceAttackComp.WeaponTransform.IsValid())
                return;

            //if the attacker does not have an active target
            //or it does but we are not allowed to start weapon rotation until the target is in range
            if (!SourceAttackComp.HasTarget
                || (!SourceAttackComp.IsInTargetRange && rotateInRangeOnly))
                UpdateIdleRotation();
            else
                UpdateActiveRotation();
        }

        public void UpdateIdleRotation ()
        {
            //can not force idle rotation, stop here
            if (!forceIdleRotation)
                return;

            SourceAttackComp.WeaponTransform.LocalRotation = smoothRotation
                ? Quaternion.Slerp(SourceAttackComp.WeaponTransform.LocalRotation, idleRotation, Time.deltaTime * rotationDamping)
                : idleRotation;
        }

        public void UpdateActiveRotation ()
        {
            Vector3 lookAt = RTSHelper.GetAttackTargetPosition(SourceAttackComp.Target) - SourceAttackComp.WeaponTransform.Position;

            //which axis should not be rotated? 
            if (freezeRotationX == true)
                lookAt.x = 0.0f;
            if (freezeRotationY == true)
                lookAt.y = 0.0f;
            if (freezeRotationZ == true)
                lookAt.z = 0.0f;

            Quaternion targetRotation = Quaternion.LookRotation(lookAt);
            if (smoothRotation == false) //make the weapon instantly look at target
                SourceAttackComp.WeaponTransform.Rotation = targetRotation;
            else //smooth rotation
                SourceAttackComp.WeaponTransform.Rotation = Quaternion.Slerp(SourceAttackComp.WeaponTransform.Rotation, targetRotation, Time.deltaTime * rotationDamping);
        }
        #endregion
    }
}
