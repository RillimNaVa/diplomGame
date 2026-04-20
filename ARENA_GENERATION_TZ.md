# Void Survivor - Phase 2 Procedural Arena Generation (Техническое задание)

**Status:** APPROVED (2026-04-20, revision 3)
**Date:** 2026-04-20
**Phase:** 2
**Scope horizon:** 4 implementation PRs
**Revision history:**
- r1 (2026-04-20): initial draft.
- r2 (2026-04-20): grid split macro/micro; verticality перенесена из PR 2 в PR 4; explicit determinism rules; soft-lock barrier вместо hard door; один универсальный procedural blockout в PR 2 вместо набора archetype-префабов; async NavMesh bake; явный API `GameManager.SetSpawnPoints`; BSP fallback; фиксированное число арен в run-е; запрет `OffMeshLink` в Phase 2.
- r3 (2026-04-20): добавлен раздел "Декорирование и интеграция визуальных ассетов" с выбранным подходом (декораторный проход), правилами для моделей, правилами размещения и расширением `BiomeDefinition`. PR 2 теперь включает генерацию anchor-точек. Сам `ArenaPropDecorator` отложен на пост-Phase 2.

## Цель

Сделать процедурную генерацию боевых арен для `Void Survivor`, которая:

- поддерживает дипломную новизну проекта: алгоритмическую генерацию уровней;
- сохраняет текущий сильный элемент игры: быстрый агрессивный FPS-геймплей;
- не ломает уже готовые системы Phase 1;
- даёт детерминированный результат по seed для дебага, демонстрации и скриншотов;
- остаётся реализуемой одним разработчиком в ограниченные сроки.

Итог Phase 2 должен быть не "бесконечный технодемо-генератор", а **стабильный боевой контент-пайплайн**, на который без переписывания смогут опереться:

- Phase 3 AI;
- Phase 4 run progression / upgrades / shop;
- Phase 5 visual polish;
- финальная дипломная демонстрация.

---

## Контекст проекта

На старте Phase 2 уже есть:

- `Assets/test.unity` как основная игровая сцена;
- рабочий `GameManager` с волнами и массивом `spawnPoints`;
- `SimpleEnemyAI`, который зависит от `NavMeshAgent`;
- быстрый FPS-movement и готовый combat loop;
- `com.unity.ai.navigation` уже подключён в проект.

Это важно: Phase 2 не должен требовать полного переписывания `PlayerController`, `WeaponManager`, `Health`, `Kill-to-Survive`.

---

## Главные решения

### 1. Рекомендуемый подход

Для проекта рекомендуется **гибридный BSP + grid-based blockout**:

1. Сначала генерируется **логическая схема** уровня:
   - BSP-деление прямоугольной области;
   - выбор прямоугольных комнат внутри leaf-узлов;
   - построение графа связей и коридоров;
   - назначение типов комнат.
2. Затем из схемы строится **физическая арена**:
   - пол;
   - стены;
   - потолок;
   - дверные проёмы;
   - платформы/рамповые элементы;
   - spawn points и переходы.
3. После сборки выполняется **runtime NavMesh baking**.

### Почему это лучший вариант сейчас

- BSP хорошо подходит под дипломную тему: алгоритм понятный, объяснимый и легко визуализируемый.
- Прямоугольные комнаты проще контролировать под FPS-ритм, чем органические пещеры.
- Grid/blockout-подход позволяет быстро собрать играбельный результат даже примитивами.
- NavMesh на прямых поверхностях и простых перепадах будет значительно стабильнее.

### Альтернативы и нюансы

- Полный runtime mesh generation:
  - плюс: максимальная гибкость формы;
  - минус: выше сложность, больше edge-cases, дороже дебаг, тяжелее NavMesh.
- Wave Function Collapse:
  - плюс: красивое разнообразие паттернов;
  - минус: хуже объясняется в дипломе, сложнее контролировать flow боёв.
- Только hand-authored комнаты без реальной генерации:
  - плюс: быстро и надёжно;
  - минус: дипломная новизна заметно слабее.

Итог: **BSP для layout, модульный blockout для геометрии** даёт лучший баланс между научной частью, сроком и качеством результата.

---

## Целевой игровой цикл Phase 2

### Рекомендуемый flow

Одна "арена" в рамках run-а должна быть не одной пустой комнатой, а **небольшим cluster-уровнем**:

- стартовая комната;
- 2-4 боевые комнаты;
- 1-2 коридора/transition-сегмента между ними;
- финальная exit-комната или exit-portal.

### Почему так

- Игрок получает ощущение продвижения, а не просто волну в одном квадрате.
- BSP реально чувствуется в игре, а не существует только "на бумаге".
- Коридоры дают короткую передышку между интенсивными боями.
- Текущий `GameManager` можно адаптировать под "encounter per room", а не переписывать в полноценный campaign system.

