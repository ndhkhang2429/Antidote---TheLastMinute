using UnityEngine;

/// <summary>
/// ZombieWardenOne — "THE RUSHER"
///
/// Thay vì dùng Animation Events (dễ bị nuốt khi Animator transition về idle),
/// script POLL trạng thái Animator mỗi frame:
///   - Khi đang WaitingAnim: chờ Animator bước vào state attack tương ứng
///     rồi chờ normalizedTime >= exitThreshold → tự chuyển combo step tiếp theo
///   - OnHit vẫn dùng Animation Event bình thường (chỉ deal damage, không điều khiển flow)
///
/// Combo Phase 1 (HP > 50%): attack1 → attack2 → attack3
/// Combo Phase 2 (HP ≤ 50%): attack1 → attack2 → attack5 (60%) hoặc attack3 (40%)
/// Punish: attack4 nếu player đứng yên sau combo
/// </summary>
public class ZombieWardenOne : ZombieBase
{
    [Header("Warden I — Rusher Settings")]
    public float comboStartRange = 2.5f;

    [Range(0f, 1f)]
    public float enrageThreshold = 0.5f;
    public float enrageSpeedMultiplier = 1.25f;
    public float comboCooldown = 2.0f;
    public float recoverDuration = 0.8f;

    [Range(0f, 1f)]
    public float enrageFinisherChance = 0.6f;
    [Range(0f, 1f)]
    public float punishChance = 0.5f;

    public float punishDamageMultiplier = 1.5f;
    public float enrageDamageMultiplier = 1.8f;

    [Tooltip("normalizedTime của clip đạt bao nhiêu thì coi là 'xong' (0.85 = 85%)")]
    [Range(0.5f, 1f)]
    public float exitThreshold = 0.85f;

    // ── State Machine ─────────────────────────────────────────────────────────
    private enum CombatState
    {
        Approach,
        WaitingEnterAnim,   // Đã set trigger, chờ Animator BƯỚC VÀO state attack
        WaitingFinishAnim,  // Animator đang chạy attack, chờ normalizedTime >= exitThreshold
        Recover,
        CooldownWait,
    }

    private CombatState _state = CombatState.Approach;

    // Combo
    private int _comboStep = 0;
    private bool _isEnraged = false;
    private bool _isFinisher = false;
    private bool _isPunish = false;

    // Tên state hiện tại đang chờ (để poll Animator)
    private string _waitingStateName = "";

    // Punish
    private Vector3 _playerPosSnapshot = Vector3.zero;
    private bool _pendingPunishCheck = false;

    // Damage
    private float _currentDamageMultiplier = 1f;
    private bool _hitDealtThisSwing = false;

    // Timers
    private float _stateTimer = 0f;
    private float _cooldownTimer = 0f;

