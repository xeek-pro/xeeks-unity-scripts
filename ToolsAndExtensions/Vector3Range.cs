using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using System.Diagnostics;
using System;

namespace Xeek.ToolsAndExtensions
{
    [DebuggerDisplay("Vector3Range: Start = {Start}, End = {End}")]
    public struct Vector3Range
    {
        [OdinSerialize]
        public Vector3 Start { get; set; }

        [OdinSerialize]
        public Vector3 End { get; set; }

        [ShowInInspector]
        [ReadOnly]
        public Vector3 Direction => IsUnset ? Vector3.zero : (End - Start).normalized;

        [ShowInInspector]
        [ReadOnly]
        public float Distance => IsUnset ? 0.0f : (End - Start).magnitude;

        public bool IsUnset => Start == DefaultVector3 && End == DefaultVector3;

        public static Vector3 DefaultVector3 => Vector3.positiveInfinity;

        public static Vector3Range Empty => new Vector3Range(DefaultVector3, DefaultVector3);


        public Vector3Range(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Vector3Range(Vector3 start, Vector3 direction, float distance)
        {
            Start = start;
            End = start + direction.normalized * distance;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3Range range)
            {
                return range.Start.Equals(Start) && range.Start.Equals(End);
            }

            return false;
        }

        public override int GetHashCode() => Tuple.Create(Start, End).GetHashCode();

        public static bool operator ==(Vector3Range left, Vector3Range right) => left.Equals(right);

        public static bool operator !=(Vector3Range left, Vector3Range right) => !(left == right);
    }

    static class Vector3RangeTransformExtensions
    {
        public static Vector3Range TransformPoint(this Transform transform, Vector3Range vector3Range)
        {
            return new Vector3Range(
                transform.TransformPoint(vector3Range.Start),
                transform.TransformPoint(vector3Range.End));
        }

        public static Vector3Range InverseTransformPoint(this Transform transform, Vector3Range vector3Range)
        {
            return new Vector3Range(
                transform.InverseTransformPoint(vector3Range.Start),
                transform.InverseTransformPoint(vector3Range.End));
        }
    }
}