### Что не стоит делать в первой итерации

- большой лабиринт на 20+ комнат;
- backtracking как в metroidvania;
- несколько активных боевых комнат одновременно;
- полностью открытый procedural megamap.

Это ухудшит читаемость, усложнит AI и сильно поднимет стоимость NavMesh и дебага.

---

## Архитектура генерации

### Пайплайн

1. `ArenaRunConfig` задаёт seed, размер карты, диапазоны комнат, числа сплитов, правила verticality.
2. `BspLayoutGenerator` строит дерево разделения.
3. `RoomPlanner` выбирает реальные границы комнат внутри leaves.
4. `CorridorPlanner` соединяет комнаты минимальным графом плюс 0-2 дополнительных связи.
5. `RoomTypeAssigner` назначает роли:
   - `Start`
   - `CombatSmall`
   - `CombatMedium`
   - `CombatLarge`
   - `Transition`
   - `Exit`
6. `ArenaBuilder` создаёт геометрию и room roots.
7. `ArenaSpawnPointGenerator` расставляет spawn points по боевым комнатам.
8. `ArenaNavMeshController` печёт NavMesh.
9. `ArenaFlowController` передаёт активные spawn points и room state в `GameManager`.

### Предлагаемая файловая структура

```text
Assets/Scripts/ProceduralArena/
  Core/
    ArenaRunConfig.cs
    ArenaGenerator.cs
    ArenaFlowController.cs
    ArenaRuntimeContext.cs       // хранит seed + sub-stream RNGs + текущий layout
    EncounterConfig.cs           // передаётся в GameManager.BeginEncounter
    BiomeDefinition.cs           // ScriptableObject
  Layout/
    BspNode.cs
    ArenaLayout.cs
    ArenaRoomData.cs
    ArenaCorridorData.cs
    BspLayoutGenerator.cs
    RoomPlanner.cs
    CorridorPlanner.cs
    RoomTypeAssigner.cs
  Build/
    ArenaBuilder.cs
    RoomBlockoutBuilder.cs
    ArenaPropBuilder.cs
    ArenaSpawnPointGenerator.cs
    ArenaTransitionPortal.cs
  Navigation/
    ArenaNavMeshController.cs
  Debug/
    ArenaDebugSettings.cs
    ArenaDebugGizmos.cs
    ArenaGenerationLog.cs
```

### Почему отдельный модуль

- Изоляция от legacy-кода в `Assets/test/`.
- Можно разрабатывать и отключать систему без риска для combat subsystem.
- Будущие фазы получат понятную точку интеграции.

---

## Детерминизм и seed

### Правила

1. Один `System.Random`, инициализированный из seed, создаётся в `ArenaGenerator` и передаётся вниз по pipeline как параметр (через `ArenaRuntimeContext`).
2. `UnityEngine.Random` в `Assets/Scripts/ProceduralArena/**` **запрещён**. Любое использование считается багом.
3. Для независимости этапов используются отдельные sub-streams, каждый засеян производной от master seed:
   - `layoutRng` — BSP splits.
   - `roomRng` — выбор реальных границ комнат внутри leaf-ов.
   - `corridorRng` — выбор порядка и лишних связей.
   - `spawnRng` — позиции spawn points и cover.
   - `biomeRng` — theme/material selection.
   Это гарантирует, что изменение, например, алгоритма расстановки cover не сдвигает layout при том же seed.
4. Seed хранится в `ArenaRuntimeContext` и логируется ровно один раз в generation summary.
5. Любые float-операции, зависящие от `Time.time`, `DateTime.Now`, frame rate и т.п., в генерации запрещены.

### Почему это важно

- Защитные скриншоты диплома должны быть воспроизводимы.
- Баги репродуцируются по seed, а не "у меня иногда ломается".
- Разделение streams экономит часы при отладке — один стрим не "ломает" другой.

---

## Формат карты

### Базовая единица

Используется **двухуровневая сетка**:

- `macroGrid = 4 м` — BSP-разбиение, границы комнат, ширина коридоров, позиция дверей.
- `microGrid = 1 м` — cover, pillars, props, door width, spawn point offsets, platform edges.

Правило: всё, что генерируется на уровне layout (BSP, room rectangles, corridors), снаппится на `macroGrid`. Всё, что добавляется на уровне build (cover/props/anchors), снаппится на `microGrid`. Промежуточные float-значения в сцене запрещены — любая координата должна кратниться одной из двух сеток.

Почему:

- коридор шириной 2 macro-клетки = 8 м нормально читается для fast FPS;
- macro-сетка упрощает BSP и логирование;
- micro-сетка не уродует стены 4-метровыми сегментами и позволяет разумно расставлять cover;
- NavMesh и blockout будут чище, чем при "произвольных" float-координатах.

