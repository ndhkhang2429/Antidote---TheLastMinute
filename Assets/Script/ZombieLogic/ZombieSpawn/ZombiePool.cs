using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ZombiePool – Pool dùng chung cho toàn bộ hệ thống ambient population
/// (và có thể tái sử dụng cho AlarmSystem sau này nếu muốn).
///
/// LƯU Ý: ZombieTank KHÔNG đi qua pool này (xem lý do ở ZombieTank.Die()) —
/// Tank luôn được Instantiate trực tiếp bởi nơi gọi, không qua ZombiePool.
///
/// Thứ tự bắt buộc khi Get(): 
///   1. Lấy object từ queue (hoặc Instantiate mới nếu queue rỗng)
///   2. Bật agent + Warp() tới vị trí mới
///   3. Gọi ResetForPool() để reset toàn bộ state AI/HP/Animator
/// Không được đảo thứ tự 2 và 3.
/// </summary>
public class ZombiePool : MonoBehaviour
{
    public static ZombiePool Instance { get; private set; }

    [Header("Warm-up (tuỳ chọn — tránh hitch Instantiate lúc gameplay)")]
    [Tooltip("Các prefab cần pre-warm sẵn số lượng khi load scene")]
    public List<PoolWarmupEntry> warmupEntries;

    [System.Serializable]
    public class PoolWarmupEntry
    {
        public GameObject prefab;
        public int prewarmCount = 5;
    }

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        foreach (var entry in warmupEntries)
        {
            if (entry.prefab == null) continue;
            for (int i = 0; i < entry.prewarmCount; i++)
            {
                var obj = CreateNew(entry.prefab);
                obj.SetActive(false);
                GetQueue(entry.prefab).Enqueue(obj);
            }
        }
    }

    /// <summary>
    /// Lấy 1 zombie sẵn sàng hoạt động tại vị trí/hướng chỉ định.
    /// Object trả về đã được Warp + ResetForPool, sẵn sàng dùng ngay.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var queue = GetQueue(prefab);
        GameObject obj = queue.Count > 0 ? queue.Dequeue() : CreateNew(prefab);

        obj.SetActive(true);

        // Bước 2: bật agent + warp TRƯỚC khi reset state
        var agent = obj.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
            if (!agent.Warp(position))
            {
                // Warp thất bại - thử tìm điểm gần nhất trên NavMesh trong bán kính rộng hơn
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                else
                {
                    obj.transform.position = position; // thực sự không tìm được NavMesh gần đó, đành chấp nhận
                    Debug.LogWarning($"[ZombiePool] Không tìm được NavMesh gần vị trí spawn {position}, node có thể đặt sai.");
                }
            }
        }
        else
        {
            obj.transform.position = position;
        }
        obj.transform.rotation = rotation;

        // Bước 3: reset toàn bộ state (HP, AI mode, Animator...)
        var zombieBase = obj.GetComponent<ZombieBase>();
        zombieBase?.ResetForPool();

        return obj;
    }

    /// <summary>Trả zombie về pool. Chỉ gọi khi zombie đang ở Patrol (an toàn), 
    /// việc kiểm tra điều kiện này là trách nhiệm của nơi gọi (ZombiePopulationManager).</summary>
    public void Release(GameObject prefab, GameObject instance)
    {
        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.ResetPath();

        instance.SetActive(false);
        GetQueue(prefab).Enqueue(instance);
    }

    private GameObject CreateNew(GameObject prefab)
    {
        bool prefabWasActive = prefab.activeSelf;
        prefab.SetActive(false);

        var obj = Instantiate(prefab);
        obj.name = prefab.name;

        prefab.SetActive(prefabWasActive);
        return obj;
    }

    private Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }
        return queue;
    }
}