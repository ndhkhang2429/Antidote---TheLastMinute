using UnityEngine;

[CreateAssetMenu(fileName = "New Helmet", menuName = "Inventory/Helmet Data")]
public class HelmetDataSO : ItemDataSO
{
    [Header("Helmet Stats")]
    [Range(0f, 1f)]
    public float damageReduction = 0.1f;   // % giảm sát thương đầu
    public int durability = 100;            // Độ bền

    [Header("Visual — mesh gắn trên đầu player")]
    public Mesh helmetMesh;
    public Material[] helmetMaterials;
}