using UnityEngine;

namespace Xeek.ToolsAndExtensions
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// Returns a copy of vector with its magnitude clamped to maxLength and minLength.
        /// </summary>
        /// <remarks>This method exist in Unity with only a maximum. This extension adds a minimum.</remarks>
        public static Vector3 ClampMagnitude(this Vector3 v, float maxLength, float minLength)
        {
            double sm = v.sqrMagnitude;

            if (sm > maxLength * (double)maxLength) return v.normalized * maxLength;
            else if (sm < minLength * (double)minLength) return v.normalized * minLength;

            return v;
        }

        /// <summary>
        /// Returns a copy of vector with its magnitude clamped to maxLength.
        /// </summary>
        public static Vector3 ClampMagnitude(this Vector3 v, float maxLength)
        {
            return Vector3.ClampMagnitude(v, maxLength);
        }
    }
}
