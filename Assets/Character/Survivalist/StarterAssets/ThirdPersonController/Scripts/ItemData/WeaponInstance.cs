using UnityEngine;

public class WeaponInstance : MonoBehaviour
{
    [Header("Weapon Identity")]
    public WeaponDataSO weaponData;

    [Header("Physical Setup")]
    public Transform gunBarrel;
    public float bulletSpread = 0.02f;

    [Header("Runtime Status")]
    public int currentAmmo;

    private InventorySlot _boundSlot;

    public InventorySlot BoundSlot =>
        _boundSlot;

    private void Awake()
    {
        /*
         * Không khởi tạo đầy đạn tại đây.
         *
         * Prefab được tạo lại mỗi lần đổi slot,
         * nên khởi tạo tại Awake sẽ làm súng tự đầy.
         */
        currentAmmo = 0;
    }

    public void BindToSlot(
        InventorySlot slot)
    {
        _boundSlot = slot;

        if (_boundSlot == null ||
            _boundSlot.IsEmpty)
        {
            currentAmmo = 0;
            return;
        }

        /*
         * Ưu tiên WeaponDataSO được lưu trong
         * InventorySlot để bảo đảm đúng khẩu súng.
         */
        if (_boundSlot.item is WeaponDataSO slotWeapon)
        {
            weaponData = slotWeapon;
        }

        if (weaponData == null ||
            weaponData.combatType !=
            CombatType.Firearm)
        {
            currentAmmo = 0;
            return;
        }

        _boundSlot.InitializeAmmoIfNeeded();

        currentAmmo = Mathf.Clamp(
            _boundSlot.currentAmmo,
            0,
            weaponData.magazineSize
        );
    }

    public void SaveAmmoToSlot()
    {
        if (_boundSlot == null ||
            weaponData == null ||
            weaponData.combatType !=
            CombatType.Firearm)
        {
            return;
        }

        _boundSlot.SetCurrentAmmo(
            currentAmmo
        );

        InventorySystem.Instance
            ?.NotifyInventoryChanged();
    }

    public bool TryConsumeAmmo(
        int amount = 1)
    {
        if (weaponData == null ||
            weaponData.combatType !=
            CombatType.Firearm)
        {
            return false;
        }

        if (amount <= 0 ||
            currentAmmo < amount)
        {
            return false;
        }

        currentAmmo -= amount;
        SaveAmmoToSlot();

        return true;
    }

    public void SetAmmoAfterReload(int amount)
    {
        if (weaponData == null ||
            weaponData.combatType !=
            CombatType.Firearm)
        {
            return;
        }

        currentAmmo = Mathf.Clamp(
            amount,
            0,
            weaponData.magazineSize
        );

        SaveAmmoToSlot();
    }

    private void OnDestroy()
    {
        SaveAmmoToSlot();
    }
}