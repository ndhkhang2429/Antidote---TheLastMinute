using UnityEngine;

/// <summary>
/// Gắn vào PanelTrigger (cùng chỗ với PanelInteractZone).
/// Khi đang trong panel mode, xử lý click chuột vào:
/// - FuseSlot trống → gắn fuse
/// - SwitchSlider → toggle ON/OFF
/// </summary>
public class PanelClickHandler : MonoBehaviour
{
    [Header("References")]
    public PanelInteractZone panelZone;
    public FusePanelManager fusePanelManager;

    [Header("Settings")]
    public float clickRayDistance = 10f;
    public LayerMask clickableLayer; // Layer của Switch001-014

    void Update()
    {
        // Chỉ xử lý click khi đang trong panel mode
        if (panelZone == null || !panelZone.IsInPanelMode) return;

        if (Input.GetMouseButtonDown(0)) // Click chuột trái
            HandleClick();
    }

    void HandleClick()
    {
        // Bắn ray từ vị trí chuột trên màn hình
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickableLayer))
            return;

        GameObject hitObj = hit.collider.gameObject;

        // ── Thử gắn fuse vào FuseSlot trống ──────────────
        FuseSlot slot = hitObj.GetComponentInParent<FuseSlot>();
        if (slot != null && slot.requiresFuse && !slot.HasFuse)
        {
            if (fusePanelManager != null && fusePanelManager.HasFuseInHand)
            {
                bool success = fusePanelManager.TryInsertHeldFuse(slot);
                InteractionUIManager.Instance?.ShowPrompt(
                    success ? "✓ Gắn cầu chì thành công!" : "✗ Sai cầu chì cho slot này!");
            }
            else
            {
                InteractionUIManager.Instance?.ShowPrompt("⚠ Không có cầu chì trong tay!");
            }
            return;
        }

        // ── Thử toggle SwitchSlider ────────────────────────
        SwitchSlider sw = hitObj.GetComponentInParent<SwitchSlider>();
        if (sw != null)
        {
            sw.Toggle();
            return;
        }
    }
}