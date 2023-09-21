using RTSEngine.Entities;
using RTSEngine.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Animation
{
    public interface IAnimatorController : IMonoBehaviour
    {
        IUnit Unit { get; }
        ModelCacheAwareAnimatorInput Animator { get; }

        AnimatorState CurrState { get; }
        bool LockState { get; set; }
        bool IsInMvtState { get; }

        void SetState(AnimatorState newState);

        void SetOverrideController(AnimatorOverrideController newOverrideController);

        void ResetAnimatorOverrideControllerOnIdle();
        void ResetOverrideController();
    }
}
