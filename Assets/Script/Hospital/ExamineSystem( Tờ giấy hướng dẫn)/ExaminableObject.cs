using System.Collections;
using UnityEngine;

/// <summary>
/// Dùng cho giấy, hồ sơ, sách và bảng thông báo có thể đọc.
///
/// Hỗ trợ hai chức năng:
/// 1. Security Notice: giao ba nhiệm vụ tìm manh mối khi đóng.
/// 2. Clue Document: hoàn thành một objective cụ thể khi đóng.
/// </summary>
public class ExaminableObject : MonoBehaviour
{
    private const string ReceptionObjectiveID =
        "find_reception_record";

    private const string IsolationObjectiveID =
        "find_isolation_report";

    private const string GuardLogObjectiveID =
        "find_guard_log";

    private const string EnterSecurityCodeObjectiveID =
        "enter_security_code";

    [Header("Nội dung hiển thị")]
    [Tooltip("Tên tài liệu hiện trong prompt và Examine UI.")]
    public string objectName = "Document";

    [Tooltip("Hình nền hoặc hình ảnh của tài liệu.")]
    public Sprite contentSprite;

    [Tooltip("Nội dung chữ hiển thị trên tài liệu.")]
    [TextArea(3, 15)]
    public string contentText = "";

    [Header("Âm thanh")]
    [Tooltip("Âm thanh phát khi mở tài liệu.")]
    public AudioClip openSound;

    [Header("Give Clue Objectives On Close")]
    [Tooltip(
        "Chỉ bật cho Emergency Security Notice. " +
        "Khi đóng thông báo, ba nhiệm vụ tìm hồ sơ sẽ xuất hiện."
    )]
    [SerializeField]
    private bool giveClueObjectivesOnClose;

    [Tooltip("Chỉ giao ba objective một lần.")]
    [SerializeField]
    private bool triggerOnlyOnce = true;

    [Header("Clue Objective Text")]
    [SerializeField]
    private string receptionObjectiveText =
        "Search the Information Desk";

    [SerializeField]
    private string isolationObjectiveText =
        "Find the isolation report";

    [SerializeField]
    private string guardLogObjectiveText =
        "Find the chief guard's duty log";

    [Header("Complete Objective On Close")]
    [Tooltip(
        "Bật nếu đây là một hồ sơ manh mối. " +
        "Objective được hoàn thành sau khi player đóng tài liệu."
    )]
    [SerializeField]
    private bool completeObjectiveOnClose;

    [Tooltip(
        "ID của objective mà tài liệu này sẽ hoàn thành."
    )]
    [SerializeField]
    private string objectiveToCompleteID;

    [Tooltip(
        "Thông báo hiện sau khi đọc xong manh mối."
    )]
    [TextArea(2, 4)]
    [SerializeField]
    private string clueFoundMessage;

    [Header("All Clues Completed")]
    [Tooltip(
        "Thời gian chờ trước khi giao nhiệm vụ nhập mật khẩu."
    )]
    [Min(0f)]
    [SerializeField]
    private float nextObjectiveDelay = 2f;

    private bool hasGivenClueObjectives;
    private bool hasCompletedOwnObjective;
    private bool hasStartedNextObjective;

    /// <summary>
    /// ExamineUIController gọi hàm này sau khi player
    /// đóng tài liệu bằng F hoặc ESC.
    /// </summary>
    public void NotifyExamineClosed()
    {
        CompleteOwnObjectiveIfPossible();
        GiveClueObjectivesIfNeeded();
    }

    /// <summary>
    /// Dùng cho Emergency Security Notice.
    /// </summary>
    private void GiveClueObjectivesIfNeeded()
    {
        if (!giveClueObjectivesOnClose)
        {
            return;
        }

        if (triggerOnlyOnce &&
            hasGivenClueObjectives)
        {
            return;
        }

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning(
                $"[ExaminableObject] Không tìm thấy " +
                $"ObjectiveManager khi đọc '{objectName}'.",
                this
            );

            return;
        }

