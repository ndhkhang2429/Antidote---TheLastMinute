using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverPanel; // Kéo bảng GameOverPanel vào đây

    [Header("Player Reference")]
    public HealthSystem playerHealth; // Kéo object Player của bạn vào đây

    private void Start()
    {
        // 1. Đảm bảo bảng Game Over bị ẩn khi bắt đầu
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // 2. Tìm và liên kết với máu của Player
        ConnectToPlayerHealth();
    }

    private void ConnectToPlayerHealth()
    {
        // Nếu bạn quên kéo thả trong Inspector, code sẽ tự động tìm Player qua Tag
        if (playerHealth == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<HealthSystem>();
            }
            else
            {
                Debug.LogWarning("GameOverManager: Không tìm thấy object nào có tag 'Player'!");
                return;
            }
        }

        // Đăng ký (Subscribe) hàm TriggerGameOver vào sự kiện OnDeath của Player
        if (playerHealth != null)
        {
            playerHealth.OnDeath += TriggerGameOver;
        }
    }

    // Quan trọng: Phải hủy đăng ký khi object này bị xóa để tránh lỗi bộ nhớ (Memory Leak)
    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= TriggerGameOver;
        }
    }

    // Hàm này sẽ tự động chạy khi playerHealth gọi OnDeath?.Invoke()
    private void TriggerGameOver()
    {
        gameOverPanel.SetActive(true); // Hiện bảng YOU DIED
        Time.timeScale = 0f;           // Đóng băng game

        // Mở khóa chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}