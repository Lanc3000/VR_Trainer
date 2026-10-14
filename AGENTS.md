# Проект
VR-тренажёр (тестовое задание Junior VR Unity Developer).
Unity 6000.0.58f2 (Unity 6), URP, XR Interaction Toolkit 3.x, C#.
Сцена: Assets/Scenes/TrainerScene.unity (создаётся вручную, Codex её не трогает).

# Правила для Codex
- Создавать/менять только: Assets/Scripts/**/*.cs, README.md, AGENTS.md.
- Запрещено: создавать/менять .unity, .prefab, .asset, .meta; добавлять пакеты;
  менять ProjectSettings и Packages/manifest.json.
- API XRI 3.x под Unity 6: события selectEntered/selectExited с SelectEnterEventArgs,
  namespace UnityEngine.XR.Interaction.Toolkit.
- Никакой физики для рычага и логики сценария. Никакого legacy UnityEngine.Input.
- Никаких сторонних ассетов: подсветка — своя, через emission материала URP.
- Стиль: private поля _camelCase, комментарии на русском кратко, без оверинжиниринга.
- После каждой задачи выводить: 1) список созданных/изменённых файлов;
  2) раздел «Инструкция для редактора»: какие компоненты на какие объекты повесить
  и какие поля какими ссылками заполнить.

# Имена объектов сцены (фиксированы)
ObjectA, ObjectB, SocketPoint, LeverPivot, LeverHandle, XR Origin, Main Camera, TaskCanvas.

# Сценарий (строго последовательно)
Шаг 1: взять ObjectA со стола (XR Grab Interactable).
Шаг 2: установить ObjectA в SocketPoint на ObjectB (XR Socket Interactor).
Шаг 3: перевести рычаг в положение «Включено».
Финал: текст «Тренировка завершена.»
Подсветка текущего объекта обязательна, UI с текстом задания обязателен.