### Размер уровня первой итерации

- внешняя рамка: `80x80` до `120x120` метров;
- число leaf-комнат: `5-8`;
- число реальных combat-комнат: `3-4`;
- число коридоров: `2-5`.

### Почему не больше

- текущий AI и wave-spawner ещё простые;
- runtime NavMesh должен оставаться дешёвым;
- для дипломной демонстрации важнее надёжность и повторяемость, чем гигантский масштаб.

---

## Комнаты и их стоимость

Ниже "стоимость" дана в двух измерениях:

- стоимость реализации для разработчика;
- runtime-стоимость для игры.

### Типы комнат

| Тип | Размер | Назначение | Dev cost | Runtime cost | Комментарий |
|---|---|---|---:|---:|---|
| Small combat | 16x16 - 20x20 м | ранний бой, высокая плотность | 3-5 часов | низкая | лучший базовый тип |
| Medium combat | 24x24 - 32x32 м | основной универсальный бой | 4-6 часов | низкая-средняя | базовый стандарт для проекта |
| Large combat | 36x36 - 44x44 м | много врагов, движение, фланги | 1-2 дня | средняя | использовать редко |
| Vertical large | 36x36+ м, 2 уровня | jump/dash showcase | 2-3 дня | средняя-высокая | только 1 archetype в первой версии |
| Transition room | 12x12 - 16x16 м | передышка, смена темпа | 2-3 часа | низкая | дешёвый, но важный тип |
| Corridor | ширина 8-12 м | связность и pacing | 1-2 часа | очень низкая | должен быть простым |
| Exit room | 16x16 - 20x20 м | финал арены / портал | 2-4 часа | низкая | можно собирать из small + portal |

### Вывод по объёму

Для Phase 2 **не делаются archetype-префабы**. Вместо этого пишется **один универсальный параметрический room builder** (`RoomBlockoutBuilder`), который принимает `ArenaRoomData` и строит:

- пол, стены, потолок из примитивов (Cube/Quad), снаппленных на macroGrid;
- door gaps по указанным anchor-ам;
- 0..N cover-объектов из примитивов, расставленных на microGrid;
- light/prop anchors как пустые transforms для будущих пассов.

Тип комнаты (`Small/Medium/Large/Transition/Exit`) в Phase 2 влияет только на:

- размер;
- плотность cover;
- число и расположение spawn points;
- материал (через `BiomeDefinition`).

Набор archetype-префабов (visually distinct rooms) — **Phase 2.5 polish**, когда алгоритм и NavMesh стабильны. Это сознательный выбор: дипломная ценность в алгоритме генерации, а не в ассет-работе.

Комбинаторное разнообразие в Phase 2 обеспечивается:

- seed-ом;
- room type assignment;
- положением door anchors;
- cover/spawn density;
- biome material swap.

### Что реально дорого

Самое дорогое в этой фазе не сам BSP, а:

- отладка геометрии и проходов;
- вертикальность, которая не ломает NavMesh;
- правильные spawn rules;
- тестирование того, чтобы room не становилась слишком пустой или слишком тесной.

---

## Как должна выглядеть генерация

### Рекомендуемая двухслойная модель

#### Слой 1. Логическая генерация

Генерируются только данные:

- размер и позиция комнаты;
- тип комнаты;
- связи;
- door anchors;
- возможные точки спавна;
- высотный уровень комнаты;
- флаги: `isStart`, `isExit`, `supportsVerticality`.

#### Слой 2. Визуальная сборка

Отдельный builder переводит данные в сцену:

- floor planes / cubes;
- wall segments;
- ceiling segments;
- door gaps;
- platform prefabs;
- ramp/stair prefabs;
- light anchors;
- prop anchors.

### Почему нужно именно разделение "data -> build"

- генерацию можно дебажить без инстанса геометрии;
- легче логировать layout;
- можно воспроизводить баг по seed и сравнивать layout отдельно от визуала;
- потом будет проще сменить blockout на арт-модули.

---

## Verticality

### Решение для Phase 2

Verticality **вводится только в PR 4** и только в виде одного варианта large-комнаты. В PR 1-3 все комнаты полностью плоские.

Ограничения PR 4 verticality:

- максимум 0-2 приподнятых платформы в комнате;
- разница по высоте: `2.5-4` метра;
- подъём только через ramps или широкие stairs, снаппленные на macroGrid;
- никаких узких прыжков, обязательных для прохождения;
- **`OffMeshLink` / NavMesh jump links запрещены** — враги поднимаются только по ramps/stairs.

### Почему перенесено из PR 2 в PR 4

