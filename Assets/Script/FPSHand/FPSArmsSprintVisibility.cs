using UnityEngine;

/// <summary>
/// Ẩn mesh tay khi Player chạy nước rút và:
/// WeaponType = 1 hoặc WeaponType = 2.
///
/// Các WeaponType khác vẫn hiện tay bình thường.
/// </summary>
public class FPSArmsSprintVisibility : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private CharacterController characterController;

    [SerializeField]
    private Animator playerAnimator;

    [Header("Arm Renderers")]
    [Tooltip(
        "Chỉ kéo Renderer của tay và áo tay vào đây. " +
        "Không kéo Renderer của súng."
    )]
    [SerializeField]
    private Renderer[] armRenderers;

    [Header("Weapon Types")]
    [Tooltip("WeaponType của Rifle.")]
    [SerializeField]
    private int rifleWeaponType = 1;

    [Tooltip("WeaponType của Pistol/Shotgun.")]
    [SerializeField]
    private int secondaryGunWeaponType = 2;

    [Header("Sprint Detection")]
    [Min(0.01f)]
    [SerializeField]
    private float minimumMoveSpeed = 0.3f;

    [SerializeField]
    private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Aiming")]
    [Tooltip(
        "Nếu bật, tay sẽ hiện lại khi đang Aim."
    )]
    [SerializeField]
    private bool keepArmsVisibleWhileAiming = true;

    [SerializeField]
    private int aimMouseButton = 1;

    private bool _armsVisible = true;

    private static readonly int WeaponTypeHash =
        Animator.StringToHash("WeaponType");

    private void Awake()
    {
        FindReferences();

        _armsVisible = false;
        SetArmsVisible(true);
    }

    private void OnEnable()
    {
        FindReferences();

        _armsVisible = false;
        SetArmsVisible(true);
    }

    private void Update()
    {
        bool isMoving = IsPlayerMoving();

        bool isSprinting =
            Input.GetKey(sprintKey);

        bool isHoldingGun =
            IsHoldingGun();

        bool isAiming =
            Input.GetMouseButton(aimMouseButton);

        bool shouldHideArms =
            isMoving &&
            isSprinting &&
            isHoldingGun;

        if (keepArmsVisibleWhileAiming &&
            isAiming)
        {
            shouldHideArms = false;
        }

        SetArmsVisible(!shouldHideArms);
    }

    private void FindReferences()
    {
        if (characterController == null)
        {
            characterController =
                GetComponent<CharacterController>();

            if (characterController == null)
            {
                characterController =
                    GetComponentInParent<CharacterController>();
            }
        }

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponent<Animator>();

            if (playerAnimator == null)
            {
                playerAnimator =
                    GetComponentInParent<Animator>();
            }

            if (playerAnimator == null)
            {
                playerAnimator =
                    GetComponentInChildren<Animator>();
            }
        }
    }

    private bool IsPlayerMoving()
    {
        if (characterController != null &&
            characterController.enabled)
        {
            Vector3 velocity =
                characterController.velocity;

            velocity.y = 0f;

            return velocity.magnitude >=
                   minimumMoveSpeed;
        }

        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        return new Vector2(
            horizontal,
            vertical
        ).sqrMagnitude > 0.01f;
    }

    private bool IsHoldingGun()
    {
        if (playerAnimator == null)
            return false;

        int weaponType =
            playerAnimator.GetInteger(
                WeaponTypeHash
            );

        return weaponType == rifleWeaponType ||
               weaponType == secondaryGunWeaponType;
    }

    private void SetArmsVisible(bool visible)
    {
        if (_armsVisible == visible)
            return;

        _armsVisible = visible;

        if (armRenderers == null)
            return;

        for (int i = 0;
             i < armRenderers.Length;
             i++)
        {
            Renderer armRenderer =
                armRenderers[i];

            if (armRenderer != null)
                armRenderer.enabled = visible;
        }
    }

    private void OnDisable()
    {
        SetArmsVisible(true);
    }
}