using System.Collections;
using UnityEngine;

namespace Art_Equilibrium
{
    /// <summary>
    /// Điều khiển cửa thường, cửa mật khẩu, cửa thẻ từ và cổng boss.
    /// Teleport/cutscene boss do BossEncounterController xử lý.
    ///
    /// Với cửa boss:
    /// - Khi player đến mà chưa có thẻ:
    ///   + Hoàn thành nhiệm vụ tìm đường lên sân thượng.
    ///   + Thông báo cần Level 3 Security Card.
    ///   + Nhận nhiệm vụ tìm Security Office.
    /// </summary>
    public class AE_Door : MonoBehaviour
    {
        private const string FindRooftopRouteObjectiveID =
            "find_rooftop_route";

        private const string LocateSecurityOfficeObjectiveID =
            "locate_security_office";

        private const string LocateSecurityOfficeDescription =
            "Locate the Security Office";

        [Header("Door Movement")]
        [SerializeField] private bool isSlidingDoor;
        [SerializeField] private float smooth = 2f;
        [SerializeField] private float doorOpenAngle = 87f;

        [SerializeField]
        private Vector3 slideOffset =
            new Vector3(1f, 0f, 0f);

        [Header("Interaction Text")]
        [SerializeField] private string openMessage = "[F] Open";
        [SerializeField] private string closeMessage = "[F] Close";

        [SerializeField]
        private string keycardPrompt =
            "Use keycard [F]";

        [SerializeField]
        private string inspectCardReaderPrompt =
        "Inspect card reader [F]";

        [SerializeField] private Font messageFont;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private Color fontColor = Color.white;

        [SerializeField]
        private Vector2 messagePosition =
            new Vector2(0.55f, 0.55f);

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip accessDeniedSound;

        [Header("Boss Door")]
        [SerializeField] private bool isBossDoor;

        [SerializeField]
        private BossEncounterController bossEncounterController;

        [Header("Power Lock")]
        [SerializeField] private bool requiresPower;

        [SerializeField]
        private string noPowerMessage =
            "No power. Restore electricity first.";

        [Header("Password Lock")]
        [SerializeField] private bool requiresPassword;

        [SerializeField]
        private string lockedMessage =
            "Password required";

        [Header("Keycard Lock")]
        [SerializeField] private bool requiresKeycard;
        [SerializeField] private ItemDataSO requiredKeycardSO;

        [SerializeField]
        private string powerRequiredMessage =
            "The card reader is offline. Restore power first.";

        [SerializeField]
        private string keycardRequiredMessage =
            "Level 3 access required. Replacement cards are stored in the Security Office.";

        [Header("Boss Objective")]
        [Tooltip(
            "Thời gian chờ sau khi hoàn thành nhiệm vụ tìm đường " +
            "trước khi hiện nhiệm vụ tìm Security Office."
        )]
        [Min(0f)]
        [SerializeField] private float nextObjectiveDelay = 2f;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;

        private Vector3 _closedLocalPosition;
        private Vector3 _openLocalPosition;

        private AudioSource _audioSource;

        private string _doorMessage = string.Empty;

        private bool _playerInRange;
        private bool _open;
        private bool _interactionHeld;
        private bool _isUnlocked;
        private bool _bossEncounterRequested;

        // Ngăn việc tạo lại objective mỗi lần player nhấn F.
        private bool _bossDoorDiscoveryHandled;

        private Coroutine _nextObjectiveCoroutine;

        private void Awake()
        {
            _closedRotation = transform.rotation;

            _openRotation = Quaternion.Euler(
                transform.eulerAngles.x,
                transform.eulerAngles.y + doorOpenAngle,
                transform.eulerAngles.z
            );

            _closedLocalPosition =
                transform.localPosition;

            _openLocalPosition =
                _closedLocalPosition + slideOffset;

            _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null)
            {
                _audioSource =
                    gameObject.AddComponent<AudioSource>();
            }
        }

        private void Update()
        {
            AnimateDoor();

            if (Input.GetKeyDown(KeyCode.F) &&
                _playerInRange &&
                !_interactionHeld)
            {
                _interactionHeld = true;
                TryInteract();
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                _interactionHeld = false;
            }

            UpdateInteractionMessage();
        }

        private void TryInteract()
        {
            if (_bossEncounterRequested)
            {
                return;
            }

            if (requiresPower &&
                (LightingManager.Instance == null ||
                !LightingManager.Instance.IsPowerOn))
            {
                NotificationUI.Instance
                    ?.ShowNotification(noPowerMessage);

                PlayDeniedSound();
                return;
            }

            if (requiresPassword && !_isUnlocked)
            {
                NotificationUI.Instance
                    ?.ShowNotification(
                        "This door requires a security code."
                    );

                PlayDeniedSound();
                return;
            }

            if (requiresKeycard &&
                !CanUnlockWithKeycard())
            {
                return;
            }

            _isUnlocked = true;

            if (isBossDoor)
            {
                StartBossEncounter();
                return;
            }

            _open = !_open;
            PlayDoorSound();
        }