- runtime NavMesh baking на ramps + многоуровневой геометрии — известный источник багов Unity (зазоры, провалы агентов, неверные connection edges);
- сначала должна стабильно работать плоская генерация + NavMesh;
- PR 2 иначе превращается в борьбу с NavMesh вместо стройки blockout-а;
- verticality в одной комнате не ломает дипломную новизну — алгоритмическая часть сосредоточена в layout, а не в вертикали.

### Что делают враги с вертикалью в Phase 2

Если NavMesh покрывает ramp → враг поднимается. Если platform вне NavMesh (island) → враг остаётся внизу. Это допустимо: игрок использует платформу как advantage. Сложные jump-links — Phase 3 AI.

---

## Спавн врагов и интеграция с волнами

### Рекомендуемый вариант

Каждая боевая комната содержит собственный набор `ArenaSpawnPoint`.

Encounter-trigger:

- комната имеет trigger-volume, покрывающий её внутренний bounds;
- encounter стартует в момент, когда игрок **полностью** вошёл в volume (по capsule bounds, а не по origin);
- случайный прострел или короткий забег не триггерит encounter.

Во время активного encounter:

- `ArenaFlowController` активирует только spawn points текущей комнаты;
- `GameManager` спавнит только из них;
- **soft-lock barrier** закрывает door gaps в соседние комнаты.

### Soft-lock barrier

Door gap закрывается **невидимым/полупрозрачным энергетическим барьером**, а не физической коллайдер-дверью:

- визуально: плоская emissive/accent-цвет quad с shader-distortion, читается как "вход закрыт";
- физически: collider на слое, который блокирует player и enemy, но не projectiles;
- открывается мгновенно после room-clear (encounter done), VFX fade-out.

Почему не hard-lock collider:

- hard collider в fast-FPS с dash/slide даёт "я застрял" моменты;
- soft barrier читаемо коммуницирует "ты внутри encounter-а", игрок видит причину;
- дешевле в реализации (один quad + material + shader-параметр).

### Почему это лучше

- бой легче балансировать;
- игрок не вытягивает половину карты одним кайтингом;
- меньше риск странного поведения AI в коридорах;
- проще объяснить difficulty curve.

### Что нужно изменить в текущем `GameManager`

Минимально, а не радикально. Конкретный API:

```csharp
// GameManager
public void SetSpawnPoints(IReadOnlyList<Transform> points);
public event Action OnSpawnPointsChanged;
public void BeginEncounter(EncounterConfig cfg);   // вместо автозапуска в Start()
public void EndEncounter();                         // вызывается при room-clear
public event Action OnEncounterEnded;
```

Правила:

- `SetSpawnPoints` заменяет внутренний список атомарно; старые `Transform[]` refs больше не используются.
- Старый автозапуск первой волны в `Start()` убирается и заменяется вызовом `BeginEncounter` от `ArenaFlowController`.
- Перед `Destroy` старой арены `ArenaFlowController` обязан сначала вызвать `EndEncounter` и `SetSpawnPoints(empty)`, чтобы избежать `MissingReferenceException` на уничтоженных transforms в следующем кадре.
- Режим совместимости: если сцена запущена без `ArenaFlowController` (legacy test scene), `GameManager` работает как раньше (fallback).

### Альтернатива

Глобальные spawn points по всей карте:

- плюс: почти без рефактора;
- минус: плохой pacing, плохой контроль сложности, больше случаев застревания AI.

Для fast FPS это худший вариант.

---

## Переходы между комнатами, уровнями и ареалами

### Внутри одного procedural уровня

Переход между комнатами должен быть через:

- широкий corridor;
- дверь/airlock;
- короткий transition-segment с визуальной рамкой.

Дверь нужна не только визуально, а как gameplay-state marker:

- closed = encounter не завершён;
- opening = room clear;
- open = можно идти дальше.

### Между уровнями run-а

Рекомендуется **single-scene regeneration**, а не additive scene streaming.

Flow:

1. Игрок зачищает финальную комнату.
2. Активируется `ExitPortal`.
3. Игрок входит в портал.
4. На 0.3-0.8 сек включается fade / freeze input.
5. Старый arena root уничтожается.
6. Новая арена генерируется в той же сцене у мирового origin.
7. Игрок телепортируется в новую стартовую комнату.

### Почему single-scene лучше сейчас

- одна сцена уже есть и она рабочая;
- меньше проблем с сериализацией run state;
- проще работать с текущими singleton-like точками (`GameManager.instance`);
- проще для дипломной демонстрации и записи видео;
- меньше технического риска.

### Когда нужен additive scene подход

Только если позже появятся:

- тяжёлые биомы с разным lighting setup;
- длинные run-цепочки;
- потребность в streaming большого мира.

Для Phase 2 это преждевременное усложнение.

---

## Биомы

### Рекомендуемый объём в Phase 2

Не делать разные алгоритмы генерации под каждый биом. Делать **общий layout + theme swap**.

