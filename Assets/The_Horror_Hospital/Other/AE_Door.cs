using UnityEngine;
using UnityEngine.Playables;
using System.Collections; // Cần thiết để chạy Coroutine (Hiệu ứng theo thời gian)

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
        public CanvasGroup fadeCanvasGroup; // Kéo UI BlackScreen vào đây
        public float fadeSpeed = 1.5f;      // Tốc độ tối/sáng màn hình (càng nhỏ càng chậm)

        private bool hasTransitioned = false;

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

            if (Input.GetKeyDown(KeyCode.F) && trig && !isKeyPressed)
            {
                isKeyPressed = true;

                if (isBossDoor)
                {
                    if (!hasTransitioned)
                    {
                        hasTransitioned = true;
                        doorMessage = "";
                        // Chạy hiệu ứng Fade To Black thay vì chuyển ngay lập tức
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

        // ==========================================
        // COROUTINE: XỬ LÝ FADE MÀN HÌNH & CHUYỂN CẢNH
        // ==========================================
        private IEnumerator TransitionRoutine()
        {
            // 1. Khóa di chuyển của Player ngay khi vừa bấm nút
            if (playerMovementScript != null) playerMovementScript.enabled = false;

            // 2. Màn hình từ từ tối lại
            if (fadeCanvasGroup != null)
            {
                float timer = 0f;
                while (timer < 1f)
                {
                    timer += Time.deltaTime * fadeSpeed;
                    fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer);
                    yield return null; // Chờ tới frame tiếp theo
                }
                fadeCanvasGroup.alpha = 1f; // Đảm bảo đen hoàn toàn
            }

            // Chờ thêm một chút (0.2s) lúc đen thui cho ngầu
            yield return new WaitForSeconds(0.2f);

            // 3. Trong lúc màn hình đang đen, ta âm thầm dịch chuyển Player
            TeleportPlayer();

            // 4. Chạy Timeline Cutscene (Phòng Boss)
            if (timelineDirector != null)
            {
                timelineDirector.Play();
                timelineDirector.stopped += OnCutsceneFinished;
            }

            // 5. Từ từ sáng màn hình lên để lộ ra cảnh Camera đang lia quanh phòng Boss
            if (fadeCanvasGroup != null)
            {
                float timer = 0f;
                while (timer < 1f)
                {
                    timer += Time.deltaTime * fadeSpeed;
                    fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 0f; // Trả lại độ trong suốt 100%
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