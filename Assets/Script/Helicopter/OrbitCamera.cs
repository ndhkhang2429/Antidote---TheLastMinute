using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Kéo object trực thăng vào đây
    public Vector3 targetOffset = new Vector3(0, 2f, 0); // Điểm nhìn cao hơn tâm trực thăng một chút
    public float distance = 15.0f; // Khoảng cách từ camera đến máy bay

    [Header("Camera Controls")]
    public float mouseSensitivityX = 150.0f;
    public float mouseSensitivityY = 120.0f;

    [Header("Pitch Limits (Y-Axis)")]
    public float yMinLimit = -15f; // Góc nhìn từ dưới lên tối đa
    public float yMaxLimit = 80f;  // Góc nhìn từ trên xuống tối đa

    [Header("Smoothness")]
    public float smoothTime = 10f; // Độ mượt khi camera bám theo

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float currentDistance;

    void Start()
    {
        // Khởi tạo góc xoay ban đầu dựa trên góc hiện tại của camera
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        currentDistance = distance;

        // Tùy chọn: Khóa và ẩn con trỏ chuột khi đang chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Nhận Input từ chuột
        currentX += Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

        // 2. Giới hạn góc nhìn lên/xuống để camera không bị lộn ngược
        currentY = ClampAngle(currentY, yMinLimit, yMaxLimit);

        // 3. Tính toán vị trí và góc xoay mới
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 targetPosition = target.position + targetOffset;
        Vector3 direction = new Vector3(0, 0, -currentDistance);

        Vector3 desiredPosition = targetPosition + rotation * direction;

        // 4. Cập nhật Transform của Camera
        transform.rotation = rotation;
        // Dùng Lerp để camera di chuyển mượt mà theo máy bay thay vì giật cục
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothTime);
    }

    // Hàm phụ trợ để giới hạn góc (clamp angle) chuẩn xác trong hệ 360 độ
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F) angle += 360F;
        if (angle > 360F) angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}