`BiomeDefinition` должен задавать:

- floor material;
- wall material;
- skybox / ambient color;
- fog preset;
- emissive-цвет для soft-lock barrier и exit portal;
- опциональный набор decorative props (позже);
- decal set (позже).

**Важно:** в Phase 2 real-time point-lights по всей арене **не используются** — это дорого для URP на слабых машинах и усложняет оптимизацию. Освещение — через ambient + skybox + emissive materials на ключевых объектах (двери, портал, cover-маркеры). Реальные point-lights появятся только в Phase 5 polish.

### Почему так

- визуально игрок чувствует прогресс;
- алгоритм остаётся один и его проще отладить;
- дипломная ценность не теряется, потому что novelty здесь в layout generation, а не в наборе текстур.

---

## Декорирование и интеграция визуальных ассетов

### Выбранный подход: декораторный проход

В Phase 2 используется **декораторный проход**, а не модульный tile-kit и не room-templates.

Рассмотренные альтернативы и причины отказа:

- **Модульный tile-kit** (набор `Wall_Straight_4m`, `Floor_Tile_4m` и т.п. вместо примитивов): визуально сильнее, но требует согласованного набора моделей с одинаковым pivot/scale и стыкуемыми краями. Для одного разработчика-студента без опыта modular level-design это неоправданный риск. Откладывается на Phase 5 polish при наличии времени.
- **Room templates** (designer лепит целые комнаты как префабы, генератор выбирает): убивает алгоритмическую новизну диплома. Отклонено.

### Архитектура декораторного прохода

```
Layout data  →  Blockout build (PR 2)  →  [пауза]  →  Prop decoration (пост-Phase 2)
```

1. **PR 2 (blockout)** строит геометрию примитивами и **резервирует anchor-точки** как empty `Transform` в предсказуемых местах комнаты:
   - `wallAnchors` — вдоль стен с шагом 4 м (1 anchor на macroGrid-cell стены);
   - `cornerAnchors` — в углах комнаты;
   - `ceilingAnchors` — на потолке по сетке;
   - `floorAnchors` — центры свободных microGrid-cell-ов пола;
   - `doorFrameAnchors` — по бокам door gaps.
   Anchor-ы создаются всегда, даже если в текущем биоме декора нет. Стоимость: ~30-80 пустых transforms на комнату.

2. **ArenaPropDecorator** (новый класс, НЕ в PR 2; вводится отдельным минорным PR после завершения алгоритмической части Phase 2) проходит по anchor-ам и спавнит префабы по правилам biome. Decorator работает поверх готового layout — геометрию не трогает.

3. `BiomeDefinition` расширяется weighted lists:
   ```csharp
   [Serializable] public class PropEntry { public GameObject prefab; public float weight; }
   // в BiomeDefinition:
   public PropEntry[] wallProps;
   public PropEntry[] cornerProps;
   public PropEntry[] ceilingProps;
   public PropEntry[] floorProps;
   public PropEntry[] coverProps;
   ```
   Пустой entry (`prefab=null`) допустим — представляет "оставить anchor пустым".

### Детерминизм декорирования

- `ArenaPropDecorator` **обязан** использовать только `ArenaRuntimeContext.spawnRng` (или отдельный `decoratorRng` sub-stream), никогда `UnityEngine.Random`.
- Decoration deterministic по тому же master seed, что и layout. Один seed → один набор props.
- Смена `BiomeDefinition` при том же seed меняет только содержимое props, не распределение anchor-ов.

### Правила для моделей и ассетов

Требования к любому prop-префабу (Asset Store или собственный Blender):

1. **Pivot** — в основании объекта (не в центре bounds). Если модель имеет нестандартный pivot, обернуть в пустой wrapper-GameObject с правильным pivot-ом, и в weighted list класть wrapper, а не raw-ассет.
2. **Scale** — настроенный в Prefab-е, а не в root transform сцены. `transform.localScale = Vector3.one` у anchor-ов.
3. **Layer** — prop-ы кладутся на layer, который **не участвует в NavMesh bake** (например, `Prop`), либо имеют `NavMeshObstacle` с `carve = false`. Это критично: крупный ящик не должен делать дыры в NavMesh и блокировать врагов.
4. **Collider** — у декоративных props collider опциональный. У cover-props (pillars, crates, используемых игроком как укрытие) collider обязательный, но NavMesh их не должен учитывать как walkable.
5. **Нет скриптов** с runtime-побочными эффектами (спавн врагов, звуки в `Update`, самоуничтожение по таймеру). Prop — чистый меш + материал.

### Правила размещения

`ArenaPropDecorator` обязан:

