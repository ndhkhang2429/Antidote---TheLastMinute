using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cấu hình luật spawn cho một ZombieZone.
/// Zone dùng ngân sách hữu hạn, không spawn bù vô hạn.
/// </summary>
[CreateAssetMenu(
    menuName = "ZombiePopulation/Zone Config",
    fileName = "NewZoneConfig"
)]
public class ZombieZoneSO : ScriptableObject
{
    [Header("Thông tin")]
    public string zoneName;

    [Header("Zombie thường")]
    [Tooltip("Các prefab zombie được phép xuất hiện. Không thêm Tank vào đây.")]
    public List<GameObject> allowedZombiePrefabs = new List<GameObject>();

    [Header("Giới hạn số lượng")]
    [Min(0)]
    [Tooltip("Số zombie muốn spawn ngay khi player vào zone lần đầu.")]
    public int initialSpawnCount = 3;

    [Min(1)]
    [Tooltip("Số zombie tối đa được sống cùng lúc trong zone.")]
    public int maxConcurrent = 5;

    [Min(0)]
    [Tooltip("Tổng số zombie thường tối đa mà zone được phép spawn trong một lần chơi.")]
    public int totalSpawnBudget = 10;

    [Header("Spawn theo đợt")]
    [Min(1)]
    [Tooltip("Số zombie tối đa được spawn trong mỗi đợt bổ sung.")]
    public int spawnPerWave = 2;

    [Min(0f)]
    [Tooltip("Khoảng thời gian giữa hai lần kiểm tra spawn đợt tiếp theo.")]
    public float timeBetweenWaves = 15f;

    [Min(0)]
    [Tooltip("Chỉ spawn đợt mới khi số zombie sống còn bằng hoặc thấp hơn mức này.")]
    public int waveTriggerAliveCount = 1;

    [Header("Tank - spawn riêng")]
    [Tooltip("Để trống nếu zone không có Tank.")]
    public GameObject tankPrefab;

    [Range(0f, 1f)]
    [Tooltip("Xác suất spawn Tank khi player vào zone lần đầu.")]
    public float tankSpawnChance = 0.1f;

    [Min(0)]
    [Tooltip("Số Tank tối đa cùng lúc trong zone.")]
    public int maxTankConcurrent = 1;

    [Header("Rời khỏi zone")]
    [Min(0f)]
    [Tooltip("Sau khi player rời zone, chờ bao lâu trước khi thu hồi zombie đang Patrol.")]
    public float despawnDelay = 5f;

    [Header("Hoàn thành zone")]
    [Tooltip("Bật: khi hết ngân sách và không còn zombie sống, zone được xem là đã dọn sạch vĩnh viễn.")]
    public bool stayClearedAfterCompletion = true;

    public GameObject GetRandomPrefab()
    {
        if (allowedZombiePrefabs == null || allowedZombiePrefabs.Count == 0)
            return null;

        return allowedZombiePrefabs[Random.Range(0, allowedZombiePrefabs.Count)];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        initialSpawnCount = Mathf.Max(0, initialSpawnCount);
        maxConcurrent = Mathf.Max(1, maxConcurrent);
        totalSpawnBudget = Mathf.Max(0, totalSpawnBudget);
        spawnPerWave = Mathf.Max(1, spawnPerWave);
        waveTriggerAliveCount = Mathf.Max(0, waveTriggerAliveCount);

        initialSpawnCount = Mathf.Min(initialSpawnCount, maxConcurrent);

        if (totalSpawnBudget > 0)
            initialSpawnCount = Mathf.Min(initialSpawnCount, totalSpawnBudget);

        waveTriggerAliveCount = Mathf.Min(waveTriggerAliveCount, maxConcurrent - 1);
    }
#endif
}
