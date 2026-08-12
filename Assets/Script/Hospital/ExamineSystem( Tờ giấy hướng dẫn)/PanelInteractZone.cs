using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class PanelInteractZone : MonoBehaviour
{
    [Header("Cinemachine")]
    public CinemachineVirtualCamera playerVCam;
    public CinemachineVirtualCamera panelVCam;

    [Header("Prompt")]
    public string enterPrompt = "Enter Keypad";
    public string exitPrompt = "Quit";

    [Tooltip(
        "Bật nếu vẫn muốn hiện dòng Quit khi đang nhập keypad. " +
        "Nếu InteractionUI cũng bị ẩn thì dòng này sẽ không thấy."
    )]
    [SerializeField] private bool showExitPrompt = true;

    [Header("Player References")]
    public GameObject playerObject;

    [Tooltip("StarterAssetsInputs hoặc PlayerInput đang sử dụng.")]
    public MonoBehaviour playerInputComponent;

    [Tooltip("FirstPersonController hoặc script điều khiển player.")]
    public MonoBehaviour firstPersonController;

    [Header("Settings")]
    [Min(0f)]
    public float blendWaitTime = 0.8f;

    [Header("Hide While In Panel Mode")]
    [Tooltip(
        "Những UI sẽ tạm ẩn khi sử dụng keypad. " +
        "Không kéo Canvas hoặc object cha của keypad vào đây."
    )]
    [SerializeField]
    private List<GameObject> objectsToHideWhileInPanelMode =
        new List<GameObject>();

    public bool IsInPanelMode { get; private set; }

    public System.Action OnEnterPanelMode;
    public System.Action OnExitPanelMode;

    private bool _isTransitioning;

    /*
     * Lưu trạng thái ban đầu của từng object.
     * Object vốn tắt sẽ tiếp tục tắt sau khi thoát keypad.
     */
    private readonly Dictionary<GameObject, bool>
        _previousObjectStates =
            new Dictionary<GameObject, bool>();

    private void Start()
    {
        if (panelVCam != null)
        {
            panelVCam.Priority = 0;
        }

        if (playerVCam != null)
        {
            playerVCam.Priority = 10;
        }
    }

    public void TogglePanelMode()
    {
        if (_isTransitioning)
        {
            return;
        }

        if (!IsInPanelMode)
        {
            StartCoroutine(EnterPanelMode());
        }
        else
        {
            StartCoroutine(ExitPanelMode());
        }
    }

    private IEnumerator EnterPanelMode()
    {
        _isTransitioning = true;

        /*
         * Xóa prompt tương tác trước khi ẩn UI.
         */
        InteractionUIManager.Instance?.HidePrompt();

        HideConfiguredObjects();
        SetPlayerControl(false);

        if (panelVCam != null)
        {
            panelVCam.Priority = 20;
        }

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, blendWaitTime)
        );

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        IsInPanelMode = true;
        _isTransitioning = false;

        /*
         * Chỉ hiện được nếu InteractionUI không nằm
         * trong danh sách object bị ẩn.
         */
        if (showExitPrompt)
        {
            InteractionUIManager.Instance
                ?.ShowPrompt(exitPrompt);
        }

        OnEnterPanelMode?.Invoke();
    }

    private IEnumerator ExitPanelMode()
    {
        _isTransitioning = true;

        InteractionUIManager.Instance?.HidePrompt();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (panelVCam != null)
        {
            panelVCam.Priority = 0;
        }

        if (playerVCam != null)
        {
            playerVCam.Priority = 10;
        }

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, blendWaitTime)
        );

        SetPlayerControl(true);

        IsInPanelMode = false;
        _isTransitioning = false;

        RestoreConfiguredObjects();

        OnExitPanelMode?.Invoke();
    }

    private void SetPlayerControl(bool enabledValue)
    {
        /*
         * Tắt/mở script di chuyển trước.
         */
        if (firstPersonController != null)
        {
            firstPersonController.enabled =
                enabledValue;
        }

        /*
         * Sau đó mới xử lý CharacterController.
         */
        if (playerObject != null)
        {
            CharacterController characterController =
                playerObject.GetComponent<CharacterController>();

            if (characterController != null)
            {
                characterController.enabled =
                    enabledValue;
            }
        }

        if (playerInputComponent != null)
        {
            playerInputComponent.enabled =
                enabledValue;
        }
    }

    private void HideConfiguredObjects()
    {
        _previousObjectStates.Clear();

        foreach (
            GameObject target
            in objectsToHideWhileInPanelMode)
        {
            if (target == null)
            {
                continue;
            }

            /*
             * Tránh lưu cùng một object hai lần.
             */
            if (_previousObjectStates.ContainsKey(target))
            {
                continue;
            }

            _previousObjectStates.Add(
                target,
                target.activeSelf
            );

            target.SetActive(false);
        }
    }

    private void RestoreConfiguredObjects()
    {
        foreach (
            KeyValuePair<GameObject, bool> pair
            in _previousObjectStates)
        {
            if (pair.Key != null)
            {
                pair.Key.SetActive(pair.Value);
            }
        }

        _previousObjectStates.Clear();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        /*
         * Bảo đảm player và UI không bị khóa vĩnh viễn
         * nếu PanelInteractZone bị tắt giữa chừng.
         */
        if (IsInPanelMode || _isTransitioning)
        {
            if (panelVCam != null)
            {
                panelVCam.Priority = 0;
            }

            if (playerVCam != null)
            {
                playerVCam.Priority = 10;
            }

            SetPlayerControl(true);
            RestoreConfiguredObjects();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            IsInPanelMode = false;
            _isTransitioning = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (panelVCam == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        Gizmos.DrawSphere(
            panelVCam.transform.position,
            0.1f
        );

        Gizmos.DrawLine(
            transform.position,
            panelVCam.transform.position
        );

        Gizmos.color = Color.blue;

        Gizmos.DrawRay(
            panelVCam.transform.position,
            panelVCam.transform.forward * 0.5f
        );
    }
}