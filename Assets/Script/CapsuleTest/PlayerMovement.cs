using UnityEngine;

// Dòng này tự động thêm component CharacterController vào Capsule nếu bạn chưa thêm
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;

    [Header("Physics")]
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        // Lấy component Character Controller trên Capsule
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        MovePlayer();
        ApplyGravity();
    }

    private void MovePlayer()
    {
        // Nhận input từ các phím W, A, S, D
        float x = Input.GetAxis("Horizontal"); // A, D
        float z = Input.GetAxis("Vertical");   // W, S

        // Tính toán hướng di chuyển dựa trên hướng mặt của Capsule
        Vector3 move = transform.right * x + transform.forward * z;

        // Kiểm tra xem người chơi có đang giữ phím Left Shift không
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        // Thực hiện lệnh di chuyển
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        // Reset gia tốc rơi nếu nhân vật đang chạm đất
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Ép nhẹ xuống để giữ nhân vật bám sát mặt đất
        }

        // Áp dụng trọng lực rơi xuống
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}