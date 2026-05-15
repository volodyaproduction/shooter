using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Главное меню: панель с кнопками Старт / Сложность / Лидерборд / Выход
// и подпанель выбора сложности (Легко / Нормально / Сложно / Назад).
// Выбранная сложность хранится в PlayerPrefs["difficulty"].
public class MenuController : MonoBehaviour
{
    [Header("Главная панель")]
    public GameObject mainPanel;
    public Button startButton;
    public Button difficultyButton;
    public Text difficultyButtonLabel;
    public Button leaderboardButton;
    public Button exitButton;

    [Header("Панель выбора сложности")]
    public GameObject difficultyPanel;
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;
    public Button backButton;

    const string PrefKey = "difficulty";

    void OnEnable()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (difficultyButton != null)
            difficultyButton.onClick.AddListener(OpenDifficulty);
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(OpenLeaderboard);
        if (exitButton != null)
        {
            // 1. В WebGL Application.Quit() ничего не делает — скрываем кнопку
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                exitButton.gameObject.SetActive(false);
            else
                exitButton.onClick.AddListener(ExitGame);
        }
        if (easyButton != null) easyButton.onClick.AddListener(() => SetDifficulty("easy"));
        if (normalButton != null) normalButton.onClick.AddListener(() => SetDifficulty("normal"));
        if (hardButton != null) hardButton.onClick.AddListener(() => SetDifficulty("hard"));
        if (backButton != null) backButton.onClick.AddListener(BackToMain);

        ShowMain();
        RefreshDifficultyLabel();
    }

    void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
        if (difficultyButton != null)
            difficultyButton.onClick.RemoveListener(OpenDifficulty);
        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(OpenLeaderboard);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
        if (easyButton != null) easyButton.onClick.RemoveAllListeners();
        if (normalButton != null) normalButton.onClick.RemoveAllListeners();
        if (hardButton != null) hardButton.onClick.RemoveAllListeners();
        if (backButton != null) backButton.onClick.RemoveListener(BackToMain);
    }

    void ShowMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
    }

    void OpenDifficulty()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(true);
    }

    void BackToMain()
    {
        ShowMain();
        RefreshDifficultyLabel();
    }

    void SetDifficulty(string id)
    {
        PlayerPrefs.SetString(PrefKey, id);
        PlayerPrefs.Save();
        BackToMain();
    }

    void RefreshDifficultyLabel()
    {
        if (difficultyButtonLabel == null) return;
        var saved = PlayerPrefs.GetString(PrefKey, "normal");
        var name = saved == "easy" ? "Легко"
                 : saved == "hard" ? "Сложно"
                 : "Нормально";
        difficultyButtonLabel.text = $"Сложность: {name}";
    }

    void StartGame() => SceneManager.LoadScene("Game");

    void OpenLeaderboard()
    {
        // 2. Лидерборд появится в Шаге 2.4. Защитная проверка — иначе
        //    SceneManager упадёт на отсутствующей сцене.
        if (Application.CanStreamedLevelBeLoaded("Leaderboard"))
            SceneManager.LoadScene("Leaderboard");
    }

    void ExitGame() => Application.Quit();
}
