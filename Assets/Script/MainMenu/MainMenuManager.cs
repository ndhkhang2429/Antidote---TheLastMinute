using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Các Màn Hình UI")]
    public GameObject screenPressStart;
    public GameObject screenMainMenu;
    public GameObject screenSettings;

    [Header("Scene")]
    [SerializeField]
    private string gameplaySceneName =
        "Test_TPP";

    [Header("Music")]
    [SerializeField]
    private MenuMusicController menuMusic;

    private bool _isLoadingGame;

    private void Start()
    {
        // Đảm bảo game không còn bị pause
        // khi quay lại Main Menu.
        Time.timeScale = 1f;

        screenPressStart.SetActive(true);
        screenMainMenu.SetActive(false);
        screenSettings.SetActive(false);
    }

    public void GoToMainMenu()
    {
        screenPressStart.SetActive(false);
        screenMainMenu.SetActive(true);
    }

    public void PlayNewGame()
    {
        if (_isLoadingGame)
        {
            return;
        }

        StartCoroutine(
            LoadGameplayScene()
        );
    }

    private IEnumerator LoadGameplayScene()
    {
        _isLoadingGame = true;

        if (menuMusic != null)
        {
            yield return menuMusic.FadeOut();
        }

        SceneManager.LoadScene(
            gameplaySceneName
        );
    }

    public void OpenOptions()
    {
        screenMainMenu.SetActive(false);
        screenSettings.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Đang thoát game...");
        Application.Quit();
    }

    public void CloseOptions()
    {
        screenSettings.SetActive(false);
        screenMainMenu.SetActive(true);
    }
}