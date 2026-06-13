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

    public bool HasFuseInHand => HasAnyFuseInInventory();

    void Start() => UpdatePanelState();

    bool HasAnyFuseInInventory()
    {
        if (InventorySystem.Instance == null) return false;
        foreach (var slot in InventorySystem.Instance.GetItemSlots())
            if (!slot.IsEmpty && slot.item is FuseItemDataSO) return true;
        return false;
    }

    public bool TryInsertHeldFuse(FuseSlot slot)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return false;

        // 1. Lấy vật phẩm nhân vật ĐANG CẦM TRÊN TAY
        var heldItem = inv.GetHeldItem();

        // 2. Kiểm tra xem nó có phải Cầu chì (FuseItemDataSO) không VÀ có đúng ID không
        bool isHoldingCorrectFuse = heldItem != null
                                 && heldItem is FuseItemDataSO fuse
                                 && fuse.fuseID == slot.correctFuseID;

        if (!isHoldingCorrectFuse)
        {
            if (NotificationUI.Instance != null)
            {
                NotificationUI.Instance.ShowNotification($"Cần cầm cầu chì loại [{slot.correctFuseID}] trên tay!");
            }
            return false;
        }

        // 3. Nếu đúng đồ, tiến hành gắn vào bảng
        bool success = slot.TryInsertFuse(slot.correctFuseID);
        if (success)
        {
            inv.RemoveItem(heldItem, 1); // Trừ vật phẩm khỏi balo/tay
            UpdatePanelState();
        }
        return success;
    }

    public void UpdatePanelState()
    {
        bool fusesOK = CheckAllFuses();
        bool switchesOK = CheckAllSwitches();
        IsPanelReady = fusesOK && switchesOK;
        UpdateIndicatorLights();
    }

    public bool CheckAllFuses()
    {
        foreach (var slot in allSlots)
            if (slot.requiresFuse && !slot.IsCorrect) return false;
        return true;
    }

    public bool CheckAllSwitches()
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