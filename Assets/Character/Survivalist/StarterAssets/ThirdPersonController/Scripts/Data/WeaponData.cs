using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Weapon")]
public class WeaponData : ItemData
{
    [Header("Inventory & Gun Stats")]
    public WeaponSlotType slotType;   // Primary, Secondary, Melee, Grenade
    public AmmoType ammoType;
    public int magazineSize;
    public int damage;

    [Header("Melee & Attack Stats")]
    public float cooldown = 0.5f;
    public int comboSteps = 3;
    public float comboResetTime = 1.0f;

    [Header("Hitbox (Melee)")]
    public float hitDistance = 1.0f;
    public float hitHeight = 1.0f;
    public float hitRadius = 0.5f;
}

public enum WeaponSlotType { Primary, Secondary, Melee, Grenade }
public enum AmmoType { Ammo556, Ammo762, Ammo12Gauge, ThrowableNone }