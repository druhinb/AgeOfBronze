﻿using UnityEngine;

using RTSEngine.Selection;
using RTSEngine.Entities;
using RTSEngine.Effect;
using RTSEngine.Movement;
using RTSEngine.Determinism;
using RTSEngine.Audio;
using RTSEngine.Utilities;
using RTSEngine.Model;
using RTSEngine.Terrain;
using RTSEngine.Event;
using System;

namespace RTSEngine.Attack
{
    public class AttackObject : EffectObject, IAttackObject
    {
        #region Class Attributes
        public AttackObjectSpawnInput Data { get; private set; }

        [SerializeField, Tooltip("Determines the movement path of the attack object where t=0 is the initial spawn position and t=1 is the target position.")]
        private MovementCurve mainMvtCurve = new MovementCurve { minDistance = 2.0f, heightMultiplier = 1.0f };

        [SerializeField, Tooltip("Define an alternative movement path for the attack object when the distance to target is lower than a certain max value.")]
        private MovementCurve altMvtCurve = new MovementCurve { minDistance = 0.0f, heightMultiplier = 1.0f };

        // The curve used during the movement is chosen when the attack object is enabled depending on the initial distance.
        private MovementCurve nextMvtCurve;

        private Vector3 mvtDirection = Vector3.zero;

        private float initialDistance = 0.0f;
        // Incremented with the same movement speed to determine the movement height extracted from the above curve.
        private float incrementalDistance = 0.0f;

        // Helper attribute that handles attack object movement before applying the heightMultiplier
        private Vector3 nextPosition = Vector3.zero;
        // Helper attribute that holds the height of the attack object during its movement
        private float nextHeight = 0.0f;

        private Vector3 lookAtPosition;

        [SerializeField, Tooltip("How fast does the attack object move?")]
        private TimeModifiedFloat speed = new TimeModifiedFloat(10.0f);

        [SerializeField, Tooltip("Can the attack object follow its target if it's moving?")]
        private bool followTarget = false;
        [SerializeField, Tooltip("If the attack object can follow its target, this defines for how far before it stops.")]
        private float followTargetMaxDistance = 10.0f;

        [SerializeField, Tooltip("Only apply damage to the first target to collide with.")]
        private bool damageOnce = true;

        private bool didDamage = false;

        [SerializeField, Tooltip("Disable the attack object when it deals its first damage to a target it collides with.")]
        private bool disableOnDamage = true;

        [SerializeField, Tooltip("When enabled, the attack object becomes a child object of its target when it deals damage to it.")]
        private bool childOnDamage = false;

        // Attack object launch delay
        private TimeModifiedTimer delayTimer;
        public float DelayTime => delayTimer.CurrValue;
        public bool InDelay => delayTimer.CurrValue > 0.0f;

        [SerializeField, Tooltip("If the attack object collides with an object that has a layer defined in this mask, it will act as if it hit a target but would not apply any damage.")]
        private LayerMask obstacleLayerMask = 0;

        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Triggerd on the attack object when it is spawned.")]
        private GameObjectToEffectObjectInput triggerEffect = null;
        [SerializeField, Tooltip("If there is a trigger effect, rotate it to face the target when created.")]
        private bool triggerEffectFaceTarget = true;

        [SerializeField, Tooltip("Audio played when the attack object is enabled/spawned.")]
        private AudioClipFetcher triggerAudio = null;

        [SerializeField, Tooltip("Trigger effects and audio post delay time?")]
        private bool triggerEffectsPostDelay = true;

        [SerializeField, Tooltip("Define what hit effect objects will be used with which target faction entities when the attack object deals them damage.")]
        private FactionEntityDependantHitEffectData[] hitEffects = new FactionEntityDependantHitEffectData[0];
        [SerializeField, Tooltip("Define what hit effect object will be used when the attack object hits an obstacle.")]
        private FactionEntityDependantHitEffectData obstacleHitEffect = new FactionEntityDependantHitEffectData();

        // Responsible for assigning the actual damage to the target's health.
        protected AttackDamage damage { private set; get; }

        protected IAttackManager attackMgr { private set; get; }
        protected ITerrainManager terrainMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnEffectObjectInit()
        {
            this.attackMgr = gameMgr.GetService<IAttackManager>();
            this.terrainMgr = gameMgr.GetService<ITerrainManager>(); 
        }

