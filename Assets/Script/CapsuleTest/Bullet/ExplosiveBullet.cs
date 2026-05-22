using UnityEngine;

public class ExplosiveBullet : BulletBase
{
    public GameObject explosionVFXPrefab; // Kéo thả prefab VFX nổ vào đây

    protected override void OnHitTarget(Collider target)
    {
        // Gây sát thương
        // target.GetComponent<TargetHealth>()?.TakeDamage(bulletData.damage);

        // Hiện VFX nổ tại vị trí chạm
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f); // Hủy VFX sau 2 giây (hoặc dùng pool cho VFX nếu muốn tối ưu sâu hơn)
        }
    }
}