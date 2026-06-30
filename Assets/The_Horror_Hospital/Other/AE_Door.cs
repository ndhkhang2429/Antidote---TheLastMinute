using UnityEngine;
using UnityEngine.Playables; // Thêm thư viện để dùng Timeline

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

        // ==========================================
        // THÊM MỚI: TÍNH NĂNG DÀNH RIÊNG CHO BOSS DOOR
        // ==========================================
        [Header("Boss Door Settings")]
        [Tooltip("Tick vào ô này nếu đây là cánh cửa dẫn tới phòng Boss")]
        public bool isBossDoor = false;

        public PlayableDirector timelineDirector;  // Timeline Cutscene
        public Transform bossRoomSpawnPoint;       // Điểm dịch chuyển
        public Transform playerTransform;          // Nhân vật
        public MonoBehaviour playerMovementScript; // Script di chuyển của nhân vật

        private bool hasTransitioned = false;      // Chống bấm F spam nhiều lần
        // ==========================================

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
            // Xử lý hoạt ảnh mở/đóng cửa thông thường (nếu không phải cửa boss, hoặc cửa boss nhưng có animation)
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

            // Xử lý Input
            if (Input.GetKeyDown(KeyCode.F) && trig && !isKeyPressed)
            {
                isKeyPressed = true;

                // KIỂM TRA: NẾU LÀ CỬA BOSS THÌ CHẠY CUTSCENE
                if (isBossDoor)
                {
                    if (!hasTransitioned)
                    {
                        hasTransitioned = true;
                        doorMessage = ""; // Xóa chữ "Open F" trên màn hình
                        StartTransitionSequence();
                    }
                }
                // NẾU LÀ CỬA THƯỜNG THÌ MỞ BÌNH THƯỜNG
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

            // Cập nhật thông báo GUI (Ẩn đi nếu đã vào phòng boss)
            if (!hasTransitioned)
            {
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

                if (messageFont != null)
                {
                    style.font = messageFont;
                }

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

        // ==========================================
        // CÁC HÀM XỬ LÝ DỊCH CHUYỂN & CUTSCENE BOSS
        // ==========================================
        private void StartTransitionSequence()
        {
            // 1. Khóa di chuyển của Player
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = false;
            }

            // 2. Dịch chuyển Player
            TeleportPlayer();

            // 3. Play Timeline Cutscene
            if (timelineDirector != null)
            {
                timelineDirector.Play();
                timelineDirector.stopped += OnCutsceneFinished;
            }
            else
            {
                Debug.LogWarning("Chưa gắn Timeline Director cho Boss Door!");
            }
        }

        private void TeleportPlayer()
        {
            if (playerTransform == null || bossRoomSpawnPoint == null) return;

            // Tắt CharacterController để tránh xung đột vật lý của Unity khi Teleport
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Dịch chuyển
            playerTransform.position = bossRoomSpawnPoint.position;
            playerTransform.rotation = bossRoomSpawnPoint.rotation;

            // Bật lại CharacterController
            if (cc != null) cc.enabled = true;
        }

        private void OnCutsceneFinished(PlayableDirector director)
        {
            // 4. Trả lại quyền di chuyển
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }

            // Hủy đăng ký sự kiện
            if (timelineDirector != null)
            {
                timelineDirector.stopped -= OnCutsceneFinished;
            }

            Debug.Log("Kết thúc Cutscene. Boss Fight bắt đầu!");
        }
    }
}