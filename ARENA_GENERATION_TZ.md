# Void Survivor — Phase 2 Procedural Arena Generation (Техническое задание)

**Status:** APPROVED pivot (2026-04-20, revision 4)
**Date:** 2026-04-20
**Phase:** 2
**Scope horizon:** 4 implementation PRs (2.A → 2.D)
**Revision history:**
- r1 (2026-04-20): initial draft — BSP multi-room arena.
- r2 (2026-04-20): grid macro/micro split, verticality to PR 4, soft-lock barrier, async NavMesh, GameManager API, BSP fallback, fixed run length.
- r3 (2026-04-20): декораторный проход, anchor-точки, правила моделей.
- **r4 (2026-04-20): PIVOT.** Phase 2 теперь генерирует **одну большую процедурную арену за encounter** вместо multi-room BSP-layout'а. Добавляется **procedural run graph** с door-choice между аренами. BSP-код из r1-r3 остаётся в репозитории как deprecated для diploma-defense материала, но из active pipeline выводится.

## Причина pivot'а (r4)

В ходе playtest'а r3-архитектуры выявлено:

1. **Ритм боя ломается коридорами.** FPS-геймплей Void Survivor построен на быстром агрессивном engagement'е. Коридоры между room'ами дают "мёртвое время" 5-20 секунд, в течение которых ничего не происходит — это работает против жанра.
2. **Encounter-per-room конфликтует с flow-движением.** Dash-slide-jump combo и kill-streak speed boost рассчитаны на крупное открытое пространство, а не на 20×20м прямоугольник.
3. **Multi-room BSP переусложняет scope.** BSP-layout + corridor-planner + door-adjacency + soft-lock barriers — это серьёзный инженерный бюджет, который уводит от главного: **качества боевой арены**.
4. **Roguelike-слой отсутствует.** В r3-плане игрок проходит линейную цепочку комнат без решений. Жанр требует **выбора** между encounter'ами (Hades / Binding of Isaac / Roboquest).
5. **Дипломная новизна смещается, но не теряется.** Вместо "BSP multi-room" тема становится **"процедурная генерация на двух уровнях: макро (run graph) + микро (arena content)"** — это даже сильнее защищается.

## Что остаётся из r1-r3

Следующие решения и артефакты **переносятся в r4 без изменений**:

