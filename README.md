# Shooter — кликер-шутер на Unity 6

Тестовое задание для ЛШ GameDev (Вариант 2). 2D-кликер: мишени появляются в
случайных точках экрана, нужно по ним кликать. Цель — набрать как можно больше
очков за раунд.

- **Главное меню** с выбором сложности и переходом в лидерборд
- **Три сложности** (Легко / Нормально / Сложно) — отличаются интервалом
  спавна, временем жизни мишени и шансом ловушки
- **Мишени двух типов**: обычная (плюс очки) и ловушка (минус очки)
- **Штраф за промах** мимо мишени
- **Бонус за скорость**: чем быстрее кликнул после появления — тем больше очков
- **Game feel**: scale-pop при появлении мишени, тряска камеры при попадании,
  партикл-эффекты и звуковые плейсхолдеры (PCM-генерируемые)
- **Локальный лидерборд** топ-5 через `PlayerPrefs` + `JsonUtility`, с диалогом
  ввода имени

Сделано на Unity **6000.3.15f1** (LTS 6.3-ветка), шаблон **2D + Built-in
Render Pipeline**. Билды собраны на macOS из CLI; редактор не открывался.
Подробности архитектуры — `ARCHITECTURE.md`.

---

## Демо

- **WebGL-демо:** ссылка появится после деплоя на Vercel  
  (содержимое папки `web/` — статический билд, заливается на любой статический
  хостинг: Vercel, Netlify, GitHub Pages, itch.io)
- **Windows .exe:** `build/Windows/Shooter.exe` (запуск с `_Data/` и `UnityPlayer.dll` рядом)

Билды собираются из репозитория командой ниже (см. *Сборка из исходников*).

---

## Управление

- **Левая кнопка мыши** или **тап** по сенсорному экрану — клик по мишени или
  по пустому полю (промах)
- В меню: клики по кнопкам, ввод имени в диалоге лидерборда — клавиатурой
  (`Active Input Handling = Both`, работает и старый и новый ввод)

---

## Запуск из исходников

### Требования

- macOS / Windows / Linux с установленным **Unity Hub**
- **Unity 6000.3.15f1** (LTS 6.3) с модулями:
  - `WebGL Build Support` — для веб-сборки
  - `Windows Build Support (Mono)` — для `.exe`

### Открыть в редакторе

1. `Unity Hub → Add → Open project from disk → выбрать папку shooter/`
2. Дождаться импорта (`Library/` создастся автоматически).
3. Сцены лежат в `Assets/_Project/Scenes/`:
   - `MainMenu.unity` — стартовая (Build Index 0)
   - `Game.unity` — игровой раунд
   - `Leaderboard.unity` — топ-5
4. `Play` из `MainMenu` — пройдёт полный цикл.

### Сборка из CLI (то же, что делает CI)

Сначала генерируем все ассеты, конфиги, префабы и три сцены:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity"
cd shooter
"$UNITY" -batchmode -nographics -projectPath . \
         -executeMethod Bootstrap.BuildAll -quit -logFile /tmp/build.log
```

Дальше — целевая сборка:

```bash
# WebGL → папка web/ (готово к деплою на Vercel)
"$UNITY" -batchmode -nographics -projectPath . \
         -executeMethod BuildScript.BuildWebGL -quit -logFile /tmp/webgl.log

# Windows .exe → папка build/Windows/
"$UNITY" -batchmode -nographics -projectPath . \
         -executeMethod BuildScript.BuildWindows -quit -logFile /tmp/win.log
```

На Apple Silicon WebGL-сборка занимает ~2 минуты (cold compile),
Windows — ~10–15 секунд.

### Локальная проверка WebGL-билда

WebAssembly не запускается из `file://` — нужен HTTP-сервер:

```bash
cd web
python3 -m http.server 8765
# → открыть http://localhost:8765/
```

В Chrome/Firefox/Safari играется без настроек заголовков благодаря
`Decompression Fallback = ON` (Unity сам распаковывает Brotli в JS).

### Деплой на Vercel

```bash
# из корня репозитория
npx vercel --prod web
```

В консоли Vercel: **Output Directory = `web/`**, никаких сборочных команд не
нужно (билд статический). `vercel.json` НЕ требуется при
`Decompression Fallback = ON`.

---

## Зависимости

- `com.unity.ugui` 2.0.0 — UGUI и `Physics2DRaycaster`
- Стандартные модули Unity 6 (включая `com.unity.modules.particlesystem`,
  `physics2d`, `audio`)
- **DOTween** — не используется (tween-эффекты на чистых корутинах ради
  отсутствия Asset Store зависимостей; см. `ARCHITECTURE.md`)

Графические и звуковые плейсхолдеры **генерируются программно** при первом
прогоне `Bootstrap.BuildAll`:

- `Assets/_Project/Art/Generated/*.png` — диски и фон через
  `Texture2D.EncodeToPNG`
- `Assets/_Project/Audio/Generated/*.wav` — PCM-тона (синус, sweep, square с
  шумом, арпеджио) через прямую запись WAV-заголовка

Эти плейсхолдеры легко заменить на реальные ассеты (kenney.nl / freesound.org),
просто положив файлы в эти же папки — никаких изменений в коде не потребуется.

---

## Проверка билдов

| Цель | Состояние |
|---|---|
| WebGL-билд собирается | ✅ 27 МБ, успех |
| `.exe` собирается из macOS | ✅ 85 МБ, успех |
| `.exe` тестировался на Windows | ⚠️ Не проверено — нужна Windows-машина |
| WebGL отдаётся через локальный HTTP | ✅ HTTP 200 на `index.html` / `loader.js` |
| WebGL играется в браузере | ⚠️ Требуется ручная проверка |

`.exe` собран на macOS кросс-билдом (Windows Build Support Mono). Перед
отправкой обязательно запустить на любой Windows-машине: Unity-кросс-билд
работает стабильно, но локально не проверяется.

---

## Источники-ассеты для замены плейсхолдеров

- **Спрайты**: [kenney.nl](https://kenney.nl/assets) — CC0, packs «UI Pack»
  и «Shape Characters». Положить в `Assets/_Project/Art/` и обновить
  `TargetTypeConfig.sprite` в инспекторе.
- **Звуки**: [freesound.org](https://freesound.org/) — CC-BY/CC0 short SFX.
  Положить в `Assets/_Project/Audio/` и обновить ссылки в:
  - `Assets/_Project/Configs/NormalTarget.asset` (`hitClip`)
  - `Assets/_Project/Configs/TrapTarget.asset` (`hitClip`)
  - В сцене `Game.unity` — `Background.Playfield.missClip` и
    `GameSession.winClip`.

---

## Известные ограничения

- **Мобильные браузеры**: Unity 6 официально поддерживает iOS Safari 15+ и
  Android Chrome 58+, но FPS зависит от устройства.
- **Лидерборд локальный** (PlayerPrefs). Глобальный требует бэкенд
  (Vercel KV / Supabase) — выходит за скоуп тестового.
- **DOTween Free** не подключён. Tween-эффекты реализованы корутинами; при
  необходимости можно импортнуть DOTween из Asset Store и переписать
  `Target.PopIn` и `GameSession.Shake` за счёт удобства, но без новой функциональности.
- **TextMeshPro** не используется — взят legacy `UnityEngine.UI.Text` с
  `LegacyRuntime.ttf`. Это сознательный размен для batch-mode (см. `ARCHITECTURE.md`).
