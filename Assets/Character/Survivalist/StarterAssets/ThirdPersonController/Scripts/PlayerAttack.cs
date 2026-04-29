using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    public float attackCooldown = 0.4f; // Bạn có thể tăng thời gian này lên nếu vung gậy mất nhiều thời gian hơn đấm
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
        bool isAttacking = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isAttacking = true;
            Debug.Log("Click chuột trái!");
        }

        if (isAttacking && Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
        else if (isAttacking)
        {
            Debug.Log($"Cooldown chưa hết, còn {nextAttackTime - Time.time:F2}s");
        }
    }

    private void PerformAttack()
    {
        if (animator != null)
        {
            // Đọc trực tiếp trạng thái "isArmed" từ Animator mà chúng ta vừa tạo
            int currentWeapon = animator.GetInteger("WeaponType");

            if (currentWeapon == 1)
            {
                // NẾU ĐANG CẦM VŨ KHÍ
                animator.SetTrigger("WeaponAttack");
                Debug.Log("Chém bằng vũ khí 2 tay!");
            }
            else if(currentWeapon == 0)
            {
                // NẾU ĐANG TAY KHÔNG
                int randomIndex = Random.Range(0, 2); // 0 = Punching1, 1 = Punching2
                animator.SetInteger("PunchIndex", randomIndex);
                animator.SetTrigger("Punch");
                Debug.Log($"Punch! Animation index: {randomIndex}");
            }
        }
    }
}