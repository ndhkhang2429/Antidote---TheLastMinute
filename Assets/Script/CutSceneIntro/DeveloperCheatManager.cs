using UnityEngine;

public class DeveloperCheatManager : MonoBehaviour
{
    public static bool GodMode { get; private set; }
    public static bool InfiniteAmmo { get; private set; }

    [Header("Player")]
    [SerializeField] private HealthSystem playerHealth;

    [Tooltip("Kéo GameObject gốc của Player có CharacterController vào đây.")]
    [SerializeField] private Transform playerTransform;

    [Header("Scene References")]
    [SerializeField] private MainSwitchInteractable mainSwitch;

    [Tooltip("Empty GameObject đặt trước cửa phòng Boss. Không cần gắn script.")]
    [SerializeField] private Transform bossDoorTeleportPoint;

    [Tooltip("Empty GameObject đặt tại vị trí xuất hiện trên tầng thượng.")]
    [SerializeField] private Transform rooftopTeleportPoint;

    [Header("Cheat Items")]
    [Tooltip("Kéo GameObject Card có component WorldItem vào đây.")]
    [SerializeField] private WorldItem bossKeyCard;

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

        FindMissingReferences();
    }

    private void FindMissingReferences()
    {
        GameObject playerObject = null;

        if (playerHealth == null || playerTransform == null)
        {
            playerObject =
                GameObject.FindGameObjectWithTag("Player");
        }

        if (playerHealth == null && playerObject != null)
        {
            playerHealth =
                playerObject.GetComponent<HealthSystem>();

            if (playerHealth == null)
            {
                playerHealth =
                    playerObject.GetComponentInChildren<HealthSystem>(true);
            }
        }

        if (playerTransform == null && playerObject != null)
        {
            CharacterController controller =
                playerObject.GetComponent<CharacterController>();

            if (controller == null)
            {
                controller =
                    playerObject.GetComponentInChildren<CharacterController>(
                        true
                    );
            }

            if (controller != null)
                playerTransform = controller.transform;
            else
                playerTransform = playerObject.transform;
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
            TeleportToBossDoor();

        if (Input.GetKeyDown(KeyCode.F6))
            RestoreFullHealth();

        if (Input.GetKeyDown(KeyCode.F7))
            TeleportToRooftop();

        if (Input.GetKeyDown(KeyCode.F8))
            GetBossKeyCard();
    }

    // =========================================================
    // F1 - CHEAT PANEL
    // =========================================================

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
            RestoreCursorState();
        }
    }

    private void RestoreCursorState()
    {
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    // =========================================================
    // F2 - GOD MODE
    // =========================================================

    private void ToggleGodMode()
    {
        GodMode = !GodMode;

        if (GodMode)
            RestoreFullHealth();

        ShowMessage(
            $"God Mode: {(GodMode ? "ON" : "OFF")}"
        );
    }

    // =========================================================
    // F3 - INFINITE AMMO
    // =========================================================

    private void ToggleInfiniteAmmo()
    {
        InfiniteAmmo = !InfiniteAmmo;

        ShowMessage(
            $"Infinite Ammo: {(InfiniteAmmo ? "ON" : "OFF")}"
        );
    }

    // =========================================================
    // F4 - TURN ON HOSPITAL POWER
    // =========================================================

    private void TurnOnHospitalPower()
    {
        if (mainSwitch == null)
        {
            mainSwitch =
                FindObjectOfType<MainSwitchInteractable>(true);
        }

        if (mainSwitch == null)
        {
            ShowMessage(
                "Không tìm thấy cần gạt điện chính."
            );

            return;
        }

        mainSwitch.CheatTurnOnPower();

        ShowMessage(
            "Nguồn điện bệnh viện đã được bật."
        );
    }

    // =========================================================
    // F5 - TELEPORT TO BOSS DOOR
    // =========================================================

    private void TeleportToBossDoor()
    {
        TeleportPlayer(
            bossDoorTeleportPoint,
            "Chưa gán Boss Door Teleport Point.",
            "Đã dịch chuyển Player đến trước cửa phòng Boss."
        );
    }

    // =========================================================
    // F6 - RESTORE FULL HEALTH
    // =========================================================

    private void RestoreFullHealth()
    {
        if (playerHealth == null)
            FindMissingReferences();

        if (playerHealth == null)
        {
            ShowMessage(
                "Không tìm thấy HealthSystem của Player."
            );

            return;
        }

        playerHealth.ResetHealth();

        ShowMessage(
            "Đã hồi đầy máu."
        );
    }

    // =========================================================
    // F7 - TELEPORT TO ROOFTOP
    // =========================================================

    private void TeleportToRooftop()
    {
        TeleportPlayer(
            rooftopTeleportPoint,
            "Chưa gán Rooftop Teleport Point.",
            "Đã dịch chuyển Player lên tầng thượng."
        );
    }

    // =========================================================
    // DÙNG CHUNG CHO F5 VÀ F7
    // =========================================================

    private void TeleportPlayer(
        Transform destination,
        string missingPointMessage,
        string successMessage)
    {
        if (playerTransform == null)
            FindMissingReferences();

        if (playerTransform == null)
        {
            ShowMessage(
                "Không tìm thấy Player Transform."
            );

            return;
        }

        if (destination == null)
        {
            ShowMessage(missingPointMessage);
            return;
        }

        CharacterController characterController =
            playerTransform.GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController =
                playerTransform.GetComponentInChildren<CharacterController>(
                    true
                );
        }

        if (characterController == null)
        {
            characterController =
                playerTransform.GetComponentInParent<CharacterController>();
        }

        bool controllerWasEnabled =
            characterController != null &&
            characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        playerTransform.SetPositionAndRotation(
            destination.position,
            destination.rotation
        );

        if (controllerWasEnabled)
            characterController.enabled = true;

        Physics.SyncTransforms();

        ShowMessage(successMessage);
    }

    // =========================================================
    // F8 - GET BOSS KEY CARD
    // =========================================================

    private void GetBossKeyCard()
    {
        if (bossKeyCard == null)
        {
            ShowMessage(
                "Chưa gán Card phòng Boss."
            );

            return;
        }

        if (bossKeyCard.itemData == null)
        {
            ShowMessage(
                "WorldItem của Card chưa có Item Data."
            );

            return;
        }

        if (InventorySystem.Instance == null)
        {
            ShowMessage(
                "Không tìm thấy InventorySystem."
            );

            return;
        }

        int originalQuantity =
            Mathf.Max(1, bossKeyCard.quantity);

        int leftover =
            InventorySystem.Instance.PickupItem(
                bossKeyCard.itemData,
                originalQuantity
            );

        int pickedAmount =
            originalQuantity - leftover;

        if (pickedAmount <= 0)
        {
            ShowMessage(
                "Không thể thêm Card. Inventory có thể đã đầy."
            );

            return;
        }

        if (leftover <= 0)
        {
            /*
             * Gọi event của WorldItem giống như
             * khi người chơi trực tiếp nhặt Card.
             */
            bossKeyCard.TriggerPickedUp();

            /*
             * Chỉ tắt Card trong scene, không Destroy,
             * để Reference trong Cheat Manager không bị mất.
             */
            bossKeyCard.gameObject.SetActive(false);
        }
        else
        {
            bossKeyCard.quantity = leftover;
        }

        ShowMessage(
            "Đã nhận Card mở phòng Boss."
        );
    }

    // =========================================================
    // NOTIFICATION
    // =========================================================

    private void ShowMessage(string message)
    {
        Debug.Log(
            $"[Developer Cheat] {message}"
        );

        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.ShowNotification(
                message
            );
        }
    }

    // =========================================================
    // CHEAT PANEL
    // =========================================================

    private void OnGUI()
    {
        if (!CheatsAreAllowed || !showCheatPanel)
            return;

        const float panelWidth = 350f;
        const float panelHeight = 455f;

        Rect panelRect = new Rect(
            20f,
            20f,
            panelWidth,
            panelHeight
        );

        GUI.Box(
            panelRect,
            "DEVELOPER CHEATS"
        );

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

        if (GUILayout.Button(
            "Toggle God Mode",
            GUILayout.Height(32f)))
        {
            ToggleGodMode();
        }

        GUILayout.Space(5f);

        GUILayout.Label(
            $"F3 - Infinite Ammo: " +
            $"{(InfiniteAmmo ? "ON" : "OFF")}"
        );

        if (GUILayout.Button(
            "Toggle Infinite Ammo",
            GUILayout.Height(32f)))
        {
            ToggleInfiniteAmmo();
        }

        GUILayout.Space(5f);

        if (GUILayout.Button(
            "F4 - Turn On Hospital Power",
            GUILayout.Height(32f)))
        {
            TurnOnHospitalPower();
        }

        if (GUILayout.Button(
            "F5 - Teleport To Boss Door",
            GUILayout.Height(32f)))
        {
            TeleportToBossDoor();
        }

        if (GUILayout.Button(
            "F6 - Restore Full Health",
            GUILayout.Height(32f)))
        {
            RestoreFullHealth();
        }

        if (GUILayout.Button(
            "F7 - Teleport To Rooftop",
            GUILayout.Height(32f)))
        {
            TeleportToRooftop();
        }

        if (GUILayout.Button(
            "F8 - Get Boss Key Card",
            GUILayout.Height(32f)))
        {
            GetBossKeyCard();
        }

        GUILayout.Space(8f);

        GUILayout.Label(
            "Press F1 to close this panel."
        );

        GUILayout.EndArea();
    }

    private void OnDisable()
    {
        GodMode = false;
        InfiniteAmmo = false;

        if (showCheatPanel)
        {
            RestoreCursorState();
            showCheatPanel = false;
        }
    }
}