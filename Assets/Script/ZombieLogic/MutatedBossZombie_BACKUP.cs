using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MutatedBossZombie_BACKUP : ZombieBase
{
    protected enum BossCombatState
    {
        None,
        P1_Normal, P1_Stomp, P1_Summon,
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

    [Header("Phase 2 Settings")]
    [SerializeField] private float p2AttackInterval = 4f;
    [SerializeField] private float windUpTime = 1.5f;
    [SerializeField] private float leapCooldown = 15f;
    [SerializeField] private float frenzyCooldown = 5f;

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

    [Header("== CUTSCENE SETTINGS (CHUYỂN PHA) ==")]
    [SerializeField] private GameObject cutsceneCamera; // Kéo Camera phụ vào đây
    [Tooltip("Thời gian Boss nằm im giả chết trước khi bò dậy")]
    [SerializeField] private float fakeDeathDuration = 3.0f;
    [Tooltip("Thời gian của clip hoạt ảnh đứng dậy")]
    [SerializeField] private float standUpDuration = 2.0f;
    [SerializeField] private float standUpRotationDuration = 1.0f;
    [Tooltip("Thời gian của clip hoạt ảnh gầm thét lột xác")]
    [SerializeField] private float roarDuration = 3.0f;
    [Tooltip("Độ phóng to của Boss")]
    [SerializeField] private float bossScaleMultiplier = 1.3f;

    [Tooltip("Kéo Prefab DarkEffect hoặc PoisonEffect vào đây để tạo vụ nổ che mắt")]
    [SerializeField] private GameObject phase2RoarVfxPrefab;

    private BossCombatState _bossState = BossCombatState.None;
    private bool _isPhase2 = false;

    private float _stompTimer = 0f;
    private float _summonTimer = 0f;
    private float _p2DecisionTimer = 0f;
    private float _rockSpikeTimer = 0f;
    private float _leapTimer = 0f;
    private float _frenzyTimer = 0f;
    private float _wanderTimer = 0f;

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
        _p2DecisionTimer = p2AttackInterval;
        _rockSpikeTimer = rockSpikesCooldown;
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
        if (_bossState == BossCombatState.P2_Leap) return;

        base.Update();

        if (_isDead || player == null || _mode != ZombieMode.Combat || !ScreamDone) return;

        _stompTimer += Time.deltaTime;
        _summonTimer += Time.deltaTime;

        if (_isPhase2)
        {
            _p2DecisionTimer += Time.deltaTime;
            _rockSpikeTimer += Time.deltaTime;
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
        if (_bossState == BossCombatState.Transition ||
            _bossState == BossCombatState.P2_Preparing ||
            _bossState == BossCombatState.P2_Leap ||
            _bossState == BossCombatState.P2_RockSpikes) return;

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
        if (_rockSpikeTimer >= rockSpikesCooldown) availableAttacks.Add(1);
        if (_frenzyTimer >= frenzyCooldown) availableAttacks.Add(2);

        if (availableAttacks.Count == 0) return;

        int randomIndex = availableAttacks[Random.Range(0, availableAttacks.Count)];

        if (randomIndex == 0) StartCoroutine(ExecuteP2_Leap_Routine());
        else if (randomIndex == 1) StartCoroutine(ExecuteP2_RockSpikes_Routine());
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

    private IEnumerator ExecuteP2_RockSpikes_Routine()
    {
        _bossState = BossCombatState.P2_Preparing;
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
            int maxAttempts = 100;

            while (spawnedCount < spikeCount && attempts < maxAttempts)
            {
                attempts++;
                float randomX = Random.Range(-roomSize.x / 2f, roomSize.x / 2f);
                float randomZ = Random.Range(-roomSize.y / 2f, roomSize.y / 2f);
                Vector3 targetPos = centerPos + new Vector3(randomX, 0, randomZ);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 4f, NavMesh.AllAreas))
                {
                    Instantiate(spikeWarningPrefab, hit.position, Quaternion.identity);
                    spawnedCount++;
                }
            }
        }

        yield return new WaitForSeconds(1.5f);
        ResetToNormalCombatState();
    }

    private IEnumerator ExecuteP2_Leap_Routine()
    {
        _bossState = BossCombatState.P2_Preparing;
        StopAgentCompletely();

        yield return new WaitForSeconds(windUpTime);

        _bossState = BossCombatState.P2_Leap;
        _leapTimer = 0f;

        Vector3 targetPosition = player.position;
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
        ResetToNormalCombatState();
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
        agent.enabled = false;

        // 1. BOSS GIẢ CHẾT
        anim.SetTrigger("DieTrigger");
        yield return new WaitForSeconds(fakeDeathDuration);

        if (cutsceneCamera != null) cutsceneCamera.SetActive(true);

        // 2. BOSS ĐỨNG DẬY
        anim.SetTrigger("StandUpTrigger");

        float rT = 0;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0f, 180f, 0f);

        while (rT < 1.0f)
        {
            rT += Time.deltaTime / standUpRotationDuration;
            transform.rotation = Quaternion.Lerp(startRot, targetRot, rT);
            yield return null;
        }

        float remainStand = standUpDuration - standUpRotationDuration;
        if (remainStand > 0) yield return new WaitForSeconds(remainStand);

        // 3. GẦM THÉT & BÙNG NỔ VFX LỘT XÁC
        anim.SetTrigger("RoarTransition");

        if (phase2RoarVfxPrefab != null)
        {
            Vector3 vfxPos = transform.position + Vector3.up * 1.5f; // Đẩy hiệu ứng lên cao giữa ngực
            GameObject roarVfx = Instantiate(phase2RoarVfxPrefab, vfxPos, Quaternion.identity);
            Destroy(roarVfx, 5f);
        }

        // Chờ 0.3s để khói bùng lên che khuất rồi mới lột da
        yield return new WaitForSeconds(0.3f);

        if (_skmr != null && materialV3 != null)
        {
            _skmr.sharedMaterial = materialV3;
        }

        // Quá trình phình to
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * bossScaleMultiplier;
        float t = 0;
        float scaleDuration = 1.5f;

        while (t < 1.0f)
        {
            t += Time.deltaTime / scaleDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        float remainingRoarTime = roarDuration - 0.3f - scaleDuration;
        if (remainingRoarTime > 0) yield return new WaitForSeconds(remainingRoarTime);

        // 4. KẾT THÚC CUTSCENE
        if (cutsceneCamera != null) cutsceneCamera.SetActive(false);

        if (!agent.enabled) agent.enabled = true;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
        }

        anim.speed = 1.25f;
        _bossState = BossCombatState.P2_Normal;
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

        foreach (Transform t in minionSpawnPoints)
        {
            Instantiate(minionPrefab, t.position, t.rotation);
        }
    }

    public override void ResetToNormalCombatState()
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

    private void OnDrawGizmosSelected()
    {
        if (roomCenter != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(roomCenter.position, new Vector3(roomSize.x, 1f, roomSize.y));
        }
    }
}