using System;
using System.Collections;
using StarterAssets;
using UnityEngine;

public class PlayerGunAnimator : MonoBehaviour
{
    [Header("Core References")]
    private Animator _animator;
    private StarterAssetsInputs _input;
    private Camera _mainCamera;

    [Header("Shooting Setup")]
    public LayerMask aimColliderLayerMask;

    // BIẾN NÀY SẼ TỰ ĐỘNG TÌM CÂY SÚNG ĐANG ĐƯỢC BẬT TRÊN TAY
    private WeaponInstance _activeWeapon;

    private bool _isReloading = false;
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

    void Update()
    {
        // Tự động tìm xem nhân vật đang cầm khẩu súng nào (mô hình 3D nào đang Active)
        _activeWeapon = GetComponentInChildren<WeaponInstance>();

        if (_isReloading || _activeWeapon == null) return;

        HandleReload();
        HandleShooting();
    }

    private void HandleReload()
    {
        var data = _activeWeapon.weaponData;

        // Bấm R và súng chưa đầy đạn
        if (_input.reload && _activeWeapon.currentAmmo < data.magazineSize)
        {
            StartReloadSequence(data);
        }
        else if (_input.reload)
        {
            // Bấm R nhưng súng đã đầy -> Hủy lệnh
            _input.reload = false;
        }
    }

    // ĐÃ FIX: Đổi từ IEnumerator sang hàm void để dùng ActionTimerManager
    private void StartReloadSequence(WeaponDataSO data)
    {
        var inv = InventorySystem.Instance;

        if (inv == null || data.compatibleAmmo == null)
        {
            _input.reload = false;
            return;
        }

        int totalAmmoInBackpack = inv.CountItem(data.compatibleAmmo);
        if (totalAmmoInBackpack <= 0)
        {
            Debug.Log("❌ Balo rỗng! Không có đạn để nạp.");
            if (NotificationUI.Instance != null)
                NotificationUI.Instance.ShowNotification("Không có đạn dự trữ!");

            _input.reload = false;
            return;
        }

        _isReloading = true;
        _input.reload = false;

        // Kích hoạt animation rút băng đạn
        if (_animator != null) _animator.SetTrigger(_hashReload);
        Debug.Log($"🔄 Đang thay đạn cho {data.itemName}...");

        // GỌI VÒNG TRÒN ĐẾM NGƯỢC
        if (ActionTimerManager.Instance != null)
        {
            ActionTimerManager.Instance.StartAction($"Đang nạp {data.itemName}...", data.reloadTime, () =>
            {
                // LƯU Ý BẢO MẬT: Kiểm tra xem súng hiện tại có bị tráo trong lúc nạp không
                if (_activeWeapon != null && _activeWeapon.weaponData == data)
                {
                    int bulletsNeeded = data.magazineSize - _activeWeapon.currentAmmo;
                    int bulletsToReload = Mathf.Min(bulletsNeeded, totalAmmoInBackpack);

                    _activeWeapon.currentAmmo += bulletsToReload;
                    inv.RemoveItem(data.compatibleAmmo, bulletsToReload);

                    Debug.Log($"✅ Nạp xong! Đạn trong súng: {_activeWeapon.currentAmmo}");
                }

                // Trả lại quyền bắn
                _isReloading = false;
            });
        }
        else
        {
            Debug.LogWarning("[PlayerGunAnimator] Không tìm thấy ActionTimerManager trong Scene!");
            _isReloading = false;
        }
    }

    private void HandleShooting()
    {
        if (!_input.shoot)
        {
            if (_animator != null) _animator.ResetTrigger(_hashShoot);
            return;
        }

        var data = _activeWeapon.weaponData;
        if (data.combatType != CombatType.Firearm)
        {
            _input.shoot = false;
            return;
        }

        if (_activeWeapon.currentAmmo <= 0)
        {
            Debug.Log("❌ Tạch tạch! Hết đạn rồi!");
            if (NotificationUI.Instance != null)
                NotificationUI.Instance.ShowNotification("Súng hết đạn!");
            _input.shoot = false;
            return;
        }

        if (Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + data.fireRate;
            if (_animator != null) _animator.SetTrigger(_hashShoot);

            ExecuteShoot();

            if (!data.isAutomatic) _input.shoot = false;
        }
    }

    private void ExecuteShoot()
    {
        var data = _activeWeapon.weaponData;

        if (data.bulletPrefab == null || _activeWeapon.gunBarrel == null) return;

        // TRỪ ĐẠN TRỰC TIẾP TRÊN CÂY SÚNG ĐÓ
        _activeWeapon.currentAmmo--;
        Debug.Log($"💥 Đùng! Súng còn: {_activeWeapon.currentAmmo} viên");

        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = _mainCamera.ScreenPointToRay(screenCenterPoint);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 999f, aimColliderLayerMask)
                              ? hit.point : ray.GetPoint(100f);

        GameObject bulletObj = Instantiate(data.bulletPrefab, _activeWeapon.gunBarrel.position, Quaternion.identity);

        Vector3 originalDirection = (targetPoint - _activeWeapon.gunBarrel.position).normalized;

        // Lấy độ giật từ cây súng hiện tại
        float spread = _activeWeapon.bulletSpread;
        Vector3 randomSpread = new Vector3(
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread)
        );

        Vector3 shootDirection = (originalDirection + randomSpread).normalized;
        bulletObj.transform.forward = shootDirection;

        if (bulletObj.TryGetComponent<BulletProjectile>(out BulletProjectile bulletScript))
            bulletScript.SetupBullet(data.damage);

        if (bulletObj.TryGetComponent<Rigidbody>(out Rigidbody bulletRb))
            bulletRb.AddForce(shootDirection * data.bulletSpeed, ForceMode.Impulse);

        if (data.muzzleFlashPrefab != null)
            Instantiate(data.muzzleFlashPrefab, _activeWeapon.gunBarrel.position, _activeWeapon.gunBarrel.rotation);
    }
}