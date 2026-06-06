using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject inventoryPanel;
    public EquipmentPanelUI equipmentPanel;
    public WeaponPanelUI weaponPanel;
    public ItemGridUI itemGridPanel;

    bool _isOpen;
    bool _tabWasPressed; // chống double-trigger

    void Start()
    {
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
        // ── Dùng GetKeyDown thay vì Input System để tránh double-trigger ──
        bool tabDown = Input.GetKeyDown(KeyCode.Tab);

        if (tabDown && !_tabWasPressed)
        {
            _tabWasPressed = true;
            Toggle();
        }

        if (!tabDown) _tabWasPressed = false;

        // Nhấn Escape để đóng nếu đang mở
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseInventory();

        // Phím số chuyển slot — dù inventory mở hay đóng
        if (Input.GetKeyDown(KeyCode.Alpha1)) InventorySystem.Instance.SelectWeaponSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) InventorySystem.Instance.SelectWeaponSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) InventorySystem.Instance.SelectWeaponSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) InventorySystem.Instance.SelectWeaponSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) InventorySystem.Instance.SelectItemSlot();
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

        // Dừng player input khi mở inventory
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tắt ThirdPersonController input
        var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;

        Refresh();
    }

    void CloseInventory()
    {
        _isOpen = false;
        inventoryPanel.SetActive(false);

        // Trả lại player input
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Bật lại ThirdPersonController
        var tpc = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = true;
    }

    public void Refresh()
    {
        if (!_isOpen) return;
        equipmentPanel?.Refresh();
        weaponPanel?.Refresh();
        itemGridPanel?.Refresh();
    }
}