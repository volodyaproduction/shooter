# Кликер-шутер на Unity 6

2D-кликер: мишени появляются в случайных точках, нужно кликать. Цель — набрать как можно больше очков за 30-секундный раунд. Собрано на **Unity 6.3 LTS (6000.3.15f1)** в headless-режиме — Unity Editor вручную не открывался.

- **WebGL-демо:** ссылка добавится после деплоя на Vercel

## Управление

- **Левая кнопка мыши** или **тап** — клик по мишени; клик по пустому фону = промах (штраф)
- В меню — клики по кнопкам, в диалоге лидерборда — ввод имени с клавиатуры

## Функционал по ТЗ

- [x] Мишени появляются в случайных точках через случайные интервалы
- [x] Клик по мишени → исчезает, очки на экране
- [x] Раунд ограничен по времени, финальный счёт в диалоге, кнопка «Заново»

**Сделано из дополнительных:**

- [x] Главное меню + локальный лидерборд (топ-5 в `PlayerPrefs`)
- [x] Три уровня сложности (Легко / Нормально / Сложно) — отличаются интервалом спавна, временем жизни мишени, шансом ловушки
- [x] Мишени-ловушки (минус очки за клик)
- [x] Штраф за промах мимо мишени
- [x] Бонус за скорость реакции (чем быстрее кликнул — тем больше очков)
- [x] Game feel: pop-эффект появления мишени, тряска камеры, партикл-эффекты при попадании
- [x] Поддержка сенсорного экрана (`Physics2DRaycaster` работает и с мышью, и с тачем)

---

## Сборка и запуск

### 1. Сборка веб-билда

```bash
cd shooter
./build.sh        # web — дефолт
```

Результат — в `web/`. Размер ~8 МБ по сети после Brotli. Скрипт сам регенерирует ассеты, конфиги и сцены (`Bootstrap.BuildAll`) перед сборкой.

