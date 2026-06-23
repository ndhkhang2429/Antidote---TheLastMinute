using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MutatedBossZombie : ZombieBase
{
    protected enum BossCombatState
    {
        None,
        P1_Normal, P1_Stomp, P1_Summon,
        Transition,
        P2_Normal, P2_Preparing, P2_Charge, P2_Leap, P2_Frenzy
    }

    [Header("== BOSS MODELS & PHASES ==")]
    [SerializeField] private GameObject modelV2;
    [SerializeField] private GameObject modelV3;
    [SerializeField] private Transform[] minionSpawnPoints;
    [SerializeField] private GameObject minionPrefab;

    [Header("== EFFECTS & VFX ==")]
    [SerializeField] private GameObject stompVfxPrefab;
    [SerializeField] private GameObject leapVfxPrefab;
    [SerializeField] private GameObject groundCrackPrefab;

    [Header("Phase 1 Cooldowns")]
    [SerializeField] private float stompCooldown = 8f;
    [SerializeField] private float summonCooldown = 20f;

    [Header("Phase 2 Settings (Pattern & Telegraph)")]
    [SerializeField] private float p2AttackInterval = 4f;
    [SerializeField] private float windUpTime = 1.5f;
    [SerializeField] private float chargeCooldown = 12f;
    [SerializeField] private float leapCooldown = 15f;
    [SerializeField] private float frenzyCooldown = 5f;

    [Header("== TUNING CÚ NHẢY (LEAP CONFIG) ==")]
    [Tooltip("Độ cao cực đại của cú nhảy (mét)")]
    [SerializeField] private float leapMaxHeight = 4f;
    [Tooltip("Thời gian bay trên không cho đến khi chạm đất (giây)")]
    [SerializeField] private float leapFlyDuration = 1.0f;
    [Tooltip("Thời gian Boss lấy đà nhún người trước khi thực sự bay lên")]
    [SerializeField] private float leapTakeoffDelay = 0.2f;

    [Header("Phase 2 Stats Boost")]
    [SerializeField] private float chargeSpeed = 16f;

    // Các biến trạng thái nội bộ
    private BossCombatState _bossState = BossCombatState.None;
    private bool _isPhase2 = false;

    private float _stompTimer = 0f;
    private float _summonTimer = 0f;
    private float _p2DecisionTimer = 0f;
    private float _chargeTimer = 0f;
    private float _leapTimer = 0f;
    private float _frenzyTimer = 0f;
    private float _wanderTimer = 0f;

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
        _p2DecisionTimer = p2AttackInterval;
        _chargeTimer = chargeCooldown;
        _leapTimer = leapCooldown;
        _frenzyTimer = frenzyCooldown;

        ForceAlert();
    }

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

        if (_isPhase2)
        {
            _p2DecisionTimer += Time.deltaTime;
            _chargeTimer += Time.deltaTime;
            _leapTimer += Time.deltaTime;
            _frenzyTimer += Time.deltaTime;
        }

        if (_bossState == BossCombatState.P2_Preparing)
        {
            FacePlayer(false);
        }
    }

    protected override void UpdateCombatBehaviour()
    {
        if (_isStunned || _bossState == BossCombatState.Transition || _bossState == BossCombatState.P2_Preparing || _bossState == BossCombatState.P2_Leap) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (_bossState)
        {
            case BossCombatState.P1_Normal:
                if (_summonTimer >= summonCooldown) { ExecuteP1_Summon(); }
                else if (distanceToPlayer <= 4.5f && _stompTimer >= stompCooldown) { ExecuteP1_Stomp(); }
                else
                {
                    ResumeAgent(runSpeed);
                    agent.stoppingDistance = attackRange;
                    agent.SetDestination(player.position);
                    anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
                }
                break;

            case BossCombatState.P2_Normal:
                if (_p2DecisionTimer >= p2AttackInterval)
                {
                    ChooseRandomP2Attack();
                }
                else
                {
                    UpdateP2WanderLogic();
                }
                break;

            case BossCombatState.P2_Charge:
                UpdateChargeMovement();
                break;
        }
    }

    private void UpdateP2WanderLogic()
    {
        _wanderTimer -= Time.deltaTime;

        if (_wanderTimer <= 0)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 8f;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 8f, 1))
            {
                agent.SetDestination(hit.position);
            }
            _wanderTimer = 3f;
        }

        ResumeAgent(runSpeed);
        agent.stoppingDistance = 0f;
        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }

    private void ChooseRandomP2Attack()
    {
        _p2DecisionTimer = 0f;

        List<int> availableAttacks = new List<int>();

        if (_leapTimer >= leapCooldown) availableAttacks.Add(0);
        if (_chargeTimer >= chargeCooldown) availableAttacks.Add(1);
        if (_frenzyTimer >= frenzyCooldown) availableAttacks.Add(2);

        if (availableAttacks.Count == 0) return;

        int randomIndex = availableAttacks[Random.Range(0, availableAttacks.Count)];

        if (randomIndex == 0) StartCoroutine(ExecuteP2_Leap_Routine());
        else if (randomIndex == 1) StartCoroutine(ExecuteP2_Charge_Routine());
        else if (randomIndex == 2) ExecuteP2_Frenzy();
    }

    private void ExecuteP1_Stomp()
    {
        _bossState = BossCombatState.P1_Stomp;
        _stompTimer = 0f;
        StopAgentCompletely();
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

    // ── THUẬT TOÁN NHẢY VÒNG CUNG THEO PLAYER (ĐÃ SỬA LỖI TẠI CHỖ) ───────────────────
    private IEnumerator ExecuteP2_Leap_Routine()
    {
        _bossState = BossCombatState.P2_Preparing;
        StopAgentCompletely();

        // 1. Chờ gồng chiêu dọa Player
        yield return new WaitForSeconds(windUpTime);

        _bossState = BossCombatState.P2_Leap;
        _leapTimer = 0f;

        // 2. Chốt tọa độ mục tiêu của người chơi TẠI THỜI ĐIỂM NÀY và tắt AI Agent
        Vector3 targetPosition = player.position;
        Vector3 startPosition = transform.position;
        agent.enabled = false;

        FacePlayer(true);
        anim.SetTrigger("LeapTrigger");

        // 3. Chờ hoạt ảnh nhún người lấy đà trước khi nhấc chân khỏi mặt đất
        yield return new WaitForSeconds(leapTakeoffDelay);

        // 4. Vòng lặp toán học di chuyển Boss bay lên theo hình vòng cung
        float elapsedTime = 0f;
        while (elapsedTime < leapFlyDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / leapFlyDuration; // Tỷ lệ từ 0.0 -> 1.0

            // Nội suy di chuyển phẳng trên trục XZ tiến về mục tiêu
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);

            // Tính toán độ cao trục Y dựa trên hàm Sin (Sin từ 0 đến PI tạo ra đồ thị vòng cung)
            float height = Mathf.Sin(t * Mathf.PI) * leapMaxHeight;
            currentPos.y += height;

            // Đưa tọa độ mới vào Boss
            transform.position = currentPos;
            yield return null;
        }

        // 5. Đảm bảo đáp chính xác xuống mặt đất khi kết thúc loop
        transform.position = targetPosition;
    }

    private IEnumerator ExecuteP2_Charge_Routine()
    {
        _bossState = BossCombatState.P2_Preparing;
        StopAgentCompletely();

        yield return new WaitForSeconds(windUpTime);

        _bossState = BossCombatState.P2_Charge;
        _chargeTimer = 0f;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;

        _chargeTargetPos = transform.position + (directionToPlayer * 50f);

        ResumeAgent(chargeSpeed);
        agent.stoppingDistance = 0f;
        agent.SetDestination(_chargeTargetPos);

        anim.SetTrigger("ChargeTrigger");
    }

    private void UpdateChargeMovement()
    {
        if (_chargeTimer < 0.5f) return;

        if (!agent.pathPending && agent.remainingDistance <= 1.0f)
        {
            ResetToNormalCombatState();
        }
    }

    private void ExecuteP2_Frenzy()
    {
        _bossState = BossCombatState.P2_Frenzy;
        _frenzyTimer = 0f;

        StopAgentCompletely();
        agent.updatePosition = true;

        FacePlayer();
        anim.SetTrigger("FrenzyTrigger");
    }

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

    private void OnCollisionEnter(Collision collision)
    {
        if (_bossState == BossCombatState.P2_Charge &&
           (collision.gameObject.CompareTag("Pillar") || collision.gameObject.CompareTag("Wall")))
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
    public void Event_TriggerStompShockwave()
    {
        if (stompVfxPrefab != null)
        {
            GameObject vfx = Instantiate(stompVfxPrefab, transform.position, stompVfxPrefab.transform.rotation);
            Destroy(vfx, 4f);
        }

        if (groundCrackPrefab != null)
        {
            Vector3 crackPos = new Vector3(transform.position.x, transform.position.y + 0.7f, transform.position.z);
            Quaternion crackRot = Quaternion.Euler(-90, 0, 0);
            GameObject crack = Instantiate(groundCrackPrefab, crackPos, crackRot);
            Destroy(crack, 10f);
        }

        float shockwaveRadius = 6.0f;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= shockwaveRadius)
        {
            player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage * 1.2f, gameObject);
        }
    }

    public void Event_TriggerLeapShockwave()
    {
        if (leapVfxPrefab != null)
        {
            GameObject vfx = Instantiate(leapVfxPrefab, transform.position, leapVfxPrefab.transform.rotation);
            Destroy(vfx, 4f);
        }

        if (groundCrackPrefab != null)
        {
            Vector3 crackPos = new Vector3(transform.position.x, transform.position.y + 0.05f, transform.position.z);
            Quaternion crackRot = Quaternion.Euler(-90, 0, 0);
            GameObject crack = Instantiate(groundCrackPrefab, crackPos, crackRot);
            Destroy(crack, 10f);
        }

        float shockwaveRadius = 8.0f;
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= shockwaveRadius)
        {
            player.GetComponent<HealthSystem>()?.TakeDamage(attackDamage * 1.5f, gameObject);
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

    public void ResetToNormalCombatState()
    {
        if (_isDead) return;

        _bossState = _isPhase2 ? BossCombatState.P2_Normal : BossCombatState.P1_Normal;

        if (!agent.enabled)
        {
            agent.enabled = true;
        }

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.Warp(transform.position);
            agent.isStopped = false;
        }
    }
}