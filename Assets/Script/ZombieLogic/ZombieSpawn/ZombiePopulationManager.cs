using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều phối zombie theo zone bằng ngân sách hữu hạn.
/// Không còn cơ chế spawn bù vô hạn sau mỗi lần zombie chết.
/// </summary>
public class ZombiePopulationManager : MonoBehaviour
{
    public static ZombiePopulationManager Instance { get; private set; }

    [Header("Giới hạn toàn map")]
    [Min(1)]
    public int globalBudget = 20;

    [Header("Xác chết")]
    [Min(0f)]
    public float corpseLingerDuration = 8f;

    [Header("Né tầm nhìn player")]
    [Min(0f)]
    public float visibilityCheckDistance = 15f;

    [Range(0f, 1f)]
    public float visibilityDotThreshold = 0.5f;

    [Min(0.1f)]
    [Tooltip("Khi node bị camera nhìn thấy, chờ bao lâu rồi thử lại.")]
    public float blockedSpawnRetryInterval = 2f;

    private int _globalActiveCount;
    private Camera _playerCam;

    private sealed class ActiveZombieEntry
    {
        public GameObject instance;
        public GameObject prefab;
        public Transform node;
        public ZombieBase zombieBase;
        public HealthSystem health;
        public bool isPooled;
        public bool deathHandled;
        public Action deathCallback;
    }

    private sealed class ZoneRuntimeData
    {
        public readonly List<ActiveZombieEntry> activeZombies =
            new List<ActiveZombieEntry>();

        public bool playerInside;
        public bool initialized;
        public bool cleared;

        // Chỉ tính zombie thường. Tank có giới hạn riêng.
        public int normalSpawnedTotal;

        public Coroutine spawnRoutine;
        public Coroutine despawnRoutine;
    }

    private readonly Dictionary<ZombieZone, ZoneRuntimeData> _zoneData =
        new Dictionary<ZombieZone, ZoneRuntimeData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OnPlayerEnterZone(ZombieZone zone)
    {
        if (!IsZoneValid(zone))
            return;

        ZoneRuntimeData data = GetOrCreateZoneData(zone);
        data.playerInside = true;

        if (data.despawnRoutine != null)
        {
            StopCoroutine(data.despawnRoutine);
            data.despawnRoutine = null;
        }

        if (data.cleared && zone.config.stayClearedAfterCompletion)
            return;

        if (!data.initialized)
        {
            data.initialized = true;
            TrySpawnTank(zone, data);

            if (data.spawnRoutine == null)
                data.spawnRoutine = StartCoroutine(InitialSpawnRoutine(zone, data));

            return;
        }

        if (data.spawnRoutine == null)
            data.spawnRoutine = StartCoroutine(WaveRoutine(zone, data));
    }

    public void OnPlayerExitZone(ZombieZone zone)
    {
        if (!_zoneData.TryGetValue(zone, out ZoneRuntimeData data))
            return;

        data.playerInside = false;

        if (data.spawnRoutine != null)
        {
            StopCoroutine(data.spawnRoutine);
            data.spawnRoutine = null;
        }

        if (data.despawnRoutine != null)
            StopCoroutine(data.despawnRoutine);

        data.despawnRoutine = StartCoroutine(DespawnZoneRoutine(zone, data));
    }

    private bool IsZoneValid(ZombieZone zone)
    {
        if (zone == null || zone.config == null)
        {
            Debug.LogWarning("[ZombiePopulationManager] Zone hoặc config bị null.");
            return false;
        }

        return true;
    }

    private ZoneRuntimeData GetOrCreateZoneData(ZombieZone zone)
    {
        if (!_zoneData.TryGetValue(zone, out ZoneRuntimeData data))
        {
            data = new ZoneRuntimeData();
            _zoneData.Add(zone, data);
        }

        return data;
    }

    private IEnumerator InitialSpawnRoutine(
        ZombieZone zone,
        ZoneRuntimeData data)
    {
        int target = Mathf.Min(
            zone.config.initialSpawnCount,
            zone.config.maxConcurrent,
            zone.config.totalSpawnBudget);

        while (data.playerInside &&
               GetNormalAliveCount(data) < target &&
               CanSpawnNormal(zone, data))
        {
            bool spawned = SpawnOneNormal(zone, data);

            yield return new WaitForSeconds(
                spawned ? 0.15f : blockedSpawnRetryInterval);
        }

        data.spawnRoutine = null;

        if (data.playerInside && !data.cleared)
            data.spawnRoutine = StartCoroutine(WaveRoutine(zone, data));
    }

