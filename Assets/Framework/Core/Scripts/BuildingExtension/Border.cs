using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Utilities;

namespace RTSEngine.BuildingExtension
{
    public class Border : MonoBehaviour, IBorder
    {
        #region Attributes
        public IBuilding Building { private set; get; }

        public bool IsActive { private set; get; }

        [SerializeField, Tooltip("Size of the territory that this component adds to the faction.")]
        private float size = 10.0f;
        public float Size { private set => size = value; get { return size; } }
        public float Surface => Mathf.PI * Mathf.Pow(Size, 2);

        [SerializeField, EnforceType(typeof(IBorderObject), prefabOnly: true), Tooltip("Assign one or multiple Border Object prefabs to this field to spawn them when this component is activated.")]
        private GameObjectToBorderObjectInput[] borderPrefabs = new GameObjectToBorderObjectInput[0];
        private IBorderObject[] currBorderObjects = null; 

        //the sorting order of this border, if border A has been activated before border B then border A has higher order than border B.
        //the order is used to determine which has priority over a common area of the map
        public int SortingOrder { private set; get; }

        [Space(), SerializeField, Tooltip("Set the maximum amount of instances of building types that can be inside this border.")]
        private BuildingAmount[] buildingLimits = new BuildingAmount[0];

        //key: code of a building type that is inside this border's territory
        //value: amount of the building instances inside this border's territory of the building type in key.
        private Dictionary<string, int> buildingTypeTracker = new Dictionary<string, int>();

        //a list of the spawned buildings inside the territory defined by this border
        private List<IBuilding> buildingsInRange = new List<IBuilding>(); 
        public IEnumerable<IBuilding> BuildingsInRange => buildingsInRange.Where(building => building.IsValid());

        //a list of the resources inside the territory defined by this border
        private List<IResource> resourcesInRange = new List<IResource>(); 
        public IEnumerable<IResource> ResourcesInRange => resourcesInRange.Where(resource => resource.IsValid());

        public IEnumerable<IEntity> EntitiesInRange => BuildingsInRange.Cast<IEntity>().Concat(ResourcesInRange);

        // Game services
        protected IGlobalEventPublisher globalEvent { private set; get; } 
        protected IResourceManager resourceMgr { private set; get; }
        protected IBuildingManager buildingMgr { private set; get; }
        #endregion

        #region Initializing/Terminating
        public void Init(IGameManager gameMgr, IBuilding building)
        {
            if (IsActive)
                return;

            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            this.resourceMgr = gameMgr.GetService<IResourceManager>();
            this.buildingMgr = gameMgr.GetService<IBuildingManager>(); 

            this.Building = building;

            buildingsInRange.Add(Building);

            SortingOrder = gameMgr.GetService<IBuildingManager>().LastBorderSortingOrder;

            if(borderPrefabs.IsValid())
            {
                currBorderObjects = borderPrefabs
                    .Select(prefab =>
                        buildingMgr.SpawnBorderObject(
                        prefab.Output,
                        new BorderObjectSpawnInput(
                            border: this,
                            spawnRotation: prefab.Output.transform.rotation)))
                    .ToArray();
            }

            IsActive = true;

            // Go through the already spawned resources and see which ones can be added to this Border
            foreach (IResource spawnedResource in resourceMgr.AllResources)
                AddResource(spawnedResource);

            globalEvent.ResourceInitiatedGlobal += HandleResourceInitiatedGlobal;

            globalEvent.BorderResourceRemovedGlobal += HandleBorderResourceRemovedGlobal;

            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacedGlobal;

            globalEvent.RaiseBorderActivatedGlobal(this);

            OnInit();
        }

        protected virtual void OnInit() { }

        public void Disable ()
        {
            if (!IsActive)
                return;  

            RemoveAllResources();

            globalEvent.ResourceInitiatedGlobal -= HandleResourceInitiatedGlobal;

            globalEvent.BorderResourceRemovedGlobal -= HandleBorderResourceRemovedGlobal;

            globalEvent.RaiseBorderDisabledGlobal(this);

            if (borderPrefabs.IsValid())
            {
                foreach(IBorderObject borderObj in currBorderObjects)
                    borderObj.Despawn();

                currBorderObjects = new IBorderObject[0];
            }

            OnDisabled();
        }

        protected virtual void OnDisabled() { }
        #endregion

        #region Territory Handling
        public bool IsInBorder (Vector3 testPosition)
        {
            return Vector3.Distance(testPosition, transform.position) <= Size;
        }
        #endregion

