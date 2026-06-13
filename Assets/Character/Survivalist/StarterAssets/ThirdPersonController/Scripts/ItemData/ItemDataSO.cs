using UnityEngine;

public enum ItemCategory
{
    Equipment,   // Nón, áo giáp, balo → không chiếm sức chứa
    Weapon,      // Súng, cận chiến → không chiếm sức chứa
    Grenade,     // Lựu đạn → chiếm sức chứa
    Consumable,  // Máu, đồ uống → chiếm sức chứa
    Ammo,        // Đạn → chiếm sức chứa
    QuestItem    // Vật phẩm nhiệm vụ → chiếm sức chứa
}

public enum WeaponSlotType
{
    None,
    Rifle,             // Ô 1
    PistolOrShotgun,   // Ô 2
    Melee,             // Ô 3
    Grenade,           // Ô 4
    QuestItem          // Ô 5
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