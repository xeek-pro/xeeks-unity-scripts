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
    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XeeksDogGroundDetection))]
    public class XeeksDogBrain : SerializedMonoBehaviour
    {
        #region Locomotion Properties
        // ------------------------------------------------------------------------------------------------------------

        [BoxGroup("Locomotion")]
        [OdinSerialize]
        public bool Walk { get; set; } = false;

        [BoxGroup("Locomotion")]
        [OdinSerialize]
        public float Acceleration { get; set; } = 2.0f;

        [BoxGroup("Locomotion")]
        [OdinSerialize]
        public float MaxVelocity { get; set; } = 0.5f;

        [BoxGroup("Locomotion")]
        [ReadOnly]
        [OdinSerialize]
        public float CurrentVelocity { get; set; }

        #endregion

        #region Ground Detection Properties
        // ------------------------------------------------------------------------------------------------------------

        [FoldoutGroup("Ground Detection")]
        [OdinSerialize] 
        private LayerMask GroundLayers { get; set; }

        [FoldoutGroup("Ground Detection")]
        [OdinSerialize]
        [ReadOnly]
        public bool IsGrounded { get; private set; }

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
        // ------------------------------------------------------------------------------------------------------------

        private const float DefaultDistanceToGround = 0.25f;

        private Animator _animator;
        private Rigidbody _rigidbody;
        private Collider _collider;

        // ------------------------------------------------------------------------------------------------------------
        #endregion

        #region Unity Messages

        void Start()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            // Auto Calculate Ground Detection Points & guarantee it exists if the list is null or the items are all 
            // unset on start:
            if (GroundDetectionPoints == null || GroundDetectionPoints.All(x => x.Start.Equals(Vector3Range.Empty) && x.End.Equals(Vector3Range.Empty)))
            {
                AutoCalculateGroundDetectionPoints();
            }
        }

        private void FixedUpdate()
        {
            _animator.SetBool("IsGrounded", CheckGround());

            _rigidbody.useGravity = !IsGrounded;

            if (IsGrounded && Walk)
            {
                CurrentVelocity = _rigidbody.velocity.magnitude;

                if(CurrentVelocity >= -1)
                _rigidbody.AddForce(
                    transform.forward * (Acceleration * Time.fixedDeltaTime 
                    /* Add extra acceleration if just starting off */ + (CurrentVelocity <= 0.5f ? 350.0f : 0.0f)));

                _rigidbody.velocity = 
                    Acceleration >= 0.0f ? // Only clamp the velocity if not moving backwards
                    Vector3.ClampMagnitude(_rigidbody.velocity, MaxVelocity) : 
                    _rigidbody.velocity;

                _animator.SetBool("IsWalking", CurrentVelocity > 0.01f);
                //_animator.SetFloat("forwardVelocity", _rigidbody.velocity.magnitude * 1.38f);
                _animator.SetFloat("Vertical", CurrentVelocity * (Acceleration < 0.0f ? -1 : 1));
                //_animator.SetFloat("Horizontal", 1);
            }
            else
            {
                _animator.SetBool("IsWalking", false);
                //_animator.SetFloat("forwardVelocity", 0);
                if (!Walk) _rigidbody.velocity = Vector3.zero;
            }

        }

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

        #region Ground Detection

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
            if(GroundDetectionPoints == null)
                GroundDetectionPoints = new List<Vector3Range>() { Vector3Range.Empty };

            bool toggle = true;
            float increment = 0.0f;

            GroundDetectionPoints = GroundDetectionPoints.Select(x => {
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

                if (IsGrounded = raycastHits.Any()) return IsGrounded;
            }

            return IsGrounded = false;
        }

        #endregion
    }

    #region Custom Editor

    [CustomEditor(typeof(XeeksDogBrain))]
    public class XeeksDogBrainGizmoHandleEditor : OdinEditor
    {
        protected virtual void OnSceneGUI()
        {
            XeeksDogBrain dogBrainTarget = (XeeksDogBrain)target;

            if (dogBrainTarget.DebugGroundDetection && dogBrainTarget.ShowGroundDetectionHandles)
            {
                var updatedGroundDetectionPoints = new List<Vector3Range>();

                foreach (var groundDetectionPoint in dogBrainTarget.GroundDetectionPoints)
                {
                    var groundDetectionWorldPoint = dogBrainTarget.transform.TransformPoint(groundDetectionPoint);

                    EditorGUI.BeginChangeCheck();

                    Vector3 newEndPoint = groundDetectionWorldPoint.End;
                    Vector3 pointDifference = groundDetectionWorldPoint.End - groundDetectionWorldPoint.Start;

                    var newStartPoint = Handles.PositionHandle(groundDetectionWorldPoint.Start, dogBrainTarget.transform.rotation);
                    if(!dogBrainTarget.LockGroundDetectionHandles)
                        newEndPoint = Handles.PositionHandle(groundDetectionWorldPoint.End, dogBrainTarget.transform.rotation);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(dogBrainTarget, "Ground Detection Point Modification");

                        if(dogBrainTarget.LockGroundDetectionHandles)
                        {
                            newEndPoint = newStartPoint + pointDifference;
                        }

                        updatedGroundDetectionPoints.Add(
                            new Vector3Range(
                                dogBrainTarget.transform.InverseTransformPoint(newStartPoint),
                                dogBrainTarget.transform.InverseTransformPoint(newEndPoint)
                        ));
                    }
                    else
                    {
                        updatedGroundDetectionPoints.Add(groundDetectionPoint);
                    }
                }

                dogBrainTarget.GroundDetectionPoints = updatedGroundDetectionPoints;
            }
        }
    }

    #endregion
}