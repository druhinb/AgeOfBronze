using System;
using System.Linq;
using System.Collections.Generic;

using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.BuildingExtension;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;

namespace RTSEngine.Faction
{
    public class FactionManager : IFactionManager
    {
        #region Attributes
        public int FactionID { private set; get; }

        public IFactionSlot Slot {private set; get;}

        private List<IFactionEntity> factionEntities; 
        public IEnumerable<IFactionEntity> FactionEntities => factionEntities.ToArray();

        private Dictionary<string, int> factionEntityToAmount;
        public IReadOnlyDictionary<string, int> FactionEntityToAmount => factionEntityToAmount;
        private Dictionary<string, int> factionEntityCategoryToAmount;
        public IReadOnlyDictionary<string, int> FactionEntityCategoryToAmount => factionEntityCategoryToAmount;

        private List<IFactionEntity> dropOffTargets;
        public IReadOnlyList<IFactionEntity> DropOffTargets => dropOffTargets;

        private List<IFactionEntity> mainEntities;
        public IEnumerable<IFactionEntity> MainEntities => mainEntities.ToArray();

        private List<IUnit> units; 
        public IEnumerable<IUnit> Units => units.ToArray();

        private List<IUnit> attackUnits;
        public IEnumerable<IUnit> GetAttackUnits(float range = 1.0f)
            => attackUnits.GetRange(0, (int)(attackUnits.Count * (range >= 0.0f && range <= 1.0f ? range : 1.0f)));

        private List<IBuilding> buildings;
        public IEnumerable<IBuilding> Buildings => buildings.ToArray();

        private List<IBuilding> buildingCenters;
        public IEnumerable<IBuilding> BuildingCenters => buildingCenters.ToArray();

        private List<FactionEntityAmountLimit> limits = new List<FactionEntityAmountLimit>();

        // Game services
        protected IGameManager gameMgr { private set; get; }
        protected IGlobalEventPublisher globalEvent { private set; get; }
        #endregion

        #region Raising Events
        public event CustomEventHandler<IFactionManager, EntityEventArgs<IFactionEntity>> OwnFactionEntityAdded;
        public event CustomEventHandler<IFactionManager, EntityEventArgs<IFactionEntity>> OwnFactionEntityRemoved;

