using UnityEngine;

public enum ItemCategory
{
    Equipment,   // Nón, áo giáp, balo → không chiếm sức chứa
    Weapon,      // Súng, cận chiến → không chiếm sức chứa
    Consumable,  // Máu, đồ uống → chiếm sức chứa
    Ammo,        // Đạn → chiếm sức chứa
    QuestItem,    // Vật phẩm nhiệm vụ → chiếm sức chứa
    Document
}

public enum WeaponSlotType
{
    None,
    Rifle,             // Ô 1
    PistolOrShotgun,   // Ô 2
    Melee,             // Ô 3
    QuestItem          // Ô 4
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public Sprite icon;
    public GameObject worldPrefab;   // prefab rơi ra ngoài world

    [Header("Equip Models")]
    public GameObject equipPrefab;

    [Header("Equip Transform")]
    [Tooltip("Vị trí local của prefab khi được gắn vào socket của Player.")]
    public Vector3 equipLocalPosition = Vector3.zero;

    [Tooltip("Góc xoay local (Euler) của prefab khi được gắn vào socket của Player.")]
    public Vector3 equipLocalRotation = Vector3.zero;

    [Tooltip("Scale local của prefab khi được gắn vào socket của Player.")]
    public Vector3 equipLocalScale = Vector3.one;

    [TextArea] public string description;

    [Header("Category")]
    public ItemCategory category;
    public WeaponSlotType weaponSlotType;

    [Header("Stack & Weight")]
    public int maxStack = 1;
    // --- ĐÃ CHUYỂN SANG FLOAT ĐỂ NHẬN SỐ THẬP PHÂN ---
    public float weightPerUnit = 0f;

    [Header("Pickup")]
    public bool autoEquip = false;
}