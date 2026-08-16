using System;
using NavKeypad;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bảng cheat dùng để kiểm thử Dead Roof.
/// F1 là phím duy nhất: mở/đóng bảng. Mọi cheat được bấm trong bảng.
/// Chỉ hoạt động trong Editor, Development Build hoặc khi allowInReleaseBuild = true.
/// </summary>
public class DeveloperCheatManager : MonoBehaviour
{
    public static bool GodMode { get; private set; }
    public static bool InfiniteAmmo { get; private set; }

    private enum CheatTab
    {
        Player,
        Inventory,
        World,
        Teleport,
        GameFlow
    }

    [Serializable]
    private class CheatItem
    {
        public string buttonName = "Get Item";
        public WorldItem worldItem;
        [Min(1)] public int quantity = 1;
    }

    [Serializable]
    private class TeleportLocation
    {
        public string buttonName = "Teleport";
        public Transform destination;
        public bool requireConfirmation;
    }

    [Serializable]
    private class CheatEventAction
    {
        public string buttonName = "Run Action";
        [TextArea] public string successMessage = "Cheat action completed.";
        public bool requireConfirmation;
        public UnityEvent onExecute;
    }

    [Header("Player")]
    [SerializeField] private HealthSystem playerHealth;

    [Tooltip("GameObject gốc của Player có CharacterController.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Các script điều khiển Player cần tạm khóa khi bảng cheat mở. Không bắt buộc.")]
    [SerializeField] private MonoBehaviour[] playerScriptsToDisable;

    [Header("Dead Roof - World")]
    [SerializeField] private MainSwitchInteractable mainSwitch;

    [Header("Dead Roof - Important Items")]
    [Tooltip("Card mở phòng Boss. Cheat sẽ thêm card bằng InventorySystem.")]
    [SerializeField] private WorldItem bossKeyCard;

    [Tooltip("Có thể thêm Rifle, Pistol, Melee, First Aid và các loại đạn tại đây.")]
    [SerializeField] private CheatItem[] inventoryItems;

    [Header("Dead Roof - Main Teleports")]
    [SerializeField] private Transform startRoomTeleportPoint;
    [SerializeField] private Transform electricalRoomTeleportPoint;
    [SerializeField] private Transform bossDoorTeleportPoint;
    [SerializeField] private Transform rooftopTeleportPoint;

    [Tooltip("Các vị trí bổ sung như tầng 1, tầng 2, phòng Security hoặc checkpoint.")]
    [SerializeField] private TeleportLocation[] additionalTeleportLocations;

    [Header("Dead Roof - Quest / World Actions")]
    [Tooltip("Emergency Security Notice dùng để giao ba nhiệm vụ manh mối.")]
    [SerializeField] private ExaminableObject securityNotice;

    [SerializeField] private ExaminableObject receptionRecord;
    [SerializeField] private ExaminableObject isolationReport;
    [SerializeField] private ExaminableObject guardLog;

    [Tooltip("Keypad của Security Office.")]
    [SerializeField] private Keypad securityOfficeKeypad;

    [Tooltip("Nối UnityEvent tới hàm hoàn thành clue, mở cửa hoặc cập nhật objective tương ứng.")]
    [SerializeField] private CheatEventAction[] questAndWorldActions;

    [Header("Dead Roof - Game Flow Actions")]
    [Tooltip("Nối UnityEvent tới skip cutscene, extraction hoặc load End Scene.")]
    [SerializeField] private CheatEventAction[] gameFlowActions;

    [Header("Panel Settings")]
    [SerializeField] private KeyCode panelKey = KeyCode.F1;
    [SerializeField] private bool disablePlayerWhilePanelOpen = true;
    [SerializeField] private bool pauseGameWhilePanelOpen;
    [SerializeField] private Vector2 panelSize = new Vector2(860f, 600f);

    [Header("Build Settings")]
    [Tooltip("Nên để false để cheat không hoạt động trong bản Release.")]
    [SerializeField] private bool allowInReleaseBuild;

    private bool showCheatPanel;
    private CheatTab selectedTab;
    private Vector2 contentScroll;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private float previousTimeScale = 1f;
    private bool[] previousPlayerScriptStates;
    private string lastMessage = "Ready. Press F1 to close the panel.";

