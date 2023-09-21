using System;

using RTSEngine.Entities;

namespace RTSEngine.AI.Event
{
    public class AITerritoryDefenseEngageEventArgs : EventArgs
    {
        public IBuilding LastDefenseCenter { private set; get; }
        public IBuilding NextDefenseCenter { private set; get; }
        public float DefenseRange { private set; get; }

        public AITerritoryDefenseEngageEventArgs(IBuilding lastDefenseCenter, IBuilding nextDefenseCenter, float defenseRange)
        {
            this.LastDefenseCenter = lastDefenseCenter;
            this.NextDefenseCenter = nextDefenseCenter;

            this.DefenseRange = defenseRange;
        }
    }
}