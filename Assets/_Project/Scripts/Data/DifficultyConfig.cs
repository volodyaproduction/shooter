using UnityEngine;

// Конфиг сложности раунда: интервалы спавна, длительность, ловушки, паддинг.
// Хранится как ScriptableObject. На Шаге 2 — три ассета (Easy/Normal/Hard),
// выбранный пишется в PlayerPrefs (ключ "difficulty").
[CreateAssetMenu(fileName = "DifficultyConfig",
    menuName = "Shooter/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    [Header("Идентификация")]
    public string id = "normal";
    public string displayName = "Нормально";

    [Header("Параметры раунда")]
    [Tooltip("Длительность раунда в секундах")]
    public float roundDuration = 30f;

    [Header("Спавн")]
    [Tooltip("Минимальный интервал между спавнами")]
    public float spawnIntervalMin = 0.6f;
    [Tooltip("Максимальный интервал между спавнами")]
    public float spawnIntervalMax = 1.2f;
    [Tooltip("Время жизни мишени, если по ней не кликнули")]
    public float targetLifetime = 1.8f;

    [Header("Ловушки")]
    [Range(0f, 1f)]
    [Tooltip("Вероятность спавна мишени-ловушки вместо обычной")]
    public float trapChance = 0.15f;

    [Header("Область спавна")]
    [Tooltip("Отступ от краёв камеры по X/Y в мировых единицах")]
    public Vector2 spawnAreaPadding = new Vector2(1.2f, 1.2f);

    [Header("Бонус за скорость")]
    [Tooltip("Множитель за мгновенную реакцию (0.0 сек)")]
    public float fastReactionBonus = 2.0f;
    [Tooltip("За какое время реакции бонус схлопывается до 1.0")]
    public float fastReactionWindow = 0.8f;
}
