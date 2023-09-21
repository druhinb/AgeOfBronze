using RTSEngine.Game;
using RTSEngine.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Health
{
    public class EntityHealthStateHandler
    {
        public IEntityHealth Source { private set; get; }

        // Health ranges are above the CurrHealth
        private Stack<EntityHealthState> inactiveStates; 
        // Health ranges are lower than currHealth or they include the CurrHealth
        private Stack<EntityHealthState> activeStates;

        protected IGameLoggingService logger { private set; get; } 

        public void Init(IGameManager gameMgr, IEntityHealth source, int statesCount)
        {
            inactiveStates = new Stack<EntityHealthState>(statesCount);
            activeStates = new Stack<EntityHealthState>(statesCount);

            this.logger = gameMgr.GetService<IGameLoggingService>();

            this.Source = source;
        }

        public void Reset(IReadOnlyList<EntityHealthState> states, int currHealth)
        {
            activeStates.Clear();
            inactiveStates.Clear();

            int i = 0;
            while (i < states.Count && states[i].LowerLimit <= currHealth)
            {
                states[i].Init(Source.Entity);

                activeStates.Push(states[i]);
                if (states[i].IsInRange(currHealth, upperBoundState: i == states.Count - 1))
                {
                    Activate(states[i]);
                    i++;
                    break;
                }
                i++;
            }

            int j = states.Count - 1;
            while (j >= i)
            {
                states[j].Init(Source.Entity);

                inactiveStates.Push(states[j]);
                j--;
            }
        }

        public void Update (bool healthIncrease, int currHealth)
        {
            if(healthIncrease)
                while(inactiveStates.Count > 0 && inactiveStates.Peek().IsInRange(currHealth, upperBoundState: inactiveStates.Count == 1))
                {
                    activeStates.Push(inactiveStates.Pop()); 
                    Activate(activeStates.Peek());
                }
            else
                while(activeStates.Count > 0 && !activeStates.Peek().IsInRange(currHealth))
                {
                    inactiveStates.Push(activeStates.Pop());
                    Activate(activeStates.Count > 0 ? activeStates.Peek() : null);
                }
        }

        public void Activate (EntityHealthState newState)
        {
            if (!newState.IsValid())
                return;

            newState.Toggle(true);
        }
    }
}
