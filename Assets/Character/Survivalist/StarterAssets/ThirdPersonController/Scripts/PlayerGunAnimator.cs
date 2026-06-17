using StarterAssets;
using UnityEngine;

public class PlayerGunAnimator : MonoBehaviour
{
    [Header("FPS Arms Animator (FPS_HANDS)")]
    [SerializeField] private Animator _fpsArmsAnimator;

    [Header("FPS Reference")]
    [SerializeField] private Transform _cameraRoot;

    [Header("Shooting Setup")]
    public LayerMask aimColliderLayerMask;

    private StarterAssetsInputs _input;
    private Camera _mainCamera;
    public WeaponInstance _activeWeapon;
    private bool _isReloading = false;
    private float _nextFireTime = 0f;

    // Hash parameters
    private int _hashIsShooting;
    private int _hashReloading;
    private int _hashWalkSpeed;

    void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _mainCamera = Camera.main;

        // Tìm FPS_HANDS Animator
        if (_fpsArmsAnimator == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name == "FPS_HANDS")
                {
                    _fpsArmsAnimator = t.GetComponent<Animator>();
                    break;
                }
            }
        }

        // Lắng nghe event từ PlayerEquipmentManager
        var equipManager = GetComponent<PlayerEquipmentManager>();
        if (equipManager != null)
            equipManager.OnWeaponEquipped += OnWeaponEquipped;

        _hashIsShooting = Animator.StringToHash("isShooting");
        _hashReloading = Animator.StringToHash("reloading");
        _hashWalkSpeed = Animator.StringToHash("walkSpeed");
    }

    void OnDestroy()
    {
        var equipManager = GetComponent<PlayerEquipmentManager>();
        if (equipManager != null)
            equipManager.OnWeaponEquipped -= OnWeaponEquipped;
    }

    void OnWeaponEquipped(WeaponInstance weapon)
    {
        _activeWeapon = weapon;
        _isReloading = false;
        _nextFireTime = 0f;
        Debug.Log($"[PlayerGunAnimator] Weapon equipped: {weapon?.weaponData?.itemName ?? "None"}");
    }

    void Update()
    {
        // KHÔNG dùng GetComponentInChildren nữa
        // _activeWeapon được set từ event

        if (_fpsArmsAnimator != null)
        {
            float speed = _input.move.magnitude;
            _fpsArmsAnimator.SetFloat(_hashWalkSpeed, speed);
        }

        if (_isReloading || _activeWeapon == null) return;
        HandleReload();
        HandleShooting();
    }

    // ── RELOAD ────────────────────────────────────────────

    private void HandleReload()
    {
        var data = _activeWeapon.weaponData;

        if (_input.reload && _activeWeapon.currentAmmo < data.magazineSize)
            StartReloadSequence(data);
        else if (_input.reload)
            _input.reload = false;
    }

    private void StartReloadSequence(WeaponDataSO data)
    {
        var inv = InventorySystem.Instance;
        if (inv == null || data.compatibleAmmo == null)
        {
            _input.reload = false;
            return;
        }

        int totalAmmo = inv.CountItem(data.compatibleAmmo);
        if (totalAmmo <= 0)
        {
            NotificationUI.Instance?.ShowNotification("Không có đạn dự trữ!");
            _input.reload = false;
            return;
        }

        _isReloading = true;
        _input.reload = false;

        // Trigger reload animation
        if (_fpsArmsAnimator != null)
            _fpsArmsAnimator.SetBool(_hashReloading, true);

        Debug.Log($"🔄 Đang nạp {data.itemName}...");

        if (ActionTimerManager.Instance != null)
        {
            ActionTimerManager.Instance.StartAction(
                $"Nạp {data.itemName}...",
                data.reloadTime,
                () => FinishReload(data, totalAmmo, inv)
            );
        }
        else
        {
            // Fallback nếu không có ActionTimerManager
            FinishReload(data, totalAmmo, inv);
        }
    }

    private void FinishReload(WeaponDataSO data, int totalAmmo, InventorySystem inv)
    {
        if (_activeWeapon != null && _activeWeapon.weaponData == data)
        {
            int needed = data.magazineSize - _activeWeapon.currentAmmo;
            int toReload = Mathf.Min(needed, totalAmmo);
            _activeWeapon.currentAmmo += toReload;
            inv.RemoveItem(data.compatibleAmmo, toReload);
            Debug.Log($"✅ Nạp xong! Đạn: {_activeWeapon.currentAmmo}/{data.magazineSize}");
        }

        // Kết thúc reload animation
        if (_fpsArmsAnimator != null)
            _fpsArmsAnimator.SetBool(_hashReloading, false);

        _isReloading = false;
    }

    // ── SHOOTING ──────────────────────────────────────────

    private void HandleShooting()
    {
        if (!_input.shoot) return;

        var data = _activeWeapon.weaponData;

        // Chỉ xử lý súng, không xử lý melee ở đây
        if (data.combatType != CombatType.Firearm)
        {
            _input.shoot = false;
            return;
        }

        if (_activeWeapon.currentAmmo <= 0)
        {
            NotificationUI.Instance?.ShowNotification("Súng hết đạn!");
            _input.shoot = false;
            return;
        }

        if (Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + data.fireRate;
            ExecuteShoot();
            if (!data.isAutomatic) _input.shoot = false;
        }
    }

    private void ExecuteShoot()
    {
        var data = _activeWeapon.weaponData;
        if (data.bulletPrefab == null || _activeWeapon.gunBarrel == null) return;

        _activeWeapon.currentAmmo--;
        Debug.Log($"💥 Bắn! Còn {_activeWeapon.currentAmmo}/{data.magazineSize}");

        // Trigger shoot animation trên FPS Arms
        if (_fpsArmsAnimator != null)
            _fpsArmsAnimator.SetTrigger(_hashIsShooting);

        // Tính hướng bắn từ camera
        Vector3 origin = _cameraRoot != null ? _cameraRoot.position : _mainCamera.transform.position;
        Vector3 forward = _cameraRoot != null ? _cameraRoot.forward : _mainCamera.transform.forward;

        Ray ray = new Ray(origin, forward);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask)
                            ? hit.point
                            : ray.GetPoint(100f);

        // Spawn đạn từ nòng súng
        GameObject bulletObj = Instantiate(
            data.bulletPrefab,
            _activeWeapon.gunBarrel.position,
            Quaternion.identity
        );

        // Tính hướng bắn + spread
        Vector3 shootDir = (targetPoint - _activeWeapon.gunBarrel.position).normalized;
        float spread = _activeWeapon.bulletSpread;
        shootDir = (shootDir + new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            Random.Range(-spread, spread)
        )).normalized;

        bulletObj.transform.forward = shootDir;

        if (bulletObj.TryGetComponent<BulletProjectile>(out var bulletScript))
            bulletScript.SetupBullet(data.damage);

        if (bulletObj.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(shootDir * data.bulletSpeed, ForceMode.Impulse);

        // Muzzle flash
        if (data.muzzleFlashPrefab != null)
            Instantiate(
                data.muzzleFlashPrefab,
                _activeWeapon.gunBarrel.position,
                _activeWeapon.gunBarrel.rotation
            );
    }
}