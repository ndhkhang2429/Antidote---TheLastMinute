using StarterAssets;
using UnityEngine;

public class PlayerGunAnimator : MonoBehaviour
{
    [Header("Core References")]
    private Animator _animator;
    private StarterAssetsInputs _input;
    private Camera _mainCamera;

    [Header("Shooting Setup")]
    public Transform gunBarrel; // VỊ TRÍ NÒNG SÚNG (FirePoint)
    public LayerMask aimColliderLayerMask;

    [Header("Test Vũ Khí Trực Tiếp")]
    public WeaponDataSO testWeaponData;

    [Header("Recoil / Spread")]
    [Tooltip("Độ tản mát của đạn. Ví dụ: 0.02 là giật nhẹ, 0.1 là giật mạnh bóp cò bay lung tung.")]
    public float bulletSpread = 0.02f;

    private float _nextFireTime = 0f;
    private int _hashShoot;
    private int _hashReload;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _input = GetComponent<StarterAssetsInputs>();
        _mainCamera = Camera.main;

        _hashShoot = Animator.StringToHash("Shoot");
        _hashReload = Animator.StringToHash("Reload");
    }

    private WeaponDataSO GetCurrentWeapon()
    {
        // Vẫn đang dùng vũ khí test trực tiếp để thử nghiệm
        return testWeaponData;
    }

    void Update()
    {
        HandleAnimations();
        HandleShooting();
    }

    private void HandleAnimations()
    {
        if (_input.reload)
        {
            if (_animator != null) _animator.SetTrigger(_hashReload);
            _input.reload = false;
        }
    }

    private void HandleShooting()
    {
        // Xóa animation thừa khi buông chuột
        if (!_input.shoot)
        {
            if (_animator != null) _animator.ResetTrigger(_hashShoot);
            return;
        }

        var currentWeapon = GetCurrentWeapon();

        if (currentWeapon == null || currentWeapon.combatType != CombatType.Firearm)
        {
            _input.shoot = false;
            return;
        }

        if (Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + currentWeapon.fireRate;
            if (_animator != null) _animator.SetTrigger(_hashShoot);

            ExecuteShoot(currentWeapon);

            // --- CƠ CHẾ ĐỔI KIỂU BẮN THÔNG MINH ---
            // Nếu khẩu súng hiện tại là súng lục/shotgun (isAutomatic = false)
            // Ép hệ thống tự nhả cò súng. Người chơi phải click chuột lần nữa mới bắn được viên tiếp theo.
            if (!currentWeapon.isAutomatic)
            {
                _input.shoot = false;
            }
        }
    }

    private void ExecuteShoot(WeaponDataSO weaponData)
    {
        if (weaponData.bulletPrefab == null || gunBarrel == null) return;

        // 1. TÌM ĐIỂM NGẮM TỪ CAMERA
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = _mainCamera.ScreenPointToRay(screenCenterPoint);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask)
                              ? hit.point : ray.GetPoint(100f);

        // 2. SINH RA VIÊN ĐẠN
        GameObject bulletObj = Instantiate(weaponData.bulletPrefab, gunBarrel.position, Quaternion.identity);

        // --- TÍNH TOÁN HƯỚNG BẮN CÓ ĐỘ LỆCH (SPREAD) ---
        Vector3 originalDirection = (targetPoint - gunBarrel.position).normalized;

        // Tạo ra một vector nhiễu ngẫu nhiên
        Vector3 randomSpread = new Vector3(
            Random.Range(-bulletSpread, bulletSpread),
            Random.Range(-bulletSpread, bulletSpread),
            Random.Range(-bulletSpread, bulletSpread)
        );

        // Cộng độ nhiễu vào hướng bắn gốc
        Vector3 shootDirection = (originalDirection + randomSpread).normalized;

        bulletObj.transform.forward = shootDirection;

        // 3. XỬ LÝ VẬT LÝ VÀ SÁT THƯƠNG
        if (bulletObj.TryGetComponent<BulletProjectile>(out BulletProjectile bulletScript))
        {
            bulletScript.SetupBullet(weaponData.damage);
        }

        if (bulletObj.TryGetComponent<Rigidbody>(out Rigidbody bulletRb))
        {
            bulletRb.AddForce(shootDirection * weaponData.bulletSpeed, ForceMode.Impulse);
        }

        // 4. TẠO TIA LỬA ĐẦU NÒNG
        if (weaponData.muzzleFlashPrefab != null)
        {
            Instantiate(weaponData.muzzleFlashPrefab, gunBarrel.position, gunBarrel.rotation);
        }
    }
}