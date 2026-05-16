using UnityEngine;

public class Zombie_Phát_Nổ : ZombieBase
{
    // override: Ghi đè hàm Start của Base để thiết lập chỉ số riêng cho con này
    protected override void Start()
    {
        // Vẫn gọi base.Start() để nó lấy NavMesh, Animator và tìm Player
        base.Start();

        attackDamage = 20f;
        attackCooldown = 1.5f;
        walkSpeed = 0.8f;
        runSpeed = 2f;
        detectionRange = 10f;
        attackRange = 2f;
    }
}
