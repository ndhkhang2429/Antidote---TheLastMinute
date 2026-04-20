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

        [Header("PUBG Head Look Logic")]
        [Tooltip("Góc lệch tối đa giữa Camera và Cơ thể trước khi cơ thể bắt đầu xoay theo")]
        public float MaxHeadTurnAngle = 90f;
        public bool EnableHeadLookIK = true;

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

        [Header("Camera Control")]
        public Transform CameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        [Range(0.1f, 5f)]
        public float CameraSensitivity = 1f;
        public bool InvertY = false; // Đảo ngược trục Y nếu muốn

        [Header("Weight Settings")]
        public float CameraSmoothTime = 0.05f; // Thời gian làm mượt (càng cao càng nặng)
        private float _yawVelocity;
        private float _pitchVelocity;

        private float _cameraTargetYaw;
        private float _cameraTargetPitch;
        private const float _threshold = 0.01f;

        // player vars
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
            _animator = GetComponentInChildren<Animator>();
            _hasAnimator = _animator != null;

            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            _cameraTargetYaw = CameraTarget.transform.rotation.eulerAngles.y;
            _cameraTargetPitch = CameraTarget.transform.rotation.eulerAngles.x;
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            HandleRotation();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation(); // Chuyển CameraRotation xuống LateUpdate
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }
        private void CameraRotation()
        {
            // Nếu có input chuột đủ lớn
            if (_input.look.sqrMagnitude >= _threshold)
            {
                float targetYaw = _cameraTargetYaw + _input.look.x * CameraSensitivity * 0.01f;
                float targetPitch = _cameraTargetPitch - _input.look.y * CameraSensitivity * 0.01f * (InvertY ? -1f : 1f);

                // 0.01f là hệ số "vàng" để cân bằng lại Delta của Input System
                _cameraTargetYaw = Mathf.SmoothDampAngle(_cameraTargetYaw, targetYaw, ref _yawVelocity, CameraSmoothTime);
                _cameraTargetPitch = Mathf.SmoothDampAngle(_cameraTargetPitch, targetPitch, ref _pitchVelocity, CameraSmoothTime);
            }

            // Giới hạn góc nhìn dọc (Pitch)
            _cameraTargetPitch = ClampAngle(_cameraTargetPitch, BottomClamp, TopClamp);
            
            // Áp dụng xoay cho CameraTarget (Xoay tuyệt đối trong không gian)
            CameraTarget.transform.rotation = Quaternion.Euler(_cameraTargetPitch, _cameraTargetYaw, 0.0f);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void HandleRotation()
        {
            float currentBodyYaw = transform.eulerAngles.y;

            // DÙNG BIẾN _cameraTargetYaw THAY VÌ _mainCamera.transform...
            // Điều này giúp ngắt vòng lặp phản hồi gây xoay tít
            float deltaYaw = Mathf.DeltaAngle(currentBodyYaw, _cameraTargetYaw);

            // Trường hợp 1: Đang di chuyển (WASD) -> Người xoay theo hướng nhìn camera
            if (_input.move != Vector2.zero)
            {
                float smoothYaw = Mathf.SmoothDampAngle(currentBodyYaw, _cameraTargetYaw, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
            }
            // Trường hợp 2: Đứng yên (Idle) -> Xử lý lệch góc 90 độ kiểu PUBG
            else
            {
                if (Mathf.Abs(deltaYaw) > MaxHeadTurnAngle)
                {
                    float angleToCatchUp = deltaYaw > 0 ? deltaYaw - MaxHeadTurnAngle : deltaYaw + MaxHeadTurnAngle;
                    float targetYaw = currentBodyYaw + angleToCatchUp;

                    float smoothYaw = Mathf.SmoothDampAngle(currentBodyYaw, targetYaw, ref _rotationVelocity, RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
                }
            }
        }

        private void Move()
        {
            float targetSpeed = 0f;
            if (_input.move != Vector2.zero)
                targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            _speed = Mathf.MoveTowards(_speed, targetSpeed, SpeedChangeRate * Time.deltaTime);

            _animationBlend = Mathf.MoveTowards(_animationBlend, targetSpeed, SpeedChangeRate * Time.deltaTime);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 camForward = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized;

            Vector3 moveDirection = camRight * _input.move.x + camForward * _input.move.y;

            _controller.Move(
                moveDirection.normalized * (_speed * Time.deltaTime) +
                new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime
            );

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);

                float inputMagnitude = _input.move == Vector2.zero ? 0f : 1f;
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

                float speedRatio = _input.move == Vector2.zero ? 0f : (_input.sprint ? 1f : 0.5f);
                float lerpSpeed = Time.deltaTime * SpeedChangeRate * 2f;

                float newH = Mathf.Lerp(_animator.GetFloat("Horizontal"), _input.move.x * speedRatio, lerpSpeed);
                if (Mathf.Abs(newH) < 0.01f) newH = 0f;
                _animator.SetFloat("Horizontal", newH);

                float newV = Mathf.Lerp(_animator.GetFloat("Vertical"), _input.move.y * speedRatio, lerpSpeed);
                if (Mathf.Abs(newV) < 0.01f) newV = 0f;
                _animator.SetFloat("Vertical", newV);
            }
        }

        // Hàm xử lý IK để đầu nhân vật luôn nhìn theo hướng Camera
        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator && EnableHeadLookIK)
            {
                // Lấy một điểm rất xa theo hướng nhìn của Camera làm mục tiêu
                Vector3 lookAtPosition = _mainCamera.transform.position + _mainCamera.transform.forward * 100f;

                // Thiết lập trọng số IK. 
                // Tham số: globalWeight, bodyWeight, headWeight, eyesWeight, clampWeight
                // clampWeight = 0.5f giúp đầu quay tự nhiên, không bị gãy cổ nếu góc quá gắt
                _animator.SetLookAtWeight(1f, 0.2f, 1f, 1f, 0.5f);
                _animator.SetLookAtPosition(lookAtPosition);
            }
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

                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator) _animator.SetBool(_animIDJump, true);
                }

                if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
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

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius
            );
        }
    }
}