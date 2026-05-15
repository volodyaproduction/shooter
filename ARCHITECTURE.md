# Архитектура

Документ описывает ключевые решения, их обоснование, и возможные точки
расширения. Цель — чтобы любой агент или человек, получив проект на руки, за
10 минут понял что где лежит и почему.

---

## Стек и базовые решения

| Слой | Решение |
|---|---|
| Render Pipeline | **2D + Built-in (BiRP)**. URP даёт +2.5–3 МБ к пустому WebGL-билду на ровном месте, для 2D-кликера никаких преимуществ нет |
| Ввод | `Active Input Handling = Both`. Гарантирует стабильный `EventSystem` со старым `StandaloneInputModule` и оставляет дверь открытой для New Input System |
| UI | **Legacy UGUI** (`UnityEngine.UI.Text` + `LegacyRuntime.ttf`). TMP в batch-mode требует ручного импорта TMP Essentials — сложнее без выигрыша на нашем объёме UI |
| Ввод-клик | `IPointerClickHandler` через `EventSystem` + `Physics2DRaycaster` (с `maxRayIntersections = 1`) — одинаково работает мышью и тачем |
| Спавн | `Instantiate`/`Destroy` каждый кадр-другой. Object Pool — оверкилл для 30-сек раундов с <50 мишеней |
| Tween | Корутины (`PopIn`, `Shake`). DOTween Free упомянут в README, но в проекте отсутствует |
| Аудио | Один `AudioSource` на `GameSession` + `PlayOneShot`. Клипы — сгенерированные PCM-плейсхолдеры |
| Лидерборд | `PlayerPrefs["leaderboard_v1"]` + `JsonUtility`. Глобальный (Supabase / Vercel KV) — out of scope |
| Билды | `BuildPipeline.BuildPlayer` из `BuildScript`. WebGL c **Brotli + Decompression Fallback = ON** |
| Сцены и ассеты | Полностью генерируются Editor-скриптами (см. `Assets/_Project/Scripts/Editor/`). Unity Editor для разработки не открывался |

---

## Слой данных (Assets/_Project/Scripts/Data)

- **`DifficultyConfig`** — `ScriptableObject` с параметрами раунда:
  длительность, интервалы спавна, время жизни мишени, шанс ловушки, паддинг
  области спавна и параметры бонуса за скорость. Три ассета —
  `Difficulty_Easy/Normal/Hard.asset`.
- **`TargetTypeConfig`** — `ScriptableObject` с описанием типа мишени: спрайт,
  tint, scale, очки, флаг `isTrap`, префаб партикла, аудиоклип. Два ассета —
  `NormalTarget.asset`, `TrapTarget.asset`. Расширяется до «золотой мишени»
  без новых классов.
- **`LeaderboardEntry`, `LeaderboardData`** — DTO для `JsonUtility`.

Все три типа конфигов проектируют **открытое расширение без правки кода
геймплея**: добавить новый тип мишени или сложность = создать ещё один
`ScriptableObject` ассет.

---

## Ядро (Assets/_Project/Scripts/Core)

### `GameSession`

Единственный синглтон проекта (без `DontDestroyOnLoad` — пересоздаётся вместе
со сценой `Game`). Несёт состояние раунда: счёт, оставшееся время, флаг
`IsPlaying`. Шлёт три события:

- `event Action<int> ScoreChanged`
- `event Action<float> TimeChanged`
- `event Action<int> GameOver`

Подписчики (HUD, GameOverPanel) подписываются в `OnEnable` / отписываются в
`OnDisable`. Чтобы подписки в `OnEnable` находили `Instance`, на классе стоит
`[DefaultExecutionOrder(-100)]` — это гарантирует, что `Awake` `GameSession`
отработает раньше остальных компонентов сцены.

Также `GameSession` отвечает за:

- Применение выбранной сложности при `Awake` (из `PlayerPrefs["difficulty"]`)
- Расчёт бонуса за скорость (`ComputeReactionBonus(t)` — линейный lerp от
  `fastReactionBonus` к 1.0 в окне `fastReactionWindow`)
- Тряску камеры (`Shake(amplitude, duration)`) — корутиной по
  `Camera.main.transform.localPosition`