- Детерминизм через `System.Random` sub-streams (`layoutRng`, `roomRng`, `corridorRng`, `spawnRng`, `biomeRng`). `UnityEngine.Random` по-прежнему запрещён в `Assets/Scripts/ProceduralArena/**`.
- `ArenaRuntimeContext` как контейнер seed + sub-streams + результата.
- `ArenaRunConfig` как ScriptableObject-вход (с обновлёнными полями).
- Двухуровневая сетка `macroGrid = 4м / microGrid = 1м` для снаппинга.
- Async runtime NavMesh bake под fade-transition (правила из r3).
- Soft-lock barrier как blocker-механика exit-двери (не door-to-door, а внутри арены).
- `GameManager` API: `SetSpawnPoints` / `BeginEncounter` / `EndEncounter` + события.
- Single-scene regeneration между аренами.
- Политика логирования (одна summary-строка на генерацию, никакого spam'а).
- Perf budget: `< 100ms` layout+build, `< 400ms` bake.
- Anchor-система для будущего prop-декоратора (wall / corner / ceiling / floor / doorFrame).
- URP Lit материалы + emissive-маркеры.

## Что из r3 больше НЕ цель

- Multi-room BSP-layout в пределах одной арены.
- `CorridorPlanner` + MST + extras.
- `RoomTypeAssigner` (Start/Combat/Exit assignment).
- Coridor geometry, door-gaps между комнатами.
- Soft-lock barrier **между room'ами** (заменяется на barrier **у exit-двери арены**).
- `targetRoomCount` в конфиге.

Код этих модулей **остаётся в репозитории** (`Assets/Scripts/ProceduralArena/Layout/` + BSP-части `ArenaBuilder`) с маркером `DEPRECATED` в заголовках файлов. Цель хранения — diploma defense (показать алгоритмическое исследование) и возможный future Phase 5 "procedural campaign mode". Активный pipeline r4 этот код не вызывает.

---

## Новая архитектура

### Два слоя процедурной генерации

```
┌─────────────────────────────────────────────┐
│ MACRO-LAYER: Run Graph Generator            │
│  вход: runSeed, runLength                    │
│  выход: граф из N узлов с типами и door-     │
│         choice развилками                    │
└──────────────────┬──────────────────────────┘
                   │ arenaIndex, arenaType, arenaSeed
                   ▼
┌─────────────────────────────────────────────┐
│ MICRO-LAYER: Single-Arena Generator         │
│  вход: arenaSeed, ArenaTypeProfile, size     │
│  выход: одна большая комната + cover +       │
│         verticality + 2 exit door anchors    │
└──────────────────┬──────────────────────────┘
                   │ ArenaRoomData
                   ▼
┌─────────────────────────────────────────────┐
│ SHARED: ArenaBuilder (reused from PR 2 r3)  │
│  Floor/Ceiling/Walls/Cover/Markers/Anchors   │
└─────────────────────────────────────────────┘
```

### Пайплайн одного encounter'а

1. `RunController` стартует run → `RunGraphGenerator.Build(runSeed, config)` → граф узлов.
2. Игрок у двери стартовой арены → `ArenaFlowController.EnterArena(node)`.
3. `SingleArenaGenerator.Generate(node.arenaSeed, node.typeProfile, config)` → `ArenaRoomData`.
4. `ArenaBuilder.Build(...)` → геометрия в сцене.
5. `ArenaNavMeshController.BakeAsync(...)` → await bake с fade-экраном.
6. `GameManager.BeginEncounter(node.encounterConfig)`.
7. Clear condition выполнено (все враги убиты / reach-point / таймер) → `GameManager.EndEncounter()`.
8. Exit soft-lock barrier'ы исчезают → игрок видит **две двери с иконками** следующих арен (combat / elite / parkour / shop / rest / boss).
9. Игрок входит в одну из дверей → fade → `Destroy(arenaRoot)` → перейти к шагу 3 для выбранного узла.
10. После boss'а — victory screen, restart.

### Новая файловая структура

```text
Assets/Scripts/ProceduralArena/
  Core/
    ArenaRunConfig.cs              (существует, уменьшается — убираем multi-room поля)
    ArenaRuntimeContext.cs         (существует, без изменений)
    ArenaGenerator.cs              [DEPRECATED] оставить, не трогать
    RunConfig.cs                   (новый: длина run'а, seed'ы, type probabilities)
    RunContext.cs                  (новый: состояние текущего run'а)
  Layout/                          (весь модуль — [DEPRECATED])
    BspNode.cs                     [DEPRECATED]
    ArenaLayout.cs                 (существует, переиспользуется: один room = одна арена)
    ArenaRoomData.cs               (существует, без изменений)
    ArenaCorridorData.cs           [DEPRECATED]
    BspLayoutGenerator.cs          [DEPRECATED]
    RoomPlanner.cs                 [DEPRECATED]
    CorridorPlanner.cs             [DEPRECATED]
    RoomTypeAssigner.cs            [DEPRECATED]
  Arena/                           (новый — single-arena generator)
    SingleArenaGenerator.cs        (новый, главный entry-point micro-layer)
    ArenaShapeGenerator.cs         (новый: форма — rect / L / T / octagon)
    ArenaCoverPlanner.cs           (новый: Poisson-disk cover с flow-constraints)
    ArenaVerticalityPlanner.cs     (новый: платформы/рампы для parkour-arena)
    ArenaExitPlanner.cs            (новый: 2 exit-двери с door-choice)
    ArenaTypeProfile.cs            (новый: ScriptableObject — Combat / Elite / Parkour / Shop / Rest / Boss / Start)
    ArenaSizePreset.cs             (новый: enum S / M / L + множители)
  Run/                             (новый — run graph)
    RunGraph.cs                    (новый: data-структура нодов/рёбер)
    RunGraphNode.cs                (новый: arenaSeed + typeProfile + children)
    RunGraphGenerator.cs           (новый: процедурный генератор графа)
    RunController.cs               (новый: state machine run'а)
    ArenaFlowController.cs         (новый: fade + regenerate + teleport)
  Build/                           (существует, PR 2 r3 pipeline переиспользуется)
    ArenaBuilder.cs                (существует — уменьшится, single-room логика уже подходит)
    RoomBlockoutBuilder.cs         (существует, без изменений)
    CorridorBlockoutBuilder.cs     [DEPRECATED]
    ArenaOccupancy.cs              (существует, адаптируется под одну комнату)
    ArenaBuildMaterials.cs         (существует)
    BuildUtils.cs                  (существует)
    ArenaExitDoorBuilder.cs        (новый: визуализация двери + door-choice icon)
  Navigation/                      (новый)
    ArenaNavMeshController.cs      (новый: async bake с NavMeshSurface)
  Debug/
    ArenaDebugGizmos.cs            (существует, расширить: draw run graph, arena exits, cover)
    ArenaDebugSettings.cs          (существует)
    ArenaGenerationLog.cs          (существует, переиспользуется)
```

Файлы с меткой `[DEPRECATED]` **компилируются, но не вызываются** из новых pipeline'ов. В их заголовке стоит блок-комментарий с объяснением.

---

## Single-Arena Generator (micro-layer)

### Вход

`SingleArenaGenerator.Generate(arenaSeed, typeProfile, sizePreset, runConfig)`:
- `arenaSeed` — production-детерминизм одной арены.
- `typeProfile` — ScriptableObject `ArenaTypeProfile` (см. ниже).
- `sizePreset` — S / M / L.
- `runConfig` — глобальные настройки.

### Выход

`ArenaRoomData` с расширенными полями:
- `boundsCells` — прямоугольная рамка арены.
- `shape` — enum формы (Rect / L / T / Octagon). Не-прямоугольные формы реализуются маской occupancy внутри bounds.
- `wallHeightMeters` — конкретный потолок этой арены (10-25м).
- `coverPlacements: List<CoverPlacement>` — каждая запись = позиция + размер + rotation.
- `platformPlacements: List<PlatformPlacement>` — для parkour-арен.
- `exitDoorAnchors: List<ExitDoorAnchor>` — 2 штуки для door-choice (для Start и Boss — одна).
- `startSpawnPoint: Vector3` — где телепортируется игрок при входе.
- `combatSpawnPoints: List<Transform>` — куда спавнятся враги (только для combat-типов).

### Алгоритмическая начинка (для diploma-защиты)

1. **Shape generation** — выбор формы из weighted list в `ArenaTypeProfile`. Rect простейший, L/T/Octagon задаются masks в occupancy-сетке.
2. **Cover placement via Poisson-disk sampling** — распределение cover-объектов с гарантированным минимальным расстоянием. Добавляются flow-constraints: минимум 2 клетки от exit door anchor'ов, минимум 3 клетки от start spawn point'а, не блокировать "осевой" коридор между началом и выходами.
3. **Verticality planning (для parkour-типа)** — генерация 2-5 платформ с рампами. Платформы на разных высотных уровнях, расстояние dash-прыжка. Алгоритм: greedy placement с проверкой reachability через `PathfindingLite`.
4. **Exit placement** — 2 exit-двери размещаются на противоположных стенах (не на одной), минимум в 60% длины стены от входа игрока.
5. **Combat spawn points** — grid placement внутри арены с минимальным отступом от игрока и от cover'а; число по `typeProfile.spawnCount`.

### Размеры арен (ArenaSizePreset)

| Preset | Base bounds (м) | Cells | Target area |
|---|---|---|---|
| S | 40 × 40 | 10 × 10 | маленькая, ближний бой, для Start / Rest / Shop |
| M | 60 × 60 | 15 × 15 | стандартный combat |
| L | 80 × 80 | 20 × 20 | elite / boss / parkour |

`sizePreset` в typeProfile'е задаёт базу; допускается jitter ±2 cells от sizeRng.

### Высота потолка (per-arena)

Диапазон `wallHeightMeters = 10..25`. Выбирается `ArenaTypeProfile.ceilingRange` с jitter'ом через `arenaSeed`:

| Тип арены | ceiling range (м) |
|---|---|
| Start | 10 – 12 |
| Combat | 10 – 14 |
| Elite | 12 – 16 |
| Parkour | 18 – 25 |
| Shop / Rest | 10 – 12 |
| Boss | 20 – 25 |

Это даёт вариативность без "пустого воздуха" в маленьких аренах и действительно высоких пространств там, где нужно.

### ArenaTypeProfile (ScriptableObject)

```csharp
[CreateAssetMenu]
public class ArenaTypeProfile : ScriptableObject
{
    public ArenaCategory category;   // Start / Combat / Elite / Parkour / Shop / Rest / Boss
    public ArenaSizePreset size;     // S / M / L
    public Vector2 ceilingRange;     // min/max метры
    public ShapeWeight[] shapes;     // weighted: Rect 0.6, L 0.2, T 0.1, Octagon 0.1
    public float coverDensity;       // 0.5..2.0 штук на 100 м²
    public int minExits;             // обычно 2, для Start/Boss = 1
    public bool enableVerticality;   // true для Parkour, Boss
    public int enemySpawnCount;      // base count; scaled by arenaIndex
    public BiomeDefinition biome;    // материалы + emissive
    public ClearCondition clearCondition;  // KillAll / ReachExit / Timer / None
}
```

---

## Run Graph (macro-layer)

### Структура run'а (v1)

**Фиксированная длина = 5 арен**: Start → Mid1 → Mid2 → Mid3 → Boss.

На каждом переходе после Mid1/Mid2/Mid3 игрок видит **2 двери с иконками** следующей арены. После Boss'а — конец run'а.

```
       Start
         │
      Mid1 (procedurally: Combat | Elite | Parkour)
         │     ↓ игрок выбирает одну из двух дверей
      Mid2 (procedurally: Combat | Elite | Parkour | Shop | Rest)
         │     ↓
      Mid3 (procedurally: Combat | Elite)
         │
       Boss
```

Start и Boss — всегда одни (без выбора).
Mid1/Mid2/Mid3 — каждый представлен **двумя узлами** (door-choice), из них игрок берёт один.

### RunGraphGenerator

Алгоритм:
1. Создать узел `Start` (фиксированный profile).
2. Для каждой стадии Mid1/Mid2/Mid3:
   - Выбрать 2 type profile'а из weighted list с учётом stage (чем дальше, тем выше вес Elite).
   - Generate `arenaSeed` для каждого через `runRng`.
3. Создать узел `Boss`.
4. Результат: `RunGraph` с 1 + 2 + 2 + 2 + 1 = 8 узлов.

Игрок фактически проходит 5 арен — выбранный путь через граф.

### Правила балансировки

- `arenaIndex` (0..4) растит:
  - базовое число врагов: `+15% per index`.
  - шанс Elite: Mid1 = 0.1, Mid2 = 0.25, Mid3 = 0.4.
  - шанс Parkour / Shop / Rest: Mid1-2 = 0.2, Mid3 = 0 (перед боссом чистый combat).
- HP scaling: `+5% per arenaIndex` (без инфляции).

---

## Door-choice UI

Простая in-world реализация:
- Над каждой exit-дверью висит floating-icon-панель (world-space Canvas).
- Иконка = тип следующей арены (Combat / Elite / Parkour / Shop / Rest / Boss).
- При приближении к двери на < 3м — tooltip: "Combat encounter — 8 enemies" или "Shop — spend Kill Points".
- Игрок входит в дверь (triggerVolume) → fade.

UI полноценный с рамкой/подсветкой — Phase 5 polish. В Phase 2 просто SpriteRenderer с иконкой + TextMeshPro-tooltip.

---

## Clear conditions

`ClearCondition` enum:
- `KillAll` — убить всех spawned врагов (combat / elite).
- `ReachExit` — дойти до zone trigger у exit-двери (parkour).
- `Timer` — выжить N секунд (future wave-based arena).
- `None` — сразу открыто (Start / Shop / Rest).

Completion в `GameManager.EndEncounter()` открывает soft-lock barrier'ы exit-дверей.

---

## Verticality (встроено в Parkour-тип)

В r3 verticality была отложена на PR 4. В r4 **verticality становится необходимой для Parkour-арены** и реализуется в PR 2.A.

Ограничения (from r3, сохраняем):
- платформы на разнице 2.5-4м (в parkour можно до 6м, потому что double jump);
- подъём только через ramps или широкие stairs, snapped на macroGrid;
- **`OffMeshLink` / NavMesh jump links запрещены**;
- combat-логика не требует верха: враги идут по нижнему уровню, игрок получает tactical advantage.

`ArenaVerticalityPlanner` реализует процедурную расстановку 2-5 платформ по reachability-алгоритму.

---

## PR split (новый)

### PR 2.A — Single-Arena Generator + Builder адаптация

Scope:
- `Arena/SingleArenaGenerator.cs` + helpers (`ArenaShapeGenerator`, `ArenaCoverPlanner` с Poisson-disk, `ArenaExitPlanner`).
- `ArenaTypeProfile` ScriptableObject + 3 preset-ассета (Start, Combat, Boss).
- `ArenaSizePreset` enum.
- Адаптация `ArenaBuilder` под single-room (удаление Room_N hierarchy, остаётся `ArenaRoot/Shell` + cover + anchors + 2 exit markers).
- Пометка старого BSP-кода как `[DEPRECATED]`.
- Debug gizmos: рисовать exit-doors, cover spots, shape mask.

Acceptance:
- один seed → одна и та же арена;
- cover не блокирует путь между start spawn и любой exit-door;
- 2 exit-door'а видны визуально emissive-материалом;
- все формы (Rect/L/T/Octagon) генерируются и ходибельны;
- потолок per arena рандомизируется в диапазоне profile'а;
- нет `UnityEngine.Random` в новом коде.

### PR 2.B — Run Graph + Run Controller + Fade Transition

Scope:
- `Run/RunGraph` + `RunGraphGenerator`.
- `RunController` как state-machine (Idle / Generating / Playing / Transitioning / GameOver).
- `ArenaFlowController` — fade canvas + destroy-and-regenerate.
- 3 type-profile ассета минимум (Start, Combat-M, Boss) — остальные позже.
- Door-choice placeholder UI (SpriteRenderer над дверью).
- Victory / GameOver screens (placeholder Canvas).

Acceptance:
- run из 5 арен проходится от Start до Boss;
- при выборе двери регенерируется правильный arena-тип;
- fade скрывает regenerate полностью;
- один runSeed → один и тот же граф и одни и те же арены;
- смерть игрока → GameOver screen → Restart генерит новый runSeed;
- нет `MissingReferenceException` при переходе.

### PR 2.C — Async NavMesh + Encounter Integration

Scope:
- `Navigation/ArenaNavMeshController` — async bake через `NavMeshSurface.UpdateNavMesh`.
- `GameManager` API: `SetSpawnPoints(IReadOnlyList<Transform>)`, `BeginEncounter(EncounterConfig)`, `EndEncounter()`, events.
- Encounter trigger: capsule-fully-inside arena bounds.
- Soft-lock barrier на exit-дверях (не между комнатами!) — emissive quad + collider на layer, блокирующем игрока и врагов, но не projectiles.
- Clear condition system: KillAll / ReachExit / None.

Acceptance:
- `SimpleEnemyAI` стабильно идёт к игроку на runtime-baked NavMesh;
- encounter стартует по полному входу в арену;
- exit-барьеры закрыты до clear condition, открываются с VFX fade-out;
- нет log spam'а в combat loop.

### PR 2.D — Verticality + Biomes + Balance + Polish

Scope:
- `ArenaVerticalityPlanner` для Parkour-арен.
- `BiomeDefinition` + 2 biome preset'а (Void Station, Alien Nexus).
- Арена-type profile'ы: Elite, Parkour, Shop, Rest (добавить к уже существующим Start/Combat/Boss).
- Difficulty scaling по `arenaIndex`.
- Debug UI: seed display, arena index, biome id, one-button regenerate.

Acceptance:
- parkour-арена проходится прыжками/dash'ами без застреваний;
- враги поднимаются по ramps на платформы там, где NavMesh их покрывает;
- biome swap меняет материалы без регенерации layout'а;
- 5-10 последовательных run'ов не ломают сцену.

---

## Acceptance criteria для всей Phase 2 (r4)

- Run из 5 арен завершается Victory screen'ом.
- Каждая арена — одна большая комната с cover'ом, 2 exit-дверьми (или 1 для Start/Boss).
- `runSeed` + `arenaSeed`-ы детерминируют всё: graph, shape, cover, spawns, verticality.
- Игрок при клире видит 2 двери с иконками и сознательно выбирает.
- `SimpleEnemyAI` работает через async NavMesh в каждой арене.
- `GameManager` использует `SetSpawnPoints` API.
- Переход между аренами через fade + single-scene regenerate.
- Parkour-арена содержит verticality; остальные плоские.
- Logs не шумят; один summary на генерацию.
- BSP-модули помечены `[DEPRECATED]` и не вызываются из активного pipeline.

---

## Что сознательно вне scope Phase 2 r4

- Настоящий UI door-choice (иконки, ценники, tooltips) — Phase 5.
- Meta-progression (unlocks между run'ами) — Phase 4.
- Shop room logic (инвентарь, покупка) — Phase 4.
- Object pooling арены — Phase 6.
- Biome-specific AI rules — Phase 3.

---

## Итоговая рекомендация r4

Phase 2 становится **контент-пайплайном для одной арены + простым roguelike run-graph'ом**, а не layout-генератором для мультикомнатного dungeon'а. Это:

- лучше соответствует FPS-геймплею Void Survivor;
- проще в реализации и перф-профиле;
- даёт игроку реальные решения (door-choice);
- сохраняет диплом-новизну через два слоя procedural'а (macro run graph + micro arena content);
- переиспользует ~70% кода PR 2 r3 (builder, materials, anchors, determinism, debug);
- ставит verticality и parkour в основной scope без усложнения NavMesh'а (только в parkour-аренах).

BSP-код r1-r3 остаётся в репозитории под `[DEPRECATED]`-маркерами как часть алгоритмической истории проекта — полезно для diploma-защиты ("рассматривались два подхода, выбран procedural single-arena + run graph по результатам playtest'а").
