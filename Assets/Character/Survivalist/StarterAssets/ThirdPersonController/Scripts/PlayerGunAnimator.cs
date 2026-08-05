using StarterAssets;
using UnityEngine;

public class PlayerGunAnimator : MonoBehaviour
{
    [Header("FPS Arms")]
    [SerializeField] private Animator _fpsArmsAnimator;

    [Header("FPS Weapon Motion")]
    [SerializeField] private FPSWeaponMotion _weaponMotion;

    [Header("FPS Reference")]
    [SerializeField] private Transform _cameraRoot;

    [Header("Shooting Setup")]
    [SerializeField] private LayerMask _aimColliderLayerMask;

    [Header("Optional Interaction")]
    [SerializeField] private FPSInteractionVisualController _interactionController;

    private StarterAssetsInputs _input;
    private Camera _mainCamera;
    private PlayerEquipmentManager _equipmentManager;

    private WeaponInstance _activeWeapon;
    private WeaponAudioController _weaponAudio;

    private bool _isReloading;
    private float _nextFireTime;

    private int _hashIsShooting;
    private int _hashReloading;
    private int _hashWalkSpeed;

    public WeaponInstance ActiveWeapon => _activeWeapon;
    public bool IsReloading => _isReloading;

    private void Awake()
    {
        _input = GetComponent<StarterAssetsInputs>();
        _mainCamera = Camera.main;
        _equipmentManager = GetComponent<PlayerEquipmentManager>();
        if (_equipmentManager == null)
            _equipmentManager = GetComponentInChildren<PlayerEquipmentManager>(true);

        _hashIsShooting = Animator.StringToHash("isShooting");
        _hashReloading = Animator.StringToHash("reloading");
        _hashWalkSpeed = Animator.StringToHash("walkSpeed");

        ResolveReferences();
    }

