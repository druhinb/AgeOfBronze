using System.Collections.Generic;
using System;

using UnityEngine;

using RTSEngine.Event;

namespace RTSEngine.Attack
{
    [System.Serializable]
    public partial class AttackLauncher : AttackSubComponent
    {
        #region Attributes
        [SerializeField, Tooltip("Enable to use attack objects to launch this attack or Disable to apply direct damage to the target.")]
        private bool useAttackObjects = true;

        [SerializeField, Tooltip("How would the attack object(s) be launched?")]
        private AttackObjectLaunchType launchType = AttackObjectLaunchType.inOrder;

        [SerializeField, Tooltip("The attack objects to launch as part of the attack.")]
        private AttackObjectSource[] sources = new AttackObjectSource[0];
        public IReadOnlyList<AttackObjectSource> Sources => sources;

        // Used to log the launched coroutines and created attack objects so that they can be disabled in case the attack launch is interrupted.
        private List<AttackObjectLaunchLog> launchLog;
        public IEnumerable<AttackObjectLaunchLog> LaunchLog => launchLog;

        // Called when the attack launch is complete to notify the main attack component
        private Action launchCompleteCallback;
        #endregion

        #region Events
        public event CustomEventHandler<AttackLauncher, AttackLaunchEventArgs> AttackLaunched;
        private void RaiseAttackLaunched(AttackLaunchEventArgs args)
        {
            var handler = AttackLaunched;
            handler?.Invoke(this, args);
        }
        #endregion

        #region Initializing/Terminating
        protected override void OnInit()
        {
            base.OnInit();

            launchLog = new List<AttackObjectLaunchLog>();

            for (int i = 0; i < sources.Length; i++)
                sources[i].Init(logger, SourceAttackComp, index: i);
        }
        #endregion

        #region Triggering Launch
        public void Trigger(Action launchCompleteCallback, IReadOnlyCollection<AttackObjectLaunchLogInput> nextLaunchLogInput)
        {
            this.launchCompleteCallback = launchCompleteCallback;
            // Direct attack? apply damage to target and complete attack.
            if (!useAttackObjects)
            {
                SourceAttackComp.Damage.Trigger(SourceAttackComp.Target.instance, RTSHelper.GetAttackTargetPosition(SourceAttackComp.Target));
                Complete();
                return;
            }

            // Pre-assigned launch log
            if (nextLaunchLogInput != null && nextLaunchLogInput.Count > 0)
            {
                foreach(var nextInput in nextLaunchLogInput)
                    launchLog.Add(new AttackObjectLaunchLog(sources, nextInput));
                return;
            }

            // Non direct attack? start coroutine to handle attack objects.
            switch(launchType)
            {
                case AttackObjectLaunchType.random:

                    // Random attack object launch? mark attack as complete after launching one attack object.
                    launchLog.Add(new AttackObjectLaunchLog(sources, sourceIndex: UnityEngine.Random.Range(0, sources.Length), isLastLaunch: true));
                    break;

                case AttackObjectLaunchType.inOrder:

                    launchLog.Add(new AttackObjectLaunchLog(sources, sourceIndex: 0, isLastLaunch: false));
                    break;

                case AttackObjectLaunchType.simultaneous:

                    for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
                        launchLog.Add(new AttackObjectLaunchLog(sources, sourceIndex, isLastLaunch: false));
                    break;
            }
        }
        #endregion

        #region Handling Launch Progress
        public void Update()
        {
            if (launchLog.Count == 0)
                return;

            for (int launchID = 0; launchID < launchLog.Count; launchID++)
            {
                AttackObjectLaunchLog nextLaunch = launchLog[launchID];

                if (nextLaunch.preDelayTimer.CurrValue >= 0.0f)
                {
                    if (!nextLaunch.preDelayTimer.ModifiedDecrease())
                        return;

                    nextLaunch.attackObject = sources[nextLaunch.sourceIndex].Launch(attackMgr, SourceAttackComp);

                    RaiseAttackLaunched(new AttackLaunchEventArgs(nextLaunch));
                }
                else
                {
                    if (!nextLaunch.postDelayTimer.ModifiedDecrease())
                        return;

                    switch (launchType)
                    {
                        case AttackObjectLaunchType.inOrder:

                            // Already launched the last attack object in the sources? stop here
                            // If not, move to the next one by creating a new coroutine for it.
                            if (nextLaunch.sourceIndex < sources.Length - 1)
                                launchLog.Add(new AttackObjectLaunchLog(sources, sourceIndex: nextLaunch.sourceIndex + 1, isLastLaunch: false));
                            break;

                        default:
                            break;
                    }

                    // Marked as the last attack object to launch or this is indeed the last one in the array.
                    if (nextLaunch.isLastLaunch || nextLaunch.sourceIndex == sources.Length - 1)
                        Complete();
                }
            }
        }
        #endregion

        #region Completing Launch
        private void Complete()
        {
            launchCompleteCallback();
            Reset();
        }

        public void Reset()
        {
            foreach(AttackObjectLaunchLog nextLaunch in launchLog)
            {
                // If the reset occurs while the attack object is still in delay
                // ... this would call the need for disabling the attack object entirely 
                // Example case is when the source attack entity is destroyed and attack objects are still in delay.
                if (nextLaunch.attackObject.IsValid() && nextLaunch.attackObject.InDelay)
                    nextLaunch.attackObject.Deactivate(useDisableTime: false);
            }

            launchLog.Clear();
        }
        #endregion
    }
}
