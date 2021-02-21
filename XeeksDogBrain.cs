using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Xeek.ToolsAndExtensions;

namespace Xeek
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XeeksDogGroundDetection))]
    public class XeeksDogBrain : SerializedMonoBehaviour
    {
        #region Locomotion Properties
        // ------------------------------------------------------------------------------------------------------------

        [FoldoutGroup("Locomotion")]
        [OdinSerialize]
        public bool Walk { get; set; } = false;

        [FoldoutGroup("Locomotion")]
        [OdinSerialize]
        public float Acceleration { get; set; } = 2.0f;

        [FoldoutGroup("Locomotion")]
        [OdinSerialize]
        public float MaxVelocity { get; set; } = 0.5f;

        [FoldoutGroup("Locomotion")]
        [ReadOnly]
        [OdinSerialize]
        public float CurrentVelocity { get; set; }

        [FoldoutGroup("Animations")]
        [OdinSerialize]
        public XeeksDogLocomotionAnimations LocomotionAnimations { get; set; } = new XeeksDogLocomotionAnimations();

        [FoldoutGroup("Animations")]
        [OdinSerialize]
        public XeeksDogIdleAnimations IdleAnimations { get; set; } = new XeeksDogIdleAnimations();

        private bool IsGrounded => _groundDetect.IsGrounded;

        #endregion

        #region Internal Fields
        // ------------------------------------------------------------------------------------------------------------

        private Animator _animator;
        private Rigidbody _rigidbody;
        private XeeksDogGroundDetection _groundDetect;

        // ------------------------------------------------------------------------------------------------------------
        #endregion

        #region Unity Messages

        void Start()
        {
            _groundDetect = GetComponent<XeeksDogGroundDetection>();
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            LocomotionAnimations.SetupAnimations(_animator);
            IdleAnimations.SetupAnimations(_animator);
        }

        private void FixedUpdate()
        {
            _animator.SetBool("IsGrounded", IsGrounded);

            _rigidbody.useGravity = !IsGrounded;

            if (IsGrounded && Walk)
            {
                CurrentVelocity = _rigidbody.velocity.magnitude;

                // Calculate force:
                Vector3 force = transform.forward * (Acceleration * Time.fixedDeltaTime);

                // Give force an initial boost if starting with a low velocity:
                //if (CurrentVelocity <= 0.5f && Acceleration > 0.0f)
                //    force *= 300.0f;

                // Move with force:
                if (CurrentVelocity >= -1)
                    _rigidbody.AddForce(force);

                // Clamp velocity if moving forward:
                if (MaxVelocity >= 0.0f)
                {
                    _rigidbody.velocity = _rigidbody.velocity.ClampMagnitude(MaxVelocity);
                }
                else
                {
                    _rigidbody.velocity = _rigidbody.velocity.ClampMagnitude(MaxVelocity, -1.0f);
                }

                _animator.SetBool("IsWalking", CurrentVelocity > 0.01f);
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

        private void OnDrawGizmos()
        {

        }

        #endregion
    }
}