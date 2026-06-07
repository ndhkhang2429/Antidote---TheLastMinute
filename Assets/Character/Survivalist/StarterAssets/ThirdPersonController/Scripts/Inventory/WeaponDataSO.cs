using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon Data")]
public class WeaponDataSO : ItemDataSO
{
    [Header("Weapon Stats")]
    public int damage;
    public float fireRate;
    public int magazineSize;
    public ItemDataSO compatibleAmmo; // SO của loại đạn tương thích
    public string attachPointName;    // tên bone/transform để gắn vào
    [Header("Melee Combat")]
    public float comboResetTime = 1.2f;
    public int comboSteps = 3;
    public float cooldown = 0.5f;

    [Header("Hitbox")]
    public float hitDistance = 1.2f;
    public float hitHeight = 0.8f;
    public float hitRadius = 0.6f;
}