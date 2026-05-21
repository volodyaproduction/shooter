using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Главное меню: Старт / Имя / Лидерборд / Выход.
// Кнопка имени показывает текущее имя из PlayerPrefs и открывает диалог
// смены (NameInputDialog лежит на этой же сцене и шлёт rename на сервер).
public class MenuController : MonoBehaviour
{
    [Header("Главная панель")]
    public GameObject mainPanel;
    public Button startButton;
    public Button nameButton;
    public Text nameButtonLabel;
    public Button leaderboardButton;
    public Button exitButton;

    [Header("Диалог имени")]
    public NameInputDialog nameDialog;

    void OnEnable()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (nameButton != null) nameButton.onClick.AddListener(OpenNameDialog);
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

        RefreshNameLabel();
    }

    void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
        if (nameButton != null) nameButton.onClick.RemoveListener(OpenNameDialog);
        if (leaderboardButton != null)
            leaderboardButton.onClick.RemoveListener(OpenLeaderboard);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
    }

    void RefreshNameLabel()
    {
        if (nameButtonLabel == null) return;
        var n = PlayerIdentity.GetName();
        nameButtonLabel.text = string.IsNullOrEmpty(n)
            ? "Указать никнейм"
            : $"Мой никнейм {n}";
    }

    void OpenNameDialog()
    {
        if (nameDialog == null) return;
        // 2. В меню — режим смены: диалог проверит уникальность на сервере и
        //    закроется только при успешном rename
        nameDialog.OpenForChange(_ => RefreshNameLabel());
    }

    void StartGame() => SceneManager.LoadScene("Game");

    void OpenLeaderboard()
    {
        if (Application.CanStreamedLevelBeLoaded("Leaderboard"))
            SceneManager.LoadScene("Leaderboard");
    }

    void ExitGame() => Application.Quit();
}