- **не перекрывать spawn points** — для anchor-а в радиусе `R` от spawn point использовать только small-props (из whitelist в BiomeDefinition) или оставлять пустым;
- **не перекрывать door gaps** — anchor-ы в door frame используют только `doorFrameAnchors` набор (обычно пустой или lightweight trim);
- **не перекрывать path from Start to Exit** — в PR 2 это гарантируется тем, что coverProps на floorAnchors не ставятся в центральном коридоре комнаты шириной ≥ 2 macro-cell;
- **не плодить дубликаты вплотную** — для соседних anchor-ов одного типа применять минимальную дистанцию повторения (параметр `minRepeatDistance` в BiomeDefinition).

### Что в PR 2 именно добавляется

- Генерация anchor-точек в `RoomBlockoutBuilder` (empty transforms с тегами/именами по типу anchor-а).
- Аналогично для `CorridorBlockoutBuilder` — door frame anchors + wall anchors вдоль коридора.
- Никакого prop-спавна. Decorator пока не существует.

Стоимость изменения относительно "PR 2 без anchor-ов": ~30-50 строк кода. Польза: после Phase 2 декор подключается без правок алгоритма.

### Что откладывается

- `ArenaPropDecorator` сам класс.
- `BiomeDefinition.propSets` контент.
- Собственно prop-ассеты (Blender + Asset Store).
- UI для biome preview / swap.

Ориентировочное окно: после PR 4 Phase 2 или в рамках Phase 5 polish, в зависимости от графика защиты.

---

## Геймплейные требования

### Основной принцип

Генерация должна усиливать существующий combat loop:

- быстрое движение;
- агрессивный вход в бой;
- короткие интенсивные encounter-ы;
- регулярные возможности для dash/slide/flank;
- room читается за 1-2 секунды после входа.

### Что это означает геометрически

В каждой combat room должны быть:

- минимум 2 маршрута движения;
- минимум 1 безопасный разворотный сектор;
- минимум 1 линия для длинного прострела;
- минимум 1 элемент cover или pillar;
- минимум 1 точка, где dash/slide даёт преимущество.

### Что нельзя допускать

- один узкий choke как единственный путь;
- слишком низкий потолок;
- спавн врагов прямо за спиной игрока без телеграфа;
- room, где игрок может полностью стоять вне досягаемости AI;
- corridor, где два врага полностью блокируют проход.

---

## Сложность

### Как должна расти сложность в Phase 2

Пока у проекта нет полноценного набора enemy archetypes, сложность должна расти в первую очередь через:

- количество врагов;
- плотность spawn waves;
- размер активной комнаты;
- долю vertical rooms;
- длину encounter chain без длинной паузы.

### Рекомендуемая формула роста

- `arenaIndex` повышает базовое число врагов на `+10-15%`;
- каждая 2-я арена повышает шанс medium/large room;
- каждая 3-я арена разрешает 1 vertical room;
- минимальная дистанция спавна до игрока растёт с room size, чтобы не было unfair burst.

### Почему не через сырой HP inflation

Слишком ранний рост HP врагов:

- замедляет темп;
- ухудшает ощущение оружия;
- маскирует слабый AI "толщиной", а не качеством.

HP scaling допустим, но мягкий:

- `+5%` на арену максимум до появления новых типов врагов в Phase 3.

---

## Run-структура в Phase 2

Полноценный `RunManager` относится к Phase 4, но Phase 2 должна иметь минимальный run-цикл для демо и acceptance:

- **Длина run-а:** 3 арены подряд.
- **После 3-й арены:** срабатывает простой victory-screen (placeholder UI: "Run Complete" + кнопка "Restart").
- **После смерти игрока:** game-over screen с кнопкой "Restart" (перегенерация с нового seed).
- Сложность растёт по `arenaIndex` согласно формуле из раздела "Сложность".

Это не финальный run loop, а минимальный, чтобы PR 4 имел воспроизводимый acceptance-критерий "run завершается".

---

## NavMesh baking — явные правила

- Используется `NavMeshSurface` из `com.unity.ai.navigation`.
- Bake **асинхронный**: `NavMeshSurface.UpdateNavMesh(NavMeshData)` возвращает `AsyncOperation`, его надо ждать корутиной/`UniTask`.
- Синхронный `BuildNavMesh()` запрещён в runtime-пути (разрешён только в editor debug-button "Rebuild NavMesh Sync").
- Во время bake показывается fade/transition-экран (из `ArenaFlowController`). Игрок не должен видеть стоп-кадр.
- Один `NavMeshSurface` на всю арену (покрывает root). Множественные surfaces — избыточно для Phase 2.
- Agent type — дефолтный Humanoid, без кастомных agent-ов (Phase 3 при необходимости).

---

## BSP fallback и устойчивость генерации

BSP может произвести плохой результат: leaf слишком мал, комната не помещается, граф не связный. Правила обработки:

