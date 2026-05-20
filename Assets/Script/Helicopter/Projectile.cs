using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] public WeaponData data;

    [Header("VFX")]
    public GameObject hitVFXPrefab; // Kéo prefab hiệu ứng nổ/tia lửa vào đây

    private float timer;

    void OnEnable()
    {
        timer = 0f;
    }

    void Update()
    {
        // Bay theo đường thẳng (hướng Z của đạn)
        transform.Translate(Vector3.forward * data.projectileSpeed * Time.deltaTime);

        // Tự động thu hồi đạn nếu bay quá lâu (không trúng gì)
        timer += Time.deltaTime;
        if (timer >= data.lifeTime)
        {
            gameObject.SetActive(false); // Trả về pool
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Xử lý logic trừ máu ở đây (ví dụ: other.GetComponent<Enemy>().TakeDamage(data.damage);)

        // Spawn VFX trúng đích
        if (hitVFXPrefab != null)
        {
            // Spawn VFX và tự động destroy VFX sau 2 giây
            GameObject vfx = Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // Tắt viên đạn đi (trả về pool)
        gameObject.SetActive(false);
    }
}