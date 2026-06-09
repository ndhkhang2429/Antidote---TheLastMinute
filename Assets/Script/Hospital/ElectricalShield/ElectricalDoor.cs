using System.Collections;
using UnityEngine;

public class ElectricalDoor : MonoBehaviour, IQuestRequirement
{
    [Header("Cài đặt Cửa")]
    public Transform hingeTransform;
    public float openAngle = -180f;
    public float openSpeed = 2f;

    [Header("Yêu cầu Item")]
    public ItemDataSO _requiredKey;

    [Header("Trạng thái")]
    public bool _isOpen = false;

    [Header("Vật phẩm bên trong tủ")]
    [Tooltip("Kéo tất cả Collider của Cầu chì hoặc Cần gạt bên trong tủ vào đây")]
    public Collider[] insideColliders;

    private Coroutine currentAnimation;

    void Start()
    {
        if (hingeTransform == null) hingeTransform = transform;

        // Cài đặt góc xoay ban đầu dựa theo trạng thái cửa lúc mới vào map
        hingeTransform.localRotation = Quaternion.Euler(0, 0, _isOpen ? openAngle : 0f);

        // Khóa hoặc mở Collider bên trong ngay khi game bắt đầu
        ToggleInsideColliders(_isOpen);
    }

    // ── IQuestRequirement ─────────────────────────────────
    public ItemDataSO GetRequiredItem() => _requiredKey;

    public bool IsCompleted() => _isOpen;

    public string GetPrompt()
    {
        return _isOpen ? "[F] Đóng tủ điện" : "[F] Mở tủ điện";
    }

    public bool TryUseItem(InventorySystem inv)
    {
        if (_isOpen)
        {
            _isOpen = false;

            // CỬA BẮT ĐẦU ĐÓNGG -> Khóa ngay lập tức các vật bên trong
            ToggleInsideColliders(false);

            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateDoor(0f));

            Debug.Log("Đóng tủ điện");
            return true;
        }

        // Kiểm tra điều kiện cầm chìa khóa trên tay (Slot 5 hoạt động)
        bool hasKeyInHand = inv != null
                           && inv.activeSlot == 4
                           && !inv.heldItemSlot.IsEmpty
                           && inv.heldItemSlot.item == _requiredKey;

        if (hasKeyInHand)
        {
            _isOpen = true;

            // CỬA MỞ THÀNH CÔNG -> Giải phóng Collider bên trong để người chơi tương tác
            ToggleInsideColliders(true);

            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateDoor(openAngle));

            inv.ClearItemSlot(); // Mở xong thì tiêu hao chìa khóa trên tay

            Debug.Log("Mở tủ điện thành công!");
            return true;
        }
        else
        {
            string keyName = _requiredKey != null ? _requiredKey.itemName : "Chìa khóa";
            NotificationUI.Instance.ShowNotification($"Cần cầm {keyName} trên tay để mở tủ điện!");
            return false;
        }
    }

    // Hàm phụ trợ bật/tắt nhanh toàn bộ Collider được chỉ định
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
        Quaternion endRot = Quaternion.Euler(0, 0, targetAngle);
        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * openSpeed;
            hingeTransform.localRotation = Quaternion.Slerp(startRot, endRot, time);
            yield return null;
        }

        transform.localRotation = endRot;
    }

    public void InteractWithDoor(bool playerHasKey)
    {
        var inv = InventorySystem.Instance;
        if (inv != null) TryUseItem(inv);
    }
}