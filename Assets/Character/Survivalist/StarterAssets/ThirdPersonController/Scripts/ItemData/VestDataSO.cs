using UnityEngine;

[CreateAssetMenu(fileName = "New Vest", menuName = "Inventory/Vest Data")]
public class VestDataSO : ItemDataSO
{
    [Header("Vest Stats")]
    [Range(0f, 1f)]
    public float damageReduction = 0.2f;   // % giảm sát thương thân
    public int durability = 100;

    [Header("Vest Level (1 = thường, 2 = xanh, 3 = cam)")]
    [Range(1, 3)]
    public int vestLevel = 1;

    [Header("Visual — mesh gắn trên thân player")]
    public Mesh vestMesh;
    public Material[] vestMaterials;
}