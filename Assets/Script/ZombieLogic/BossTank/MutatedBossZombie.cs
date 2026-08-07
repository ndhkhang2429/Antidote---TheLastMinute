using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI chiến đấu độc lập của Mutated Boss. Leap impact được khóa để không tạo decal hai lần.
/// Không phụ thuộc Timeline hoặc bất kỳ Cutscene Manager nào.
/// BossEncounterController chỉ cần gọi BeginEncounter() khi cutscene mở trận kết thúc.
/// </summary>
public class MutatedBossZombie : ZombieBase
{
    private enum BossState
    {
        Disabled,
        Phase1,
        Melee,
        Stomp,
        Summon,
        Charge,
        PhaseTransition,
        Phase2,
        RockSpikes,
        Leap,
        Frenzy
    }

    private enum Phase2Attack
    {
        None = -1,
        Leap = 0,
        RockSpikes = 1,
        Frenzy = 2
    }

    [Header("== ENCOUNTER ==")]
    [Tooltip("Chỉ bật để test boss mà không cần cutscene.")]
    [SerializeField] private bool startActiveForTesting = false;

    [Header("== PHASE VISUAL ==")]
    [SerializeField] private SkinnedMeshRenderer bossRenderer;
    [SerializeField] private Material phase1Material;
    [SerializeField] private Material phase2Material;
    [SerializeField] private float phase2Threshold = 0.5f;
    [SerializeField] private float phaseTransitionDuration = 2.5f;
    [SerializeField] private GameObject phase2TransitionVfx;
    [SerializeField] private BossPhaseTransitionController phaseTransitionController;

    [Header("== PHASE 1: STOMP ==")]
    [SerializeField] private float stompCooldown = 8f;
    [SerializeField] private float stompRange = 4.5f;
    [SerializeField] private float stompAnimationDuration = 2.2f;
    [SerializeField] private float stompTakeoffDelay = 0.2f;
    [SerializeField] private float stompJumpHeight = 2.8f;
    [SerializeField] private float stompJumpDuration = 0.9f;
    [SerializeField] private float stompRadius = 6f;
    [SerializeField] private float stompDamageMultiplier = 1.2f;

    [Header("== PHASE 1: CHARGE ==")]
    [SerializeField] private float chargeCooldown = 10f;
    [SerializeField] private float chargeMinDistance = 5f;
    [SerializeField] private float chargeMaxDistance = 12f;
    [SerializeField] private float chargeWindUp = 0.8f;
    [SerializeField] private float chargeDuration = 1.6f;
    [SerializeField] private float chargeSpeed = 11f;
    [SerializeField] private float chargeOvershootDistance = 1.5f;
    [SerializeField] private float chargeMaxTravelDistance = 14f;
    [SerializeField] private float chargeHitRadius = 1.8f;
    [SerializeField] private float chargeDamageMultiplier = 1.4f;

    [Header("== PHASE 1: MELEE ==")]
    [SerializeField] private float meleeRange = 3f;
    [SerializeField] private float meleeCooldown = 1.8f;
    [SerializeField] private float meleeAnimationDuration = 1.35f;
    [SerializeField] private float meleeHitDelay = 0.55f;
    [SerializeField] private float meleeDamageMultiplier = 1f;

