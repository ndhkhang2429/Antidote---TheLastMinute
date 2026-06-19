using UnityEngine;

/// <summary>
/// ZombieWardenTwo — "THE GHOST"
///
/// 3 zone hành vi:
///   FAR   (dist > farRange, mặc định 8m)  : Strafe + spam attack2LSpike
///   MID   (dist 3–8m)                     : Strafe né + attack1LSpike / attack1RSpike
///   CLOSE (dist < closeRange, mặc định 3m): RAGE BURST → đẩy player ra xa
///
/// Phase 2 (HP ≤ 50%): thêm attack3RSpike ở MID, Rage Burst cooldown ngắn hơn,
///                      strafe nhanh hơn.
///
/// Poll-based (giống WardenOne): dùng AnimatorStateInfo.normalizedTime thay Animation Events.
/// </summary>
public class ZombieWardenTwo : ZombieBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Warden II — Ghost Settings")]

    [Tooltip("Khoảng cách xa: ưu tiên spike tầm xa")]
    public float farRange = 8f;

    [Tooltip("Khoảng cách gần: kích hoạt Rage Burst")]
    public float closeRange = 3f;

    [Tooltip("Tốc độ strafe ngang")]
    public float strafeSpeed = 3f;

    [Tooltip("Thời gian strafe mỗi lần (giây) trước khi đổi hướng")]
    public float strafeDuration = 1.2f;

    [Tooltip("Cooldown giữa các lần bắn spike")]
    public float spikeCooldown = 2.0f;

    [Tooltip("Cooldown Rage Burst (giây)")]
    public float rageBurstCooldown = 8f;

    [Tooltip("Lực đẩy Rage Burst ra xa player")]
    public float rageBurstForce = 12f;

    [Tooltip("Bán kính AoE của Rage Burst")]
    public float rageBurstRadius = 4f;

    [Tooltip("Damage của Rage Burst")]
    public float rageBurstDamage = 20f;

    [Tooltip("HP % để kích hoạt Phase 2")]
    [Range(0f, 1f)]
    public float enrageThreshold = 0.5f;

    [Tooltip("normalizedTime coi là animation xong")]
    [Range(0.5f, 1f)]
    public float exitThreshold = 0.85f;

    // ── State Machine ─────────────────────────────────────────────────────────
    private enum CombatState
    {
        Strafe,             // Di chuyển ngang, tìm cơ hội bắn
        WaitingEnterAnim,   // Đã set trigger, chờ Animator vào đúng state
        WaitingFinishAnim,  // Chờ animation xong
        RageBurstWindup,    // Chuẩn bị Rage Burst (animation rage)
        RageBurstRelease,   // Phát nổ thực sự
        RageBurstRecover,   // Đứng yên ngắn sau burst
    }

    private CombatState _state = CombatState.Strafe;

    // Strafe
    private float _strafeTimer = 0f;
    private int _strafeDirection = 1; // 1 = phải, -1 = trái

    // Spike cooldown
    private float _spikeCooldownTimer = 0f;
    private float _rageBurstTimer = 0f; // cooldown rage burst

    // Anim polling
    private string _waitingStateName = "";
    private float _stateTimer = 0f;
    private bool _hitDealtThisSwing = false;

    // Phase
    private bool _isEnraged = false;

    // Blood FX
    private ZombieBloodFXHandler _bloodFX;

    // ── Start ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        _bloodFX = GetComponent<ZombieBloodFXHandler>();
    }

    // ── Overrides ─────────────────────────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        _state = CombatState.Strafe;
        _strafeTimer = 0f;
        _spikeCooldownTimer = 0f;
        _rageBurstTimer = rageBurstCooldown; // burst available ngay từ đầu nếu bị dồn
        _isEnraged = false;
    }

    protected override void OnExitCombat()
    {
        _state = CombatState.Strafe;
    }

    protected override void UpdateCombatBehaviour()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        CheckEnrage();

        float dist = Vector3.Distance(transform.position, player.position);

        // Tick cooldowns
        _spikeCooldownTimer += Time.deltaTime;
        _rageBurstTimer += Time.deltaTime;

        switch (_state)
        {
            case CombatState.Strafe: HandleStrafe(dist); break;
            case CombatState.WaitingEnterAnim: HandleWaitingEnter(); break;
            case CombatState.WaitingFinishAnim: HandleWaitingFinish(dist); break;
            case CombatState.RageBurstWindup: HandleRageWindup(); break;
            case CombatState.RageBurstRelease: HandleRageRelease(); break;
            case CombatState.RageBurstRecover: HandleRageRecover(); break;
        }
    }

    // ── State Handlers ────────────────────────────────────────────────────────

    private void HandleStrafe(float dist)
    {
        // ── CLOSE: Rage Burst nếu cooldown xong ──
        if (dist < closeRange)
        {
            if (_rageBurstTimer >= rageBurstCooldown)
            {
                StartRageBurst();
                return;
            }
            // Cooldown chưa xong → lùi ra
            RetreatFromPlayer();
            return;
        }

        // ── Strafe ngang quanh player ──
        DoStrafe();

        // ── FAR: spam spike tầm xa ──
        if (dist > farRange)
        {
            if (_spikeCooldownTimer >= spikeCooldown)
            {
                TriggerAttack("Attack2LSpike");
                _spikeCooldownTimer = 0f;
            }
            return;
        }

        // ── MID: spike bên sườn ──
        if (_spikeCooldownTimer >= spikeCooldown)
        {
            string midAttack = ChooseMidAttack();
            TriggerAttack(midAttack);
            _spikeCooldownTimer = 0f;
        }
    }

    /// <summary>Di chuyển ngang (strafe) quanh player, đổi hướng theo strafeDuration.</summary>
    private void DoStrafe()
    {
        _strafeTimer += Time.deltaTime;
        if (_strafeTimer >= strafeDuration)
        {
            _strafeTimer = 0f;
            _strafeDirection = -_strafeDirection;

            // Phase 2: thỉnh thoảng đổi hướng ngẫu nhiên
            if (_isEnraged && Random.value < 0.4f)
                _strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        // Tính vector strafe vuông góc với hướng nhìn player
        Vector3 toPlayer = (player.position - transform.position).normalized;
        toPlayer.y = 0f;
        Vector3 strafeDir = Vector3.Cross(Vector3.up, toPlayer) * _strafeDirection;

        float speed = _isEnraged ? strafeSpeed * 1.3f : strafeSpeed;
        agent.isStopped = false;
        agent.updateRotation = false; // tự xoay mặt về player
        agent.velocity = strafeDir * speed;

        // Giữ khoảng cách: nếu quá xa thì tiến lại, quá gần thì lùi
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > farRange + 2f)
        {
            // Tiến lại
            agent.velocity = (toPlayer * speed * 0.5f) + (strafeDir * speed * 0.5f);
        }
        else if (dist < closeRange + 1f)
        {
            // Lùi ra nhẹ
            agent.velocity = (-toPlayer * speed * 0.3f) + (strafeDir * speed * 0.7f);
        }

        FacePlayer();

        // Chọn animation strafe
        if (_strafeDirection > 0)
            anim.SetFloat("Speed", 1.5f, 0.15f, Time.deltaTime);
        else
            anim.SetFloat("Speed", 1.5f, 0.15f, Time.deltaTime);

        // Set strafe trigger nếu Animator có parameter StrafeRight/StrafeLeft
        // Nếu không có thì dùng Speed blend tree là đủ
    }

    /// <summary>Lùi ra xa khi Rage Burst còn cooldown nhưng player đã quá gần.</summary>
    private void RetreatFromPlayer()
    {
        Vector3 away = (transform.position - player.position).normalized;
        away.y = 0f;
        agent.isStopped = false;
        agent.velocity = away * runSpeed;
        agent.updateRotation = false;
        FacePlayer();
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
    }

    // ── Rage Burst ────────────────────────────────────────────────────────────

    private void StartRageBurst()
    {
        _state = CombatState.RageBurstWindup;
        _stateTimer = 0f;
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("rage");
        Debug.Log("[WardenII] RAGE BURST — Windup!");
    }

    private void HandleRageWindup()
    {
        StopAgentCompletely();
        FacePlayer();
        anim.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);

        // Chờ animation rage vào
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Rage") && info.normalizedTime >= 0.6f)
        {
            // Lúc 60% animation rage → phát nổ
            _state = CombatState.RageBurstRelease;
            _stateTimer = 0f;
        }

        // Timeout fallback
        _stateTimer += Time.deltaTime;
        if (_stateTimer > 3f)
        {
            Debug.LogWarning("[WardenII] Rage windup timeout, force release");
            _state = CombatState.RageBurstRelease;
            _stateTimer = 0f;
        }
    }

    private void HandleRageRelease()
    {
        // Phát nổ 1 lần
        DoRageBurstExplosion();
        _rageBurstTimer = 0f; // reset cooldown
        _state = CombatState.RageBurstRecover;
        _stateTimer = 0f;
    }

    private void HandleRageRecover()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        _stateTimer += Time.deltaTime;

        // Chờ animation rage kết thúc
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        bool rageDone = !info.IsName("Rage") || info.normalizedTime >= exitThreshold;

        if (rageDone && _stateTimer > 0.5f)
        {
            _state = CombatState.Strafe;
            _strafeTimer = 0f;
        }
    }

    /// <summary>AoE shockwave: đẩy + damage player nếu trong bán kính.</summary>
    private void DoRageBurstExplosion()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, rageBurstRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            // Damage
            hit.GetComponent<HealthSystem>()?.TakeDamage(rageBurstDamage, gameObject);

            // Knockback
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                dir.y = 0.3f; // hơi đẩy lên
                rb.AddForce(dir * rageBurstForce, ForceMode.Impulse);
            }

            Debug.Log("[WardenII] Rage Burst hit player!");
            break;
        }

        // Optional: spawn VFX explosion ở đây
        // Instantiate(rageBurstVFX, transform.position, Quaternion.identity);
    }

    // ── Attack Anim Polling ───────────────────────────────────────────────────

    private void HandleWaitingEnter()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
        FacePlayer();

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (info.IsName(_waitingStateName))
        {
            _state = CombatState.WaitingFinishAnim;
            _hitDealtThisSwing = false;
            _stateTimer = 0f;
            return;
        }

        // Timeout → thử lại trigger
        _stateTimer += Time.deltaTime;
        if (_stateTimer > 0.5f)
        {
            anim.SetTrigger(AnimStateToTrigger(_waitingStateName));
            _stateTimer = 0f;
        }
    }

    private void HandleWaitingFinish(float dist)
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);
        FacePlayer();

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(_waitingStateName)) return;

        float t = info.normalizedTime;

        // Deal damage tại 50% clip
        if (!_hitDealtThisSwing && t >= 0.5f)
        {
            DealHitDamage(dist);
            _hitDealtThisSwing = true;
        }

        // Xong → về Strafe
        if (t >= exitThreshold)
        {
            _state = CombatState.Strafe;
            _strafeTimer = 0f;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string ChooseMidAttack()
    {
        if (_isEnraged)
        {
            // Phase 2: thêm attack3RSpike
            float r = Random.value;
            if (r < 0.33f) return "Attack1LSpike";
            if (r < 0.66f) return "Attack1RSpike";
            return "Attack3RSpike";
        }
        // Phase 1: xen kẽ 2 bên
        return Random.value > 0.5f ? "Attack1LSpike" : "Attack1RSpike";
    }

    private void TriggerAttack(string stateName)
    {
        _waitingStateName = stateName;
        _stateTimer = 0f;
        _state = CombatState.WaitingEnterAnim;
        StopAgentCompletely();
        anim.SetTrigger(AnimStateToTrigger(stateName));
    }

    /// <summary>
    /// Convert tên State → tên Trigger.
    /// State:   "Attack2LSpike" → Trigger: "attack2LSpike"
    /// Chỉ lowercase chữ cái đầu tiên.
    /// </summary>
    private string AnimStateToTrigger(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return stateName;
        return char.ToLower(stateName[0]) + stateName.Substring(1);
    }

    private void DealHitDamage(float dist)
    {
        if (player == null) return;

        // Spike tầm xa có range rộng hơn cận chiến
        bool isFarSpike = _waitingStateName == "Attack2LSpike";
        float hitRange = isFarSpike ? farRange + 1f : attackRange * 1.4f;

        if (dist <= hitRange)
        {
            player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage, gameObject);

            if (_bloodFX != null)
            {
                Vector3 hitPoint = player.position + Vector3.up * 1.0f;
                Vector3 hitNormal = (player.position - transform.position).normalized;
                _bloodFX.OnHitMelee(hitPoint, hitNormal);
            }
        }
    }

    private void CheckEnrage()
    {
        if (_isEnraged || healthSystem == null) return;
        float ratio = healthSystem.CurrentHP / healthSystem.MaxHP;
        if (ratio <= enrageThreshold)
        {
            _isEnraged = true;
            Debug.Log("[WardenII] ENRAGE — Phase 2!");
        }
    }

    public override void DealDamageToPlayer() { /* Poll-based */ }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, farRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closeRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rageBurstRadius);
    }
}