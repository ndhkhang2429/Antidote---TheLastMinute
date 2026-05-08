using UnityEngine;

namespace Art_Equilibrium
{
    public class AE_Drawer : MonoBehaviour
    {
        private bool trig = false;
        private bool open = false;
        private bool isKeyPressed = false;

        public float smooth = 2.0f; // скорость открытия
        public Vector3 openOffset = new Vector3(0, 0, 0.3f); // локальное смещение открытия

        private Vector3 closedLocalPos;
        private Vector3 openedLocalPos;

        [Header("GUI Settings")]
        public string openMessage = "Open E";
        public string closeMessage = "Close E";
        public Font messageFont;
        public int fontSize = 24;
        public Color fontColor = Color.white;
        public Vector2 messagePosition = new Vector2(0.5f, 0.5f);

        private string drawerMessage = "";

        [Header("Audio Settings")]
        public AudioClip openSound;
        public AudioClip closeSound;
        private AudioSource audioSource;

        private void Start()
        {
            closedLocalPos = transform.localPosition;
            openedLocalPos = closedLocalPos + openOffset;

            audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            Vector3 targetLocalPos = open ? openedLocalPos : closedLocalPos;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * smooth);

            if (Input.GetKeyDown(KeyCode.E) && trig && !isKeyPressed)
            {
                open = !open;
                isKeyPressed = true;
                PlaySound();
            }

            if (Input.GetKeyUp(KeyCode.E))
            {
                isKeyPressed = false;
            }

            drawerMessage = trig ? (open ? closeMessage : openMessage) : "";
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(drawerMessage))
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = fontSize;
                style.normal.textColor = fontColor;
                if (messageFont != null)
                    style.font = messageFont;

                float screenWidth = Screen.width;
                float screenHeight = Screen.height;
                Vector2 labelSize = style.CalcSize(new GUIContent(drawerMessage));
                float labelX = screenWidth * messagePosition.x - labelSize.x / 2;
                float labelY = screenHeight * messagePosition.y - labelSize.y / 2;

                GUI.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), drawerMessage, style);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                trig = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                trig = false;
        }

        private void PlaySound()
        {
            if (audioSource != null)
            {
                audioSource.clip = open ? openSound : closeSound;
                if (audioSource.clip != null)
                    audioSource.Play();
            }
        }
    }
}
