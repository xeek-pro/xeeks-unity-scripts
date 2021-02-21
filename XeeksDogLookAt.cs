using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeek.ToolsAndExtensions;

namespace Xeek
{
    public class XeeksDogLookAt : SerializedMonoBehaviour
    {
        #region Configuration
        // ------------------------------------------------------------------------------------------------------------

        [FoldoutGroup("Look Configuration")]
        [PropertyTooltip("This enabled or disabled the ability for this object to look at another object.")]
        [OdinSerialize]
        public bool LookActive
        {
            get => _lookActive;
            set
            {
                _lookActiveChanged = _lookActive != value;
                _lookActive = value;
            }
        }

        [FoldoutGroup("Look Configuration")]
        [PropertyTooltip("Where to look from. If this is null, this object's head is automatically detected.")]
        [InlineButton(nameof(DetectLookFrom), Label = "Auto Detect")]
        [OdinSerialize]
        public Transform LookFrom { get; set; }

        [FoldoutGroup("Look Configuration")]
        [PropertyTooltip("What to look at. If this is null, the player's head is automatically detected.")]
        [InlineButton(nameof(DetectLookAtTarget), Label = "Auto Detect")]
        [OdinSerialize]
        public Transform LookAtTarget { get; set; }

        [FoldoutGroup("Look Configuration")]
        [PropertyTooltip("How quickly the look occurs.")]
        [PropertyRange(0, 10)]
        [OdinSerialize]
        public float LookSpeed { get; set; } = 3f;

        [FoldoutGroup("Look Configuration")]
        [PropertyTooltip("The delayed time the look at target position is updated in seconds.")]
        [InfoBox("This setting is only available when " + nameof(SlerpEnabled) + " is enabled.", visibleIfMemberName: nameof(SlerpDisabled), InfoMessageType = InfoMessageType.None)]
        [EnableIf(nameof(SlerpEnabled))]
        [PropertyRange(0, 1)]
        [OdinSerialize]
        public float TargetTrackingInterval { get; set; } = 0.15f;

        [FoldoutGroup("Look Configuration")]
        [PropertyTooltip("When checked, gradually try to look at the target rather than right away.")]
        [OdinSerialize]
        public bool SlerpEnabled { get; set; } = true;
        private bool SlerpDisabled => !SlerpEnabled;

        #endregion

        #region Look Limits

        [FoldoutGroup("Look Limits")]
        [PropertyTooltip("If the look angle is at or beyond " + nameof(MaxLookAngle) + " stop trying to look at the target and return to the original rotation.")]
        [OdinSerialize]
        public bool StopLookingOnMaxAngle { get; set; } = true;

        [FoldoutGroup("Look Limits")]
        [PropertyTooltip("When this angle is required to look at, the look from will return to it's original rotation.")]
        [PropertyRange(0, 180)]
        [OdinSerialize]
        public float MaxLookAngle { get; set; } = 85.0f;

        [FoldoutGroup("Look Limits")]
        [PropertyTooltip("If the look at target is obstructed in the look from's view, stop looking at it.")]
        [OdinSerialize]
        public bool StopLookingOnObstruction { get; set; } = true;

        #endregion

        #region Look Adjustments
        // ------------------------------------------------------------------------------------------------------------

        [FoldoutGroup("Look Adjustments")]
        [InfoBox("Multiplied by that rotation needed to look at a target to adjust it. You may have to play with this " +
            "in play-mode until the " + nameof(LookFrom) + " object is looking at the " + nameof(LookAtTarget) + " object.", InfoMessageType.None)]
        [OdinSerialize]
        private Vector3 LookRotationOffset { get; set; } = new Vector3(80, 180, 180);

        [FoldoutGroup("Look Adjustments")]
        [InfoBox("Used to determine what is Up for the " + nameof(LookFrom) + " transform. You may have to play with this " +
            "in play-mode and use trial-and error if it's something other than Up.", InfoMessageType.None)]
        [ValueDropdown(nameof(LookUpDirections))]
        [OdinSerialize]
        private Vector3 LookUpDirection { get; set; } = Vector3.up;

        private static readonly IEnumerable LookUpDirections = new ValueDropdownList<Vector3>()
        {
            { "Up", Vector3.up },
            { "Forward", Vector3.forward },
            { "Back", Vector3.back },
            { "Right", Vector3.right },
            { "Left", Vector3.left },
            { "UpNegative", -Vector3.up },
            { "ForwardNegative", -Vector3.forward },
            { "BackNegative", -Vector3.back },
            { "RightNegative", -Vector3.right },
            { "LeftNegative", -Vector3.left }
        };

        #endregion

        #region Internal Fields
        // ------------------------------------------------------------------------------------------------------------

        private bool _lookActive = true;
        private bool _lookActiveChanged = false;

        private Quaternion _originalLookFromRotation;
        private Quaternion _toRotation;
        private Quaternion _lastRotation;

        private Vector3 _lastTargetPosition = Vector3.zero;
        private float _lastTargetDistance = 0.0f;
        private bool _targetPositionChanged = false;

