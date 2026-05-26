using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ config của một loại vũ khí.
/// Tạo asset: chuột phải → Create → ZombieGame → Weapon Data
/// </summary>
[CreateAssetMenu(fileName = "WeaponData_", menuName = "ZombieGame/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Thông tin")]
    public string weaponName = "Tay không";
    public int weaponType = 0;   // 0=Unarmed | 1=Melee | 2=Pistol | 3=Rifle

    [Header("Damage")]
    public float damage = 10f;
    public float cooldown = 0.4f;

    [Header("Melee Hitbox")]
    public float hitRadius = 1.2f;
    public float hitDistance = 1.0f;
    public float hitHeight = 1.0f;

    [Header("Combo (chỉ WeaponType = 0)")]
    public int comboSteps = 2;           // số bước combo
    public float comboResetTime = 0.8f;
}