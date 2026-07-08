using System.Collections;
using UnityEngine;

public class ElectricalDoor : MonoBehaviour, IQuestRequirement
{
    [Header("Cài đặt Cửa")]
    public Transform hingeTransform;
    [Tooltip("Thường cửa xoay quanh trục Y. Nếu model bị lỗi trục, đổi thành X hoặc Z.")]
    public Vector3 rotationAxis = Vector3.forward; // Mặc định là trục Y (0, 1, 0)
    public float openAngle = -150f;
    public float openSpeed = 2f;

    [Header("Trạng thái")]
    public bool _isOpen = false;

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

    // Trả về null vì không còn cần yêu cầu item nào nữa
    public ItemDataSO GetRequiredItem() => null;

    public bool IsCompleted() => _isOpen;

    // Chỉ hiển thị Mở hoặc Đóng
    public string GetPrompt()
    {
        return _isOpen ? "[F] Đóng tủ điện" : "[F] Mở tủ điện";
    }

    public bool TryUseItem(InventorySystem inv)
    {
        // Bỏ hoàn toàn bước check chìa khóa, trực tiếp thực hiện mở/đóng
        _isOpen = !_isOpen; // Đảo trạng thái

        ToggleInsideColliders(_isOpen);

        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateDoor(_isOpen ? openAngle : 0f));

        Debug.Log(_isOpen ? "Mở tủ điện" : "Đóng tủ điện");

        // (Tùy chọn) Bạn có thể thêm NotificationUI ở đây nếu muốn thông báo mỗi khi mở/đóng
        return true;
    }

    // ── Hàm phụ trợ ──────────────────────────────────────────
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