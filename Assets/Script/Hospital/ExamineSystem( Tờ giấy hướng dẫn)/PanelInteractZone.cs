using System.Collections;
using UnityEngine;
using Cinemachine;

public class PanelInteractZone : MonoBehaviour
{
    [Header("Cinemachine")]
    public CinemachineVirtualCamera playerVCam;
    public CinemachineVirtualCamera panelVCam;

    [Header("Prompt")]
    public string enterPrompt = "[F] Kiểm tra bảng điện";
    public string exitPrompt = "[F] Thoát";

    [Header("Player References")]
    public GameObject playerObject;
    public MonoBehaviour playerInputComponent;  // StarterAssetsInputs
    public MonoBehaviour thirdPersonController; // ThirdPersonController - THÊM MỚI

    [Header("Settings")]
    public float blendWaitTime = 0.8f;

    public bool IsInPanelMode { get; private set; } = false;
    private bool _isTransitioning = false;

    public System.Action OnEnterPanelMode;
    public System.Action OnExitPanelMode;

    void Start()
    {
        if (panelVCam != null) panelVCam.Priority = 0;
        if (playerVCam != null) playerVCam.Priority = 10;
    }

    public void TogglePanelMode()
    {
        if (_isTransitioning) return;

        if (!IsInPanelMode)
            StartCoroutine(EnterPanelMode());
        else
            StartCoroutine(ExitPanelMode());
    }

    IEnumerator EnterPanelMode()
    {
        _isTransitioning = true;

        panelVCam.Priority = 20;
        SetPlayerControl(false); // lock player

        yield return new WaitForSeconds(blendWaitTime);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        IsInPanelMode = true;
        _isTransitioning = false;

        InteractionUIManager.Instance?.ShowPrompt(exitPrompt);
        OnEnterPanelMode?.Invoke();
    }

    IEnumerator ExitPanelMode()
    {
        _isTransitioning = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        panelVCam.Priority = 0;

        yield return new WaitForSeconds(blendWaitTime);

        SetPlayerControl(true); // unlock sau khi blend xong
        IsInPanelMode = false;
        _isTransitioning = false;

        InteractionUIManager.Instance?.HidePrompt();
        OnExitPanelMode?.Invoke();
    }

    void SetPlayerControl(bool enabled)
    {
        // 1. Disable ThirdPersonController TRƯỚC (ngăn nó gọi Move)
        if (thirdPersonController != null)
            thirdPersonController.enabled = enabled;

        // 2. Sau đó mới disable CharacterController
        if (playerObject != null)
        {
            var cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = enabled;
        }

        // 3. Input
        if (playerInputComponent != null)
            playerInputComponent.enabled = enabled;
    }

    void OnDrawGizmosSelected()
    {
        if (panelVCam != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(panelVCam.transform.position, 0.1f);
            Gizmos.DrawLine(transform.position, panelVCam.transform.position);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(panelVCam.transform.position, panelVCam.transform.forward * 0.5f);
        }
    }
}