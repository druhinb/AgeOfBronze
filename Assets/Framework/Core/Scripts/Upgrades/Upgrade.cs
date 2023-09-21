﻿using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Utilities;

namespace RTSEngine.Upgrades
{
    [System.Serializable]
    public struct TriggerUpgrade
    {
        public Upgrade upgradeComp;
        public int upgradeIndex;
    }

    public abstract class Upgrade : MonoBehaviour, IMonoBehaviour
    {
        [SerializeField, EnforceType(typeof(IEntity)), Tooltip("Source entity whose upgrade is handled by this component.")]
        private GameObject sourceEntity = null;
        public IFactionEntity SourceEntity => sourceEntity.IsValid() ? sourceEntity.GetComponent<IFactionEntity>() : null;

        [Space(), SerializeField, Tooltip("Upgrade only the source instance that has this component attached to it?")]
        private bool sourceInstanceOnly = false;
        public bool SourceInstanceOnly => sourceInstanceOnly;

        [SerializeField, Tooltip("Upgrade already spawned instances?")]
        private bool updateSpawnedInstances = true;
        public bool UpdateSpawnedInstances => updateSpawnedInstances;

        [Space(), SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Effect shown when spawned instances are upgraded.")]
        private GameObjectToEffectObjectInput upgradeEffect = null;
        public IEffectObject UpgradeEffect => upgradeEffect.Output;

        // Why provide the IGameManager instance as an input? Because Upgrade components are not always attached to spawned instances but sometimes to prefabs
        public abstract void LaunchLocal(IGameManager gameMgr, int upgradeIndex, int factionID);
    }
}
