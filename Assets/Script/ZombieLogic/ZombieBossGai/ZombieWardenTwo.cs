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

    [Header("Rage Burst VFX")]
    [Tooltip("Prefab VFX shockwave khi Rage Burst (spawn tại vị trí zombie)")]
    public GameObject rageBurstVFXPrefab;

    [Tooltip("Thời gian tồn tại của VFX trước khi tự destroy (giây)")]
    public float rageBurstVFXLifetime = 3f;

    [Tooltip("Scale của VFX so với mặc định (2 = to gấp đôi)")]
    public float rageBurstVFXScale = 2.5f;
    [Header("Spike Projectile")]
    [Tooltip("Prefab viên spike (cần có SpikeProjectile script + Collider trigger + Rigidbody kinematic)")]
    public GameObject spikePrefab;

    [Tooltip("Các điểm spawn spike — mỗi chi sau lưng 1 Transform, bắn đồng loạt cùng lúc")]
    public Transform[] spikeSpawnPoints;

    [Tooltip("Damage mỗi viên spike")]
    public float spikeDamage = 15f;

    [Tooltip("Góc lệch ngẫu nhiên mỗi viên so với hướng thẳng vào player (độ) — tạo cảm giác tự nhiên")]
    public float spikeRandomAngle = 5f;

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
        _rageBurstTimer = rageBurstCooldown;
        _isEnraged = false;
        anim.applyRootMotion = false; // agent kiểm soát movement khi strafe
    }

    protected override void OnExitCombat()
    {
        _state = CombatState.Strafe;
        anim.applyRootMotion = false;
    }

    protected override void UpdateCombatBehaviour()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        CheckEnrage();

        float dist = Vector3.Distance(transform.position, player.position);

        // Tick spike cooldown luôn
        _spikeCooldownTimer += Time.deltaTime;

        // Rage cooldown chỉ tick khi KHÔNG đang trong rage sequence
        bool isRaging = _state == CombatState.RageBurstWindup
                     || _state == CombatState.RageBurstRelease
                     || _state == CombatState.RageBurstRecover;
        if (!isRaging)
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
        _rageBurstTimer = 0f; // reset cooldown ngay khi bắt đầu rage
        StopAgentCompletely();
        anim.applyRootMotion = true;
        anim.SetFloat("Speed", 0f);
        anim.SetTrigger("rage");

        // Spawn VFX ngay lúc bắt đầu rage animation
        if (rageBurstVFXPrefab != null)
        {
            GameObject vfx = Instantiate(
                rageBurstVFXPrefab,
                transform.position,
                Quaternion.identity
            );
            // Scale to lên theo inspector
            vfx.transform.localScale = Vector3.one * rageBurstVFXScale;
            Destroy(vfx, rageBurstVFXLifetime);
        }

        Debug.Log("[WardenII] RAGE BURST — Windup + VFX!");
    }

    private void HandleRageWindup()
    {
        StopAgentCompletely();
        FacePlayer();
        anim.SetFloat("Speed", 0f, 0.05f, Time.deltaTime);

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

        // Tại 60% animation → phát damage + knockback
        if (info.IsName("Rage") && info.normalizedTime >= 0.6f)
        {
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
        // Phát damage + knockback đúng 1 lần
        DoRageBurstExplosion();
        _state = CombatState.RageBurstRecover;
        _stateTimer = 0f;
    }

    private void HandleRageRecover()
    {
        StopAgentCompletely();
        anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        _stateTimer += Time.deltaTime;

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        bool rageDone = !info.IsName("Rage") || info.normalizedTime >= exitThreshold;

        if (rageDone && _stateTimer > 0.5f)
            ReturnToStrafe();
    }

    /// <summary>AoE damage + knockback player tại 60% animation rage.</summary>
    private void DoRageBurstExplosion()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, rageBurstRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            // Damage
            hit.GetComponent<HealthSystem>()?.TakeDamage(rageBurstDamage, gameObject);

            // Knockback — hỗ trợ cả Rigidbody lẫn CharacterController
            Vector3 dir = (hit.transform.position - transform.position).normalized;
            dir.y = 0.4f;
            dir = dir.normalized;

            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(dir * rageBurstForce, ForceMode.Impulse);
            }
            else
            {
                CharacterController cc = hit.GetComponent<CharacterController>();
                StartCoroutine(KnockbackCC(cc, dir));
            }

            Debug.Log($"[WardenII] Rage Burst hit! Damage={rageBurstDamage}");
            break;
        }
    }

    private System.Collections.IEnumerator KnockbackCC(CharacterController cc, Vector3 dir)
    {
        if (cc == null) yield break;
        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration);
            cc.Move(dir * rageBurstForce * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
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
            ReturnToStrafe();
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
        anim.applyRootMotion = true;  // bật root motion để animation phóng gai tự nhiên
        anim.SetTrigger(AnimStateToTrigger(stateName));
    }

    private void ReturnToStrafe()
    {
        anim.applyRootMotion = false; // tắt root motion → agent kiểm soát lại vị trí
        _state = CombatState.Strafe;
        _strafeTimer = 0f;
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

        bool isFarSpike = _waitingStateName == "Attack2LSpike";
        bool isAnySpike = _waitingStateName.Contains("Spike");

        if (isAnySpike)
        {
            // Spike attack → spawn projectile thay vì check range
            SpawnSpike(isFarSpike);
        }
        else
        {
            // Cận chiến thường
            if (dist <= attackRange * 1.4f)
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
    }

    /// <summary>
    /// Spawn spike từ TẤT CẢ spikeSpawnPoints cùng lúc — mỗi chi sau lưng bắn 1 viên.
    /// Mỗi viên hướng thẳng về player + lệch ngẫu nhiên spikeRandomAngle độ cho tự nhiên.
    /// </summary>
    private void SpawnSpike(bool isFarSpike)
    {
        if (spikePrefab == null)
        {
            Debug.LogWarning("[WardenII] spikePrefab chưa được gán!");
            return;
        }

        if (spikeSpawnPoints == null || spikeSpawnPoints.Length == 0)
        {
            // Fallback: bắn từ vị trí zombie nếu chưa gán spawn points
            Vector3 fallbackPos = transform.position + Vector3.up * 1.5f;
            Vector3 fallbackDir = (player.position + Vector3.up - fallbackPos).normalized;
            FireSpike(fallbackPos, fallbackDir);
            Debug.LogWarning("[WardenII] spikeSpawnPoints chưa gán, dùng fallback!");
            return;
        }

        // Bắn từ mỗi spawn point (mỗi chi) cùng lúc
        foreach (Transform spawnPoint in spikeSpawnPoints)
        {
            if (spawnPoint == null) continue;

            Vector3 spawnPos = spawnPoint.position;

            // Hướng cơ bản về phía ngực player
            Vector3 baseDir = (player.position + Vector3.up * 1f - spawnPos).normalized;

            // Thêm góc lệch ngẫu nhiên nhỏ — mỗi chi bắn hơi lệch nhau trông tự nhiên hơn
            float randomYaw = Random.Range(-spikeRandomAngle, spikeRandomAngle);
            float randomPitch = Random.Range(-spikeRandomAngle * 0.5f, spikeRandomAngle * 0.5f);
            Vector3 dir = Quaternion.Euler(randomPitch, randomYaw, 0f) * baseDir;

            FireSpike(spawnPos, dir);
        }
    }

    private void FireSpike(Vector3 spawnPos, Vector3 direction)
    {
        GameObject go = Instantiate(spikePrefab, spawnPos, Quaternion.identity);
        SpikeProjectile spike = go.GetComponent<SpikeProjectile>();
        if (spike != null)
            spike.Init(direction, spikeDamage, gameObject);
        else
            Debug.LogWarning("[WardenII] spikePrefab thiếu SpikeProjectile script!");
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