        protected sealed override void OnEffectObjectDestroy()
        {
        }
        #endregion

        #region Launching Attack Object
        public void OnSpawn(AttackObjectSpawnInput data)
        {
            base.OnSpawn(data);

            didDamage = false;

            this.Data = data;

            lookAtPosition = TargetPosition - transform.position;

            // Damage handler
            damage = data.source.Damage;

            // Delay options:
            this.delayTimer = new TimeModifiedTimer(data.delayTime);

            // Only if there's delay time, we'll have the attack object as child of another object
            if (this.delayTimer.CurrValue > 0.0f)
            {
                followTransform.SetTarget(data.delayParent, enableCallback: false);
            }
            // No delay time? reset movement instantly to start moving towards target
            else
                PrepareMovement();

            // Trigger effect objects and audio if they are meant to be triggered after delay
            if (!InDelay || !triggerEffectsPostDelay)
                TriggerEffectAudio();

            Data.source.Entity.Health.EntityDead += HandleSourceEntityDead;
        }

        private void HandleSourceEntityDead(IEntity sourceEntity, DeadEventArgs args)
        {
            // In case the source entity that launched this object dies while the attack object is still in delay
            // Stop following the parent target (if there is any, usually it is one of the source entity's child objects)
            // Then immediately disable the attack object
            if (InDelay)
            {
                followTransform.ResetTarget();
                Deactivate(useDisableTime: false); 
            }
        }

        private Vector3 TargetPosition => Data.target.IsValid() ? Data.target.Selection.transform.position : Data.targetPosition;

        private void PrepareMovement()
        {
            // Movement inputs:
            mvtDirection = (TargetPosition - transform.position).normalized;

            // Only consider the X and Z axis to calculate the distance because the heightMultiplier will affect the position on the Y axis.
            initialDistance = Vector3.Distance(
                new Vector3(transform.position.x, 0.0f, transform.position.z),
                new Vector3(TargetPosition.x, 0.0f, TargetPosition.z));
            incrementalDistance = 0.0f;

            nextMvtCurve = initialDistance >= mainMvtCurve.minDistance && Mathf.Abs(transform.position.y - TargetPosition.y) <= mainMvtCurve.maxHeightDifference
                ? mainMvtCurve
                : altMvtCurve;

            nextPosition = transform.position;
            nextHeight = transform.position.y;
        }

        private void TriggerEffectAudio()
        {
            effectObjPool.Spawn(
                triggerEffect.Output,
                new EffectObjectSpawnInput(
                    parent: null,
                    
                    useLocalTransform: false,
                    spawnPosition: transform.position,
                    spawnRotation: new RotationData(
                        triggerEffectFaceTarget ? RotationType.lookAtPosition : RotationType.free,
                        Data.targetPosition,
                        fixYRotation: false)));

            audioMgr.PlaySFX(AudioSourceComponent, triggerAudio, false);
        }
        #endregion

        #region Handling State/Movement
        protected override void OnActiveUpdate()
        {
            // Update the follow transform in case the attack object is "parented" into another game object that it needs to follow.
            followTransform.Update();

            // Allow the attack object to keep moving if it is being disabled but it does not have a parent object.
            if (State == EffectObjectState.inactive
                || (State == EffectObjectState.disabling && followTransform.HasTarget))
                return;

            if (lookAtPosition != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookAtPosition);

            // Handle delay mode:
            if (InDelay)
            {
                // Delay mode is done.
                if (delayTimer.ModifiedDecrease())
                {
                    followTransform.ResetTarget();

                    PrepareMovement();

                    if (triggerEffectsPostDelay)
                        TriggerEffectAudio();
                }

                // The attack object doesn't move while in delay
                return;
            }

            // Following the target
            if (followTarget
                && Data.target.IsValid()
                && Vector3.Distance(transform.position, Data.target.Selection.transform.position) <= followTargetMaxDistance)
            {
                mvtDirection = (Data.target.Selection.transform.position - transform.position).normalized;
            }

            // Handle movement
            transform.position = new Vector3(nextPosition.x, nextHeight, nextPosition.z);

            nextPosition += mvtDirection * speed.Value * Time.deltaTime;
            incrementalDistance += speed.Value * Time.deltaTime;

            float evalTime = Mathf.Clamp(incrementalDistance / initialDistance, 0.0f, Mathf.Infinity);
            nextHeight = nextPosition.y + nextMvtCurve.curve.Evaluate(evalTime) * nextMvtCurve.heightMultiplier;

            // Update rotation target
            lookAtPosition = new Vector3(nextPosition.x, nextHeight, nextPosition.z) - transform.position;
        }
        #endregion

