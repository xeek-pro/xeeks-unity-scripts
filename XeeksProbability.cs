using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Xeek
{
    public class XeeksProbability : SerializedMonoBehaviour
    {
        #region Configuration Properties & Fields

        [BoxGroup("Configuration")]
        [OdinSerialize]
        public float SecondsBeforeRecalculation { get; set; } = 0.5f;

        [BoxGroup("Configuration")]
        [OdinSerialize]
        public bool IsEnabled {
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
        [OdinSerialize] [ReadOnly] private float CurrentValue { get; set; }

        [PropertySpace]
        [BoxGroup("Probability")]
        [ListDrawerSettings(Expanded = true, AlwaysAddDefaultValue = true, CustomAddFunction = nameof(AddValueProbability))]
        [Searchable]
        [OdinSerialize]
        public List<XeeksProbabilityValue> ValueProbabilities { get; set; } = new List<XeeksProbabilityValue>();

        #endregion

        #region Diagnostic Properties & Fields

        [BoxGroup("Diagnostics")]
        [OdinSerialize] [ReadOnly] private int MaxRandomValue { get; set; }

        [BoxGroup("Diagnostics")]
        [OdinSerialize] [ReadOnly] private float CurrentRandomValue { get; set; }

        [BoxGroup("Diagnostics")]
        [PropertySpace]
        [DisableIf(nameof(IsDisabled))]
        [InfoBox("This button is disabled because updating isn't possible when the script is not enabled.",
            InfoMessageType.None, VisibleIf = nameof(IsDisabled))]
        [Button]
        public void ForceUpdate() => UpdateCurrentProbability();

        #endregion

        #region Private Properties & Fields

        private const float UPDATE_INTERVAL = 0.250f;
        private bool _isEnabled = false;
        private Coroutine _probabilityRoutine;

        #endregion

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

            UpdateCurrentProbability();

            _probabilityRoutine = StartCoroutine(ProbabilityCoroutine());
        }

        private void UpdateCurrentProbability()
        {
            // Check if there's nothing to do:
            if (ValueProbabilities == null || !ValueProbabilities.Any() || !IsEnabled) return;

            GenerateProbabilityOffsets();

            MaxRandomValue = ValueProbabilities.Sum(x => x.Probability);
            CurrentRandomValue = Random.Range(0, MaxRandomValue);

            XeeksProbabilityValue selection = null;
            for(int iteration = 0; iteration < 1000; iteration++) // Avoid infinite looping
            {
                selection = ValueProbabilities.FirstOrDefault(x =>
                    CurrentRandomValue >= x.Start &&
                    CurrentRandomValue <= x.End);

                if (selection == null) break;

                if (!selection.Repeatable && selection.Value == CurrentValue) continue;
                else break;
            }

            // Second pass, guarantee non-sequential selection isn't made if iteration above was met:
            if(selection != null && ValueProbabilities.Count > 1 && !selection.Repeatable && selection.Value == CurrentValue)
            {
                var probabilitiesMinusSelection = ValueProbabilities.Where(x => x != selection).ToList();
                selection = probabilitiesMinusSelection[Random.Range(0, probabilitiesMinusSelection.Count - 1)];
            }

            // If no probability value matched the current random value, just choose the first one:
            var value = selection?.Value ?? ValueProbabilities.First().Value;

            CurrentValue = value;
        }

        private void GenerateProbabilityOffsets()
        {
            int offset = 0;
            ValueProbabilities.ForEach(x =>
            {
                x.Start = offset;
                x.End = offset + x.Probability;
                offset = x.End + 1;
            });
        }

        private XeeksProbabilityValue AddValueProbability()
        {
            return new XeeksProbabilityValue(ValueProbabilities.Any() ? ValueProbabilities.Last().Value + 1 : 0);
        }
    }

    [DebuggerDisplay("Comment = {Comment}, Value = {Value}, Offset Range = {Start} - {End}")]
    public class XeeksProbabilityValue
    {
        [OdinSerialize]
        public string Comment { get; set; }

        [OdinSerialize]
        public int Value { get; set; }

        [OdinSerialize]
        [InfoBox("For a very high probability, consider using \"Repeatable\" for that probability", 
            InfoMessageType.None, VisibleIf = nameof(IsHighProbabilityAndRepeatableNotAllowed))]
        [PropertyRange(0, 1000)]
        public int Probability { get; set; }

        private bool IsHighProbabilityAndRepeatableNotAllowed => Probability >= 350 && !Repeatable;

        [OdinSerialize]
        public bool Repeatable { get; set; } = false;

        [OdinSerialize]
        [ReadOnly]
        [HorizontalGroup("Range", MarginRight = 0.025f)]
        public int Start { get; set; }

        [OdinSerialize]
        [ReadOnly]
        [HorizontalGroup("Range")]
        public int End { get; set; }

        public XeeksProbabilityValue(int value = 0)
        {
            Value = value;
        }
    }
}