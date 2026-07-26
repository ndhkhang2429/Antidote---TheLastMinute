using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bộ não điều phối ambient zombie population — tách biệt hoàn toàn với AlarmSystem.
///
/// Trách nhiệm:
///   - Spawn zombie thường (Axe/Spitter/Normal...) qua ZombiePool khi player vào zone
///   - Né spawn tại các node đang nằm trong tầm nhìn player (tránh cảm giác "hiện ra từ hư không")
///   - Retry định kỳ khi zone chưa đủ minConcurrent do bị chặn bởi tầm nhìn
///   - Despawn an toàn khi player rời zone (chỉ despawn zombie đang Patrol)
///   - Spawn bù khi zombie chết (sau respawnCooldown)
///   - Spawn Tank riêng theo xác suất, KHÔNG qua pool, không bao giờ despawn khi còn sống
///   - Giới hạn tổng số zombie active cùng lúc (globalBudget)
/// </summary>
public class ZombiePopulationManager : MonoBehaviour
{
    public static ZombiePopulationManager Instance { get; private set; }

    [Header("Giới hạn toàn map (bảo vệ performance)")]
    public int globalBudget = 20;

    [Header("Xác chết")]
    [Tooltip("Thời gian xác zombie thường nằm lại trước khi thu hồi vào pool. Không áp dụng cho Tank.")]
    public float corpseLingerDuration = 8f;

    [Header("Né tầm nhìn player khi spawn")]
    [Tooltip("Node cách camera trong khoảng này VÀ nằm trong góc nhìn sẽ bị coi là 'đang thấy', loại khỏi lượt spawn này")]
    public float visibilityCheckDistance = 15f;
    [Range(0f, 1f)]
    [Tooltip("Dot product tối thiểu để coi là nằm trong góc nhìn (0.5 ~ góc nửa 60 độ, càng cao càng hẹp)")]
    public float visibilityDotThreshold = 0.5f;
    [Tooltip("Sau khi bị chặn bởi tầm nhìn, bao lâu thì thử spawn lại (giây)")]
    public float retryInterval = 2f;

    private int _globalActiveCount = 0;
    private Camera _playerCam;

    private class ActiveZombieEntry
    {
        public GameObject instance;
        public GameObject prefab;
        public Transform node;
        public ZombieBase zombieBase;
        public HealthSystem health;
        public bool isPooled = true; // false = Tank
    }

    private class ZoneRuntimeData
    {
        public List<ActiveZombieEntry> activeZombies = new List<ActiveZombieEntry>();
        public bool playerInside;
        public Coroutine despawnRoutine;
        public Coroutine periodicFillRoutine;
    }

    private readonly Dictionary<ZombieZone, ZoneRuntimeData> _zoneData = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Entry points gọi từ ZombieZone ───────────────────────────────────────

    public void OnPlayerEnterZone(ZombieZone zone)
    {
        var data = GetOrCreateZoneData(zone);
        data.playerInside = true;

        if (data.despawnRoutine != null)
        {
            StopCoroutine(data.despawnRoutine);
            data.despawnRoutine = null;
        }

        TrySpawnTank(zone, data);

        if (data.periodicFillRoutine == null)
            data.periodicFillRoutine = StartCoroutine(PeriodicFillRoutine(zone, data));
    }

    public void OnPlayerExitZone(ZombieZone zone)
    {
        if (!_zoneData.TryGetValue(zone, out var data)) return;
        data.playerInside = false;
        data.despawnRoutine = StartCoroutine(DespawnZoneRoutine(zone, data));
    }

    private ZoneRuntimeData GetOrCreateZoneData(ZombieZone zone)
    {
        if (!_zoneData.TryGetValue(zone, out var data))
        {
            data = new ZoneRuntimeData();
            _zoneData[zone] = data;
        }
        return data;
    }

    // ── Retry định kỳ để lấp đầy zone (xử lý trường hợp node bị chặn bởi tầm nhìn) ──

    private IEnumerator PeriodicFillRoutine(ZombieZone zone, ZoneRuntimeData data)
    {
        while (data.playerInside)
        {
            if (data.activeZombies.Count < zone.config.minConcurrent
                && _globalActiveCount < globalBudget)
            {
                SpawnOne(zone, data);
            }
            yield return new WaitForSeconds(retryInterval);
        }
        data.periodicFillRoutine = null;
    }

    // ── Spawn zombie thường (qua Pool) ───────────────────────────────────────

    private void SpawnOne(ZombieZone zone, ZoneRuntimeData data)
    {
        var config = zone.config;
        var occupied = GetOccupiedNodes(data);
        Transform node = PickSpawnNode(zone, occupied);
        if (node == null) return; // hết chỗ trống, hoặc tất cả node còn lại đang bị player nhìn thấy

        GameObject prefab = config.GetRandomPrefab();
        if (prefab == null) return;

        GameObject instance = ZombiePool.Instance.Get(prefab, node.position, node.rotation);
        var zb = instance.GetComponent<ZombieBase>();
        var health = instance.GetComponent<HealthSystem>();

        var entry = new ActiveZombieEntry
        {
            instance = instance,
            prefab = prefab,
            node = node,
            zombieBase = zb,
            health = health,
            isPooled = true
        };
        data.activeZombies.Add(entry);
        _globalActiveCount++;

        Action onDeath = null;
        onDeath = () =>
        {
            health.OnDeath -= onDeath;
            HandleZombieDeath(zone, data, entry);
        };
        health.OnDeath += onDeath;
    }

    // ── Spawn Tank (KHÔNG qua Pool) ───────────────────────────────────────────

