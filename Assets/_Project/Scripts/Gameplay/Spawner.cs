using UnityEngine;
using UnityEngine.UI;

// Спавнер мишеней: тикает в Update, спавнит мишень когда Time.time достигает
// nextSpawnTime. От корутины отказались — в WebGL/IL2CPP корутины иногда
// «умирают» после первого WaitForSeconds или MissingReference, оставляя
// игру без новых мишеней; Update же гарантированно вызывается каждый кадр.
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

    [Header("Дебаг")]
    [Tooltip("Статус-индикатор на экране для WebGL без DevTools")]
    public Text statusText;

    int spawnAttempts;
    int spawnDone;
    float lastSpawnTime;
    float nextSpawnTime;
    string lastError = "";

    void Update()
    {
        TickSpawn();
        UpdateStatusText();
    }

    void TickSpawn()
    {
        // 1. Подготовка ссылок
        var session = GameSession.Instance;
        if (session == null) return;

        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera == null) return;

        if (targetPrefab == null || normalConfig == null)
        {
            lastError = "prefab/cfg=NULL";
            return;
        }

        // 2. Раунд не идёт — таймер «замораживается» до старта/рестарта
        if (!session.IsPlaying)
        {
            nextSpawnTime = 0f;
            return;
        }

        // 3. Первый кадр в активной игре — спавн сразу
        if (nextSpawnTime <= 0.0001f)
        {
            SpawnOne(session.Difficulty);
            ScheduleNext(session.Difficulty);
            return;
        }

        // 4. Иначе ждём, пока Time.time не пересечёт nextSpawnTime
        if (Time.time >= nextSpawnTime)
        {
            SpawnOne(session.Difficulty);
            ScheduleNext(session.Difficulty);
        }
    }

    void ScheduleNext(DifficultyConfig diff)
    {
        var interval = Random.Range(
            diff != null ? diff.spawnIntervalMin : 0.7f,
            diff != null ? diff.spawnIntervalMax : 1.3f);
        nextSpawnTime = Time.time + interval;
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;
        var sinceSpawn = lastSpawnTime > 0 ? Time.time - lastSpawnTime : -1f;
        statusText.text =
            $"session={(GameSession.Instance != null ? "ok" : "NULL")} "
          + $"cam={(viewCamera != null ? "ok" : "?")} "
          + $"try={spawnAttempts} done={spawnDone} "
          + $"since={sinceSpawn:F1}s t={Time.time:F1}"
          + (string.IsNullOrEmpty(lastError) ? "" : $" err={lastError}");
    }

    void SpawnOne(DifficultyConfig diff)
    {
        spawnAttempts++;
        try
        {
            // 5. Выбор типа: ловушка с вероятностью trapChance, иначе обычная
            var trapAvailable = trapConfig != null;
            var trapRoll = diff != null && Random.value < diff.trapChance;
            var cfg = (trapAvailable && trapRoll) ? trapConfig : normalConfig;

            // 6. Случайная точка в зоне камеры
            var pos = PickSpawnPoint(diff);

            // 7. Инстанс и инициализация
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

            spawnDone++;
            lastSpawnTime = Time.time;
        }
        catch (System.Exception e)
        {
            lastError = e.GetType().Name + ":" + e.Message;
            Debug.LogError("[Spawner] " + lastError);
        }
    }

    Vector3 PickSpawnPoint(DifficultyConfig diff)
    {
        // 8. Зона = весь видимый прямоугольник минус паддинг
        var padding = diff != null ? diff.spawnAreaPadding : new Vector2(1f, 1f);
        var cam = viewCamera != null ? viewCamera : Camera.main;
        var dist = Mathf.Abs(cam.transform.position.z);
        var lt = cam.ViewportToWorldPoint(new Vector3(0, 0, dist));
        var rt = cam.ViewportToWorldPoint(new Vector3(1, 1, dist));
        var x = Random.Range(lt.x + padding.x, rt.x - padding.x);
        var y = Random.Range(lt.y + padding.y, rt.y - padding.y);
        return new Vector3(x, y, -0.5f);  // ближе к камере, чем Background (z=0)
    }
}
