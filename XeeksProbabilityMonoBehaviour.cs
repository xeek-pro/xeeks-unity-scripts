using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeek
{
    public class XeeksProbabilityMonoBehaviour : SerializedMonoBehaviour
    {
        #region Configuration Properties & Fields

        [BoxGroup("Configuration")]
        [OdinSerialize]
        public float SecondsBeforeRecalculation { get; set; } = 0.5f;

        [BoxGroup("Configuration")]
        [DetailedInfoBox("When enabled, auto-calculation occurs.",
            "The auto-calculation is performed with a coroutine every so many seconds as determined by " + nameof(SecondsBeforeRecalculation) + ". " +
            "If this is not enabled, the " + nameof(Calculate) + " method must be used to update the value from the probabilities.",
            InfoMessageType.None)]
        [OdinSerialize]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                IsEnabledChanged = value != _isEnabled;
                _isEnabled = value;
            }
        }

        public bool IsEnabledChanged { get; private set; } = false;
        public bool IsDisabled => !IsEnabled;

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

        private const float UPDATE_INTERVAL = 0.250f;
        private bool _isEnabled = false;
        private XeeksProbability _probabilityObject = new XeeksProbability();
        private Coroutine _probabilityRoutine;

        #endregion

        private void Calculate() => _probabilityObject.UpdateCurrentProbability();

        private XeeksProbabilityValue AddValueProbability()
        {
            return new XeeksProbabilityValue(ValueProbabilities.Any() ? ValueProbabilities.Last().Value + 1 : 0);
        }

        private void Start()
        {
            if (IsEnabled) _probabilityRoutine = StartCoroutine(ProbabilityCoroutine());
        }

        private IEnumerator UpdateCoroutine()
        {
            yield return new WaitForSeconds(UPDATE_INTERVAL);

            if (IsEnabledChanged)
            {
                IsEnabledChanged = false;
                StopCoroutine(_probabilityRoutine);
                if (IsEnabled) _probabilityRoutine = StartCoroutine(ProbabilityCoroutine());
            }
        }

        private IEnumerator ProbabilityCoroutine()
        {
            yield return new WaitForSeconds(SecondsBeforeRecalculation);

            _probabilityObject.UpdateCurrentProbability();

            _probabilityRoutine = StartCoroutine(ProbabilityCoroutine());
        }
    }
}