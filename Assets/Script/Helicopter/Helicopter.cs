using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HelicopterController : MonoBehaviour
{
    [Header("Rotor Settings")]
    public Transform mainRotor;
    public Transform tailRotor;
    public float maxRotorSpeed = 1000f;
    public float rotorAcceleration = 300f;
    public float takeoffSpeed = 700f; // Tốc độ tối thiểu để bay

    [Header("Flight Dynamics")]
    public float liftForce = 50f;
    public float movementSpeed = 20f;
    public float tiltAmount = 25f; // Góc chúi tối đa
    public float tiltSpeed = 2f;
    public float turnSpeed = 50f;

    private Rigidbody rb;
    private float currentRotorSpeed = 0f;
    private bool isEngineOn = false;

    // Inputs
    private float moveHorizontal;
    private float moveVertical;
    private float liftInput;
    private float yawInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Tối ưu vật lý cho trực thăng để không bị lật lung tung
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    void Update()
    {
        HandleInputs();
        HandleRotors();
    }

    void FixedUpdate()
    {
        // Chỉ áp dụng vật lý bay khi cánh quạt đủ tốc độ
        if (currentRotorSpeed >= takeoffSpeed)
        {
            HandleLift();
            HandleMovementAndTilt();
        }
    }

    private void HandleInputs()
    {
        // Bật/tắt động cơ (Ví dụ phím E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            isEngineOn = !isEngineOn;
        }

        moveHorizontal = Input.GetAxis("Horizontal"); // A/D
        moveVertical = Input.GetAxis("Vertical");     // W/S

        // Bay lên / Hạ xuống (Space / Left Shift)
        liftInput = 0f;
        if (Input.GetKey(KeyCode.Space)) liftInput = 1f;
        else if (Input.GetKey(KeyCode.LeftShift)) liftInput = -1f;

        // Xoay đầu máy bay (Q / E hoặc chuột)
        yawInput = 0f;
        if (Input.GetKey(KeyCode.Q)) yawInput = -1f;
        else if (Input.GetKey(KeyCode.R)) yawInput = 1f; // Dùng R vì E đã dùng bật động cơ
    }

    private void HandleRotors()
    {
        // Logic tăng/giảm tốc độ cánh quạt
        if (isEngineOn)
        {
            currentRotorSpeed = Mathf.MoveTowards(currentRotorSpeed, maxRotorSpeed, rotorAcceleration * Time.deltaTime);
        }
        else
        {
            // Từ từ chậm lại và dừng hẳn
            currentRotorSpeed = Mathf.MoveTowards(currentRotorSpeed, 0f, rotorAcceleration * Time.deltaTime * 0.5f);
        }

        // Quay visual của cánh quạt
        if (mainRotor != null)
            mainRotor.Rotate(Vector3.up, currentRotorSpeed * Time.deltaTime);
        if (tailRotor != null)
            tailRotor.Rotate(Vector3.right, currentRotorSpeed * 1.5f * Time.deltaTime); // Đuôi thường quay nhanh hơn
    }

    private void HandleLift()
    {
        // Lực nâng cơ bản để chống lại trọng lực (Hovering) + lực do người chơi nhập
        Vector3 lift = Vector3.up * (Physics.gravity.magnitude + (liftInput * liftForce));
        rb.AddRelativeForce(lift, ForceMode.Acceleration);
    }

    private void HandleMovementAndTilt()
    {
        // 1. Tính toán góc chúi mục tiêu dựa trên input
        float targetPitch = moveVertical * tiltAmount; // Chúi tới/lui (Quay quanh trục X)
        float targetRoll = -moveHorizontal * tiltAmount; // Nghiêng trái/phải (Quay quanh trục Z)

        // Lấy góc Yaw hiện tại và cộng thêm input xoay đầu
        float targetYaw = transform.eulerAngles.y + (yawInput * turnSpeed * Time.fixedDeltaTime);

        // 2. Tạo Quaternion mục tiêu
        Quaternion targetRotation = Quaternion.Euler(targetPitch, targetYaw, targetRoll);

        // 3. Smoothly xoay Rigidbody về hướng chúi
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, tiltSpeed * Time.fixedDeltaTime));

        // 4. (Tùy chọn) Thêm một chút lực đẩy ngang để di chuyển nhanh hơn, 
        // vì đôi khi chỉ dựa vào góc nghiêng để tiến tới là hơi chậm trong game
        Vector3 moveDirection = (transform.forward * moveVertical) + (transform.right * moveHorizontal);
        rb.AddForce(moveDirection * movementSpeed, ForceMode.Acceleration);
    }
}