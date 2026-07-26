using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đại diện 1 khu vực ambient zombie trong scene. Không tự spawn/despawn —
/// chỉ báo sự kiện player vào/ra cho ZombiePopulationManager xử lý (Bước 4).
///
/// Setup:
///   1. Tạo GameObject rỗng, đặt tên theo khu (VD: "Zone_Sanh_Tang1")
///   2. Add Component BoxCollider, tick Is Trigger, chỉnh Size bao trùm khu vực
///   3. Kéo asset ZombieZoneSO tương ứng vào field Config
///   4. Tạo các GameObject con làm spawn node (VD: SpawnNode_01, SpawnNode_02...),
///      kéo tất cả vào list Spawn Nodes
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ZombieZone : MonoBehaviour
{
    [Header("Config")]
    public ZombieZoneSO config;

    [Header("Các điểm có thể spawn zombie trong khu này")]
    [Tooltip("Nên đặt 8-15 node rải rác, nhiều hơn maxConcurrent để có chỗ chọn ngẫu nhiên tránh trùng vị trí")]
    public List<Transform> spawnNodes;

    private BoxCollider _trigger;

    private void Awake()
    {
        _trigger = GetComponent<BoxCollider>();
        _trigger.isTrigger = true;

        if (config == null)
            Debug.LogWarning($"[ZombieZone] '{gameObject.name}' chưa gán Config!");
        if (spawnNodes == null || spawnNodes.Count == 0)
            Debug.LogWarning($"[ZombieZone] '{gameObject.name}' chưa có Spawn Node nào!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ZombiePopulationManager.Instance?.OnPlayerEnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ZombiePopulationManager.Instance?.OnPlayerExitZone(this);
    }

    /// <summary>Trả về 1 spawn node ngẫu nhiên, loại trừ các node đã bị chiếm (đang có zombie active).</summary>
    /// <summary>Trả về TẤT CẢ node còn trống (chưa bị chiếm), Manager sẽ lọc tiếp theo tầm nhìn player.</summary>
    public List<Transform> GetAvailableNodes(HashSet<Transform> occupiedNodes)
    {
        List<Transform> available = new List<Transform>();
        foreach (var node in spawnNodes)
        {
            if (node != null && !occupiedNodes.Contains(node))
                available.Add(node);
        }
        return available;
    }

    // ── Gizmos — vẽ vùng zone + spawn node để dễ đặt trong Scene view ─────────
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
        Gizmos.DrawWireCube(col.center, col.size);
        Gizmos.matrix = Matrix4x4.identity;

        if (spawnNodes == null) return;
        Gizmos.color = Color.yellow;
        foreach (var node in spawnNodes)
        {
            if (node == null) continue;
            Gizmos.DrawWireSphere(node.position, 0.4f);
        }
    }
}