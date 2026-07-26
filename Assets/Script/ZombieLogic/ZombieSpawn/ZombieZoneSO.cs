using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Định nghĩa "luật chơi" cho 1 khu vực ambient zombie (VD: Sảnh tầng 1, Hành lang Đông...).
/// Tạo asset qua Assets > Create > ZombiePopulation > Zone Config.
/// </summary>
[CreateAssetMenu(menuName = "ZombiePopulation/Zone Config", fileName = "NewZoneConfig")]
public class ZombieZoneSO : ScriptableObject
{
    [Header("Thông tin")]
    public string zoneName;

    [Header("Loại zombie được phép xuất hiện ở zone này")]
    [Tooltip("Chỉ kéo prefab Axe/Spitter vào đây - KHÔNG kéo Tank (Tank không dùng pool, xử lý riêng)")]
    public List<GameObject> allowedZombiePrefabs;

    [Header("Số lượng")]
    [Tooltip("Số zombie tối thiểu duy trì khi player đang ở trong zone")]
    public int minConcurrent = 2;
    [Tooltip("Số zombie tối đa được phép cùng lúc trong zone")]
    public int maxConcurrent = 5;

    [Header("Tank (spawn riêng, không dùng pool)")]
    [Tooltip("Để trống nếu zone này không có Tank")]
    public GameObject tankPrefab;
    [Range(0f, 1f)]
    [Tooltip("Xác suất roll Tank mỗi lần zone được lấp đầy (kiểm tra khi player vào zone)")]
    public float tankSpawnChance = 0.15f;
    [Tooltip("Số Tank tối đa cùng lúc trong zone này (thường là 1)")]
    public int maxTankConcurrent = 1;

    [Header("Timing")]
    [Tooltip("Sau khi 1 zombie trong zone chết, bao lâu thì spawn bù (giây)")]
    public float respawnCooldown = 20f;
    [Tooltip("Sau khi player rời zone, đợi bao lâu trước khi bắt đầu despawn (tránh flicker khi qua lại ranh giới)")]
    public float despawnDelay = 5f;

    /// <summary>Chọn ngẫu nhiên 1 prefab được phép trong danh sách.</summary>
    public GameObject GetRandomPrefab()
    {
        if (allowedZombiePrefabs == null || allowedZombiePrefabs.Count == 0) return null;
        return allowedZombiePrefabs[Random.Range(0, allowedZombiePrefabs.Count)];
    }
}