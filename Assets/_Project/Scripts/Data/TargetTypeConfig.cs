using UnityEngine;

// Конфиг типа мишени: спрайт, цвет, ценность, эффект, флаг ловушки.
// Два ассета — Normal и Trap. Префабы Target и TrapTarget отличаются только
// привязанным конфигом, что даёт расширяемость до «золотой мишени» и т. п.
[CreateAssetMenu(fileName = "TargetTypeConfig",
    menuName = "Shooter/Target Type Config")]
public class TargetTypeConfig : ScriptableObject
{
    [Header("Идентификация")]
    public string id = "normal";
    public string displayName = "Обычная";

    [Header("Внешний вид")]
    [Tooltip("Если null — спрайт берётся из префаба (плейсхолдер)")]
    public Sprite sprite;
    public Color tint = Color.white;
    [Tooltip("Локальный масштаб мишени относительно префаба")]
    public float scale = 1f;

    [Header("Геймплей")]
    [Tooltip("Базовая ценность в очках")]
    public int basePoints = 10;
    [Tooltip("Если true — клик списывает очки вместо начисления")]
    public bool isTrap = false;

    [Header("Эффекты")]
    [Tooltip("Партикл, инстанцируемый на месте клика")]
    public GameObject hitEffectPrefab;
    [Tooltip("Звук попадания")]
    public AudioClip hitClip;
}
