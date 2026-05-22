using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Kéo thả các điểm (object con) vào đây")]
    public Transform[] waypoints;
    public float speed = 5f;

    // Mảng này sẽ lưu chết các tọa độ ngay lúc Start
    private Vector3[] fixedPositions;
    private int currentWaypointIndex = 0;

    void Start()
    {
        // Khởi tạo mảng Vector3 có kích thước bằng với số lượng waypoint bạn gắn vào
        fixedPositions = new Vector3[waypoints.Length];

        // Quét qua các waypoint và lưu lại tọa độ World Space ban đầu của chúng
        for (int i = 0; i < waypoints.Length; i++)
        {
            fixedPositions[i] = waypoints[i].position;
        }
    }

    void Update()
    {
        // Tránh lỗi nếu chưa gán waypoint nào
        if (fixedPositions.Length == 0) return;

        // Lấy tọa độ Vector3 cố định đã lưu, thay vì dùng Transform đang bị tịnh tiến
        Vector3 targetPosition = fixedPositions[currentWaypointIndex];

        // Di chuyển
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= fixedPositions.Length)
            {
                currentWaypointIndex = 0;
            }
        }
    }
}