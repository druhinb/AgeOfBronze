using RTSEngine.Event;
using RTSEngine.AI.Attack;
using System;

namespace RTSEngine.AI.Event
{
    public interface IAIEventPublisher : IAIComponent
    {
        event CustomEventHandler<IAIAttackManager, AIAttackEngageEventArgs> AttackEngageOrder;
        event CustomEventHandler<IAIAttackManager, EventArgs> AttackCancelled;
        event CustomEventHandler<IAIDefenseManager, AITerritoryDefenseEngageEventArgs> TerritoryDefenseOrder;
        event CustomEventHandler<IAIDefenseManager, EventArgs> TerritoryDefenseCancelled;

        void RaiseAttackEngageOrder(IAIAttackManager sender, AIAttackEngageEventArgs args);
        void RaiseAttackCancelled(IAIAttackManager sender);
        void RaiseTerritoryDefenseOrder(IAIDefenseManager sender, AITerritoryDefenseEngageEventArgs args);
        void RaiseTerritoryDefenseCancelled(IAIDefenseManager sender);
    }
}