using UnityEngine;
using UnityEngine.UI;

// HUD: текст счёта + текст таймера. Подписан на события GameSession.
// Подписка в OnEnable / отписка в OnDisable — стандартный паттерн, не даёт
// дублей при перезагрузке сцены (Awake GameSession отрабатывает раньше за счёт
// атрибута DefaultExecutionOrder(-100)).
public class HUD : MonoBehaviour
{
    [Header("UI-элементы")]
    public Text scoreText;
    public Text timerText;

    void OnEnable()
    {
        var session = GameSession.Instance;
        if (session == null) return;
        session.ScoreChanged += OnScoreChanged;
        session.TimeChanged += OnTimeChanged;
        // 1. Первичный рендер: подтягиваем уже выставленные значения
        OnScoreChanged(session.Score);
        OnTimeChanged(session.TimeLeft);
    }

    void OnDisable()
    {
        var session = GameSession.Instance;
        if (session == null) return;
        session.ScoreChanged -= OnScoreChanged;
        session.TimeChanged -= OnTimeChanged;
    }

    void OnScoreChanged(int score)
    {
        if (scoreText != null) scoreText.text = $"Счёт: {score}";
    }

    void OnTimeChanged(float time)
    {
        // 2. Один знак после запятой — достаточно, не дёргается
        if (timerText != null) timerText.text = $"Время: {time:F1}";
    }
}
