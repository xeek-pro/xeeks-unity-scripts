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

        private bool IsGrounded => _groundDetect.IsGrounded;

        #endregion

        #region Internal Fields
        // ------------------------------------------------------------------------------------------------------------

        private Animator _animator;
        private Rigidbody _rigidbody;
        private Collider _collider;
        private XeeksDogGroundDetection _groundDetect;

        // ------------------------------------------------------------------------------------------------------------
        #endregion

        #region Unity Messages

        void Start()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _groundDetect = GetComponent<XeeksDogGroundDetection>();
        }

        private void FixedUpdate()
        {
            _animator.SetBool("IsGrounded", IsGrounded);

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

        #endregion

    }
}