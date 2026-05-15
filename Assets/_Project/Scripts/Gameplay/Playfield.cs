using UnityEngine;
using UnityEngine.EventSystems;

// Фон игрового поля: ловит клики в пустоту и снимает очки.
// Лежит «под» мишенями по z; верхний коллайдер мишени забирает клик первым,
// поэтому промах фиксируется только если попали мимо.
[RequireComponent(typeof(Collider2D))]
public class Playfield : MonoBehaviour, IPointerClickHandler
{
    [Header("Штраф")]
    [Tooltip("Положительное число; знак минус ставится в коде")]
    public int missPenalty = 5;

    [Header("Эффекты")]
    public GameObject missEffectPrefab;
    public AudioClip missClip;

    public void OnPointerClick(PointerEventData eventData)
    {
        var session = GameSession.Instance;
        if (session == null || !session.IsPlaying) return;

        // 1. Точка клика в мировых координатах для эффекта
        var cam = eventData.pressEventCamera != null
            ? eventData.pressEventCamera : Camera.main;
        var worldPos = cam != null
            ? (Vector2)cam.ScreenToWorldPoint(eventData.position)
            : (Vector2)transform.position;

        // 2. Партикл и звук промаха
        if (missEffectPrefab != null)
        {
            Instantiate(
                original: missEffectPrefab,
                position: new Vector3(worldPos.x, worldPos.y, 0),
                rotation: Quaternion.identity);
        }
        if (missClip != null && session.sfxSource != null)
            session.sfxSource.PlayOneShot(missClip);

        // 3. Снимаем очки
        session.AddScore(-Mathf.Abs(missPenalty), worldPos);
    }
}