    private Action pendingAction;
    private string pendingActionName;

    private GUIStyle titleStyle;
    private GUIStyle tabStyle;
    private GUIStyle selectedTabStyle;
    private GUIStyle sectionStyle;
    private GUIStyle statusStyle;
    private GUIStyle onStyle;
    private GUIStyle offStyle;
    private GUIStyle dangerStyle;

    private bool CheatsAreAllowed =>
        Application.isEditor || Debug.isDebugBuild || allowInReleaseBuild;

    private void Awake()
    {
        if (!CheatsAreAllowed)
        {
            enabled = false;
            return;
        }

        FindMissingReferences();
    }

    private void Update()
    {
        if (Input.GetKeyDown(panelKey))
            ToggleCheatPanel();
    }

    private void FindMissingReferences()
    {
        GameObject playerObject = null;

        if (playerHealth == null || playerTransform == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerHealth == null && playerObject != null)
        {
            playerHealth = playerObject.GetComponent<HealthSystem>();
            if (playerHealth == null)
                playerHealth = playerObject.GetComponentInChildren<HealthSystem>(true);
        }

        if (playerTransform == null && playerObject != null)
        {
            CharacterController controller =
                playerObject.GetComponentInChildren<CharacterController>(true);
            playerTransform = controller != null ? controller.transform : playerObject.transform;
        }

        if (mainSwitch == null)
            mainSwitch = FindObjectOfType<MainSwitchInteractable>(true);
    }

    private void ToggleCheatPanel()
    {
        if (showCheatPanel)
            CloseCheatPanel();
        else
            OpenCheatPanel();
    }

    private void OpenCheatPanel()
    {
        showCheatPanel = true;
        pendingAction = null;
        pendingActionName = null;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseGameWhilePanelOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (disablePlayerWhilePanelOpen)
            SetPlayerScriptsEnabled(false);
    }

    private void CloseCheatPanel()
    {
        showCheatPanel = false;
        pendingAction = null;
        pendingActionName = null;

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        if (pauseGameWhilePanelOpen)
            Time.timeScale = previousTimeScale;

        RestorePlayerScriptStates();
    }

    private void SetPlayerScriptsEnabled(bool value)
    {
        if (playerScriptsToDisable == null)
            return;

        previousPlayerScriptStates = new bool[playerScriptsToDisable.Length];

        for (int i = 0; i < playerScriptsToDisable.Length; i++)
        {
            if (playerScriptsToDisable[i] == null)
                continue;

            previousPlayerScriptStates[i] = playerScriptsToDisable[i].enabled;
            playerScriptsToDisable[i].enabled = value;
        }
    }

    private void RestorePlayerScriptStates()
    {
        if (playerScriptsToDisable == null || previousPlayerScriptStates == null)
            return;

        int count = Mathf.Min(playerScriptsToDisable.Length, previousPlayerScriptStates.Length);
        for (int i = 0; i < count; i++)
        {
            if (playerScriptsToDisable[i] != null)
                playerScriptsToDisable[i].enabled = previousPlayerScriptStates[i];
        }

        previousPlayerScriptStates = null;
    }

    private void ToggleGodMode()
    {
        GodMode = !GodMode;
        if (GodMode)
            RestoreFullHealth(false);

        ShowMessage($"God Mode: {(GodMode ? "ON" : "OFF")}");
    }

    private void ToggleInfiniteAmmo()
    {
        InfiniteAmmo = !InfiniteAmmo;
        ShowMessage($"Infinite Ammo: {(InfiniteAmmo ? "ON" : "OFF")}");
    }

    private void RestoreFullHealth(bool showResult = true)
    {
        if (playerHealth == null)
            FindMissingReferences();

        if (playerHealth == null)
        {
            ShowMessage("Không tìm thấy HealthSystem của Player.");
            return;
        }

        playerHealth.ResetHealth();
        if (showResult)
            ShowMessage("Đã hồi đầy máu cho Player.");
    }

