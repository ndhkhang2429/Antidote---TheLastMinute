using System;
using UnityEngine;

/// <summary>
/// Quản lý tiến trình 1 QuestChain (VD: chuỗi nhiệm vụ tầng trệt) HOÀN TOÀN NGẦM —
/// không có UI nào báo cho player biết đang ở bước nào. Chỉ dùng để backend biết
/// khi nào bắn onStepCompletedEvent (mở cửa, spawn zombie, bật đèn, trigger cutscene...).
/// Player tự nhận biết tiến trình qua Document đọc được + môi trường xung quanh.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Chain đang active (VD: GroundFloor_QuestChain)")]
    [SerializeField] private QuestChainSO activeChain;

    private int currentStepIndex = 0;

    public QuestStepSO CurrentStep =>
        (activeChain != null && currentStepIndex < activeChain.steps.Length)
            ? activeChain.steps[currentStepIndex]
            : null;

    // UI đăng ký các event này để tự cập nhật, không cần Update() polling
    public event Action<QuestStepSO> OnStepChanged;
    public event Action<QuestStepSO> OnStepCompleted;
    public event Action OnChainCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        currentStepIndex = 0;
        AnnounceCurrentStep();
    }

    private void AnnounceCurrentStep()
    {
        if (CurrentStep != null)
            OnStepChanged?.Invoke(CurrentStep);
    }

    // Gọi hàm này từ RoomZone / item pickup / interact script khi điều kiện khớp
    public void ReportEvent(QuestCompletionType type, string id)
    {
        var step = CurrentStep;
        if (step == null) return;
        if (step.completionType != type) return;
        if (step.targetID != id) return;

        CompleteCurrentStep();
    }

    // Dùng cho CustomEvent (VD: boss chết, cutscene xong) — gọi trực tiếp từ code liên quan
    public void ReportCustomEvent(string id)
    {
        ReportEvent(QuestCompletionType.CustomEvent, id);
    }

    private void CompleteCurrentStep()
    {
        var step = CurrentStep;
        OnStepCompleted?.Invoke(step);

        if (step.onStepCompletedEvent != null)
            step.onStepCompletedEvent.Raise();

        currentStepIndex++;

        if (activeChain != null && currentStepIndex >= activeChain.steps.Length)
        {
            OnChainCompleted?.Invoke();
        }
        else
        {
            AnnounceCurrentStep();
        }
    }
}