    private void TrySpawnTank(ZombieZone zone, ZoneRuntimeData data)
    {
        var config = zone.config;
        if (config.tankPrefab == null) return;

        int currentTankCount = data.activeZombies.FindAll(e => !e.isPooled).Count;
        if (currentTankCount >= config.maxTankConcurrent) return;
        if (_globalActiveCount >= globalBudget) return;
        if (UnityEngine.Random.value > config.tankSpawnChance) return;

        var occupied = GetOccupiedNodes(data);
        Transform node = PickSpawnNode(zone, occupied);
        if (node == null) return;

        GameObject instance = Instantiate(config.tankPrefab, node.position, node.rotation);
        var zb = instance.GetComponent<ZombieBase>();
        var health = instance.GetComponent<HealthSystem>();

        var entry = new ActiveZombieEntry
        {
            instance = instance,
            prefab = config.tankPrefab,
            node = node,
            zombieBase = zb,
            health = health,
            isPooled = false
        };
        data.activeZombies.Add(entry);
        _globalActiveCount++;

        Action onDeath = null;
        onDeath = () =>
        {
            health.OnDeath -= onDeath;
            data.activeZombies.Remove(entry);
            _globalActiveCount--;
            // KHÔNG gọi Release() - Tank tự Destroy(gameObject) trong Explode()
        };
        health.OnDeath += onDeath;
    }

    // ── Chọn spawn node, né tầm nhìn player ───────────────────────────────────

    private Transform PickSpawnNode(ZombieZone zone, HashSet<Transform> occupied)
    {
        var available = zone.GetAvailableNodes(occupied);
        if (available.Count == 0) return null;

        List<Transform> hidden = new List<Transform>();
        foreach (var n in available)
        {
            if (!IsNodeVisibleToPlayer(n.position))
                hidden.Add(n);
        }

        if (hidden.Count > 0)
            return hidden[UnityEngine.Random.Range(0, hidden.Count)];

        // Tất cả node còn trống đều đang bị player nhìn thấy -> không spawn đợt này,
        // PeriodicFillRoutine sẽ tự thử lại sau retryInterval giây
        return null;
    }

    private bool IsNodeVisibleToPlayer(Vector3 nodePos)
    {
        if (_playerCam == null) _playerCam = Camera.main;
        if (_playerCam == null) return false; // không tìm thấy camera -> coi như an toàn để spawn

        Vector3 camPos = _playerCam.transform.position;
        float dist = Vector3.Distance(camPos, nodePos);
        if (dist > visibilityCheckDistance) return false; // quá xa, không tính là "đang thấy"

        Vector3 toNode = (nodePos - camPos).normalized;
        float dot = Vector3.Dot(_playerCam.transform.forward, toNode);
        if (dot < visibilityDotThreshold) return false; // ngoài góc nhìn

        // Trong góc nhìn + trong tầm -> kiểm tra có bị tường che không
        if (Physics.Linecast(camPos, nodePos, out RaycastHit hit))
        {
            // Nếu tia bị chặn TRƯỚC KHI chạm tới node -> có vật cản (tường) -> thực ra không thấy được
            if (hit.distance < dist - 0.5f) return false;
        }

        return true; // trong góc nhìn, trong tầm, không bị che -> coi là đang bị nhìn thấy
    }

    // ── Death & Respawn ───────────────────────────────────────────────────────

    private void HandleZombieDeath(ZombieZone zone, ZoneRuntimeData data, ActiveZombieEntry entry)
    {
        data.activeZombies.Remove(entry);
        _globalActiveCount--;

        StartCoroutine(ReleaseCorpseAfterDelay(entry));
        StartCoroutine(RespawnAfterCooldown(zone, data));
    }

    private IEnumerator ReleaseCorpseAfterDelay(ActiveZombieEntry entry)
    {
        yield return new WaitForSeconds(corpseLingerDuration);
        ZombiePool.Instance.Release(entry.prefab, entry.instance);
    }

    private IEnumerator RespawnAfterCooldown(ZombieZone zone, ZoneRuntimeData data)
    {
        yield return new WaitForSeconds(zone.config.respawnCooldown);

        if (data.playerInside
            && data.activeZombies.Count < zone.config.maxConcurrent
            && _globalActiveCount < globalBudget)
        {
            SpawnOne(zone, data);
        }
    }

    // ── Despawn an toàn khi player rời zone ──────────────────────────────────

    private IEnumerator DespawnZoneRoutine(ZombieZone zone, ZoneRuntimeData data)
    {
        yield return new WaitForSeconds(zone.config.despawnDelay);

        while (HasDespawnableZombies(data) && !data.playerInside)
        {
            var toRemove = new List<ActiveZombieEntry>();

            foreach (var entry in data.activeZombies)
            {
                if (!entry.isPooled) continue; // Tank không bao giờ despawn khi còn sống
                if (entry.zombieBase != null && entry.zombieBase.IsPatrolling)
                    toRemove.Add(entry);
            }

            foreach (var entry in toRemove)
            {
                ZombiePool.Instance.Release(entry.prefab, entry.instance);
                data.activeZombies.Remove(entry);
                _globalActiveCount--;
            }

            if (!HasDespawnableZombies(data)) break;

            yield return new WaitForSeconds(3f);
        }

        data.despawnRoutine = null;
    }

    private bool HasDespawnableZombies(ZoneRuntimeData data)
    {
        foreach (var entry in data.activeZombies)
            if (entry.isPooled) return true;
        return false;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private HashSet<Transform> GetOccupiedNodes(ZoneRuntimeData data)
    {
        var occupied = new HashSet<Transform>();
        foreach (var e in data.activeZombies) occupied.Add(e.node);
        return occupied;
    }
}