    private void TurnOnHospitalPower()
    {
        if (mainSwitch == null)
            mainSwitch = FindObjectOfType<MainSwitchInteractable>(true);

        if (mainSwitch == null)
        {
            ShowMessage("Không tìm thấy MainSwitchInteractable.");
            return;
        }

        mainSwitch.CheatTurnOnPower();
        ShowMessage("Nguồn điện bệnh viện đã được bật bằng hệ thống thật.");
    }

    private void GetBossKeyCard()
    {
        AddWorldItemToInventory(bossKeyCard, 0, "Boss Key Card");
    }

    private void AddConfiguredItem(CheatItem cheatItem)
    {
        if (cheatItem == null)
            return;

        AddWorldItemToInventory(
            cheatItem.worldItem,
            Mathf.Max(1, cheatItem.quantity),
            cheatItem.buttonName);
    }

    private void AddWorldItemToInventory(WorldItem worldItem, int quantityOverride, string displayName)
    {
        if (worldItem == null)
        {
            ShowMessage($"Chưa gán WorldItem cho: {displayName}.");
            return;
        }

        if (worldItem.itemData == null)
        {
            ShowMessage($"WorldItem của {displayName} chưa có Item Data.");
            return;
        }

        if (InventorySystem.Instance == null)
        {
            ShowMessage("Không tìm thấy InventorySystem.");
            return;
        }

        int requested = quantityOverride > 0
            ? quantityOverride
            : Mathf.Max(1, worldItem.quantity);

        int leftover = InventorySystem.Instance.PickupItem(worldItem.itemData, requested);
        int pickedAmount = requested - leftover;

        if (pickedAmount <= 0)
        {
            ShowMessage($"Không thể thêm {displayName}. Inventory có thể đã đầy.");
            return;
        }

        // Chỉ xử lý object trong scene khi lấy đúng toàn bộ số lượng gốc của WorldItem.
        if (quantityOverride <= 0 && leftover <= 0)
        {
            worldItem.TriggerPickedUp();
            worldItem.gameObject.SetActive(false);
        }

        ShowMessage($"Đã thêm {pickedAmount} x {displayName} vào Inventory.");
    }

    private void TeleportPlayer(Transform destination, string locationName)
    {
        if (playerTransform == null)
            FindMissingReferences();

        if (playerTransform == null)
        {
            ShowMessage("Không tìm thấy Player Transform.");
            return;
        }

        if (destination == null)
        {
            ShowMessage($"Chưa gán Teleport Point: {locationName}.");
            return;
        }

        CharacterController controller =
            playerTransform.GetComponent<CharacterController>();

        if (controller == null)
            controller = playerTransform.GetComponentInChildren<CharacterController>(true);
        if (controller == null)
            controller = playerTransform.GetComponentInParent<CharacterController>();

        bool wasEnabled = controller != null && controller.enabled;
        if (wasEnabled)
            controller.enabled = false;

        playerTransform.SetPositionAndRotation(destination.position, destination.rotation);

        if (wasEnabled)
            controller.enabled = true;

        Physics.SyncTransforms();
        ShowMessage($"Đã dịch chuyển Player đến: {locationName}.");
    }

    private void ExecuteEventAction(CheatEventAction action)
    {
        if (action == null)
            return;

        if (action.onExecute == null || action.onExecute.GetPersistentEventCount() == 0)
        {
            ShowMessage($"Chưa gán UnityEvent cho: {action.buttonName}.");
            return;
        }

        action.onExecute.Invoke();
        ShowMessage(string.IsNullOrWhiteSpace(action.successMessage)
            ? $"Đã thực hiện: {action.buttonName}."
            : action.successMessage);
    }

    private void GiveClueObjectives()
    {
        if (securityNotice == null)
        {
            ShowMessage("Chưa gán Emergency Security Notice.");
            return;
        }

        securityNotice.CheatGiveClueObjectives();
        ShowMessage("Đã giao ba nhiệm vụ tìm manh mối.");
    }

    private void CompleteClue(ExaminableObject clue, string clueName)
    {
        if (clue == null)
        {
            ShowMessage($"Chưa gán tài liệu: {clueName}.");
            return;
        }

        GiveClueObjectivesSilently();
        clue.CheatCompleteOwnObjective();
        ShowMessage($"Đã hoàn thành manh mối: {clueName}.");
    }

