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
        // TEST ONLY
        if (Input.GetKeyDown(KeyCode.F7))
        {
            StartRooftopTransition();
        }
    }

    public void StartRooftopTransition()
    {
        if (isTransitioning)
            return;

        StartCoroutine(RooftopTransitionRoutine());
    }

    private IEnumerator RooftopTransitionRoutine()
    {
        if (fadeController == null ||
            player == null ||
            rooftopSpawnPoint == null)
        {
            Debug.LogError("[EndingTransition] Missing reference!");
            yield break;
        }

        isTransitioning = true;

        // 1. Fade màn hình thành màu đen
        fadeController.FadeOut(fadeOutDuration);

        yield return new WaitForSecondsRealtime(fadeOutDuration);

        // 2. Teleport player trong lúc màn hình đang đen
        TeleportPlayerToRooftop();

        // 3. Giữ màn hình đen một chút
        yield return new WaitForSecondsRealtime(blackScreenHoldTime);

        // 4. Fade trở lại gameplay
        fadeController.FadeIn(fadeInDuration);

        yield return new WaitForSecondsRealtime(fadeInDuration);

        isTransitioning = false;
    }

    private void TeleportPlayerToRooftop()
    {
        player.SetPositionAndRotation(
            rooftopSpawnPoint.position,
            rooftopSpawnPoint.rotation
        );
    }
}