        hasGivenClueObjectives = true;

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
            $"[ExaminableObject] Đã đọc '{objectName}' " +
            "và nhận ba nhiệm vụ tìm manh mối."
        );
    }

    /// <summary>
    /// Dùng cho từng hồ sơ manh mối.
    /// </summary>
    private void CompleteOwnObjectiveIfPossible()
    {
        if (!completeObjectiveOnClose ||
            hasCompletedOwnObjective ||
            string.IsNullOrWhiteSpace(
                objectiveToCompleteID))
        {
            return;
        }

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning(
                $"[ExaminableObject] Không tìm thấy " +
                $"ObjectiveManager khi đọc '{objectName}'.",
                this
            );

            return;
        }

        /*
         * Chỉ hoàn thành nếu objective đã được giao
         * sau khi đọc Emergency Security Notice.
         */
        if (!ObjectiveManager.Instance.HasObjective(
                objectiveToCompleteID))
        {
            NotificationUI.Instance
                ?.ShowNotification(
                    "This document may be important."
                );

            Debug.Log(
                $"[ExaminableObject] Đã đọc '{objectName}' " +
                $"nhưng objective '{objectiveToCompleteID}' " +
                "chưa được nhận."
            );

            return;
        }

        if (!ObjectiveManager.Instance
                .IsObjectiveCompleted(
                    objectiveToCompleteID))
        {
            ObjectiveManager.Instance
                .CompleteObjective(
                    objectiveToCompleteID
                );
        }

        hasCompletedOwnObjective = true;

        if (!string.IsNullOrWhiteSpace(
                clueFoundMessage))
        {
            NotificationUI.Instance
                ?.ShowNotification(
                    clueFoundMessage
                );
        }

        Debug.Log(
            $"[ExaminableObject] Hoàn thành objective " +
            $"'{objectiveToCompleteID}' từ '{objectName}'."
        );

        CheckAllCluesCompleted();
    }

    /// <summary>
    /// Sau khi đủ cả ba hồ sơ, giao nhiệm vụ quay lại
    /// Security Office để nhập mật khẩu.
    /// </summary>
    private void CheckAllCluesCompleted()
    {
        if (ObjectiveManager.Instance == null ||
            hasStartedNextObjective)
        {
            return;
        }

        bool receptionCompleted =
            ObjectiveManager.Instance
                .IsObjectiveCompleted(
                    ReceptionObjectiveID
                );

        bool isolationCompleted =
            ObjectiveManager.Instance
                .IsObjectiveCompleted(
                    IsolationObjectiveID
                );

        bool guardLogCompleted =
            ObjectiveManager.Instance
                .IsObjectiveCompleted(
                    GuardLogObjectiveID
                );

        if (!receptionCompleted ||
            !isolationCompleted ||
            !guardLogCompleted)
        {
            return;
        }

        hasStartedNextObjective = true;

        StartCoroutine(
            GiveEnterCodeObjectiveAfterDelay()
        );
    }

    private IEnumerator
        GiveEnterCodeObjectiveAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            nextObjectiveDelay
        );

        if (ObjectiveManager.Instance == null)
        {
            yield break;
        }

        if (!ObjectiveManager.Instance.HasObjective(
                EnterSecurityCodeObjectiveID))
        {
            ObjectiveManager.Instance.AddObjective(
                EnterSecurityCodeObjectiveID,
                "Enter the code at the Security Office"
            );
        }
    }

    private void AddObjectiveIfMissing(
        string objectiveID,
        string description)
    {
        if (ObjectiveManager.Instance == null)
        {
            return;
        }

        if (!ObjectiveManager.Instance.HasObjective(
                objectiveID))
        {
            ObjectiveManager.Instance.AddObjective(
                objectiveID,
                description
            );
        }
    }
}