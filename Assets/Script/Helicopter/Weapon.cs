using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public string projectilePoolTag; // Tag của đạn trong Object Pool
    public float fireRate = 0.1f;    // Thời gian chờ giữa 2 viên
    public int magSize = 30;         // Kích thước băng đạn
    public float reloadTime = 2f;    // Thời gian nạp đạn

    [Header("References")]
    public Transform firePoint;      // Điểm bắn (đầu nòng súng)
    public ParticleSystem muzzleFlash; // Hiệu ứng chớp lửa đầu nòng

    private int currentAmmo;
    private bool isReloading = false;
    private float nextTimeToFire = 0f;

    void Start()
    {
        currentAmmo = magSize;
    }

    public void TryShoot()
    {
        if (isReloading || Time.time < nextTimeToFire) return;

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        nextTimeToFire = Time.time + fireRate;
        Shoot();
    }

    void Shoot()
    {
        currentAmmo--;

        // Chạy VFX chớp lửa
        if (muzzleFlash != null) muzzleFlash.Play();

        // Lấy đạn từ Pool và bắn
        ObjectPooler.Instance.SpawnFromPool(projectilePoolTag, firePoint.position, firePoint.rotation);
    }

    public IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log(gameObject.name + " is Reloading...");

        // Có thể thêm âm thanh nạp đạn ở đây

        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magSize;
        isReloading = false;
    }
}