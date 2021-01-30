using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeek
{
    public class XeeksProbabilityStateMachineBehaviour : SerializedStateMachineBehaviour
    {
        #region Configuration Properties & Fields

        [BoxGroup("Configuration")]
        [Required] [SerializeField] private string animatorParameter;
        [SerializeField] private bool isParameterAnInteger = false;

        #endregion

        #region Probability Properties & Fields

        [BoxGroup("Probability")]
        [PropertySpace]
        [InfoBox("You cannot modify this value manually, use the '" + nameof(Calculate) + "' button instead", InfoMessageType.None)]
        [InlineButton(nameof(Calculate))]
        [OdinSerialize]
        public float CurrentValue { get => _probabilityObject.CurrentValue; set => _ = value; }

        [PropertySpace]
        [BoxGroup("Probability")]
        [ListDrawerSettings(Expanded = true, AlwaysAddDefaultValue = true, CustomAddFunction = nameof(AddValueProbability))]
        [Searchable]
        [OdinSerialize]
        public List<XeeksProbabilityValue> ValueProbabilities { get => _probabilityObject.ValueProbabilities; set => _probabilityObject.ValueProbabilities = value; }

        #endregion

        #region Diagnostic Properties & Fields

        [BoxGroup("Diagnostics")]
        [OdinSerialize] [ReadOnly] public int MaxRandomValue { get => _probabilityObject.MaxRandomValue; set => _ = value; }

        [BoxGroup("Diagnostics")]
        [OdinSerialize] [ReadOnly] public float CurrentRandomValue { get => _probabilityObject.CurrentRandomValue; set => _ = value; }

        #endregion

        #region Private Properties & Fields

        private XeeksProbability _probabilityObject = new XeeksProbability();

        #endregion

        private void Calculate() => _probabilityObject.UpdateCurrentProbability();

        private XeeksProbabilityValue AddValueProbability()
        {
            return new XeeksProbabilityValue(ValueProbabilities.Any() ? ValueProbabilities.Last().Value + 1 : 0);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _probabilityObject.UpdateCurrentProbability();

            if (isParameterAnInteger) animator.SetInteger(animatorParameter, (int)_probabilityObject.CurrentValue);
            else animator.SetFloat(animatorParameter, _probabilityObject.CurrentValue);
        }
    }
}