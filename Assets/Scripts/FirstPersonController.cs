using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        public float MoveSpeed = 4.0f;
        public float SprintSpeed = 6.0f;
        public float CrouchSpeed = 2.5f;
        public float RotationSpeed = 1.0f;
        public float SpeedChangeRate = 10.0f;
        [SerializeField] GameObject gun;

        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;

        [Space(10)]
        public float JumpTimeout = 0.1f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.5f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 90.0f;
        public float BottomClamp = -90.0f;

        private float _cinemachineTargetPitch;
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private bool isCrouching = false;

        [SerializeField] private Animator _meleeAnimator;
        [SerializeField] private float punchCooldown = 0.5f;
        private float punchTimer = 0f;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private MeleeController _meleeController;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private Vector3 _cameraRootDefaultPos;
        private Vector3 _cameraRootCrouchPos;
        [SerializeField] private Transform PlayerCameraRoot;
        [SerializeField] private Transform modelTransform;
        [SerializeField] private GunController _gunController;
        [SerializeField] private RuntimeAnimatorController meleeAnimatorController;
        [SerializeField] private RuntimeAnimatorController gunAnimatorController;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform gunTransform;
        private Camera mainCamera; // Reference to the main camera


        private enum WeaponMode { Melee, Gun }
        private WeaponMode currentMode = WeaponMode.Melee; // Start in melee mode

        private const float _threshold = 0.01f;
        private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            mainCamera = _mainCamera.GetComponent<Camera>(); // Get Camera component

            // No renderer changes, just disable gun initially
            if (gunTransform != null)
            {
                gunTransform.gameObject.SetActive(false); // Hide gun initially
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _meleeController = GetComponentInChildren<MeleeController>(true);
            _gunController = GetComponentInChildren<GunController>(true);
            _meleeAnimator.runtimeAnimatorController = meleeAnimatorController;
            _gunController.enabled = false;

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _cameraRootDefaultPos = PlayerCameraRoot.transform.localPosition;
            _cameraRootCrouchPos = _cameraRootDefaultPos + new Vector3(0f, -0.25f, 0f);
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleCrouch();
            HandleShooting();
            HandleWeaponSwitch();
        }

        private void LateUpdate()
        {
            CameraRotation();

            if (gunTransform != null)
            {
                gunTransform.gameObject.SetActive(currentMode == WeaponMode.Gun);
            }
        }

        private void SetHandRenderers(bool enabled)
        {
            // Do nothing, leaving the renderer enabled to keep the character visible
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // Apply vertical rotation (pitch) to camera target only
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

                // Apply horizontal rotation (yaw) to the body
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            float targetSpeed = isCrouching ? CrouchSpeed : (_input.sprint ? SprintSpeed : MoveSpeed);
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            if (_input.move != Vector2.zero)
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_meleeController.IsPunching)
            {
                _meleeAnimator.SetFloat("Speed", 0f);
                _meleeAnimator.SetFloat("Strafe", 0f);
                return;
            }

            Vector3 localInputDir = modelTransform.InverseTransformDirection(inputDirection.normalized);

            float forwardAmount = localInputDir.z;
            float strafeAmount = localInputDir.x;

            _meleeAnimator.SetFloat("Speed", forwardAmount * _speed);
            _meleeAnimator.SetFloat("Strafe", strafeAmount * _speed);

            modelTransform.localRotation = Quaternion.identity;

            float horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;
            _meleeAnimator.speed = (!isCrouching && horizontalVelocity > 0.1f && horizontalVelocity < SprintSpeed)
                ? Mathf.Clamp(Mathf.Lerp(0.5f, 1f, (horizontalVelocity - CrouchSpeed) / (SprintSpeed - CrouchSpeed)), 0.5f, 1f)
                : 1f;
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void HandleCrouch()
        {
            if (_input.crouch && !isCrouching)
            {
                isCrouching = true;
            }
            else if (!_input.crouch && isCrouching)
            {
                isCrouching = false;
            }

            Vector3 targetPos = isCrouching ? _cameraRootCrouchPos : _cameraRootDefaultPos;
            PlayerCameraRoot.localPosition = Vector3.Lerp(PlayerCameraRoot.localPosition, targetPos, Time.deltaTime * 10f);
        }

        private void HandleShooting()
        {
            if (currentMode == WeaponMode.Melee && _input.shoot && punchTimer <= 0f)
            {
                _meleeController.OnShoot();
                punchTimer = punchCooldown;
            }

            if (currentMode == WeaponMode.Gun)
            {
                if (_input.shoot) _gunController?.OnShoot();
                if (_input.reload) _gunController?.OnReload();
            }

            if (punchTimer > 0f)
                punchTimer -= Time.deltaTime;
        }

        private void HandleWeaponSwitch()
        {
            if (_input.switchToMelee && currentMode != WeaponMode.Melee)
            {
                currentMode = WeaponMode.Melee;
                _meleeAnimator.runtimeAnimatorController = meleeAnimatorController;
                _meleeController.enabled = true;
                _gunController.enabled = false;
                gun.SetActive(false);
            }
            else if (_input.switchToGun && currentMode != WeaponMode.Gun)
            {
                currentMode = WeaponMode.Gun;
                _meleeAnimator.runtimeAnimatorController = gunAnimatorController;
                _meleeController.enabled = false;
                _gunController.enabled = true;
                gun.SetActive(true);
            }

            _input.switchToMelee = false;
            _input.switchToGun = false;
            modelRoot.localRotation = Quaternion.Euler(0f, currentMode == WeaponMode.Gun ? 45f : 0f, 0f);
        }
    }
}