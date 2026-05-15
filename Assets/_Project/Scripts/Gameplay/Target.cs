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

    public void Init(TargetTypeConfig cfg, float lifetime)
    {
        // 1. Запоминаем конфиг и применяем визуал
        config = cfg;
        sr = GetComponent<SpriteRenderer>();
        if (cfg != null)
        {
            if (cfg.sprite != null) sr.sprite = cfg.sprite;
            sr.color = cfg.tint;
            if (Mathf.Abs(cfg.scale - 1f) > 0.001f)
                transform.localScale = Vector3.one * cfg.scale;
        }

        // 2. Стартовое время для бонуса за скорость
        spawnTime = Time.time;

        // 3. Авто-уничтожение, если по мишени не успели кликнуть
        Destroy(gameObject, lifetime);
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

        // 6. Передаём очки в сессию
        if (GameSession.Instance != null)
            GameSession.Instance.AddScore(points, transform.position);

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
