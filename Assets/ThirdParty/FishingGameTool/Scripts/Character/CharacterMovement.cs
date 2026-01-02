using UnityEngine;
using FishingGameTool.CustomAttribute;
using System;

namespace FishingGameTool.Example
{
    [RequireComponent(typeof(Animator), typeof(CharacterController))]
    public class CharacterMovement : MonoBehaviour
    {
        public enum WitchCamera
        {
            TPP,
            FPP
        };

        [BetterHeader("Character Movement Settings", 20), Space]
        public float _moveSpeed;
        public float _turnSmoothTime;
        public float _gravity;
        public float _gravityAccel;
        [Space]
        public LayerMask _groundMask;
        public Vector3 _groundCheckerSize;
        [Space]
        [BetterHeader("Camera Settings", 20)]
        public Transform _tppCamera;
        public Transform _fppCamera;
        public WitchCamera _witchCamera;

        #region PRIVATE VARIABLES

        private Vector2 _moveInput;
        private Vector3 _moveVel;
        private Vector3 _gravityVel;
        private float _currentGravityAccel;
        private float _turnSmoothVelocity;

        private CharacterController _characterController;
        private Animator _animator;

        #endregion

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();

            WitchCameraControl();
        }

        private void Update()
        {
            WitchCameraControl();
            HandleInput();
        }

        private void WitchCameraControl()
        {
            if (_witchCamera == WitchCamera.TPP)
            {
                _fppCamera.gameObject.SetActive(false);
                _tppCamera.gameObject.SetActive(true);
            }
            else
            {
                _tppCamera.gameObject.SetActive(false);
                _fppCamera.gameObject.SetActive(true);
            }
        }

        private void FixedUpdate()
        {
            Movement();
            Gravity();
        }

        private void Gravity()
        {
            if (IsGrounded())
            {
                _currentGravityAccel = 0f;
                float constantGravity = 2f;
                _gravityVel = Vector3.up * -constantGravity;
            }
            else
            {
                _currentGravityAccel = Mathf.Lerp(_currentGravityAccel, 1f, _gravityAccel * Time.fixedDeltaTime);
                _gravityVel += Vector3.up * _gravity * _currentGravityAccel * Time.fixedDeltaTime;
            }

            _characterController.Move(_gravityVel * Time.fixedDeltaTime);
        }

        private void Movement()
        {
            if (_witchCamera == WitchCamera.TPP)
            {
                Vector3 dir = new Vector3(_moveInput.x, 0f, _moveInput.y);

                AnimationControl(dir);

                if (dir.magnitude >= 0.1f)
                {
                    float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + _tppCamera.eulerAngles.y;
                    float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime * Time.fixedDeltaTime);
                    transform.rotation = Quaternion.Euler(0f, angle, 0f);

                    _moveVel = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                    _characterController.Move(_moveVel.normalized * _moveSpeed * Time.fixedDeltaTime);
                }
            }
            else
            {
                Vector3 dir = transform.right * _moveInput.x + transform.forward * _moveInput.y;

                AnimationControl(dir);

                _characterController.Move(dir * _moveSpeed * Time.fixedDeltaTime);
            }
        }

        private void AnimationControl(Vector3 dir)
        {
            _animator.SetFloat("Walk", dir.magnitude);
        }

        private bool IsGrounded()
        {
            bool isGrounded = Physics.CheckBox(transform.position, new Vector3(_groundCheckerSize.x / 2, _groundCheckerSize.y / 2, _groundCheckerSize.z / 2), transform.rotation, _groundMask);
            return isGrounded;
        }

        private void HandleInput()
        {
            _moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        public Transform GetCurrentCam()
        {
            return _witchCamera == WitchCamera.TPP ? _tppCamera : _fppCamera;
        }

        public void ChangeCamera()
        {
            if (_witchCamera == WitchCamera.TPP)
                _witchCamera = WitchCamera.FPP;
            else
                _witchCamera = WitchCamera.TPP;
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (IsGrounded())
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawWireCube(transform.position, _groundCheckerSize);
        }

#endif
    }
}
