using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Панель конца раунда. Логика отправки счёта:
//
//   1. Раунд закончен → подписываемся на GameSession.GameOver.
//   2. Если у игрока нет имени — открываем NameInputDialog, ждём успешного
//      ответа от сервера (имя записано в shooter:names), после чего шлём счёт.
//   3. Если имя есть — сразу шлём счёт.
//   4. Сервер возвращает {personalBest, isNewRecord}. Показываем «Счёт: X»
//      + либо «Новый рекорд!», либо «Твой рекорд: Y».
public class GameOverPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public Text titleText;
    public Text finalScoreText;
    public Text recordText;
    public Button restartButton;
    public Button menuButton;
    public Button leaderboardButton;

    [Header("Диалог имени")]
    public NameInputDialog nameDialog;

    int lastScore;

    void OnEnable()
    {
        if (root != null) root.SetActive(false);
        if (GameSession.Instance != null)
            GameSession.Instance.GameOver += OnGameOver;
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(ToLeaderboard);
    }

    void OnDisable()
    {
        if (GameSession.Instance != null)
            GameSession.Instance.GameOver -= OnGameOver;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (menuButton != null) menuButton.onClick.RemoveListener(ToMenu);
        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(ToLeaderboard);
    }

    void OnGameOver(int finalScore)
    {
        lastScore = finalScore;
        if (root != null) root.SetActive(true);
        if (titleText != null) titleText.text = "Время вышло";
        if (finalScoreText != null) finalScoreText.text = $"Счёт: {finalScore}";
        if (recordText != null) recordText.text = "...";

        // 1. Первая игра — сначала имя, потом сабмит. Иначе сразу сабмит.
        if (!PlayerIdentity.HasName())
        {
            if (nameDialog != null)
                nameDialog.OpenForFirstTime(_ => SubmitScore(finalScore));
            else
                SubmitScore(finalScore);    // диалога нет — шлём без имени
        }
        else
        {
            SubmitScore(finalScore);
        }
    }

    void SubmitScore(int score)
    {
        if (LeaderboardClient.Instance == null)
        {
            if (recordText != null) recordText.text = "Сервер недоступен";
            return;
        }
        LeaderboardClient.Instance.SaveScore(score, resp =>
        {
            if (recordText == null) return;
            if (resp == null || !string.IsNullOrEmpty(resp.error))
            {
                // 2. Локализация частых ошибок
                recordText.text = resp != null && resp.error == "rate_limited"
                    ? "Сабмит уже учтён"
                    : "Не удалось отправить счёт";
                return;
            }
            recordText.text = resp.isNewRecord
                ? $"Новый рекорд: {resp.personalBest}!"
                : $"Твой рекорд: {resp.personalBest}";
        });
    }

    void ToLeaderboard()
    {
        if (Application.CanStreamedLevelBeLoaded("Leaderboard"))
            SceneManager.LoadScene("Leaderboard");
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ToMenu()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
    }
}
