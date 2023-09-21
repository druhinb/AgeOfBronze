using System;

using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.AI.Event
{
    public class AIAttackEngageEventArgs : EventArgs
    {
        public IFactionEntity Target { private set; get; }
        public Vector3 TargetPosition { private set; get; }

        public AIAttackEngageEventArgs(IFactionEntity target, Vector3 targetPosition)
        {
            Target = target;
            TargetPosition = targetPosition;
        }
    }
}