        private void RaiseOwnFactionEntityAdded (EntityEventArgs<IFactionEntity> args)
        {
            var handler = OwnFactionEntityAdded;
            handler?.Invoke(this, args);
        }
        private void RaiseOwnFactionEntityRemoved (EntityEventArgs<IFactionEntity> args)
        {
            var handler = OwnFactionEntityRemoved;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        public void Init (IGameManager gameMgr, IFactionSlot slot) 
        {
            this.gameMgr = gameMgr;
            this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();

            this.Slot = slot;
            this.Slot.FactionSlotStateUpdated += HandleFactionSlotStateUpdated;
            this.FactionID = slot.ID;

            this.limits = new List<FactionEntityAmountLimit>();
            if (slot.Data.type.IsValid() && slot.Data.type.Limits.IsValid())
                limits = slot.Data.type.Limits
                    .Select(limit => new FactionEntityAmountLimit(definer: limit.Definer, maxAmount: limit.MaxAmount))
                    .ToList();

            factionEntities = new List<IFactionEntity>();
            factionEntityToAmount = new Dictionary<string, int>();
            factionEntityCategoryToAmount = new Dictionary<string, int>();

            mainEntities = new List<IFactionEntity>();

            dropOffTargets = new List<IFactionEntity>();

            units = new List<IUnit>();
            attackUnits = new List<IUnit>();

            buildings = new List<IBuilding>();
            buildingCenters = new List<IBuilding>();

            globalEvent.UnitInitiatedGlobal += HandleUnitInitiatedGlobal;

            globalEvent.BorderActivatedGlobal += HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal += HandleBorderDisabledGlobal;
            globalEvent.BuildingPlacedGlobal += HandleBuildingPlacedGlobal;

            globalEvent.FactionEntityDeadGlobal += HandleFactionEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal += HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal += HandleEntityFactionUpdateCompleteGlobal;
		}

        private void HandleFactionSlotStateUpdated(IFactionSlot slot, EventArgs args)
        {
            // Disable this component when the faction is eliminated.
            if (slot.State == FactionSlotState.eliminated)
                Disable();
        }

        private void Disable()
        {
            globalEvent.UnitInitiatedGlobal -= HandleUnitInitiatedGlobal;

            globalEvent.BorderActivatedGlobal -= HandleBorderActivatedGlobal;
            globalEvent.BorderDisabledGlobal -= HandleBorderDisabledGlobal;
            globalEvent.BuildingPlacedGlobal -= HandleBuildingPlacedGlobal;

            globalEvent.FactionEntityDeadGlobal -= HandleFactionEntityDeadGlobal;

            globalEvent.EntityFactionUpdateStartGlobal -= HandleEntityFactionUpdateStartGlobal;
            globalEvent.EntityFactionUpdateCompleteGlobal -= HandleEntityFactionUpdateCompleteGlobal;
        }
        #endregion

        #region Handling Events
        private void HandleBorderActivatedGlobal(IBorder border, EventArgs args)
        {
            if (!RTSHelper.IsFactionEntity(border.Building, FactionID))
                return;

            buildingCenters.Add(border.Building);
        }
        private void HandleBorderDisabledGlobal(IBorder border, EventArgs args)
        {
            if (!RTSHelper.IsFactionEntity(border.Building, FactionID))
                return;

            buildingCenters.Remove(border.Building);
        }

        private void HandleUnitInitiatedGlobal(IUnit sender, EventArgs args) => AddUnit(sender);

        private void HandleBuildingPlacedGlobal(IBuilding sender, EventArgs args) => AddBuilding(sender);

        private void HandleFactionEntityDeadGlobal(IFactionEntity factionEntity, DeadEventArgs args)
        {
            if (factionEntity.IsUnit())
                RemoveUnit(factionEntity as IUnit);
            else if (factionEntity.IsBuilding())
                RemoveBuilding(factionEntity as IBuilding);
        }

        private void HandleEntityFactionUpdateStartGlobal(IEntity updatedInstance, FactionUpdateArgs args)
        {
            if (updatedInstance.IsUnit())
                RemoveUnit(updatedInstance as IUnit);
            else if (updatedInstance.IsBuilding())
                RemoveBuilding(updatedInstance as IBuilding);
        }

        private void HandleEntityFactionUpdateCompleteGlobal (IEntity updatedInstance, FactionUpdateArgs args)
        {
            //when the conversion is complete and the faction entity is assigned their new faction, add them back to the faction lists:
            if (updatedInstance.IsUnit())
                AddUnit(updatedInstance as IUnit);
            else if (updatedInstance.IsBuilding())
                AddBuilding(updatedInstance as IBuilding);
        }
        #endregion

        #region Adding/Removing Faction Entities
        private void AddFactionEntity(IFactionEntity factionEntity)
        {
            if (factionEntity.IsDummy)
                return;

            factionEntities.Add(factionEntity);

            if (factionEntity.IsMainEntity)
                mainEntities.Add(factionEntity);

            if (factionEntity.DropOffTarget.IsValid())
                dropOffTargets.Add(factionEntity);

            foreach (string category in factionEntity.Category)
            {
                if (!factionEntityCategoryToAmount.ContainsKey(category))
                    factionEntityCategoryToAmount.Add(category, 0);

                factionEntityCategoryToAmount[category]++;
            }

            if (!factionEntityToAmount.ContainsKey(factionEntity.Code))
                factionEntityToAmount.Add(factionEntity.Code, 0);
            factionEntityToAmount[factionEntity.Code]++;

            UpdateLimit(factionEntity.Code, factionEntity.Category, increment: true);

            RaiseOwnFactionEntityAdded(new EntityEventArgs<IFactionEntity>(factionEntity));
        }

        private void RemoveFactionEntity (IFactionEntity factionEntity)
        {
            if (factionEntity.IsDummy)
                return;

            factionEntities.Remove(factionEntity);

            foreach (string category in factionEntity.Category)
                factionEntityCategoryToAmount[category]--;

            factionEntityToAmount[factionEntity.Code]--;

            if (factionEntity.IsMainEntity)
                mainEntities.Remove(factionEntity);

            if (factionEntity.DropOffTarget.IsValid())
                dropOffTargets.Remove(factionEntity);

            UpdateLimit(factionEntity.Code, factionEntity.Category, increment:false);

            RaiseOwnFactionEntityRemoved(new EntityEventArgs<IFactionEntity>(factionEntity));

            // Check if the faction doesn't have any buildings/units anymore and trigger the faction defeat in that case
            CheckFactionDefeat(); 
        }

        private void AddUnit (IUnit unit)
        {
            if(!RTSHelper.IsFactionEntity(unit, FactionID))
                return;

            AddFactionEntity(unit);

			units.Add (unit);
            if (unit.AttackComponent != null)
                attackUnits.Add(unit);
        }

		private void RemoveUnit (IUnit unit)
		{
            if(!RTSHelper.IsFactionEntity(unit, FactionID))
                return;

            RemoveFactionEntity(unit);

			units.Remove (unit);
            if (unit.AttackComponent != null)
                attackUnits.Remove(unit);
        }

        private void AddBuilding (IBuilding building)
		{
            if(!RTSHelper.IsFactionEntity(building, FactionID))
                return;

            AddFactionEntity(building);

			buildings.Add (building);
		}

		private void RemoveBuilding (IBuilding building)
		{
            if(!RTSHelper.IsFactionEntity(building, FactionID))
                return;

            RemoveFactionEntity(building);

			buildings.Remove (building);
        }
        #endregion

        #region Handling Faction Defeat Conditions
        // A method that checks if the faction doesn't have any more units/buildings and trigger a faction defeat in that case.
        private void CheckFactionDefeat ()
        {
            if (mainEntities.Count == 0)
                globalEvent.RaiseFactionSlotDefeatConditionTriggeredGlobal(Slot, new DefeatConditionEventArgs(DefeatConditionType.eliminateMain));

            if (factionEntities.Count == 0)
                globalEvent.RaiseFactionSlotDefeatConditionTriggeredGlobal(Slot, new DefeatConditionEventArgs(DefeatConditionType.eliminateAll));
        }
        #endregion

        #region Handling Faction Limits
        public bool AssignLimits (IEnumerable<FactionEntityAmountLimit> newLimits)
        {
            if (!newLimits.IsValid())
                return false;

            limits = newLimits.ToList();

            return true;
        }

        public bool HasReachedLimit(IEntity entity)
            => HasReachedLimit(entity.Code, entity.Category);

        public bool HasReachedLimit(string code, IEnumerable<string> category) 
            => limits
                .Any(limit => limit.IsMaxAmountReached(code, category));
        public bool HasReachedLimit(string code, string category) 
            => limits
                .Any(limit => limit.IsMaxAmountReached(code, Enumerable.Repeat(category, 1)));

        public void UpdateLimit(IEntity entity, bool increment)
            => UpdateLimit(entity.Code, entity.Category, increment);

        private void UpdateLimit(string code, IEnumerable<string> category, bool increment)
        {
            foreach(FactionEntityAmountLimit limit in limits)
                if (limit.Contains(code, category))
                {
                    limit.Update(increment ? 1 : -1);
                    return;
                }
        }
        #endregion
    }
}
