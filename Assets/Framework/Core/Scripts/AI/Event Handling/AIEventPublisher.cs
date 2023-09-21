using RTSEngine.Event;
using RTSEngine.AI.Attack;
using System;

namespace RTSEngine.AI.Event
{
    public class AIEventPublisher : AIComponentBase, IAIEventPublisher
    {
        public event CustomEventHandler<IAIAttackManager, AIAttackEngageEventArgs> AttackEngageOrder;
        public event CustomEventHandler<IAIAttackManager, EventArgs> AttackCancelled;

        public void RaiseAttackEngageOrder(IAIAttackManager sender, AIAttackEngageEventArgs args)
        {
            var handler = AttackEngageOrder;
            handler?.Invoke(sender, args);
        }

        public void RaiseAttackCancelled(IAIAttackManager sender)
        {
            var handler = AttackCancelled;
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public event CustomEventHandler<IAIDefenseManager, AITerritoryDefenseEngageEventArgs> TerritoryDefenseOrder;
        public event CustomEventHandler<IAIDefenseManager, EventArgs> TerritoryDefenseCancelled;

        public void RaiseTerritoryDefenseOrder(IAIDefenseManager sender, AITerritoryDefenseEngageEventArgs args)
        {
            var handler = TerritoryDefenseOrder;
            handler?.Invoke(sender, args);
        }

        public void RaiseTerritoryDefenseCancelled(IAIDefenseManager sender)
        {
            var handler = TerritoryDefenseCancelled;
            handler?.Invoke(sender, EventArgs.Empty);
        }

    }
}
