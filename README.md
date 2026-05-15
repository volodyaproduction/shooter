# Shooter — кликер-шутер на Unity 6

Тестовое задание для ЛШ GameDev: 2D-кликер-шутер с мишенями, ловушками,
сложностями и лидербордом. Собирается в WebGL и Windows из единого проекта.

**Статус:** в разработке. Полный README с инструкциями и ссылкой на демо появится
после Шага 3 (билды и деплой).

## Стек

- Unity **6000.3.15f1** (LTS 6.3), шаблон **2D + Built-in Render Pipeline**
- Active Input Handling = **Both** (стабильный `EventSystem` + опциональный New Input System)
- UI: **UGUI** (legacy `UnityEngine.UI.Text` через `LegacyRuntime.ttf` — проверенный путь в batch-mode, см. `ARCHITECTURE.md`)
- Tween: **DOTween Free** (импортируется вручную из Asset Store, см. ниже)

## Установка DOTween (вручную)

Asset Store пакеты нельзя коммитить в git (лицензия). Перед первым запуском:

1. Открыть проект в Unity Editor.
2. `Window → Package Manager → My Assets → DOTween (HOTween v2)` → `Import`.
3. После импорта запустить `Tools → Demigiant → DOTween Utility Panel → Setup DOTween`.

Если DOTween не установлен — игра соберётся и запустится, но без tween-эффектов
(scale-pop и тряска камеры реализованы с защитой от отсутствия пакета).
