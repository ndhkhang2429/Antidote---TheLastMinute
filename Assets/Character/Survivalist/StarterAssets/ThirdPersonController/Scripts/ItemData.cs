using UnityEngine;

/// <summary>
/// Gắn script này lên TỪNG prefab vật phẩm trong scene.
/// Định nghĩa loại vũ khí và offset khi cầm trên tay.
/// </summary>
public class ItemData : MonoBehaviour
{
    [Header("Thông tin vật phẩm")]
    public string itemName = "Unnamed Item";

    [Tooltip("0=Tay không(không dùng) | 1=Gậy/2 tay | 2=Pistol | 3=Rifle | 4=Grenade")]
    public int weaponType = 1;

    [Header("Offset khi cầm trên tay")]
    public Vector3 holdPositionOffset = Vector3.zero;
    public Vector3 holdRotationOffset = Vector3.zero;

    [Header("Hiển thị")]
    public Sprite itemIcon;          // Dùng cho UI nếu cần
    [TextArea] public string itemDescription;
}