using UnityEngine;

public enum CombatType
{
    Firearm, // Súng bắn đạn
    Melee    // Vũ khí cận chiến (Dao, Rìu, Cây sắt...)
}

[CreateAssetMenu(fileName = "New Weapon", menuName = "Inventory/Weapon Data")]
public class WeaponDataSO : ItemDataSO // Kế thừa từ ItemDataSO gốc của bạn
{
    [Header("General Weapon Stats")]
    public CombatType combatType;
    public float damage;               // Dùng chung cho cả chém và bắn
    public string attachPointName;     // Tên Bone/Transform để gắn súng/dao vào tay

    [Header("Ranged Combat (Dành cho Súng)")]
    public float fireRate = 0.2f;      // Thời gian trễ giữa 2 phát bắn
    public int magazineSize = 30;      // Sức chứa của 1 băng đạn
    public ItemDataSO compatibleAmmo;  // SO của loại đạn dùng để nạp (vd: đạn 5.56mm)
    public GameObject bulletPrefab;    // Viên đạn vật lý sẽ bay ra
    public float bulletSpeed = 50f;    // Tốc độ bay của đạn
    public GameObject muzzleFlashPrefab; // Hiệu ứng tia lửa đầu nòng

    [Header("Melee Combat (Dành cho Cận chiến)")]
    public float cooldown = 0.5f;      // Thời gian chờ giữa các đòn đánh
    public int comboSteps = 3;
    public float comboResetTime = 1.2f;

    [Header("Melee Hitbox")]
    public float hitDistance = 1.2f;
    public float hitHeight = 0.8f;
    public float hitRadius = 0.6f;
}