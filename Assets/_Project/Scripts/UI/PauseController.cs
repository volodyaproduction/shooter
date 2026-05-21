using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Пауза по ESC во время раунда. Замораживает игру через Time.timeScale,
// показывает оверлей с кнопками «Продолжить» / «В меню». До старта раунда и
// после GameOver неактивна — там работают свои панели.
public class PauseController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public Button resumeButton;
    public Button menuButton;
    public Button openButton;  // бургер в углу — открыть паузу мышью/тапом

    bool paused;

    void OnEnable()
    {
        if (panel != null) panel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(ToMenu);
        if (openButton != null) openButton.onClick.AddListener(RequestPause);
    }

    void OnDisable()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (menuButton != null) menuButton.onClick.RemoveListener(ToMenu);
        if (openButton != null) openButton.onClick.RemoveListener(RequestPause);
        // Подстраховка: возвращаем timeScale при выгрузке сцены на паузе
        if (paused) Time.timeScale = 1f;
    }

    public void RequestPause()
    {
        if (paused) return;
        var session = GameSession.Instance;
        if (session == null || !session.IsPlaying) return;
        Toggle();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        var session = GameSession.Instance;
        if (session == null || !session.IsPlaying) return;
        Toggle();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Авто-пауза при уходе со вкладки: иначе Unity замораживает Update и
        // таймер «висит», выглядит как баг бесконечной игры. По возвращении
        // игрок видит явный экран паузы и продолжает по кнопке.
        if (hasFocus || paused) return;
        var session = GameSession.Instance;
        if (session == null || !session.IsPlaying) return;
        Toggle();
    }

    void Toggle()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        if (panel != null) panel.SetActive(paused);
    }

    void Resume()
    {
        if (paused) Toggle();
    }

    void ToMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
    }
}
