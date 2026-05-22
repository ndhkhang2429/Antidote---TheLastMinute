using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zombie mập: trâu, khỏe, chậm.
/// Khi chết → gồng người, đỏ dần, phình to → BOOM.
/// </summary>
public class ZombieTank : ZombieBase
{
    [Header("Tank Stats")]
    public float explosionRadius = 5f;
    public float explosionDamageMax = 80f;
    public float explosionDamageMin = 10f;
    public float explosionForce = 500f;

    [Header("Pre-Explosion Effect")]
    public float preExplosionDuration = 2f;    // Thời gian gồng trước khi nổ
    public float maxScaleMultiplier = 1.4f;  // Phình to tối đa

    [Header("Explosion VFX")]
    public GameObject explosionVFX;            // Prefab particle effect

    // ── Private ─────────────────────────────────────────────
    private Vector3 _originalScale;
    private Renderer[] _renderers;

    // Shader property IDs
    private static readonly int PropColor = Shader.PropertyToID("_BaseColor");
    private static readonly int PropEmission = Shader.PropertyToID("_EmissionColor");

    // ────────────────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();

        // Stats riêng ZombieTank
        attackDamage = 25f;
        attackCooldown = 2.5f;
        walkSpeed = 0.6f;
        runSpeed = 1.8f;
        detectionRange = 8f;
        attackRange = 2.2f;

        // Lưu scale gốc để phình to
        _originalScale = transform.localScale;

        // Lấy tất cả Renderer trên body
        _renderers = GetComponentsInChildren<Renderer>();
    }

    // ── Override Die ─────────────────────────────────────────

    protected override void Die()
    {
        // Dừng AI
        _isDead = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.enabled = false;

        // Tắt collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Trigger animation gồng người
        anim.SetTrigger("PreExplode");

        // Bật emission trên tất cả material
        EnableEmission();

        // Bắt đầu hiệu ứng phình + đỏ → rồi nổ
        StartCoroutine(PreExplosionRoutine());
    }

    // ── Pre-Explosion Effect ─────────────────────────────────

    private IEnumerator PreExplosionRoutine()
    {
        float elapsed = 0f;

        while (elapsed < preExplosionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / preExplosionDuration);

            // 1. Phình to dần
            float scaleMultiplier = Mathf.Lerp(1f, maxScaleMultiplier, t);
            transform.localScale = _originalScale * scaleMultiplier;

            // 2. Đỏ dần + phát sáng
            Color bodyColor = Color.Lerp(Color.white, Color.red, t);
            Color emissionColor = Color.Lerp(Color.black, Color.red, t) * (t * 3f);

            foreach (var r in _renderers)
            {
                foreach (var mat in r.materials)
                {
                    mat.SetColor(PropColor, bodyColor);
                    mat.SetColor(PropEmission, emissionColor);
                }
            }

            // 3. Rung lắc nhẹ ở cuối (t > 0.7)
            if (t > 0.7f)
            {
                float shakeIntensity = (t - 0.7f) / 0.3f * 0.05f;
                transform.localScale = _originalScale * scaleMultiplier
                    + Vector3.one * Mathf.Sin(Time.time * 30f) * shakeIntensity;
            }

            yield return null;
        }

        // Hiệu ứng xong → NỔ
        Explode();
    }

    // ── Explode ──────────────────────────────────────────────

    private void Explode()
    {
        Vector3 explosionCenter = transform.position + Vector3.up * 1.5f;
        // Spawn VFX
        if (explosionVFX != null)
            Instantiate(explosionVFX, explosionCenter, Quaternion.identity);

        // Detect tất cả collider trong bán kính
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            float damage = CalculateExplosionDamage(dist);

            // Damage Player
            if (hit.CompareTag("Player"))
            {
                HealthSystem playerHealth = hit.GetComponent<HealthSystem>()
                    ?? hit.GetComponentInParent<HealthSystem>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage, gameObject);
                    Debug.Log($"[ZombieTank] Nổ gây {damage:F1} damage cho Player!");
                }

                // Đẩy player văng ra
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dir = (hit.transform.position - transform.position).normalized;
                    float distFactor = 1f - Mathf.Clamp01(dist / explosionRadius);
                    rb.AddForce(dir * explosionForce * distFactor, ForceMode.Impulse);
                }
            }

            // Damage zombie khác trong tầm (nổ dây chuyền nếu gặp Tank khác)
            ZombieBase zombie = hit.GetComponent<ZombieBase>()
                ?? hit.GetComponentInParent<ZombieBase>();

            if (zombie != null && zombie != this)
                zombie.TakeDamage(damage * 0.5f, gameObject);
        }

        Debug.Log($"[ZombieTank] BOOM! Bán kính: {explosionRadius}m");

        // Destroy object sau khi nổ
        Destroy(gameObject, 0.1f);
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>
    /// Damage giảm dần từ tâm nổ ra rìa (giống lựu đạn).
    /// dist=0 → DamageMax, dist=radius → DamageMin
    /// </summary>
    private float CalculateExplosionDamage(float dist)
    {
        float normalizedDist = Mathf.Clamp01(dist / explosionRadius);
        return Mathf.Lerp(explosionDamageMax, explosionDamageMin, normalizedDist);
    }

    private void EnableEmission()
    {
        foreach (var r in _renderers)
            foreach (var mat in r.materials)
                mat.EnableKeyword("_EMISSION");
    }

    // ── Gizmos ───────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Dời vòng Gizmos lên ngang bụng
        Vector3 gizmoCenter = transform.position + Vector3.up * 1.5f;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawSphere(gizmoCenter, explosionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(gizmoCenter, explosionRadius);
    }
}