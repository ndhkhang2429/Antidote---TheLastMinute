using UnityEngine;

public class ZombieSpitter : ZombieBase
{
    [Header("Spitter Settings")]
    public GameObject acidProjectilePrefab;
    public Transform firePoint;     // Vị trí bắn ra (vd: miệng của zombie)
    public float projectileSpeed = 15f;

    // Override lại hành động gây dame. 
    // Thay vì đánh cận chiến, ta sẽ spawn viên axit.
    public override void DealDamageToPlayer()
    {
        if (player == null || _isDead) return;

        // Tính toán hướng bắn từ miệng zombie đến ngực của player
        Vector3 targetPosition = player.position + new Vector3(0, 1.5f, 0); // Bắn vào ngực player
        Vector3 shootDirection = targetPosition - firePoint.position;

        // Tạo viên đạn
        GameObject acidObj = Instantiate(acidProjectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        // Truyền thông số cho viên đạn
        AcidProjectile projectile = acidObj.GetComponent<AcidProjectile>();
        if (projectile != null)
        {
            // Lấy thẳng attackDamage từ ZombieBase
            projectile.Setup(shootDirection, attackDamage, projectileSpeed);
        }
    }

    // Tùy chọn (Optional): Nếu bạn muốn zombie bắn xong lùi lại một chút để giữ khoảng cách
    /*
    protected override Node BuildTree()
    {
        // Bạn có thể override nguyên cây Behaviour Tree ở đây nếu muốn 
        // Spitter có hành vi phức tạp hơn (ví dụ: Hit & Run).
        // Nhưng với yêu cầu hiện tại, gọi base.BuildTree() là đã đủ ngon rồi!
        return base.BuildTree();
    }
    */
}