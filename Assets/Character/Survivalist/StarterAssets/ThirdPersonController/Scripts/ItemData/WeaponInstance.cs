using UnityEngine;

public class WeaponInstance : MonoBehaviour
{
    [Header("Weapon Identity")]
    public WeaponDataSO weaponData; // Khẩu súng này là súng gì? (Kéo file SO vào đây)

    [Header("Physical Setup")]
    public Transform gunBarrel;     // Nòng súng của riêng khẩu này
    public float bulletSpread = 0.02f; // Độ giật của riêng khẩu này

    [Header("Runtime Status")]
    public int currentAmmo;         // Số đạn hiện tại trong băng

    private void Awake()
    {
        // Khởi tạo đạn đầy băng khi súng được sinh ra lần đầu
        if (weaponData != null)
        {
            currentAmmo = weaponData.magazineSize;
        }
    }
}