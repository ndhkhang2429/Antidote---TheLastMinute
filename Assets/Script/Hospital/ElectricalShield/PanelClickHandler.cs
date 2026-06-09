using UnityEngine;

public class PanelClickHandler : MonoBehaviour
{
    [Header("References")]
    public PanelInteractZone panelZone;
    public FusePanelManager fusePanelManager;

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

        Debug.Log($"[PanelClick] Ray từ {ray.origin} → {ray.direction} | Layer mask: {clickableLayer.value}");

        if (!Physics.Raycast(ray, out RaycastHit hit, clickRayDistance, clickableLayer))
        {
            // Thử raycast không có layer mask để xem có hit gì không
            if (Physics.Raycast(ray, out RaycastHit hitAny, clickRayDistance))
                Debug.Log($"[PanelClick] Miss layer, nhưng hit: {hitAny.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hitAny.collider.gameObject.layer)})");
            else
                Debug.Log("[PanelClick] Không hit gì cả");
            return;
        }

        Debug.Log($"[PanelClick] Hit: {hit.collider.gameObject.name}");

        GameObject hitObj = hit.collider.gameObject;

        FuseSlot slot = hitObj.GetComponentInParent<FuseSlot>();
        if (slot != null && slot.requiresFuse && !slot.HasFuse)
        {
            if (fusePanelManager != null && fusePanelManager.HasFuseInHand)
            {
                bool success = fusePanelManager.TryInsertHeldFuse(slot);
                InteractionUIManager.Instance?.ShowPrompt(
                    success ? "✓ Gắn cầu chì thành công!" : "✗ Sai cầu chì!");
            }
            else
            {
                InteractionUIManager.Instance?.ShowPrompt("⚠ Không có cầu chì trong tay!");
            }
            return;
        }

        SwitchSlider sw = hitObj.GetComponentInParent<SwitchSlider>();
        if (sw != null)
        {
            sw.Toggle();
            return;
        }

        Debug.Log($"[PanelClick] Hit {hit.collider.gameObject.name} nhưng không có FuseSlot hay SwitchSlider");
    }
}