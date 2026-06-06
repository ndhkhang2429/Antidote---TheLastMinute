using UnityEngine;

public class FusePanelManager : MonoBehaviour
{
    [Header("References")]
    public FuseSlot[] allSlots;
    public SwitchSlider[] allSwitches;
    public bool[] correctSwitchStates;

    [Header("Indicators")]
    public Renderer powerOKLight;
    public Renderer lineBreakLight;
    public Material matLightOn;
    public Material matLightOff;

    public bool IsPanelReady { get; private set; } = false;

    // Không còn _heldFuseID — dùng inventory
    public bool HasFuseInHand => HasAnyFuseInInventory();

    void Start() => UpdatePanelState();

    // ── Kiểm tra inventory có fuse nào không ─────────────
    bool HasAnyFuseInInventory()
    {
        if (InventorySystem.Instance == null) return false;
        foreach (var slot in InventorySystem.Instance.GetItemSlots())
            if (!slot.IsEmpty && slot.item is FuseItemDataSO) return true;
        return false;
    }

    // ── Tìm fuse đúng ID trong inventory ─────────────────
    FuseItemDataSO GetFuseFromInventory(string fuseID)
    {
        if (InventorySystem.Instance == null) return null;
        foreach (var slot in InventorySystem.Instance.GetItemSlots())
        {
            if (slot.IsEmpty) continue;
            if (slot.item is FuseItemDataSO fuse && fuse.fuseID == fuseID)
                return fuse;
        }
        return null;
    }

    // ── Gắn fuse vào slot ────────────────────────────────
    public bool TryInsertHeldFuse(FuseSlot slot)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return false;

        // ── Bắt buộc phải đang cầm đúng fuse ở slot 5 ───────
        if (!inv.IsHoldingFuse(slot.correctFuseID))
        {
            Debug.Log($"[FusePanel] Chưa cầm {slot.correctFuseID}! Nhấn [5] trước.");
            return false;
        }

        bool success = slot.TryInsertFuse(slot.correctFuseID);
        if (success)
        {
            inv.RemoveItem(inv.heldItemSlot.item, 1);
            UpdatePanelState();
        }
        return success;
    }

    // ── Giữ lại để không lỗi tham chiếu cũ ──────────────

    public void PickUpFuse(string fuseID)
    {
        Debug.Log($"[FusePanel] PickUpFuse gọi nhưng giờ dùng inventory: {fuseID}");
    }

    public void UpdatePanelState()
    {
        bool fusesOK = CheckAllFuses();
        bool switchesOK = CheckAllSwitches();
        IsPanelReady = fusesOK && switchesOK;
        UpdateIndicatorLights();
        Debug.Log($"[FusePanel] Fuse:{fusesOK} | Switch:{switchesOK} | Ready:{IsPanelReady}");
    }

    bool CheckAllFuses()
    {
        foreach (var slot in allSlots)
            if (slot.requiresFuse && !slot.IsCorrect) return false;
        return true;
    }

    bool CheckAllSwitches()
    {
        if (correctSwitchStates == null || correctSwitchStates.Length == 0) return true;
        for (int i = 0; i < allSwitches.Length; i++)
        {
            if (i >= correctSwitchStates.Length) break;
            if (allSwitches[i].isOn != correctSwitchStates[i]) return false;
        }
        return true;
    }

    void UpdateIndicatorLights()
    {
        if (powerOKLight != null)
            powerOKLight.material = IsPanelReady ? matLightOn : matLightOff;
        if (lineBreakLight != null)
            lineBreakLight.material = IsPanelReady ? matLightOff : matLightOn;
    }
}