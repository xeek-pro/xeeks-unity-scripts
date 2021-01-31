using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Xeek
{
    [RequireComponent(typeof(Animator))]
    public class XeeksDogBrain : SerializedMonoBehaviour
    {
        private Animator _animator;
        private Rigidbody _rigidbody;
        private Collider _collider;

        [SerializeField] bool walk = false;
        [SerializeField] private float acceleration = 2.0f;
        [SerializeField] private float maxVelocity = 0.5f;
        [SerializeField] private float groundAdjustment = 0.0f;

        void Start()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
        }

        private void FixedUpdate()
        {
            CheckGround();

            _rigidbody.useGravity = !IsGrounded;

            if (IsGrounded && walk)
            {
                _rigidbody.AddRelativeForce(transform.forward * acceleration * Time.fixedDeltaTime);
                _rigidbody.velocity = Vector3.ClampMagnitude(_rigidbody.velocity, maxVelocity);
                _animator.SetBool("isWalking", _rigidbody.velocity.magnitude > 0.05f);
                _animator.SetFloat("forwardVelocity", _rigidbody.velocity.magnitude * 1.5f);
            }
            else
            {
                _animator.SetBool("isWalking", false);
                _animator.SetFloat("forwardVelocity", 0);
                if(!walk) _rigidbody.velocity = Vector3.zero;
            }

        }

        private (Vector3 Start, Vector3 End, float Radius)? GenerateGroundDetectionFields()
        {
            if (_collider != null)
            {
                return (
                    new Vector3(
                    _collider.bounds.center.x,
                    _collider.bounds.center.y + groundAdjustment,
                    _collider.bounds.center.z + _collider.bounds.size.z / 6),

                    new Vector3(
                    _collider.bounds.center.x,
                    _collider.bounds.center.y + groundAdjustment,
                    _collider.bounds.center.z - _collider.bounds.size.z / 6),

                    _collider.bounds.size.y / 2
                );
            }
            else return null;
        }

        private void OnDrawGizmos()
        {
            _collider = GetComponent<Collider>();

            var groundDetectionFields = GenerateGroundDetectionFields();
            if (groundDetectionFields.HasValue)
            {
                Gizmos.color = new Color(1.0f, 0, 0);
                Gizmos.DrawWireSphere(
                    groundDetectionFields.Value.Start,
                    groundDetectionFields.Value.Radius);

                Gizmos.color = new Color(0, 0, 1.0f);
                Gizmos.DrawWireSphere(
                    groundDetectionFields.Value.End,
                    groundDetectionFields.Value.Radius);
            }
        }

        [ShowInInspector] [ReadOnly] public bool IsGrounded { get; private set; }
        private bool CheckGround()
        {
            var groundDetectionFields = GenerateGroundDetectionFields();

            if (groundDetectionFields.HasValue)
            {
                IsGrounded = Physics.CheckCapsule(
                    groundDetectionFields.Value.Start,
                    groundDetectionFields.Value.End,
                    groundDetectionFields.Value.Radius);
            }

            return groundDetectionFields != null && IsGrounded;
        }
    }
}