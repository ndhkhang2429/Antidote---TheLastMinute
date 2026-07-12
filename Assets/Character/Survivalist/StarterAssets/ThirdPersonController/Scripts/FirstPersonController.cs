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
    public class FirstPersonController : MonoBehaviour // Giữ tên class để không hỏng reference trên Unity
    {
        [Header("Player - Move")]
        public float MoveSpeed = 3.0f;
        public float SprintSpeed = 6.5f;
        public float SpeedChangeRate = 12.0f;

        [Header("Player - Stamina System")]
        public PlayerStamina playerStamina;

        [Header("Player - Audio")]
        public PlayerAudioController playerAudioController;

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

        [Header("Body Pitch Reference")]
        [SerializeField] private Transform _spine;

        [Header("FPS Camera Control")]
        [Tooltip("Điểm neo Camera (phải đặt ở vị trí mắt)")]
        public Transform CameraTarget;
        public float TopClamp = 89.0f; // FPS thường cho ngước lên tối đa 89 độ
        public float BottomClamp = -89.0f; // Góc cúi tối đa
        [Range(0.1f, 5f)]
        public float CameraSensitivity = 1.5f;
        public bool InvertY = false;

        private float _cameraTargetYaw;
        private float _cameraTargetPitch;
        private const float _threshold = 0.01f;

        private float _speed;
        private float _animationBlend;
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
        private bool _hasAnimator;

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _animator = GetComponentInChildren<Animator>();
            _hasAnimator = _animator != null;
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            if (playerStamina == null) playerStamina = GetComponent<PlayerStamina>();

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // Lấy góc quay ban đầu
            _cameraTargetYaw = transform.rotation.eulerAngles.y;
            _cameraTargetPitch = CameraTarget.localRotation.eulerAngles.x;

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
            if (Cursor.lockState != CursorLockMode.Locked) return;
            // Nếu có di chuyển chuột
            if (_input.look.sqrMagnitude >= _threshold)
            {
                // THÊM * 0.01f VÀO 2 DÒNG DƯỚI ĐÂY ĐỂ GIẢM TỐC ĐỘ CHUỘT XUỐNG MỨC BÌNH THƯỜNG
                _cameraTargetYaw += _input.look.x * CameraSensitivity * 0.01f;
                _cameraTargetPitch -= _input.look.y * CameraSensitivity * 0.01f * (InvertY ? -1f : 1f);
            }

            // Giới hạn trục ngước/cúi
            _cameraTargetPitch = ClampAngle(_cameraTargetPitch, BottomClamp, TopClamp);

            // 1. Trục Y (Trái/Phải) xoay TOÀN BỘ CƠ THỂ nhân vật
            transform.rotation = Quaternion.Euler(0f, _cameraTargetYaw, 0f);

            // 2. Trục X (Lên/Xuống) CHỈ XOAY ĐIỂM CAMERA TARGET (Cổ/Mắt)
            CameraTarget.localRotation = Quaternion.Euler(_cameraTargetPitch, 0f, 0f);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x,
                transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void Move()
        {
            bool isMoving = _input.move != Vector2.zero;

            bool isSprinting = _input.sprint && isMoving && !_isCrouching;

            if (playerStamina != null)
            {
                isSprinting = isSprinting && playerStamina.CanRun;
                playerStamina.HandleStamina(isSprinting);
            }

            if (playerAudioController != null)
            {
                playerAudioController.SetMovementState(isMoving, isSprinting);
            }

            float targetSpeed = 0f;
            if (!isMoving) targetSpeed = 0f;
            else if (_isCrouching) targetSpeed = CrouchSpeed;
            else targetSpeed = isSprinting ? SprintSpeed : MoveSpeed;

            _speed = Mathf.MoveTowards(_speed, targetSpeed, SpeedChangeRate * Time.deltaTime);
            _animationBlend = Mathf.MoveTowards(_animationBlend, targetSpeed, SpeedChangeRate * Time.deltaTime);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // Lấy hướng di chuyển trực tiếp từ transform của nhân vật (Vì giờ nhân vật luôn nhìn theo Camera)
            Vector3 moveDirection = transform.right * _input.move.x + transform.forward * _input.move.y;

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