using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Xeek
{
    class XeeksProbability
    {
        #region Probability Properties & Fields

        public float CurrentValue { get => _currentValue; }
        private float _currentValue = 0.0f;

        public List<XeeksProbabilityValue> ValueProbabilities { get; set; } = new List<XeeksProbabilityValue>();

        #endregion

        #region Diagnostic Properties & Fields

        public int MaxRandomValue { get; private set; }
        public float CurrentRandomValue { get; private set; }

        #endregion

        public void UpdateCurrentProbability()
        {
            // Check if there's nothing to do:
            if (ValueProbabilities == null || !ValueProbabilities.Any()) return;

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

            _currentValue = value;
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