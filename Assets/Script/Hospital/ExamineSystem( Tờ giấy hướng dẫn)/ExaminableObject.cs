using UnityEngine;

/// <summary>
/// Gắn vào các vật thể có thể đọc/xem:
/// giấy, sách, bảng thông báo, hồ sơ...
/// Vật thể vẫn nằm tại chỗ sau khi đọc.
/// </summary>
public class ExaminableObject : MonoBehaviour
{
    private const string ReceptionObjectiveID =
        "find_reception_record";

    private const string IsolationObjectiveID =
        "find_isolation_report";

    private const string GuardLogObjectiveID =
        "find_guard_log";

    [Header("Nội dung hiển thị")]
    [Tooltip("Tên tài liệu hiện trong prompt và Examine UI.")]
    public string objectName = "Document";

    [Tooltip("Nếu tài liệu sử dụng hình ảnh.")]
    public Sprite contentSprite;

    [Tooltip("Nếu tài liệu sử dụng nội dung chữ.")]
    [TextArea(3, 15)]
    public string contentText = "";

    [Header("Âm thanh")]
    [Tooltip("Âm thanh phát khi mở tài liệu.")]
    public AudioClip openSound;

    [Header("Objective Trigger")]
    [Tooltip(
        "Bật riêng cho Emergency Security Notice. " +
        "Khi đóng tài liệu, ba nhiệm vụ tìm manh mối sẽ xuất hiện."
    )]
    [SerializeField] private bool giveClueObjectivesOnClose;

    [Tooltip(
        "Chỉ cho phép kích hoạt objective một lần."
    )]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Objective Text")]
    [SerializeField]
    private string receptionObjectiveText =
        "Search the Information Desk";

    [SerializeField]
    private string isolationObjectiveText =
        "Find the isolation report";

    [SerializeField]
    private string guardLogObjectiveText =
        "Find the chief guard's duty log";

    private bool hasTriggeredObjectives;

    /// <summary>
    /// Được ExamineUIController gọi sau khi người chơi đóng tài liệu.
    /// </summary>
    public void NotifyExamineClosed()
    {
        if (!giveClueObjectivesOnClose)
        {
            return;
        }

        if (triggerOnlyOnce && hasTriggeredObjectives)
        {
            return;
        }

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning(
                $"[ExaminableObject] Không tìm thấy ObjectiveManager: " +
                $"{objectName}",
                this
            );

            return;
        }

        hasTriggeredObjectives = true;

        AddObjectiveIfMissing(
            ReceptionObjectiveID,
            receptionObjectiveText
        );

        AddObjectiveIfMissing(
            IsolationObjectiveID,
            isolationObjectiveText
        );

        AddObjectiveIfMissing(
            GuardLogObjectiveID,
            guardLogObjectiveText
        );

        Debug.Log(
            $"[ExaminableObject] Đã đọc xong '{objectName}' " +
            "và nhận ba nhiệm vụ tìm manh mối."
        );
    }

    private void AddObjectiveIfMissing(
        string objectiveID,
        string description)
    {
        if (!ObjectiveManager.Instance.HasObjective(objectiveID))
        {
            ObjectiveManager.Instance.AddObjective(
                objectiveID,
                description
            );
        }
    }
}