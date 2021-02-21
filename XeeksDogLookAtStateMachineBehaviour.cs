using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xeek {
    public class XeeksDogLookAtStateMachineBehaviour : StateMachineBehaviour
    {
        public bool disableLookAtForState = false;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var lookAtComponent = animator.gameObject.GetComponent<XeeksDogLookAt>();
            if (lookAtComponent != null && lookAtComponent.isActiveAndEnabled)
            {
                if (disableLookAtForState && lookAtComponent.LookActive) lookAtComponent.LookActive = false;
            }
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var lookAtComponent = animator.gameObject.GetComponent<XeeksDogLookAt>();
            if (lookAtComponent != null && lookAtComponent.isActiveAndEnabled)
            {
                if (disableLookAtForState && !lookAtComponent.LookActive) lookAtComponent.LookActive = true;
            }
        }
    }
}