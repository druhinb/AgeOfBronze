using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.Animation
{
	public class StateMachineController : StateMachineBehaviour {

        #region Attributes
        [System.Serializable]
        public struct ParameterState
        {
            public string name;
            public bool enabled;
        }
        [SerializeField, Tooltip("Input parameters that get enabled/disabled when this animator state is entered.")]
        private ParameterState[] onStateEnter = new ParameterState[0];
        [SerializeField, Tooltip("Input parameters that get enabled/disabled when this animator state is exited.")]
        private ParameterState[] onStateExit = new ParameterState[0];
        #endregion

        #region Handling State Update
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            UpdateParameters(animator, onStateEnter);
        }

		override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            UpdateParameters(animator, onStateExit);
        }

        private void UpdateParameters(Animator animator, IReadOnlyList<ParameterState> paramList)
        {
            for (int i = 0; i < paramList.Count; i++)
            {
                animator.SetBool(paramList[i].name, paramList[i].enabled);
            }
        }
        #endregion
    }
}
