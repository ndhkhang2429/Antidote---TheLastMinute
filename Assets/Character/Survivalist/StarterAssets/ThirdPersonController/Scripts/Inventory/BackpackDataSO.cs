using UnityEngine;

[CreateAssetMenu(fileName = "New Backpack", menuName = "Inventory/Backpack Data")]
public class BackpackDataSO : ItemDataSO
{
    [Header("Backpack")]
    public int capacity; // tổng sức chứa
}