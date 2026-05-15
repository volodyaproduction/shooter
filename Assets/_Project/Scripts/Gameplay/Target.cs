using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Мишень: при клике начисляет очки в GameSession и уничтожается.
// Тип мишени (обычная / ловушка / золотая) задаётся через TargetTypeConfig,
// привязанный в Spawner при инстансе. Реакция-бонус считается по разнице
// между Time.time и моментом спавна.
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Target : MonoBehaviour, IPointerClickHandler
{
    public TargetTypeConfig config;

    SpriteRenderer sr;
    float spawnTime;
    bool clicked;
    float fullScale;

    public void Init(TargetTypeConfig cfg, float lifetime)
    {
        // 1. Запоминаем конфиг и применяем визуал
        config = cfg;
        sr = GetComponent<SpriteRenderer>();
        fullScale = 1f;
        if (cfg != null)
        {
            if (cfg.sprite != null) sr.sprite = cfg.sprite;
            sr.color = cfg.tint;
            fullScale = cfg.scale > 0 ? cfg.scale : 1f;
        }

        // 2. Стартовое время для бонуса за скорость
        spawnTime = Time.time;

        // 3. Авто-уничтожение, если по мишени не успели кликнуть
        Destroy(gameObject, lifetime);

        // 4. Pop-эффект появления (корутиной, без внешних tween-библиотек)
        StartCoroutine(PopIn());
    }

    IEnumerator PopIn()
    {
        const float dur = 0.14f;
        var from = Vector3.one * 0.15f;
        var to = Vector3.one * fullScale;
        transform.localScale = from;
        var t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / dur);
            // EaseOutBack — лёгкий «перелёт» на 12% и возврат
            var s = 1.12f;
            var eased = 1f + (s + 1f) * Mathf.Pow(k - 1f, 3f)
                          + s * Mathf.Pow(k - 1f, 2f);
            transform.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }
        transform.localScale = to;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clicked) return;
        clicked = true;

        // 4. Базовая ценность + бонус за скорость (только для не-ловушек)
        var basePoints = config != null ? config.basePoints : 10;
        int points;
        if (config != null && config.isTrap)
        {
            // Ловушка снимает очки (бонус не применяется)
            points = -Mathf.Abs(basePoints);
        }
        else
        {
            var reaction = Time.time - spawnTime;
            var bonus = GameSession.Instance != null
                ? GameSession.Instance.ComputeReactionBonus(reaction) : 1f;
            points = Mathf.RoundToInt(basePoints * bonus);
        }

        // 5. Эффекты: партикл + звук
        SpawnEffectsAndSfx();

        // 6. Тряска камеры (сильнее для ловушек) + очки в сессию
        var session = GameSession.Instance;
        if (session != null)
        {
            session.Shake(amplitude: config != null && config.isTrap ? 0.28f : 0.12f);
            session.AddScore(points, transform.position);
        }

        Destroy(gameObject);
    }

    void SpawnEffectsAndSfx()
    {
        if (config == null) return;
        if (config.hitEffectPrefab != null)
        {
            Instantiate(
                original: config.hitEffectPrefab,
                position: transform.position,
                rotation: Quaternion.identity);
        }
        var session = GameSession.Instance;
        if (config.hitClip != null && session != null && session.sfxSource != null)
        {
            session.sfxSource.PlayOneShot(config.hitClip);
        }
    }
}