    [Header("== PHASE 1: SUMMON ==")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private Transform[] minionSpawnPoints;
    [SerializeField] private int maxAliveMinions = 4;
    [SerializeField] private float summonCooldown = 20f;
    [SerializeField] private float summonAnimationDuration = 2.5f;
    [SerializeField] private float summonSpawnDelay = 1.2f;

    [Header("== PHASE 2: GENERAL ==")]
    [SerializeField] private float phase2MoveSpeedMultiplier = 1.15f;
    [SerializeField] private float phase2DecisionInterval = 3.5f;
    [SerializeField] private float skillRecoveryTime = 1f;
    [SerializeField] private float preferredPhase2Distance = 7f;
    [SerializeField] private float strafeDistance = 4f;

    [Header("== PHASE 2: ROCK SPIKES ==")]
    [SerializeField] private GameObject spikeWarningPrefab;
    [SerializeField] private Transform roomCenter;
    [SerializeField] private Vector2 roomSize = new Vector2(40f, 40f);
    [SerializeField] private int spikeCount = 20;
    [SerializeField] private float rockSpikesCooldown = 12f;
    [SerializeField] private float rockWindUp = 1f;
    [SerializeField] private float spikeWarningDuration = 1.5f;
    [SerializeField] private float minimumSpikeSpacing = 2.2f;
    [SerializeField] private float playerSafeRadius = 3f;

    [Header("== PHASE 2: LEAP ==")]
    [SerializeField] private float leapCooldown = 14f;
    [SerializeField] private float leapWindUp = 1.2f;
    [SerializeField] private float leapMaxDistance = 14f;
    [SerializeField] private float leapMaxHeight = 4f;
    [SerializeField] private float leapFlyDuration = 1f;
    [SerializeField] private float leapRadius = 8f;
    [SerializeField] private float leapDamageMultiplier = 1.5f;

    [Header("== PHASE 2: FRENZY ==")]
    [SerializeField] private float frenzyCooldown = 7f;
    [SerializeField] private float frenzyRange = 5.5f;
    [SerializeField] private float frenzyAnimationDuration = 2.4f;
    [SerializeField] private float frenzyHitRange = 3.2f;
    [SerializeField] private float frenzyHitMultiplier = 0.65f;

    [Header("== VFX ==")]
    [SerializeField] private GameObject stompVfxPrefab;
    [SerializeField] private GameObject leapVfxPrefab;
    [SerializeField] private GameObject groundCrackPrefab;

    private BossState _state = BossState.Disabled;
    private Phase2Attack _lastPhase2Attack = Phase2Attack.None;
    private bool _encounterActive;
    private bool _isPhase2;
    private bool _phaseTransitionStarted;
    private bool _phaseVisualsApplied;
    private bool _skillRunning;
    private bool _stompImpactTriggered;
    private bool _stompCanImpact;
    private bool _meleeImpactTriggered;
    private bool _leapImpactTriggered;

    private float _stompTimer;
    private float _meleeTimer;
    private float _chargeTimer;
    private float _summonTimer;
    private float _phase2DecisionTimer;
    private float _rockSpikeTimer;
    private float _leapTimer;
    private float _frenzyTimer;
    private float _strafeTimer;

    private readonly List<GameObject> _aliveMinions = new List<GameObject>();

    public bool EncounterActive => _encounterActive;
    public bool IsPhase2 => _isPhase2;
    public HealthSystem BossHealth => healthSystem;

    protected override void Start()
    {
        base.Start();

        if (bossRenderer == null)
            bossRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // Toàn bộ di chuyển do NavMeshAgent/script quản lý, tránh Root Motion đẩy boss hai lần.
        anim.applyRootMotion = false;

        ApplyMaterial(phase1Material);
        ResetCooldowns();

        // Súng trong project có thể trừ máu trực tiếp qua HealthSystem thay vì
        // gọi MutatedBossZombie.TakeDamage(), vì vậy phải theo dõi HP tại nguồn.
        if (healthSystem != null)
            healthSystem.OnDamaged += HandleBossHealthChanged;

        if (startActiveForTesting)
            BeginEncounter();
        else
            SetBossFrozen(true);
    }

    protected override void Update()
    {
        if (_isDead || player == null) return;

        if (!_encounterActive)
        {
            SetBossFrozen(true);
            return;
        }

        // Boss dùng state machine riêng, không chạy Behaviour Tree tuần tra của ZombieBase.
        _mode = ZombieMode.Combat;

        if (_skillRunning) return;

        TickCooldowns();

        if (_isPhase2)
            UpdatePhase2();
        else
            UpdatePhase1();
    }

    /// <summary>Được BossEncounterController gọi sau khi cutscene mở trận kết thúc.</summary>
    public void BeginEncounter()
    {
        if (_isDead || _encounterActive) return;

        _encounterActive = true;
        _state = _isPhase2 ? BossState.Phase2 : BossState.Phase1;
        _mode = ZombieMode.Combat;
        ForceAlert();
        SetBossFrozen(false);
        ResetCooldowns();
    }

    /// <summary>Dùng khi reset phòng, chuyển scene hoặc tạm khóa boss.</summary>
    public void PauseEncounter()
    {
        _encounterActive = false;
        _state = BossState.Disabled;
        SetBossFrozen(true);
    }

    private void UpdatePhase1()
    {
        float distance = FlatDistanceToPlayer();
        CleanupMinionList();

        if (_summonTimer >= summonCooldown && _aliveMinions.Count < maxAliveMinions)
        {
            StartCoroutine(SummonRoutine());
        }
        else if (distance <= stompRange && _stompTimer >= stompCooldown)
        {
            StartCoroutine(StompRoutine());
        }
        else if (distance <= meleeRange && _meleeTimer >= meleeCooldown)
        {
            StartCoroutine(MeleeRoutine());
        }
        else if (distance >= chargeMinDistance &&
                 distance <= chargeMaxDistance &&
                 _chargeTimer >= chargeCooldown)
        {
            StartCoroutine(ChargeRoutine());
        }
        else if (distance <= meleeRange)
        {
            // Đứng chờ rất ngắn trong lúc melee hồi, không chui vào collider Player.
            StopAgentSafely();
            FacePlayer();
            anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        }
        else
        {
            ChasePlayer(runSpeed, meleeRange * 0.85f);
        }
    }

    private void UpdatePhase2()
    {
        _phase2DecisionTimer += Time.deltaTime;

        if (_phase2DecisionTimer >= phase2DecisionInterval)
        {
            Phase2Attack selectedAttack = SelectPhase2Attack();
            if (selectedAttack != Phase2Attack.None)
            {
                _phase2DecisionTimer = 0f;
                _lastPhase2Attack = selectedAttack;

                if (selectedAttack == Phase2Attack.Leap)
                    StartCoroutine(LeapRoutine());
                else if (selectedAttack == Phase2Attack.RockSpikes)
                    StartCoroutine(RockSpikesRoutine());
                else
                    StartCoroutine(FrenzyRoutine());

                return;
            }
        }

        MoveAroundPlayer();
    }

    private Phase2Attack SelectPhase2Attack()
    {
        float distance = FlatDistanceToPlayer();
        List<Phase2Attack> available = new List<Phase2Attack>();

        if (distance >= 9f && _leapTimer >= leapCooldown)
            available.Add(Phase2Attack.Leap);

        if (distance >= 4.5f && distance <= 14f && _rockSpikeTimer >= rockSpikesCooldown)
            available.Add(Phase2Attack.RockSpikes);

        if (distance <= frenzyRange && _frenzyTimer >= frenzyCooldown)
            available.Add(Phase2Attack.Frenzy);

        // Nếu chiêu đúng khoảng cách chưa hồi, cho phép một chiêu khác đã sẵn sàng.
        if (available.Count == 0)
        {
            if (_leapTimer >= leapCooldown) available.Add(Phase2Attack.Leap);
            if (_rockSpikeTimer >= rockSpikesCooldown) available.Add(Phase2Attack.RockSpikes);
            if (_frenzyTimer >= frenzyCooldown) available.Add(Phase2Attack.Frenzy);
        }

        if (available.Count > 1)
            available.Remove(_lastPhase2Attack);

        if (available.Count == 0) return Phase2Attack.None;
        return available[Random.Range(0, available.Count)];
    }

    private IEnumerator MeleeRoutine()
    {
        BeginSkill(BossState.Melee);
        _meleeTimer = 0f;
        _meleeImpactTriggered = false;
        FacePlayer(true);
        anim.SetTrigger("MeleeTrigger");

        float hitDelay = Mathf.Clamp(meleeHitDelay, 0f, meleeAnimationDuration);
        yield return new WaitForSeconds(hitDelay);

        if (_isDead) yield break;
        Event_TriggerMeleeHit();

        float remainingAnimation = Mathf.Max(0f, meleeAnimationDuration - hitDelay);
        if (remainingAnimation > 0f)
            yield return new WaitForSeconds(remainingAnimation);

        yield return SkillRecovery();
        FinishSkill();
    }

    private IEnumerator StompRoutine()
    {
        BeginSkill(BossState.Stomp);
        _stompTimer = 0f;
        _stompImpactTriggered = false;
        _stompCanImpact = false;
        FacePlayer(true);
        anim.SetTrigger("StompTrigger");

        // Cho Animator đủ thời gian rời Locomotion trước khi script nâng boss lên.
        if (stompTakeoffDelay > 0f)
            yield return new WaitForSeconds(stompTakeoffDelay);

        if (_isDead) yield break;

        Vector3 groundPosition = transform.position;

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        float elapsed = 0f;
        float jumpDuration = Mathf.Max(0.1f, stompJumpDuration);
        while (elapsed < jumpDuration && !_isDead)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);
            transform.position = groundPosition +
                                 Vector3.up * (Mathf.Sin(t * Mathf.PI) * stompJumpHeight);
            yield return null;
        }

