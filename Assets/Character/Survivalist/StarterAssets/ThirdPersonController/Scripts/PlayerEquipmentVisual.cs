using UnityEngine;

public class PlayerEquipmentVisual : MonoBehaviour
{
    public static PlayerEquipmentVisual Instance { get; private set; }

    [Header("SkinnedMeshRenderer của balo trên character")]
    [SerializeField] private SkinnedMeshRenderer _backpackRenderer;

    [Header("Bắt đầu không có balo — để trống")]
    [SerializeField] private bool _hideOnStart = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (_hideOnStart && _backpackRenderer != null)
            _backpackRenderer.enabled = false;

        // Lắng nghe khi inventory thay đổi
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += RefreshVisual;
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= RefreshVisual;
    }

    void RefreshVisual()
    {
        var backpack = InventorySystem.Instance?.equippedBackpack as BackpackDataSO;

        if (backpack == null || _backpackRenderer == null)
        {
            // Không có balo → ẩn mesh
            if (_backpackRenderer != null)
                _backpackRenderer.enabled = false;
            return;
        }

        // Có balo → hiện và swap mesh
        _backpackRenderer.enabled = true;

        if (backpack.backpackMesh != null)
            _backpackRenderer.sharedMesh = backpack.backpackMesh;

        if (backpack.backpackMaterials != null && backpack.backpackMaterials.Length > 0)
            _backpackRenderer.sharedMaterials = backpack.backpackMaterials;
    }

    // Gọi thủ công nếu cần
    public void ForceRefresh() => RefreshVisual();
}