    private IEnumerator WaveRoutine(
        ZombieZone zone,
        ZoneRuntimeData data)
    {
        while (data.playerInside && !data.cleared)
        {
            yield return new WaitForSeconds(zone.config.timeBetweenWaves);

            if (!data.playerInside)
                break;

            int normalAlive = GetNormalAliveCount(data);

            if (normalAlive <= zone.config.waveTriggerAliveCount)
            {
                int availableConcurrentSlots =
                    zone.config.maxConcurrent - normalAlive;

                int remainingBudget =
                    zone.config.totalSpawnBudget - data.normalSpawnedTotal;

                int amountToSpawn = Mathf.Min(
                    zone.config.spawnPerWave,
                    availableConcurrentSlots,
                    remainingBudget,
                    globalBudget - _globalActiveCount);

                for (int i = 0; i < amountToSpawn; i++)
                {
                    if (!data.playerInside || !CanSpawnNormal(zone, data))
                        break;

                    bool spawned = SpawnOneNormal(zone, data);

                    if (!spawned)
                    {
                        yield return new WaitForSeconds(
                            blockedSpawnRetryInterval);
                        i--;
                        continue;
                    }

                    yield return new WaitForSeconds(0.15f);
                }
            }

            CheckZoneCleared(zone, data);
        }

        data.spawnRoutine = null;
    }

    private bool CanSpawnNormal(
        ZombieZone zone,
        ZoneRuntimeData data)
    {
        if (!data.playerInside)
            return false;

        if (data.cleared)
            return false;

        if (_globalActiveCount >= globalBudget)
            return false;

        if (GetNormalAliveCount(data) >= zone.config.maxConcurrent)
            return false;

        if (data.normalSpawnedTotal >= zone.config.totalSpawnBudget)
            return false;

        return true;
    }

