using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie Runner: di chuyển 4 chân, nhanh, hung hãn.
/// Cơ chế:
/// - Patrol: đi chậm 4 chân
/// - Phát hiện player → Hét ngắn → Chase tốc độ cao
/// - Pounce: lao thẳng tốc độ cao khi cách 4-8m
/// - Bite Attack: cắn khi trong tầm gần
/// - Frenzy: tăng tốc khi HP < 30%
/// - Alert: kéo zombie xung quanh khi phát hiện player
/// </summary>
public class ZombieRunner : ZombieBase
{
    [Header("Runner Stats")]
    public float frenzySpeedMultiplier = 1.5f;
    public float frenzyThreshold = 0.3f;
    public float alertRadius = 15f;

    [Header("Scream Settings")]
    public float runnerScreamDuration = 0.8f; // Hét ngắn hơn Normal (0.8s)

    [Header("Pounce Attack")]
    public float pounceMinRange = 4f;    // Khoảng cách tối thiểu để Pounce
    public float pounceMaxRange = 8f;    // Khoảng cách tối đa để Pounce
    public float pounceCooldown = 4f;    // Cooldown Pounce
    public float pounceDamage = 25f;   // Damage khi Pounce trúng
    public float pounceSpeed = 15f;   // Tốc độ lao về phía player
    public float pounceDuration = 0.4f;  // Thời gian lao

    // ── Private ─────────────────────────────────────────────
    private bool _isFrenzy = false;
    private bool _isPouncing = false;
    private float _nextPounceTime = 0f;
    private float _originalRunSpeed;
    private float _originalAttackCooldown;

    // ────────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();

        attackDamage = 10f;
        attackCooldown = 0.8f;
        walkSpeed = 1.5f;
        runSpeed = 6f;
        detectionRange = 15f;
        attackRange = 1.8f;

        // Override scream duration ngắn hơn
        screamDuration = runnerScreamDuration;

        _originalRunSpeed = runSpeed;
        _originalAttackCooldown = attackCooldown;
    }

    // ── Update: check Frenzy ─────────────────────────────────

    protected override void Update()
    {
        if (!_isDead)
            CheckFrenzy();

        base.Update();
    }

    // ── Override BuildTree ───────────────────────────────────

    protected override Node BuildTree()
    {
        return new Selector(new List<Node>
        {
            // Nhánh 1: Thấy player
            new Sequence(new List<Node>
            {
                new ConditionNode(CanDetectPlayer),
                new ActionNode(Scream),  // Hét ngắn 0.8s
                new Selector(new List<Node>
                {
                    // Ưu tiên 1: Pounce nếu đủ điều kiện
                    new Sequence(new List<Node>
                    {
                        new ConditionNode(CanPounce),
                        new ActionNode(Pounce)
                    }),

                    // Ưu tiên 2: Bite Attack nếu trong tầm
                    new Sequence(new List<Node>
                    {
                        new ConditionNode(IsInAttackRange),
                        new ActionNode(Attack)
                    }),

                    // Ưu tiên 3: Chase tốc độ cao
                    new ActionNode(Chase)
                })
            }),

            // Nhánh 2: Patrol chậm
            new ActionNode(Patrol)
        });
    }

    // ── Alert đồng bọn khi Scream xong ──────────────────────

    protected override void OnScreamComplete()
    {
        AlertNearbyZombies();
    }

    private void AlertNearbyZombies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, alertRadius);
        int alerted = 0;

        foreach (Collider hit in hits)
        {
            ZombieBase zombie = hit.GetComponent<ZombieBase>()
                ?? hit.GetComponentInParent<ZombieBase>();

            if (zombie != null && zombie != this)
            {
                zombie.ForceAlert();
                alerted++;
            }
        }

        if (alerted > 0)
            Debug.Log($"[ZombieRunner] Alert {alerted} zombie xung quanh!");
    }

    // ── Frenzy ───────────────────────────────────────────────

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

    // ── Pounce ───────────────────────────────────────────────

    private bool CanPounce()
    {
        if (_isPouncing) return false;
        if (Time.time < _nextPounceTime) return false;
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        return dist >= pounceMinRange && dist <= pounceMaxRange;
    }

    private NodeState Pounce()
    {
        if (!_isPouncing)
        {
            _isPouncing = true;
            _nextPounceTime = Time.time + pounceCooldown;
            StartCoroutine(PerformPounce());
        }

        return NodeState.Running;
    }

    private IEnumerator PerformPounce()
    {
        // Dừng NavMesh
        agent.isStopped = true;
        agent.enabled = false;

        // Xoay mặt về player
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        float elapsed = 0f;
        bool hitPlayer = false;

        // Lao thẳng về phía player (không có arc)
        while (elapsed < pounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pounceDuration);

            // Lao thẳng, easing về cuối (ease out)
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            transform.position = Vector3.Lerp(startPos, targetPos, eased);

            // Xoay theo hướng lao
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            // Check hit player
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer < 1.5f && !hitPlayer)
            {
                hitPlayer = true;
                OnPounceHit();
            }

            yield return null;
        }

        // Snap xuống NavMesh
        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit navHit,
            3f, NavMesh.AllAreas))
        {
            transform.position = navHit.position;
        }

        yield return new WaitForSeconds(0.1f);

        // Bật lại NavMesh
        if (!_isDead && gameObject.activeInHierarchy)
        {
            agent.enabled = true;
            yield return null;
            if (agent.isOnNavMesh)
                agent.isStopped = false;
        }

        _isPouncing = false;
        Debug.Log("[ZombieRunner] Pounce xong!");
    }

    private void OnPounceHit()
    {
        if (player == null) return;

        // Gây damage
        HealthSystem playerHealth = player.GetComponent<HealthSystem>()
            ?? player.GetComponentInParent<HealthSystem>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(pounceDamage, gameObject);
            Debug.Log($"[ZombieRunner] Pounce trúng! Damage: {pounceDamage}");
        }

        // Trigger bite animation khi trúng
        anim.SetTrigger("Attack");
    }

    // ── Animation Event ──────────────────────────────────────

    /// <summary>
    /// Gọi từ Animation Event trong Bite Attack clip.
    /// </summary>
    public override void DealDamageToPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > attackRange * 1.2f)
            return;

        HealthSystem playerHealth = player.GetComponent<HealthSystem>()
            ?? player.GetComponentInParent<HealthSystem>();

        playerHealth?.TakeDamage(attackDamage, gameObject);
        Debug.Log($"[ZombieRunner] Bite! Damage: {attackDamage}");
    }

    // ── Gizmos ───────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Pounce range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pounceMinRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, pounceMaxRange);

        // Alert radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}