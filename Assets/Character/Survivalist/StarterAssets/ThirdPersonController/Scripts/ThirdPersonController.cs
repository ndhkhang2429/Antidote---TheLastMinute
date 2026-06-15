using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player - Move")]
        public float MoveSpeed = 3.0f;
        public float SprintSpeed = 6.5f;
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.08f;
        public float SpeedChangeRate = 12.0f;

        [Header("Player - Stamina System")]
        public PlayerStamina playerStamina; // Reference tới hệ thống thể lực

        [Header("RE-Style Body Rotation")]
        [Tooltip("Tốc độ cơ thể xoay để luôn face theo camera (RE style)")]
        public float BodyTurnSpeed = 10f;

        [Header("Jump & Gravity")]
        public float JumpHeight = 1.2f;
        public float Gravity = -18.0f;
        public float JumpTimeout = 0.1f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Crouch Settings")]
        public float CrouchSpeed = 1.5f;
        public float CrouchHeight = 1.2f;
        public float StandHeight = 1.8f;
        public float CrouchCenter = 0.6f;
        public float StandCenter = 0.9f;
        private bool _isCrouching = false;
        private int _animIDCrouch;

        [Header("Camera Control")]
        public Transform CameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        [Range(0.1f, 5f)]
        public float CameraSensitivity = 1f;
        public bool InvertY = false;

        [Header("Camera Smoothing")]
        public float CameraSmoothTime = 0.05f;
        private float _yawVelocity;
        private float _pitchVelocity;

        [Header("Head Look IK")]
        public bool EnableHeadLookIK = true;

        private float _cameraTargetYaw;
        private float _cameraTargetPitch;
        private const float _threshold = 0.01f;

        private float _speed;
        private float _animationBlend;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private bool _hasAnimator;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _animator = GetComponentInChildren<Animator>();
            _hasAnimator = _animator != null;
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            // Tự động tìm PlayerStamina nếu quên kéo thả trên Inspector
            if (playerStamina == null) playerStamina = GetComponent<PlayerStamina>();

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            _cameraTargetYaw = CameraTarget.rotation.eulerAngles.y;
            _cameraTargetPitch = CameraTarget.rotation.eulerAngles.x;

            transform.rotation = Quaternion.Euler(0f, _cameraTargetYaw, 0f);

            if (_hasAnimator)
            {
                _animator.SetFloat("Horizontal", 0f);
                _animator.SetFloat("Vertical", 0f);
            }
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            HandleRotation();
            HandleCrouch();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDCrouch = Animator.StringToHash("isCrouch");
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float targetYaw = _cameraTargetYaw + _input.look.x * CameraSensitivity * 0.01f;
                float targetPitch = _cameraTargetPitch - _input.look.y * CameraSensitivity * 0.01f * (InvertY ? -1f : 1f);

                _cameraTargetYaw = Mathf.SmoothDampAngle(_cameraTargetYaw, targetYaw, ref _yawVelocity, CameraSmoothTime);
                _cameraTargetPitch = Mathf.SmoothDampAngle(_cameraTargetPitch, targetPitch, ref _pitchVelocity, CameraSmoothTime);
            }

            _cameraTargetPitch = ClampAngle(_cameraTargetPitch, BottomClamp, TopClamp);
            CameraTarget.rotation = Quaternion.Euler(_cameraTargetPitch, _cameraTargetYaw, 0f);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x,
                transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void HandleRotation()
        {
            float currentYaw = transform.eulerAngles.y;
            float smoothYaw = Mathf.LerpAngle(currentYaw, _cameraTargetYaw,
                BodyTurnSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
        }

        private void Move()
        {
            bool isMoving = _input.move != Vector2.zero;

            // Xác định xem người chơi CÓ THỂ chạy không
            bool isSprinting = _input.sprint && isMoving && !_isCrouching;

            if (playerStamina != null)
            {
                isSprinting = isSprinting && playerStamina.CanRun;
                // Truyền tín hiệu cho PlayerStamina để xử lý tụt/hồi
                playerStamina.HandleStamina(isSprinting);
            }

            // Gán tốc độ di chuyển
            float targetSpeed = 0f;
            if (!isMoving)
                targetSpeed = 0f;
            else if (_isCrouching)
                targetSpeed = CrouchSpeed;
            else
                targetSpeed = isSprinting ? SprintSpeed : MoveSpeed;

            _speed = Mathf.MoveTowards(_speed, targetSpeed, SpeedChangeRate * Time.deltaTime);
            _animationBlend = Mathf.MoveTowards(_animationBlend, targetSpeed, SpeedChangeRate * Time.deltaTime);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 camForward = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized;
            Vector3 moveDirection = camRight * _input.move.x + camForward * _input.move.y;

            if (_controller != null && _controller.enabled && _controller.gameObject.activeInHierarchy)
            {
                _controller.Move(
                    moveDirection.normalized * (_speed * Time.deltaTime) +
                    new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime
                );
            }

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);

                float inputMagnitude = isMoving ? 1f : 0f;
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

                // Cập nhật lại BlendTree để phản hồi đúng với tốc độ Sprint thực tế
                float speedRatio = !isMoving ? 0f : (isSprinting ? 1f : 0.5f);
                float lerpSpeed = Time.deltaTime * SpeedChangeRate * 2f;

                float newH = Mathf.Lerp(_animator.GetFloat("Horizontal"), _input.move.x * speedRatio, lerpSpeed);
                float newV = Mathf.Lerp(_animator.GetFloat("Vertical"), _input.move.y * speedRatio, lerpSpeed);
                if (Mathf.Abs(newH) < 0.01f) newH = 0f;
                if (Mathf.Abs(newV) < 0.01f) newV = 0f;

                _animator.SetFloat("Horizontal", newH);
                _animator.SetFloat("Vertical", newV);
            }
        }

        private void HandleCrouch()
        {
            if (_input.crouch)
            {
                _isCrouching = !_isCrouching;
                _input.crouch = false;

                _controller.height = _isCrouching ? CrouchHeight : StandHeight;
                _controller.center = new Vector3(0, _isCrouching ? CrouchCenter : StandCenter, 0);

                if (_hasAnimator)
                    _animator.SetBool(_animIDCrouch, _isCrouching);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_animator || !EnableHeadLookIK) return;

            Vector3 lookAtPosition = _mainCamera.transform.position
                                   + _mainCamera.transform.forward * 100f;

            _animator.SetLookAtWeight(1f, 0.05f, 1f, 0.5f, 0.5f);
            _animator.SetLookAtPosition(lookAtPosition);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0f) _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator) _animator.SetBool(_animIDJump, true);
                }

                if (_jumpTimeoutDelta >= 0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_fallTimeoutDelta >= 0f) _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        public void SmoothFaceCameraDirection()
        {
            StartCoroutine(SmoothTurnRoutine());
        }

        private System.Collections.IEnumerator SmoothTurnRoutine()
        {
            float time = 0f;
            float duration = 0.15f;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.Euler(0f, _cameraTargetYaw, 0f);

            while (time < duration)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRot;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Grounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius
            );
        }
    }
}