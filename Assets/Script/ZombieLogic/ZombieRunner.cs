using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie Runner: nhanh, gầy, tấn công liên tục.
/// Cơ chế đặc biệt:
/// - Leap Attack: nhảy vồ khi cách player 4-6m
/// - Frenzy: tăng tốc khi HP < 30%
/// - Alert: hét kéo zombie xung quanh khi phát hiện player
/// </summary>
public class ZombieRunner : ZombieBase
{
    [Header("Runner Stats")]
    public float frenzySpeedMultiplier = 1.5f;  // Tăng tốc khi Frenzy
    public float frenzyThreshold = 0.3f;         // HP < 30% → Frenzy
    public float alertRadius = 15f;              // Bán kính alert đồng bọn

    [Header("Leap Attack")]
    public float leapMinRange = 4f;              // Khoảng cách tối thiểu để nhảy
    public float leapMaxRange = 8f;              // Khoảng cách tối đa để nhảy
    public float leapCooldown = 5f;              // Cooldown nhảy vồ
    public float leapDamage = 30f;               // Damage khi nhảy vồ
    public float leapForce = 8f;                 // Lực nhảy
    public float leapKnockback = 5f;             // Lực đẩy player khi nhảy trúng

    // ── Private ─────────────────────────────────────────────
    private bool _isFrenzy = false;
    private bool _isLeaping = false;
    private float _nextLeapTime = 0f;
    private float _originalRunSpeed;

    // ────────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();

        // Stats Runner: nhanh, yếu, đánh liên tục
        attackDamage = 12f;
        attackCooldown = 0.8f;   // Đánh nhanh hơn Normal
        walkSpeed = 2f;     // Đi nhanh hơn
        runSpeed = 6f;     // Chạy rất nhanh
        detectionRange = 15f;    // Nhạy cảm hơn
        attackRange = 1.8f;

        _originalRunSpeed = runSpeed;
    }

    // ── Override Update để check Frenzy ─────────────────────

    protected override void Update()
    {
        if (!_isDead)
            CheckFrenzy();

        base.Update(); // Gọi BT tick từ ZombieBase
    }

    // ── Override BuildTree → thêm nhánh Leap ────────────────

    protected override Node BuildTree()
    {
        return new Selector(new List<Node>
        {
            // Nhánh 1: Thấy player
            new Sequence(new List<Node>
            {
                new ConditionNode(CanDetectPlayer),
                new ActionNode(Scream),
                new Selector(new List<Node>
                {
                    // Ưu tiên 1: Leap Attack nếu đủ điều kiện
                    new Sequence(new List<Node>
                    {
                        new ConditionNode(CanLeap),
                        new ActionNode(LeapAttack)
                    }),

                    // Ưu tiên 2: Tấn công thường nếu trong tầm
                    new Sequence(new List<Node>
                    {
                        new ConditionNode(IsInAttackRange),
                        new ActionNode(Attack)
                    }),

                    // Ưu tiên 3: Đuổi theo
                    new ActionNode(Chase)
                })
            }),

            // Nhánh 2: Tuần tra
            new ActionNode(Patrol)
        });
    }

    // ── Frenzy ───────────────────────────────────────────────

    private void CheckFrenzy()
    {
        if (_isFrenzy || healthSystem == null) return;

        if (healthSystem.HPPercent < frenzyThreshold)
        {
            _isFrenzy = true;
            runSpeed = _originalRunSpeed * frenzySpeedMultiplier;
            attackCooldown *= 0.5f; // Đánh nhanh gấp đôi

            // Đổi màu mắt đỏ nếu có renderer
            StartCoroutine(FrenzyVisualEffect());

            Debug.Log($"[ZombieRunner] {gameObject.name} FRENZY MODE!");
        }
    }

    private IEnumerator FrenzyVisualEffect()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        while (_isFrenzy && !_isDead)
        {
            // Nhấp nháy đỏ liên tục
            float pulse = (Mathf.Sin(Time.time * 10f) + 1f) / 2f;
            Color frenzyColor = Color.Lerp(Color.white, Color.red, pulse * 0.4f);

            foreach (var r in renderers)
                foreach (var mat in r.materials)
                    mat.SetColor("_BaseColor", frenzyColor);

            yield return null;
        }
    }

    // ── Leap Attack ──────────────────────────────────────────

    private bool CanLeap()
    {
        if (_isLeaping) return false;
        if (Time.time < _nextLeapTime) return false;
        if (player == null) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        return dist >= leapMinRange && dist <= leapMaxRange;
    }

    private NodeState LeapAttack()
    {
        if (!_isLeaping)
        {
            _isLeaping = true;
            _nextLeapTime = Time.time + leapCooldown;
            StartCoroutine(PerformLeap());
        }

        return NodeState.Running;
    }

    private IEnumerator PerformLeap()
    {
        // Dừng NavMesh, tự điều khiển physics
        agent.isStopped = true;
        agent.enabled = false;

        anim.SetTrigger("Leap");

        // Tính hướng và lực nhảy về phía player
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 leapVelocity = dir * leapForce + Vector3.up * (leapForce * 0.4f);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(leapVelocity, ForceMode.VelocityChange);
        }

        // Chờ trong không trung
        float leapDuration = 0.6f;
        float elapsed = 0f;
        bool hitPlayer = false;

        while (elapsed < leapDuration)
        {
            elapsed += Time.deltaTime;

            // Xoay mặt về phía player khi bay
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 10f);

            // Check va chạm với player trong khi bay
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer < 1.5f && !hitPlayer)
            {
                hitPlayer = true;
                HitPlayerOnLand();
            }

            yield return null;
        }

        // Hạ cánh
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // Bật lại NavMesh
        agent.enabled = true;
        agent.isStopped = false;
        _isLeaping = false;

        Debug.Log("[ZombieRunner] Leap xong!");
    }

    private void HitPlayerOnLand()
    {
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(leapDamage, gameObject);
            Debug.Log($"[ZombieRunner] Leap trúng player! Damage: {leapDamage}");
        }

        // Knockback player
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 knockDir = (player.position - transform.position).normalized;
            playerRb.AddForce(knockDir * leapKnockback, ForceMode.Impulse);
        }
    }

    // ── Alert đồng bọn ───────────────────────────────────────

    // Gọi từ ScreamAction trong ZombieBase qua override
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
                // Force zombie khác chuyển sang chase
                zombie.TakeDamage(0f, gameObject); // Trick: damage 0 để trigger alert
                alerted++;
            }
        }

        Debug.Log($"[ZombieRunner] Alert {alerted} zombie xung quanh!");
    }

    // ── Gizmos ───────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Leap range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, leapMinRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, leapMaxRange);

        // Alert radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }
}