using UnityEngine;

/// <summary>
/// Hệ thống báo động trung tâm (singleton). Bất kỳ cơ chế nào trong game cần "gọi cả đàn zombie tới"
/// chỉ cần gọi AlarmSystem.SpawnHorde(...) - không cần biết prefab zombie nào, số lượng bao nhiêu,
/// hệ thống tự random loại (trong danh sách 6 loại đã gán 1 lần) và spawn từ các điểm được truyền vào.
/// </summary>
public class AlarmSystem : MonoBehaviour
{
    public static AlarmSystem Instance { get; private set; }

    [Header("Danh sách Zombie Prefabs (gán đủ 6 loại 1 lần duy nhất)")]
    [SerializeField] private GameObject[] zombiePrefabs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Spawn 1 đợt zombie: random loại (trong zombiePrefabs), random điểm spawn (trong spawnPoints),
    /// mỗi zombie spawn ra sẽ tự động lao thẳng tới player (qua ZombieBase.ForceChasePlayer()).
    /// </summary>
    /// <param name="spawnPoints">Danh sách điểm có thể spawn (nên đặt trên/gần NavMesh)</param>
    /// <param name="count">Số lượng zombie muốn spawn trong đợt này</param>
    public static void SpawnHorde(Transform[] spawnPoints, int count)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[AlarmSystem] Chưa có AlarmSystem trong scene!");
            return;
        }
        Instance.SpawnHordeInternal(spawnPoints, count);
    }

    private void SpawnHordeInternal(Transform[] spawnPoints, int count)
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogWarning("[AlarmSystem] Chưa gán Zombie Prefabs!");
            return;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[AlarmSystem] Chưa gán Spawn Points!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

            GameObject instance = Instantiate(prefab, point.position, point.rotation);

            ZombieBase zombie = instance.GetComponent<ZombieBase>();
            if (zombie != null)
                zombie.ForceChasePlayer();
            else
                Debug.LogWarning($"[AlarmSystem] Prefab {prefab.name} không có ZombieBase!");
        }
    }
}