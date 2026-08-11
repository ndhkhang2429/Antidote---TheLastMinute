using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Object chứa danh sách các dòng nhiệm vụ.")]
    [SerializeField] private Transform objectiveContainer;

    [Tooltip("Prefab dùng để tạo một dòng nhiệm vụ.")]
    [SerializeField] private ObjectiveUIItem objectiveItemPrefab;

    [Header("Completed Objective")]
    [Tooltip("Thời gian giữ dòng nhiệm vụ sau khi hoàn thành.")]
    [SerializeField] private float completedDisplayTime = 1.5f;

    // Lưu dữ liệu của tất cả nhiệm vụ đã được nhận.
    private readonly Dictionary<string, ObjectiveData> objectives =
        new Dictionary<string, ObjectiveData>();

    // Lưu các dòng UI hiện đang được hiển thị.
    private readonly Dictionary<string, ObjectiveUIItem> objectiveUIItems =
        new Dictionary<string, ObjectiveUIItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Nhận và hiển thị một nhiệm vụ mới.
    /// </summary>
    public void AddObjective(string objectiveID, string description)
    {
        if (string.IsNullOrWhiteSpace(objectiveID))
        {
            Debug.LogWarning(
                "[ObjectiveManager] Objective ID không được để trống."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            Debug.LogWarning(
                $"[ObjectiveManager] Objective '{objectiveID}' không có nội dung."
            );

            return;
        }

        if (objectives.ContainsKey(objectiveID))
        {
            Debug.LogWarning(
                $"[ObjectiveManager] Objective '{objectiveID}' đã được nhận trước đó."
            );

            return;
        }

        if (objectiveContainer == null || objectiveItemPrefab == null)
        {
            Debug.LogError(
                "[ObjectiveManager] Chưa gán Objective Container hoặc Objective Item Prefab."
            );

            return;
        }

        ObjectiveData newObjective =
            new ObjectiveData(objectiveID, description);

        objectives.Add(objectiveID, newObjective);

        ObjectiveUIItem newUIItem = Instantiate(
            objectiveItemPrefab,
            objectiveContainer
        );

        newUIItem.Initialize(objectiveID, description);

        objectiveUIItems.Add(objectiveID, newUIItem);
    }

    /// <summary>
    /// Thay đổi nội dung của một nhiệm vụ đang hoạt động.
    /// Ví dụ: Find password clues: 1/3.
    /// </summary>
    public void UpdateObjective(
        string objectiveID,
        string newDescription
    )
    {
        if (!objectives.TryGetValue(
                objectiveID,
                out ObjectiveData objective))
        {
            Debug.LogWarning(
                $"[ObjectiveManager] Không tìm thấy objective '{objectiveID}'."
            );

            return;
        }

        if (objective.IsCompleted)
        {
            Debug.LogWarning(
                $"[ObjectiveManager] Objective '{objectiveID}' đã hoàn thành nên không thể cập nhật."
            );

            return;
        }

        objective.Description = newDescription;

        if (objectiveUIItems.TryGetValue(
                objectiveID,
                out ObjectiveUIItem uiItem))
        {
            uiItem.SetDescription(newDescription);
        }
    }

    /// <summary>
    /// Đánh dấu một nhiệm vụ là đã hoàn thành.
    /// </summary>
    public void CompleteObjective(string objectiveID)
    {
        if (!objectives.TryGetValue(
                objectiveID,
                out ObjectiveData objective))
        {
            Debug.LogWarning(
                $"[ObjectiveManager] Không tìm thấy objective '{objectiveID}'."
            );

            return;
        }

        if (objective.IsCompleted)
            return;

        objective.IsCompleted = true;

        if (objectiveUIItems.TryGetValue(
                objectiveID,
                out ObjectiveUIItem uiItem))
        {
            uiItem.MarkCompleted();

            StartCoroutine(
                RemoveCompletedObjective(objectiveID, uiItem)
            );
        }
    }

    /// <summary>
    /// Kiểm tra nhiệm vụ đã từng được nhận hay chưa.
    /// </summary>
    public bool HasObjective(string objectiveID)
    {
        return objectives.ContainsKey(objectiveID);
    }

    /// <summary>
    /// Kiểm tra nhiệm vụ đã hoàn thành hay chưa.
    /// </summary>
    public bool IsObjectiveCompleted(string objectiveID)
    {
        return objectives.TryGetValue(
                   objectiveID,
                   out ObjectiveData objective
               )
               && objective.IsCompleted;
    }

    private IEnumerator RemoveCompletedObjective(
        string objectiveID,
        ObjectiveUIItem uiItem
    )
    {
        yield return StartCoroutine(
            uiItem.FadeOutAndDestroy(completedDisplayTime)
        );

        objectiveUIItems.Remove(objectiveID);
    }
}