using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

namespace Art_Equilibrium
{
    public class AE_Door : MonoBehaviour
    {
        bool trig, open;
        public float smooth = 2.0f;
        public float DoorOpenAngle = 87.0f;
        private Quaternion defaultRot;
        private Quaternion openRot;
        private Vector3 defaultLocalPos;
        private Vector3 targetLocalSlidePos;

        private bool isKeyPressed;

        [Header("Door Type")]
        public bool isSlidingDoor = false;
        public Vector3 slideOffset = new Vector3(1, 0, 0);

        [Header("GUI Settings")]
        public string openMessage = "Open F";
        public string closeMessage = "Close F";
        public Font messageFont;
        public int fontSize = 24;
        public Color fontColor = Color.white;
        public Vector2 messagePosition = new Vector2(0.5f, 0.5f);

        private string doorMessage = "";

        [Header("Audio Settings")]
        public AudioClip openSound;
        public AudioClip closeSound;
        private AudioSource audioSource;

        [Header("Boss Door Settings")]
        public bool isBossDoor = false;
        public PlayableDirector timelineDirector;
        public Transform bossRoomSpawnPoint;
        public Transform playerTransform;
        public MonoBehaviour playerMovementScript;

        // --- THÊM PHẦN HIỆU ỨNG CHUYỂN CẢNH ---
        [Header("Transition Effects")]
        public CanvasGroup fadeCanvasGroup;
        public float fadeSpeed = 1.5f;

        private bool hasTransitioned = false;

        // ===========================================
        // Khóa mật khẩu 
        // ===========================================
        [Header("Password Lock (chỉ dùng cho cửa cần mật khẩu)")]
        [Tooltip("Bật true nếu cửa này cần mở khóa bằng mật khẩu trước khi cho phép bấm F mở cửa")]
        public bool requiresPassword = false;
        [Tooltip("Chữ hiện khi player đứng gần nhưng CHƯA nhập đúng mật khẩu")]
        public string lockedMessage = "Cần mật khẩu";

        // ===========================================
        // Khóa bằng thẻ từ (Dùng cho phòng Boss)
        // ===========================================
        [Header("Keycard Lock (Thẻ từ)")]
        [Tooltip("Bật true nếu cửa này cần quét thẻ từ để mở")]
        public bool requiresKeycard = false;

        [Tooltip("Kéo file Scriptable Object của chiếc thẻ từ vào đây")]
        public ItemDataSO requiredKeycardSO;

        public AudioClip accessDeniedSound;

        private bool isUnlocked = false;

        private void Start()
        {
            defaultRot = transform.rotation;
            openRot = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + DoorOpenAngle, transform.eulerAngles.z);
            defaultLocalPos = transform.localPosition;
            targetLocalSlidePos = defaultLocalPos + slideOffset;
            isKeyPressed = false;

            audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            // Xử lý Animation cửa mở/đóng
            if (isSlidingDoor)
            {
                Vector3 targetPos = open ? targetLocalSlidePos : defaultLocalPos;
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);
            }
            else
            {
                Quaternion targetRot = open ? openRot : defaultRot;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smooth);
            }

            // Xử lý Tương tác phím F
            if (Input.GetKeyDown(KeyCode.F) && trig && !isKeyPressed)
            {
                isKeyPressed = true;

                // 1. Kiểm tra Cửa Mật Khẩu
                if (requiresPassword && !isUnlocked)
                {
                    return; // Chặn lại, player phải giải puzzle mật khẩu trước
                }

                // 2. Kiểm tra Cửa Thẻ Từ
                if (requiresKeycard)
                {
                    // Ưu tiên 1: Cần có điện
                    if (LightingManager.Instance != null && !LightingManager.Instance.IsPowerOn)
                    {
                        if (NotificationUI.Instance != null)
                            NotificationUI.Instance.ShowNotification("Máy đọc thẻ không hoạt động. Cần mở hệ thống điện.");

                        if (audioSource != null && accessDeniedSound != null)
                            audioSource.PlayOneShot(accessDeniedSound);

                        return; // Chặn lại
                    }

                    // Ưu tiên 2: Điện đã có -> Check túi đồ xem có thẻ không
                    if (InventorySystem.Instance != null && requiredKeycardSO != null)
                    {
                        // Gọi hàm kiểm tra Item trong InventorySystem
                        bool hasCard = InventorySystem.Instance.HasItem(requiredKeycardSO);

                        if (!hasCard)
                        {
                            if (NotificationUI.Instance != null)
                                NotificationUI.Instance.ShowNotification("Truy cập bị từ chối: Yêu cầu Thẻ Từ.");

                            if (audioSource != null && accessDeniedSound != null)
                                audioSource.PlayOneShot(accessDeniedSound);

                            return; // Chặn lại
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[AE_Door] Thiếu InventorySystem.Instance hoặc chưa kéo requiredKeycardSO vào Inspector!");
                    }
                }

                // 3. Nếu mọi điều kiện hợp lệ
                if (isBossDoor)
                {
                    if (!hasTransitioned)
                    {
                        hasTransitioned = true;
                        doorMessage = "";
                        StartCoroutine(TransitionRoutine());
                    }
                }
                else
                {
                    open = !open;
                    PlayDoorSound();
                }
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                isKeyPressed = false;
            }

            // Hiển thị chữ gợi ý trên màn hình
            if (!hasTransitioned)
            {
                if (requiresPassword && !isUnlocked)
                    doorMessage = trig ? lockedMessage : "";
                else
                    doorMessage = trig ? (open ? closeMessage : openMessage) : "";
            }
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(doorMessage))
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = fontSize,
                    normal = { textColor = fontColor }
                };

                if (messageFont != null) style.font = messageFont;

                float screenWidth = Screen.width;
                float screenHeight = Screen.height;
                Vector2 labelSize = style.CalcSize(new GUIContent(doorMessage));
                float labelX = screenWidth * messagePosition.x - labelSize.x / 2;
                float labelY = screenHeight * messagePosition.y - labelSize.y / 2;

                GUI.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), doorMessage, style);
            }
        }

        private void OnTriggerEnter(Collider coll)
        {
            if (coll.CompareTag("Player") && !hasTransitioned)
            {
                doorMessage = open ? closeMessage : openMessage;
                trig = true;
            }
        }

        private void OnTriggerExit(Collider coll)
        {
            if (coll.CompareTag("Player"))
            {
                doorMessage = "";
                trig = false;
            }
        }

        private void PlayDoorSound()
        {
            if (audioSource != null)
            {
                if (open && openSound != null)
                {
                    audioSource.clip = openSound;
                    audioSource.Play();
                }
                else if (!open && closeSound != null)
                {
                    audioSource.clip = closeSound;
                    audioSource.Play();
                }
            }
        }

        // Gọi hàm này từ Keypad khi nhập đúng mật khẩu
        public void UnlockByPassword()
        {
            if (isUnlocked) return;
            isUnlocked = true;
            open = true;
            doorMessage = "";
            PlayDoorSound();
        }

        // ==========================================
        // COROUTINE: XỬ LÝ FADE MÀN HÌNH & CHUYỂN CẢNH
        // ==========================================
        private IEnumerator TransitionRoutine()
        {
            if (playerMovementScript != null) playerMovementScript.enabled = false;

            if (fadeCanvasGroup != null)
            {
                float timer = 0f;
                while (timer < 1f)
                {
                    timer += Time.deltaTime * fadeSpeed;
                    fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(0.2f);

            TeleportPlayer();

            if (timelineDirector != null)
            {
                timelineDirector.Play();
                timelineDirector.stopped += OnCutsceneFinished;
            }

            if (fadeCanvasGroup != null)
            {
                float timer = 0f;
                while (timer < 1f)
                {
                    timer += Time.deltaTime * fadeSpeed;
                    fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 0f;
            }
        }

        private void TeleportPlayer()
        {
            if (playerTransform == null || bossRoomSpawnPoint == null) return;

            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = bossRoomSpawnPoint.position;
            playerTransform.rotation = bossRoomSpawnPoint.rotation;

            if (cc != null) cc.enabled = true;
        }

        private void OnCutsceneFinished(PlayableDirector director)
        {
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }

            if (timelineDirector != null)
            {
                timelineDirector.stopped -= OnCutsceneFinished;
            }
        }
    }
}