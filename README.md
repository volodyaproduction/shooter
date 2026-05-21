# Кликер-шутер на Unity 6

**Играть:** https://lsh-shooter.vercel.app/

2D-кликер: мишени появляются в случайных точках, нужно кликать. Цель — набрать как можно больше очков за 30-секундный раунд. Развёрнуто на Vercel через WebGL — играется прямо в браузере. Есть глобальный лидерборд, общий с [платформером](https://lsh-platformer.vercel.app/). Собрано на **Unity 6.3 LTS (6000.3.15f1)** в headless-режиме — Unity Editor вручную не открывался.

---

## Сборка и запуск

### 1. Сборка веб-билда

```bash
cd shooter
./build.sh        # web — дефолт
```

Результат — в `web/`, ~8 МБ после Brotli. Скрипт ожидает Unity по дефолтному пути Unity Hub; иначе `UNITY=/path/to/Unity ./build.sh`.

### 2. Локальный запуск в браузере

WebAssembly не грузится с `file://` — нужен HTTP. Любой статический сервер в `web/`, например:

```bash
cd web && python3 -m http.server 3001
# http://localhost:3001/
```

### 3. Сборка `.exe` для Windows

```bash
cd shooter
./build.sh win
```

Результат — `build/Windows/Shooter.exe`. Кросс-билд с macOS работает.

### 4. Деплой на Vercel

**New Project** → выбрать репозиторий → **Output Directory = `web`** → **Deploy**. Лидерборд требует Upstash Redis: **Storage → Create Database → Upstash for Redis** → подключить к проекту → Redeploy. Vercel сам пропишет `KV_REST_API_URL` и `KV_REST_API_TOKEN`. Одну базу можно подключить к обоим проектам — ключи разведены префиксом (`shooter:*`, `platformer:*`).

---

## Структура проекта

### Игровой код (попадает в билд)

```
Assets/_Project/Scripts/
├── Core/                                ← 345 LOC
│   ├── GameSession.cs                   ← singleton раунда, счёт, таймер, события
│   ├── PlayerIdentity.cs                ← GUID игрока + текущее имя (PlayerPrefs)
│   └── LeaderboardClient.cs             ← HTTP-клиент сетевого лидерборда
├── Data/                                ← 72 LOC
│   ├── DifficultyConfig.cs              ← параметры раунда
│   └── TargetTypeConfig.cs              ← тип мишени (визуал + очки)
├── Gameplay/                            ← 302 LOC
│   ├── Spawner.cs                       ← цикл спавна мишеней
│   ├── Target.cs                        ← клик, очки, эффекты
│   └── Playfield.cs                     ← клик «в пустоту» = промах
└── UI/                                  ← 711 LOC
    ├── HUD.cs                           ← счёт + таймер
    ├── GameOverPanel.cs                 ← итог раунда + личный рекорд
    ├── NameInputDialog.cs               ← ввод и смена имени
    ├── MenuController.cs                ← главное меню
    ├── PauseController.cs               ← пауза по ESC и бургер-кнопке
    └── LeaderboardView.cs               ← список топа, медали, t.me-ссылки
```

### Серверный код (Vercel serverless)

```
api/                                     ← 259 LOC
├── _redis.js                            ← общая обёртка над Upstash REST API
├── leaderboard-top.js                   ← GET — топ всех игроков по убыванию
├── leaderboard-save-score.js            ← POST — записать счёт (sanity cap + rate limit)
└── leaderboard-change-name.js           ← POST — задать/сменить имя (с проверкой уникальности)
```

### Editor-инфраструктура (в билд **не** попадает)

```
Assets/_Project/Scripts/Editor/          ← 1644 LOC
├── Bootstrap.cs                         ← главный entry: BuildAll
├── AssetForge.cs                        ← генерация PNG, WAV, ScriptableObject (425 LOC)
├── SceneBuilderGame.cs                  ← собирает Game.unity (416 LOC)
├── SceneBuilderMainMenu.cs              ← собирает MainMenu.unity (314 LOC)
├── SceneBuilderLeaderboard.cs           ← собирает Leaderboard.unity (171 LOC)
├── BuildScript.cs                       ← Build WebGL / Windows (122 LOC)
├── UiHelpers.cs                         ← фабрики Text/Button
├── WavWriter.cs                         ← PCM → WAV (16-bit mono)
└── GeneratedTexturePostprocessor.cs     ← PPU=128 для Art/Generated
```

**Что конкретно делает эта папка:**

- `AssetForge.cs` — **рисует спрайты** (зелёный диск мишени, красный диск ловушки с крестом, фон, искра партикла) через `Texture2D.SetPixels32()` + `EncodeToPNG()`, **синтезирует звуки** (синус 880 Гц, sweep, square + шум, арпеджио) прямой записью PCM-сэмплов в WAV, **создаёт ScriptableObject-конфиги** (1 пресет сложности + 2 типа мишеней) и **префабы** (Target, HitEffect, MissEffect).
- `SceneBuilderGame/MainMenu/Leaderboard.cs` — создают три `.unity`-сцены: камера, EventSystem, Canvas с панелями HUD/GameOver/NameInput, кнопки меню, привязки `onClick`.
- `Bootstrap.cs` — оркестрирует AssetForge + три SceneBuilder'а в правильном порядке.

**Если бы открывали Unity Editor вручную, этой папки бы вообще не было:** спрайты рисовались бы в Photoshop, кнопки расставлялись в Canvas мышкой, `onClick` привязывались бы в инспекторе. 1644 строки Editor-папки — это замена этих кликов через C#. `BuildScript.cs` нужен в любом случае — он заменяет команду `File → Build` в редакторе.

### Сцены, конфиги, префабы (генерируются автоматически)

```
Assets/_Project/
├── Scenes/                              ← пересоздаются SceneBuilder'ами
│   ├── MainMenu.unity                   ← главное меню (Build Index 0)
│   ├── Game.unity                       ← игровой раунд
│   └── Leaderboard.unity                ← список всех игроков
├── Configs/                             ← .asset, генерируются AssetForge
│   ├── Difficulty_Normal.asset          ← единственный пресет сложности
│   └── NormalTarget.asset, TrapTarget.asset
├── Prefabs/Target.prefab                ← генерируется AssetForge
└── VFX/HitEffect.prefab, MissEffect.prefab  ← генерируются AssetForge
```

### Ассеты

```
Assets/_Project/
├── Art/Generated/        ← .png-плейсхолдеры (4 файла, рисуются AssetForge)
├── Audio/Generated/      ← .wav-плейсхолдеры (4 файла, синтезируются AssetForge)
└── Fonts/                ← DejaVuSans.ttf (кириллица в UI)
```

### Корень проекта

```
shooter/
├── ProjectSettings/    ← Unity-конфиги (теги, слои, ввод, билд)
├── Packages/           ← манифест зависимостей Unity
├── web/                ← готовый WebGL-билд (для Vercel)
├── build/Windows/      ← локальный .exe-билд
└── build.sh            ← одношаговая сборка (web|win)
```

---

## Глобальный лидерборд

Все игроки соревнуются в одной таблице. Имена уникальны: если ник занят — сервер вернёт ошибку. Имя с `@` впереди (`@vova`) в таблице становится кликабельной ссылкой на `t.me/vova`.

### Как игра узнаёт «своего» игрока

При первом запуске генерируется **`player_id`** — случайный 128-битный GUID в `PlayerPrefs` (в WebGL это IndexedDB браузера). Сервер опознаёт игрока по нему: чужой счёт переписать нельзя без знания чужого `player_id`, а GUID неугадываем. Переживает деплой Vercel и перезагрузку браузера, не переживает чистку данных сайта и не синхронизируется между устройствами.

### Хранение в Redis

```
shooter:scores       ZSET  player_id  → лучший счёт          (топ строится отсюда)
shooter:names        Hash  player_id  → имя
shooter:name-index   Hash  имя_lower  → player_id            (для проверки уникальности)
shooter:rate:<id>    Key   1, EX 25                          (rate limit)
```

### Защита от накрутки

- **Sanity cap:** счёт выше `2000` (теоретический максимум за раунд) отклоняется на сервере.
- **Rate limit:** один сабмит на `player_id` раз в 25 секунд (раунд 30 сек). Атомарно через `SET NX EX 25` в Redis.

GUID хранится в IndexedDB без HMAC-подписи — для дружеского кликера хватает. Серьёзный анти-чит (подписанные сессии, серверная игровая логика) — оверкилл для этого проекта.

---

## Спрайты и звуки

**Всё нарисовано и синтезировано программно в C#.** Внешних ассетов нет, кроме шрифта.

### Спрайты — генерируются в `AssetForge.MakeDiskTexture()`

Рисуются через `Texture2D.SetPixels32()` + `EncodeToPNG()` пиксель за пикселем:

- `target_normal.png` (128×128) — зелёный диск с белой обводкой
- `target_trap.png` (128×128) — красный диск с белой обводкой и белым крестом ✕
- `playfield_bg.png` (64×64) — сплошная тёмно-синяя заливка (фон поля)
- `spark.png` (32×32) — белый диск для текстуры частиц

Подменить плейсхолдер на реальный спрайт = положить PNG в `Assets/_Project/Art/Generated/` с тем же именем. Код не правится. Готовые CC0-ассеты есть на [kenney.nl/assets](https://kenney.nl/assets).

### Звуки — генерируются в `AssetForge.ForgeAudioClips()`

Прямая запись PCM-сэмплов в WAV-заголовок через `WavWriter.cs` (16-bit mono):

- `hit.wav` (0.08 с) — синус 880 Гц + экспо-затухание
- `miss.wav` (0.15 с) — sweep 350 → 140 Гц
- `trap.wav` (0.25 с) — square-волна 95 Гц + белый шум
- `win.wav` (0.6 с) — арпеджио C5 / E5 / G5

Замены — с [freesound.org](https://freesound.org/) (CC-BY/CC0) или [kenney.nl/assets/sci-fi-sounds](https://kenney.nl/assets/sci-fi-sounds) (CC0), положить с теми же именами в `Assets/_Project/Audio/Generated/`.

---

## Unity CLI: как создавался проект

«Unity CLI» — это **не отдельный инструмент**, а тот же бинарник Unity, запущенный из терминала с `-batchmode`. Editor не показывается, а просто исполняется указанный C#-метод из `Assets/_Project/Scripts/Editor/`. Это способ заставить редактор сделать что-то без человека за мышкой.

### Что делает Unity CLI vs обычный C#

| Этап | Что делает агент | Unity CLI? |
|---|---|---|
| Игровая логика (`Core/`, `Data/`, `Gameplay/`, `UI/`) | пишет `.cs`-файлы как обычные исходники | ❌ не нужен |
| Спрайты, звуки, конфиги, префабы | пишет `AssetForge.cs` (рисует пикселями, пишет PCM) | ✅ `Bootstrap.BuildAll` |
| Три сцены (MainMenu, Game, Leaderboard) | пишет `SceneBuilder*.cs` (описание «как мышкой») | ✅ `Bootstrap.BuildAll` |
| Сборка WebGL / Windows | (готов `BuildScript.cs`) | ✅ `BuildScript.BuildWebGL/Windows` |

Без CLI всё это делалось бы вручную: спрайты — в Photoshop, сцены — мышкой в Hierarchy, конфиги — через `Create → ScriptableObject`. 1644 строки Editor-папки заменяют эти клики на C#-код.

### Команда

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics -projectPath . \
         -executeMethod BuildScript.BuildWebGL \
         -quit -logFile -
```

| Флаг | Обязателен | Что без него |
|---|---|---|
| `-batchmode` | да | Unity откроет GUI, команда зависнет |
| `-projectPath` | да | Unity не поймёт какой проект собирать |
| `-executeMethod` | да | нечего вызывать |
| `-quit` | да | Unity отработает метод и продолжит висеть |
| `-nographics` | нет | без него Unity создаёт скрытый GL-контекст. На CI без GPU упадёт. |
| `-logFile -` | нет | без флага логи в `~/Library/Logs/Unity/Editor.log`. С `-` — в stdout. |

`BuildScript.BuildWebGL/BuildWindows` сам вызывает `Bootstrap.BuildAll()` в начале, поэтому одной команды хватает на всё.

### Когда запускать `./build.sh` при разработке

Открывать Unity Editor не нужно ни на одном шаге — **любое изменение = правка C# → `./build.sh`** (логика, конфиги, спрайты, сцены, ProjectSettings — всё одной командой).

---

## Что осталось

### Улучшения функциональности

- **Оффлайн-кэш лидерборда.** Сейчас при оффлайне таблица показывает «нет связи». В продакшене на ненадёжных сетях — fallback на `PlayerPrefs`-кэш последнего ответа.
- **Object Pool для мишеней.** Сейчас `Instantiate`/`Destroy` каждый кадр-другой; для коротких раундов (<50 мишеней) оверкилл, но при mass-spawn — `UnityEngine.Pool`.
- **Tween-эффекты на DOTween/PrimeTween.** Сейчас `PopIn`/`Shake` на корутинах; для большого числа эффектов лучше zero-alloc.
- **Замена плейсхолдер-спрайтов на CC0** (kenney.nl) — без правок кода.
- **TextMeshPro вместо Legacy UGUI `Text`** — для типографики.

### Рефакторинг архитектуры

- **Дубликаты с платформером (~1300 LOC).** `api/_redis.js`, `leaderboard-*.js`, `Core/LeaderboardClient.cs`, `Core/PlayerIdentity.cs`, `UI/NameInputDialog.cs`, `UI/LeaderboardView.cs`, `UI/HUD.cs`, `UI/PauseController.cs`, `UI/MenuController.cs` отличаются только префиксом ключей Redis/PlayerPrefs и парой строк. Аккуратный путь — UPM-пакет в `Packages/com.silkin.shared/` или симлинки на корневой `_shared/`. Не сделано в этой итерации: миграция двух Unity-проектов на общий пакет требует отдельной проверки, что асемблии и meta-файлы переживут переход.
- **`isNewRecord` ложное срабатывание.** В `api/leaderboard-save-score.js` поле возвращается через `personalBest === score` — повторная отправка того же рекорда после rate-limit вернёт `true`, хотя `ZADD GT` не обновил запись. Лечится сравнением через `ZSCORE` до и после `ZADD`.
- **Нет автотестов на API.** Уникальность ника, rate-limit, повторный submit, граничные score проверялись руками. Минимум — Vitest с моком Upstash REST.
- **`web/Build/*.unityweb` в git** — раздувает репозиторий и историю коммитов. Билды лучше держать вне git и собирать в CI.
