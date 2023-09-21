using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using System;
using UnityEngine;

namespace RTSEngine.Model
{
    public interface IEntityModel : IEntityPrefabInitializable, IEntityPriorityPreInitializable, IEntityPostInitializable, ICachedModel
    {
        IEntity Entity { get; }
        IEntityModelConnections ModelConnections { get; }

        event CustomEventHandler<IEntityModel, EventArgs> ModelCached;
        event CustomEventHandler<IEntityModel, EventArgs> ModelShown;

        bool IsTransformChildValid(int indexKey);
        IModelChildTransform GetTransformChild(int indexKey);

        bool IsAnimatorChildValid(int indexKey);
        IModelChildAnimator GetAnimatorChild(int indexKey);

        bool IsRendererChildValid(int indexKey);
        IModelChildRenderer GetRendererChild(int indexKey);

        void SetParent(IEntityModel parent);
    }
}