using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject inventoryPanel;
    public EquipmentPanelUI equipmentPanel;
    public WeaponPanelUI weaponPanel;
    public ItemGridUI itemGridPanel;
    public HotbarUI hotbarPanel;

    bool _isOpen;

    void Start()
    {
        InventorySystem.Instance.OnInventoryChanged += Refresh;
        inventoryPanel.SetActive(false);
        hotbarPanel.gameObject.SetActive(true); // hotbar luôn hiện
    }

    void Update()
    {
        // Nhấn Tab để mở/đóng
        if (Keyboard.current.tabKey.wasPressedThisFrame) Toggle();

        // Hotbar shortcuts
        if (Keyboard.current.qKey.wasPressedThisFrame) InventorySystem.Instance.UseHotbar(0);
        if (Keyboard.current.eKey.wasPressedThisFrame) InventorySystem.Instance.UseHotbar(1);
        if (Keyboard.current.rKey.wasPressedThisFrame) InventorySystem.Instance.UseHotbar(2);
    }

    void Toggle()
    {
        _isOpen = !_isOpen;
        inventoryPanel.SetActive(_isOpen);
        // Lock / unlock cursor
        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _isOpen;
        if (_isOpen) Refresh();
    }

    void Refresh()
    {
        equipmentPanel.Refresh();
        weaponPanel.Refresh();
        itemGridPanel.Refresh();
        hotbarPanel.Refresh();
    }

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
    }
}