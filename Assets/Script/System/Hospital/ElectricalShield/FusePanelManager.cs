using UnityEngine;

/// <summary>
/// Não trung tâm của thùng điện.
/// Kiểm tra: đủ fuse + tất cả switch đúng ON/OFF → cho phép gạt.
/// </summary>
public class FusePanelManager : MonoBehaviour
{
    [Header("References")]
    public FuseSlot[] allSlots;
    public SwitchSlider[] allSwitches;

    [Tooltip("Trạng thái đúng của từng switch (true=ON, false=OFF). " +
             "Thứ tự phải khớp với allSwitches.")]
    public bool[] correctSwitchStates;

    [Header("Indicators")]
    public Renderer powerOKLight;
    public Renderer lineBreakLight;
    public Material matLightOn;
    public Material matLightOff;

    // ── State ──────────────────────────────────────────────
    public bool IsPanelReady { get; private set; } = false;

    private string _heldFuseID = null;
    public bool HasFuseInHand => _heldFuseID != null;
    public string HeldFuseID => _heldFuseID;

    void Start()
    {
        UpdatePanelState();
    }

    public void PickUpFuse(string fuseID)
    {
        _heldFuseID = fuseID;
        Debug.Log($"[FusePanel] Player đang cầm fuse: {fuseID}");
    }

    public bool TryInsertHeldFuse(FuseSlot slot)
    {
        if (_heldFuseID == null) return false;

        bool success = slot.TryInsertFuse(_heldFuseID);
        if (success)
        {
            _heldFuseID = null;
            UpdatePanelState();
        }
        return success;
    }

    /// <summary>
    /// Gọi mỗi khi switch toggle hoặc fuse được gắn.
    /// </summary>
    public void UpdatePanelState()
    {
        bool fusesOK = CheckAllFuses();
        bool switchesOK = CheckAllSwitches();

        IsPanelReady = fusesOK && switchesOK;
        UpdateIndicatorLights();

        Debug.Log($"[FusePanel] Fuse: {fusesOK} | Switch: {switchesOK} | Ready: {IsPanelReady}");
    }

    bool CheckAllFuses()
    {
        foreach (var slot in allSlots)
        {
            if (slot.requiresFuse && !slot.IsCorrect)
                return false;
        }
        return true;
    }

    bool CheckAllSwitches()
    {
        // Nếu chưa setup correctSwitchStates thì bỏ qua bước check này
        if (correctSwitchStates == null || correctSwitchStates.Length == 0)
            return true;

        for (int i = 0; i < allSwitches.Length; i++)
        {
            if (i >= correctSwitchStates.Length) break;

            if (allSwitches[i].isOn != correctSwitchStates[i])
                return false;
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