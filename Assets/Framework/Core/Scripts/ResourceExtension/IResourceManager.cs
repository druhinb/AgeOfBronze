using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Game;

namespace RTSEngine.ResourceExtension
{
    public interface IResourceManager : IPreRunGameService
    {
        IEnumerable<IResource> AllResources { get; }

        IReadOnlyDictionary<int, IFactionSlotResourceManager> FactionResources { get; }

        bool HasResources(ResourceInput resourceInput, int factionID);
        bool HasResources(IEnumerable<ResourceInput> resourceInputArray, int factionID);
        bool HasResources(IEnumerable<ResourceInputRange> resourceInputArray, int factionID);

        void UpdateResource(int factionID, IEnumerable<ResourceInput> resourceInputArray, bool add);
        void UpdateResource(int factionID, ResourceInput resourceInput, bool add);

        void SetResource(int factionID, IEnumerable<ResourceInput> resourceInputArray);
        void SetResource(int factionID, ResourceInput resourceInput);

        ErrorMessage CreateResource(IResource resourcePrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitResourceParameters initParams);
        IResource CreateResourceLocal(IResource resourcePrefab, Vector3 spawnPosition, Quaternion spawnRotation, InitResourceParameters initParams);

        bool IsResourceTypeValidInGame(ResourceInput resourceInput, int factionID);

        void UpdateReserveResources(IEnumerable<ResourceInput> requiredResources, int factionID);
        void UpdateReserveResources(ResourceInput resourceInput, int factionID);

        void SetReserveResources(IEnumerable<ResourceInput> inputResources, int factionID);
        void SetReserveResources(ResourceInput resourceInput, int factionID);

        void ReleaseResources(IEnumerable<ResourceInput> inputResources, int factionID);
        bool TryGetResourceTypeWithKey(string key, out ResourceTypeInfo resourceType);
    }
}