### `LeaderboardSaveSystem`

Статический фасад над `PlayerPrefs`:

- `Load() / Save(data)` — JSON через `JsonUtility`
- `QualifiesForTop(score)` — попадает ли в топ-5
- `Submit(name, score, difficulty)` — сортирует и обрезает
- `Clear()` — очистить

Версия ключа (`leaderboard_v1`) — задел под будущие миграции схемы записи без
потери совместимости.

---

## Геймплей (Assets/_Project/Scripts/Gameplay)

```
Spawner -> создаёт Target
Target  -> IPointerClickHandler -> GameSession.Shake + AddScore
Playfield (фоновый коллайдер) -> IPointerClickHandler -> GameSession.AddScore(-penalty)
```

- **`Spawner`** — корутина `SpawnLoop`. Каждые
  `Random.Range(spawnIntervalMin..Max)` секунд выбирает тип мишени (`trapConfig`
  с вероятностью `DifficultyConfig.trapChance`, иначе `normalConfig`) и
  спавнит в случайной точке `ViewportToWorld(0..1, 0..1)` с паддингом.
- **`Target`** — `IPointerClickHandler`. `Init(cfg, lifetime)` применяет
  визуал из `TargetTypeConfig`, запускает `Destroy(gameObject, lifetime)` для
  автоисчезновения и `PopIn`-корутину для появления (EaseOutBack 0.15 → 1.0
  scale за 0.14 сек). При клике считает бонус за скорость, инстанцирует
  партикл, играет звук, шейкает камеру и зовёт `GameSession.AddScore`.
- **`Playfield`** — `IPointerClickHandler` на большом фоновом коллайдере.
  `Physics2DRaycaster.maxRayIntersections = 1` гарантирует, что клик с Target
  не пробьётся в Playfield: Target ближе по `z` к камере (`z = 0`, фон на
  `z = 0` тоже, но Background SpriteRenderer.sortingOrder = -100, Target.sortingOrder = 10).

---

## UI (Assets/_Project/Scripts/UI)

- **`HUD`** — два `Text` (счёт + таймер). Подписан на `GameSession` через
  `OnEnable/OnDisable`.
- **`GameOverPanel`** — корневой объект скрыт по умолчанию, появляется по
  `GameOver(score)`. При попадании в топ-5 открывает `NameInputDialog`. Три
  кнопки: `Заново` (`SceneManager.LoadScene` той же сцены), `Лидерборд`
  (если сцена в Build Settings), `В меню`.
- **`NameInputDialog`** — модальный диалог с `InputField` (limit 12 символов).
  Принимает `Action<string>` callback в `Open(callback)`.
- **`MenuController`** — главное меню с двумя SetActive-панелями (главная и
  выбор сложности). Сохраняет выбранную сложность в `PlayerPrefs["difficulty"]`.
  Кнопка «Выход» скрыта в WebGL (`Application.Quit()` там no-op).
- **`LeaderboardView`** — пять фиксированных строк (`rank`/`name`/`score`),
  кнопка Назад и кнопка Очистить.

---

## Editor (Assets/_Project/Scripts/Editor)

Главная идея: **проект полностью генерируется из кода**, чтобы не зависеть
от состояния Unity Editor (открытых сцен, ручных правок, кэша Library).

- **`Bootstrap.BuildAll`** — CLI entry, оркестрирует:
  1. `AssetForge.BuildAll()` — PNG-плейсхолдеры, WAV-плейсхолдеры,
     ScriptableObject-конфиги, префабы Target/HitEffect/MissEffect
  2. `SceneBuilderGame.Build()` — `Game.unity` (камера, EventSystem, Playfield,
     GameSession, Spawner, Canvas с HUD/GameOverPanel/NameInputDialog)
  3. `SceneBuilderMainMenu.Build()` — `MainMenu.unity`
  4. `SceneBuilderLeaderboard.Build()` — `Leaderboard.unity`
- **`BuildScript`** — `BuildWebGL` / `BuildWindows` через `BuildPipeline`.
- **`GeneratedTexturePostprocessor`** — `AssetPostprocessor`, автоматически
  ставит `PPU=128` и `textureType=Sprite` для всех PNG в `Art/Generated/`.
