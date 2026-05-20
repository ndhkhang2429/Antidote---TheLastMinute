using UnityEngine;

public class HelicopterCombat : MonoBehaviour
{
    public WeaponController machineGun;
    public WeaponController rocketLauncher;

    private WeaponController currentWeapon;

    void Start()
    {
        // Mặc định chọn súng máy
        currentWeapon = machineGun;
    }

    void Update()
    {
        HandleWeaponSwitch();
        HandleShooting();

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(currentWeapon.Reload());
        }
    }

    private void HandleShooting()
    {
        if (currentWeapon.weaponData.isAutomatic)
        {
            // Súng máy: Giữ chuột trái
            if (Input.GetMouseButton(0)) currentWeapon.AttemptFire();
        }
        else
        {
            // Rocket: Click từng phát
            if (Input.GetMouseButtonDown(0)) currentWeapon.AttemptFire();
        }
    }

    private void HandleWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentWeapon = machineGun;
            Debug.Log("Switched to Machine Gun");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentWeapon = rocketLauncher;
            Debug.Log("Switched to Rocket");
        }
    }
}