        #region Updating State (Enabling/Disabling)
        protected override void OnDeactivated() 
        {
            attackMgr.Despawn(this);

            if(Data.source.IsValid())
                Data.source.Entity.Health.EntityDead -= HandleSourceEntityDead;
        }
        #endregion

        #region Dealing Damage
        private void OnTriggerEnter(Collider other)
        {
            var entitySelection = other.gameObject.GetComponent<EntitySelectionCollider>();

            if (State != EffectObjectState.running
                || (InDelay && !Data.damageInDelay)
                || (didDamage && damageOnce)
                || RTSHelper.IsInLayerMask(terrainMgr.BaseTerrainLayerMask, other.gameObject.layer)
                || (Data.source.Entity.IsValid() && entitySelection?.Entity == Data.source.Entity))
                return;

            // Does the collider belong to an obstacle?
            if (RTSHelper.IsInLayerMask(obstacleLayerMask, other.gameObject.layer))
            {
                ApplyDamage(other.gameObject, null, transform.position);
                return;
            }

            // Does the collider belong to a IFactionEntity?
            if (!(entitySelection?.Entity is IFactionEntity hitFactionEntity)
                || !hitFactionEntity.IsInitialized)
                return;

            // If this is a valid target to deal damage to, apply the damage.
            if ((hitFactionEntity.FactionID != Data.sourceFactionID || Data.damageFriendly) && !hitFactionEntity.Health.IsDead)
                ApplyDamage(other.gameObject, hitFactionEntity, hitFactionEntity.transform.position);
        }

        //a method called to apply damage to a target (position)
        private void ApplyDamage(GameObject targetObject, IFactionEntity target, Vector3 targetPosition)
        {
            // Deal damage
            damage.Trigger(target, targetPosition);
            didDamage = true;

            // Hit effect and audio

            if (target.IsValid())
            {
                foreach (FactionEntityDependantHitEffectData hitEffectData in hitEffects)
                    if (hitEffectData.picker.IsValidTarget(target))
                    {
                        effectObjPool.Spawn(
                            hitEffectData.effect.Output,
                            new EffectObjectSpawnInput(
                                parent: null,

                                useLocalTransform: false,
                                spawnPosition: transform.position,
                                spawnRotation: new RotationData(
                                    hitEffectData.faceSource ? RotationType.lookAtPosition : RotationType.free,
                                    Data.spawnPosition,
                                    fixYRotation: true)));

                        audioMgr.PlaySFX(target?.AudioSourceComponent,
                                         hitEffectData.audio.Fetch(),
                                         false);
                        break;
                    }
            }
            else
            {
                effectObjPool.Spawn(
                    obstacleHitEffect.effect.Output,
                    new EffectObjectSpawnInput(
                        parent: null,

                        useLocalTransform: false,
                        spawnPosition: transform.position,
                        spawnRotation: new RotationData(
                            obstacleHitEffect.faceSource ? RotationType.lookAtPosition : RotationType.free,
                            Data.spawnPosition,
                            fixYRotation: true)));

                audioMgr.PlaySFX(target?.AudioSourceComponent,
                                 obstacleHitEffect.audio.Fetch(),
                                 false);
            }

            // Child object of the target?
            if (targetObject.IsValid() && childOnDamage == true)
                followTransform.SetTarget(
                    target.IsValid() ? target.TransformInput : new ModelCacheAwareTransformInput(targetObject.transform),
                    offset: (transform.position - targetObject.transform.position),
                    enableCallback: true
                );

            // Disable on damage? Handle it through the effect object.
            if (disableOnDamage == true)
                Deactivate();
        }
        #endregion
    }
}
