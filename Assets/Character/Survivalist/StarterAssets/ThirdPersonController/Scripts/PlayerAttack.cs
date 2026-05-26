using UnityEngine;
using StarterAssets;  // ThirdPersonController input

/// <summary>
/// Xử lý toàn bộ logic tấn công của player.
/// — Đọc config từ WeaponDataSO (damage, cooldown, hitbox)
/// — Nhận input từ StarterAssets (đồng bộ với ThirdPersonController)
/// — Fire GameEventSO để HUD/Audio phản ứng mà không cần biết nhau
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Weapon Configs — tạo asset WeaponData_ trong Project")]
    [SerializeField] private WeaponDataSO _unarmedData;   // kéo WeaponData_Unarmed vào
    [SerializeField] private WeaponDataSO _meleeData;     // kéo WeaponData_Melee vào

    [Header("Zombie Layer")]
    [SerializeField] private LayerMask _zombieLayer;

    [Header("Events — kéo SO vào đây trong Inspector")]
    [SerializeField] private GameEventSO OnWeaponFired;   // HUD, Audio lắng nghe

    // ── Private refs ───────────────────────────────────────
    private Animator _animator;
    private StarterAssetsInputs _input;      // input chung với ThirdPersonController

    // ── Combo state ────────────────────────────────────────
    private int _punchComboIndex = 0;
    private float _lastPunchTime = 0f;

    // ── Cooldown state ─────────────────────────────────────
    private float _nextAttackTime = 0f;
    private float _currentDamage = 0f;

    // ── Animator param IDs ─────────────────────────────────
    private int _paramWeaponAttack;
    private int _paramPunch;
    private int _paramPunchIndex;

    // ── Lifecycle ──────────────────────────────────────────

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _input = GetComponent<StarterAssetsInputs>();

        if (_animator == null) Debug.LogError("[PlayerAttack] Không tìm thấy Animator!");
        if (_input == null) Debug.LogError("[PlayerAttack] Không tìm thấy StarterAssetsInputs!");
        if (_unarmedData == null) Debug.LogError("[PlayerAttack] Chưa gán _unarmedData!");
        if (_meleeData == null) Debug.LogError("[PlayerAttack] Chưa gán _meleeData!");

        _paramWeaponAttack = Animator.StringToHash("WeaponAttack");
        _paramPunch = Animator.StringToHash("Punch");
        _paramPunchIndex = Animator.StringToHash("PunchIndex");
    }

    private void Update()
    {
        // Reset combo nếu lâu không bấm
        WeaponDataSO current = CurrentWeaponData();
        if (current != null && Time.time - _lastPunchTime > current.comboResetTime)
            _punchComboIndex = 0;

        //Input: StarterAssets dùng bool shoot — set true khi click chuột trái
        if (_input != null && _input.shoot)
        {
            _input.shoot = false;   // consume input, tránh fire liên tục
            TryAttack();
        }
    }

    // ── Logic tấn công ─────────────────────────────────────

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        if (PlayerState.Instance == null)
        {
            Debug.LogError("[PlayerAttack] Không tìm thấy PlayerState!");
            return;
        }

        int weaponType = PlayerState.Instance.WeaponType;
        WeaponDataSO data = weaponType == 0 ? _unarmedData : _meleeData;

        if (data == null)
        {
            Debug.LogWarning($"[PlayerAttack] Không có WeaponDataSO cho WeaponType={weaponType}");
            return;
        }

        _currentDamage = data.damage;
        _nextAttackTime = Time.time + data.cooldown;

        if (weaponType == 0)
            PerformPunch(data);
        else
            PerformWeaponAttack(data);

        OnWeaponFired?.Raise();
    }

    private void PerformPunch(WeaponDataSO data)
    {
        if (_animator == null) return;

        _animator.SetInteger(_paramPunchIndex, _punchComboIndex);
        _animator.SetTrigger(_paramPunch);

        _punchComboIndex = (_punchComboIndex + 1) % data.comboSteps;
        _lastPunchTime = Time.time;

        Debug.Log($"[PlayerAttack] Punch combo {_punchComboIndex}");
    }

    private void PerformWeaponAttack(WeaponDataSO data)
    {
        if (_animator == null) return;

        _animator.SetTrigger(_paramWeaponAttack);
        Debug.Log($"[PlayerAttack] Tấn công: {data.weaponName}");
    }

    // ── Animation Event (gắn vào đúng frame trong Animator) ──

    /// <summary>
    /// Gọi từ Animation Event khi cú đánh chạm mục tiêu.
    /// </summary>
    public void OnMeleeHit()
    {
        WeaponDataSO data = CurrentWeaponData();
        if (data == null) return;

        Vector3 hitCenter = transform.position
                          + transform.forward * data.hitDistance
                          + Vector3.up * data.hitHeight;

        Collider[] hits = Physics.OverlapSphere(hitCenter, data.hitRadius, _zombieLayer);
        if (hits.Length == 0) return;

        foreach (Collider hit in hits)
        {
            ZombieBase zombie = hit.GetComponent<ZombieBase>()
                             ?? hit.GetComponentInParent<ZombieBase>();

            if (zombie != null)
            {
                zombie.TakeDamage(_currentDamage, gameObject);

                // Blood VFX
                Vector3 hitPoint = hit.ClosestPoint(hitCenter);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;
                hit.GetComponentInParent<ZombieBloodFXHandler>()
                   ?.OnHitMelee(hitPoint, hitNormal);

                break;
            }
        }
    }

    // ── Helper ─────────────────────────────────────────────

    private WeaponDataSO CurrentWeaponData()
    {
        if (PlayerState.Instance == null) return null;
        return PlayerState.Instance.WeaponType == 0 ? _unarmedData : _meleeData;
    }

    // ── Gizmos ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        WeaponDataSO data = CurrentWeaponData() ?? _unarmedData;
        if (data == null) return;

        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position
                          + transform.forward * data.hitDistance
                          + Vector3.up * data.hitHeight;
        Gizmos.DrawWireSphere(hitCenter, data.hitRadius);
    }
}