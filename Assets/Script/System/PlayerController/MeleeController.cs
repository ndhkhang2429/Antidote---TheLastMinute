using UnityEngine;

public class MeleeController : MonoBehaviour, IEquippable
{
    [Header("Data Reference")]
    [Tooltip("Kéo file WeaponDataSO của món vũ khí này vào đây")]
    public WeaponDataSO weaponData;

    private bool isEquipped = false;
    private float lastAttackTime = 0f;

    // --- Triển khai giao kèo IEquippable ---
    public void OnEquip()
    {
        isEquipped = true;
        Debug.Log($"<color=orange>[Melee] Đã rút {weaponData.itemName} ra. Sẵn sàng chiến đấu!</color>");

        // TODO sau này: Kích hoạt animation cầm vũ khí cận chiến
    }

    public void OnUnequip()
    {
        isEquipped = false;
        Debug.Log($"<color=grey>[Melee] Đã cất {weaponData.itemName}.</color>");

        // TODO sau này: Reset animation
    }

    // --- Logic Tấn công ---
    private void Update()
    {
        if (!isEquipped || weaponData == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        // Kiểm tra Cooldown (Tốc độ chém)
        if (Time.time - lastAttackTime < weaponData.cooldown)
        {
            // Chưa hết thời gian hồi chiêu -> Không cho chém
            return;
        }

        lastAttackTime = Time.time;

        // Tạm thời Log ra để test luồng, sau này ghép Animation và BoxCast (Hitbox) vào đây
        Debug.Log($"<color=red>VÚT! Chém trúng mục tiêu -> Sát thương: {weaponData.damage}</color>");
    }
}