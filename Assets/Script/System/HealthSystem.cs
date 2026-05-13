using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("HP settings")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("Die Effect")]
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private bool destroyOnDeath = false;

    // ── Events ──────────────────────────────────────────────
    public event Action<float, float> OnDamaged;   // (currentHP, maxHP)
    public event Action<float, float> OnHealed;    // (currentHP, maxHP)
    public event Action OnDeath;
    public event Action<float, float> OnHPChanged; // Gọi mỗi khi HP thay đổi (cho UI)

    private bool _isDead = false;
    public bool IsDead => _isDead;
    public float HPPercent => currentHP / maxHP;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;


    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (_isDead) return;
        if (damage <= 0) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        Debug.Log($"[HealthSystem] {gameObject.name} bị {damage} damage " +
                  $"bởi {attacker?.name ?? "Unknown"}. HP: {currentHP}/{maxHP}");

        OnDamaged?.Invoke(currentHP, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[HealthSystem] {gameObject.name} đã chết!");

        OnDeath?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }

    private void OnDrawGizmosSelected()
    {
        // Hiển thị HP bar đơn giản trong Scene view
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Vector3 pos = transform.position + Vector3.up * 2.5f;
        Gizmos.DrawCube(pos, new Vector3(HPPercent * 1f, 0.1f, 0.1f));
    }

}
