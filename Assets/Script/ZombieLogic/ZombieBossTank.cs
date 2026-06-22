using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MutatedBossZombie : ZombieBase
{
    // Định nghĩa các trạng thái Combat nội bộ của riêng Boss
    protected enum BossCombatState
    {
        None,
        P1_Normal, P1_Stomp, P1_Summon,
        Transition,
        P2_Normal, P2_Charge, P2_Leap, P2_Frenzy
    }

    [Header("== BOSS MODELS & PHASES ==")]
    [SerializeField] private GameObject modelV2;
    [SerializeField] private GameObject modelV3;
    [SerializeField] private Transform[] minionSpawnPoints;
    [SerializeField] private GameObject minionPrefab;

    [Header("Phase 1 Cooldowns")]
    [SerializeField] private float stompCooldown = 8f;
    [SerializeField] private float summonCooldown = 20f;

    [Header("Phase 2 Cooldowns")]
    [SerializeField] private float chargeCooldown = 12f;
    [SerializeField] private float leapCooldown = 15f;
    [SerializeField] private float frenzyCooldown = 5f;

    [Header("Phase 2 Stats Boost")]
    [SerializeField] private float enragedRunSpeed = 6f;
    [SerializeField] private float chargeSpeed = 16f;

    // Các biến trạng thái nội bộ
    private BossCombatState _bossState = BossCombatState.None;
    private bool _isPhase2 = false;

    // Bộ đếm thời gian hồi chiêu (Timers)
    private float _stompTimer = 0f;
    private float _summonTimer = 0f;
    private float _chargeTimer = 0f;
    private float _leapTimer = 0f;
    private float _frenzyTimer = 0f;

    private Vector3 _chargeTargetPos;
    private bool _isStunned = false;

    // ── Khởi tạo ─────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();

        if (modelV2 != null) modelV2.SetActive(true);
        if (modelV3 != null) modelV3.SetActive(false);

        _stompTimer = stompCooldown;
        _summonTimer = summonCooldown - 5f;
        _chargeTimer = chargeCooldown;
        _leapTimer = leapCooldown;
        _frenzyTimer = frenzyCooldown;
        ForceAlert();
    }

    // ── Override Hooks từ Lớp Cha ────────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        if (!_isPhase2)
            _bossState = BossCombatState.P1_Normal;
        else
            _bossState = BossCombatState.P2_Normal;
    }

    protected override void OnExitCombat()
    {
        _bossState = BossCombatState.None;
    }

    protected override void Update()
    {
        base.Update();

        if (_isDead || player == null || _mode != ZombieMode.Combat || !ScreamDone) return;

        _stompTimer += Time.deltaTime;
        _summonTimer += Time.deltaTime;
        _chargeTimer += Time.deltaTime;
        _leapTimer += Time.deltaTime;
        _frenzyTimer += Time.deltaTime;
    }

    protected override void UpdateCombatBehaviour()
    {
        if (_isStunned || _bossState == BossCombatState.Transition) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (_bossState)
        {
            // ==================== PHASE 1 LOGIC ====================
            case BossCombatState.P1_Normal:
                if (_summonTimer >= summonCooldown)
                {
                    ExecuteP1_Summon();
                }
                else if (distanceToPlayer <= 4.5f && _stompTimer >= stompCooldown)
                {
                    ExecuteP1_Stomp();
                }
                else
                {
                    ResumeAgent(runSpeed);
                    agent.stoppingDistance = attackRange;
                    agent.SetDestination(player.position);
                    anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
                }
                break;

            // ==================== PHASE 2 LOGIC ====================
            case BossCombatState.P2_Normal:
                if (distanceToPlayer >= 14f && _leapTimer >= leapCooldown)
                {
                    ExecuteP2_Leap();
                }
                else if (distanceToPlayer >= 6f && distanceToPlayer < 14f && _chargeTimer >= chargeCooldown)
                {
                    ExecuteP2_Charge();
                }
                else if (distanceToPlayer <= attackRange && _frenzyTimer >= frenzyCooldown)
                {
                    ExecuteP2_Frenzy();
                }
                else
                {
                    ResumeAgent(enragedRunSpeed);
                    agent.stoppingDistance = attackRange;
                    agent.SetDestination(player.position);
                    anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
                }
                break;

            case BossCombatState.P2_Charge:
                UpdateChargeMovement();
                break;
        }
    }

    // ── Xử Lý Các Đòn Đánh Phase 1 ───────────────────────────────────────────
    private void ExecuteP1_Stomp()
    {
        _bossState = BossCombatState.P1_Stomp;
        _stompTimer = 0f;

        StopAgentCompletely();
        // KHÓA NAVMESH: Cho phép animation Stomp nhấc bổng Boss lên
        agent.updatePosition = false;

        FacePlayer(true);
        anim.SetTrigger("StompTrigger");
    }

    private void ExecuteP1_Summon()
    {
        _bossState = BossCombatState.P1_Summon;
        _summonTimer = 0f;
        StopAgentCompletely();
        anim.SetTrigger("SummonTrigger");
    }

    // ── Xử Lý Các Đòn Đánh Phase 2 ───────────────────────────────────────────
    private void ExecuteP2_Leap()
    {
        _bossState = BossCombatState.P2_Leap;
        _leapTimer = 0f;

        StopAgentCompletely();
        // KHÓA NAVMESH: Cho phép animation Leap vọt tới trước và nhảy lên
        agent.updatePosition = false;

        FacePlayer(true);
        anim.SetTrigger("LeapTrigger");
    }

    private void ExecuteP2_Charge()
    {
        _bossState = BossCombatState.P2_Charge;
        _chargeTimer = 0f;
        _chargeTargetPos = player.position;

        ResumeAgent(chargeSpeed);
        agent.stoppingDistance = 0f;
        agent.SetDestination(_chargeTargetPos);

        anim.SetTrigger("ChargeTrigger");
    }

    private void UpdateChargeMovement()
    {
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            ResetToNormalCombatState();
        }
    }

    private void ExecuteP2_Frenzy()
    {
        _bossState = BossCombatState.P2_Frenzy;
        _frenzyTimer = 0f;
        StopAgentCompletely();
        agent.updatePosition = false;
        FacePlayer();
        anim.SetTrigger("FrenzyTrigger");
    }

    // ── Đánh Chặn TakeDamage Để Chuyển Pha ────────────────────────────────────
    public override void TakeDamage(float damage, GameObject attacker = null)
    {
        if (_isDead) return;

        base.TakeDamage(damage, attacker);

        if (healthSystem != null && !_isPhase2 && _bossState != BossCombatState.Transition)
        {
            float hpPercent = (float)healthSystem.CurrentHP / healthSystem.MaxHP;
            if (hpPercent <= 0.5f)
            {
                StartCoroutine(TriggerPhaseTransition());
            }
        }
    }

    private IEnumerator TriggerPhaseTransition()
    {
        _bossState = BossCombatState.Transition;
        _isPhase2 = true;
        StopAgentCompletely();

        anim.SetTrigger("RoarTransition");

        yield return new WaitForSeconds(2.0f);

        if (modelV2 != null) modelV2.SetActive(false);
        if (modelV3 != null) modelV3.SetActive(true);

        anim.speed = 1.25f;
        _bossState = BossCombatState.P2_Normal;
    }

    // ── Xử Lý Va Chạm Khi Lao Tới ─────────────────────────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        if (_bossState == BossCombatState.P2_Charge && collision.gameObject.CompareTag("Pillar"))
        {
            StartCoroutine(TriggerStunRoutine());
        }
    }

    private IEnumerator TriggerStunRoutine()
    {
        _isStunned = true;
        StopAgentCompletely();
        anim.SetTrigger("StunnedTrigger");

        yield return new WaitForSeconds(2.5f);

        _isStunned = false;
        ResetToNormalCombatState();
    }

    // ── ANIMATION EVENTS ──────────────────────────────────────────────────────

    /// <summary>
    /// Gắn vào frame chạm đất của clip STOMP (Phase 1)
    /// </summary>
    public void Event_TriggerStompShockwave()
    {
        float shockwaveRadius = 6.0f;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= shockwaveRadius)
        {
            player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage * 1.2f, gameObject);
            Debug.Log("Người chơi trúng làn sóng chấn động dậm đất (Stomp)!");
        }
    }

    /// <summary>
    /// Gắn vào frame chạm đất của clip LEAP (Phase 2)
    /// </summary>
    public void Event_TriggerLeapShockwave()
    {
        float shockwaveRadius = 8.0f; // Leap xa hơn nên sóng xung kích to hơn
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= shockwaveRadius)
        {
            player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage * 1.5f, gameObject);
            Debug.Log("Người chơi trúng làn sóng nhảy bổ (Leap)!");
        }
    }

    public void Event_TriggerSummonMinions()
    {
        if (minionPrefab == null || minionSpawnPoints == null) return;

        foreach (Transform t in minionSpawnPoints)
        {
            Instantiate(minionPrefab, t.position, t.rotation);
        }
    }

    /// <summary>
    /// Gắn vào FRAME CUỐI CÙNG của TẤT CẢ các clip đòn đánh (Stomp, Leap, Charge, Summon, Frenzy, Stunned).
    /// </summary>
    public void ResetToNormalCombatState()
    {
        if (_isDead) return;

        _bossState = _isPhase2 ? BossCombatState.P2_Normal : BossCombatState.P1_Normal;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            // BẬT LẠI NAVMESH VÀ ĐỒNG BỘ VỊ TRÍ
            agent.updatePosition = true;
            agent.Warp(transform.position); // Ép NavMesh đi theo vị trí mới của Boss do Root Motion kéo đi

            agent.isStopped = false;
        }
    }
}