Скрипт ожидает Unity по дефолтному пути Unity Hub. Если у тебя другое расположение — `UNITY=/path/to/Unity ./build.sh`. Полная команда CLI и пояснение флагов — в разделе [«Unity CLI»](#unity-cli-как-создавался-проект) ниже.

### 2. Локальный запуск в браузере

Двойным кликом по `web/index.html` игра **не откроется** — браузер из соображений безопасности запрещает WebAssembly-приложениям загружать соседние файлы (`.wasm`, `.data`) напрямую с диска. Эти файлы должны прийти по HTTP. Поэтому нужно поднять локальный HTTP-сервер в папке `web/`.

Проще всего — встроенный сервер из **Python 3** (на macOS он установлен по умолчанию, на Linux — через пакетный менеджер):

```bash
cd web
python3 -m http.server 3001
# открыть в браузере http://localhost:3001/
```

Если Python не хочется — подойдёт любой статический файловый сервер: `npx serve` (нужен Node.js), `caddy file-server`, VS Code расширение «Live Server» и т.п. Python выбран потому, что он почти всегда уже есть на машине разработчика и команда — однострочник.

### 3. Сборка `.exe` для Windows

```bash
cd shooter
./build.sh win
```

Результат — `build/Windows/Shooter.exe` (запуск с `Shooter_Data/` и `UnityPlayer.dll` рядом). Кросс-билд с macOS работает; для запуска нужна Windows-машина.

### 4. Деплой на Vercel

В dashboard Vercel: **New Project** → выбрать репозиторий `shooter` → в **Build and Output Settings** установить **Output Directory = `web`** → **Deploy**. Vercel раздаёт уже собранный билд из `web/`, ничего не собирает сам.

---

## Структура проекта

Разбита по назначению: что выполняется в игре, что только собирает проект, где ассеты, где результаты сборки.

### Игровой код (попадает в билд) — 953 строки

```
Assets/_Project/Scripts/
├── Core/
│   ├── GameSession.cs              ← singleton раунда, счёт, таймер, события
│   └── LeaderboardSaveSystem.cs    ← фасад над PlayerPrefs
├── Data/                            ← ScriptableObject-конфиги
│   ├── DifficultyConfig.cs         ← параметры раунда
│   ├── TargetTypeConfig.cs         ← тип мишени (визуал + очки)
│   └── LeaderboardData.cs          ← DTO для JsonUtility
├── Gameplay/
│   ├── Spawner.cs                  ← цикл спавна мишеней
│   ├── Target.cs                   ← клик, очки, эффекты
│   └── Playfield.cs                ← клик «в пустоту» = промах
└── UI/
    ├── HUD.cs                      ← счёт + таймер
    ├── GameOverPanel.cs            ← итог раунда
    ├── NameInputDialog.cs          ← ввод имени в топ-5
    ├── MenuController.cs           ← главное меню
    └── LeaderboardView.cs          ← таблица топ-5
```

Большую часть занимает UI (5 файлов) — главное меню, диалог имени в лидерборд, отображение топ-5.

### Editor-инфраструктура (в билд **не** попадает) — 1585 строк

```
Assets/_Project/Scripts/Editor/
├── Bootstrap.cs                   ← главный entry: BuildAll
├── AssetForge.cs                  ← генерация PNG, WAV, ScriptableObject (463 LOC)
├── SceneBuilderGame.cs            ← собирает Game.unity (440 LOC)
├── SceneBuilderMainMenu.cs        ← собирает MainMenu.unity
├── SceneBuilderLeaderboard.cs     ← собирает Leaderboard.unity
├── BuildScript.cs                 ← Build WebGL / Windows
├── WavWriter.cs                   ← PCM → WAV (16-bit mono)
└── UiHelpers.cs                   ← фабрики Text/Button
```

**Что конкретно делает эта папка:**

- `AssetForge.cs` — **рисует спрайты** (зелёный диск мишени, красный диск ловушки с крестом, фон, искра партикла) через `Texture2D.SetPixels32()` + `EncodeToPNG()`, **синтезирует звуки** (синус 880 Гц, sweep, square + шум, арпеджио) прямой записью PCM-сэмплов в WAV, **создаёт ScriptableObject-конфиги** (3 сложности + 2 типа мишеней) и **префабы** (Target, HitEffect, MissEffect).
- `SceneBuilderGame/MainMenu/Leaderboard.cs` — создают три `.unity`-сцены: камера, EventSystem, Canvas с панелями HUD/GameOver/NameInput, кнопки меню, привязки `onClick`.
- `Bootstrap.cs` — оркестрирует AssetForge + три SceneBuilder'а в правильном порядке.

**Если бы открывали Unity Editor вручную, этой папки бы вообще не было:** спрайты рисовались бы в Photoshop, кнопки расставлялись в Canvas мышкой, `onClick` привязывались бы в инспекторе. Все 1585 строк Editor-папки — это замена этих кликов мышкой и работы в графическом редакторе через C#.

`BuildScript.cs` (116 строк) был бы нужен в любом случае — это запуск сборки через CLI, заменяет команду `File → Build` в редакторе.

### Сцены, конфиги, префабы (генерируются автоматически)

```
Assets/_Project/
├── Scenes/                              ← пересоздаются SceneBuilder'ами
│   ├── MainMenu.unity                   ← главное меню (Build Index 0)
│   ├── Game.unity                       ← игровой раунд
│   └── Leaderboard.unity                ← топ-5
├── Configs/                             ← .asset, генерируются AssetForge
│   ├── Difficulty_Easy/Normal/Hard.asset
│   └── NormalTarget.asset, TrapTarget.asset
├── Prefabs/Target.prefab                ← генерируется AssetForge
└── VFX/HitEffect.prefab, MissEffect.prefab  ← генерируются AssetForge
```

### Ассеты (см. раздел «Спрайты и звуки»)

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

## Технические решения

- **`ScriptableObject`-конфиги (`DifficultyConfig`, `TargetTypeConfig`)** — добавить новую сложность или тип мишени = создать ещё один `.asset` через `AssetForge`. Код геймплея не правится.
- **`DefaultExecutionOrder(-100)` на `GameSession`** — гарантирует `Awake` раньше остальных компонентов; подписки в `OnEnable` других объектов уже находят `Instance`.
- **Спавн через `Update`, не корутиной** — корутина в WebGL/IL2CPP может «умереть» после `WaitForSeconds`/`MissingReference` и оставить игру без новых мишеней.
- **`Physics2DRaycaster.maxRayIntersections = 1`** — клик с `Target` не пробьётся в `Playfield` ниже по `z`.
- **WebGL: Brotli + `Decompression Fallback = ON`** — Unity сам распаковывает в JS, билд работает на любом хостинге без правки заголовков (Vercel, Netlify, itch.io). Размен — лоадер на ~150 КБ тяжелее.
- **Шрифт `DejaVuSans.ttf`** — встроенный `LegacyRuntime.ttf` не содержит кириллицы.

---

## Unity CLI: как создавался проект

### Что такое «Unity CLI»

Это **не отдельный инструмент**. «Unity CLI» — это тот же самый бинарник Unity, который обычно открывает графический редактор, **но запущенный из терминала с флагом `-batchmode`**. В этом режиме Unity не показывает окно, а просто исполняет указанный C#-метод из папки `Assets/_Project/Scripts/Editor/` и выходит. То есть это **не способ программировать Unity, а способ заставить редактор сделать что-то без человека за мышкой**.

### Разработка делится на 3 этапа — Unity CLI нужен только в двух

**Этап 1. Игровая логика — Unity CLI НЕ нужен.**
`GameSession.cs`, `Spawner.cs`, `Target.cs`, `MenuController.cs` и другие файлы в `Core/`, `Data/`, `Gameplay/`, `UI/` — это **обычный C#-код**. Агент пишет их как любые исходники, в текстовом редакторе. Unity при этом не запускается. Эти скрипты потом просто компилятся вместе с билдом.

**Этап 2. Сцены, спрайты, звуки, конфиги — Unity CLI НУЖЕН.**
Три `.unity`-файла (главное меню, игра, лидерборд), PNG-плейсхолдеры мишеней, WAV-плейсхолдеры звуков, `ScriptableObject`-конфиги сложностей, префабы Target/HitEffect/MissEffect — всё это не текст, который агент мог бы написать руками. В нормальной разработке:
- Сцены собирают **мышкой в Unity Editor**.
- Спрайты рисуют в **Photoshop**.
- Звуки делают в **Audacity** или скачивают.
- Конфиги создают через **Create → ScriptableObject**.

Агент Editor не открывал, в Photoshop не работал. Чтобы получить всё это всё равно, агент:
1. Пишет `SceneBuilder*.cs` — программное описание сцен (как «мышкой в редакторе»).
2. Пишет `AssetForge.cs` — программно **рисует PNG** через `Texture2D.SetPixels32()` и **синтезирует WAV** через прямую запись PCM-сэмплов.
3. Запускает Unity из терминала: тот исполняет `Bootstrap.BuildAll()` и сохраняет всё это на диск.

**Этап 3. Сборка билда (`.wasm`, `.exe`) — Unity CLI НУЖЕН.**
Финальные артефакты получаются через `BuildPipeline.BuildPlayer(...)` в `BuildScript.cs`. Это **тот же механизм, что при клике `File → Build` в редакторе**, но вызванный из CLI. Никаких альтернатив тут нет — собрать билд без Unity физически нельзя.

### Сводная таблица

| Этап | Что делает агент | Unity CLI? |
|---|---|---|
| Игровая логика (`Core/`, `Data/`, `Gameplay/`, `UI/`) | пишет `.cs`-файлы как обычные исходники | ❌ не нужен |
| Спрайты, звуки, конфиги сложностей, префабы | пишет `AssetForge.cs` (рисует пикселями, пишет PCM) | ✅ запускает `Bootstrap.BuildAll` |
| Три сцены (MainMenu, Game, Leaderboard) | пишет `SceneBuilder*.cs` (описание «как мышкой») | ✅ запускает `Bootstrap.BuildAll` |
| Сборка WebGL / Windows | (готов `BuildScript.cs`) | ✅ запускает `BuildScript.BuildWebGL/Windows` |

### Команда Unity CLI

Этапы 2 и 3 запускаются **одной командой** — `BuildScript.BuildWebGL` сам вызывает `Bootstrap.BuildAll()` в начале:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics -projectPath . \
         -executeMethod BuildScript.BuildWebGL \
         -quit -logFile -
```

| Флаг | Значение в `build.sh` | Обязателен | Что без него |
|---|---|---|---|
| `-batchmode` | флаг включён | да | Unity откроет GUI, команда зависнет |
| `-projectPath` | `$PROJECT_DIR` (корень проекта) | да | Unity не поймёт какой проект собирать |
| `-executeMethod` | `BuildScript.BuildWebGL` или `BuildScript.BuildWindows` (по аргументу `web`/`win`) | да | нечего вызывать |
| `-quit` | флаг включён | да (после `-executeMethod`) | Unity отработает метод и продолжит висеть |
| `-nographics` | флаг включён | нет | стандартный «безопасный дефолт»: без него Unity создаёт скрытый GL-контекст. На CI без GPU упадёт. |
| `-logFile` | `-` (stdout) | нет | без флага — логи в `~/Library/Logs/Unity/Editor.log`. С тире (`-`) — в stdout, прямо в терминал. |

В обычной работе это обёрнуто в `build.sh` в корне проекта (см. блок «Сборка и запуск» выше). `BuildScript.BuildWebGL/BuildWindows` сам вызывает `Bootstrap.BuildAll()` в начале, поэтому одной команды хватает на всё.

`Bootstrap.BuildAll` (этап 2) оркестрирует:

1. `AssetForge.BuildAll()` — генерирует PNG-плейсхолдеры, PCM-WAV, 5 ScriptableObject-конфигов (3 сложности + 2 типа мишеней), 3 префаба (`Target`, `HitEffect`, `MissEffect`).
2. `SceneBuilderGame.Build()` — собирает `Game.unity`.
3. `SceneBuilderMainMenu.Build()` — собирает `MainMenu.unity`.
4. `SceneBuilderLeaderboard.Build()` — собирает `Leaderboard.unity`.

### Когда снова запускать `./build.sh` при разработке фичи

Открывать Unity Editor не нужно ни на одном шаге — любое изменение применяется одной и той же командой:

| Что меняешь | Что запускать |
|---|---|
| Логику в runtime-скрипте (`Target.cs`, `Spawner.cs`, `GameSession.cs`) | `./build.sh` |
| Параметры сложности (правишь `AssetForge.ForgeEasyDifficulty()` и т.п.) | `./build.sh` |
| Внешний вид мишени (правишь `AssetForge.MakeDiskTexture(...)`) | `./build.sh` |
| Состав сцены, новые объекты (правишь `SceneBuilder*.cs`) | `./build.sh` |
| Положить новый спрайт/звук в `Art/Generated/` или `Audio/Generated/` с тем же именем | `./build.sh` |
| Параметры тегов / слоёв / Input (правишь `ProjectSettings/*.asset`) | `./build.sh` |

Короткое правило: **любая фича = правка C# → `./build.sh`.**

---

## Что можно улучшить

- **Локальный лидерборд** — топ-5 в `PlayerPrefs`. Глобальный требует бэкенда (Vercel KV / Supabase) — выходит за скоуп тестового.
- **`Instantiate` / `Destroy` мишеней каждый кадр-другой** — для коротких раундов (<50 мишеней) Object Pool оверкилл, но при mass-spawn имеет смысл `UnityEngine.Pool`.
- **Tween-эффекты на корутинах** (`PopIn`, `Shake`) — для большого числа эффектов лучше DOTween/PrimeTween (zero-alloc, не падают при `Destroy`).
- **Спрайты — плейсхолдеры из примитивов.** Замена на CC0-ассеты (kenney.nl) — без правок кода, см. блок «Спрайты и звуки».
- **Legacy UGUI `Text`** вместо TextMeshPro — сознательный размен для batch-mode; для типографики стоит подключить TMP через `AssetDatabase.ImportPackage`.
