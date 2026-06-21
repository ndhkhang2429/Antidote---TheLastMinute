using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ZombieWardenFinal — "THE APEX" — Boss cuối DEAD ROOF
///
/// Phase 1 (HP 100→50%, Skin 3):
///   FAR  (>7m) : attack2LSpike + attack2RLSpike luân phiên
///   MID  (3-7m): strafe nhẹ + attack1LSpike / attack1RSpike
///   CLOSE(<3m) : jump slam + attack4 punish
///   Cooldown: 2.5s
///
/// Chuyển dạng (HP chạm 50%):
///   Dừng → rage animation → swap mesh skin3→skin4 → VFX → Phase 2
///
/// Phase 2 (HP 50→0%, Skin 4):
///   Tất cả skill Phase 1 + combo cận chiến dồn dập
///   Cooldown: 1.2s
///   Summon zombie mỗi 20s
///   Jump slam có thêm AoE
/// </summary>
public class ZombieWardenFinal : ZombieBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Warden Final — Phase Settings")]
    [Tooltip("HP % để kích hoạt chuyển dạng Phase 2")]
    [Range(0f, 1f)]
    public float phase2Threshold = 0.5f;

    [Tooltip("Cooldown giữa các đòn Phase 1 (giây)")]
    public float phase1Cooldown = 2.5f;

    [Tooltip("Cooldown giữa các đòn Phase 2 (giây)")]
    public float phase2Cooldown = 1.2f;

    [Tooltip("normalizedTime coi là animation xong")]
    [Range(0.5f, 1f)]
    public float exitThreshold = 0.85f;

    [Header("Zone Ranges")]
    public float farRange = 7f;
    public float closeRange = 3f;

    [Header("Phase 1 — Spike")]
    public Transform[] spikeSpawnPoints;
    public GameObject spikePrefab;
    public float spikeDamage = 20f;
    public float spikeRandomAngle = 5f;

    [Header("Phase 2 — Jump Slam")]
    [Tooltip("Damage AoE khi đáp xuống ở Phase 2")]
    public float jumpSlamAoEDamage = 30f;
    [Tooltip("Bán kính AoE jump slam Phase 2")]
    public float jumpSlamAoERadius = 3f;

    [Header("Phase 2 — Summon")]
    [Tooltip("Prefab zombie thường để summon")]
    public GameObject zombieSummonPrefab;
    [Tooltip("Các điểm spawn zombie quanh phòng boss")]
    public Transform[] summonSpawnPoints;
    [Tooltip("Số zombie mỗi lần summon")]
    public int summonCount = 3;
    [Tooltip("Thời gian giữa các lần summon (giây)")]
    public float summonInterval = 20f;

    [Header("Phase Transition")]
    [Tooltip("Mesh Renderer của skin Phase 1 (skin 3)")]
    public GameObject skin3Object;
    [Tooltip("Mesh Renderer của skin Phase 2 (skin 4)")]
    public GameObject skin4Object;
    [Tooltip("VFX chuyển dạng")]
    public GameObject transformVFXPrefab;
    [Tooltip("Thời gian VFX tồn tại")]
    public float transformVFXLifetime = 3f;

    // ── State Machine ─────────────────────────────────────────────────────────
    private enum CombatState
    {
        Approach,
        Strafe,
        WaitingEnterAnim,
        WaitingFinishAnim,
        Transforming,       // đang chuyển dạng
        Cooldown,
    }

    private enum BossPhase { Phase1, Phase2 }

    private CombatState _state = CombatState.Approach;
    private BossPhase _phase = BossPhase.Phase1;

    // Tracking
    private string _waitingStateName = "";
    private float _stateTimer = 0f;
    private float _cooldownTimer = 0f;
    private float _summonTimer = 0f;
    private bool _hasTransformed = false;
    private bool _hitDealtThisSwing = false;
    private bool _isJumpSlam = false; // để biết swing hiện tại là jump slam
    private bool _isSummoning = false;

    // Strafe Phase 1
    private float _strafeTimer = 0f;
    private float _strafeDuration = 1.0f;
    private int _strafeDir = 1;

    // Spike alternation Phase 1
    private bool _lastWasFarLeft = false; // xen kẽ attack2LSpike / attack2RLSpike

    // Blood FX
    private ZombieBloodFXHandler _bloodFX;

    // ── Start ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        _bloodFX = GetComponent<ZombieBloodFXHandler>();

        // Bắt đầu với skin 3, ẩn skin 4
        if (skin3Object != null) skin3Object.SetActive(true);
        if (skin4Object != null) skin4Object.SetActive(false);
    }

    // ── Overrides ─────────────────────────────────────────────────────────────
    protected override void OnEnterCombat()
    {
        _state = CombatState.Approach;
        _phase = BossPhase.Phase1;
        _hasTransformed = false;
        _summonTimer = 0f;
        _cooldownTimer = 0f;
    }

    protected override void OnExitCombat()
    {
        _state = CombatState.Approach;
        anim.applyRootMotion = false;
    }

    protected override void UpdateCombatBehaviour()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Kiểm tra chuyển dạng
        CheckPhaseTransition();

        // Tick summon timer Phase 2
        if (_phase == BossPhase.Phase2 && _state != CombatState.Transforming)
            _summonTimer += Time.deltaTime;

        switch (_state)
        {
            case CombatState.Approach: HandleApproach(dist); break;
            case CombatState.Strafe: HandleStrafe(dist); break;
            case CombatState.WaitingEnterAnim: HandleWaitingEnter(); break;
            case CombatState.WaitingFinishAnim: HandleWaitingFinish(dist); break;
            case CombatState.Transforming:      /* coroutine tự xử lý */       break;
            case CombatState.Cooldown: HandleCooldown(dist); break;
        }
    }

    // ── Phase 1 Handlers ──────────────────────────────────────────────────────

    private void HandleApproach(float dist)
    {
        // Summon check Phase 2
        if (TryStartSummon()) return;

        if (dist > farRange)
        {
            // Tiến lại gần hơn trước khi bắn
            ResumeAgent(runSpeed);
            agent.stoppingDistance = farRange - 1f;
            agent.SetDestination(player.position);
            anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);

            // Vẫn bắn spike khi đang tiến
            if (dist <= farRange + 3f)
                PickAndTriggerAttack(dist);
        }
        else
        {
            _state = CombatState.Strafe;
            _strafeTimer = 0f;
        }
    }

    private void HandleStrafe(float dist)
    {
        // Summon check Phase 2
        if (TryStartSummon()) return;

        // CLOSE: jump slam
        if (dist < closeRange)
        {
            TriggerAttack("Jump");
            _isJumpSlam = true;
            return;
        }

        // Strafe ngang
        DoStrafe();

        // Bắn theo zone
        PickAndTriggerAttack(dist);
    }

    private void DoStrafe()
    {
        _strafeTimer += Time.deltaTime;
        if (_strafeTimer >= _strafeDuration)
        {
            _strafeTimer = 0f;
            _strafeDir = -_strafeDir;
        }

        Vector3 toPlayer = (player.position - transform.position).normalized;
        toPlayer.y = 0f;
        Vector3 strafeDir = Vector3.Cross(Vector3.up, toPlayer) * _strafeDir;

        float speed = _phase == BossPhase.Phase2 ? runSpeed * 1.2f : runSpeed * 0.7f;
        agent.isStopped = false;
        agent.updateRotation = false;
        agent.velocity = strafeDir * speed;

        FacePlayer();
        anim.SetFloat("Speed", 1.5f, 0.15f, Time.deltaTime);
    }

    /// <summary>Chọn attack phù hợp theo zone và phase.</summary>
    private void PickAndTriggerAttack(float dist)
    {
        if (_state == CombatState.WaitingEnterAnim ||
            _state == CombatState.WaitingFinishAnim) return;

        float cd = _phase == BossPhase.Phase2 ? phase2Cooldown : phase1Cooldown;
        _cooldownTimer += Time.deltaTime;
        if (_cooldownTimer < cd) return;

        _cooldownTimer = 0f;
        _isJumpSlam = false;

        if (dist > farRange)
        {
            // FAR: xen kẽ attack2LSpike / attack2RLSpike
            string farAttack = _lastWasFarLeft ? "Attack2RLSpike" : "Attack2LSpike";
            _lastWasFarLeft = !_lastWasFarLeft;
            TriggerAttack(farAttack);
        }
        else if (dist <= farRange && dist > closeRange)
        {
            // MID
            if (_phase == BossPhase.Phase2)
            {
                // Phase 2: thêm combo cận chiến
                float r = Random.value;
                if (r < 0.3f) TriggerAttack("Attack1");
                else if (r < 0.5f) TriggerAttack("Attack1LSpike");
                else if (r < 0.7f) TriggerAttack("Attack1RSpike");
                else TriggerAttack("Attack3RSpike");
            }
            else
            {
                // Phase 1: spike bên sườn
                TriggerAttack(Random.value > 0.5f ? "Attack1LSpike" : "Attack1RSpike");
            }
        }
        else
        {
            // CLOSE: attack4 punish
            TriggerAttack("Attack4");
        }
    }

    private void HandleCooldown(float dist)
    {
        float cd = _phase == BossPhase.Phase2 ? phase2Cooldown : phase1Cooldown;
        _cooldownTimer += Time.deltaTime;

        // Tiến lại gần player trong lúc chờ
        ResumeAgent(_phase == BossPhase.Phase2 ? runSpeed * 1.2f : runSpeed * 0.8f);
        agent.stoppingDistance = closeRange;
        agent.SetDestination(player.position);
        anim.SetFloat("Speed", 1.5f, 0.15f, Time.deltaTime);

        if (_cooldownTimer >= cd)
        {
            _cooldownTimer = 0f;
            _state = dist > farRange ? CombatState.Approach : CombatState.Strafe;
        }
    }

    // ── Anim Polling ──────────────────────────────────────────────────────────

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

        _stateTimer += Time.deltaTime;
        if (_stateTimer > 0.5f)
        {
            anim.SetTrigger(StateToTrigger(_waitingStateName));
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

        if (!_hitDealtThisSwing && t >= 0.5f)
        {
            DealHitDamage(dist);
            _hitDealtThisSwing = true;
        }

        if (t >= exitThreshold)
        {
            anim.applyRootMotion = false;
            _isSummoning = false;
            _state = CombatState.Cooldown;
            _cooldownTimer = 0f;
        }
    }

    // ── Phase Transition ──────────────────────────────────────────────────────

    private void CheckPhaseTransition()
    {
        if (_hasTransformed || healthSystem == null) return;
        if (_state == CombatState.Transforming) return;

        float ratio = healthSystem.CurrentHP / healthSystem.MaxHP;
        if (ratio <= phase2Threshold)
        {
            _hasTransformed = true; // set ngay lập tức trước khi start coroutine
            _state = CombatState.Transforming; // block mọi check tiếp theo
            StartCoroutine(DoPhaseTransition());
        }
    }

    private IEnumerator DoPhaseTransition()
    {
        StopAgentCompletely();
        anim.applyRootMotion = true;
        anim.SetTrigger("rage");
        Debug.Log("[WardenFinal] PHASE TRANSITION — bắt đầu chuyển dạng!");

        // Chờ 40% animation rage
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            return info.IsName("Rage") && info.normalizedTime >= 0.4f;
        });

        // Spawn VFX chuyển dạng
        if (transformVFXPrefab != null)
        {
            GameObject vfx = Instantiate(transformVFXPrefab,
                transform.position, Quaternion.identity);
            Destroy(vfx, transformVFXLifetime);
        }

        // Swap mesh: ẩn skin3, hiện skin4
        if (skin3Object != null) skin3Object.SetActive(false);
        if (skin4Object != null) skin4Object.SetActive(true);

        Debug.Log("[WardenFinal] Skin đã swap sang Phase 2!");

        // Chờ animation rage xong
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            return !info.IsName("Rage") || info.normalizedTime >= exitThreshold;
        });

        // Kích hoạt Phase 2
        _phase = BossPhase.Phase2;
        _summonTimer = 0f;
        anim.applyRootMotion = false;
        _state = CombatState.Strafe;

        Debug.Log("[WardenFinal] PHASE 2 — The Apex awakens!");
    }

    // ── Summon ────────────────────────────────────────────────────────────────

    private bool TryStartSummon()
    {
        if (_phase != BossPhase.Phase2) return false;
        if (_isSummoning) return false;
        if (zombieSummonPrefab == null) return false;
        if (summonSpawnPoints == null || summonSpawnPoints.Length == 0) return false;
        if (_summonTimer < summonInterval) return false;

        _summonTimer = 0f;
        _isSummoning = true;
        StartCoroutine(DoSummon());
        return false; // không block combat, chỉ trigger summon
    }

    private IEnumerator DoSummon()
    {
        // Trigger rage animation ngắn (tái dụng)
        StopAgentCompletely();
        anim.applyRootMotion = true;
        anim.SetTrigger("rage");

        Debug.Log("[WardenFinal] SUMMON — gọi quân!");

        // Chờ 50% animation
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            return info.IsName("Rage") && info.normalizedTime >= 0.5f;
        });

        // Spawn zombie tại các điểm ngẫu nhiên
        List<Transform> availablePoints = new List<Transform>(summonSpawnPoints);
        int spawnCount = Mathf.Min(summonCount, availablePoints.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            int idx = Random.Range(0, availablePoints.Count);
            Transform point = availablePoints[idx];
            availablePoints.RemoveAt(idx);

            Instantiate(zombieSummonPrefab, point.position, point.rotation);
            Debug.Log($"[WardenFinal] Spawned zombie tại {point.name}");
        }

        // Chờ animation xong
        yield return new WaitUntil(() =>
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            return !info.IsName("Rage") || info.normalizedTime >= exitThreshold;
        });

        anim.applyRootMotion = false;
        _isSummoning = false;
        _state = CombatState.Strafe;
    }

    // ── Damage ────────────────────────────────────────────────────────────────

    private void DealHitDamage(float dist)
    {
        if (player == null) return;

        bool isSpike = _waitingStateName.Contains("Spike");

        if (isSpike)
        {
            SpawnSpike();
        }
        else if (_isJumpSlam || _waitingStateName == "Jump")
        {
            DoJumpSlamDamage();
        }
        else
        {
            // Cận chiến thường
            if (dist <= attackRange * 1.4f)
            {
                float dmg = attackDamage * (_phase == BossPhase.Phase2 ? 1.5f : 1f);
                player.GetComponent<HealthSystem>()?.TakeDamage(dmg, gameObject);

                if (_bloodFX != null)
                {
                    Vector3 hp = player.position + Vector3.up;
                    Vector3 hn = (player.position - transform.position).normalized;
                    _bloodFX.OnHitMelee(hp, hn);
                }
            }
        }
    }

    private void DoJumpSlamDamage()
    {
        float radius = _phase == BossPhase.Phase2 ? jumpSlamAoERadius : attackRange;
        float dmg = _phase == BossPhase.Phase2 ? jumpSlamAoEDamage : attackDamage;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            hit.GetComponent<HealthSystem>()?.TakeDamage(dmg, gameObject);
            Debug.Log($"[WardenFinal] Jump Slam hit! Damage={dmg}");
            break;
        }
    }

    private void SpawnSpike()
    {
        if (spikePrefab == null || spikeSpawnPoints == null) return;

        foreach (Transform sp in spikeSpawnPoints)
        {
            if (sp == null) continue;
            Vector3 dir = (player.position + Vector3.up - sp.position).normalized;
            float rY = Random.Range(-spikeRandomAngle, spikeRandomAngle);
            float rP = Random.Range(-spikeRandomAngle * 0.5f, spikeRandomAngle * 0.5f);
            dir = Quaternion.Euler(rP, rY, 0f) * dir;

            GameObject go = Instantiate(spikePrefab, sp.position, Quaternion.identity);
            SpikeProjectile s = go.GetComponent<SpikeProjectile>();
            if (s != null) s.Init(dir, spikeDamage, gameObject);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void TriggerAttack(string stateName)
    {
        _waitingStateName = stateName;
        _stateTimer = 0f;
        _state = CombatState.WaitingEnterAnim;
        StopAgentCompletely();
        anim.applyRootMotion = true;
        anim.SetTrigger(StateToTrigger(stateName));
    }

    private string StateToTrigger(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return stateName;
        return char.ToLower(stateName[0]) + stateName.Substring(1);
    }

    private void CheckPhase2SpeedBoost()
    {
        if (_phase == BossPhase.Phase2)
            runSpeed = Mathf.Min(runSpeed, 5f); // cap tốc độ Phase 2
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
        Gizmos.DrawWireSphere(transform.position, jumpSlamAoERadius);
    }
}