using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Quản lý Panels Giao Diện")]
    public GameObject pausePanel;    // Kéo object PausePanel vào đây
    public GameObject settingsPanel; // Kéo object SettingsPanel vào đây

    private bool isPaused = false;

    void Update()
    {
        // Kiểm tra nếu người chơi bấm nút ESC trên bàn phím
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                // Nếu đang ở bảng Settings mà bấm ESC -> Quay lại bảng Pause chính
                if (settingsPanel.activeSelf)
                {
                    CloseOptions();
                }
                // Nếu đang ở bảng Pause chính mà bấm ESC -> Tắt Menu và chơi tiếp
                else
                {
                    ResumeGame();
                }
            }
            else
            {
                PauseGame();
            }
        }
    }

    // 1. Chức năng TIẾP TỤC CHƠI (Dùng cho nút CONTINUE hoặc bấm ESC)
    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false); // Đảm bảo tắt sạch toàn bộ các bảng UI
        Time.timeScale = 1f;            // Kích hoạt lại thời gian thực cho game
        isPaused = false;

        // Khóa con trỏ chuột và ẩn đi để người chơi quay lại điều khiển camera 3D
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Hàm nội bộ xử lý việc tạm dừng game
    private void PauseGame()
    {
        pausePanel.SetActive(true);  // Hiện bảng pause chính
        settingsPanel.SetActive(false);
        Time.timeScale = 0f;         // Đóng băng toàn bộ thời gian và chuyển động trong game
        isPaused = true;

        // Giải phóng con trỏ chuột và hiển thị lên để người chơi click menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 2. Chức năng LƯU VÀ THOÁT GAME (Dùng cho nút SAVE & QUIT)
    public void SaveAndQuitGame()
    {
        // Nơi chạy lệnh lưu game thực tế của bạn sau này (Ví dụ: SaveSystem.Save())
        Debug.Log("Hệ thống: Đang tiến hành lưu tiến trình game của người chơi...");

        // BẮT BUỘC: Trả lại Time.timeScale bằng 1 trước khi chuyển cảnh
        // Nếu quên dòng này, khi sang Scene MainMenu toàn bộ hệ thống UI ngoài đó cũng sẽ bị đóng băng theo!
        Time.timeScale = 1f;

        Debug.Log("Hệ thống: Lưu thành công! Đang chuyển hướng về Main Menu.");
        SceneManager.LoadScene("MainMenu"); // Hãy điền chính xác tên Scene Menu chính của bạn ở đây
    }

    // 3. Chức năng MỞ BẢNG OPTIONS (Dùng cho nút OPTIONS)
    public void OpenOptions()
    {
        pausePanel.SetActive(false);   // Tạm thời ẩn bảng Pause chính
        settingsPanel.SetActive(true);  // Hiện bảng tùy chỉnh âm thanh/ánh sáng lên
    }

    // 4. Chức năng ĐÓNG BẢNG OPTIONS (Dùng cho nút BACK hoặc khi bấm ESC)
    public void CloseOptions()
    {
        settingsPanel.SetActive(false); // Ẩn bảng tùy chỉnh đi
        pausePanel.SetActive(true);    // Hiện lại bảng Pause chính
    }
}