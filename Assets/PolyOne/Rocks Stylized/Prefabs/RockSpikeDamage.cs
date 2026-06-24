using System.Collections;
using UnityEngine;

public class RockSpikeDamage : MonoBehaviour
{
    [Header("== CÀI ĐẶT SÁT THƯƠNG ==")]
    [SerializeField] private float damageAmount = 25f; // Lượng sát thương
    [SerializeField] private float lifetime = 3.0f;     // Thời gian tồn tại

    [Header("== HIỆU ỨNG MỌC LÊN (RISE UP) ==")]
    [SerializeField] private float riseDuration = 0.15f; // Thời gian đâm lên (Giây) - Càng nhỏ đâm càng nhanh
    [SerializeField] private float riseDepth = 4.0f;     // Bắt đầu từ độ sâu bao nhiêu mét dưới đất

    private bool _hasDealtDamage = false;

    private void Start()
    {
        // Chạy hiệu ứng mọc lên ngay khi Prefab xuất hiện
        StartCoroutine(RiseRoutine());

        // Tự động hủy sau khi hết thời gian tồn tại (cộng thêm thời gian mọc để tránh mất sớm)
        Destroy(gameObject, lifetime + riseDuration);
    }

    private IEnumerator RiseRoutine()
    {
        // 1. Lưu lại vị trí đích (vị trí trên mặt đất mà Vòng Cảnh Báo truyền cho)
        Vector3 targetPosition = transform.position;

        // 2. Dời trụ đá tụt xuống lòng đất theo trục Y
        Vector3 startPosition = targetPosition - new Vector3(0, riseDepth, 0);
        transform.position = startPosition;

        // 3. Vòng lặp đẩy trụ đá trồi lên
        float timer = 0f;
        while (timer < riseDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / riseDuration;

            // Mathf.SmoothStep giúp hiệu ứng đâm lên mượt mà hơn (nhanh ở giữa, hơi chậm lại lúc kết thúc)
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);

            yield return null;
        }

        // 4. Chốt lại vị trí đích để tránh sai số
        transform.position = targetPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasDealtDamage) return;

        if (other.CompareTag("Player"))
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount, gameObject);
                _hasDealtDamage = true;
                Debug.Log("Boss Rock Spike đã đâm trúng Player!");
            }
        }
    }
}