- **`UiHelpers`** — фабрики `Text`/`Button` с настройкой шрифта,
  outline-обводки, RectTransform. Используется всеми SceneBuilder'ами.
- **`WavWriter`** — запись PCM-сэмплов в WAV (16-bit, mono) через
  `BinaryWriter`. Источник для `AssetForge.ForgeAudioClips`.

Прогон в batch-mode идемпотентен: повторный `Bootstrap.BuildAll` перегенерирует
всё с тем же результатом. Это упростит интеграцию в CI/CD при желании.

---

## ProjectSettings (правится напрямую, не через Editor)

- `EditorSettings.asset` → `m_DefaultBehaviorMode: 1` (2D)
- `ProjectSettings.asset` → `activeInputHandler: 2` (Both)
- `EditorBuildSettings.asset` — три сцены в порядке `MainMenu, Game, Leaderboard`
  (выставляется SceneBuilder'ами автоматически)

---

## WebGL-сборка

Ключевые настройки в `BuildScript.BuildWebGL`:

```csharp
PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
PlayerSettings.WebGL.decompressionFallback = true;
PlayerSettings.WebGL.threadsSupport = false;
PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
PlayerSettings.WebGL.dataCaching = true;
PlayerSettings.WebGL.initialMemorySize = 64;
```

**Brotli + Decompression Fallback = ON** — сознательный размен:

- ➕ Билд работает на любом статическом хостинге без правки заголовков
  (Vercel / Netlify / itch.io / GitHub Pages)
- ➖ Лоадер тяжелее на ~50–200 КБ, WASM streaming compilation отключается
  (старт +1–2 сек)

Альтернатива «Fallback = OFF + `vercel.json` с `Content-Encoding: br`» даёт
быстрый старт, но привязывает к конкретному хостингу. Для тестового задания
размен в пользу надёжности — правильнее.

**Threads = OFF** обязательно: иначе требуются COOP/COEP-заголовки, которые
ещё больше усложняют деплой.

Размеры билда (Apple Silicon, Unity 6.3.15f1):

- `web/Build/web.data.unityweb` — 1.27 МБ
- `web/Build/web.wasm.unityweb` — 6.05 МБ
- `web/Build/web.framework.js.unityweb` — 74 КБ
- `web/Build/web.loader.js` — 117 КБ
- Итого ~27 МБ (на диске), ~8 МБ по сети после транспорта

---

## Точки расширения и возможные улучшения

| Что | Замена |
|---|---|
| `Instantiate`/`Destroy` мишеней | **Object Pool** (`UnityEngine.Pool`) если уровень станет mass-spawn |
| Корутинные tween-эффекты | **DOTween** или **PrimeTween** (zero-alloc, не падает при `Destroy`) если эффектов станет много |
| Legacy UGUI | **TextMeshPro** (нужен явный импорт TMP Essentials в batch-mode через `AssetDatabase.ImportPackage`) если важна типографика |
| Локальный лидерборд | **Supabase / Vercel KV** через `UnityWebRequest` — глобальный топ |
| Один тип `Target` префаба | **`AddressableAssets`** + загрузка по id если число типов вырастет |
| PCM-плейсхолдеры | **Kenney + freesound** клипы (CC0/CC-BY) — кладутся в те же папки без правок кода |
| Plug-in типов мишеней | Сейчас тип «золотая» добавляется одним `TargetTypeConfig.asset` — масштабируется до десятков типов до того, как имеет смысл вводить компонент-наследование |

---

## Источники решений

- [Unity Manual — Web browser compatibility (6000.3)](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-browsercompatibility.html)
- [Unity Manual — Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html)
- [Unity Manual — Command line arguments](https://docs.unity3d.com/Manual/CommandLineArguments.html)
- [Aras Pranckevičius — Unity 6 empty web build file sizes](https://gist.github.com/aras-p/740c2d4f9977ce92b7de72b1394dd365)
- [github/gitignore — Unity.gitignore](https://github.com/github/gitignore/blob/main/Unity.gitignore)
