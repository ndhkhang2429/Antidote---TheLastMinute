using UnityEngine;
using Cinemachine;

public class RooftopExtractionTrigger : MonoBehaviour
{
    [Header("Ending")]
    [SerializeField]
    private EndingHelicopterController helicopterController;

    [Header("Player Control")]
    [SerializeField]
    private Behaviour[] playerControlsToDisable;

    [Header("Cinematic Camera")]
    [SerializeField] private CinemachineVirtualCamera gameplayVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera approachVirtualCamera;

    [Header("Cinematic Cleanup")]
    [SerializeField] private GameObject[] objectsToHide;
    [SerializeField] private AudioSource[] audioSourcesToStop;

    [Header("Final Objective")]
    [SerializeField] private GameObject endingObjectiveCanvas;

    public bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasTriggered = true;

        Debug.Log(
            "[ENDING] Player reached the Extraction Point!"
        );

        LockPlayerControl();

        CleanupForCinematic();

        EnableApproachCamera();

        if (helicopterController != null)
        {
            helicopterController.StartApproach();
        }
    }

    public void LockPlayerControl()
    {
        if (playerControlsToDisable == null)
            return;

        foreach (Behaviour behaviour in playerControlsToDisable)
        {
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }

        Debug.Log("[ENDING] Player control LOCKED.");
    }

    public void UnlockPlayerControl()
    {
        if (playerControlsToDisable == null)
            return;

        foreach (Behaviour behaviour in playerControlsToDisable)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        Debug.Log("[ENDING] Player control UNLOCKED.");
    }

    private void EnableApproachCamera()
    {
        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.Priority = 10;
        }

        if (approachVirtualCamera != null)
        {
            approachVirtualCamera.Priority = 20;
        }

        Debug.Log("[ENDING] Cinemachine blending to Approach camera.");
    }

    private void CleanupForCinematic()
    {
        if (objectsToHide != null)
        {
            foreach (GameObject obj in objectsToHide)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        if (audioSourcesToStop != null)
        {
            foreach (AudioSource audioSource in audioSourcesToStop)
            {
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.Stop();
            }
        }

        Debug.Log("[ENDING] Cinematic cleanup completed.");
    }

    public void ReturnToPlayerCamera()
    {
        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.Priority = 50;
        }

        if (approachVirtualCamera != null)
        {
            approachVirtualCamera.Priority = 0;
        }

        Debug.Log("[ENDING] Returning to Player camera.");
    }

    public void ShowBoardingObjective()
    {
        if (endingObjectiveCanvas != null)
        {
            endingObjectiveCanvas.SetActive(true);
        }

        Debug.Log("[ENDING] FINAL OBJECTIVE: BOARD THE HELICOPTER");
    }

    public void HideBoardingObjective()
    {
        if (endingObjectiveCanvas != null)
        {
            endingObjectiveCanvas.SetActive(false);
        }

        Debug.Log("[ENDING] Boarding objective hidden.");
    }
}