        private bool CanUnlockWithKeycard()
        {
            if (LightingManager.Instance != null &&
                !LightingManager.Instance.IsPowerOn)
            {
                NotificationUI.Instance
                    ?.ShowNotification(
                        powerRequiredMessage
                    );

                PlayDeniedSound();
                return false;
            }

            if (InventorySystem.Instance == null ||
                requiredKeycardSO == null)
            {
                Debug.LogWarning(
                    "[AE_Door] Thiếu InventorySystem hoặc " +
                    "chưa gán Required Keycard SO.",
                    this
                );

                PlayDeniedSound();
                return false;
            }

            if (!InventorySystem.Instance.HasItem(
                    requiredKeycardSO))
            {
                NotificationUI.Instance
                    ?.ShowNotification(
                        keycardRequiredMessage
                    );

                PlayDeniedSound();

                /*
                 * Chỉ cửa boss mới kích hoạt chuỗi nhiệm vụ này.
                 * Những cửa keycard thông thường không bị ảnh hưởng.
                 */
                if (isBossDoor)
                {
                    HandleBossDoorDiscovery();
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Player đã tìm được tuyến lên tầng 3 nhưng bị cửa boss chặn.
        /// Hoàn thành nhiệm vụ tìm đường và giao nhiệm vụ tìm
        /// Security Office.
        /// </summary>
        private void HandleBossDoorDiscovery()
        {
            if (_bossDoorDiscoveryHandled)
            {
                return;
            }

            _bossDoorDiscoveryHandled = true;

            if (ObjectiveManager.Instance == null)
            {
                Debug.LogWarning(
                    "[AE_Door] Không tìm thấy ObjectiveManager.",
                    this
                );

                return;
            }

            if (ObjectiveManager.Instance.HasObjective(
                    FindRooftopRouteObjectiveID) &&
                !ObjectiveManager.Instance.IsObjectiveCompleted(
                    FindRooftopRouteObjectiveID))
            {
                ObjectiveManager.Instance.CompleteObjective(
                    FindRooftopRouteObjectiveID
                );
            }

            if (_nextObjectiveCoroutine != null)
            {
                StopCoroutine(_nextObjectiveCoroutine);
            }

            _nextObjectiveCoroutine =
                StartCoroutine(
                    GiveSecurityOfficeObjective()
                );
        }

        private IEnumerator GiveSecurityOfficeObjective()
        {
            yield return new WaitForSecondsRealtime(
                nextObjectiveDelay
            );

            if (ObjectiveManager.Instance == null)
            {
                yield break;
            }

            if (!ObjectiveManager.Instance.HasObjective(
                    LocateSecurityOfficeObjectiveID))
            {
                ObjectiveManager.Instance.AddObjective(
                    LocateSecurityOfficeObjectiveID,
                    LocateSecurityOfficeDescription
                );
            }

            _nextObjectiveCoroutine = null;
        }

        private void StartBossEncounter()
        {
            if (bossEncounterController == null)
            {
                Debug.LogError(
                    "[AE_Door] Cửa boss chưa được gán " +
                    "BossEncounterController.",
                    this
                );

                return;
            }

            if (bossEncounterController.StartBossEncounter())
            {
                _bossEncounterRequested = true;
                _doorMessage = string.Empty;
            }
        }

        private void AnimateDoor()
        {
            if (isSlidingDoor)
            {
                Vector3 target =
                    _open
                        ? _openLocalPosition
                        : _closedLocalPosition;

                transform.localPosition =
                    Vector3.Lerp(
                        transform.localPosition,
                        target,
                        Time.deltaTime * smooth
                    );
            }
            else
            {
                Quaternion target =
                    _open
                        ? _openRotation
                        : _closedRotation;

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        target,
                        Time.deltaTime * smooth
                    );
            }
        }

        private void UpdateInteractionMessage()
        {
            if (!_playerInRange ||
                _bossEncounterRequested)
            {
                _doorMessage = string.Empty;
                return;
            }

            if (requiresPower &&
                (LightingManager.Instance == null ||
                 !LightingManager.Instance.IsPowerOn))
            {
                _doorMessage = noPowerMessage;
                return;
            }

            if (requiresPassword && !_isUnlocked)
            {
                _doorMessage = lockedMessage;
                return;
            }

            if (requiresKeycard && !_isUnlocked)
            {
                bool hasRequiredKeycard =
                    InventorySystem.Instance != null &&
                    requiredKeycardSO != null &&
                    InventorySystem.Instance.HasItem(
                        requiredKeycardSO
                    );

                _doorMessage = hasRequiredKeycard
                    ? keycardPrompt
                    : inspectCardReaderPrompt;

                return;
            }

            _doorMessage = _open
                ? closeMessage
                : openMessage;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") &&
                !_bossEncounterRequested)
            {
                _playerInRange = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            _playerInRange = false;
            _doorMessage = string.Empty;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_doorMessage))
            {
                return;
            }

            GUIStyle style =
                new GUIStyle(GUI.skin.label)
                {
                    alignment =
                        TextAnchor.MiddleCenter,

                    fontSize = fontSize
                };

            style.normal.textColor = fontColor;

            if (messageFont != null)
            {
                style.font = messageFont;
            }

            Vector2 labelSize =
                style.CalcSize(
                    new GUIContent(_doorMessage)
                );

            float x =
                Screen.width * messagePosition.x -
                labelSize.x * 0.5f;

            float y =
                Screen.height * messagePosition.y -
                labelSize.y * 0.5f;

            GUI.Label(
                new Rect(
                    x,
                    y,
                    labelSize.x,
                    labelSize.y
                ),
                _doorMessage,
                style
            );
        }

        private void PlayDoorSound()
        {
            AudioClip clip =
                _open
                    ? openSound
                    : closeSound;

            if (_audioSource != null &&
                clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        private void PlayDeniedSound()
        {
            if (_audioSource != null &&
                accessDeniedSound != null)
            {
                _audioSource.PlayOneShot(
                    accessDeniedSound
                );
            }
        }

        /// <summary>
        /// Được Keypad gọi sau khi nhập đúng mật khẩu.
        /// </summary>
        public void UnlockByPassword()
        {
            if (_isUnlocked)
            {
                return;
            }

            _isUnlocked = true;

            // Cửa boss là portal nên không xoay model tại đây.
            if (!isBossDoor)
            {
                _open = true;
                PlayDoorSound();
            }
        }
    }
}