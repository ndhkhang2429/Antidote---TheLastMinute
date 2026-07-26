using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

/// <summary>
/// Custom Inspector cho ZombieZone - thêm nút "Snap Nodes To NavMesh"
/// để tự động chỉnh lại vị trí Y của tất cả spawn node cho khớp chính xác
/// với NavMesh, tránh phải kéo tay từng node (dễ sai lệch vài chục cm).
/// </summary>
[CustomEditor(typeof(ZombieZone))]
public class ZombieZoneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ZombieZone zone = (ZombieZone)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Công cụ hỗ trợ đặt Node", EditorStyles.boldLabel);

        if (GUILayout.Button("Snap Nodes To NavMesh (chỉnh Y tự động)"))
        {
            SnapNodesToNavMesh(zone);
        }

        if (GUILayout.Button("Snap Nodes To Ground (Raycast xuống, không cần NavMesh)"))
        {
            SnapNodesToGround(zone);
        }
    }

    private void SnapNodesToNavMesh(ZombieZone zone)
    {
        if (zone.spawnNodes == null) return;
        int fixedCount = 0;

        foreach (var node in zone.spawnNodes)
        {
            if (node == null) continue;

            if (NavMesh.SamplePosition(node.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Undo.RecordObject(node, "Snap Node To NavMesh");
                node.position = hit.position;
                fixedCount++;
            }
            else
            {
                Debug.LogWarning($"[ZombieZoneEditor] Không tìm thấy NavMesh gần node '{node.name}' (vị trí {node.position}) trong bán kính 5m.");
            }
        }

        Debug.Log($"[ZombieZoneEditor] Đã snap {fixedCount}/{zone.spawnNodes.Count} node xuống NavMesh.");
    }

    private void SnapNodesToGround(ZombieZone zone)
    {
        if (zone.spawnNodes == null) return;
        int fixedCount = 0;

        foreach (var node in zone.spawnNodes)
        {
            if (node == null) continue;

            // Raycast từ trên cao xuống, tìm mặt sàn gần nhất bằng Collider vật lý thường
            Vector3 rayStart = node.position + Vector3.up * 5f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 15f))
            {
                Undo.RecordObject(node, "Snap Node To Ground");
                node.position = hit.point;
                fixedCount++;
            }
            else
            {
                Debug.LogWarning($"[ZombieZoneEditor] Không tìm thấy mặt sàn nào bên dưới node '{node.name}'.");
            }
        }

        Debug.Log($"[ZombieZoneEditor] Đã snap {fixedCount}/{zone.spawnNodes.Count} node xuống mặt sàn (raycast).");
    }
}