    private bool GiveClueObjectivesSilently()
    {
        if (securityNotice == null)
        {
            ShowMessage("Chưa gán Emergency Security Notice.");
            return false;
        }

        securityNotice.CheatGiveClueObjectives();
        return true;
    }

    private void CompleteAllClues()
    {
        if (securityNotice == null ||
            receptionRecord == null ||
            isolationReport == null ||
            guardLog == null)
        {
            ShowMessage("Chưa gán đủ Security Notice và ba tài liệu manh mối.");
            return;
        }

        securityNotice.CheatGiveClueObjectives();
        receptionRecord.CheatCompleteOwnObjective();
        isolationReport.CheatCompleteOwnObjective();
        guardLog.CheatCompleteOwnObjective();

        ShowMessage("Đã hoàn thành toàn bộ ba manh mối.");
    }

    private void GrantKeypadAccess()
    {
        if (securityOfficeKeypad == null)
        {
            ShowMessage("Chưa gán Security Office Keypad.");
            return;
        }

        securityOfficeKeypad.CheatGrantAccess();
        ShowMessage("Security Office Keypad: ACCESS GRANTED.");
    }

    private void RequestConfirmation(string actionName, Action action)
    {
        pendingActionName = actionName;
        pendingAction = action;
    }

    private void ConfirmPendingAction()
    {
        Action action = pendingAction;
        pendingAction = null;
        pendingActionName = null;
        action?.Invoke();
    }

    private void ShowMessage(string message)
    {
        lastMessage = message;
        Debug.Log($"[Developer Cheat] {message}");

        if (NotificationUI.Instance != null)
            NotificationUI.Instance.ShowNotification(message);
    }

    private void OnGUI()
    {
        if (!CheatsAreAllowed || !showCheatPanel)
            return;

        InitializeStyles();

        float width = Mathf.Min(panelSize.x, Screen.width - 30f);
        float height = Mathf.Min(panelSize.y, Screen.height - 30f);
        Rect panelRect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);

        GUI.Box(panelRect, GUIContent.none);

        GUILayout.BeginArea(new Rect(panelRect.x + 14f, panelRect.y + 10f, width - 28f, height - 20f));
        DrawHeader();
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        DrawTabColumn();
        GUILayout.Space(12f);
        DrawContentColumn();
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        DrawFooter();
        GUILayout.EndArea();

