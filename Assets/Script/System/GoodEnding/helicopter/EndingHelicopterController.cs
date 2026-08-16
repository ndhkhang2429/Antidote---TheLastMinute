using System.Collections;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndingHelicopterController : MonoBehaviour
{
    private enum EndingPhase
    {
        Idle,
        Approaching,
        Boarding,
        TakingOff,
        Ending
    }

    [Header("Flight Points")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform approachPoint;
    [SerializeField] private Transform hoverPoint;
    [SerializeField] private Transform landingPoint;
    [SerializeField] private Transform takeoffPoint;

    [Header("Components")]
    [SerializeField] private HelicopterRotorController rotorController;

    [Header("Flight Settings")]
    [SerializeField] private float approachDuration = 10f;
    [SerializeField] private float moveToHoverDuration = 4f;
    [SerializeField] private float landingDuration = 5f;

    [Header("Cinematic Cameras")]
    [SerializeField] private CinemachineVirtualCamera approachVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera landingVirtualCamera;

    [Header("Ending Gameplay")]
    [SerializeField]
    private RooftopExtractionTrigger extractionTrigger;

    [Header("Ending Screen")]
    [SerializeField] private EndingFadeController fadeController;
    [SerializeField]
    private CinemachineVirtualCamera finalWideVirtualCamera;

    [SerializeField] private float finalShotHoldTime = 3f;
    [SerializeField] private float finalFadeDuration = 2f;
    [SerializeField]
    private float switchToFinalWideDelay = 3f;
    [SerializeField] private string endingSceneName = "EndingScene";

    [Header("Takeoff Settings")]
    [SerializeField] private float takeoffDuration = 4f;

    [SerializeField] private Transform departurePoint;

    [SerializeField] private CinemachineVirtualCamera departureVirtualCamera;

    [SerializeField] private float departureDuration = 8f;

    [Header("Skip Cutscene")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    [SerializeField] private GameObject skipText;
    [SerializeField] private string skipMessage = "Press SPACE to skip";

    [Tooltip("Thời gian fade nhanh khi skip cảnh cất cánh.")]
    [SerializeField, Min(0f)] private float skipFadeDuration = 0.5f;


    private bool hasStarted = false;
    private bool skipInProgress;
    private EndingPhase currentPhase = EndingPhase.Idle;

    private void Start()
    {
        ConfigureSkipText();
        SetSkipTextVisible(false);

        if (startPoint != null)
        {
            transform.SetPositionAndRotation(
                startPoint.position,
                startPoint.rotation
            );
        }
    }

    private void Update()
    {
        if (skipInProgress || !Input.GetKeyDown(skipKey))
            return;

        switch (currentPhase)
        {
            case EndingPhase.Approaching:
                SkipApproachAndLanding();
                break;

            case EndingPhase.TakingOff:
                SkipTakeoffAndDeparture();
                break;
        }
    }

    public void StartApproach()
    {
        Debug.Log("[ENDING] StartApproach() WAS CALLED!");

        if (hasStarted)
        {
            Debug.LogWarning("[ENDING] Helicopter approach already started!");
            return;
        }

        hasStarted = true;
        currentPhase = EndingPhase.Approaching;
        skipInProgress = false;
        SetSkipTextVisible(true);

        if (rotorController != null)
        {
            Debug.Log("[ENDING] Starting helicopter rotor...");
            rotorController.StartRotor();
        }
        else
        {
            Debug.LogError("[ENDING] Rotor Controller is NULL!");
        }

        StartCoroutine(ApproachRoutine());
    }

    private IEnumerator ApproachRoutine()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = approachPoint.position;

        Vector3 flightDirection =
            targetPosition - startPosition;

        Quaternion targetRotation = startRotation;

        if (flightDirection.sqrMagnitude > 0.001f)
        {
            targetRotation = Quaternion.LookRotation(
                flightDirection.normalized,
                Vector3.up
            );
        }

        float timer = 0f;

        while (timer < approachDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / approachDuration
            );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothT
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothT
            );

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        Debug.Log(
            "[ENDING] Helicopter reached Approach Point."
        );
        yield return StartCoroutine(MoveToHoverPoint());
    }
    private IEnumerator MoveToHoverPoint()
    {
        if (hoverPoint == null)
        {
            Debug.LogError(
                "[EndingHelicopter] HoverPoint is missing!"
            );
            yield break;
        }

        Debug.Log(
            "[ENDING] Helicopter moving to Hover Point..."
        );

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = hoverPoint.position;

        // Quan trọng:
        // Rotation của HoverPoint sẽ quyết định
        // helicopter quay mặt về hướng nào khi chuẩn bị đáp.
        Quaternion targetRotation = hoverPoint.rotation;

        float timer = 0f;

        while (timer < moveToHoverDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer / moveToHoverDuration
            );

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothT
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothT
            );

            yield return null;
        }

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        Debug.Log(
            "[ENDING] Helicopter reached Hover Point."
        );
        SwitchToLandingCamera();

        yield return new WaitForSecondsRealtime(1.5f);

        yield return StartCoroutine(LandingRoutine());
    }

    private IEnumerator LandingRoutine()
    {
        if (landingPoint == null)
        {
            Debug.LogError(
                "[EndingHelicopter] LandingPoint is missing!"
            );

            yield break;
        }

        Debug.Log(
            "[ENDING] Helicopter landing..."
        );

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = landingPoint.position;
        Quaternion targetRotation = landingPoint.rotation;

        float timer = 0f;

        while (timer < landingDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer / landingDuration
            );

            // Hạ xuống nhẹ nhàng
            float smoothT = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothT
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothT
            );

            yield return null;
        }

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        Debug.Log(
            "[ENDING] Helicopter LANDED!"
        );
        yield return new WaitForSecondsRealtime(1.5f);

        ReturnControlToPlayer();
    }
    private void SwitchToLandingCamera()
    {
        if (approachVirtualCamera != null)
            approachVirtualCamera.Priority = 10;

        if (landingVirtualCamera != null)
            landingVirtualCamera.Priority = 30;

        Debug.Log("[ENDING] Switching to Landing camera.");
    }

    private void ReturnControlToPlayer()
    {
        currentPhase = EndingPhase.Boarding;
        skipInProgress = false;
        SetSkipTextVisible(false);

        // Hạ Priority camera landing
        if (landingVirtualCamera != null)
        {
            landingVirtualCamera.Priority = 0;
        }

        // Hạ luôn camera approach
        if (approachVirtualCamera != null)
        {
            approachVirtualCamera.Priority = 0;
        }

        // Trả Player camera + movement
        if (extractionTrigger != null)
        {
            extractionTrigger.ReturnToPlayerCamera();
            extractionTrigger.UnlockPlayerControl();
            extractionTrigger.ShowBoardingObjective();
        }

        Debug.Log(
            "[ENDING] Player can now board the helicopter."
        );
    }

    public void StartTakeoff()
    {
        if (currentPhase == EndingPhase.TakingOff ||
            currentPhase == EndingPhase.Ending)
        {
            return;
        }

        currentPhase = EndingPhase.TakingOff;
        skipInProgress = false;
        SetSkipTextVisible(true);
        StartCoroutine(TakeoffRoutine());
    }

    private IEnumerator TakeoffRoutine()
    {
        if (takeoffPoint == null)
        {
            Debug.LogError(
                "[ENDING] Helicopter Takeoff Point is missing!"
            );

            yield break;
        }

        Debug.Log("[ENDING] Helicopter TAKEOFF started.");

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = takeoffPoint.position;
        Quaternion targetRotation = takeoffPoint.rotation;

        float elapsed = 0f;

        while (elapsed < takeoffDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / takeoffDuration
            );

            // Smooth acceleration/deceleration
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothT
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothT
            );

            yield return null;
        }

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        Debug.Log("[ENDING] Helicopter reached TAKEOFF point.");

        yield return new WaitForSecondsRealtime(1.0f);

        SwitchToDepartureCamera();

        yield return new WaitForSecondsRealtime(1.0f);

        yield return StartCoroutine(DepartureRoutine());
    }

    private IEnumerator DepartureRoutine()
    {
        if (departurePoint == null)
        {
            Debug.LogError(
                "[ENDING] Helicopter Departure Point is missing!"
            );

            yield break;
        }

        Debug.Log("[ENDING] Helicopter departure started.");
        StartCoroutine(SwitchToFinalWideRoutine());

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = departurePoint.position;

        Vector3 direction =
            targetPosition - startPosition;

        Quaternion targetRotation = startRotation;

        if (direction.sqrMagnitude > 0.001f)
        {
            targetRotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );
        }

        float elapsed = 0f;

        while (elapsed < departureDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / departureDuration
            );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothT
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                smoothT
            );

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        Debug.Log(
            "[ENDING] Helicopter reached Departure Point."
        );

        yield return StartCoroutine(FinalEndingRoutine());
    }

    private IEnumerator SwitchToFinalWideRoutine()
    {
        yield return new WaitForSecondsRealtime(
            switchToFinalWideDelay
        );

        if (departureVirtualCamera != null)
        {
            departureVirtualCamera.Priority = 0;
        }

        if (finalWideVirtualCamera != null)
        {
            finalWideVirtualCamera.Priority = 150;
        }

        Debug.Log(
            "[ENDING] Camera locked to Final Wide Shot."
        );
    }
    private IEnumerator FinalEndingRoutine()
    {
        currentPhase = EndingPhase.Ending;
        SetSkipTextVisible(false);

        Debug.Log("[ENDING] Final wide shot holding...");

        // Giữ shot cuối: camera đứng yên,
        // helicopter tiếp tục bay xa.
        yield return new WaitForSecondsRealtime(finalShotHoldTime);

        // Fade to black
        if (fadeController != null)
        {
            fadeController.FadeOut(finalFadeDuration);
        }
        else
        {
            Debug.LogError("[ENDING] Fade Controller is missing!");
        }

        yield return new WaitForSecondsRealtime(finalFadeDuration);

        // Khi đã đen hoàn toàn mới tắt tiếng helicopter
        if (rotorController != null)
        {
            rotorController.StopRotor();
        }

        Debug.Log("[ENDING] Loading Ending Scene...");

        LoadEndingScene();
    }

    private void SwitchToDepartureCamera()
    {
        if (landingVirtualCamera != null)
            landingVirtualCamera.Priority = 0;

        if (approachVirtualCamera != null)
            approachVirtualCamera.Priority = 0;

        if (departureVirtualCamera != null)
            departureVirtualCamera.Priority = 120;

        Debug.Log("[ENDING] Switching to Departure camera.");
    }

    private void SkipApproachAndLanding()
    {
        if (skipInProgress)
            return;

        skipInProgress = true;
        StopAllCoroutines();

        if (landingPoint == null)
        {
            Debug.LogError(
                "[ENDING] Cannot skip approach: Landing Point is missing!"
            );

            skipInProgress = false;
            return;
        }

        transform.SetPositionAndRotation(
            landingPoint.position,
            landingPoint.rotation
        );

        Debug.Log(
            "[ENDING] Approach cutscene skipped. Helicopter snapped to Landing Point."
        );

        ReturnControlToPlayer();
    }

    private void SkipTakeoffAndDeparture()
    {
        if (skipInProgress)
            return;

        skipInProgress = true;
        currentPhase = EndingPhase.Ending;
        SetSkipTextVisible(false);
        StopAllCoroutines();

        SetAllEndingCamerasInactive();
        StartCoroutine(SkipToEndingSceneRoutine());
    }

    private IEnumerator SkipToEndingSceneRoutine()
    {
        if (fadeController != null)
        {
            fadeController.FadeOut(skipFadeDuration);

            if (skipFadeDuration > 0f)
                yield return new WaitForSecondsRealtime(skipFadeDuration);
        }

        LoadEndingScene();
    }

    private void LoadEndingScene()
    {
        currentPhase = EndingPhase.Ending;
        SetSkipTextVisible(false);

        if (rotorController != null)
            rotorController.StopRotor();

        if (string.IsNullOrWhiteSpace(endingSceneName))
        {
            Debug.LogError("[ENDING] Ending Scene Name is empty!");
            return;
        }

        Debug.Log($"[ENDING] Loading scene: {endingSceneName}");
        SceneManager.LoadScene(endingSceneName);
    }

    private void SetAllEndingCamerasInactive()
    {
        if (approachVirtualCamera != null)
            approachVirtualCamera.Priority = 0;

        if (landingVirtualCamera != null)
            landingVirtualCamera.Priority = 0;

        if (departureVirtualCamera != null)
            departureVirtualCamera.Priority = 0;

        if (finalWideVirtualCamera != null)
            finalWideVirtualCamera.Priority = 0;
    }

    private void ConfigureSkipText()
    {
        if (skipText == null)
            return;

        TextMeshProUGUI label = skipText.GetComponent<TextMeshProUGUI>();

        if (label == null)
            label = skipText.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label != null)
            label.text = skipMessage;
    }

    private void SetSkipTextVisible(bool visible)
    {
        if (skipText != null)
            skipText.SetActive(visible);
    }

    private void OnDisable()
    {
        SetSkipTextVisible(false);
    }
}