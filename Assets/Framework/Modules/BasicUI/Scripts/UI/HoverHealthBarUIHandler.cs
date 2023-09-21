using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Utilities;

namespace RTSEngine.UI
{
    public class HoverHealthBarUIHandler : ObjectPool<HoverHealthBar, PoolableObjectSpawnInput>, IPreRunGameService
    {
        #region Attributes
        [SerializeField, Tooltip("Enable or disable showing health bars when the player hovers the mouse over an entity in the game.")]
        private bool isActive = true;

        [SerializeField, EnforceType(prefabOnly: true), Tooltip("Hover health bar prefab object that includes the 'HoverHelathBar' component")]
        private HoverHealthBar prefab = null;

        [SerializeField, Tooltip("Display the hover health bar for an entity only when the player places their mouse cursor on that entity.")]
        private bool onMouseEnterOnly = false;
        private HoverHealthBar mouseEnterHoverHealthBar;

        [SerializeField, Tooltip("Enable to only display the hover health bar for the player faction's units and buildings.")]
        private bool playerFactionOnly = true;

        [SerializeField, Tooltip("What types of entites are allowed to have a hover health bar.")]
        private EntityType allowedEntityTypes = EntityType.all;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Initializing/Terminating
        protected override void OnObjectPoolInit()
        {
            this.logger = gameMgr.GetService<IGameLoggingService>();
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            if (!isActive)
                return;

            if (!logger.RequireValid(prefab,
              $"[{GetType().Name}] The 'Prefab' field must be assigned"))
                return; 

            Hide();

            mouseEnterHoverHealthBar = null;

            if (onMouseEnterOnly)
            {
                globalEvent.EntityMouseEnterGlobal += HandleEntityMouseEnterGlobal;
                globalEvent.EntityMouseExitGlobal += HandleEntityMouseExitGlobal;
            }
            else
            {
                globalEvent.EntityInitiatedGlobal += HandleEntityInitiatedGlobal;
            }
        }

        private void HandleEntityInitiatedGlobal(IEntity source, EventArgs args)
        {
            if (!CanEnable(source))
                return;

            Spawn(prefab, new HoverHealthBarSpawnInput(source, new Vector3(0.0f, source.Health.HoverHealthBarY, 0.0f)));
        }

        public bool CanEnable(IEntity source)
        {
            return isActive
                && source.IsValid()
                && !source.IsDummy
                && source.IsEntityTypeMatch(allowedEntityTypes)
                && (!playerFactionOnly || source.IsLocalPlayerFaction());
        }

        private void OnDestroy ()
        {
            globalEvent.EntityMouseEnterGlobal -= HandleEntityMouseEnterGlobal;
            globalEvent.EntityMouseExitGlobal -= HandleEntityMouseExitGlobal;
        }
        #endregion

        #region Enabling/Disabling Hover Health Bar On Mouse Enter/Exit
        private void HandleEntityMouseEnterGlobal(IEntity entity, EventArgs e) => Enable(entity);
        private void HandleEntityMouseExitGlobal(IEntity entity, EventArgs e) => Hide(entity);

        private void Enable(IEntity source)
        {
            if (!CanEnable(source))
                return;

            if (mouseEnterHoverHealthBar.IsValid())
                Hide(mouseEnterHoverHealthBar.Entity);

            Spawn(prefab, new HoverHealthBarSpawnInput(source, new Vector3(0.0f, source.Health.HoverHealthBarY, 0.0f)));
        }

        private void Hide() => Hide(null);

        private void Hide (IEntity source)
        {
            // If there's a current active source and it's not the input one attempting to disable this -> do not disable
            // Since this could be called by the mouse exit event from an entity that is positioned near the entity for which the hover health bar is active
            if (!mouseEnterHoverHealthBar.IsValid() || mouseEnterHoverHealthBar.Entity != source) 
                return;

            Despawn(mouseEnterHoverHealthBar);
            mouseEnterHoverHealthBar = null;
        }
        #endregion

        public HoverHealthBar Spawn(HoverHealthBar prefab, HoverHealthBarSpawnInput input)
        {
            HoverHealthBar nextHealthBar = base.Spawn(prefab);
            if (!nextHealthBar.IsValid())
                return null;

            nextHealthBar.OnSpawn(input);

            if (onMouseEnterOnly)
                mouseEnterHoverHealthBar = nextHealthBar;

            return nextHealthBar;
        }
    }
}
