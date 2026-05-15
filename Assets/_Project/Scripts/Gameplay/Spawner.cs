using System.Collections;
using UnityEngine;

// Спавнер мишеней: корутина с переменным интервалом из DifficultyConfig.
// Тип мишени выбирается случайно с учётом trapChance. Координаты — внутри
// видимой области камеры с паддингом.
public class Spawner : MonoBehaviour
{
    [Header("Префаб мишени")]
    public GameObject targetPrefab;

    [Header("Типы мишеней")]
    public TargetTypeConfig normalConfig;
    [Tooltip("Опционально; если null — ловушки выключены")]
    public TargetTypeConfig trapConfig;

    [Header("Камера для расчёта зоны спавна")]
    [Tooltip("Если null — используется Camera.main")]
    public Camera viewCamera;

    Coroutine loop;

    void OnEnable()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        loop = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        // 1. Ждём появления GameSession (на случай порядка инициализации)
        while (GameSession.Instance == null) yield return null;
        var session = GameSession.Instance;

        // 2. Цикл спавна, пока идёт раунд
        while (session.IsPlaying)
        {
            var diff = session.Difficulty;
            var interval = Random.Range(
                diff != null ? diff.spawnIntervalMin : 0.7f,
                diff != null ? diff.spawnIntervalMax : 1.3f);
            yield return new WaitForSeconds(interval);
            if (!session.IsPlaying) yield break;
            SpawnOne(diff);
        }
    }

    void SpawnOne(DifficultyConfig diff)
    {
        // 3. Выбор типа: ловушка с вероятностью trapChance, иначе обычная
        var trapAvailable = trapConfig != null;
        var trapRoll = diff != null && Random.value < diff.trapChance;
        var cfg = (trapAvailable && trapRoll) ? trapConfig : normalConfig;

        // 4. Случайная точка в зоне камеры
        var pos = PickSpawnPoint(diff);

        // 5. Инстанс и инициализация
        var go = Instantiate(
            original: targetPrefab,
            position: pos,
            rotation: Quaternion.identity);
        var target = go.GetComponent<Target>();
        if (target != null)
        {
            var lifetime = diff != null ? diff.targetLifetime : 1.8f;
            target.Init(cfg: cfg, lifetime: lifetime);
        }
    }

    Vector3 PickSpawnPoint(DifficultyConfig diff)
    {
        // 6. Зона = весь видимый прямоугольник минус паддинг
        var padding = diff != null ? diff.spawnAreaPadding : new Vector2(1f, 1f);
        var cam = viewCamera != null ? viewCamera : Camera.main;
        var lt = cam.ViewportToWorldPoint(
            new Vector3(0, 0, Mathf.Abs(cam.transform.position.z)));
        var rt = cam.ViewportToWorldPoint(
            new Vector3(1, 1, Mathf.Abs(cam.transform.position.z)));
        var x = Random.Range(lt.x + padding.x, rt.x - padding.x);
        var y = Random.Range(lt.y + padding.y, rt.y - padding.y);
        return new Vector3(x, y, 0f);
    }
}
