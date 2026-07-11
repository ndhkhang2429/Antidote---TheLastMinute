using System.Collections;
using UnityEngine;
using Art_Equilibrium;

/// <summary>
/// Gắn script này lên CÙNG GameObject với PanelInteractZone (khu vực bàn phím Phòng Trưởng khoa).
/// Nối hàm OnPasswordCorrect() vào ô "On Access Granted ()" của component Keypad (asset NavKeypad)
/// qua Inspector - UnityEvent tự lo việc gọi, không cần code bắt sự kiện.
/// </summary>
public class PasswordDoorController : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private AE_Door targetDoor;
    [SerializeField] private PanelInteractZone panelZone;
    [SerializeField] private GameObject rewardRoomContent; // Object cha chứa Keycard/Medkit/Ammo, set active khi mở

    [Header("Thời gian chờ trước khi tự thoát chế độ zoom")]
    [Tooltip("Nên >= Display Result Time của Keypad để player kịp thấy chữ 'Granted' trước khi camera thoát zoom")]
    [SerializeField] private float exitPanelDelay = 1.2f;

    /// <summary>
    /// Gọi hàm này từ UnityEvent "On Access Granted ()" trong Inspector của component Keypad.
    /// </summary>
    public void OnPasswordCorrect()
    {
        if (targetDoor != null)
            targetDoor.UnlockByPassword();

        if (rewardRoomContent != null)
            rewardRoomContent.SetActive(true);

        // TODO: nếu muốn ghi nhận quest qua QuestManager, gọi tại đây, ví dụ:
        // QuestManager.Instance?.ReportEvent(QuestCompletionType.???, "PasswordDoorUnlocked");
        Debug.Log("[PasswordDoorController] Mật khẩu đúng - đã mở phòng Trưởng khoa Nội.");

        StartCoroutine(AutoExitPanelMode());
    }

    private IEnumerator AutoExitPanelMode()
    {
        yield return new WaitForSeconds(exitPanelDelay);

        if (panelZone != null && panelZone.IsInPanelMode)
            panelZone.TogglePanelMode();
    }
}