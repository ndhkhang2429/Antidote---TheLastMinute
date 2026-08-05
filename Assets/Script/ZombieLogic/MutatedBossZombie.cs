using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MutatedBossZombie : ZombieBase
{
    protected enum BossCombatState
    {
        None,
        P1_Normal, P1_Stomp, P1_Summon, P1_Charge,
        Transition,
        P2_Normal, P2_Preparing, P2_RockSpikes, P2_Leap, P2_Frenzy
    }

    [Header("== BOSS MATERIALS (CẬP NHẬT ĐỔI MÀU DA) ==")]
    [Tooltip("Kéo Material của V2 (Da thường) vào đây")]
    [SerializeField] private Material materialV2;
    [Tooltip("Kéo Material của V3 (Da máu me/Đột biến) vào đây")]
    [SerializeField] private Material materialV3;

    [SerializeField] private Transform[] minionSpawnPoints;
    [SerializeField] private GameObject minionPrefab;

    [Header("== EFFECTS & VFX ==")]
    [SerializeField] private GameObject stompVfxPrefab;
    [SerializeField] private GameObject leapVfxPrefab;
    [SerializeField] private GameObject groundCrackPrefab;

    [Header("Phase 1 Cooldowns")]
    [SerializeField] private float stompCooldown = 8f;
    [SerializeField] private float summonCooldown = 20f;

    [Header("== CHARGE (PHASE 1) ==")]
    [SerializeField] private float chargeCooldown = 10f;
    [SerializeField] private float chargeMinDistance = 5f;
    [SerializeField] private float chargeMaxDistance = 11f;
    [SerializeField] private float chargeWindUp = 0.8f;
    [SerializeField] private float chargeDuration = 1.1f;
    [SerializeField] private float chargeSpeed = 11f;
    [SerializeField] private float chargeHitRadius = 1.8f;
    [SerializeField] private float chargeDamageMultiplier = 1.4f;

    [Header("== SUMMON LIMIT ==")]
    [SerializeField] private int maxAliveMinions = 4;

    [Header("Phase 2 Settings")]
    [SerializeField] private float p2AttackInterval = 4f;
    [SerializeField] private float windUpTime = 1.5f;
    [SerializeField] private float leapCooldown = 15f;
    [SerializeField] private float frenzyCooldown = 5f;
    [SerializeField] private float skillRecoveryTime = 1f;
    [SerializeField] private float skillSafetyTimeout = 7f;

    [Header("== CHIÊU TRỤ ĐÁ (ROCK SPIKES) ==")]
    [SerializeField] private GameObject spikeWarningPrefab;
    [SerializeField] private float rockSpikesCooldown = 12f;
    [SerializeField] private int spikeCount = 25;

    [SerializeField] private Transform roomCenter;
    [SerializeField] private Vector2 roomSize = new Vector2(40f, 40f);

    [Header("== TUNING CÚ NHẢY (LEAP CONFIG) ==")]
    [SerializeField] private float leapMaxHeight = 4f;
    [SerializeField] private float leapFlyDuration = 1.0f;
    [SerializeField] private float leapTakeoffDelay = 0.2f;

    [Tooltip("Kéo Prefab DarkEffect hoặc PoisonEffect vào đây để tạo vụ nổ che mắt")]
    [SerializeField] private GameObject phase2RoarVfxPrefab;

    [Header("== CUTSCENE PHASE TRANSITION ==")]
    [SerializeField] private CutscenePhaseTransition cutsceneManager;
    [SerializeField] private AudioCutsceneManager audioCutsceneManager;

    private BossCombatState _bossState = BossCombatState.None;
    private bool _isPhase2 = false;

    private float _stompTimer = 0f;
    private float _summonTimer = 0f;
    private float _chargeTimer = 0f;
    private float _p2DecisionTimer = 0f;
    private float _rockSpikeTimer = 0f;
    private float _leapTimer = 0f;
    private float _frenzyTimer = 0f;
    private float _wanderTimer = 0f;
    private float _stateTimer = 0f;
    private int _lastP2Attack = -1;
    private readonly List<GameObject> _aliveMinions = new List<GameObject>();

    private SkinnedMeshRenderer _skmr;

    protected override void Start()
    {
        base.Start();

        _skmr = GetComponentInChildren<SkinnedMeshRenderer>();

        // Mặc áo V2 lúc mới vào game
        if (_skmr != null && materialV2 != null)
        {
            _skmr.sharedMaterial = materialV2;
        }

        _stompTimer = stompCooldown;
        _summonTimer = summonCooldown - 5f;
        _chargeTimer = chargeCooldown;
        _p2DecisionTimer = p2AttackInterval;
        _rockSpikeTimer = rockSpikesCooldown;
        _leapTimer = leapCooldown;
        _frenzyTimer = frenzyCooldown;
        // === SETUP CUTSCENE MANAGER ===
        if (cutsceneManager == null)
        {
            cutsceneManager = GetComponent<CutscenePhaseTransition>();
            if (cutsceneManager == null)
            {
                Debug.LogError("[Boss] CutscenePhaseTransition not found!");
                cutsceneManager = gameObject.AddComponent<CutscenePhaseTransition>();
            }
        }

        // Initialize cutscene with references
        cutsceneManager.Initialize(
            this,
            transform,
            anim,
            agent,
            _skmr,
            healthSystem
        );

        if (audioCutsceneManager == null)
        {
            audioCutsceneManager = GetComponent<AudioCutsceneManager>();
        }

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
        // Trong lúc tung skill, không cho Behaviour Tree của ZombieBase giành lại
        // quyền điều khiển NavMeshAgent (đặc biệt khi player chạy ra ngoài detectionRange).
        if (IsUsingSkill())
        {
            if (_isDead) return;

            if (_bossState == BossCombatState.P2_Preparing)
                FacePlayer(false);

            if (CanAnimationSkillTimeout())
            {
                _stateTimer += Time.deltaTime;
                if (_stateTimer >= skillSafetyTimeout)
                {
                    Debug.LogWarning($"[Boss] Skill {_bossState} timed out. Returning to combat.");
                    ResetToNormalCombatState();
                }
            }

            return;
        }

        base.Update();

        if (_isDead || player == null || _mode != ZombieMode.Combat || !ScreamDone) return;

        _stompTimer += Time.deltaTime;
        _summonTimer += Time.deltaTime;
        _chargeTimer += Time.deltaTime;

        if (_isPhase2)
        {
            _p2DecisionTimer += Time.deltaTime;
            _rockSpikeTimer += Time.deltaTime;
            _leapTimer += Time.deltaTime;
            _frenzyTimer += Time.deltaTime;
        }

    }

    protected override void UpdateCombatBehaviour()
    {
        if (_bossState == BossCombatState.Transition ||
            _bossState == BossCombatState.P2_Preparing ||
            _bossState == BossCombatState.P2_Leap ||
            _bossState == BossCombatState.P2_RockSpikes) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (_bossState)
        {
            case BossCombatState.P1_Normal:
                CleanupMinionList();

                if (_summonTimer >= summonCooldown && _aliveMinions.Count < maxAliveMinions)
                {
                    ExecuteP1_Summon();
                }
                else if (distanceToPlayer <= 4.5f && _stompTimer >= stompCooldown) { ExecuteP1_Stomp(); }
                else if (distanceToPlayer >= chargeMinDistance &&
                         distanceToPlayer <= chargeMaxDistance &&
                         _chargeTimer >= chargeCooldown)
                {
                    StartSkillRoutine(ExecuteP1_Charge_Routine());
                }
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
        }
    }

    private void UpdateP2WanderLogic()
    {
        _wanderTimer -= Time.deltaTime;

        if (_wanderTimer <= 0)
        {
            Vector3 fromPlayer = FlatDir(transform.position - player.position);
            if (fromPlayer == Vector3.zero) fromPlayer = transform.forward;

            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 tangent = Vector3.Cross(Vector3.up, fromPlayer) * side;
            Vector3 randomDirection = player.position + fromPlayer * 7f + tangent * Random.Range(3f, 6f);
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

        float distance = Vector3.Distance(transform.position, player.position);
        List<int> availableAttacks = new List<int>();

        // 0 = Leap (xa), 1 = Rock Spikes (tầm trung), 2 = Frenzy (gần)
        if (distance >= 10f && _leapTimer >= leapCooldown) availableAttacks.Add(0);
        if (distance >= 4.5f && distance <= 14f && _rockSpikeTimer >= rockSpikesCooldown) availableAttacks.Add(1);
        if (distance <= 6f && _frenzyTimer >= frenzyCooldown) availableAttacks.Add(2);

        // Nếu chiêu phù hợp khoảng cách chưa hồi xong, cho phép dùng chiêu khác đã sẵn sàng.
        if (availableAttacks.Count == 0)
        {
            if (_leapTimer >= leapCooldown) availableAttacks.Add(0);
            if (_rockSpikeTimer >= rockSpikesCooldown) availableAttacks.Add(1);
            if (_frenzyTimer >= frenzyCooldown) availableAttacks.Add(2);
        }

        // Tránh lặp lại cùng một chiêu nếu còn lựa chọn khác.
        if (availableAttacks.Count > 1)
            availableAttacks.Remove(_lastP2Attack);

        if (availableAttacks.Count == 0) return;

        int randomIndex = availableAttacks[Random.Range(0, availableAttacks.Count)];

        _lastP2Attack = randomIndex;

        if (randomIndex == 0) StartSkillRoutine(ExecuteP2_Leap_Routine());
        else if (randomIndex == 1) StartSkillRoutine(ExecuteP2_RockSpikes_Routine());
        else if (randomIndex == 2) ExecuteP2_Frenzy();
    }

    private void ExecuteP1_Stomp()
    {
        _bossState = BossCombatState.P1_Stomp;
        _stateTimer = 0f;
        _stompTimer = 0f;
        StopAgentCompletely();
        agent.updatePosition = false;
        FacePlayer(true);
        anim.SetTrigger("StompTrigger");
    }

    private void ExecuteP1_Summon()
    {
        _bossState = BossCombatState.P1_Summon;
        _stateTimer = 0f;
        _summonTimer = 0f;
        StopAgentCompletely();
        anim.SetTrigger("SummonTrigger");
    }

    private IEnumerator ExecuteP1_Charge_Routine()
    {
        _bossState = BossCombatState.P1_Charge;
        _stateTimer = 0f;
        _chargeTimer = 0f;
        StopAgentCompletely();
        FacePlayer(true);

        // Attack_Charge có thể dùng làm đoạn lấy đà; lúc lao dùng dáng chạy nhanh.
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("ChargeTrigger");
        yield return new WaitForSeconds(chargeWindUp);

        if (_isDead || player == null) yield break;

        Vector3 chargeDirection = FlatDir(player.position - transform.position);
        bool hasHitPlayer = false;
        float elapsed = 0f;

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.updateRotation = false;
            agent.updatePosition = true;
        }

        anim.CrossFade("Locomotion", 0.1f);
        anim.SetFloat("Speed", 2f);

        while (elapsed < chargeDuration && !_isDead)
        {
            elapsed += Time.deltaTime;

            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.Move(chargeDirection * chargeSpeed * Time.deltaTime);

            if (!hasHitPlayer && player != null &&
                Vector3.Distance(transform.position, player.position) <= chargeHitRadius)
            {
                hasHitPlayer = true;
                player.GetComponent<HealthSystem>()?.TakeDamage(
                    attackDamage * chargeDamageMultiplier, gameObject);
            }

            yield return null;
        }

        StopAgentCompletely();
        anim.SetFloat("Speed", 0f);
        yield return new WaitForSeconds(skillRecoveryTime);
        ResetToNormalCombatState();
    }

    private IEnumerator ExecuteP2_RockSpikes_Routine()
    {
        _bossState = BossCombatState.P2_Preparing;
        _stateTimer = 0f;
        StopAgentCompletely();

        FacePlayer(true);
        anim.SetTrigger("StompTrigger");

        yield return new WaitForSeconds(1.0f);

        _bossState = BossCombatState.P2_RockSpikes;
        _rockSpikeTimer = 0f;

        if (spikeWarningPrefab != null)
        {
            Vector3 centerPos = roomCenter != null ? roomCenter.position : transform.position;
            int spawnedCount = 0;
            int attempts = 0;
            int maxAttempts = spikeCount * 8;
            List<Vector3> usedPositions = new List<Vector3>();

            while (spawnedCount < spikeCount && attempts < maxAttempts)
            {
                attempts++;
                Vector3 targetPos;

                // Khoảng 40% gai gây áp lực quanh player, phần còn lại phủ trong phòng.
                if (player != null && spawnedCount < Mathf.CeilToInt(spikeCount * 0.4f))
                {
                    Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(3.5f, 8f);
                    targetPos = player.position + new Vector3(circle.x, 0f, circle.y);
                }
                else
                {
                    float randomX = Random.Range(-roomSize.x / 2f, roomSize.x / 2f);
                    float randomZ = Random.Range(-roomSize.y / 2f, roomSize.y / 2f);
                    targetPos = centerPos + new Vector3(randomX, 0f, randomZ);
                }

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 4f, NavMesh.AllAreas))
                {
                    bool overlaps = false;
                    foreach (Vector3 used in usedPositions)
                    {
                        if (Vector3.Distance(used, hit.position) < 2.2f)
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (overlaps) continue;

                    Instantiate(spikeWarningPrefab, hit.position, Quaternion.identity);
                    usedPositions.Add(hit.position);
                    spawnedCount++;
                }
            }
        }

        yield return new WaitForSeconds(1.5f + skillRecoveryTime);
        ResetToNormalCombatState();
    }

    private IEnumerator ExecuteP2_Leap_Routine()
    {
        _bossState = BossCombatState.P2_Preparing;
        _stateTimer = 0f;
        StopAgentCompletely();

        yield return new WaitForSeconds(windUpTime);

        _bossState = BossCombatState.P2_Leap;
        _leapTimer = 0f;

        Vector3 desiredTarget = player.position;
        Vector3 toTarget = FlatDir(desiredTarget - transform.position);
        float leapDistance = Mathf.Min(Vector3.Distance(transform.position, desiredTarget), 14f);
        desiredTarget = transform.position + toTarget * leapDistance;

        NavMeshHit landingHit;
        Vector3 targetPosition = NavMesh.SamplePosition(
            desiredTarget, out landingHit, 4f, NavMesh.AllAreas)
            ? landingHit.position
            : transform.position;
        Vector3 startPosition = transform.position;
        agent.enabled = false;

        FacePlayer(true);
        anim.SetTrigger("LeapTrigger");

        yield return new WaitForSeconds(leapTakeoffDelay);

        float elapsedTime = 0f;
        while (elapsedTime < leapFlyDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / leapFlyDuration;

            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
            float height = Mathf.Sin(t * Mathf.PI) * leapMaxHeight;
            currentPos.y += height;

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetPosition;
        yield return new WaitForSeconds(skillRecoveryTime);
        ResetToNormalCombatState();
    }

    private void ExecuteP2_Frenzy()
    {
        _bossState = BossCombatState.P2_Frenzy;
        _stateTimer = 0f;
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

        // Không được bật cutscene Phase 2 nếu viên đạn này đã kết liễu boss.
        if (_isDead || healthSystem == null || healthSystem.IsDead) return;

        if (!_isPhase2 && _bossState != BossCombatState.Transition)
        {
            float hpPercent = (float)healthSystem.CurrentHP / healthSystem.MaxHP;
            if (hpPercent <= 0.5f && !cutsceneManager.IsCutsceneActive)
            {
                StartCoroutine(TriggerPhaseTransition());
            }
        }
    }

    private IEnumerator TriggerPhaseTransition()
    {
        _bossState = BossCombatState.Transition;
        _isPhase2 = true;

        // Lock combat
        StopAgentCompletely();

        // Start cutscene
        cutsceneManager.StartPhaseTransitionCutscene();

        if (audioCutsceneManager != null)
        {
            audioCutsceneManager.StartCutsceneAudio();
        }

        // Wait for cutscene to complete
        while (cutsceneManager.IsCutsceneActive)
        {
            yield return null;
        }

        // Cutscene has called ResetToNormalCombatState(), we're ready for phase 2 combat
        yield return null;
    }

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

        CleanupMinionList();
        int remainingSlots = Mathf.Max(0, maxAliveMinions - _aliveMinions.Count);

        foreach (Transform t in minionSpawnPoints)
        {
            if (remainingSlots <= 0) break;
            if (t == null) continue;

            GameObject minion = Instantiate(minionPrefab, t.position, t.rotation);
            _aliveMinions.Add(minion);
            remainingSlots--;
        }
    }

    public override void ResetToNormalCombatState()
    {
        if (_isDead) return;

        _bossState = _isPhase2 ? BossCombatState.P2_Normal : BossCombatState.P1_Normal;
        _stateTimer = 0f;

        if (!agent.enabled)
        {
            agent.enabled = true;
        }

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.Warp(transform.position);
            agent.isStopped = false;
        }

        anim.SetFloat("Speed", 0f);
    }

    private void StartSkillRoutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    private bool CanAnimationSkillTimeout()
    {
        return _bossState == BossCombatState.P1_Stomp ||
               _bossState == BossCombatState.P1_Summon ||
               _bossState == BossCombatState.P2_Frenzy;
    }

    private bool IsUsingSkill()
    {
        return _bossState != BossCombatState.None &&
               _bossState != BossCombatState.P1_Normal &&
               _bossState != BossCombatState.P2_Normal;
    }

    private void CleanupMinionList()
    {
        _aliveMinions.RemoveAll(minion =>
        {
            if (minion == null) return true;
            HealthSystem minionHealth = minion.GetComponent<HealthSystem>();
            return minionHealth != null && minionHealth.IsDead;
        });
    }

    private void OnDrawGizmosSelected()
    {
        if (roomCenter != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(roomCenter.position, new Vector3(roomSize.x, 1f, roomSize.y));
        }
    }
}