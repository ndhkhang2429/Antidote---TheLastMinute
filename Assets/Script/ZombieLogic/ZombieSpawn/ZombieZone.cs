using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đại diện một khu vực zombie trong scene.
/// Script này không tự spawn; chỉ báo player vào/ra cho ZombiePopulationManager.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ZombieZone : MonoBehaviour
{
    [Header("Config")]
    public ZombieZoneSO config;

    [Header("Các điểm spawn")]
    [Tooltip("Nên có nhiều node hơn maxConcurrent để Manager có thể chọn node khuất tầm nhìn.")]
    public List<Transform> spawnNodes = new List<Transform>();

    private BoxCollider _trigger;

    private void Awake()
    {
        _trigger = GetComponent<BoxCollider>();
        _trigger.isTrigger = true;

        if (config == null)
            Debug.LogWarning($"[ZombieZone] '{name}' chưa gán ZombieZoneSO.");

        if (spawnNodes == null || spawnNodes.Count == 0)
            Debug.LogWarning($"[ZombieZone] '{name}' chưa có spawn node.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ZombiePopulationManager.Instance?.OnPlayerEnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ZombiePopulationManager.Instance?.OnPlayerExitZone(this);
    }

    public List<Transform> GetAvailableNodes(HashSet<Transform> occupiedNodes)
    {
        var available = new List<Transform>();

        if (spawnNodes == null)
            return available;

        foreach (Transform node in spawnNodes)
        {
            if (node != null && !occupiedNodes.Contains(node))
                available.Add(node);
        }

        return available;
    }

    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
        Gizmos.DrawWireCube(col.center, col.size);
        Gizmos.matrix = Matrix4x4.identity;

        if (spawnNodes == null)
            return;

        Gizmos.color = Color.yellow;

        foreach (Transform node in spawnNodes)
        {
            if (node != null)
                Gizmos.DrawWireSphere(node.position, 0.4f);
        }
    }
}
