using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Pool dùng chung cho zombie thường.
/// Tank không đi qua pool này.
/// </summary>
public class ZombiePool : MonoBehaviour
{
    public static ZombiePool Instance { get; private set; }

    [Header("Warm-up")]
    public List<PoolWarmupEntry> warmupEntries = new List<PoolWarmupEntry>();

    [System.Serializable]
    public class PoolWarmupEntry
    {
        public GameObject prefab;
        [Min(0)] public int prewarmCount = 5;
    }

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools =
        new Dictionary<GameObject, Queue<GameObject>>();

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
        foreach (PoolWarmupEntry entry in warmupEntries)
        {
            if (entry.prefab == null)
                continue;

            for (int i = 0; i < entry.prewarmCount; i++)
            {
                GameObject obj = CreateNew(entry.prefab);
                obj.SetActive(false);
                GetQueue(entry.prefab).Enqueue(obj);
            }
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[ZombiePool] Get được gọi với prefab null.");
            return null;
        }

        Queue<GameObject> queue = GetQueue(prefab);
        GameObject obj = queue.Count > 0 ? queue.Dequeue() : CreateNew(prefab);

        obj.SetActive(true);

        NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.enabled = true;

            if (!agent.Warp(position))
            {
                if (NavMesh.SamplePosition(
                    position,
                    out NavMeshHit hit,
                    5f,
                    NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                else
                {
                    obj.transform.position = position;
                    Debug.LogWarning(
                        $"[ZombiePool] Không tìm được NavMesh gần vị trí {position}.");
                }
            }
        }
        else
        {
            obj.transform.position = position;
        }

        obj.transform.rotation = rotation;

        ZombieBase zombieBase = obj.GetComponent<ZombieBase>();
        zombieBase?.ResetForPool();

        return obj;
    }

    public void Release(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null)
            return;

        NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.ResetPath();

        instance.SetActive(false);
        GetQueue(prefab).Enqueue(instance);
    }

    private GameObject CreateNew(GameObject prefab)
    {
        bool prefabWasActive = prefab.activeSelf;
        prefab.SetActive(false);

        GameObject obj = Instantiate(prefab);
        obj.name = prefab.name;

        prefab.SetActive(prefabWasActive);
        return obj;
    }

    private Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pools.Add(prefab, queue);
        }

        return queue;
    }
}
