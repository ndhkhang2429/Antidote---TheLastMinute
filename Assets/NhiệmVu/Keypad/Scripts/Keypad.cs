using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace NavKeypad
{
    public class Keypad : MonoBehaviour
    {
        private const string LocateSecurityOfficeObjectiveID =
            "locate_security_office";

        private const string EnterSecurityCodeObjectiveID =
            "enter_security_code";

        [Header("Events")]
        [SerializeField] private UnityEvent onAccessGranted;
        [SerializeField] private UnityEvent onAccessDenied;

        [Header("Combination Code")]
        [Tooltip("Mật khẩu tối đa 9 chữ số.")]
        [SerializeField] private int keypadCombo = 1734;

        public UnityEvent OnAccessGranted =>
            onAccessGranted;

        public UnityEvent OnAccessDenied =>
            onAccessDenied;

        [Header("Settings")]
        [SerializeField]
        private string accessGrantedText =
            "Granted";

        [SerializeField]
        private string accessDeniedText =
            "Denied";

        [Header("Visuals")]
        [SerializeField]
        private float displayResultTime = 1f;

        [Range(0f, 5f)]
        [SerializeField]
        private float screenIntensity = 2.5f;

        [Header("Colors")]
        [SerializeField]
        private Color screenNormalColor =
            new Color(0.98f, 0.50f, 0.032f, 1f);

        [SerializeField]
        private Color screenDeniedColor =
            new Color(1f, 0f, 0f, 1f);

        [SerializeField]
        private Color screenGrantedColor =
            new Color(0f, 0.62f, 0.07f, 1f);

        [Header("Sound FX")]
        [SerializeField]
        private AudioClip buttonClickedSfx;

        [SerializeField]
        private AudioClip accessDeniedSfx;

        [SerializeField]
        private AudioClip accessGrantedSfx;

        [Header("Component References")]
        [SerializeField] private Renderer panelMesh;
        [SerializeField] private TMP_Text keypadDisplayText;
        [SerializeField] private AudioSource audioSource;

        [Header("Security Office Discovery")]
        [TextArea(2, 4)]
        [SerializeField]
        private string lockedOfficeMessage =
            "Access denied. A security code is required.";

        private string currentInput = string.Empty;

        private bool displayingResult;
        private bool accessWasGranted;
        private bool securityOfficeDiscovered;

        private void Awake()
        {
            ClearInput();

            if (LightingManager.Instance != null &&
                !LightingManager.Instance.IsPowerOn)
            {
                SetPanelEmission(Color.black);

                if (keypadDisplayText != null)
                {
                    keypadDisplayText.text =
                        string.Empty;
                }
            }
            else
            {
                SetPanelEmission(
                    screenNormalColor *
                    screenIntensity
                );
            }
        }

        /// <summary>
        /// Được KeypadButton gọi khi người chơi nhấn một phím.
        /// </summary>
        public void AddInput(string input)
        {
            if (LightingManager.Instance != null &&
                !LightingManager.Instance.IsPowerOn)
            {
                NotificationUI.Instance
                    ?.ShowNotification(
                        "The keypad has no power. " +
                        "Restore the hospital power first."
                    );

                PlaySound(accessDeniedSfx);
                return;
            }

            if (displayingResult ||
                accessWasGranted)
            {
                return;
            }

            TryDiscoverSecurityOffice();

            PlaySound(buttonClickedSfx);

            switch (input)
            {
                case "enter":
                    CheckCombo();
                    break;

                case "clear":
                    ClearInput();
                    break;

                default:
                    AddNumber(input);
                    break;
            }
        }

        /// <summary>
        /// Thêm chữ số vào màn hình keypad.
        /// </summary>
        private void AddNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            if (currentInput.Length >= 9)
            {
                return;
            }

            currentInput += input;

            if (keypadDisplayText != null)
            {
                keypadDisplayText.text =
                    currentInput;
            }
        }

        /// <summary>
        /// Khi người chơi lần đầu sử dụng keypad,
        /// hoàn thành nhiệm vụ tìm Security Office.
        /// </summary>
        private void TryDiscoverSecurityOffice()
        {
            if (securityOfficeDiscovered)
            {
                return;
            }

            if (ObjectiveManager.Instance == null)
            {
                return;
            }

            if (!ObjectiveManager.Instance.HasObjective(
                    LocateSecurityOfficeObjectiveID))
            {
                return;
            }

            securityOfficeDiscovered = true;

            if (!ObjectiveManager.Instance
                    .IsObjectiveCompleted(
                        LocateSecurityOfficeObjectiveID))
            {
                ObjectiveManager.Instance
                    .CompleteObjective(
                        LocateSecurityOfficeObjectiveID
                    );
            }

            NotificationUI.Instance
                ?.ShowNotification(
                    lockedOfficeMessage
                );
        }

        /// <summary>
        /// Kiểm tra mật khẩu hiện tại.
        /// </summary>
        public void CheckCombo()
        {
            if (displayingResult ||
                accessWasGranted)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(
                    currentInput))
            {
                StartCoroutine(
                    DisplayResultRoutine(false)
                );

                return;
            }

            if (!int.TryParse(
                    currentInput,
                    out int enteredCombination))
            {
                StartCoroutine(
                    DisplayResultRoutine(false)
                );

                return;
            }

            bool granted =
                enteredCombination == keypadCombo;

            StartCoroutine(
                DisplayResultRoutine(granted)
            );
        }

        /// <summary>
        /// Developer Cheat: cấp quyền truy cập ngay.
        /// Cửa và Objective System vẫn được cập nhật đầy đủ.
        /// </summary>
        public void CheatGrantAccess()
        {
            if (accessWasGranted)
            {
                NotificationUI.Instance
                    ?.ShowNotification(
                        "Keypad access was already granted."
                    );

                return;
            }

            StopAllCoroutines();

            displayingResult = false;

            AccessGranted();

            NotificationUI.Instance
                ?.ShowNotification(
                    "Security Office access granted."
                );
        }

        private IEnumerator DisplayResultRoutine(
            bool granted)
        {
            displayingResult = true;

            if (granted)
            {
                AccessGranted();
            }
            else
            {
                AccessDenied();
            }

            yield return new WaitForSecondsRealtime(
                displayResultTime
            );

            displayingResult = false;

            if (granted)
            {
                yield break;
            }

            ClearInput();

            SetPanelEmission(
                screenNormalColor *
                screenIntensity
            );
        }

        private void AccessDenied()
        {
            if (keypadDisplayText != null)
            {
                keypadDisplayText.text =
                    accessDeniedText;
            }

            onAccessDenied?.Invoke();

            SetPanelEmission(
                screenDeniedColor *
                screenIntensity
            );

            PlaySound(accessDeniedSfx);
        }

        /// <summary>
        /// Xử lý toàn bộ kết quả khi nhập đúng mật khẩu.
        /// </summary>
        private void AccessGranted()
        {
            if (accessWasGranted)
            {
                return;
            }

            accessWasGranted = true;

            if (keypadDisplayText != null)
            {
                keypadDisplayText.text =
                    accessGrantedText;
            }

            CompleteEnterCodeObjective();

            onAccessGranted?.Invoke();

            SetPanelEmission(
                screenGrantedColor *
                screenIntensity
            );

            PlaySound(accessGrantedSfx);
        }

        /// <summary>
        /// Hoàn thành nhiệm vụ nhập mã tại Security Office.
        /// </summary>
        private void CompleteEnterCodeObjective()
        {
            if (ObjectiveManager.Instance == null)
            {
                Debug.LogWarning(
                    "[Keypad] Không tìm thấy ObjectiveManager.",
                    this
                );

                return;
            }

            if (!ObjectiveManager.Instance.HasObjective(
                    EnterSecurityCodeObjectiveID))
            {
                Debug.LogWarning(
                    $"[Keypad] Không tìm thấy objective " +
                    $"'{EnterSecurityCodeObjectiveID}'. " +
                    "Hãy kiểm tra lại Objective ID.",
                    this
                );

                return;
            }

            if (ObjectiveManager.Instance
                    .IsObjectiveCompleted(
                        EnterSecurityCodeObjectiveID))
            {
                return;
            }

            ObjectiveManager.Instance
                .CompleteObjective(
                    EnterSecurityCodeObjectiveID
                );

            Debug.Log(
                $"[Keypad] Hoàn thành objective " +
                $"'{EnterSecurityCodeObjectiveID}'.",
                this
            );
        }

        private void ClearInput()
        {
            currentInput = string.Empty;

            if (keypadDisplayText != null)
            {
                keypadDisplayText.text =
                    currentInput;
            }
        }

        private void SetPanelEmission(Color color)
        {
            if (panelMesh == null)
            {
                return;
            }

            panelMesh.material.SetVector(
                "_EmissionColor",
                color
            );
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null &&
                clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}