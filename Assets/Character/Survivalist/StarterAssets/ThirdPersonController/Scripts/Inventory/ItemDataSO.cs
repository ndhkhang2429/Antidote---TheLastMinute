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
    PistolOrShotgun,  // Ô 1
    Rifle,            // Ô 2
    Melee,            // Ô 3
    Grenade           // Ô 4
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public Sprite icon;
    public GameObject worldPrefab;   // prefab rơi ra ngoài world
    [TextArea] public string description;

    [Header("Category")]
    public ItemCategory category;
    public WeaponSlotType weaponSlotType; // chỉ dùng nếu category == Weapon/Grenade

    [Header("Stack & Weight")]
    public int maxStack = 1;
    public int weightPerUnit = 0; // chỉ Grenade/Consumable/Ammo/QuestItem mới > 0

    [Header("Pickup")]
    public bool autoEquip = false; // tự trang bị khi nhặt (balo, giáp...)
}