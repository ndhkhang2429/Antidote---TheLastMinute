using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ZombieRunner – Zombie 4 chân, nhanh, hung hãn.
///
/// Combat state machine (chạy bên trong UpdateCombatBehaviour):
///   Idle → CanPounce? → Pouncing
///        → InAttackRange? → Biting
///        → Chase (đuổi tốc độ cao)
///
/// Không override BT, không đụng đến ZombieBase internals.
/// </summary>
public class ZombieRunner : ZombieBase
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Runner Stats")]
    public float frenzySpeedMultiplier = 1.5f;
    public float frenzyThreshold = 0.3f;
    public float alertRadius = 15f;

    [Header("Scream")]
    public float runnerScreamDuration = 0.8f;   // Ngắn hơn zombie thường

    [Header("Pounce")]
    public float pounceMinRange = 4f;
    public float pounceMaxRange = 8f;
    public float pounceCooldown = 4f;
    public float pounceDamage = 25f;
    public float pounceSpeed = 15f;
    public float pounceDuration = 0.4f;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private bool _isFrenzy = false;
    private bool _isPouncing = false;
    private float _nextPounceTime = 0f;
    private float _originalRunSpeed;
    private float _originalAttackCooldown;

    // ── Init ─────────────────────────────────────────────────────────────────
    protected override void Start()
    {
        // Set stats TRƯỚC base.Start() để BT build đúng giá trị
        attackDamage = 10f;
        attackCooldown = 0.8f;
        walkSpeed = 1.5f;
        runSpeed = 6f;
        detectionRange = 15f;
        attackRange = 1.8f;
        screamDuration = runnerScreamDuration;

        base.Start();

        _originalRunSpeed = runSpeed;
        _originalAttackCooldown = attackCooldown;
    }

    // ── Update: thêm Frenzy check mỗi frame ──────────────────────────────────
    protected override void Update()
    {
        if (!_isDead) CheckFrenzy();
        base.Update();
    }

    // ── Hook: scream xong → alert đồng bọn ──────────────────────────────────
    protected override void OnEnterCombat()
    {
        AlertNearbyZombies();
    }

    private void AlertNearbyZombies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        int alerted = 0;
        foreach (Collider hit in hits)
        {
            ZombieBase z = hit.GetComponent<ZombieBase>()
                        ?? hit.GetComponentInParent<ZombieBase>();
            if (z != null && z != this)
            {
                z.ForceAlert();
                alerted++;
            }
        }
        if (alerted > 0)
            Debug.Log($"[ZombieRunner] Alert {alerted} zombie xung quanh!");
    }

    // ── Hook: mất player ────────────────────────────────────────────────────
    protected override void OnExitCombat()
    {
        // Nếu đang pounce mà mất player → không làm gì,
        // coroutine tự kết thúc và reset _isPouncing
    }

    // ── Combat State Machine ─────────────────────────────────────────────────
    protected override void UpdateCombatBehaviour()
    {
        if (_isPouncing) return;    // Coroutine đang chạy, không can thiệp

        float dist = Vector3.Distance(transform.position, player.position);

        // Ưu tiên 1: Pounce
        if (CanPounce(dist))
        {
            _isPouncing = true;
            _nextPounceTime = Time.time + pounceCooldown;
            StartCoroutine(PerformPounce());
            return;
        }

        // Ưu tiên 2: Bite (melee)
        if (dist <= attackRange)
        {
            StopAgentCompletely();
            anim.SetFloat("Speed", 0f, 0.15f, Time.deltaTime);
            FacePlayer();

            if (Time.time >= _nextAttackTime)
            {
                anim.SetTrigger("Attack");
                _nextAttackTime = Time.time + attackCooldown;
            }
            return;
        }

        // Ưu tiên 3: Chase
        ResumeAgent(runSpeed);
        agent.SetDestination(player.position);
        agent.stoppingDistance = attackRange;
        anim.SetFloat("Speed", 2f, 0.1f, Time.deltaTime);
    }

    // ── Frenzy ───────────────────────────────────────────────────────────────
    private void CheckFrenzy()
    {
        if (_isFrenzy || healthSystem == null) return;
        if (healthSystem.HPPercent >= frenzyThreshold) return;

        _isFrenzy = true;
        runSpeed = _originalRunSpeed * frenzySpeedMultiplier;
        attackCooldown = _originalAttackCooldown * 0.5f;
        pounceSpeed *= 1.3f;

        StartCoroutine(FrenzyVisualEffect());
        Debug.Log($"[ZombieRunner] {gameObject.name} FRENZY!");
    }

    private IEnumerator FrenzyVisualEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        while (_isFrenzy && !_isDead)
        {
            float pulse = (Mathf.Sin(Time.time * 10f) + 1f) / 2f;
            Color frenzyColor = Color.Lerp(Color.white, Color.red, pulse * 0.4f);
            foreach (var r in renderers)
                foreach (var mat in r.materials)
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", frenzyColor);
            yield return null;
        }
        // Reset màu khi chết
        foreach (var r in renderers)
            foreach (var mat in r.materials)
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.white);
    }

    // ── Pounce ───────────────────────────────────────────────────────────────
    private bool CanPounce(float dist)
    {
        if (_isPouncing) return false;
        if (Time.time < _nextPounceTime) return false;
        if (player == null) return false;
        return dist >= pounceMinRange && dist <= pounceMaxRange;
    }

    private IEnumerator PerformPounce()
    {
        // Dừng NavMesh hoàn toàn
        agent.isStopped = true;
        agent.enabled = false;

        // Snap hướng về player
        Vector3 dir = FlatDir(player.position - transform.position);
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        float elapsed = 0f;
        bool hitPlayer = false;

        // Lao thẳng về phía player
        while (elapsed < pounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pounceDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);   // ease-out

            transform.position = Vector3.Lerp(startPos, targetPos, eased);
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

            // Hit check
            if (!hitPlayer && Vector3.Distance(transform.position, player.position) < 1.5f)
            {
                hitPlayer = true;
                OnPounceHit();
            }

            yield return null;
        }

        // Snap lại NavMesh
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            transform.position = navHit.position;

        yield return new WaitForSeconds(0.1f);

        // Bật lại agent
        if (!_isDead && gameObject.activeInHierarchy)
        {
            agent.enabled = true;
            yield return null;  // chờ 1 frame để agent init
            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }

        _isPouncing = false;
        Debug.Log("[ZombieRunner] Pounce xong!");
    }

    private void OnPounceHit()
    {
        if (player == null) return;
        HealthSystem ph = player.GetComponent<HealthSystem>()
                       ?? player.GetComponentInParent<HealthSystem>();
        ph?.TakeDamage(pounceDamage, gameObject);
        anim.SetTrigger("Attack");
        Debug.Log($"[ZombieRunner] Pounce trúng! Damage: {pounceDamage}");
    }

    // ── Animation Event ──────────────────────────────────────────────────────
    public override void DealDamageToPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.2f) return;

        HealthSystem ph = player.GetComponent<HealthSystem>()
                       ?? player.GetComponentInParent<HealthSystem>();
        ph?.TakeDamage(attackDamage, gameObject);
        Debug.Log($"[ZombieRunner] Bite! Damage: {attackDamage}");
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pounceMinRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, pounceMaxRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}