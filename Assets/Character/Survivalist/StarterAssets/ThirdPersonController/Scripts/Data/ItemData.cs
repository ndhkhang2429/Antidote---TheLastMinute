using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemCategory category;   // Equipment, Weapon, Consumable, Ammo, Quest
    public int weight;              // chỉ tính vào balo nếu là Consumable/Grenade
    public int maxStack;
    [TextArea] public string description;
}

public enum ItemCategory { Equipment, Weapon, Consumable, Ammo, Quest, Grenade }