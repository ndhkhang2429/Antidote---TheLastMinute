using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectiveDiscoveryTrigger : MonoBehaviour
{
    [Header("Objective")]
    [SerializeField] private string objectiveID = "restore_power";

    [TextArea(2, 4)]
    [SerializeField]
    private string objectiveDescription =
        "Restore power to the hospital";

    [Header("Player Thought")]
    [TextArea(2, 4)]
    [SerializeField]
    private string thoughtText =
        "It's too dark... I need to restore the power before I can find a way out.";

    [Tooltip("Thời gian chờ sau suy nghĩ trước khi hiện objective.")]
    [SerializeField] private float objectiveDelay = 2.5f;

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Tự tắt trigger sau khi đã kích hoạt.")]
    [SerializeField] private bool disableAfterTriggered = true;

    private bool hasTriggered;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();

        if (!triggerCollider.isTrigger)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !other.CompareTag(playerTag))
        {
            return;
        }

        hasTriggered = true;

        StartCoroutine(DiscoverObjective());
    }

    private IEnumerator DiscoverObjective()
    {
        if (!string.IsNullOrWhiteSpace(thoughtText))
        {
            NotificationUI.Instance?.ShowNotification(thoughtText);
        }

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, objectiveDelay)
        );

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogError(
                "[ObjectiveDiscoveryTrigger] Không tìm thấy ObjectiveManager."
            );

            yield break;
        }

        if (!ObjectiveManager.Instance.HasObjective(objectiveID))
        {
            ObjectiveManager.Instance.AddObjective(
                objectiveID,
                objectiveDescription
            );
        }

        if (disableAfterTriggered)
        {
            gameObject.SetActive(false);
        }
    }
}