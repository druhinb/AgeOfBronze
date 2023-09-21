using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.AI
{
    public class FSMAIManager : MonoBehaviour, IAIManager
    {
        #region Attributes
        public AIType Type { private set; get; }

        private Dictionary<Type, IAIComponent> oneInstanceComponents;
        private Dictionary<Type, IEnumerable<IAIComponent>> multipleInstanceComponents;

        public IFactionManager FactionMgr { private set; get; }

        protected IGameManager gameMgr { private set; get; }
        protected IGameLoggingService logger { private set; get; } 
        #endregion

        #region Raising Events
        public event CustomEventHandler<IAIManager, EventArgs> InitComplete;
        private void RaiseInitComplete()
        {
            var handler = InitComplete;
            handler?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Initializing/Terminating
        public void Init(AIType AIType, IGameManager gameMgr, IFactionManager factionMgr)
        {
            this.Type = AIType;
            this.gameMgr = gameMgr;
            this.FactionMgr = factionMgr;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            gameMgr.GameStartRunning += HandleGameStartRunning;

            oneInstanceComponents = new Dictionary<Type, IAIComponent>();
            multipleInstanceComponents = new Dictionary<Type, IEnumerable<IAIComponent>>();
        }

        private void OnDestroy()
        {
            gameMgr.GameStartRunning -= HandleGameStartRunning;
        }

        private void HandleGameStartRunning(IGameManager sender, EventArgs args)
        {
            var allComponents = GetComponentsInChildren<IAIComponent>();
            var componentGroups = allComponents 
                .GroupBy(component => component.IsSingleInstance);

            oneInstanceComponents = componentGroups
                // Fetch singular components
                .Where(group => group.Key)
                .SelectMany(group => group)
                .ToDictionary(
                component => component.GetType().GetSuperInterfaceType<IAIComponent>(),
                component => component
                );

            multipleInstanceComponents = componentGroups
                // Fetch sets of components
                .Where(group => !group.Key)
                .SelectMany(group => group)
                .GroupBy(component => component.GetType().GetSuperInterfaceType<IAIComponent>())
                .ToDictionary(
                group => group.Key,
                group => group.Select(component => component));

            foreach (var component in allComponents)
                component.Init(gameMgr, this);

            RaiseInitComplete();
        }
        #endregion

        #region AI Component Handling
        public T GetAIComponent<T>() where T : IAIComponent
        {
            if (!logger.RequireTrue(oneInstanceComponents.ContainsKey(typeof(T)),
                $"[AIManager - {FactionMgr.FactionID}] AI Faction does not have an active instance of type '{typeof(T)}' that implements the '{typeof(IAIComponent).Name}' interface!"))
                return default;

            return (T)oneInstanceComponents[typeof(T)];
        }

        public IEnumerable<T> GetAIComponentSet<T>() where T : IAIComponent
        {
            if (!logger.RequireTrue(multipleInstanceComponents.ContainsKey(typeof(T)),
                $"[AIManager - Faction ID: {FactionMgr.FactionID}] AI Faction does not have an active set of instances of type '{typeof(T)}' that implement the '{typeof(IAIComponent).Name}' interface!"))
                return default;

            return multipleInstanceComponents[typeof(T)].ToArray().Cast<T>();
        }
        #endregion
    }
}
