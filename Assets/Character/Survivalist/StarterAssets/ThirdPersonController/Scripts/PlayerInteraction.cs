using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Xử lý nhặt đồ, vứt đồ, và giao tiếp với vật phẩm trong scene.
/// Cập nhật PlayerState sau đó mới set Animator.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Cài đặt Tương tác")]
    public float interactionRadius = 2.0f;
    public LayerMask itemLayer;

    [Header("Thành phần kết nối")]
    public Transform weaponSlot;    // Gắn Transform bàn tay phải vào đây

    // ── Private ─────────────────────────────────────────────
    private Animator _animator;
    private GameObject _nearestItem = null;

    // Animator Parameter IDs
    private int _paramPickUp;
    private int _paramWeaponType;

    // ── UI callback (gán từ UIManager nếu có) ───────────────
    public event System.Action<string> OnShowPickupPrompt;   // tên item
    public event System.Action OnHidePickupPrompt;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
            Debug.LogError("[PlayerInteraction] Không tìm thấy Animator!");

        _paramPickUp = Animator.StringToHash("PickUp");
        _paramWeaponType = Animator.StringToHash("WeaponType");

        // Subscribe event từ PlayerState để sync Animator khi weapon thay đổi
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnWeaponChanged += SyncAnimatorWeaponType;
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnWeaponChanged -= SyncAnimatorWeaponType;
    }

    private void Update()
    {
        FindNearestItem();

        if (_nearestItem != null && Input.GetKeyDown(KeyCode.F))
            PerformPickup();

        // Bấm G để vứt đồ
        if (Input.GetKeyDown(KeyCode.G))
            DropCurrentItem();
    }

    // ── Tìm đồ gần nhất ─────────────────────────────────────

    private void FindNearestItem()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, itemLayer);

        if (hits.Length == 0)
        {
            _nearestItem = null;
            OnHidePickupPrompt?.Invoke();
            return;
        }

        float shortest = Mathf.Infinity;
        GameObject closest = null;

        foreach (Collider col in hits)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < shortest)
            {
                shortest = dist;
                closest = col.gameObject;
            }
        }

        _nearestItem = closest;

        // Hiện UI prompt nếu có ItemData
        ItemData data = _nearestItem?.GetComponent<ItemData>();
        string name = data != null ? data.itemName : _nearestItem.name;
        OnShowPickupPrompt?.Invoke(name);
    }

    // ── Nhặt đồ ─────────────────────────────────────────────

    private void PerformPickup()
    {
        if (_animator == null) return;

        PlayerState.Instance?.SetPickingUp(true);
        _animator.SetTrigger(_paramPickUp);

        Debug.Log($"[PlayerInteraction] Bắt đầu nhặt: {_nearestItem.name}");
    }

    /// <summary>
    /// GỌI TỪ ANIMATION EVENT khi tay vươn tới vật phẩm.
    /// Gắn event này vào đúng frame trong animation PickUp.
    /// </summary>
    public void EquipItem()
    {
        if (_nearestItem == null)
        {
            Debug.LogWarning("[PlayerInteraction] EquipItem gọi nhưng không có item!");
            PlayerState.Instance?.SetPickingUp(false);
            return;
        }

        ItemData data = _nearestItem.GetComponent<ItemData>();
        if (data == null)
        {
            Debug.LogWarning($"[PlayerInteraction] {_nearestItem.name} không có ItemData!");
            PlayerState.Instance?.SetPickingUp(false);
            return;
        }

        // Tắt vật lý, gắn vào tay
        var rb = _nearestItem.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        var col = _nearestItem.GetComponent<Collider>();
        if (col) col.enabled = false;

        _nearestItem.transform.SetParent(weaponSlot);
        _nearestItem.transform.localPosition = data.holdPositionOffset;
        _nearestItem.transform.localRotation = Quaternion.Euler(data.holdRotationOffset);

        // Cập nhật PlayerState (sẽ tự fire event → SyncAnimatorWeaponType)
        PlayerState.Instance?.EquipWeapon(data.weaponType, _nearestItem);
        PlayerState.Instance?.SetPickingUp(false);

        OnHidePickupPrompt?.Invoke();
        _nearestItem = null;

        Debug.Log($"[PlayerInteraction] Đã trang bị: {data.itemName} (WeaponType={data.weaponType})");
    }

    // ── Vứt đồ ──────────────────────────────────────────────

    private void DropCurrentItem()
    {
        if (PlayerState.Instance?.CurrentItemInHand == null)
        {
            Debug.Log("[PlayerInteraction] Không có đồ để vứt.");
            return;
        }

        PlayerState.Instance.DropCurrentItem();
        // SyncAnimatorWeaponType sẽ tự chạy qua event OnWeaponChanged
        Debug.Log("[PlayerInteraction] Đã vứt đồ.");
    }

    // ── Sync Animator theo PlayerState ──────────────────────

    private void SyncAnimatorWeaponType(int weaponType)
    {
        if (_animator != null)
            _animator.SetInteger(_paramWeaponType, weaponType);
    }

    // ── Gizmos ──────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}