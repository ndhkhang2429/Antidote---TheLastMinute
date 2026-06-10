using UnityEngine;

public class PanelClickHandler : MonoBehaviour
{
    [Header("References")]
    public PanelInteractZone panelZone;
    // Đã xóa fusePanelManager ở đây vì PanelClickHandler không cần quản lý kho đồ nữa

    [Header("Settings")]
    public float clickRayDistance = 10f;
    public LayerMask clickableLayer;

    void Update()
    {
        if (panelZone == null || !panelZone.IsInPanelMode) return;

        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Debug góc nhìn
        Debug.Log($"[PanelClick] Ray từ {ray.origin} → {ray.direction} | Layer mask: {clickableLayer.value}");

        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickableLayer))
        {
            // Bắn trượt lớp Layer cài đặt
            if (Physics.Raycast(ray, out RaycastHit hitAny, clickRayDistance))
                Debug.Log($"[PanelClick] Miss layer, nhưng hit: {hitAny.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hitAny.collider.gameObject.layer)})");
            else
                Debug.Log("[PanelClick] Không hit gì cả");
            return;
        }

        Debug.Log($"[PanelClick] Hit: {hit.collider.gameObject.name}");
        GameObject hitObj = hit.collider.gameObject;

        // ────────────────────────────────────────────────────────
        // 1. XỬ LÝ KHI BẮN TRÚNG KHE CẮM CẦU CHÌ
        // ────────────────────────────────────────────────────────
        FuseSlot slot = hitObj.GetComponentInParent<FuseSlot>();
        if (slot != null)
        {
            // Bàn giao toàn quyền cho FuseSlot tự kiểm tra ID, kho đồ và hiện thông báo!
            slot.InteractWithSlot();
            return; // Ngắt lệnh luôn
        }

        // ────────────────────────────────────────────────────────
        // 2. XỬ LÝ KHI BẮN TRÚNG CÔNG TẮC GẠT
        // ────────────────────────────────────────────────────────
        SwitchSlider sw = hitObj.GetComponentInParent<SwitchSlider>();
        if (sw != null)
        {
            sw.Toggle();
            return; // Ngắt lệnh luôn
        }

        Debug.Log($"[PanelClick] Hit {hit.collider.gameObject.name} nhưng không có FuseSlot hay SwitchSlider");
    }
}