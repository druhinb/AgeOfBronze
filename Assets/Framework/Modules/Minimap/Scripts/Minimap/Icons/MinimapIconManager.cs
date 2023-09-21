using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Logging;
using RTSEngine.Utilities;


namespace RTSEngine.Minimap.Icons
{

    public class MinimapIconManager : ObjectPool<IMinimapIcon, MinimapIconSpawnInput>, IMinimapIconManager
    {
        #region Attributes
        [SerializeField, EnforceType(typeof(IMinimapIcon), prefabOnly: true), Tooltip("Prefab cloned to spawn a new minimap icon.")]
        private GameObjectToMinimapIconInput prefab = null;

        private Dictionary<int, IMinimapIcon> activeIcons = null;

        [SerializeField, Tooltip("Height of the the minimap icons. When you have multiple elements that can be drawn on the minimap, you want to assign them different heights depending on what gets priority to be visible first in your game.")]
        private float height = 20.0f;

        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnObjectPoolInit()
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            if (!logger.RequireValid(prefab,
                $"[{GetType().Name}] The 'Prefab' field must be assigned!", source: this))
                return;

            activeIcons = new Dictionary<int, IMinimapIcon>();

            // Only display the building's minimap icon when the building is placed.
            globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal += HandleEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal += HandleEntityFactionUpdateCompleteGlobal;

            globalEvent.EntityVisibilityUpdateGlobal += HandleEntityVisiblityUpdateGlobal;
        }

        private void OnDestroy()
        {
            globalEvent.EntityInitiatedGlobal -= HandleEntityInitiatedGlobal;

            globalEvent.EntityDeadGlobal -= HandleEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal -= HandleEntityFactionUpdateCompleteGlobal;

            globalEvent.EntityVisibilityUpdateGlobal -= HandleEntityVisiblityUpdateGlobal;
        }
        #endregion

        #region Handling Events
        private void HandleEntityInitiatedGlobal(IEntity entity, EventArgs args)
        {
            if(entity.IsInteractable)
                Spawn(entity);
        }

        private void HandleEntityDeadGlobal(IEntity entity, EventArgs args)
        {
            Despawn(entity);
        }

        private void HandleEntityFactionUpdateStartGlobal(IEntity entity, FactionUpdateArgs args)
        {
            Despawn(entity);
        }

        private void HandleEntityFactionUpdateCompleteGlobal(IEntity entity, FactionUpdateArgs args)
        {
            Spawn(entity);
        }

        private void HandleEntityVisiblityUpdateGlobal(IEntity entity, VisibilityEventArgs args)
        {
            // Despawn the minimap icon first in all cases
            Despawn(entity);

            // In case the entity is now visible, show the minimap icon
            if(args.IsVisible)
                Spawn(entity);
        }
        #endregion

        #region Spawning/Despawning Minimap Icons
        /// <summary>
        /// Creates a new minimap icon or gets an inactive one.
        /// </summary>
        private IMinimapIcon Spawn(IEntity source)
        {
            if (activeIcons.ContainsKey(source.Key))
                return activeIcons[source.Key];

            IMinimapIcon nextIcon = base.Spawn(prefab.Output);

            if (!nextIcon.IsValid())
                return null;

            MinimapIconSpawnInput input = new MinimapIconSpawnInput(sourceEntity: source, height, prefab.Output.transform.rotation);

            nextIcon.OnSpawn(input);
            activeIcons.Add(source.Key, nextIcon);

            return nextIcon;
        }

        private void Despawn(IEntity source)
        {
            if (activeIcons.TryGetValue(source.Key, out IMinimapIcon icon))
            {
                base.Despawn(icon);

                activeIcons.Remove(source.Key);
            }
        }
        #endregion

        #region IMinimapIcon Pooling
        public IMinimapIcon SpawnMinimapIcon(IMinimapIcon prefab, MinimapIconSpawnInput input)
        {
            IMinimapIcon nextAttackObj = base.Spawn(prefab);
            if (!nextAttackObj.IsValid())
                return null;

            nextAttackObj.OnSpawn(input);

            return nextAttackObj;
        }
        #endregion
    }
}