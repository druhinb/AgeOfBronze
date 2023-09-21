using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Logging;

namespace RTSEngine.BuildingExtension
{
    public class BaseBuildingObstacle<T> : MonoBehaviour, IEntityFullInitializable where T : Behaviour
    {
        #region Attributes
        protected IBuilding building { private set; get; }
        private T[] obstacles = new T[0];
        public IEnumerable<T> Obstacles => obstacles;

        protected IGameLoggingService logger { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
        {
            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            building = entity as IBuilding;
            if (!logger.RequireValid(building,
                $"[{GetType().Name}] This component can only be attached to an object where a component that extends '{typeof(IBuilding).Name}' interface is attached!"))
                return;

            obstacles = building.gameObject.GetComponentsInChildren<T>();

            if (!logger.RequireValid(obstacles,
                $"[{GetType().Name} - {building.Code}] A component of type '{typeof(T).Name}' must be attached to the building!"))
                return;

            foreach(T obstacle in obstacles)
                obstacle.enabled = false;

            OnPreInit();
        }

        protected virtual void OnPreInit() { }

        public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
        {
            foreach(T obstacle in obstacles)
                obstacle.enabled = true;

            OnPostInit();
        }

        protected virtual void OnPostInit() { }

        public virtual void Disable() { }
        #endregion
    }
}
