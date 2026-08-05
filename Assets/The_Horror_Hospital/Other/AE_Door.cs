using UnityEngine;

namespace Art_Equilibrium
{
    /// <summary>
    /// Điều khiển cửa thường, cửa mật khẩu, cửa thẻ từ và cổng vào boss.
    /// Việc teleport/cutscene boss được giao hoàn toàn cho BossEncounterController.
    /// </summary>
    public class AE_Door : MonoBehaviour
    {
        [Header("Door Movement")]
        [SerializeField] private bool isSlidingDoor;
        [SerializeField] private float smooth = 2f;
        [SerializeField] private float doorOpenAngle = 87f;
        [SerializeField] private Vector3 slideOffset = new Vector3(1f, 0f, 0f);

        [Header("Interaction Text")]
        [SerializeField] private string openMessage = "Open F";
        [SerializeField] private string closeMessage = "Close F";
        [SerializeField] private Font messageFont;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private Color fontColor = Color.white;
        [SerializeField] private Vector2 messagePosition = new Vector2(0.5f, 0.5f);

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip accessDeniedSound;

        [Header("Boss Door")]
        [SerializeField] private bool isBossDoor;
        [SerializeField] private BossEncounterController bossEncounterController;

        [Header("Password Lock")]
        [SerializeField] private bool requiresPassword;
        [SerializeField] private string lockedMessage = "Cần mật khẩu";

        [Header("Keycard Lock")]
        [SerializeField] private bool requiresKeycard;
        [SerializeField] private ItemDataSO requiredKeycardSO;

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

        private void Awake()
        {
            _closedRotation = transform.rotation;
            _openRotation = Quaternion.Euler(
                transform.eulerAngles.x,
                transform.eulerAngles.y + doorOpenAngle,
                transform.eulerAngles.z);

            _closedLocalPosition = transform.localPosition;
            _openLocalPosition = _closedLocalPosition + slideOffset;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            AnimateDoor();

            if (Input.GetKeyDown(KeyCode.F) && _playerInRange && !_interactionHeld)
            {
                _interactionHeld = true;
                TryInteract();
            }

            if (Input.GetKeyUp(KeyCode.F))
                _interactionHeld = false;

            UpdateInteractionMessage();
        }

        private void TryInteract()
        {
            if (_bossEncounterRequested) return;

            if (requiresPassword && !_isUnlocked)
            {
                PlayDeniedSound();
                return;
            }

            if (requiresKeycard && !CanUnlockWithKeycard())
                return;

            _isUnlocked = true;

            if (isBossDoor)
            {
                if (bossEncounterController == null)
                {
                    Debug.LogError("[AE_Door] Cửa boss chưa được gán BossEncounterController.", this);
                    return;
                }

                if (bossEncounterController.StartBossEncounter())
                {
                    _bossEncounterRequested = true;
                    _doorMessage = string.Empty;
                }

                return;
            }

            _open = !_open;
            PlayDoorSound();
        }

        private bool CanUnlockWithKeycard()
        {
            if (LightingManager.Instance != null && !LightingManager.Instance.IsPowerOn)
            {
                NotificationUI.Instance?.ShowNotification(
                    "Máy đọc thẻ không hoạt động. Cần mở hệ thống điện.");
                PlayDeniedSound();
                return false;
            }

            if (InventorySystem.Instance == null || requiredKeycardSO == null)
            {
                Debug.LogWarning(
                    "[AE_Door] Thiếu InventorySystem hoặc chưa gán Required Keycard SO.",
                    this);
                PlayDeniedSound();
                return false;
            }

            if (!InventorySystem.Instance.HasItem(requiredKeycardSO))
            {
                NotificationUI.Instance?.ShowNotification(
                    "Truy cập bị từ chối: Yêu cầu Thẻ Từ.");
                PlayDeniedSound();
                return false;
            }

            return true;
        }

        private void AnimateDoor()
        {
            if (isSlidingDoor)
            {
                Vector3 target = _open ? _openLocalPosition : _closedLocalPosition;
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    target,
                    Time.deltaTime * smooth);
            }
            else
            {
                Quaternion target = _open ? _openRotation : _closedRotation;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    target,
                    Time.deltaTime * smooth);
            }
        }

        private void UpdateInteractionMessage()
        {
            if (!_playerInRange || _bossEncounterRequested)
            {
                _doorMessage = string.Empty;
                return;
            }

            if (requiresPassword && !_isUnlocked)
                _doorMessage = lockedMessage;
            else
                _doorMessage = _open ? closeMessage : openMessage;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !_bossEncounterRequested)
                _playerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInRange = false;
            _doorMessage = string.Empty;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_doorMessage)) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize
            };
            style.normal.textColor = fontColor;

            if (messageFont != null)
                style.font = messageFont;

            Vector2 labelSize = style.CalcSize(new GUIContent(_doorMessage));
            float x = Screen.width * messagePosition.x - labelSize.x * 0.5f;
            float y = Screen.height * messagePosition.y - labelSize.y * 0.5f;
            GUI.Label(new Rect(x, y, labelSize.x, labelSize.y), _doorMessage, style);
        }

        private void PlayDoorSound()
        {
            AudioClip clip = _open ? openSound : closeSound;
            if (_audioSource != null && clip != null)
                _audioSource.PlayOneShot(clip);
        }

        private void PlayDeniedSound()
        {
            if (_audioSource != null && accessDeniedSound != null)
                _audioSource.PlayOneShot(accessDeniedSound);
        }

        /// <summary>Được Keypad gọi sau khi nhập đúng mật khẩu.</summary>
        public void UnlockByPassword()
        {
            if (_isUnlocked) return;
            _isUnlocked = true;

            // Cửa boss là portal nên không xoay/mở model cửa tại đây.
            if (!isBossDoor)
            {
                _open = true;
                PlayDoorSound();
            }
        }
    }
}