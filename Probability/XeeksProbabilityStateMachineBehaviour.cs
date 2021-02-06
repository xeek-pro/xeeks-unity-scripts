using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeek.Probability
{
    public class XeeksProbabilityStateMachineBehaviour : SerializedStateMachineBehaviour
    {
        #region Configuration Properties & Fields

        [BoxGroup("Configuration")]
        [InfoBox("Drop in an Animator if you want to select a parameter below from a drop-down menu.", InfoMessageType = InfoMessageType.None)]
        [OdinSerialize]
        private Animator Animator { get; set; }

        [BoxGroup("Configuration")]
        [ValueDropdown(nameof(AvailableParameters))]
        [ShowIf(nameof(HasAvailableParameters))]
        [Required]
        [OdinSerialize]
        private string AnimatorParameterSelection { get => AnimatorParameter; set => AnimatorParameter = value != "None" ? value : string.Empty; }

        [BoxGroup("Configuration")]
        [HideIf(nameof(HasAvailableParameters))]
        [Required]
        [OdinSerialize]
        private string AnimatorParameter { get; set; }

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

        private List<string> AvailableParameters
        {
            get
            {
                if (Animator != null)
                {
                    List<string> availableParameters = new List<string>(new string[] { "None" });
                    availableParameters.AddRange(Animator.parameters
                        .Where(x => x.type == AnimatorControllerParameterType.Float || x.type == AnimatorControllerParameterType.Int)
                        .Select(x => x.name));

                    return availableParameters;
                }
                else
                {
                    return new List<string>();
                }
            }
        }

        private bool HasAvailableParameters => AvailableParameters?.Any() == true;

        #endregion

        private void Calculate() => _probabilityObject.UpdateCurrentProbability();

        private XeeksProbabilityValue AddValueProbability()
        {
            return new XeeksProbabilityValue(ValueProbabilities.Any() ? ValueProbabilities.Last().Value + 1 : 1);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            _probabilityObject.UpdateCurrentProbability();

            AnimatorControllerParameter parameter = animator.parameters.FirstOrDefault(x => x.name == AnimatorParameter);

            if(parameter != null)
            {
                switch(parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(parameter.nameHash, (int)_probabilityObject.CurrentValue);
                        break;
                    case AnimatorControllerParameterType.Float:
                        animator.SetFloat(parameter.nameHash, _probabilityObject.CurrentValue);
                        break;
                    default:
                        Debug.LogError($"The animator parameter {parameter.name} isn't a type this state machine behaviour can set", this);
                        break;
                }
            }
            else
            {
                Debug.LogError($"The animator parameter {parameter.name} cannot be found by this state machine behaviour", this);
            }
        }
    }
}