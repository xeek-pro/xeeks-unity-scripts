using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Diagnostics;

namespace Xeek.Probability
{
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