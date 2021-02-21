using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeek.ToolsAndExtensions;

namespace Xeek
{
    public class XeeksDogGroundDetection : SerializedMonoBehaviour
    {
        #region Properties
        // ------------------------------------------------------------------------------------------------------------

        [FoldoutGroup("Ground Detection")]
        [OdinSerialize]
        private LayerMask GroundLayers { get; set; }

        [FoldoutGroup("Ground Detection")]
        [OdinSerialize]
        [ReadOnly]
        public bool IsGrounded { get => _isGrounded; set => _ = value; }

        /// <summary>
        /// List of Vector3 positions as pairs (start and end of raycast) to aid in ground detection. 
        /// These are in local coordinates.
        /// </summary>
        [PropertySpace]
        [FoldoutGroup("Ground Detection")]
        [InfoBox("Ground detection raycast start points are green and raycast end points are blue.", InfoMessageType = InfoMessageType.None)]
        [ListDrawerSettings(AlwaysAddDefaultValue = true, CustomAddFunction = nameof(AddGroundDetectionPoint))]
        [OdinSerialize]
        public List<Vector3Range> GroundDetectionPoints { get; set; }
            = new List<Vector3Range>() { Vector3Range.Empty };

        [FoldoutGroup("Ground Detection")]
        [OdinSerialize]
        public bool DebugGroundDetection { get; set; } = false;

        [FoldoutGroup("Ground Detection")]
        [ShowIf(nameof(DebugGroundDetection), Value = true)]
        [OdinSerialize]
        private Color GroundDetectionStartColor { get; set; } = Color.white;

        [FoldoutGroup("Ground Detection")]
        [ShowIf(nameof(DebugGroundDetection), Value = true)]
        [OdinSerialize]
        private Color GroundDetectionEndColor { get; set; } = Color.blue;

        [FoldoutGroup("Ground Detection")]
        [ShowIf(nameof(DebugGroundDetection), Value = true)]
        [OdinSerialize]
        public bool ShowGroundDetectionHandles { get; set; } = false;

        [FoldoutGroup("Ground Detection")]
        [ShowIf("@this." + nameof(ShowGroundDetectionHandles) + " && this." + nameof(DebugGroundDetection))]
        [OdinSerialize]
        public bool LockGroundDetectionHandles { get; set; } = false;

        #endregion

        #region Internal Fields

        private bool _isGrounded = false;

        #endregion

        #region Unity Messages

        void Start()
        {
            // Auto Calculate Ground Detection Points & guarantee it exists if the list is null or the items are all 
            // unset on start:
            if (GroundDetectionPoints == null || GroundDetectionPoints.All(x => x.Start.Equals(Vector3Range.Empty) && x.End.Equals(Vector3Range.Empty)))
            {
                AutoCalculateGroundDetectionPoints();
            }
        }

        void FixedUpdate()
        {
            CheckGround();
        }

        #endregion

        #region Where The Magic Happens

        private Vector3Range AddGroundDetectionPoint()
        {
            return Vector3Range.Empty;
        }

        [FoldoutGroup("Ground Detection")]
        [Button(Name = "Auto Calculate Ground Points")]
        private void AutoCalculateGroundDetectionPoints()
        {
            if (!gameObject.GetBounds(out Bounds bounds))
            {
                Debug.LogError("Failed to get the bounds to calculate ground detection points, do you have a colliders or a renderer?", this);
                return;
            }

            // Guarantee Ground Detection Points exists:
            if (GroundDetectionPoints == null)
                GroundDetectionPoints = new List<Vector3Range>() { Vector3Range.Empty };

            bool toggle = true;
            float increment = 0.0f;

            GroundDetectionPoints = GroundDetectionPoints.Select(x =>
            {
                x = new Vector3Range
                (
                    new Vector3(
                        bounds.center.x,
                        bounds.center.y,
                        bounds.center.z + (toggle ? -increment : increment)),
                    new Vector3(
                        bounds.center.x,
                        bounds.center.y - bounds.extents.y - 0.001f,
                        bounds.center.z + (toggle ? -increment : increment))
                );

                if (toggle) increment += 0.15f;
                toggle = !toggle;

                return x;
            }).ToList();
        }

        private bool CheckGround()
        {
            foreach (var groundDetectionPoint in GroundDetectionPoints)
            {
                if (groundDetectionPoint.Equals(Vector3.positiveInfinity)) continue;

                var groundDetectionPointWorld = transform.TransformPoint(groundDetectionPoint);

                var raycastHits = Physics.RaycastAll
                (
                    groundDetectionPointWorld.Start,
                    groundDetectionPointWorld.Direction,
                    groundDetectionPointWorld.Distance,
                    GroundLayers
                );

                if (_isGrounded = raycastHits.Any()) return _isGrounded;
            }

            return _isGrounded = false;
        }

        #endregion

        #region Debug & Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (GroundDetectionPoints != null && DebugGroundDetection)
            {
                foreach (var groundDetectionPoint in GroundDetectionPoints)
                {
                    if (groundDetectionPoint.IsUnset) continue;

                    var groundDetectionPointWorld = transform.TransformPoint(groundDetectionPoint);

                    Gizmos.color = GroundDetectionStartColor;
                    Gizmos.DrawWireSphere(groundDetectionPointWorld.Start, 0.05f);

                    Gizmos.color = GroundDetectionEndColor;
                    Gizmos.DrawLine(
                        groundDetectionPointWorld.Start,
                        groundDetectionPointWorld.End
                    );

                    Gizmos.DrawWireSphere(groundDetectionPointWorld.End, 0.025f);
                }
            }
        }
#endif
        #endregion
    }

    #region Custom Editor

    [CustomEditor(typeof(XeeksDogGroundDetection))]
    public class XeeksDogGroundDetectionEditor : OdinEditor
    {
        protected virtual void OnSceneGUI()
        {
            XeeksDogGroundDetection groundDetect = (XeeksDogGroundDetection)target;

            if (groundDetect.DebugGroundDetection && groundDetect.ShowGroundDetectionHandles)
            {
                var updatedGroundDetectionPoints = new List<Vector3Range>();

                foreach (var groundDetectionPoint in groundDetect.GroundDetectionPoints)
                {
                    var groundDetectionWorldPoint = groundDetect.transform.TransformPoint(groundDetectionPoint);

                    EditorGUI.BeginChangeCheck();

                    Vector3 newEndPoint = groundDetectionWorldPoint.End;
                    Vector3 pointDifference = groundDetectionWorldPoint.End - groundDetectionWorldPoint.Start;

                    var newStartPoint = Handles.PositionHandle(groundDetectionWorldPoint.Start, groundDetect.transform.rotation);
                    if (!groundDetect.LockGroundDetectionHandles)
                        newEndPoint = Handles.PositionHandle(groundDetectionWorldPoint.End, groundDetect.transform.rotation);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(groundDetect, "Ground Detection Point Modification");

                        if (groundDetect.LockGroundDetectionHandles)
                        {
                            newEndPoint = newStartPoint + pointDifference;
                        }

                        updatedGroundDetectionPoints.Add(
                            new Vector3Range(
                                groundDetect.transform.InverseTransformPoint(newStartPoint),
                                groundDetect.transform.InverseTransformPoint(newEndPoint)
                        ));
                    }
                    else
                    {
                        updatedGroundDetectionPoints.Add(groundDetectionPoint);
                    }
                }

                groundDetect.GroundDetectionPoints = updatedGroundDetectionPoints;
            }
        }
    }

    #endregion
}