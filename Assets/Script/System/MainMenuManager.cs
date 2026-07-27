using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Các Màn Hình UI")]
    public GameObject screenPressStart; // Màn hình 1: Có tên game và chữ Start
    public GameObject screenMainMenu;   // Màn hình 2: New Game, Load, Options...
    public GameObject screenSettings;   // Màn hình 3: Bảng chỉnh âm thanh/ánh sáng

    private void Start()
    {
        // Vừa vào game: Bật màn hình 1, tắt màn hình 2 và 3
        screenPressStart.SetActive(true);
        screenMainMenu.SetActive(false);
        screenSettings.SetActive(false);
    }

    // --- HÀM CHO MÀN HÌNH 1 ---
    public void GoToMainMenu()
    {
        // Khi bấm Start ở màn hình ngoài: Tắt nó đi, bật Menu chính lên
        screenPressStart.SetActive(false);
        screenMainMenu.SetActive(true);
    }

    // --- CÁC HÀM CHO MÀN HÌNH 2 (MAIN MENU) ---
    public void PlayNewGame()
    {
        // Chuyển scene vào game
        SceneManager.LoadScene("Test_TPP"); // Nhớ đổi đúng tên Scene game của bạn
    }

    public void OpenOptions()
    {
        // Ẩn Menu chính, Hiện bảng Settings
        screenMainMenu.SetActive(false);
        screenSettings.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Đang thoát game...");
        Application.Quit();
    }

    // --- HÀM CHO MÀN HÌNH 3 (SETTINGS) ---
    public void CloseOptions()
    {
        // Tắt bảng Settings, Hiện lại Menu chính
        screenSettings.SetActive(false);
        screenMainMenu.SetActive(true);
    }
}