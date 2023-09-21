using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.Model
{
    public interface IModelCacheManager : IPreRunGamePriorityService 
    {
        bool IsActive { get; }
        bool UseGridSearch { get; }

        EntityModelConnections GetCachedEntityModelReference(IEntity entity);
        void HideEntityModelReference(IEntity entity);

        void CacheModel(string code, EntityModelConnections modelObject);
        EntityModelConnections Get(IEntity source);

        GameObject Get(NonEntityModel source);
        void CacheModel(string code, GameObject modelObject);
        void UpdateModelRenderering(ICachedModel nextModel);
    }
}

