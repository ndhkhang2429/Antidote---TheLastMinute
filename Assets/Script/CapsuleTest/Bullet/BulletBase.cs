using UnityEngine;
using System;

public abstract class BulletBase : MonoBehaviour
{
    public BulletData bulletData;
    public PlayerStats playerStatsSO; // KÉO FILE SO VÀO ĐÂY TRÊN PREFAB ĐẠN

    private Action<BulletBase> returnToPool;
    private float currentLifeTime;
    private bool hasHitTarget = false;
    private bool isReleased = false;

    // Hàm Init giờ cực kỳ sạch, không cần truyền Player nữa
    public void Init(Action<BulletBase> returnAction)
    {
        returnToPool = returnAction;
        currentLifeTime = bulletData.lifeTime;
        hasHitTarget = false;
        isReleased = false;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * bulletData.speed * Time.deltaTime);

        currentLifeTime -= Time.deltaTime;
        if (currentLifeTime <= 0)
        {
            if (!hasHitTarget && playerStatsSO != null)
            {
                playerStatsSO.DeductHealthPercent(bulletData.missHealthPenaltyPercent);
            }
            ReturnToPoolSafe();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            hasHitTarget = true;
            OnHitTarget(other);
            ReturnToPoolSafe();
        }
    }

    private void ReturnToPoolSafe()
    {
        if (!isReleased)
        {
            isReleased = true;
            returnToPool?.Invoke(this);
        }
    }

    protected abstract void OnHitTarget(Collider target);
}