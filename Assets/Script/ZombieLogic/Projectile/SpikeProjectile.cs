using UnityEngine;

/// <summary>
/// SpikeProjectile — Gắn lên prefab viên spike của Warden II.
///
/// Bay thẳng theo hướng được set lúc spawn (không home in).
/// Destroy khi: chạm Player (deal damage) hoặc chạm môi trường hoặc hết lifetime.
///
/// Setup prefab cần:
///   - Rigidbody (Is Kinematic = true, Use Gravity = false)  
///   - Collider (Is Trigger = true)
///   - Script này
/// </summary>
public class SpikeProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 12f;
    public float lifetime = 5f;   // tự destroy sau bao nhiêu giây nếu không trúng gì

    // Set từ bên ngoài lúc spawn
    [HideInInspector] public float damage = 0f;
    [HideInInspector] public GameObject shooter = null; // zombie bắn ra (tránh tự bắn mình)

    private Vector3 _direction;
    private float _timer = 0f;

    /// <summary>Gọi ngay sau Instantiate để set hướng bay.</summary>
    public void Init(Vector3 direction, float damage, GameObject shooter)
    {
        _direction = direction.normalized;
        this.damage = damage;
        this.shooter = shooter;

        // Xoay prefab theo hướng bay cho đẹp
        if (_direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_direction);
    }

    private void Update()
    {
        // Bay thẳng
        transform.position += _direction * speed * Time.deltaTime;

        // Lifetime
        _timer += Time.deltaTime;
        if (_timer >= lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Bỏ qua chính zombie bắn ra
        if (shooter != null && other.gameObject == shooter) return;
        if (shooter != null && other.transform.IsChildOf(shooter.transform)) return;

        if (other.CompareTag("Player"))
        {
            // Deal damage
            other.GetComponent<HealthSystem>()?.TakeDamage(damage, shooter);
            Destroy(gameObject);
            return;
        }

        // Chạm tường / môi trường → destroy
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}