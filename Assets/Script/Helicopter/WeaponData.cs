using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Helicopter/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;

    [Header("Projectile Stats")]
    public float projectileSpeed = 50f;
    public float damage = 20f;
    public float lifeTime = 3f; // Thời gian đạn tự biến mất nếu không trúng đích

    [Header("Weapon Stats")]
    public int magSize = 30;         // Số lượng đạn 1 băng
    public float reloadTime = 2f;    // Thời gian nạp đạn
    public float fireRate = 0.1f;    // Thời gian giữa 2 lần bắn (0.1s = súng máy, 1s = rocket)
    public bool isAutomatic = true;  // Súng máy giữ chuột để bắn, Rocket bấm từng phát
}