    private void OnEnable()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnWeaponEquipped += OnWeaponEquipped;
        }
    }

    private void OnDisable()
    {
        if (_equipmentManager != null)
        {
            _equipmentManager.OnWeaponEquipped -= OnWeaponEquipped;
        }
    }

    private void ResolveReferences()
    {
        if (_fpsArmsAnimator == null)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);

            foreach (Animator animator in animators)
            {
                if (animator.gameObject.name == "FPSViewModel" ||
                    animator.gameObject.name == "FPS_HANDS")
                {
                    _fpsArmsAnimator = animator;
                    break;
                }
            }
        }

        if (_weaponMotion == null)
        {
            _weaponMotion = GetComponentInChildren<FPSWeaponMotion>(true);
        }

        if (_interactionController == null)
        {
            _interactionController =
                GetComponent<FPSInteractionVisualController>();
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateWalkAnimation();

        if (InventoryUI.Instance != null &&
        InventoryUI.Instance.IsOpen)
        {
            CancelCombatInput();
            return;
        }

        if (_activeWeapon == null)
        {
            return;
        }

        if (_interactionController != null &&
            _interactionController.IsInteracting)
        {
            CancelCombatInput();
            return;
        }

        if (_isReloading)
        {
            _input.shoot = false;
            return;
        }

        HandleReload();
        HandleShooting();
    }

    private void UpdateWalkAnimation()
    {
        if (_fpsArmsAnimator == null || _input == null)
        {
            return;
        }

        _fpsArmsAnimator.SetFloat(
            _hashWalkSpeed,
            _input.move.magnitude
        );
    }

    private void OnWeaponEquipped(WeaponInstance weapon)
    {
        CancelReload();

        _activeWeapon = weapon;
        _weaponAudio = null;
        _nextFireTime = 0f;

        if (_activeWeapon != null)
        {
            _weaponAudio =
                _activeWeapon.GetComponent<WeaponAudioController>();

            if (_weaponAudio == null)
            {
                _weaponAudio =
                    _activeWeapon
                        .GetComponentInChildren<WeaponAudioController>(true);
            }

            _weaponAudio?.PlayDraw();
        }

        Debug.Log(
            $"[PlayerGunAnimator] Weapon equipped: " +
            $"{weapon?.weaponData?.itemName ?? "None"}"
        );
    }

    private void HandleReload()
    {
        if (_input == null || !_input.reload)
        {
            return;
        }

        WeaponDataSO data = _activeWeapon.weaponData;

        if (data == null)
        {
            _input.reload = false;
            return;
        }

        if (_activeWeapon.currentAmmo >= data.magazineSize)
        {
            _input.reload = false;
            return;
        }

        StartReloadSequence(data);
    }

    private void StartReloadSequence(WeaponDataSO data)
    {
        InventorySystem inventory = InventorySystem.Instance;

        if (inventory == null || data.compatibleAmmo == null)
        {
            _input.reload = false;
            return;
        }

        int totalAmmo = inventory.CountItem(data.compatibleAmmo);

        if (totalAmmo <= 0)
        {
            _weaponAudio?.PlayEmpty();
            NotificationUI.Instance?.ShowNotification(
                "No reserve ammunition."
            );

            _input.reload = false;
            return;
        }

        _isReloading = true;
        _input.reload = false;
        _input.shoot = false;

        _weaponAudio?.PlayReload();

        if (_fpsArmsAnimator != null)
        {
            _fpsArmsAnimator.SetBool(_hashReloading, true);
        }

        Debug.Log($"[Reload] Reloading {data.itemName}...");

        if (ActionTimerManager.Instance != null)
        {
            ActionTimerManager.Instance.StartAction(
                $"Reloading {data.itemName}...",
                data.reloadTime,
                () => FinishReload(data, inventory)
            );
        }
        else
        {
            _fallbackReloadData = data;
            _fallbackInventory = inventory;

            Invoke(
                nameof(FinishReloadFallback),
                data.reloadTime
            );
        }
    }

    private WeaponDataSO _fallbackReloadData;
    private InventorySystem _fallbackInventory;

    private void FinishReloadFallback()
    {
        if (_fallbackReloadData != null &&
            _fallbackInventory != null)
        {
            FinishReload(
                _fallbackReloadData,
                _fallbackInventory
            );
        }

        _fallbackReloadData = null;
        _fallbackInventory = null;
    }

    private void FinishReload(
        WeaponDataSO data,
        InventorySystem inventory)
    {
        if (_activeWeapon == null ||
            _activeWeapon.weaponData != data)
        {
            CancelReload();
            return;
        }

        int totalAmmo =
            inventory.CountItem(data.compatibleAmmo);

        int needed =
            data.magazineSize - _activeWeapon.currentAmmo;

        int amountToReload =
            Mathf.Min(needed, totalAmmo);

        if (amountToReload > 0)
        {
            int ammoAfterReload =
                _activeWeapon.currentAmmo + amountToReload;

            _activeWeapon.SetAmmoAfterReload(ammoAfterReload);

            inventory.RemoveItem(
                data.compatibleAmmo,
                amountToReload
            );
        }

        Debug.Log(
            $"[Reload] Complete. Ammo: " +
            $"{_activeWeapon.currentAmmo}/" +
            $"{data.magazineSize}"
        );

        EndReloadState();
    }

    private void CancelReload()
    {
        if (!_isReloading)
        {
            return;
        }

        CancelInvoke(nameof(FinishReloadFallback));
        EndReloadState();
    }

    private void EndReloadState()
    {
        if (_fpsArmsAnimator != null)
        {
            _fpsArmsAnimator.SetBool(
                _hashReloading,
                false
            );
        }

        _isReloading = false;
    }

    private void HandleShooting()
    {
        if (_input == null || !_input.shoot)
        {
            return;
        }

        WeaponDataSO data = _activeWeapon.weaponData;

        if (data == null ||
            data.combatType != CombatType.Firearm)
        {
            _input.shoot = false;
            return;
        }

        if (_activeWeapon.currentAmmo <= 0 &&
            !DeveloperCheatManager.InfiniteAmmo)
        {
            _weaponAudio?.PlayEmpty();

            NotificationUI.Instance?.ShowNotification(
                "Magazine empty."
            );

            _input.shoot = false;
            return;
        }

        if (Time.time < _nextFireTime)
        {
            return;
        }

        _nextFireTime =
            Time.time + Mathf.Max(0.01f, data.fireRate);

        ExecuteShoot();

        if (!data.isAutomatic)
        {
            _input.shoot = false;
        }
    }

    private void ExecuteShoot()
    {
        if (!ValidateShootReferences(out WeaponDataSO data))
        {
            return;
        }

        if (!_activeWeapon.TryConsumeAmmo())
        {
            _weaponAudio?.PlayEmpty();
            NotificationUI.Instance?.ShowNotification("Magazine empty.");
            _input.shoot = false;
            return;
        }

        _weaponAudio?.PlayFire();

        Debug.Log(
            $"[Shoot] Ammo remaining: " +
            $"{_activeWeapon.currentAmmo}/" +
            $"{data.magazineSize}"
        );

        if (_fpsArmsAnimator != null)
        {
            _fpsArmsAnimator.SetTrigger(
                _hashIsShooting
            );
        }

        _weaponMotion?.AddRecoil();

        Ray aimRay = CreateAimRay();

        Vector3 targetPoint =
            GetTargetPoint(
                aimRay,
                999f
            );

        Vector3 barrelPosition =
            _activeWeapon.gunBarrel.position;

        Vector3 shootDirection =
            (targetPoint - barrelPosition).normalized;

        shootDirection =
            ApplySpread(
                shootDirection,
                _activeWeapon.bulletSpread
            );

        GameObject bulletObject = Instantiate(
            data.bulletPrefab,
            barrelPosition,
            Quaternion.LookRotation(shootDirection)
        );

        SetupBullet(
            bulletObject,
            shootDirection,
            data
        );

        SpawnMuzzleFlash(data);
    }

    private bool ValidateShootReferences(
        out WeaponDataSO data)
    {
        data = null;

        if (_activeWeapon == null)
        {
            Debug.LogError(
                "[Shoot] Active Weapon đang null!"
            );

            return false;
        }

        data = _activeWeapon.weaponData;

        if (data == null)
        {
            Debug.LogError(
                "[Shoot] WeaponData đang null!",
                _activeWeapon
            );

            return false;
        }

        if (data.bulletPrefab == null)
        {
            Debug.LogError(
                $"[Shoot] {data.itemName} chưa gán Bullet Prefab!",
                _activeWeapon
            );

            return false;
        }

        if (_activeWeapon.gunBarrel == null)
        {
            Debug.LogError(
                $"[Shoot] {data.itemName} chưa gán Gun Barrel!",
                _activeWeapon
            );

            return false;
        }

        return true;
    }

    private Ray CreateAimRay()
    {
        if (_cameraRoot != null)
        {
            return new Ray(
                _cameraRoot.position,
                _cameraRoot.forward
            );
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera != null)
        {
            return _mainCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );
        }

        return new Ray(
            transform.position,
            transform.forward
        );
    }

    private Vector3 GetTargetPoint(
        Ray aimRay,
        float distance)
    {
        if (Physics.Raycast(
                aimRay,
                out RaycastHit hit,
                distance,
                _aimColliderLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return aimRay.GetPoint(distance);
    }

    private static Vector3 ApplySpread(
        Vector3 direction,
        float spread)
    {
        if (spread <= 0f)
        {
            return direction;
        }

        Vector3 randomSpread = new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            Random.Range(-spread, spread)
        );

        return (direction + randomSpread).normalized;
    }

    private static void SetupBullet(
        GameObject bulletObject,
        Vector3 direction,
        WeaponDataSO data)
    {
        if (bulletObject == null)
        {
            return;
        }

        if (bulletObject.TryGetComponent(
                out BulletProjectile bulletScript))
        {
            bulletScript.SetupBullet(data.damage);
        }

        if (bulletObject.TryGetComponent(
                out Rigidbody rigidbody))
        {
            rigidbody.velocity =
                direction * data.bulletSpeed;
        }
        else
        {
            Debug.LogError(
                $"[Shoot] Bullet prefab " +
                $"{bulletObject.name} không có Rigidbody trên root.",
                bulletObject
            );
        }
    }

    private void SpawnMuzzleFlash(
        WeaponDataSO data)
    {
        if (data.muzzleFlashPrefab == null ||
            _activeWeapon == null ||
            _activeWeapon.gunBarrel == null)
        {
            return;
        }

        GameObject muzzleFlash = Instantiate(
            data.muzzleFlashPrefab,
            _activeWeapon.gunBarrel.position,
            _activeWeapon.gunBarrel.rotation
        );

        Destroy(muzzleFlash, 2f);
    }

    private void CancelCombatInput()
    {
        if (_input == null)
        {
            return;
        }

        _input.shoot = false;
        _input.reload = false;
    }
}