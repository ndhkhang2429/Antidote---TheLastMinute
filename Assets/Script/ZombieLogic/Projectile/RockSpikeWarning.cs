using System.Collections;
using UnityEngine;

public class RockSpikeWarning : MonoBehaviour
{
    [Header("== CÀI ĐẶT TRỤ ĐÁ ==")]
    [Tooltip("Kéo Prefab Trụ Đá Thật (Rock Spike) vào đây")]
    public GameObject realRockSpikePrefab;

    [Tooltip("Thời gian từ lúc hiện vòng đỏ đến khi đá mọc (giây)")]
    public float warningTime = 1.5f;

    [Tooltip("Kích thước tối đa của vòng cảnh báo")]
    public float maxRadius = 3f;

    private void Start()
    {
        StartCoroutine(WarningAndSpawnRoutine());
    }

    private IEnumerator WarningAndSpawnRoutine()
    {
        float timer = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.02f, 0.1f);
        Vector3 endScale = new Vector3(maxRadius, 0.02f, maxRadius);

        // Phóng to dần vòng đỏ
        while (timer < warningTime)
        {
            timer += Time.deltaTime;
            float progress = timer / warningTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            yield return null;
        }

        // Hết thời gian, đẻ ra Trụ Đá thật ngay tại vị trí vòng đỏ
        if (realRockSpikePrefab != null)
        {
            Instantiate(realRockSpikePrefab, transform.position, Quaternion.identity);
        }

        // Tự hủy đĩa đỏ cảnh báo
        Destroy(gameObject);
    }
}