    // ── Overrides ─────────────────────────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        _state = CombatState.Approach;
        _comboStep = 0;
        _isEnraged = false;
        _pendingPunishCheck = false;
        _isPunish = false;
    }

    protected override void OnExitCombat()
    {
        _state = CombatState.Approach;
        _comboStep = 0;
        _pendingPunishCheck = false;
        _isPunish = false;
    }

    protected override void UpdateCombatBehaviour()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        CheckEnrage();

        float dist = Vector3.Distance(transform.position, player.position);
        float speed = _isEnraged ? runSpeed * enrageSpeedMultiplier : runSpeed;

        switch (_state)
        {
            case CombatState.Approach: HandleApproach(dist, speed); break;
            case CombatState.WaitingEnterAnim: HandleWaitingEnter(); break;
            case CombatState.WaitingFinishAnim: HandleWaitingFinish(); break;
            case CombatState.Recover: HandleRecover(); break;
            case CombatState.CooldownWait: HandleCooldownWait(dist, speed); break;
        }
    }

    // ── State Handlers ────────────────────────────────────────────────────────

    private void HandleApproach(float dist, float speed)
    {
        ResumeAgent(speed);
        agent.stoppingDistance = comboStartRange - 0.3f;
        agent.SetDestination(player.position);
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);

        if (dist <= comboStartRange)
            StartCombo();
    }

    /// <summary>
    /// Chờ Animator thực sự BƯỚC VÀO state attack mà ta vừa trigger.
    /// Cần bước này vì SetTrigger không apply ngay — có thể mất 1-2 frame.
    /// </summary>
    private void HandleWaitingEnter()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
        FacePlayer();

        // Kiểm tra Animator đã vào đúng state chưa
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(_waitingStateName))
        {
            _state = CombatState.WaitingFinishAnim;
            _hitDealtThisSwing = false;
        }

        // Timeout 0.5s phòng trigger bị nuốt → thử lại
        _stateTimer += Time.deltaTime;
        if (_stateTimer > 0.5f)
        {
            Debug.LogWarning($"[WardenI] Trigger bị nuốt, thử lại: {_waitingStateName}");
            anim.SetTrigger(_waitingStateName.ToLower()); // tên trigger = tên state lowercase
            _stateTimer = 0f;
        }
    }

    /// <summary>
    /// Chờ animation chạy đến exitThreshold rồi chuyển bước tiếp.
    /// Đồng thời deal damage tại hitThreshold (50% clip).
    /// </summary>
    private void HandleWaitingFinish()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
        FacePlayer();

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

        // Kiểm tra vẫn còn trong đúng state
        if (!info.IsName(_waitingStateName)) return;

        float t = info.normalizedTime;

        // Deal damage tại 50% clip
        if (!_hitDealtThisSwing && t >= 0.5f)
        {
            DealHitDamage();
            _hitDealtThisSwing = true;
        }

        // Animation xong → chuyển bước
        if (t >= exitThreshold)
        {
            OnCurrentAnimDone();
        }
    }

    private void HandleRecover()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.2f, Time.deltaTime);
        FacePlayer();
        _stateTimer += Time.deltaTime;

        if (_stateTimer >= recoverDuration)
        {
            // Kiểm tra punish
            if (_pendingPunishCheck && Random.value < punishChance)
            {
                float moved = Vector3.Distance(player.position, _playerPosSnapshot);
                if (moved < 1.5f)
                {
                    _pendingPunishCheck = false;
                    DoPunish();
                    return;
                }
            }

            _pendingPunishCheck = false;
            _state = CombatState.CooldownWait;
            _cooldownTimer = 0f;
            _comboStep = 0;
            _isPunish = false;
        }
    }

    private void HandleCooldownWait(float dist, float speed)
    {
        _cooldownTimer += Time.deltaTime;

        float approachSpeed = _isEnraged ? speed : walkSpeed;
        ResumeAgent(approachSpeed);
        agent.stoppingDistance = comboStartRange - 0.3f;
        agent.SetDestination(player.position);
        anim.SetFloat("Speed", _isEnraged ? 2f : 1f, 0.15f, Time.deltaTime);

        float cd = _isEnraged ? comboCooldown * 0.7f : comboCooldown;
        if (_cooldownTimer >= cd)
            _state = CombatState.Approach;
    }

    // ── Combo Logic ───────────────────────────────────────────────────────────

    private void StartCombo()
    {
        _comboStep = 1;
        _currentDamageMultiplier = 1f;
        _isFinisher = false;
        _isPunish = false;
        TriggerAttack("Attack1");
    }

    /// <summary>Gọi khi animation hiện tại đã chạy xong (normalizedTime >= exitThreshold).</summary>
    private void OnCurrentAnimDone()
    {
        if (_isPunish)
        {
            // Punish xong → về CooldownWait
            _state = CombatState.CooldownWait;
            _cooldownTimer = 0f;
            _comboStep = 0;
            _isPunish = false;
            return;
        }

        if (_comboStep == 1)
        {
            _comboStep = 2;
            _currentDamageMultiplier = 1f;
            TriggerAttack("Attack2");
        }
        else if (_comboStep == 2)
        {
            _comboStep = 3;
            _playerPosSnapshot = player.position;
            _pendingPunishCheck = true;

            if (_isEnraged && Random.value < enrageFinisherChance)
            {
                _isFinisher = true;
                _currentDamageMultiplier = enrageDamageMultiplier;
                TriggerAttack("Attack5");
            }
            else
            {
                _isFinisher = false;
                _currentDamageMultiplier = 1.2f;
                TriggerAttack("Attack3");
            }
        }
        else
        {
            // Combo step 3 xong → Recover
            _state = CombatState.Recover;
            _stateTimer = 0f;
        }
    }

    private void DoPunish()
    {
        _isPunish = true;
        _currentDamageMultiplier = punishDamageMultiplier;
        TriggerAttack("Attack4");
    }

    /// <summary>
    /// Set trigger VÀ ghi nhớ tên state đang chờ.
    /// Tên trigger = lowercase của tên state (attack1, attack2...).
    /// Tên state trong Animator = Attack1, Attack2... (viết hoa chữ đầu).
    /// </summary>
    private void TriggerAttack(string stateName)
    {
        _waitingStateName = stateName;
        _stateTimer = 0f;
        _state = CombatState.WaitingEnterAnim;
        StopAgentCompletely();

        // Trigger name = lowercase của stateName
        anim.SetTrigger(stateName.ToLower());
    }

    // ── Blood FX ──────────────────────────────────────────────────────────────
    private ZombieBloodFXHandler _bloodFX;

    protected override void Start()
    {
        base.Start();
        _bloodFX = GetComponent<ZombieBloodFXHandler>();
    }

    // ── Damage ────────────────────────────────────────────────────────────────

    private void DealHitDamage()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        float hitRange = _isFinisher ? attackRange * 1.6f : attackRange * 1.3f;

        if (dist <= hitRange)
        {
            float dmg = attackDamage * _currentDamageMultiplier;
            player.GetComponent<HealthSystem>()?.TakeDamage(dmg, gameObject);

            // Spawn blood VFX trên player
            if (_bloodFX != null)
            {
                Vector3 hitPoint = player.position + Vector3.up * 1.0f;
                Vector3 hitNormal = (player.position - transform.position).normalized;
                _bloodFX.OnHitMelee(hitPoint, hitNormal);
            }
        }
    }

    public override void DealDamageToPlayer() { /* Poll-based, không dùng */ }

    // ── Enrage ────────────────────────────────────────────────────────────────

    private void CheckEnrage()
    {
        if (_isEnraged || healthSystem == null) return;
        float ratio = healthSystem.CurrentHP / healthSystem.MaxHP;
        if (ratio <= enrageThreshold)
        {
            _isEnraged = true;
            Debug.Log("[WardenI] ENRAGE — Phase 2!");
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, comboStartRange);
    }
}