    private bool SpawnOneNormal(
        ZombieZone zone,
        ZoneRuntimeData data)
    {
        if (!CanSpawnNormal(zone, data))
            return false;

        Transform node = PickSpawnNode(zone, GetOccupiedNodes(data));

        if (node == null)
            return false;

        GameObject prefab = zone.config.GetRandomPrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                $"[ZombiePopulationManager] Zone '{zone.name}' không có prefab zombie hợp lệ.");
            return false;
        }

        GameObject instance = ZombiePool.Instance.Get(
            prefab,
            node.position,
            node.rotation);

        if (instance == null)
            return false;

        ZombieBase zombieBase = instance.GetComponent<ZombieBase>();
        HealthSystem health = instance.GetComponent<HealthSystem>();

        if (health == null)
        {
            Debug.LogError(
                $"[ZombiePopulationManager] Prefab '{prefab.name}' thiếu HealthSystem.");

            ZombiePool.Instance.Release(prefab, instance);
            return false;
        }

        var entry = new ActiveZombieEntry
        {
            instance = instance,
            prefab = prefab,
            node = node,
            zombieBase = zombieBase,
            health = health,
            isPooled = true
        };

        entry.deathCallback = () => HandleZombieDeath(zone, data, entry);
        health.OnDeath += entry.deathCallback;

        data.activeZombies.Add(entry);
        data.normalSpawnedTotal++;
        _globalActiveCount++;

        return true;
    }

    private void TrySpawnTank(
        ZombieZone zone,
        ZoneRuntimeData data)
    {
        ZombieZoneSO config = zone.config;

        if (config.tankPrefab == null)
            return;

        if (_globalActiveCount >= globalBudget)
            return;

        if (CountTanks(data) >= config.maxTankConcurrent)
            return;

        if (UnityEngine.Random.value > config.tankSpawnChance)
            return;

        Transform node = PickSpawnNode(zone, GetOccupiedNodes(data));

        if (node == null)
            return;

        GameObject instance = Instantiate(
            config.tankPrefab,
            node.position,
            node.rotation);

        ZombieBase zombieBase = instance.GetComponent<ZombieBase>();
        HealthSystem health = instance.GetComponent<HealthSystem>();

        if (health == null)
        {
            Debug.LogError(
                $"[ZombiePopulationManager] Tank '{config.tankPrefab.name}' thiếu HealthSystem.");
            Destroy(instance);
            return;
        }

        var entry = new ActiveZombieEntry
        {
            instance = instance,
            prefab = config.tankPrefab,
            node = node,
            zombieBase = zombieBase,
            health = health,
            isPooled = false
        };

        entry.deathCallback = () => HandleZombieDeath(zone, data, entry);
        health.OnDeath += entry.deathCallback;

        data.activeZombies.Add(entry);
        _globalActiveCount++;
    }

    private void HandleZombieDeath(
        ZombieZone zone,
        ZoneRuntimeData data,
        ActiveZombieEntry entry)
    {
        if (entry == null || entry.deathHandled)
            return;

        entry.deathHandled = true;
        UnsubscribeDeath(entry);

        if (data.activeZombies.Remove(entry))
            _globalActiveCount = Mathf.Max(0, _globalActiveCount - 1);

        if (entry.isPooled)
            StartCoroutine(ReleaseCorpseAfterDelay(entry));

        // Tank tự Destroy trong logic riêng, vì vậy Manager không Release Tank.
        CheckZoneCleared(zone, data);
    }

    private IEnumerator ReleaseCorpseAfterDelay(
        ActiveZombieEntry entry)
    {
        yield return new WaitForSeconds(corpseLingerDuration);

        if (entry.instance != null)
            ZombiePool.Instance.Release(entry.prefab, entry.instance);
    }

    private void CheckZoneCleared(
        ZombieZone zone,
        ZoneRuntimeData data)
    {
        bool budgetExhausted =
            data.normalSpawnedTotal >= zone.config.totalSpawnBudget;

        bool noAliveZombie =
            data.activeZombies.Count == 0;

        if (!budgetExhausted || !noAliveZombie)
            return;

        data.cleared = true;

        if (data.spawnRoutine != null)
        {
            StopCoroutine(data.spawnRoutine);
            data.spawnRoutine = null;
        }

        Debug.Log(
            $"[ZombiePopulationManager] Zone '{zone.config.zoneName}' đã được dọn sạch.");
    }

    private IEnumerator DespawnZoneRoutine(
        ZombieZone zone,
        ZoneRuntimeData data)
    {
        yield return new WaitForSeconds(zone.config.despawnDelay);

        while (!data.playerInside)
        {
            bool releasedAny = false;

            for (int i = data.activeZombies.Count - 1; i >= 0; i--)
            {
                ActiveZombieEntry entry = data.activeZombies[i];

                if (!entry.isPooled)
                    continue;

                if (entry.zombieBase == null ||
                    !entry.zombieBase.IsPatrolling)
                {
                    continue;
                }

                UnsubscribeDeath(entry);

                ZombiePool.Instance.Release(
                    entry.prefab,
                    entry.instance);

                data.activeZombies.RemoveAt(i);
                _globalActiveCount =
                    Mathf.Max(0, _globalActiveCount - 1);

                releasedAny = true;
            }

            if (!HasDespawnableZombie(data))
                break;

            yield return new WaitForSeconds(releasedAny ? 0.5f : 3f);
        }

        data.despawnRoutine = null;
    }

    private void UnsubscribeDeath(ActiveZombieEntry entry)
    {
        if (entry?.health != null && entry.deathCallback != null)
        {
            entry.health.OnDeath -= entry.deathCallback;
            entry.deathCallback = null;
        }
    }

    private int GetNormalAliveCount(ZoneRuntimeData data)
    {
        int count = 0;

        foreach (ActiveZombieEntry entry in data.activeZombies)
        {
            if (entry.isPooled)
                count++;
        }

        return count;
    }

    private int CountTanks(ZoneRuntimeData data)
    {
        int count = 0;

        foreach (ActiveZombieEntry entry in data.activeZombies)
        {
            if (!entry.isPooled)
                count++;
        }

        return count;
    }

    private bool HasDespawnableZombie(
        ZoneRuntimeData data)
    {
        foreach (ActiveZombieEntry entry in data.activeZombies)
        {
            if (entry.isPooled)
                return true;
        }

        return false;
    }

    private HashSet<Transform> GetOccupiedNodes(
        ZoneRuntimeData data)
    {
        var occupied = new HashSet<Transform>();

        foreach (ActiveZombieEntry entry in data.activeZombies)
        {
            if (entry.node != null)
                occupied.Add(entry.node);
        }

        return occupied;
    }

    private Transform PickSpawnNode(
        ZombieZone zone,
        HashSet<Transform> occupied)
    {
        List<Transform> available =
            zone.GetAvailableNodes(occupied);

        if (available.Count == 0)
            return null;

        var hidden = new List<Transform>();

        foreach (Transform node in available)
        {
            if (!IsNodeVisibleToPlayer(node.position))
                hidden.Add(node);
        }

        if (hidden.Count == 0)
            return null;

        return hidden[UnityEngine.Random.Range(0, hidden.Count)];
    }

    private bool IsNodeVisibleToPlayer(Vector3 nodePosition)
    {
        if (_playerCam == null)
            _playerCam = Camera.main;

        if (_playerCam == null)
            return false;

        Vector3 cameraPosition =
            _playerCam.transform.position;

        float distance =
            Vector3.Distance(cameraPosition, nodePosition);

        if (distance > visibilityCheckDistance)
            return false;

        Vector3 direction =
            (nodePosition - cameraPosition).normalized;

        float dot = Vector3.Dot(
            _playerCam.transform.forward,
            direction);

        if (dot < visibilityDotThreshold)
            return false;

        if (Physics.Linecast(
            cameraPosition,
            nodePosition,
            out RaycastHit hit))
        {
            if (hit.distance < distance - 0.5f)
                return false;
        }

        return true;
    }
}