1. Если leaf меньше `minRoomSize` — split для этого leaf отклоняется, он остаётся неразбитым.
2. Если после split-ов число valid leaves меньше требуемого числа комнат — **повтор с той же seed и меньшей максимальной глубиной** (`maxDepth -= 1`).
3. После N=3 неудачных попыток — fallback: вернуть hand-coded минимальный layout (1 start + 2 combat + 1 exit в жёсткой форме 2×2). Логируется как `Warning`.
4. Corridor planner обязан гарантировать связность графа. Если MST-шаг не связывает, добавляются принудительные ребра.
5. Любой fallback обязан логироваться одной `Warning`-записью с причиной. Silent fallback запрещён.

Цель: генератор **никогда** не оставляет игрока в пустой сцене. Плохой layout допустим, сломанная сцена — нет.

---

## Дебагинг и логирование

### Обязательные debug-инструменты

1. Seed display:
   - текущий seed;
   - arena index;
   - biome id.
2. Gizmos:
   - BSP leaf bounds;
   - room bounds;
   - corridor connections;
   - spawn points;
   - player start;
   - exit portal.
3. Editor-only regenerate button:
   - `Generate from Seed`;
   - `Generate Random`;
   - `Rebuild NavMesh`;
   - `Clear Arena`.
4. Generation summary log:
   - seed;
   - room count;
   - combat room count;
   - corridor count;
   - build time;
   - navmesh time;
   - warnings count.

### Политика логирования

Логирование должно быть уровневым:

- `Error`: генерация не удалась;
- `Warning`: room/corridor пришлось чинить fallback-логикой;
- `Info`: одна summary-запись на успешную генерацию;
- `Verbose`: детальные шаги, только в Editor/Development build.

### Почему это важно

Сейчас в проекте уже есть пример вредного логирования: `SimpleEnemyAI` пишет `Debug.Log` почти в боевом цикле. Для procedural systems это особенно опасно, потому что:

- спам в Console замедляет Editor;
- тяжело находить реальную ошибку;
- performance profile становится шумным.

### Правило

Ни один лог не должен писаться:

- в `Update`;
- на каждый spawn;
- на каждый corridor segment;
- на каждый room wall.

Если нужна детализация, она включается через отдельный verbose toggle.

---

## Влияние на производительность

### Ожидаемая runtime-стоимость

Для первой версии при 5-8 комнатах:

- layout generation: очень дёшево, обычно `< 1-2 ms`;
- blockout instantiation: `10-40 ms` в зависимости от числа сегментов;
- prop pass: `5-20 ms`;
- runtime NavMesh: самый дорогой этап, обычно `80-250 ms`, иногда выше;
- cleanup старой арены: зависит от числа объектов, обычно `5-30 ms`.

### Главный bottleneck

Не BSP и не random, а:

- число инстансов геометрии;
- NavMesh baking;
- лишний мусор от Instantiate/Destroy.

### Что использовать

- `NavMeshSurface` из `com.unity.ai.navigation`;
- один arena root для быстрого cleanup;
- pooled props позже, но не обязательно в первом PR;
- combine/static batching только после профилирования;
- генерация по coroutine/state machine, если bake заметно фризит кадр.

### Практические ограничения

Для Phase 2 надо принять такие budgets:

- генерация новой арены без bake: целиться в `< 100 ms`;
- bake + финальная активация: целиться в `< 400 ms`;
- если больше, закрывать это fade/transition-экраном, а не пытаться скрыть в realtime.

### Чего не делать рано

- ECS/DOTS только ради генерации;
- runtime CSG;
- сложный mesh combining на старте;
- асинхронный стриминг ради нескольких комнат.

Это даст много инженерной стоимости и мало пользы на текущем масштабе.

---

## Визуальные принципы

Phase 2 должен выглядеть как **чистый функциональный blockout с атмосферой**, а не как сырой greybox без направления.

Минимум нужен:

- читаемый silhouette комнаты;
- 2-3 материала на биом;
- emissive/accent lights возле дверей и exit;
- различимая стартовая и финальная комнаты;
- платформы и cover, которые понятны с первого взгляда.

### Почему этого достаточно

На этом этапе критичнее:

- читаемость боя;
- стабильность генерации;
- скорость iteration.

А не финальная художественная детализация.

---

## PR split

### PR 1. Layout + Seed

Scope:

- seed system;
- BSP tree;
- room rectangles;
- corridor graph;
- basic debug gizmos;
- textual generation summary.

Acceptance:

- один и тот же seed даёт одинаковый layout;
- комнаты не пересекаются;
- start и exit всегда существуют;
- все combat rooms достижимы графом.

### PR 2. Physical Build (flat only)

Scope:

