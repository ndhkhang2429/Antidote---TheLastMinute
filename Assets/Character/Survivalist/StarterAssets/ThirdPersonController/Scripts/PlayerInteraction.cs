using UnityEngine;
using StarterAssets;

/// <summary>
/// Xử lý nhặt đồ, vứt đồ, và giao tiếp với vật phẩm trong scene.
/// — Đọc interactionRadius từ PlayerStatsSO
/// — Input: F để nhặt, G để vứt (giữ nguyên theo thiết kế game)
/// — Sync Animator qua event OnWeaponChanged của PlayerState
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStatsSO _statsSO;   // kéo PlayerStats SO vào

    [Header("Thành phần kết nối")]
    [SerializeField] private Transform _weaponSlot;    // bàn tay phải

    [Header("Events — kéo SO vào đây trong Inspector")]
    [SerializeField] private GameEventSO OnItemPickedUp;
    [SerializeField] private GameEventSO OnItemDropped;

    // ── Private refs ───────────────────────────────────────
    private Animator _animator;
    private GameObject _nearestItem = null;

    // ── Animator param IDs ─────────────────────────────────
    private int _paramPickUp;
    private int _paramWeaponType;

    // ── UI callback (gán từ HUDManager nếu có) ─────────────
    public event System.Action<string> OnShowPickupPrompt;
    public event System.Action OnHidePickupPrompt;

    private float InteractionRadius =>
        _statsSO != null ? _statsSO.interactionRadius : 2f;

    // ── Lifecycle ──────────────────────────────────────────

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null) Debug.LogError("[PlayerInteraction] Không tìm thấy Animator!");
        if (_statsSO == null) Debug.LogWarning("[PlayerInteraction] Chưa gán PlayerStatsSO, dùng giá trị mặc định.");
        if (_weaponSlot == null) Debug.LogError("[PlayerInteraction] Chưa gán WeaponSlot!");

        _paramPickUp = Animator.StringToHash("PickUp");
        _paramWeaponType = Animator.StringToHash("WeaponType");

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

        if (Input.GetKeyDown(KeyCode.G))
            DropCurrentItem();
    }

    // ── Tìm đồ gần nhất ────────────────────────────────────

    private void FindNearestItem()
    {
        // Lấy itemLayer từ ItemData nếu muốn mở rộng, hoặc giữ SerializeField riêng
        Collider[] hits = Physics.OverlapSphere(transform.position, InteractionRadius);

        GameObject closest = null;
        float shortest = Mathf.Infinity;

        foreach (Collider col in hits)
        {
            // Chỉ nhặt object có ItemData
            if (col.GetComponent<ItemData>() == null) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < shortest)
            {
                shortest = dist;
                closest = col.gameObject;
            }
        }

        _nearestItem = closest;

        if (_nearestItem != null)
        {
            ItemData data = _nearestItem.GetComponent<ItemData>();
            OnShowPickupPrompt?.Invoke(data != null ? data.itemName : _nearestItem.name);
        }
        else
        {
            OnHidePickupPrompt?.Invoke();
        }
    }

    // ── Nhặt đồ ────────────────────────────────────────────

    private void PerformPickup()
    {
        if (_animator == null) return;

        PlayerState.Instance?.SetPickingUp(true);
        _animator.SetTrigger(_paramPickUp);

        Debug.Log($"[PlayerInteraction] Bắt đầu nhặt: {_nearestItem.name}");
    }

    /// <summary>
    /// Gọi từ Animation Event khi tay chạm vật phẩm.
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

        _nearestItem.transform.SetParent(_weaponSlot);
        _nearestItem.transform.localPosition = data.holdPositionOffset;
        _nearestItem.transform.localRotation = Quaternion.Euler(data.holdRotationOffset);

        PlayerState.Instance?.EquipWeapon(data.weaponType, _nearestItem);
        PlayerState.Instance?.SetPickingUp(false);

        OnItemPickedUp?.Raise();
        OnHidePickupPrompt?.Invoke();
        _nearestItem = null;

        Debug.Log($"[PlayerInteraction] Đã trang bị: {data.itemName} (WeaponType={data.weaponType})");
    }

    // ── Vứt đồ ─────────────────────────────────────────────

    private void DropCurrentItem()
    {
        if (PlayerState.Instance?.CurrentItemInHand == null)
        {
            Debug.Log("[PlayerInteraction] Không có đồ để vứt.");
            return;
        }

        PlayerState.Instance.DropCurrentItem();
        OnItemDropped?.Raise();

        Debug.Log("[PlayerInteraction] Đã vứt đồ.");
    }

    // ── Sync Animator ───────────────────────────────────────

    private void SyncAnimatorWeaponType(int weaponType)
    {
        if (_animator != null)
            _animator.SetInteger(_paramWeaponType, weaponType);
    }

    // ── Gizmos ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, InteractionRadius);
    }
}