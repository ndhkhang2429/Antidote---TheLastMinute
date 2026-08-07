using UnityEngine;

public class RooftopExtractionTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        hasTriggered = true;

        Debug.Log("[ENDING] Player reached the Extraction Point!");
    }
}