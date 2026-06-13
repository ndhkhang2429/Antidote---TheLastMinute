using StarterAssets;
using UnityEngine;

public class PlayerGunAnimator : MonoBehaviour
{
    [Header("Core References")]
    private Animator _animator;
    private StarterAssetsInputs _input;
    private Camera _mainCamera;

    [Header("Shooting Setup")]
    public Transform gunBarrel; // VỊ TRÍ NÒNG SÚNG (Kéo FirePoint vào đây)
    public LayerMask aimColliderLayerMask;

    [Header("Test Vũ Khí Trực Tiếp")]
    public WeaponDataSO testWeaponData;

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

    // Tự động lấy vũ khí đang cầm từ Inventory
    private WeaponDataSO GetCurrentWeapon()
    {
        //if (InventorySystem.Instance == null) return null;
        //var heldItem = InventorySystem.Instance.GetHeldItem();
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
        var currentWeapon = GetCurrentWeapon();

        // Kiểm tra: Có đang cầm súng không?
        if (currentWeapon != null && currentWeapon.combatType == CombatType.Firearm)
        {
            if (_input.shoot && Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + currentWeapon.fireRate;
                if (_animator != null) _animator.SetTrigger(_hashShoot);

                ExecuteShoot(currentWeapon);
            }
        }
        else if (_input.shoot)
        {
            // Trả lại input.shoot = false nếu không cầm súng để PlayerAttack xử lý đấm
            _input.shoot = false;
        }
    }

    private void ExecuteShoot(WeaponDataSO weaponData)
    {
        // === KIỂM TRA LỖI TẬN GỐC ===
        if (weaponData.bulletPrefab == null)
        {
            Debug.LogError("❌ LỖI: Chưa kéo Prefab Viên Đạn vào file Vũ Khí SO (" + weaponData.itemName + ")!");
            return; // Dừng lại ngay lập tức
        }
        if (gunBarrel == null)
        {
            Debug.LogError("❌ LỖI: Chưa kéo FirePoint vào ô Gun Barrel trên script Player Gun Animator!");
            return; // Dừng lại ngay lập tức
        }

        Debug.Log("✅ Đang sinh ra viên đạn...");

        // 1. TÌM ĐIỂM NGẮM TỪ CAMERA
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = _mainCamera.ScreenPointToRay(screenCenterPoint);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask)
                              ? hit.point : ray.GetPoint(100f);

        // 2. SINH RA VIÊN ĐẠN
        GameObject bulletObj = Instantiate(weaponData.bulletPrefab, gunBarrel.position, Quaternion.identity);

        Vector3 shootDirection = (targetPoint - gunBarrel.position).normalized;
        bulletObj.transform.forward = shootDirection;

        if (bulletObj.TryGetComponent<BulletProjectile>(out BulletProjectile bulletScript))
        {
            bulletScript.SetupBullet(weaponData.damage);
        }

        if (bulletObj.TryGetComponent<Rigidbody>(out Rigidbody bulletRb))
        {
            bulletRb.AddForce(shootDirection * weaponData.bulletSpeed, ForceMode.Impulse);
        }

        // 3. TẠO TIA LỬA (Muzzle Flash)
        if (weaponData.muzzleFlashPrefab != null)
        {
            Instantiate(weaponData.muzzleFlashPrefab, gunBarrel.position, gunBarrel.rotation);
        }
    }
}