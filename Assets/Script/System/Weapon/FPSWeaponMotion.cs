using StarterAssets;
using UnityEngine;

public class FPSWeaponMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponMotionRoot;
    [SerializeField] private Transform weaponBobRoot;
    [SerializeField] private Transform weaponRecoilRoot;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private StarterAssetsInputs input;

    [Header("Mouse Sway")]
    [SerializeField] private float swayPositionAmount = 0.0025f;
    [SerializeField] private float swayRotationAmount = 1.5f;
    [SerializeField] private float maxSwayPosition = 0.035f;
    [SerializeField] private float maxSwayRotation = 4f;
    [SerializeField] private float swaySmoothSpeed = 12f;

    [Header("Walking Bob")]
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobHorizontal = 0.012f;
    [SerializeField] private float walkBobVertical = 0.018f;
    [SerializeField] private float walkBobRotation = 0.8f;

    [Header("Running Bob")]
    [SerializeField] private float runBobSpeed = 12f;
    [SerializeField] private float runBobHorizontal = 0.025f;
    [SerializeField] private float runBobVertical = 0.035f;
    [SerializeField] private float runBobRotation = 1.6f;

    [Header("Bob Smoothing")]
    [SerializeField] private float bobSmoothSpeed = 12f;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Visual Recoil")]
    [SerializeField]
    private Vector3 recoilPosition =
        new Vector3(0f, 0.008f, -0.08f);

    [SerializeField]
    private Vector3 recoilRotation =
        new Vector3(-5f, 1.2f, 1.5f);

    [SerializeField] private float recoilKickSpeed = 25f;
    [SerializeField] private float recoilReturnSpeed = 12f;

    private Vector3 initialMotionPosition;
    private Quaternion initialMotionRotation;

    private Vector3 initialBobPosition;
    private Quaternion initialBobRotation;

    private Vector3 initialRecoilPosition;
    private Quaternion initialRecoilRotation;

    private Vector3 currentRecoilPosition;
    private Vector3 targetRecoilPosition;

    private Vector3 currentRecoilRotation;
    private Vector3 targetRecoilRotation;

    private float bobTimer;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (input == null)
            input = GetComponentInParent<StarterAssetsInputs>();

        if (weaponMotionRoot != null)
        {
            initialMotionPosition = weaponMotionRoot.localPosition;
            initialMotionRotation = weaponMotionRoot.localRotation;
        }

        if (weaponBobRoot != null)
        {
            initialBobPosition = weaponBobRoot.localPosition;
            initialBobRotation = weaponBobRoot.localRotation;
        }

        if (weaponRecoilRoot != null)
        {
            initialRecoilPosition = weaponRecoilRoot.localPosition;
            initialRecoilRotation = weaponRecoilRoot.localRotation;
        }
    }

    private void LateUpdate()
    {
        UpdateSway();
        UpdateBob();
        UpdateRecoil();
    }

    private void UpdateSway()
    {
        if (weaponMotionRoot == null || input == null)
            return;

        Vector2 lookInput = input.look;

        float positionX = Mathf.Clamp(
            -lookInput.x * swayPositionAmount,
            -maxSwayPosition,
            maxSwayPosition
        );

        float positionY = Mathf.Clamp(
            -lookInput.y * swayPositionAmount,
            -maxSwayPosition,
            maxSwayPosition
        );

        Vector3 targetPosition =
            initialMotionPosition +
            new Vector3(positionX, positionY, 0f);

        float rotationX = Mathf.Clamp(
            lookInput.y * swayRotationAmount,
            -maxSwayRotation,
            maxSwayRotation
        );

        float rotationY = Mathf.Clamp(
            -lookInput.x * swayRotationAmount,
            -maxSwayRotation,
            maxSwayRotation
        );

        float rotationZ = Mathf.Clamp(
            lookInput.x * swayRotationAmount * 0.5f,
            -maxSwayRotation,
            maxSwayRotation
        );

        Quaternion targetRotation =
            initialMotionRotation *
            Quaternion.Euler(rotationX, rotationY, rotationZ);

        float smooth = 1f - Mathf.Exp(
            -swaySmoothSpeed * Time.deltaTime
        );

        weaponMotionRoot.localPosition = Vector3.Lerp(
            weaponMotionRoot.localPosition,
            targetPosition,
            smooth
        );

        weaponMotionRoot.localRotation = Quaternion.Slerp(
            weaponMotionRoot.localRotation,
            targetRotation,
            smooth
        );
    }

    private void UpdateBob()
    {
        if (weaponBobRoot == null || characterController == null)
            return;

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        float speed = horizontalVelocity.magnitude;

        bool isMoving =
            characterController.isGrounded &&
            speed > movementThreshold;

        Vector3 targetPosition = initialBobPosition;
        Quaternion targetRotation = initialBobRotation;

        if (isMoving)
        {
            bool isRunning =
                input != null &&
                input.sprint &&
                speed > 3.5f;

            float bobSpeed =
                isRunning ? runBobSpeed : walkBobSpeed;

            float horizontalAmount =
                isRunning ? runBobHorizontal : walkBobHorizontal;

            float verticalAmount =
                isRunning ? runBobVertical : walkBobVertical;

            float rotationAmount =
                isRunning ? runBobRotation : walkBobRotation;

            bobTimer += Time.deltaTime * bobSpeed;

            float horizontal =
                Mathf.Cos(bobTimer * 0.5f) * horizontalAmount;

            float vertical =
                Mathf.Abs(Mathf.Sin(bobTimer)) * verticalAmount;

            targetPosition =
                initialBobPosition +
                new Vector3(horizontal, -vertical, 0f);

            targetRotation =
                initialBobRotation *
                Quaternion.Euler(
                    vertical * rotationAmount * 20f,
                    0f,
                    -horizontal * rotationAmount * 25f
                );
        }
        else
        {
            bobTimer = 0f;
        }

        float smooth =
            1f - Mathf.Exp(-bobSmoothSpeed * Time.deltaTime);

        weaponBobRoot.localPosition = Vector3.Lerp(
            weaponBobRoot.localPosition,
            targetPosition,
            smooth
        );

        weaponBobRoot.localRotation = Quaternion.Slerp(
            weaponBobRoot.localRotation,
            targetRotation,
            smooth
        );
    }

    private void UpdateRecoil()
    {
        if (weaponRecoilRoot == null)
            return;

        targetRecoilPosition = Vector3.Lerp(
            targetRecoilPosition,
            Vector3.zero,
            recoilReturnSpeed * Time.deltaTime
        );

        targetRecoilRotation = Vector3.Lerp(
            targetRecoilRotation,
            Vector3.zero,
            recoilReturnSpeed * Time.deltaTime
        );

        currentRecoilPosition = Vector3.Lerp(
            currentRecoilPosition,
            targetRecoilPosition,
            recoilKickSpeed * Time.deltaTime
        );

        currentRecoilRotation = Vector3.Lerp(
            currentRecoilRotation,
            targetRecoilRotation,
            recoilKickSpeed * Time.deltaTime
        );

        weaponRecoilRoot.localPosition =
            initialRecoilPosition + currentRecoilPosition;

        weaponRecoilRoot.localRotation =
            initialRecoilRotation *
            Quaternion.Euler(currentRecoilRotation);
    }

    public void AddRecoil()
    {
        float randomYaw = Random.Range(
            -recoilRotation.y,
            recoilRotation.y
        );

        float randomRoll = Random.Range(
            -recoilRotation.z,
            recoilRotation.z
        );

        targetRecoilPosition += recoilPosition;

        targetRecoilRotation += new Vector3(
            recoilRotation.x,
            randomYaw,
            randomRoll
        );
    }
}