- универсальный `RoomBlockoutBuilder` (параметрический, без archetype-префабов);
- floor/wall/ceiling из примитивов по macroGrid;
- corridor geometry;
- door gaps с anchor-ами (без barrier-ов — это PR 3);
- start/exit markers;
- cover placement на microGrid;
- **anchor-точки для будущего декора** (wall/corner/ceiling/floor/doorFrame) как empty transforms в структуре room root-а;
- **все комнаты плоские, verticality НЕТ**.

Acceptance:

- игрок может пройти путь от start до exit своим ходом;
- нет сломанных стен/дыр;
- room shape соответствует layout debug view;
- один seed → один и тот же набор геометрии;
- anchor-точки присутствуют в hierarchy и корректно расположены (видно в Scene view).

### PR 3. NavMesh + Encounter Integration

Scope:

- async runtime NavMesh bake (`UpdateNavMesh` + fade-transition);
- `GameManager` API: `SetSpawnPoints`, `BeginEncounter`, `EndEncounter`, events;
- generated spawn points в боевых комнатах;
- trigger-volume encounter start (capsule-fully-inside);
- soft-lock barrier (emissive quad + collider) на door gaps;
- exit portal и single-scene regeneration между аренами;
- минимальный 3-arena run + victory/game-over placeholder screens.

Acceptance:

- `SimpleEnemyAI` стабильно идёт к игроку в generated room;
- encounter запускается только по полному входу в room;
- soft-lock barrier закрывается на старте encounter и открывается после clear;
- переход в следующую арену работает по порталу без смены сцены;
- run из 3 арен завершается victory-screen-ом;
- нет `MissingReferenceException` при регенерации.

### PR 4. Verticality + Debug + Balance Pass

Scope:

- verticality: одна vertical large-room variant (0-2 платформы, ramps/stairs, без OffMeshLink);
- verbose/debug toggles и editor buttons (`Generate from Seed`, `Random`, `Rebuild NavMesh`, `Clear`);
- generation timings и warnings log;
- BSP fallback handling (см. раздел BSP fallback);
- difficulty tuning по `arenaIndex`;
- biome material swap (`BiomeDefinition`).

Acceptance:

- можно воспроизвести арену по seed-у, указанному вручную;
- warnings читаемы и не спамят Console;
- нет log spam в боевом цикле;
- 5-10 последовательных генераций не ломают сцену;
- vertical room корректно обрабатывается NavMesh-ем (враг поднимается по ramp);
- смена biome меняет материалы без регенерации layout.

---

## Acceptance criteria для всей Phase 2

- Проект генерирует играбельную арену при старте или по кнопке.
- Арена детерминирована по seed (`System.Random`, не `UnityEngine.Random`).
- В арене есть start room, 2-4 combat rooms и exit.
- Между комнатами есть корректные переходы с soft-lock barrier.
- `SimpleEnemyAI` работает через async runtime-baked NavMesh.
- `GameManager` использует сгенерированные spawn points через `SetSpawnPoints` API.
- Игрок может завершить encounter и перейти в следующую арену без смены сцены.
- Run из 3 арен завершается victory-screen-ом.
- Одна vertical large-room variant работает корректно с NavMesh.
- Есть editor/debug инструменты для повтора багов и анализа layout.
- Логи не создают заметного editor spam.
- Генератор никогда не оставляет игрока в пустой/сломанной сцене — есть fallback.

---

## Что сознательно остаётся вне scope

- сложный streaming;
- полноценный shop room logic;
- полноценные biome-specific rules;
- object pooling всех arena pieces;
- сложные scripted events;
- procedural loot/upgrade placement;
- новые enemy archetypes.

Это не потому что не нужно, а потому что это уже Phase 3-4 territory.

---

## Решения по ранее открытым вопросам (подтверждены r2)

1. **Room-by-room combat lock** — ДА, через soft-lock barrier (не hard collider door). Encounter стартует при полном входе игрока в room trigger-volume.
2. **Вертикальность** — одна vertical large-room variant в PR 4. Не в каждой арене. В PR 1-3 все комнаты плоские.
3. **Safe/upgrade room** — отложено до Phase 4 (вместе с upgrade-system). В Phase 2 только боевой переход через exit portal.

---

## Итоговая рекомендация

Phase 2 строится как **контролируемая процедурная система**, а не как максимально "умная" генерация любой ценой.

То есть:

- BSP-layout на macroGrid 4м;
- один универсальный параметрический blockout builder (без archetype-префабов);
- room-scoped encounters с soft-lock barrier;
- single-scene regeneration;
- deterministic seeds через `System.Random` sub-streams;
- async NavMesh bake под fade-transition;
- verticality только в PR 4 в одной room-variant;
- BSP fallback → никогда не сломанная сцена;
- жёсткий debug/perf discipline.

Такой подход лучше всего удерживает баланс между:

- качеством геймплея;
- объяснимостью для диплома;
- реальными сроками разработки;
- будущей расширяемостью.
