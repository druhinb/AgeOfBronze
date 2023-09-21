using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Audio;
using RTSEngine.Effect;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Logging;
using RTSEngine.Game;
using RTSEngine.Minimap.Cameras;
using RTSEngine.Utilities;
using UnityEngine.Serialization;

namespace RTSEngine.Minimap.Notification
{
    public class AttackMinimapNotificationManager : MonoBehaviour, IAttackMinimapNotificationManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IEffectObject), prefabOnly: true), Tooltip("Attack warning prefab as an effect object."), FormerlySerializedAs("effectPrefab")]
        private GameObjectToEffectObjectInput prefab = null;

        [SerializeField, Tooltip("Height of the the attack warning effects. When you have multiple elements that can be drawn on the minimap, you want to assign them different heights depending on what gets priority to be visible first in your game.")]
        private float height = 20.0f;

        [SerializeField, Tooltip("Played when a new attack warning is spawned.")]
        private AudioClipFetcher audioClip = new AudioClipFetcher();
        [SerializeField, Tooltip("When enabled, a player message is sent to the IPlayerMessageHandler manager component which interpretes the message and can communicate it to the player.")]
        private bool sendPlayerMessage = true;

        [SerializeField, Tooltip("The minimum distance required between all active attack warnings.")]
        private float minDistance = 10.0f;

        protected IGameLoggingService logger { private set; get; }
        protected IMinimapCameraController minimapCameraController { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        protected IEffectObjectPool effectObjPool { private set; get; }
        protected IGameAudioManager audioMgr { private set; get; }
        protected IPlayerMessageHandler playerMsgHandler { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.minimapCameraController = gameMgr.GetService<IMinimapCameraController>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.effectObjPool = gameMgr.GetService<IEffectObjectPool>();
            this.audioMgr = gameMgr.GetService<IGameAudioManager>();
            this.playerMsgHandler = gameMgr.GetService<IPlayerMessageHandler>();

            if (!logger.RequireValid(prefab,
                $"[{GetType().Name}] The 'Effect Prefab' field hasn't been assigned!",
                source: this))
                return;

            globalEvent.FactionEntityHealthUpdatedGlobal += HandleFactionEntityHealthUpdatedGlobal;
        }

        private void OnDestroy()
        {
            globalEvent.FactionEntityHealthUpdatedGlobal += HandleFactionEntityHealthUpdatedGlobal;
        }
        #endregion

        #region Handling Event: Faction Entity Health Updated
        private void HandleFactionEntityHealthUpdatedGlobal(IFactionEntity factionEntity, HealthUpdateArgs args)
        {
            // Local player's faction entity received damage?
            if (RTSHelper.IsLocalPlayerFaction(factionEntity)
                && args.Value < 0)
                Spawn(factionEntity.transform.position);
        }
        #endregion

        #region Spawning Minimap Attack Notifications
        /// <summary>
        /// Checks whether a new attack warning effect can be added in a potential position.
        /// </summary>
        public bool CanSpawn(Vector3 spawnPosition)
        {
            return effectObjPool.ActiveDic.TryGetValue(prefab.Output.Code, out IEnumerable<IEffectObject> currActiveSet)
                ? currActiveSet
                    .All(activeEffect => Vector3.Distance(activeEffect.GetComponent<RectTransform>().localPosition, spawnPosition) > minDistance)
                : true;
        }

        /// <summary>
        /// Spawns a new attack warning.
        /// </summary>
        public void Spawn(Vector3 targetPosition)
        {
            if (!logger.RequireTrue(minimapCameraController.WorldPointToLocalPointInMinimapCanvas(
                targetPosition, out Vector3 spawnPosition, height),
                $"[{GetType().Name}] Unable to find the target position of '{targetPosition}' for the new attack warning effect on the minimap canvas!"))
                return;

            if (!CanSpawn(spawnPosition))
                return;

            effectObjPool.Spawn(
                prefab.Output,
                new EffectObjectSpawnInput(
                    parent: minimapCameraController.MinimapCanvas.transform,

                    useLocalTransform: true,
                    spawnPosition: spawnPosition,
                    spawnRotation: prefab.Output.transform.localRotation));

            audioMgr.PlaySFX(audioClip.Fetch(), false);

            if (sendPlayerMessage)
                playerMsgHandler.OnErrorMessage(new PlayerErrorMessageWrapper
                {
                    message = ErrorMessage.factionUnderAttack,
                });
        }
    }
    #endregion
}