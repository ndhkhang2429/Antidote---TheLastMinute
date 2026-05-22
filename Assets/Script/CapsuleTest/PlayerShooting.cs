using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class PlayerShooting : MonoBehaviour
{
    public Transform firePoint;

    [Header("Prefabs")]
    public NormalBullet normalBulletPrefab;
    public ExplosiveBullet explosiveBulletPrefab;

    [Header("Current Setup")]
    public bool useExplosiveBullet = false;

    [Header("Data Reference")]
    public PlayerStatsSO playerStatsSO; // Kéo file PlayerStatsData vào đây

    private IObjectPool<BulletBase> normalPool;
    private IObjectPool<BulletBase> explosivePool;

    private BulletData currentData => useExplosiveBullet ? explosiveBulletPrefab.bulletData : normalBulletPrefab.bulletData;
    private int currentAmmo;
    private float nextFireTime;
    private bool isReloading = false;

    void Start()
    {
        GameObject normalContainer = new GameObject("Pool_NormalBullets");
        GameObject explosiveContainer = new GameObject("Pool_ExplosiveBullets");

        normalPool = new ObjectPool<BulletBase>(
            createFunc: () => {
                BulletBase bullet = Instantiate(normalBulletPrefab);
                bullet.transform.SetParent(normalContainer.transform);
                return bullet;
            },
            actionOnGet: (bullet) => bullet.gameObject.SetActive(true),
            actionOnRelease: (bullet) => bullet.gameObject.SetActive(false),
            actionOnDestroy: (bullet) => Destroy(bullet.gameObject),
            defaultCapacity: 20,
            maxSize: 50
        );

        explosivePool = new ObjectPool<BulletBase>(
            createFunc: () => {
                BulletBase bullet = Instantiate(explosiveBulletPrefab);
                bullet.transform.SetParent(explosiveContainer.transform);
                return bullet;
            },
            actionOnGet: (bullet) => bullet.gameObject.SetActive(true),
            actionOnRelease: (bullet) => bullet.gameObject.SetActive(false),
            actionOnDestroy: (bullet) => Destroy(bullet.gameObject),
            defaultCapacity: 10,
            maxSize: 30
        );

        currentAmmo = currentData.magSize;
    }

    void Update()
    {
        if (playerStatsSO != null && playerStatsSO.isDead) return; // Chết thì nghỉ bắn
        if (isReloading) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchBullet(false);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchBullet(true);

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < currentData.magSize)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                // Gọi trừ mana trực tiếp từ SO
                if (playerStatsSO != null && playerStatsSO.ConsumeMana(currentData.manaCost))
                {
                    Shoot();
                }
            }
            else
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void Shoot()
    {
        currentAmmo--;
        nextFireTime = Time.time + currentData.fireRate;

        BulletBase bullet = useExplosiveBullet ? explosivePool.Get() : normalPool.Get();
        bullet.transform.position = firePoint.position;
        bullet.transform.rotation = firePoint.rotation;

        // Hàm Init giờ siêu gọn, không cần truyền cái gì ngoài hàm trả pool
        if (useExplosiveBullet)
            bullet.Init((b) => explosivePool.Release(b));
        else
            bullet.Init((b) => normalPool.Release(b));
    }

    private void SwitchBullet(bool toExplosive)
    {
        if (useExplosiveBullet == toExplosive) return;
        useExplosiveBullet = toExplosive;
        currentAmmo = currentData.magSize;
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(currentData.reloadTime);
        currentAmmo = currentData.magSize;
        isReloading = false;
    }
}