        if (pendingAction != null)
            DrawConfirmationPopup(panelRect);
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("DEAD ROOF - DEVELOPER CHEATS", titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("X", dangerStyle, GUILayout.Width(42f), GUILayout.Height(30f)))
            CloseCheatPanel();
        GUILayout.EndHorizontal();
    }

    private void DrawTabColumn()
    {
        GUILayout.BeginVertical(GUILayout.Width(170f));
        DrawTabButton(CheatTab.Player, "PLAYER");
        DrawTabButton(CheatTab.Inventory, "INVENTORY & ITEMS");
        DrawTabButton(CheatTab.World, "QUEST & WORLD");
        DrawTabButton(CheatTab.Teleport, "TELEPORT");
        DrawTabButton(CheatTab.GameFlow, "GAME FLOW");
        GUILayout.FlexibleSpace();
        GUILayout.Label("F1: Close Panel", statusStyle);
        GUILayout.EndVertical();
    }

    private void DrawTabButton(CheatTab tab, string label)
    {
        GUIStyle style = selectedTab == tab ? selectedTabStyle : tabStyle;
        if (GUILayout.Button(label, style, GUILayout.Height(46f)))
        {
            selectedTab = tab;
            contentScroll = Vector2.zero;
            pendingAction = null;
        }
    }

    private void DrawContentColumn()
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        contentScroll = GUILayout.BeginScrollView(contentScroll);

        switch (selectedTab)
        {
            case CheatTab.Player: DrawPlayerTab(); break;
            case CheatTab.Inventory: DrawInventoryTab(); break;
            case CheatTab.World: DrawWorldTab(); break;
            case CheatTab.Teleport: DrawTeleportTab(); break;
            case CheatTab.GameFlow: DrawEventActions("GAME FLOW / TESTING", gameFlowActions); break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawPlayerTab()
    {
        DrawSectionTitle("PLAYER STATUS");

        GUILayout.BeginHorizontal();
        GUILayout.Label("God Mode", GUILayout.Width(180f));
        if (GUILayout.Button(GodMode ? "ON" : "OFF", GodMode ? onStyle : offStyle, GUILayout.Height(38f)))
            ToggleGodMode();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Infinite Ammo", GUILayout.Width(180f));
        if (GUILayout.Button(InfiniteAmmo ? "ON" : "OFF", InfiniteAmmo ? onStyle : offStyle, GUILayout.Height(38f)))
            ToggleInfiniteAmmo();
        GUILayout.EndHorizontal();

        GUILayout.Space(12f);
        if (GUILayout.Button("RESTORE FULL HEALTH", GUILayout.Height(42f)))
            RestoreFullHealth();

        GUILayout.Space(8f);
        GUILayout.Label("Infinite Ammo cần được WeaponInstance/logic bắn kiểm tra DeveloperCheatManager.InfiniteAmmo.", statusStyle);
    }

    private void DrawInventoryTab()
    {
        DrawSectionTitle("IMPORTANT ITEM");
        if (GUILayout.Button("GET BOSS KEY CARD", GUILayout.Height(42f)))
            GetBossKeyCard();

        DrawSectionTitle("WEAPONS, HEALING & AMMO");
        if (inventoryItems == null || inventoryItems.Length == 0)
        {
            DrawEmptyHint("Thêm phần tử vào Inventory Items trong Inspector để tạo nút Rifle, Pistol, Melee, First Aid và Ammo.");
            return;
        }

        foreach (CheatItem item in inventoryItems)
        {
            if (item == null) continue;
            if (GUILayout.Button(item.buttonName, GUILayout.Height(40f)))
                AddConfiguredItem(item);
        }
    }

    private void DrawWorldTab()
    {
        DrawSectionTitle("HOSPITAL SYSTEM");
        if (GUILayout.Button("TURN ON HOSPITAL POWER", GUILayout.Height(42f)))
            TurnOnHospitalPower();

        DrawSectionTitle("SECURITY CLUE OBJECTIVES");

        if (GUILayout.Button("GIVE CLUE OBJECTIVES", GUILayout.Height(40f)))
            GiveClueObjectives();

        if (GUILayout.Button("COMPLETE RECEPTION RECORD", GUILayout.Height(40f)))
            CompleteClue(receptionRecord, "Reception Record");

        if (GUILayout.Button("COMPLETE ISOLATION REPORT", GUILayout.Height(40f)))
            CompleteClue(isolationReport, "Isolation Report");

        if (GUILayout.Button("COMPLETE GUARD LOG", GUILayout.Height(40f)))
            CompleteClue(guardLog, "Guard Log");

        if (GUILayout.Button("COMPLETE ALL CLUES", dangerStyle, GUILayout.Height(40f)))
        {
            RequestConfirmation(
                "Complete all three clue objectives?",
                CompleteAllClues);
        }

        DrawSectionTitle("SECURITY OFFICE");

        if (GUILayout.Button("GRANT KEYPAD ACCESS", dangerStyle, GUILayout.Height(40f)))
        {
            RequestConfirmation(
                "Grant Security Office keypad access?",
                GrantKeypadAccess);
        }

        DrawEventActions("QUESTS, CLUES & DOORS", questAndWorldActions, false);
    }

    private void DrawTeleportTab()
    {
        DrawSectionTitle("MAIN LOCATIONS");
        DrawTeleportButton("START ROOM", startRoomTeleportPoint, false);
        DrawTeleportButton("ELECTRICAL ROOM", electricalRoomTeleportPoint, false);
        DrawTeleportButton("BOSS DOOR", bossDoorTeleportPoint, false);
        DrawTeleportButton("ROOFTOP", rooftopTeleportPoint, true);

        DrawSectionTitle("ADDITIONAL LOCATIONS");
        if (additionalTeleportLocations == null || additionalTeleportLocations.Length == 0)
        {
            DrawEmptyHint("Thêm các Teleport Point của tầng 1, tầng 2, Security Room hoặc checkpoint trong Inspector.");
            return;
        }

        foreach (TeleportLocation location in additionalTeleportLocations)
        {
            if (location == null) continue;
            DrawTeleportButton(location.buttonName, location.destination, location.requireConfirmation);
        }
    }

    private void DrawTeleportButton(string label, Transform destination, bool confirm)
    {
        if (!GUILayout.Button($"TELEPORT: {label}", GUILayout.Height(40f)))
            return;

        if (confirm)
            RequestConfirmation($"Teleport to {label}?", () => TeleportPlayer(destination, label));
        else
            TeleportPlayer(destination, label);
    }

    private void DrawEventActions(string title, CheatEventAction[] actions, bool drawTitle = true)
    {
        if (drawTitle)
            DrawSectionTitle(title);
        else
            DrawSectionTitle(title);

        if (actions == null || actions.Length == 0)
        {
            DrawEmptyHint("Chưa có action. Thêm phần tử trong Inspector và nối On Execute tới hàm public của hệ thống tương ứng.");
            return;
        }

        foreach (CheatEventAction action in actions)
        {
            if (action == null) continue;

            GUIStyle style = action.requireConfirmation ? dangerStyle : GUI.skin.button;
            if (!GUILayout.Button(action.buttonName, style, GUILayout.Height(40f)))
                continue;

            if (action.requireConfirmation)
                RequestConfirmation(action.buttonName, () => ExecuteEventAction(action));
            else
                ExecuteEventAction(action);
        }
    }

    private void DrawSectionTitle(string title)
    {
        GUILayout.Space(6f);
        GUILayout.Label(title, sectionStyle);
        GUILayout.Space(5f);
    }

    private void DrawEmptyHint(string hint)
    {
        GUILayout.Label(hint, statusStyle);
    }

    private void DrawFooter()
    {
        GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(36f));
        GUILayout.Label("STATUS:", sectionStyle, GUILayout.Width(72f));
        GUILayout.Label(lastMessage, statusStyle);
        GUILayout.EndHorizontal();
    }

    private void DrawConfirmationPopup(Rect panelRect)
    {
        const float width = 430f;
        const float height = 155f;
        Rect popup = new Rect(
            panelRect.center.x - width * 0.5f,
            panelRect.center.y - height * 0.5f,
            width,
            height);

        GUI.Box(popup, GUIContent.none);
        GUILayout.BeginArea(new Rect(popup.x + 18f, popup.y + 15f, width - 36f, height - 30f));
        GUILayout.Label("CONFIRM CHEAT ACTION", sectionStyle);
        GUILayout.Space(8f);
        GUILayout.Label(pendingActionName ?? "Run this action?", statusStyle);
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CANCEL", GUILayout.Height(38f)))
        {
            pendingAction = null;
            pendingActionName = null;
        }

        if (GUILayout.Button("CONFIRM", dangerStyle, GUILayout.Height(38f)))
            ConfirmPendingAction();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void InitializeStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 21,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        tabStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        tabStyle.padding.left = 14;

        selectedTabStyle = new GUIStyle(tabStyle);
        selectedTabStyle.normal.textColor = new Color(0.45f, 0.95f, 0.55f);
        selectedTabStyle.fontSize = 13;

        sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft
        };

        onStyle = new GUIStyle(GUI.skin.button);
        onStyle.normal.textColor = new Color(0.35f, 1f, 0.45f);
        onStyle.fontStyle = FontStyle.Bold;

        offStyle = new GUIStyle(GUI.skin.button);
        offStyle.normal.textColor = new Color(1f, 0.45f, 0.45f);
        offStyle.fontStyle = FontStyle.Bold;

        dangerStyle = new GUIStyle(GUI.skin.button);
        dangerStyle.normal.textColor = new Color(1f, 0.55f, 0.4f);
        dangerStyle.fontStyle = FontStyle.Bold;
    }

    private void OnDisable()
    {
        GodMode = false;
        InfiniteAmmo = false;

        if (showCheatPanel)
            CloseCheatPanel();
    }
}