using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 200f;

    void Start()
    {
        // Khóa con trỏ chuột vào giữa màn hình và ẩn nó đi để dễ điều khiển
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Chỉ nhận input từ trục ngang của chuột (di chuyển chuột sang trái/phải)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;

        // Xoay toàn bộ Capsule quanh trục Y (trục dọc hướng lên)
        transform.Rotate(Vector3.up * mouseX);
    }
}