using System;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Selection;
using RTSEngine.Health;
using RTSEngine.Event;
using RTSEngine.Animation;
using RTSEngine.Upgrades;
using RTSEngine.Task;
using RTSEngine.Faction;
using RTSEngine.UnitExtension;
using RTSEngine.Model;
using RTSEngine.Game;

namespace RTSEngine.Entities
{
    public interface IEntity : IMonoBehaviour, IEquatable<IEntity>
    {
        EntityType Type { get; }

        bool IsInitialized { get; }

        IReadOnlyDictionary<string, IEntityComponent> EntityComponents { get; }

        IPendingTasksHandler PendingTasksHandler { get; }

        IReadOnlyDictionary<string, IEntityTargetComponent> EntityTargetComponents { get; }

        IReadOnlyDictionary<string, IEntityTargetProgressComponent> EntityTargetProgressComponents { get; }

        event CustomEventHandler<IEntity, EventArgs> EntityInitiated;

        ErrorMessage SetTargetFirst(SetTargetInputData input);
        ErrorMessage SetTargetFirstLocal(SetTargetInputData input);

        IEnumerable<IAttackComponent> AttackComponents { get; }
        IAttackComponent AttackComponent { get; }
        bool CanAttack { get; }

        IReadOnlyDictionary<string, IAddableUnit> AddableUnitComponents { get; }

        IMovementComponent MovementComponent { get; }
        bool CanMove(bool playerCommand);
        bool CanMove();

        string Code { get; }
        IEnumerable<string> Category { get; }
        string Name { get; }
        string Description { get; }
        Sprite Icon { get; }
        bool IsFree { get; }
        float Radius { get; }

        IEntityModel EntityModel { get; }

        bool IsInteractable { get; }
        bool IsSearchable { get; }

        int FactionID { get; }
        IFactionSlot Slot { get; }

        Color SelectionColor { get; }
        IAnimatorController AnimatorController { get; }
        IEntitySelection Selection { get; }
        IEntitySelectionMarker SelectionMarker { get; }

        AudioSource AudioSourceComponent { get; }

        IEntityHealth Health { get; }
        IEntityWorkerManager WorkerMgr { get; }

        bool CanLaunchTask { get; }
        //a variable to check that an entity is interactable, can launch tasks, can set targets and is initiated.
        void SetIdle(bool includeMovement = true);
        void SetIdle(IEntityTargetComponent exception, bool includeMovement = true);
        bool IsIdle { get; }

        ErrorMessage SetFaction(IEntity source, int targetFactionID);

        ErrorMessage SetFactionLocal(IEntity source, int targetFactionID);

        void UpgradeComponent(UpgradeElement<IEntityComponent> upgradeElement);
        void InitPrefab(IGameManager gameMgr);

        int Key { get; }
        bool IsDummy { get; }
        ModelCacheAwareTransformInput TransformInput { get; }
        IEntityTasksQueueHandler TasksQueue { get; }

        event CustomEventHandler<IEntity, FactionUpdateArgs> FactionUpdateComplete;
        event CustomEventHandler<IEntity, EntityComponentUpgradeEventArgs> EntityComponentUpgraded;
    }
}
