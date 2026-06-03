using UnityEngine;

/// <summary>
/// Gắn vào các cục fuse bị rải rác trong phòng.
/// Player nhìn vào + nhấn F để nhặt (tích hợp PlayerInteraction).
/// </summary>
public class FuseItem : MonoBehaviour
{
    [Header("Fuse Identity")]
    [Tooltip("ID của fuse này, phải trùng với correctFuseID trong FuseSlot tương ứng")]
    public string fuseID = "FUSE_01";

    [Tooltip("Tên hiển thị trong UI prompt")]
    public string displayName = "Cầu chì số 1";
}