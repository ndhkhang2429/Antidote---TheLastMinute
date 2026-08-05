using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject của cần gạt (Main Switch).
/// Đã tích hợp IQuestRequirement và NotificationUI để hiển thị cảnh báo thông minh.
/// </summary>
public class MainSwitchInteractable : MonoBehaviour, IQuestRequirement
{
    [Header("References")]
    public LightingManager lightingManager;
    public FusePanelManager fusePanelManager;

    [Header("Switch Animation")]
    public Vector3 rotationAxis = Vector3.right;
    public float offAngle = 40f;
    public float onAngle = -40f;
    public float animDuration = 0.3f;

    [Header("Feedback")]
    [Tooltip("Sound khi gạt thành công (tuỳ chọn)")]
    public AudioClip soundSuccess;
    [Tooltip("Sound khi panel chưa sẵn sàng")]
    public AudioClip soundFail;

    private AudioSource _audio;
    private bool isOn = false;
    private bool isAnimating = false;

    void Start()
    {
        transform.localRotation = Quaternion.Euler(rotationAxis * offAngle);
        _audio = GetComponent<AudioSource>();
    }

    // ─────────────────────────────────────────────────────────────
    // 1. THỰC HIỆN CÁC ĐIỀU KHOẢN INTERFACE (IQuestRequirement)
    // ─────────────────────────────────────────────────────────────

    // Cần gạt không yêu cầu người chơi phải cầm cụ thể item gì trên tay khi gạt
    public ItemDataSO GetRequiredItem() => null;

    // Trả về Prompt tương ứng với trạng thái để Raycast hiển thị chữ
    public string GetPrompt()
    {
        return isOn ? "[F] Ngắt nguồn điện tổng" : "[F] Gạt cần kích hoạt nguồn điện tổng";
    }

    public bool IsCompleted() => isOn;

    /// <summary>
    /// Hàm đầu não xử lý khi Player nhìn vào Cần gạt và nhấn F (Gọi từ PlayerInteraction mới)
    /// </summary>
    public bool TryUseItem(InventorySystem inv)
    {
        if (isAnimating) return false;

        // TRƯỜNG HỢP 1: Điện đang BẬT -> Cho phép gạt TẮT tự do không cần điều kiện
        if (isOn)
        {
            isOn = false;
            if (lightingManager != null) lightingManager.SetPower(isOn);

            PlaySound(soundSuccess);
            StartCoroutine(AnimateSwitch(offAngle));

            Debug.Log("[MainSwitch] Power: OFF");
            return true;
        }

        // TRƯỜNG HỢP 2: Điện đang TẮT -> Tiến hành bóc tách điều kiện để BẬT
        if (fusePanelManager != null)
        {
            // Bước A: Kiểm tra xem đã lắp ĐỦ số lượng cầu chì vào các lỗ chưa
            bool missingFuse = false;
            foreach (var slot in fusePanelManager.allSlots)
            {
                // (Lưu ý: Nếu FuseSlot dùng tên biến khác biến 'hasFuse' như isFilled, placedFuse == null... hãy đổi cho đúng)
                if (slot.requiresFuse && !slot.HasFuse)
                {
                    missingFuse = true;
                    break;
                }
            }

            if (missingFuse)
            {
                PlaySound(soundFail);
                NotificationUI.Instance.ShowNotification("Chưa lắp đủ số lượng cầu chì vào bảng điện!");
                return false;
            }

            // Bước B: Đã lắp đủ số lượng nhưng sai vị trí sơ đồ hướng dẫn
            if (!fusePanelManager.CheckAllFuses())
            {
                PlaySound(soundFail);
                NotificationUI.Instance.ShowNotification("Vị trí các cầu chì chưa khớp với sơ đồ chỉ dẫn!");
                return false;
            }

            // Bước C: Cầu chì chuẩn hết rồi nhưng chỉnh sai cụm công tắc phụ
            if (!fusePanelManager.CheckAllSwitches())
            {
                PlaySound(soundFail);
                NotificationUI.Instance.ShowNotification("Cấu hình các công tắc gạt phụ chưa chính xác!");
                return false;
            }
        }

        // VƯỢT QUA TẤT CẢ BỘ LỌC -> KÍCH HOẠT THÀNH CÔNG!
        isOn = true;

        if (lightingManager != null)
            lightingManager.SetPower(isOn);
        else
            Debug.LogWarning("[MainSwitch] Chưa gán LightingManager!");

        PlaySound(soundSuccess);
        StartCoroutine(AnimateSwitch(onAngle));

        NotificationUI.Instance.ShowNotification("Nguồn điện toàn khu bệnh viện đã được khôi phục!");
        Debug.Log("[MainSwitch] Power: ON");
        return true;
    }

    /// <summary>
    /// Giữ lại hàm Interact cũ để không làm gãy các logic gọi code khác của bạn (nếu có)
    /// </summary>
    public void Interact()
    {
        // Điều hướng hàm Interact cũ chạy thông qua hàm bộ lọc mới
        TryUseItem(InventorySystem.Instance);
    }

    public void CheatTurnOnPower()
    {
        if (isOn)
        {
            Debug.Log("[MainSwitch Cheat] Nguồn điện đã được bật trước đó.");
            return;
        }

        StopAllCoroutines();

        isAnimating = false;
        isOn = true;

        if (lightingManager == null)
            lightingManager = LightingManager.Instance;

        if (lightingManager != null)
        {
            lightingManager.SetPower(true);
        }
        else
        {
            Debug.LogWarning(
                "[MainSwitch Cheat] Không tìm thấy LightingManager."
            );
        }

        PlaySound(soundSuccess);
        StartCoroutine(AnimateSwitch(onAngle));

        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.ShowNotification(
                "Cheat: Nguồn điện toàn bệnh viện đã được khôi phục!"
            );
        }

        Debug.Log("[MainSwitch Cheat] Power: ON");
    }

    // ── Animation (Giữ nguyên logic SmoothStep của bạn) ───────────
    IEnumerator AnimateSwitch(float targetAngle)
    {
        isAnimating = true;

        Quaternion startRot = transform.localRotation;
        Quaternion endRot = Quaternion.Euler(rotationAxis * targetAngle);
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        transform.localRotation = endRot;
        isAnimating = false;
    }

    void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}