using System.Collections;
using UnityEngine;

public class ElectricalDoor : MonoBehaviour, IQuestRequirement
{
    [Header("Cài đặt Cửa")]
    public Transform hingeTransform;
    [Tooltip("Thường cửa xoay quanh trục Y. Nếu model bị lỗi trục, đổi thành X hoặc Z.")]
    public Vector3 rotationAxis = Vector3.forward; // Mặc định là trục Y (0, 1, 0)
    public float openAngle = -150f; // Cửa thường mở 90 độ thôi
    public float openSpeed = 2f;

    [Header("Yêu cầu Item")]
    public ItemDataSO _requiredKey;

    [Header("Trạng thái")]
    public bool _isOpen = false;
    public bool _isUnlocked = false; // BIẾN MỚI: Ghi nhớ trạng thái ổ khóa

    [Header("Vật phẩm bên trong tủ")]
    [Tooltip("Kéo tất cả Collider của Cầu chì hoặc Cần gạt bên trong tủ vào đây")]
    public Collider[] insideColliders;

    private Coroutine currentAnimation;

    void Start()
    {
        if (hingeTransform == null) hingeTransform = transform;

        // Khởi tạo góc xoay ban đầu
        float startAngle = _isOpen ? openAngle : 0f;
        hingeTransform.localRotation = Quaternion.Euler(rotationAxis * startAngle);

        ToggleInsideColliders(_isOpen);
    }

    public ItemDataSO GetRequiredItem() => _requiredKey;

    public bool IsCompleted() => _isOpen;

    // Cập nhật chữ hiển thị thông minh hơn
    public string GetPrompt()
    {
        if (!_isUnlocked) return "[F] Mở khóa tủ điện"; // Nếu chưa mở khóa
        return _isOpen ? "[F] Đóng tủ điện" : "[F] Mở tủ điện"; // Nếu đã mở khóa
    }

    public bool TryUseItem(InventorySystem inv)
    {
        // ── TRƯỜNG HỢP 1: Ổ KHÓA ĐÃ ĐƯỢC MỞ TỪ TRƯỚC ──
        // Từ nay về sau chỉ cần bấm F là Mở/Đóng tự do, không check chìa nữa
        if (_isUnlocked)
        {
            _isOpen = !_isOpen; // Đảo trạng thái (Mở thành Đóng, Đóng thành Mở)

            ToggleInsideColliders(_isOpen);

            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateDoor(_isOpen ? openAngle : 0f));

            Debug.Log(_isOpen ? "Mở tủ điện" : "Đóng tủ điện");
            return true;
        }

        // ── TRƯỜNG HỢP 2: CỬA ĐANG BỊ KHÓA, CẦN KIỂM TRA CHÌA CHÌA MỚI CHO MỞ ──
        bool hasKeyInHand = inv != null
                           && inv.activeSlot == 4
                           && !inv.heldItemSlot.IsEmpty
                           && inv.heldItemSlot.item == _requiredKey;

        if (hasKeyInHand)
        {
            _isUnlocked = true; // PHÁ KHÓA THÀNH CÔNG! Ghi nhớ vĩnh viễn.
            _isOpen = true;     // Và mở cửa ra luôn

            ToggleInsideColliders(true);

            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateDoor(openAngle));

            inv.ClearItemSlot(); // Tiêu hao chìa khóa

            NotificationUI.Instance.ShowNotification("Đã mở khóa tủ điện!");
            return true;
        }
        else
        {
            string keyName = _requiredKey != null ? _requiredKey.itemName : "Chìa khóa";
            NotificationUI.Instance.ShowNotification($"Cần cầm {keyName} trên tay để mở khóa tủ điện!");
            return false;
        }
    }

    // Hàm phụ trợ
    private void ToggleInsideColliders(bool state)
    {
        if (insideColliders == null) return;
        foreach (var col in insideColliders)
        {
            if (col != null) col.enabled = state;
        }
    }

    // ── Animation ──────────────────────────────────────────
    IEnumerator AnimateDoor(float targetAngle)
    {
        Quaternion startRot = hingeTransform.localRotation;

        // Đã sửa lại để áp dụng trục xoay (rotationAxis) một cách linh hoạt
        Quaternion endRot = Quaternion.Euler(rotationAxis * targetAngle);
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * openSpeed;
            hingeTransform.localRotation = Quaternion.Slerp(startRot, endRot, time);
            yield return null;
        }

        hingeTransform.localRotation = endRot;
    }
}