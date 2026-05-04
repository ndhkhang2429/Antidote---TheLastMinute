using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

/// <summary>
/// Xử lý toàn bộ logic tấn công của player.
/// Đọc WeaponType từ PlayerState, KHÔNG đọc từ Animator.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Cài đặt Cooldown")]
    public float unarmedCooldown = 0.4f;
    public float weaponCooldown = 0.7f;  // Gậy/xà beng thường chậm hơn đấm

    [Header("Combo Punch")]
    // PunchIndex: 0 = Punching, 1 = Punching1 (khớp với Animator)
    private int _punchComboIndex = 0;
    private float _comboResetTime = 0.8f;  // Thời gian reset combo nếu không bấm tiếp
    private float _lastPunchTime = 0f;

    // ── Private ─────────────────────────────────────────────
    private Animator _animator;
    private float _nextAttackTime = 0f;

    // Animator Parameter IDs
    private int _paramWeaponAttack;
    private int _paramPunch;
    private int _paramPunchIndex;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
            Debug.LogError("[PlayerAttack] Không tìm thấy Animator!");

        _paramWeaponAttack = Animator.StringToHash("WeaponAttack");
        _paramPunch = Animator.StringToHash("Punch");
        _paramPunchIndex = Animator.StringToHash("PunchIndex");
    }

    private void Update()
    {
        // Reset combo nếu lâu không bấm
        if (Time.time - _lastPunchTime > _comboResetTime)
            _punchComboIndex = 0;

        // Detect click chuột trái
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
        {
            Debug.Log($"[PlayerAttack] Cooldown chưa hết, còn {_nextAttackTime - Time.time:F2}s");
            return;
        }

        if (PlayerState.Instance == null)
        {
            Debug.LogError("[PlayerAttack] Không tìm thấy PlayerState!");
            return;
        }

        int weaponType = PlayerState.Instance.WeaponType;

        if (weaponType == 0)
        {
            PerformPunch();
            _nextAttackTime = Time.time + unarmedCooldown;
        }
        else
        {
            // WeaponType 1, 2, 3... đều dùng WeaponAttack trigger
            // Mở rộng sau nếu muốn animation khác nhau theo từng loại
            PerformWeaponAttack();
            _nextAttackTime = Time.time + weaponCooldown;
        }
    }

    private void PerformPunch()
    {
        if (_animator == null) return;

        // Combo: 0 → 1 → 0 → 1...
        _animator.SetInteger(_paramPunchIndex, _punchComboIndex);
        _animator.SetTrigger(_paramPunch);

        _punchComboIndex = (_punchComboIndex + 1) % 2;
        _lastPunchTime = Time.time;

        Debug.Log($"[PlayerAttack] Punch! ComboIndex: {_punchComboIndex}");
    }

    private void PerformWeaponAttack()
    {
        if (_animator == null) return;

        _animator.SetTrigger(_paramWeaponAttack);

        int wt = PlayerState.Instance.WeaponType;
        string weaponName = wt == 1 ? "Gậy/2 tay" : wt == 2 ? "Pistol" : wt == 3 ? "Rifle" : "Weapon";
        Debug.Log($"[PlayerAttack] Tấn công bằng: {weaponName}");
    }

    /// <summary>
    /// Gọi từ bên ngoài (ví dụ input system mới) nếu cần
    /// </summary>
    public void OnAttackInput()
    {
        TryAttack();
    }
}