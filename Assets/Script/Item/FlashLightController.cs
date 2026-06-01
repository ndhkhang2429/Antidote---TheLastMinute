using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Kéo thả object Spotlight (con của Flashlight) vào đây")]
    public Light flashlightSpotlight;

    [Header("Status")]
    [Tooltip("Đánh dấu True khi player đang cầm đèn pin trên tay")]
    public bool isEquipped = false;

    // (Tùy chọn) Thêm âm thanh click cho chân thực
    // public AudioClip clickSound;
    // private AudioSource audioSource;

    void Start()
    {
        // Tự động tìm component Light ở các object con nếu bạn quên chưa gán trong Inspector
        if (flashlightSpotlight == null)
        {
            flashlightSpotlight = GetComponentInChildren<Light>();
        }

        // audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Kiểm tra xem người chơi có đang cầm đèn pin hay không
        if (isEquipped)
        {
            // Kiểm tra click chuột trái (0: trái, 1: phải, 2: giữa)
            if (Input.GetMouseButtonDown(0))
            {
                ToggleFlashlight();
            }
        }
    }

    private void ToggleFlashlight()
    {
        if (flashlightSpotlight != null)
        {
            // Đảo ngược trạng thái của đèn: đang bật -> tắt, đang tắt -> bật
            flashlightSpotlight.enabled = !flashlightSpotlight.enabled;

            /* Bỏ comment đoạn này nếu bạn muốn thêm âm thanh
            if (clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            */
        }
    }

    // Hàm này dùng để gọi từ hệ thống Inventory của bạn khi nhặt hoặc trang bị item
    public void SetEquippedState(bool state)
    {
        isEquipped = state;

        // Tùy chọn: Tắt đèn khi cất vào túi
        if (!isEquipped && flashlightSpotlight != null)
        {
            flashlightSpotlight.enabled = false;
        }
    }
}