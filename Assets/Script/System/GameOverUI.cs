using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private string gameplaySceneName = "Test_TPP"; // scene chơi hiện tại của bạn

    private void Start()
    {
        retryButton.onClick.AddListener(Retry);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
        quitButton.onClick.AddListener(Quit);
    }

    private void Retry()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void Quit()
    {
        Application.Quit();
    }
}