        #region Handling Events: IResource
        private void HandleResourceInitiatedGlobal (IResource resource, EventArgs e)
        {
            AddResource(resource);
        }

        private void HandleBorderResourceDead (IEntity resource, DeadEventArgs e)
        {
            resource.Health.EntityDead -= HandleBorderResourceDead;

            RemoveResource(resource as IResource);
        }

        private void HandleBorderResourceRemovedGlobal(object sender, ResourceEventArgs e)
        {
            if (sender as Border == this) //make sure this is not the same border that removed the resource
                return;

            AddResource(e.Resource);
        }
        #endregion

        #region Border Resource Handling 
        /// <summary>
        /// Adds a Resource instance to the border list if it belongs to it. 
        /// </summary>
        /// <param name="resource">Resource instance to add.</param>
        private void AddResource (IResource resource)
        {
            if (resourcesInRange.Contains(resource)
                || (!resource.IsSameFaction(Building) && resource.FactionID != -1)
                || !IsInBorder(resource.transform.position))
                return;

            resourcesInRange.Add(resource);

            // Only update the resource's "owner" faction ID if it is a resource building that already only belongs to one faction
            if(!(resource is IResourceBuilding))
                resource.SetFaction(Building, Building.FactionID);

            resource.Health.EntityDead += HandleBorderResourceDead;

            globalEvent.RaiseBorderResourceAddedGlobal(this, new ResourceEventArgs(resource));

            OnResourceAdded(resource);
        }

        protected virtual void OnResourceAdded(IResource resource) { }

        /// <summary>
        /// Removes all Resource instances registered as part of the border territory.
        /// </summary>
        private void RemoveAllResources()
        {
            while (resourcesInRange.Count > 0)
                RemoveResource(resourcesInRange[0]);
        }

        /// <summary>
        /// Removes a Resource instance from the border resources list.
        /// </summary>
        /// <param name="resource">Resource instance to remove from Border instance.</param>
        private void RemoveResource(IResource resource)
        {
            if (!resourcesInRange.Contains(resource))
                return;

            resourcesInRange.Remove(resource);

            // Only update the resource's "owner" faction ID if it is not a resource building that already only belongs to one faction
            // Make sure the resource entity is still here and not destroyed
            if(!resource.Health.IsDead && !(resource is IResourceBuilding))
                resource.SetFaction(Building, -1);

            globalEvent.RaiseBorderResourceRemovedGlobal(this, new ResourceEventArgs(resource));

            OnResourceRemoved(resource);
        }

        protected virtual void OnResourceRemoved(IResource resource) { }
        #endregion

        #region Handling Events: IBuilding
        private void HandleBuildingPlacedGlobal(IBuilding building, EventArgs args)
        {
            AddBuilding(building);
        }

        private void HandleBorderBuildingDead(IEntity building, DeadEventArgs args)
        {
            RemoveBuilding(building as IBuilding);
            building.Health.EntityDead -= HandleBorderBuildingDead;
        }
        #endregion

        #region Border Building Handling 
        private void AddBuilding(IBuilding building)
        {
            if (!building.CurrentCenter.IsValid()
                || building.CurrentCenter != this as IBorder)
                return;

            buildingsInRange.Add(building);

            if (!buildingTypeTracker.ContainsKey(building.Code))
                buildingTypeTracker.Add(building.Code, 0);

            buildingTypeTracker[building.Code] += 1;

            building.Health.EntityDead += HandleBorderBuildingDead;
            OnBuildingAdded(building);
        }

        protected virtual void OnBuildingAdded(IBuilding building) { }

        private void RemoveBuilding(IBuilding building)
        {
            buildingsInRange.Remove(building);

            buildingTypeTracker[building.Code] -= 1;

            OnBuildingUnregistererd(building);
        }

        protected virtual void OnBuildingUnregistererd(IBuilding building) { }

        public virtual bool IsBuildingAllowedInBorder(IBuilding building)
        {
            foreach(BuildingAmount ba in buildingLimits)
                if(ba.codes.Contains(building))
                {
                    buildingTypeTracker.TryGetValue(building.Code, out int currValue);

                    return currValue < ba.amount;
                }

            return true; //if the building type doesn't have a defined slot in the buildings limits, then it can be definitely accepted.
        }
        #endregion

#if UNITY_EDITOR
        #region Editor
        private void OnDrawGizmosSelected()
        {
            if (!IsActive)
                return;

            Gizmos.color = Building.Slot.Data.color;
            Gizmos.DrawWireSphere(Building.transform.position, Size);
        }
        #endregion
#endif
    }
}