using UnityEngine;

/// <summary>
/// ZombieWardenOne — "THE RUSHER"
/// Boss đầu tiên của DEAD ROOF. Personality: aggressive, lao thẳng vào mặt player,
/// đánh combo tay thuần, không dùng spike.
///
/// Animation triggers dùng:
///   attack1  → đánh tay phải đơn (hit 1 của combo)
///   attack2  → đánh 2 tay luân phiên (hit 2, kết thúc combo cơ bản)
///   attack3  → đánh tay phải mạnh hơn (hit 3, finisher)
///   attack4  → quất mạnh 1 cú ra trước (punish khi player đứng yên)
///   attack5  → bước lên rồi đánh mạnh (phase 2 finisher, có bước tiến)
///
/// Phase 1 (HP > 50%): Combo 3 hit → attack1 → attack2 → attack3
///                      Nếu player không di chuyển sau combo → attack4 (punish)
/// Phase 2 (HP ≤ 50%): Tốc độ +25%, thỉnh thoảng thay attack3 bằng attack5,
///                      cooldown ngắn hơn, bắt đầu FacePlayer liên tục
/// </summary>
public class ZombieWardenOne : ZombieBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Warden I — Rusher Settings")]
    [Tooltip("Khoảng cách bắt đầu combo (nên bằng hoặc lớn hơn attackRange một chút)")]
    public float comboStartRange = 2.5f;

    [Tooltip("Damage nhân thêm cho attack4 (punish)")]
    public float punishDamageMultiplier = 1.5f;

    [Tooltip("Damage nhân thêm cho attack5 (phase 2 finisher)")]
    public float enrageDamageMultiplier = 1.8f;

    [Tooltip("HP % để kích hoạt Phase 2")]
    [Range(0f, 1f)]
    public float enrageThreshold = 0.5f;

    [Tooltip("Tốc độ bonus khi Enrage (nhân với runSpeed)")]
    public float enrageSpeedMultiplier = 1.25f;

    [Tooltip("Thời gian chờ giữa các combo (giây)")]
    public float comboCooldown = 2.0f;

    [Tooltip("Thời gian delay giữa từng hit trong combo (giây)")]
    public float hitDelay = 0.6f;

    [Tooltip("Thời gian recover sau khi xong toàn bộ combo")]
    public float recoverDuration = 1.0f;

    [Tooltip("Xác suất dùng attack5 thay attack3 khi ở Phase 2")]
    [Range(0f, 1f)]
    public float enrageFinisherChance = 0.6f;

    [Tooltip("Xác suất attack4 punish sau combo nếu player không di chuyển")]
    [Range(0f, 1f)]
    public float punishChance = 0.5f;

    // ── Private State Machine ─────────────────────────────────────────────────
    private enum CombatState
    {
        Approach,       // Chạy đến gần player
        ComboHit1,      // Đang thực hiện attack1
        ComboHit2,      // Đang thực hiện attack2
        ComboHit3,      // Đang thực hiện attack3 / attack5
        PunishAttack,   // Đang thực hiện attack4
        Recover,        // Thời gian nghỉ sau combo
        CooldownWait,   // Chờ comboCooldown trước combo tiếp theo
    }

    private CombatState _state = CombatState.Approach;

    // Timers
    private float _stateTimer = 0f;     // đếm thời gian trong state hiện tại
    private float _cooldownTimer = 0f;  // đếm comboCooldown

    // Phase tracking
    private bool _isEnraged = false;

    // Punish tracking — kiểm tra player có di chuyển sau combo không
    private Vector3 _playerPosAfterCombo = Vector3.zero;
    private bool _checkingPunish = false;

    // Damage flag — tránh damage nhiều lần trong 1 swing
    private bool _hit1Dealt = false;
    private bool _hit2Dealt = false;
    private bool _hit3Dealt = false;
    private bool _punishDealt = false;

    // ── ZombieBase Overrides ──────────────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        _state = CombatState.Approach;
        _cooldownTimer = 0f;
        _isEnraged = false;
        Debug.Log("[WardenI] Enter Combat");
    }

    protected override void OnExitCombat()
    {
        _state = CombatState.Approach;
        _isEnraged = false;
        _checkingPunish = false;
    }

    protected override void UpdateCombatBehaviour()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        CheckEnrage();

        float dist = Vector3.Distance(transform.position, player.position);
        float currentRunSpeed = _isEnraged ? runSpeed * enrageSpeedMultiplier : runSpeed;

        switch (_state)
        {
            case CombatState.Approach:
                HandleApproach(dist, currentRunSpeed);
                break;

            case CombatState.ComboHit1:
                HandleComboHit1(dist);
                break;

            case CombatState.ComboHit2:
                HandleComboHit2(dist);
                break;

            case CombatState.ComboHit3:
                HandleComboHit3(dist);
                break;

            case CombatState.PunishAttack:
                HandlePunishAttack();
                break;

            case CombatState.Recover:
                HandleRecover();
                break;

            case CombatState.CooldownWait:
                HandleCooldownWait(dist, currentRunSpeed);
                break;
        }
    }

    // ── State Handlers ────────────────────────────────────────────────────────

    /// <summary>Chạy thẳng về phía player, khi vào tầm bắt đầu combo.</summary>
    private void HandleApproach(float dist, float speed)
    {
        ResumeAgent(speed);
        agent.stoppingDistance = comboStartRange - 0.3f;
        agent.SetDestination(player.position);
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);

        if (dist <= comboStartRange)
        {
            StartComboHit1();
        }
    }

    /// <summary>Hit 1 — attack1 (tay phải đơn).</summary>
    private void HandleComboHit1(float dist)
    {
        StopAgentCompletely();
        FacePlayer();
        _stateTimer += Time.deltaTime;

        // Deal damage tại midpoint của animation
        if (!_hit1Dealt && _stateTimer >= hitDelay * 0.5f)
        {
            if (dist <= attackRange * 1.3f)
                DealDamageToPlayer();
            _hit1Dealt = true;
        }

        if (_stateTimer >= hitDelay)
        {
            TransitionTo(CombatState.ComboHit2);
            anim.SetTrigger("attack2");
        }
    }

    /// <summary>Hit 2 — attack2 (2 tay luân phiên).</summary>
    private void HandleComboHit2(float dist)
    {
        StopAgentCompletely();
        FacePlayer();
        _stateTimer += Time.deltaTime;

        if (!_hit2Dealt && _stateTimer >= hitDelay * 0.6f)
        {
            if (dist <= attackRange * 1.3f)
                DealDamageToPlayer();
            _hit2Dealt = true;
        }

        if (_stateTimer >= hitDelay)
        {
            // Ghi vị trí player để check punish sau
            _playerPosAfterCombo = player.position;
            _checkingPunish = true;

            TransitionTo(CombatState.ComboHit3);

            // Phase 2: random dùng attack5 (bước lên đánh) hoặc attack3
            if (_isEnraged && Random.value < enrageFinisherChance)
                anim.SetTrigger("attack5");
            else
                anim.SetTrigger("attack3");
        }
    }

    /// <summary>Hit 3 — attack3 hoặc attack5 (finisher).</summary>
    private void HandleComboHit3(float dist)
    {
        StopAgentCompletely();
        FacePlayer();
        _stateTimer += Time.deltaTime;

        if (!_hit3Dealt && _stateTimer >= hitDelay * 0.5f)
        {
            if (dist <= attackRange * 1.5f) // attack5 có bước tiến nên range rộng hơn
            {
                float dmg = attackDamage * (_isEnraged ? enrageDamageMultiplier : 1f);
                player.GetComponent<HealthSystem>()?.TakeDamage(dmg, gameObject);
            }
            _hit3Dealt = true;
        }

        if (_stateTimer >= hitDelay * 1.2f) // finisher animation dài hơn
        {
            // Kiểm tra xem có punish không
            if (_checkingPunish && Random.value < punishChance)
            {
                float moved = Vector3.Distance(player.position, _playerPosAfterCombo);
                if (moved < 1.5f) // player đứng yên → punish
                {
                    TransitionTo(CombatState.PunishAttack);
                    anim.SetTrigger("attack4");
                    _checkingPunish = false;
                    return;
                }
            }

            _checkingPunish = false;
            TransitionTo(CombatState.Recover);
        }
    }

    /// <summary>Punish — attack4 (quất mạnh 1 cú, damage cao).</summary>
    private void HandlePunishAttack()
    {
        StopAgentCompletely();
        FacePlayer();
        _stateTimer += Time.deltaTime;

        if (!_punishDealt && _stateTimer >= hitDelay * 0.4f)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange * 1.2f)
            {
                float dmg = attackDamage * punishDamageMultiplier;
                player.GetComponent<HealthSystem>()?.TakeDamage(dmg, gameObject);
            }
            _punishDealt = true;
        }

        if (_stateTimer >= hitDelay * 0.9f)
        {
            TransitionTo(CombatState.Recover);
        }
    }

    /// <summary>Recover — đứng yên ngắn sau combo.</summary>
    private void HandleRecover()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.2f, Time.deltaTime);
        FacePlayer();
        _stateTimer += Time.deltaTime;

        if (_stateTimer >= recoverDuration)
        {
            TransitionTo(CombatState.CooldownWait);
            _cooldownTimer = 0f;
        }
    }

    /// <summary>CooldownWait — tiến lại gần player trong lúc chờ cooldown.</summary>
    private void HandleCooldownWait(float dist, float speed)
    {
        _cooldownTimer += Time.deltaTime;

        // Vẫn tiến về phía player trong lúc chờ (không đứng yên)
        float approachSpeed = _isEnraged ? speed : walkSpeed;
        ResumeAgent(approachSpeed);
        agent.stoppingDistance = comboStartRange - 0.3f;
        agent.SetDestination(player.position);

        float animSpeed = _isEnraged ? 2f : 1f;
        anim.SetFloat("Speed", animSpeed, 0.15f, Time.deltaTime);

        float cd = _isEnraged ? comboCooldown * 0.7f : comboCooldown;
        if (_cooldownTimer >= cd)
        {
            TransitionTo(CombatState.Approach);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void StartComboHit1()
    {
        TransitionTo(CombatState.ComboHit1);
        anim.SetTrigger("attack1");
    }

    /// <summary>Chuyển state, reset timer và damage flags liên quan.</summary>
    private void TransitionTo(CombatState next)
    {
        _state = next;
        _stateTimer = 0f;

        // Reset damage flags khi bắt đầu một đợt tấn công mới
        if (next == CombatState.ComboHit1)
        {
            _hit1Dealt = false;
            _hit2Dealt = false;
            _hit3Dealt = false;
            _punishDealt = false;
        }
        if (next == CombatState.PunishAttack)
        {
            _punishDealt = false;
        }
    }

    /// <summary>Kiểm tra HP để kích hoạt Enrage (Phase 2). Chỉ kích hoạt 1 lần.</summary>
    private void CheckEnrage()
    {
        if (_isEnraged) return;
        if (healthSystem == null) return;

        float hpRatio = healthSystem.CurrentHP / healthSystem.MaxHP;
        if (hpRatio <= enrageThreshold)
        {
            _isEnraged = true;
            OnEnrage();
        }
    }

    private void OnEnrage()
    {
        Debug.Log("[WardenI] ENRAGE! Phase 2 activated.");
        // Có thể trigger VFX, âm thanh, roar animation ở đây
        // anim.SetTrigger("Scream"); // optional: gầm lên khi enrage
    }

    // ── Override DealDamageToPlayer (dùng attackDamage base) ─────────────────
    public override void DealDamageToPlayer()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange * 1.3f) return;
        player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage, gameObject);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // Combo start range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, comboStartRange);
    }
}