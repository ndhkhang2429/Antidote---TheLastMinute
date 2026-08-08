using System.Collections;
using UnityEngine;

public class EndingTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EndingFadeController fadeController;
    [SerializeField] private Transform player;
    [SerializeField] private Transform rooftopSpawnPoint;

    [Header("Transition Settings")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float blackScreenHoldTime = 0.5f;
    [SerializeField] private float fadeInDuration = 1.5f;

    private bool isTransitioning = false;

    private void Update()
    {
        // CHEAT - GIỮ LẠI ĐỂ TEST ENDING
        if (Input.GetKeyDown(KeyCode.F7))
        {
            Debug.Log("[EndingTransition] F7 pressed.");

            StartRooftopTransition();
        }
    }

    public void StartRooftopTransition()
    {
        Debug.Log(
            $"[EndingTransition] StartRooftopTransition called. " +
            $"isTransitioning = {isTransitioning}"
        );

        if (isTransitioning)
        {
            Debug.LogWarning(
                "[EndingTransition] Transition already running."
            );

            return;
        }

        // Kiểm tra reference NGAY TẠI ĐÂY
        if (fadeController == null)
        {
            Debug.LogError(
                "[EndingTransition] FadeController is NULL!"
            );

            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "[EndingTransition] Player is NULL!"
            );

            return;
        }

        if (rooftopSpawnPoint == null)
        {
            Debug.LogError(
                "[EndingTransition] RooftopSpawnPoint is NULL!"
            );

            return;
        }

        isTransitioning = true;

        StartCoroutine(RooftopTransitionRoutine());
    }

    private IEnumerator RooftopTransitionRoutine()
    {
        Debug.Log(
            "[EndingTransition] Transition coroutine STARTED."
        );

        // ============================
        // 1. FADE OUT
        // ============================

        Debug.Log("[EndingTransition] Fade OUT started.");

        fadeController.FadeOut(fadeOutDuration);

        yield return new WaitForSecondsRealtime(
            fadeOutDuration + 0.1f
        );

        Debug.Log("[EndingTransition] Fade OUT finished.");

        // ============================
        // 2. TELEPORT
        // ============================

        Debug.Log(
            "[EndingTransition] Teleporting player to rooftop..."
        );

        TeleportPlayerToRooftop();

        Debug.Log(
            $"[EndingTransition] Player teleported to: " +
            $"{player.position}"
        );

        // ============================
        // 3. BLACK HOLD
        // ============================

        yield return new WaitForSecondsRealtime(
            blackScreenHoldTime
        );

        // ============================
        // 4. FADE IN
        // ============================

        Debug.Log("[EndingTransition] Fade IN started.");

        fadeController.FadeIn(fadeInDuration);

        yield return new WaitForSecondsRealtime(
            fadeInDuration + 0.1f
        );

        Debug.Log("[EndingTransition] Fade IN finished.");

        isTransitioning = false;

        Debug.Log(
            "[EndingTransition] Rooftop transition COMPLETE."
        );
    }

    private void TeleportPlayerToRooftop()
    {
        CharacterController characterController =
            player.GetComponent<CharacterController>();

        // Rất quan trọng với FirstPersonController
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        player.SetPositionAndRotation(
            rooftopSpawnPoint.position,
            rooftopSpawnPoint.rotation
        );

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        Physics.SyncTransforms();
    }
}