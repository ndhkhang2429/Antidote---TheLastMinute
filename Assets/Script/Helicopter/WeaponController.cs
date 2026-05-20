using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public WeaponData weaponData;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("VFX")]
    public ParticleSystem muzzleFlashVFX; // Kéo Particle System lửa đầu nòng vào đây

    // Quản lý đạn
    private int currentAmmo;
    private bool isReloading = false;
    private float nextFireTime = 0f;

    // Object Pool
    private List<GameObject> projectilePool = new List<GameObject>();
    public int poolSize = 20;

    void Start()
    {
        currentAmmo = weaponData.magSize;
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(projectilePrefab);
            obj.SetActive(false);
            projectilePool.Add(obj);
        }
    }

    // Lấy đạn từ Pool ra để dùng
    private GameObject GetPooledProjectile()
    {
        foreach (GameObject obj in projectilePool)
        {
            if (!obj.activeInHierarchy) return obj;
        }

        // Nếu thiếu đạn trong pool, tạo thêm
        GameObject newObj = Instantiate(projectilePrefab);
        newObj.SetActive(false);
        projectilePool.Add(newObj);
        return newObj;
    }

    public void AttemptFire()
    {
        if (isReloading || Time.time < nextFireTime) return;

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        Fire();
    }

    private void Fire()
    {
        currentAmmo--;
        nextFireTime = Time.time + weaponData.fireRate;

        // Chạy VFX đầu nòng
        if (muzzleFlashVFX != null) muzzleFlashVFX.Play();

        // Lấy đạn từ Pool và bắn
        GameObject projectileObj = GetPooledProjectile();
        projectileObj.transform.position = firePoint.position;
        projectileObj.transform.rotation = firePoint.rotation;
        projectileObj.SetActive(true);

        // Gán data cho đạn
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.data = weaponData;
        }
    }

    public IEnumerator Reload()
    {
        if (isReloading || currentAmmo == weaponData.magSize) yield break;

        isReloading = true;
        Debug.Log($"{weaponData.weaponName} is reloading...");

        yield return new WaitForSeconds(weaponData.reloadTime);

        currentAmmo = weaponData.magSize;
        isReloading = false;
        Debug.Log("Reload complete!");
    }
}