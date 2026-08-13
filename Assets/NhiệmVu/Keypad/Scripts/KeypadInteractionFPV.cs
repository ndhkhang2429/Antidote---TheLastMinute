using UnityEngine;

namespace NavKeypad
{
    public class KeypadInteractionFPV : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("Camera thật render gameplay, thường là Main Camera.")]
        [SerializeField] private Camera interactionCamera;

        [Header("Raycast")]
        [SerializeField] private float interactionDistance = 10f;

        [Tooltip("Chọn các layer được phép kiểm tra.")]
        [SerializeField] private LayerMask keypadLayers = ~0;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;

        private void Awake()
        {
            FindInteractionCamera();
        }

        private void OnEnable()
        {
            FindInteractionCamera();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (interactionCamera == null)
            {
                FindInteractionCamera();

                if (interactionCamera == null)
                {
                    Debug.LogWarning(
                        "[KeypadInteractionFPV] Không tìm thấy Main Camera.",
                        this
                    );

                    return;
                }
            }

            Ray ray = interactionCamera.ScreenPointToRay(
                Input.mousePosition
            );

            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                interactionDistance,
                keypadLayers,
                QueryTriggerInteraction.Collide
            );

            if (hits.Length == 0)
            {
                if (showDebugLog)
                {
                    Debug.Log(
                        "[KeypadInteractionFPV] Raycast không chạm collider."
                    );
                }

                return;
            }

            System.Array.Sort(
                hits,
                (a, b) => a.distance.CompareTo(b.distance)
            );

            foreach (RaycastHit hit in hits)
            {
                KeypadButton keypadButton =
                    hit.collider.GetComponent<KeypadButton>();

                if (keypadButton == null)
                {
                    keypadButton =
                        hit.collider.GetComponentInParent<KeypadButton>();
                }

                if (keypadButton == null)
                {
                    continue;
                }

                if (showDebugLog)
                {
                    Debug.Log(
                        $"[KeypadInteractionFPV] Đã bấm: " +
                        $"{keypadButton.gameObject.name}",
                        keypadButton
                    );
                }

                keypadButton.PressButton();
                return;
            }

            if (showDebugLog)
            {
                Debug.Log(
                    $"[KeypadInteractionFPV] Chạm '{hits[0].collider.name}' " +
                    "nhưng không tìm thấy KeypadButton."
                );
            }
        }

        private void FindInteractionCamera()
        {
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }
        }
    }
}