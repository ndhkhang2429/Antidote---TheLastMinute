using UnityEngine;

[CreateAssetMenu(fileName = "New Fuse", menuName = "Inventory/Fuse Item")]
public class FuseItemDataSO : ItemDataSO
{
    [Header("Fuse Identity")]
    public string fuseID; // phải khớp với FuseSlot.correctFuseID
}