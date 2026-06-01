using UnityEngine;
using TMPro;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private GameObject _crosshair; // Có thể đổi màu crosshair sau này nếu muốn

    private void Awake()
    {
        // Thiết lập Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HidePrompt(); // Mặc định ẩn chữ khi vào game
    }

    public void ShowPrompt(string message)
    {
        if (_promptText == null) return;
        _promptText.text = message;
        _promptText.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (_promptText == null) return;
        _promptText.gameObject.SetActive(false);
    }
}