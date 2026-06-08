using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject inventoryPanel;
    public WeaponPanelUI weaponPanel;
    public ItemGridUI itemGridPanel;

    [Header("SlotBar — tắt khi mở inventory")]
    public GameObject slotBarUI;

    bool _isOpen;
    bool _tabWasPressed;

    void Start()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += Refresh;

        inventoryPanel.SetActive(false);
        _isOpen = false;
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
    }

    void Update()
    {
        // ── Toggle inventory ──────────────────────────────
        bool tabDown = Input.GetKeyDown(KeyCode.Tab);
        if (tabDown && !_tabWasPressed)
        {
            _tabWasPressed = true;
            Toggle();
        }
        if (!tabDown) _tabWasPressed = false;

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseInventory();

        // ── Slot selection — hoạt động dù inventory mở/đóng
        if (Input.GetKeyDown(KeyCode.Alpha1))
            InventorySystem.Instance?.SelectWeaponSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            InventorySystem.Instance?.SelectWeaponSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            InventorySystem.Instance?.SelectWeaponSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            InventorySystem.Instance?.SelectWeaponSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            InventorySystem.Instance?.SelectItemSlot();

        // ── X → tay không ────────────────────────────────
        if (Input.GetKeyDown(KeyCode.X))
            InventorySystem.Instance?.DeselectAll();
    }

    void Toggle()
    {
        if (_isOpen) CloseInventory();
        else OpenInventory();
    }

    void OpenInventory()
    {
        _isOpen = true;
        inventoryPanel.SetActive(true);

        if (slotBarUI != null)
            slotBarUI.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;

        Refresh();
    }

    void CloseInventory()
    {
        _isOpen = false;
        inventoryPanel.SetActive(false);

        // Bật lại SlotBarUI
        if (slotBarUI != null)
            slotBarUI.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = true;
    }

    void Refresh()
    {
        if (!_isOpen) return;
        weaponPanel?.Refresh();
        itemGridPanel?.Refresh();
    }
}