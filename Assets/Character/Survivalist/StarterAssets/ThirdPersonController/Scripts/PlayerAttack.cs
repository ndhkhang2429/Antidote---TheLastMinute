using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    public float attackCooldown = 0.4f;
    private float nextAttackTime = 0f;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("LỖI: Không tìm thấy Animator!");
        }
    }

    private void Update()
    {
        bool isPunching = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isPunching = true;
            Debug.Log("Click chuột trái!");
        }

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            isPunching = true;
            Debug.Log("Nhấn phím F!");
        }

        if (isPunching && Time.time >= nextAttackTime)
        {
            Punch();
            nextAttackTime = Time.time + attackCooldown;
        }
        else if (isPunching)
        {
            Debug.Log($"Cooldown chưa hết, còn {nextAttackTime - Time.time:F2}s");
        }
    }

    private void Punch()
    {
        if (animator != null)
        {
            int randomIndex = Random.Range(0, 2); // 0 = Punching1, 1 = Punching2
            animator.SetInteger("PunchIndex", randomIndex);
            animator.SetTrigger("Punch");
            Debug.Log($"Punch! Animation index: {randomIndex}");
        }
    }
}