        // ------------------------------------------------------------------------------------------------------------
        #endregion

        #region Unity Messages

        void Start()
        {
            if (LookFrom == null) DetectLookFrom();
            if (LookAtTarget == null) DetectLookAtTarget();

            if (LookFrom)
            {
                _toRotation = _lastRotation = _originalLookFromRotation = LookFrom.rotation;
            }

            StartCoroutine(TrackLookAtTargetPositionCoroutine());
        }

        private void LateUpdate()
        {
            if (LookFrom)
            {
                bool updateLookRotation =
                    _targetPositionChanged ||               // The look at target moved
                    _toRotation == Quaternion.identity ||   // The look to rotation hasn't been set for the first time
                    (_lookActiveChanged && LookActive);     // LookActive has been enabled   

                // Calculate what direction and rotation is needed to look at the target:
                if (LookActive && LookAtTarget && updateLookRotation)
                {
                    var localUp = Vector3.Normalize(LookFrom.localPosition - LookUpDirection);
                    var targetDirection = Vector3.Normalize(LookAtTarget.position - LookFrom.position);

                    // Model heads can be different, the LookRotationOffset is to correct the rotation to correlate
                    // with the rotation that would look at LookAtTarget's direction:
                    _toRotation = Quaternion.LookRotation(targetDirection, localUp) * Quaternion.Euler(LookRotationOffset);

                    RaycastHit hit = new RaycastHit();
                    if (StopLookingOnObstruction) Physics.Raycast(LookFrom.position, targetDirection, out hit);

                    // If the max angle is reached or the LookAtTarget is obstructed from view, rotate back to the
                    // original LookFrom rotation:
                    if (Quaternion.Angle(_toRotation, _originalLookFromRotation) > MaxLookAngle || (StopLookingOnObstruction && hit.transform != LookAtTarget.transform))
                    {
                        if(StopLookingOnMaxAngle) _toRotation = _originalLookFromRotation;
                        else _toRotation = _lastRotation;
                    }
                }

                // If LookActive is disabled, the rotation should get set back to the original LookFrom's rotation:
                if (_lookActiveChanged && !LookActive || !LookAtTarget)
                {
                    _toRotation = _originalLookFromRotation;
                }

                // Where the actual rotation (Look) occurs:
                if (SlerpEnabled)
                {
                    LookFrom.rotation = Quaternion.Slerp(_lastRotation, _toRotation, LookSpeed * Time.fixedDeltaTime);
                    _lastRotation = LookFrom.rotation;
                }
                else
                {
                    LookFrom.rotation = _toRotation;
                }

                // Reset:
                _lookActiveChanged = false;
                _targetPositionChanged = false;
            }
        }

        //private void OnDrawGizmos()
        //{
        //    var targetDirection = Vector3.Normalize(LookAtTarget.position - LookFrom.position);
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawRay(LookFrom.position, targetDirection);
        //}

        #endregion

        #region Target Tracking

        /// <summary>
        /// Track the look at target as a coroutine, after the target position has changed, _targetPositionChanged
        /// needs to be set to false for tracking to continue.
        /// </summary>
        /// <returns>An IEnumerator designed for Unity's StartCoroutine.</returns>
        private IEnumerator TrackLookAtTargetPositionCoroutine()
        {
            // If slerping is disabled, track the target every frame, additionally, switch to wait every frame if
            // look at is not active:
            if (SlerpEnabled && LookActive) yield return new WaitForSeconds(TargetTrackingInterval);
            else yield return new WaitForFixedUpdate();

            if (LookActive && LookAtTarget && !_targetPositionChanged /* don't update until reset */)
            {
                var targetPosition = LookAtTarget.position;
                var targetDistance = (LookFrom.localPosition - LookUpDirection).magnitude;

                _targetPositionChanged = targetPosition != _lastTargetPosition || targetDistance != _lastTargetDistance;
                _lastTargetPosition = targetPosition;
                _lastTargetDistance = targetDistance;
            }

            // Continuous
            StartCoroutine(TrackLookAtTargetPositionCoroutine());
        }

        #endregion

        #region Auto Detection

        private void DetectLookFrom()
        {
            var headObject = transform.FindRecursively("head", ignoreCase: true);

            if (headObject != null)
            {
                Debug.Log("LookFrom was not specified so a head object was auto detected", gameObject);
                LookFrom = headObject.transform;
            }
        }

        private void DetectLookAtTarget()
        {
            // Using FindObjectsOfTypeAll can find inactive and active objects whereas FindWithTag can only find 
            // active objects.
            var playerObject = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.CompareTag("Player")).FirstOrDefault();
            //var playerObject = GameObject.FindWithTag("Player");

            if (playerObject)
            {
                var headObject = playerObject.transform.FindRecursively("head", ignoreCase: true);
                if (headObject != null)
                {
                    Debug.Log("LookAtTarget was not specified so a player head object was auto detected", gameObject);
                    LookAtTarget = headObject.transform;
                }
            }
        }

        #endregion
    }
}