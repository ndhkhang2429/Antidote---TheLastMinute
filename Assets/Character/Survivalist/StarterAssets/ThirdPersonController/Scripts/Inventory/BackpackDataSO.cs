using UnityEngine;

[CreateAssetMenu(fileName = "New Backpack", menuName = "Inventory/Backpack Data")]
public class BackpackDataSO : ItemDataSO
{
    [Header("Backpack")]
    public int capacity;

    [Header("Visual — mesh gắn trên player")]
    public Mesh backpackMesh;
    public Material[] backpackMaterials;
}