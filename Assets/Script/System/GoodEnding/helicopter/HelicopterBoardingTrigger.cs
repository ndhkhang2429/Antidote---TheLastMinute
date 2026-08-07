using UnityEngine;
using Cinemachine;
using System.Collections;

public class HelicopterBoardingTrigger : MonoBehaviour
{
    [Header("Ending")]
    [SerializeField]
    private RooftopExtractionTrigger extractionTrigger;

    [Header("Cinematic Camera")]
    [SerializeField]
    private CinemachineVirtualCamera boardingVirtualCamera;

    [Header("Helicopter")]
    [SerializeField]
    private EndingHelicopterController helicopterController;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasTriggered = true;

        Debug.Log(
            "[ENDING] Player reached Boarding Point!"
        );

        if (extractionTrigger != null)
        {
            extractionTrigger.LockPlayerControl();
            extractionTrigger.HideBoardingObjective();
        }

        StartBoardingCamera();

        StartCoroutine(BoardPlayerRoutine());

        Debug.Log(
            "[ENDING] Final boarding sequence started."
        );
    }

    private void StartBoardingCamera()
    {
        if (boardingVirtualCamera != null)
        {
            boardingVirtualCamera.Priority = 100;

            Debug.Log(
                "[ENDING] Blending to Boarding Camera."
            );
        }
        else
        {
            Debug.LogError(
                "[ENDING] Boarding Virtual Camera is missing!"
            );
        }
    }

    private IEnumerator BoardPlayerRoutine()
    {
        // Chờ camera Boarding blend hoàn tất
        yield return new WaitForSecondsRealtime(1.8f);

        Debug.Log("[ENDING] Player boarded the helicopter.");

        // Giữ shot một chút sau khi boarding
        yield return new WaitForSecondsRealtime(1.0f);

        if (helicopterController != null)
        {
            helicopterController.StartTakeoff();
        }
        else
        {
            Debug.LogError(
                "[ENDING] Helicopter Controller is missing!"
            );
        }
    }
}