using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Панель конца раунда: появляется по событию GameOver, показывает итог,
// предлагает рестарт и (если есть сцена MainMenu в Build Settings) — выход в меню.
// Сохранение в лидерборд (Шаг 2) делает LeaderboardSubmitter — здесь только UI.
public class GameOverPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public Text titleText;
    public Text finalScoreText;
    public Button restartButton;
    public Button menuButton;

    void OnEnable()
    {
        if (root != null) root.SetActive(false);
        if (GameSession.Instance != null)
            GameSession.Instance.GameOver += OnGameOver;
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
    }

    void OnDisable()
    {
        if (GameSession.Instance != null)
            GameSession.Instance.GameOver -= OnGameOver;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (menuButton != null) menuButton.onClick.RemoveListener(ToMenu);
    }

    void OnGameOver(int finalScore)
    {
        if (root != null) root.SetActive(true);
        if (titleText != null) titleText.text = "Время вышло";
        if (finalScoreText != null) finalScoreText.text = $"Итог: {finalScore}";
    }

    void Restart()
    {
        // 1. Простой рестарт: загрузка той же сцены по индексу
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ToMenu()
    {
        // 2. Возврат в меню (сцена MainMenu появится на Шаге 2)
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
