﻿using UnityEngine;
using UnityEngine.AI;

using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Game;
using RTSEngine.Logging;
using RTSEngine.Event;

namespace RTSEngine.Movement
{
    public class NavMeshAgentController : MonoBehaviour, IMovementController
    {
        #region Attributes
        public bool Enabled
        {
            set { navAgent.enabled = value; }
            get => navAgent.enabled;
        }

        public bool IsActive
        {
            set
            {
                if (navAgent.isOnNavMesh)
                    navAgent.isStopped = !value;
            }
            get => !navAgent.isStopped;
        }

        private NavMeshAgent navAgent; 
        private NavMeshPath navPath;

        private MovementControllerData data;

        // Used to save the NavMeshAgent velocity to re-assign it when the game is paused and the unit resumes movement
        private Vector3 cachedVelocity;

        public MovementControllerData Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;

                navAgent.speed = data.speed;

                navAgent.acceleration = data.acceleration;

                navAgent.angularSpeed = data.angularSpeed;

                navAgent.stoppingDistance = data.stoppingDistance;

                // If the speed value is positive and the movement was stopped (due to a game pause for example) before this assignment
                if (navAgent.speed > 0)
                {
                    if (navAgent.isStopped)
                    {
                        navAgent.velocity = cachedVelocity; // Assign velocity before the isStopped is enabled.
                        navAgent.isStopped = false; // Enable movement
                    }
                }
                else
                {
                    if (!navAgent.isStopped)
                    {
                        cachedVelocity = navAgent.velocity; // Cache current velocity of unit. 
                        navAgent.isStopped = true; // Disable movement
                        navAgent.velocity = Vector3.zero; // Nullify velocity to stop any in progress movement
                    }
                }
            }
        }

        /// <summary>
        /// The navigation mesh area mask in which the unit can move.
        /// </summary>
        public LayerMask NavigationAreaMask => navAgent.areaMask;

        /// <summary>
        /// The size that the unit occupies in the navigation mesh while moving.
        /// </summary>
        public float Radius => navAgent.radius;

        /// <summary>
        /// The position of the next corner of the unit's active path.
        /// </summary>
        public Vector3 NextPathTarget { get { return navAgent.steeringTarget; } }

        /// <summary>
        /// The position of the last corner of the unit's active path, AKA, the path's destination.
        /// </summary>
        public Vector3 FinalTarget { get { return navAgent.destination; } }

        public MovementSource LastSource { get; private set; }

        public Vector3 Destination => navAgent.destination;

        // Game services
        protected IGameLoggingService logger { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; } 

        // Other components
        protected IGameManager gameMgr { private set; get; }
        protected IMovementComponent mvtComponent { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IMovementComponent mvtComponent, MovementControllerData data)
        {
            this.gameMgr = gameMgr;
            this.mvtComponent = mvtComponent;

            this.logger = gameMgr.GetService<IGameLoggingService>(); 

            IEntity entity = mvtComponent?.Entity;
            if (!logger.RequireValid(entity
                , $"[{GetType().Name}] Can not initialize without a valid Unit instance."))
                return;

            navAgent = entity.gameObject.GetComponent<NavMeshAgent>();
            if (!logger.RequireValid(navAgent,
                $"[{GetType().Name} - '{entity.Code}'] '{typeof(NavMeshAgent).Name}' component must be attached to the unit."))
                return;
            navAgent.enabled = true;

            this.Data = data;

            navPath = new NavMeshPath();

            // Always set to none as Navmesh's obstacle avoidance desyncs multiplayer game since it is far from deterministic
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            // Make sure the NavMeshAgent component updates our unit's position when active.
            navAgent.updatePosition = true;
        }
        #endregion

        #region Preparing/Launching Movement
        /// <summary>
        /// Attempts to calculate a valid path for the specified destination position.
        /// </summary>
        /// <param name="destination">Vector3 that represents the movement's target position.</param>
        /// <returns>True if the path is valid and complete, otherwise false.</returns>
        public void Prepare(Vector3 destination, MovementSource source)
        {
            this.LastSource = source;

            navAgent.CalculatePath(destination, navPath);

            if (navPath != null && navPath.status == NavMeshPathStatus.PathComplete)
                mvtComponent.OnPathPrepared(LastSource);
            else
            {
                this.LastSource = new MovementSource();
                mvtComponent.OnPathFailure();
            }
        }

        /// <summary>
        /// Starts the unit movement using the last calculated path from the "Prepare()" method.
        /// </summary>
        public void Launch ()
        {
            IsActive = true;

            navAgent.SetPath(navPath);
        }
        #endregion
    }
}
