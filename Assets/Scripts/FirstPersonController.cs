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

        [Header("Weapon Transforms")]
        [SerializeField] private Transform gunTransform;
        [SerializeField] private Transform meleeTransform;
        [SerializeField] private Transform gunActivePos;
        [SerializeField] private Transform gunRestPos;
        [SerializeField] private Transform meleeActivePos;
        [SerializeField] private Transform meleeRestPos;
        [SerializeField] private float switchSpeed = 10f;

        private float _cinemachineTargetPitch;
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private bool isCrouching = false;

        private Animator _meleeAnimator;
        [SerializeField] private float punchCooldown = 0.5f;
        private float punchTimer = 0f;
        [SerializeField] private float shootCooldown = 0.2f; // Duration to keep shooting animation active
        private float shootTimer = 0f; // Timer for shooting state

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
        private GunController _gunController;
        private bool isUsingGun;
        private const float _threshold = 0.01f;
        [SerializeField] private GameObject gunArms;
        [SerializeField] private GameObject meleeArms;
        [SerializeField] private Animator _gunAnimator;

        private bool isSwitchingWeapon = false;
        private Coroutine currentSwitchCoroutine;

        private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _meleeController = GetComponentInChildren<MeleeController>();
            _gunController = GetComponentInChildren<GunController>();
            _meleeAnimator = meleeArms.GetComponent<Animator>();
            _gunAnimator = gunArms.GetComponent<Animator>();

            // Set only melee arms active at start
            gunArms.SetActive(false);
            meleeArms.SetActive(true);
            isUsingGun = false;

            // Position gun at rest position initially
            gunTransform.position = gunRestPos.position;
            gunTransform.rotation = gunRestPos.rotation;
            // Position melee at active position
            meleeTransform.position = meleeActivePos.position;
            meleeTransform.rotation = meleeActivePos.rotation;

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _cameraRootDefaultPos = PlayerCameraRoot.transform.localPosition;
            _cameraRootCrouchPos = _cameraRootDefaultPos + new Vector3(0f, -0.8f, 0f);
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleCrouch();
            HandleShooting();
            HandleWeaponSwitching();
            HandleReloading();
        }

        private void LateUpdate()
        {
            CameraRotation();
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

                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
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

            float horizontalVelocity = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;
            bool isStrafing = _input.move.y == 0f && Mathf.Abs(_input.move.x) > 0f;

            if (isUsingGun)
            {
                if (_gunController.IsShooting || _gunController.IsReloading)
                {
                    // Override movement anims while shooting or reloading
                    _gunAnimator.SetFloat("Speed", 0f);
                    _gunAnimator.speed = 1f;
                }
                else
                {
                    _gunAnimator.SetFloat("Speed", horizontalVelocity);
                    _gunAnimator.speed = (!isCrouching && !isStrafing)
                        ? Mathf.Clamp(Mathf.Lerp(0.5f, 1f, (horizontalVelocity - CrouchSpeed) / (SprintSpeed - CrouchSpeed)), 0.5f, 1f)
                        : 1f;
                }
            }
            else
            {
                _meleeAnimator.SetFloat("Speed", isCrouching || isStrafing ? 0f : horizontalVelocity);
                _meleeAnimator.speed = (!isCrouching && !isStrafing && horizontalVelocity > 0.1f && horizontalVelocity < SprintSpeed)
                    ? Mathf.Clamp(Mathf.Lerp(0.5f, 1f, (horizontalVelocity - CrouchSpeed) / (SprintSpeed - CrouchSpeed)), 0.5f, 1f)
                    : 1f;
            }
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
                _controller.height = 1.2f;
                _controller.center = new Vector3(0f, 0.6f, 0f);
            }
            else if (!_input.crouch && isCrouching)
            {
                isCrouching = false;
                _controller.height = 2f;
                _controller.center = new Vector3(0f, 1f, 0f);
            }

            Vector3 targetPos = isCrouching ? _cameraRootCrouchPos : _cameraRootDefaultPos;
            PlayerCameraRoot.localPosition = Vector3.Lerp(PlayerCameraRoot.localPosition, targetPos, Time.deltaTime * 10f);
        }

        private void HandleShooting()
        {
            if (_input.shoot && punchTimer <= 0f)
            {
                if (isUsingGun)
                {
                    if (!_gunController.IsReloading)
                    {
                        _gunController.OnShoot();
                        shootTimer = shootCooldown;
                    }
                }
                else
                {
                    _meleeController.OnShoot();
                    punchTimer = punchCooldown;
                }
            }

            if (!isUsingGun && punchTimer > 0f)
                punchTimer -= Time.deltaTime;

            if (isUsingGun && shootTimer > 0f)
                shootTimer -= Time.deltaTime;
        }

        private void HandleWeaponSwitching()
        {
            if (_input.switchToGun)
            {
                SwitchToGun();
                _input.switchToGun = false;
            }
            if (_input.switchToMelee)
            {
                SwitchToMelee();
                _input.switchToMelee = false;
            }
        }

        private void HandleReloading()
        {
            if (isUsingGun && _input.reload && !_gunController.IsReloading)
            {
                _gunController.OnReload();
                _input.reload = false;
                _input.shoot = false; // Prevent queued shooting during reload
            }
        }

        public void SwitchToGun()
        {
            if (isUsingGun || isSwitchingWeapon) return;
            if (currentSwitchCoroutine != null) StopCoroutine(currentSwitchCoroutine);
            currentSwitchCoroutine = StartCoroutine(SwitchWeaponCoroutine(true));
        }

        public void SwitchToMelee()
        {
            if (!isUsingGun || isSwitchingWeapon) return;
            if (currentSwitchCoroutine != null) StopCoroutine(currentSwitchCoroutine);
            currentSwitchCoroutine = StartCoroutine(SwitchWeaponCoroutine(false));
        }

        private IEnumerator SwitchWeaponCoroutine(bool switchToGun)
        {
            isSwitchingWeapon = true;

            float duration = 0.2f;
            float elapsed = 0f;

            // Determine current and new weapon transforms
            Transform currentWeapon = isUsingGun ? gunTransform : meleeTransform;
            Transform currentRest = isUsingGun ? gunRestPos : meleeRestPos;
            Transform newWeapon = switchToGun ? gunTransform : meleeTransform;
            Transform newRest = switchToGun ? gunRestPos : meleeRestPos;
            Transform newActive = switchToGun ? gunActivePos : meleeActivePos;

            // Move current weapon to rest position
            while (elapsed < duration)
            {
                currentWeapon.position = Vector3.Lerp(currentWeapon.position, currentRest.position, elapsed / duration);
                currentWeapon.rotation = Quaternion.Lerp(currentWeapon.rotation, currentRest.rotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            currentWeapon.position = currentRest.position;
            currentWeapon.rotation = currentRest.rotation;

            // Disable current arms and enable new arms
            gunArms.SetActive(switchToGun);
            meleeArms.SetActive(!switchToGun);
            isUsingGun = switchToGun;

            // Reset elapsed time for the new weapon transition
            elapsed = 0f;

            // Ensure new weapon starts at rest position
            newWeapon.position = newRest.position;
            newWeapon.rotation = newRest.rotation;

            // Move new weapon to active position
            while (elapsed < duration)
            {
                newWeapon.position = Vector3.Lerp(newRest.position, newActive.position, elapsed / duration);
                newWeapon.rotation = Quaternion.Lerp(newRest.rotation, newActive.rotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            newWeapon.position = newActive.position;
            newWeapon.rotation = newActive.rotation;

            isSwitchingWeapon = false;
        }
    }
}