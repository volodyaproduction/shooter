using System;
using System.Collections;
using UnityEngine;

// Центральная игровая сессия раунда: состояние, счёт, таймер, события.
// Не использует DontDestroyOnLoad — пересоздаётся вместе со сценой Game.
// DefaultExecutionOrder(-100) гарантирует Awake раньше остальных объектов,
// поэтому подписки в OnEnable других компонентов уже находят Instance.
[DefaultExecutionOrder(-100)]
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Конфиг сложности")]
    [Tooltip("Если задан — берётся из PlayerPrefs по ключу 'difficulty'")]
    public DifficultyConfig difficulty;

    [Header("Поиск конфигов сложности (для PlayerPrefs)")]
    [Tooltip("Список всех сложностей; нужная выбирается по id")]
    public DifficultyConfig[] availableDifficulties;

    [Header("Аудио")]
    public AudioSource sfxSource;
    public AudioClip winClip;

    public event Action<int> ScoreChanged;
    public event Action<float> TimeChanged;
    public event Action<int> GameOver;

    public int Score { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsPlaying { get; private set; }
    public DifficultyConfig Difficulty => difficulty;

    void Awake()
    {
        // 1. Singleton (без DontDestroyOnLoad — сцена короткоживущая)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 2. Подменяем difficulty по PlayerPrefs, если выбрана сложность
        ApplyChosenDifficulty();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // 3. Инициализация счёта/таймера + первичная отправка событий
        Score = 0;
        TimeLeft = difficulty != null ? difficulty.roundDuration : 30f;
        IsPlaying = true;
        ScoreChanged?.Invoke(Score);
        TimeChanged?.Invoke(TimeLeft);
        StartCoroutine(TimerLoop());
    }

    void ApplyChosenDifficulty()
    {
        // 4. Подбор конфига по сохранённому id (если есть)
        if (availableDifficulties == null || availableDifficulties.Length == 0)
            return;
        var saved = PlayerPrefs.GetString("difficulty", string.Empty);
        if (string.IsNullOrEmpty(saved)) return;
        foreach (var cfg in availableDifficulties)
        {
            if (cfg != null && cfg.id == saved)
            {
                difficulty = cfg;
                return;
            }
        }
    }

    public void AddScore(int amount, Vector2 worldPos = default)
    {
        if (!IsPlaying) return;
        // 5. Клампим в ноль — отрицательный итоговый счёт неудобен для лидерборда
        Score = Mathf.Max(0, Score + amount);
        ScoreChanged?.Invoke(Score);
    }

    public float ComputeReactionBonus(float reactionTime)
    {
        // 6. Линейный размах: window сек → 1.0, мгновенно → fastReactionBonus
        if (difficulty == null) return 1f;
        var window = Mathf.Max(0.05f, difficulty.fastReactionWindow);
        var t = Mathf.Clamp01(reactionTime / window);
        return Mathf.Lerp(difficulty.fastReactionBonus, 1f, t);
    }

    IEnumerator TimerLoop()
    {
        // 7. Каждый кадр уменьшаем таймер, шлём событие
        while (TimeLeft > 0f && IsPlaying)
        {
            yield return null;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft < 0f) TimeLeft = 0f;
            TimeChanged?.Invoke(TimeLeft);
        }
        EndGame();
    }

    void EndGame()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        TimeChanged?.Invoke(0f);
        if (sfxSource != null && winClip != null)
            sfxSource.PlayOneShot(winClip);
        GameOver?.Invoke(Score);
    }
}
