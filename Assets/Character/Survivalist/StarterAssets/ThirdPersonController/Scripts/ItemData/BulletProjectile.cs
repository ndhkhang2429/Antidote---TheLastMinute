using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BulletProjectile : MonoBehaviour
{
    private float _bulletDamage;

    [Header("Impact Effects")]
    [Tooltip("Kéo Prefab Vết đạn hoặc Tia lửa vào đây")]
    public GameObject wallHitPrefab;

    // (Tùy chọn) Có thể thêm prefab máu văng khi bắn trúng Zombie
    // public GameObject fleshHitPrefab; 

    public void SetupBullet(float damageAmount)
    {
        _bulletDamage = damageAmount;

        // Tự hủy đạn sau 3 giây nếu bắn chỉ thiên không trúng gì cả
        Destroy(gameObject, 3f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Bullet")) return;

        ContactPoint contact = collision.contacts[0];

        // --- SỬA Ở ĐÂY ---
        // 1. Thêm dấu TRỪ (-) trước contact.normal để lật ngược hướng máy chiếu, đâm thẳng vào tường
        Quaternion hitRotation = Quaternion.LookRotation(-contact.normal);

        if (collision.gameObject.TryGetComponent<HealthSystem>(out HealthSystem targetHealth))
        {
            targetHealth.TakeDamage(_bulletDamage);
        }
        else
        {
            if (wallHitPrefab != null)
            {
                // 2. Kéo máy chiếu lùi ra ngoài không khí một chút xíu (0.05 unit) 
                // để tránh vách hộp máy chiếu bị kẹt sâu vào trong tường gây lỗi Z-Fighting
                Vector3 spawnPosition = contact.point + contact.normal * 0.05f;

                GameObject hole = Instantiate(wallHitPrefab, spawnPosition, hitRotation);
                hole.transform.SetParent(collision.transform);
                Destroy(hole, 10f);
            }
        }

        Destroy(gameObject);
    }
}
