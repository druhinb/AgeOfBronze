using System;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Determinism;

namespace RTSEngine.EntityComponent
{
    [Serializable]
    public struct EntityTargetComponentData
    {
        public int targetKey;
        public Vector3 position;
        public Vector3 opPosition;
    }

    public struct SetTargetInputData
    {
        public string componentCode;

        public TargetData<IEntity> target;
        public bool playerCommand;

        public bool includeMovement;
        public bool isMoveAttackRequest;

        public bool fromTasksQueue;

        public SetTargetInputDataBooleans BooleansToMask()
        {
            SetTargetInputDataBooleans nextMask = SetTargetInputDataBooleans.none; 
            if (includeMovement)
                nextMask |= SetTargetInputDataBooleans.includeMovement;
            if (isMoveAttackRequest)
                nextMask |= SetTargetInputDataBooleans.isMoveAttackRequest;
            if (fromTasksQueue)
                nextMask |= SetTargetInputDataBooleans.fromTasksQueue;

            return nextMask;
        }
    }

    public enum SetTargetInputDataBooleans 
    {
        none = 0,
        includeMovement = 1 << 0,
        isMoveAttackRequest = 1 << 1,
        fromTasksQueue = 1 << 2,
        all = ~0
    };


    public interface IEntityTargetComponent : IEntityComponent
    {
        int Priority { get; }

        bool HasTarget { get; }
        bool RequireIdleEntity { get; }
        bool IsIdle { get; }

        AudioClip OrderAudio { get; }
        EntityTargetComponentData TargetData { get; }

        event CustomEventHandler<IEntityTargetComponent, TargetDataEventArgs> TargetUpdated;
        event CustomEventHandler<IEntityTargetComponent, EventArgs> TargetStop;

        // When SetIdle on the IEntity is called and there is a source component assigned to the idling request
        // The source will not be set to idle and all entity target components will be queried to see if the provided source would allow or disallow the component to be set to idle.
        bool CanStopOnSetIdleSource(IEntityTargetComponent idleSource);
        void Stop();

        bool CanSearch { get; }

        bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target);

        ErrorMessage IsTargetValid(TargetData<IEntity> testTarget, bool playerCommand);
        ErrorMessage SetTarget(TargetData<IEntity> newTarget, bool playerCommand);
        ErrorMessage SetTarget(SetTargetInputData input);
        ErrorMessage SetTargetLocal(TargetData<IEntity> newTarget, bool playerCommand);
        ErrorMessage SetTargetLocal(SetTargetInputData input);
    }
}