        if (_isDead) yield break;

        transform.position = groundPosition;
        EnableAndWarpAgent();
        _stompCanImpact = true;
        Event_TriggerStompShockwave();

        float remainingAnimation = Mathf.Max(
            0f,
            stompAnimationDuration - stompTakeoffDelay - jumpDuration);
        if (remainingAnimation > 0f)
            yield return new WaitForSeconds(remainingAnimation);

        yield return SkillRecovery();
        FinishSkill();
    }

    private IEnumerator SummonRoutine()
    {
        BeginSkill(BossState.Summon);
        _summonTimer = 0f;
        FacePlayer(true);
        anim.SetTrigger("SummonTrigger");

        float spawnDelay = Mathf.Clamp(summonSpawnDelay, 0f, summonAnimationDuration);
        yield return new WaitForSeconds(spawnDelay);

        if (_isDead) yield break;
        Event_TriggerSummonMinions();

        float remainingAnimation = Mathf.Max(0f, summonAnimationDuration - spawnDelay);
        if (remainingAnimation > 0f)
            yield return new WaitForSeconds(remainingAnimation);

        yield return SkillRecovery();
        FinishSkill();
    }

    private IEnumerator ChargeRoutine()
    {
        BeginSkill(BossState.Charge);
        _chargeTimer = 0f;
        FacePlayer(true);
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("ChargeTrigger");

        yield return new WaitForSeconds(chargeWindUp);
        if (_isDead || player == null) yield break;

        Vector3 chargeDirection = FlatDir(player.position - transform.position);
        float distanceToLockedTarget = FlatDistanceToPlayer();
        float targetTravelDistance = Mathf.Min(
            distanceToLockedTarget + chargeOvershootDistance,
            chargeMaxTravelDistance);
        bool damagedPlayer = false;
        float elapsed = 0f;
        float travelled = 0f;

        PrepareAgentForManualMovement();
        anim.CrossFade("Locomotion", 0.1f);
        anim.SetFloat("Speed", 2f);

        while (elapsed < chargeDuration && travelled < targetTravelDistance && !_isDead)
        {
            elapsed += Time.deltaTime;
            float remainingDistance = targetTravelDistance - travelled;
            float stepDistance = Mathf.Min(chargeSpeed * Time.deltaTime, remainingDistance);

            if (AgentReady())
            {
                Vector3 beforeMove = transform.position;
                agent.Move(chargeDirection * stepDistance);
                travelled += Vector3.Distance(beforeMove, transform.position);
            }
            else
            {
                break;
            }

            if (!damagedPlayer && FlatDistanceToPlayer() <= chargeHitRadius)
            {
                damagedPlayer = true;
                DamagePlayer(attackDamage * chargeDamageMultiplier);
            }

            yield return null;
        }

        StopAgentSafely();
        anim.SetFloat("Speed", 0f);
        yield return SkillRecovery();
        FinishSkill();
    }

    private IEnumerator RockSpikesRoutine()
    {
        BeginSkill(BossState.RockSpikes);
        _rockSpikeTimer = 0f;
        FacePlayer(true);
        anim.SetTrigger("StompTrigger");

        yield return new WaitForSeconds(rockWindUp);
        SpawnRockSpikeWarnings();
        yield return new WaitForSeconds(spikeWarningDuration);
        yield return SkillRecovery();
        FinishSkill();
    }

    private IEnumerator LeapRoutine()
    {
        BeginSkill(BossState.Leap);
        _leapTimer = 0f;
        _leapImpactTriggered = false;
        FacePlayer(true);
        anim.SetTrigger("LeapTrigger");

        yield return new WaitForSeconds(leapWindUp);
        if (_isDead || player == null) yield break;

        Vector3 startPosition = transform.position;
        Vector3 direction = FlatDir(player.position - startPosition);
        float distance = Mathf.Min(FlatDistanceToPlayer(), leapMaxDistance);
        Vector3 desiredLanding = startPosition + direction * distance;

        Vector3 landingPosition = startPosition;
        if (NavMesh.SamplePosition(desiredLanding, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            landingPosition = hit.position;

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        float elapsed = 0f;
        while (elapsed < leapFlyDuration && !_isDead)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / leapFlyDuration);
            Vector3 position = Vector3.Lerp(startPosition, landingPosition, t);
            position.y += Mathf.Sin(t * Mathf.PI) * leapMaxHeight;
            transform.position = position;
            yield return null;
        }

        if (_isDead) yield break;

        transform.position = landingPosition;
        EnableAndWarpAgent();
        TriggerLeapImpact(landingPosition);

        yield return SkillRecovery();
        FinishSkill();
    }

    private IEnumerator FrenzyRoutine()
    {
        BeginSkill(BossState.Frenzy);
        _frenzyTimer = 0f;
        FacePlayer(true);
        anim.SetTrigger("FrenzyTrigger");

        yield return new WaitForSeconds(frenzyAnimationDuration);
        yield return SkillRecovery();
        FinishSkill();
    }

    private IEnumerator PhaseTransitionRoutine()
    {
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("RoarTransition");

        // ZombieBase cũng phản ứng với event nhận damage; dừng lại lần nữa ở
        // frame kế tiếp để nó không kéo agent đi trong phase transition.
        yield return null;
        StopAgentSafely();

        yield return new WaitForSeconds(phaseTransitionDuration * 0.5f);
        ApplyPhase2Visuals();
        yield return new WaitForSeconds(phaseTransitionDuration * 0.5f);
        CompletePhase2Transition();
    }

    public override void TakeDamage(float damage, GameObject attacker = null)
    {
        if (_isDead || !_encounterActive || _state == BossState.PhaseTransition) return;

        base.TakeDamage(damage, attacker);
        if (_isDead || healthSystem == null || healthSystem.IsDead)
        {
            StopAllCoroutines();
            return;
        }

    }

    private void HandleBossHealthChanged(float currentHP, float maxHP)
    {
        if (currentHP <= 0f || maxHP <= 0f) return;
        if (!_encounterActive || _isPhase2 || _phaseTransitionStarted) return;

        if (currentHP / maxHP <= phase2Threshold)
        {
            Debug.Log($"[Boss] HP còn {currentHP / maxHP:P0}. Bắt đầu chuyển Phase 2.", this);
            BeginPhase2Transition();
        }
    }

    private void BeginPhase2Transition()
    {
        StopAllCoroutines();
        _phaseTransitionStarted = true;
        _phaseVisualsApplied = false;
        _skillRunning = true;
        _state = BossState.PhaseTransition;
        StopAgentSafely();
        anim.SetFloat("Speed", 0f);

        if (phaseTransitionController != null &&
            phaseTransitionController.PlayTransition(this))
        {
            return;
        }

        // Nếu Timeline chưa được gán thì vẫn chuyển phase bằng coroutine cũ.
        StartCoroutine(PhaseTransitionRoutine());
    }

    /// <summary>Controller gọi ở giữa Timeline để đổi material và bật VFX.</summary>
    public void ApplyPhase2Visuals()
    {
        if (_phaseVisualsApplied) return;
        _phaseVisualsApplied = true;

        ApplyMaterial(phase2Material);

        if (phase2TransitionVfx != null)
        {
            GameObject vfx = Instantiate(
                phase2TransitionVfx,
                transform.position,
                Quaternion.identity);
            Destroy(vfx, 5f);
        }
    }

    /// <summary>Controller gọi khi Timeline kết thúc để bắt đầu gameplay Phase 2.</summary>
    public void CompletePhase2Transition()
    {
        if (_isDead || _isPhase2) return;

        ApplyPhase2Visuals();
        _isPhase2 = true;
        Debug.Log("[Boss] Đã chuyển sang Phase 2.", this);
        _skillRunning = false;
        _state = BossState.Phase2;
        _phase2DecisionTimer = 0f;
        _rockSpikeTimer = rockSpikesCooldown;
        _leapTimer = leapCooldown;
        _frenzyTimer = frenzyCooldown;
        ResumeAgentSafely(runSpeed * phase2MoveSpeedMultiplier);
    }

    // Script tự gọi theo Melee Hit Delay; có thể giữ thêm Animation Event mà không gây double damage.
    public void Event_TriggerMeleeHit()
    {
        if (_state != BossState.Melee || _meleeImpactTriggered) return;
        _meleeImpactTriggered = true;

        if (FlatDistanceToPlayer() <= meleeRange * 1.15f)
            DamagePlayer(attackDamage * meleeDamageMultiplier);
    }

    // Animation Event: đặt đúng thời điểm chân boss chạm đất trong Attack_Stomp.
    public void Event_TriggerStompShockwave()
    {
        if (_state != BossState.Stomp || !_stompCanImpact || _stompImpactTriggered) return;
        _stompImpactTriggered = true;

        SpawnImpactEffects(stompVfxPrefab, 0.05f);

        if (FlatDistanceToPlayer() <= stompRadius)
            DamagePlayer(attackDamage * stompDamageMultiplier);
    }

    // Animation Event: đặt vào thời điểm boss hoàn tất động tác gọi minion.
    public void Event_TriggerSummonMinions()
    {
        if (minionPrefab == null)
        {
            Debug.LogWarning("[Boss] Chưa gán Minion Prefab.", this);
            return;
        }

        if (minionSpawnPoints == null || minionSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[Boss] Chưa gán Minion Spawn Points.", this);
            return;
        }

        CleanupMinionList();
        int slots = Mathf.Max(0, maxAliveMinions - _aliveMinions.Count);

        foreach (Transform spawnPoint in minionSpawnPoints)
        {
            if (slots <= 0) break;
            if (spawnPoint == null) continue;

            GameObject minion = Instantiate(minionPrefab, spawnPoint.position, spawnPoint.rotation);
            _aliveMinions.Add(minion);
            slots--;
        }

        Debug.Log($"[Boss] Summon hoàn tất. Minion đang sống: {_aliveMinions.Count}.", this);
    }

    // Giữ lại cho các clip cũ có Animation Event. Event giữa không trung sẽ bị bỏ qua.
    public void Event_TriggerLeapShockwave()
    {
        if (_state != BossState.Leap || _leapImpactTriggered)
            return;

        // Animation Event cũ có thể chạy khi boss còn đang bay. Chỉ chấp nhận
        // event khi chân boss đã rất gần bề mặt NavMesh.
        if (!NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit groundHit,
                1f,
                NavMesh.AllAreas))
        {
            return;
        }

        if (Mathf.Abs(transform.position.y - groundHit.position.y) > 0.35f)
            return;

        TriggerLeapImpact(groundHit.position);
    }

    private void TriggerLeapImpact(Vector3 groundPosition)
    {
        if (_state != BossState.Leap || _leapImpactTriggered)
            return;

        _leapImpactTriggered = true;
        SpawnImpactEffectsAt(leapVfxPrefab, groundPosition, 0.05f);

        if (FlatDistanceToPlayer() <= leapRadius)
            DamagePlayer(attackDamage * leapDamageMultiplier);
    }

    // Có thể đặt event này nhiều lần trong clip Frenzy, mỗi cú chỉ gây damage nhỏ.
    public void Event_TriggerFrenzyHit()
    {
        if (_state != BossState.Frenzy || FlatDistanceToPlayer() > frenzyHitRange) return;
        DamagePlayer(attackDamage * frenzyHitMultiplier);
    }

    /// <summary>
    /// Giữ lại để các clip cũ có event này không báo lỗi.
    /// State mới tự kết thúc bằng coroutine nên hàm không đổi state giữa chừng.
    /// </summary>
    public override void ResetToNormalCombatState()
    {
        if (!_skillRunning && _encounterActive)
            ResumeAgentSafely(_isPhase2 ? runSpeed * phase2MoveSpeedMultiplier : runSpeed);
    }

    private void BeginSkill(BossState state)
    {
        _skillRunning = true;
        _state = state;
        StopAgentSafely();
        anim.SetFloat("Speed", 0f);
    }

    private void FinishSkill()
    {
        if (_isDead) return;

        _skillRunning = false;
        _state = _isPhase2 ? BossState.Phase2 : BossState.Phase1;
        ResumeAgentSafely(_isPhase2 ? runSpeed * phase2MoveSpeedMultiplier : runSpeed);
    }

    private IEnumerator SkillRecovery()
    {
        yield return new WaitForSeconds(skillRecoveryTime);
    }

    private void ChasePlayer(float speed, float stoppingDistance)
    {
        if (!AgentReady()) return;

        agent.isStopped = false;
        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.SetDestination(player.position);
        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }

    private void MoveAroundPlayer()
    {
        if (!AgentReady()) return;

        _strafeTimer -= Time.deltaTime;
        if (_strafeTimer <= 0f)
        {
            Vector3 away = FlatDir(transform.position - player.position);
            if (away == Vector3.zero) away = -player.forward;

            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 tangent = Vector3.Cross(Vector3.up, away) * side;
            Vector3 desired = player.position +
                              away * preferredPhase2Distance +
                              tangent * strafeDistance;

            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);

            _strafeTimer = Random.Range(2f, 3.5f);
        }

        agent.isStopped = false;
        agent.speed = runSpeed * phase2MoveSpeedMultiplier;
        agent.stoppingDistance = 0f;
        agent.updatePosition = true;
        agent.updateRotation = true;
        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }

    private void SpawnRockSpikeWarnings()
    {
        if (spikeWarningPrefab == null) return;

        Vector3 center = roomCenter != null ? roomCenter.position : transform.position;
        List<Vector3> positions = new List<Vector3>();
        int attempts = 0;

        while (positions.Count < spikeCount && attempts < spikeCount * 10)
        {
            attempts++;
            Vector3 candidate;

            // 40% gai tạo áp lực quanh player nhưng luôn chừa vùng an toàn ban đầu.
            if (positions.Count < Mathf.CeilToInt(spikeCount * 0.4f))
            {
                Vector2 ring = Random.insideUnitCircle.normalized *
                               Random.Range(playerSafeRadius, 8f);
                candidate = player.position + new Vector3(ring.x, 0f, ring.y);
            }
            else
            {
                candidate = center + new Vector3(
                    Random.Range(-roomSize.x * 0.5f, roomSize.x * 0.5f),
                    0f,
                    Random.Range(-roomSize.y * 0.5f, roomSize.y * 0.5f));
            }

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                continue;

            bool tooClose = false;
            foreach (Vector3 used in positions)
            {
                if (Vector3.Distance(used, hit.position) < minimumSpikeSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;
            positions.Add(hit.position);
            Instantiate(spikeWarningPrefab, hit.position, Quaternion.identity);
        }
    }

    private void TickCooldowns()
    {
        _stompTimer += Time.deltaTime;
        _meleeTimer += Time.deltaTime;
        _chargeTimer += Time.deltaTime;
        _summonTimer += Time.deltaTime;

        if (_isPhase2)
        {
            _rockSpikeTimer += Time.deltaTime;
            _leapTimer += Time.deltaTime;
            _frenzyTimer += Time.deltaTime;
        }
    }

    private void ResetCooldowns()
    {
        _stompTimer = 2f;
        _meleeTimer = meleeCooldown;
        _chargeTimer = 0f;
        // Lần summon đầu diễn ra sớm để người chơi thấy rõ cơ chế Phase 1.
        _summonTimer = Mathf.Max(0f, summonCooldown - 5f);
        _phase2DecisionTimer = 0f;
        _rockSpikeTimer = 0f;
        _leapTimer = 0f;
        _frenzyTimer = 0f;
    }

    private void SetBossFrozen(bool frozen)
    {
        if (!AgentReady()) return;

        agent.isStopped = frozen;
        if (frozen)
        {
            agent.velocity = Vector3.zero;
            if (agent.hasPath) agent.ResetPath();
            anim.SetFloat("Speed", 0f);
        }
    }

    private void StopAgentSafely()
    {
        if (!AgentReady()) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updateRotation = false;
        if (agent.hasPath) agent.ResetPath();
    }

    private void ResumeAgentSafely(float speed)
    {
        if (!AgentReady()) return;
        agent.isStopped = false;
        agent.speed = speed;
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    private void PrepareAgentForManualMovement()
    {
        if (!AgentReady()) return;
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = false;
    }

    private void EnableAndWarpAgent()
    {
        if (agent == null) return;
        if (!agent.enabled) agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
            agent.isStopped = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }

    private bool AgentReady()
    {
        return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    }

    private float FlatDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        Vector3 delta = player.position - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    private void DamagePlayer(float damage)
    {
        if (player == null) return;
        player.GetComponent<HealthSystem>()?.TakeDamage(damage, gameObject);
    }

    private void SpawnImpactEffects(GameObject mainVfx, float crackHeight)
    {
        SpawnImpactEffectsAt(mainVfx, transform.position, crackHeight);
    }

    private void SpawnImpactEffectsAt(
        GameObject mainVfx,
        Vector3 groundPosition,
        float crackHeight)
    {
        if (mainVfx != null)
        {
            GameObject vfx = Instantiate(
                mainVfx,
                groundPosition,
                mainVfx.transform.rotation);
            Destroy(vfx, 4f);
        }

        if (groundCrackPrefab != null)
        {
            Vector3 position = groundPosition + Vector3.up * crackHeight;
            GameObject crack = Instantiate(
                groundCrackPrefab,
                position,
                Quaternion.Euler(-90f, 0f, 0f));
            Destroy(crack, 10f);
        }
    }

    private void ApplyMaterial(Material material)
    {
        if (bossRenderer != null && material != null)
            bossRenderer.sharedMaterial = material;
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
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawCube(roomCenter.position, new Vector3(roomSize.x, 0.5f, roomSize.y));
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stompRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, leapRadius);
    }
}