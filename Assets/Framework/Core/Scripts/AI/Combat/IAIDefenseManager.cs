using RTSEngine.Entities;

using UnityEngine;

namespace RTSEngine.AI.Attack
{
    public interface IAIDefenseManager : IAIComponent
    {
        bool IsDefending { get; }
        IBuilding LastDefenseCenter { get; }

        void LaunchDefense(IBuilding nextDefenseCenter, bool forceUpdateDefenseCenter);
        void LaunchDefense(Vector3 defensePosition, bool forceUpdateDefenseCenter);
        void CancelDefense();

        bool OnUnitSupportRequest(Vector3 supportPosition, IFactionEntity target);
    }
}