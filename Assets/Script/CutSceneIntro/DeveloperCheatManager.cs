using UnityEngine;

public class DeveloperCheatManager : MonoBehaviour
{
    public static bool GodMode { get; private set; }
    public static bool InfiniteAmmo { get; private set; }

    [Header("References")]
    [SerializeField] private HealthSystem playerHealth;
    [SerializeField] private BossRoomTransition bossRoomTransition;
    [SerializeField] private MainSwitchInteractable mainSwitch;

    [Header("Build Settings")]
    [Tooltip("Nên để false để cheat không hoạt động trong bản Release.")]
    [SerializeField] private bool allowInReleaseBuild = false;

    private bool showCheatPanel;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private bool CheatsAreAllowed =>
        Application.isEditor ||
        Debug.isDebugBuild ||
        allowInReleaseBuild;

    private void Awake()
    {
        if (!CheatsAreAllowed)
        {
            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerHealth =
                    playerObject.GetComponent<HealthSystem>();
            }
        }

        if (bossRoomTransition == null)
        {
            bossRoomTransition =
                FindObjectOfType<BossRoomTransition>(true);
        }

        if (mainSwitch == null)
        {
            mainSwitch =
                FindObjectOfType<MainSwitchInteractable>(true);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            ToggleCheatPanel();

        if (Input.GetKeyDown(KeyCode.F2))
            ToggleGodMode();

        if (Input.GetKeyDown(KeyCode.F3))
            ToggleInfiniteAmmo();

        if (Input.GetKeyDown(KeyCode.F4))
            TurnOnHospitalPower();

        if (Input.GetKeyDown(KeyCode.F5))
            GoToBossRoom();

        if (Input.GetKeyDown(KeyCode.F6))
            RestoreFullHealth();
    }

    private void ToggleCheatPanel()
    {
        showCheatPanel = !showCheatPanel;

        if (showCheatPanel)
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void ToggleGodMode()
    {
        GodMode = !GodMode;

        if (GodMode)
            RestoreFullHealth();

        ShowMessage($"God Mode: {(GodMode ? "ON" : "OFF")}");
    }

    private void ToggleInfiniteAmmo()
    {
        InfiniteAmmo = !InfiniteAmmo;

        ShowMessage(
            $"Infinite Ammo: {(InfiniteAmmo ? "ON" : "OFF")}"
        );
    }

    private void TurnOnHospitalPower()
    {
        if (mainSwitch == null)
        {
            mainSwitch =
                FindObjectOfType<MainSwitchInteractable>(true);
        }

        if (mainSwitch == null)
        {
            ShowMessage("Không tìm thấy cần gạt điện chính.");
            return;
        }

        mainSwitch.CheatTurnOnPower();
    }

    private void GoToBossRoom()
    {
        if (bossRoomTransition == null)
        {
            ShowMessage("Chưa gán BossRoomTransition.");
            return;
        }

        bossRoomTransition.StartTransitionSequence();
        ShowMessage("Đang chuyển tới phòng boss.");
    }

    private void RestoreFullHealth()
    {
        if (playerHealth == null)
        {
            ShowMessage("Chưa gán HealthSystem của Player.");
            return;
        }

        playerHealth.ResetHealth();
        ShowMessage("Đã hồi đầy máu.");
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[Developer Cheat] {message}");

        if (NotificationUI.Instance != null)
            NotificationUI.Instance.ShowNotification(message);
    }

    private void OnGUI()
    {
        if (!CheatsAreAllowed || !showCheatPanel)
            return;

        const float panelWidth = 330f;
        const float panelHeight = 330f;

        Rect panelRect = new Rect(
            20f,
            20f,
            panelWidth,
            panelHeight
        );

        GUI.Box(panelRect, "DEVELOPER CHEATS");

        GUILayout.BeginArea(
            new Rect(
                panelRect.x + 15f,
                panelRect.y + 35f,
                panelWidth - 30f,
                panelHeight - 45f
            )
        );

        GUILayout.Label(
            $"F2 - God Mode: {(GodMode ? "ON" : "OFF")}"
        );

        if (GUILayout.Button("Toggle God Mode", GUILayout.Height(32f)))
            ToggleGodMode();

        GUILayout.Space(5f);

        GUILayout.Label(
            $"F3 - Infinite Ammo: {(InfiniteAmmo ? "ON" : "OFF")}"
        );

        if (GUILayout.Button("Toggle Infinite Ammo", GUILayout.Height(32f)))
            ToggleInfiniteAmmo();

        GUILayout.Space(5f);

        if (GUILayout.Button(
    "F4 - Turn On Hospital Power",
    GUILayout.Height(32f)))
        {
            TurnOnHospitalPower();
        }

        if (GUILayout.Button("F5 - Go To Boss Room", GUILayout.Height(32f)))
            GoToBossRoom();

        if (GUILayout.Button("F6 - Restore Full Health", GUILayout.Height(32f)))
            RestoreFullHealth();

        GUILayout.Space(8f);
        GUILayout.Label("Press F1 to close this panel.");

        GUILayout.EndArea();
    }

    private void OnDisable()
    {
        GodMode = false;
        InfiniteAmmo = false;

        if (showCheatPanel)
        {
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }
}