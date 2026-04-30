# Void Survivor - Arena Complex MVP (техническое задание)

**Статус:** DRAFT для реализации  
**Дата:** 2026-04-30  
**Фаза:** 3.5  
**Горизонт:** первый playable MVP, затем расширение до ветвлений и специальных комнат  
**Целевое расположение:** `docs/ARENA_COMPLEX_TZ.md`

---

## 1. Цель

Реализовать первый рабочий MVP системы **Arena Complex**: одна процедурно сгенерированная карта-комплекс, внутри которой есть несколько больших боевых комнат/арен, соединенных прямыми широкими воротами. Игрок зачищает текущую комнату, после зачистки открывается проход в следующую, а в финальной комнате появляется особая дверь/портал на следующий процедурно сгенерированный комплекс.

Это не возврат к старому corridor-heavy BSP dungeon. Новый комплекс должен сохранить быстрый FPS-ритм проекта: большие пространства, короткие переходы, понятные боевые арены, никакого длинного пустого бега между encounter'ами.

Ожидаемый игровой опыт:

1. Игрок появляется в первой комнате нового комплекса.
2. Текущая комната закрывается на время encounter'а.
3. После убийства всех врагов открываются широкие ворота в следующую комнату.
4. Игрок сразу попадает в новую arena-scale комнату, а не в длинный коридор.
5. После зачистки последней комнаты открывается визуально отличающаяся финальная дверь.
6. Вход в финальную дверь запускает fade, уничтожает старый `ArenaRoot` и генерирует следующий комплекс с повышенной сложностью.

---

## 2. Почему это имеет смысл

Текущий Phase 2 pipeline хорошо генерирует одну большую арену на encounter и умеет переходить между аренами через fade + door choice. Phase 3 уже добавила врагов, spawn composition, читаемость атак и pooling, поэтому теперь можно поднять уровень структуры: не просто "одна арена -> fade -> другая арена", а "один комплекс из нескольких связанных боевых пространств".

Arena Complex должен дать:

- более цельное ощущение уровня;
- меньше фрагментации между боями;
- физические двери/ворота как понятные ориентиры;
- сильнее выраженную procedural generation тему для диплома;
- основу для будущих reward/shop/elite/rest комнат;
- возможность позже превратить один run graph node в целый комплекс, а не одну арену.

Важное ограничение: первый MVP должен быть небольшим. Он должен доказать, что связка комнат, staged clears, room-local spawns, gates, NavMesh, pooling и переход на следующий комплекс работают стабильно.

---

## 3. Что не входит в MVP

В MVP не нужно пытаться сделать финальную версию всей идеи.

Не входит:

- длинные коридоры;
- возвращение старого BSP dungeon как активного pipeline;
- полноценная ветвящаяся карта с выбором комнат;
- shop/reward/rest комнаты;
- ключи, locked doors, секреты, minimap;
- новая meta progression;
- boss arena redesign;
- кастомные portal shaders;
- новая Blender/Asset Store зависимая art-pipeline;
- несколько Unity-сцен на один комплекс;
- streaming комнат;
- AI Director, понимающий весь комплекс целиком;
- save/load состояния комплекса.

MVP может использовать простые placeholder-визуалы, если gameplay flow работает правильно.

---

## 4. Проектные ограничения

Реализация обязана соблюдать текущие правила проекта:

- `Assets/test.unity` остается основной gameplay-сценой.
- Deprecated BSP-файлы не удалять. Они остаются как diploma reference.
- В `Assets/Scripts/ProceduralArena/**` запрещен `UnityEngine.Random`.
- Все procedural-решения должны использовать deterministic `System.Random` sub-streams.
- Текущий single-arena mode должен остаться рабочим fallback'ом.
- Нужно переиспользовать существующие системы, где это разумно:
  - `SingleArenaGenerator`
  - `ArenaRoomData`
  - `ArenaBuilder`
  - `ArenaBuildMaterials`
  - `WorldUVScaler`
  - `ArenaPostProcessingController`
  - `ArenaNavMeshController`
  - `EncounterController`
  - `SoftLockBarrier`
  - `ArenaFlowController` / `RunController` conventions
- Нельзя ломать Phase 3 combat contracts:
  - enemy pooling и projectile pooling продолжают работать;
  - `Health.onDeath` и `GameManager.OnEnemyKilled` вызываются один раз на смерть;
  - HP orb drops, stagger, glory-kill, telegraph flash, active attack slots и fair-spawn delay сохраняются;
  - recycled enemies не должны сохранять старый HP/stagger/trail/listener state.

---

## 5. MVP в одном абзаце

Первый Arena Complex - это 3 большие прямоугольные комнаты, соединенные линейно двумя широкими воротами. В каждой комнате запускается отдельный encounter. Следующая дверь открывается только после зачистки текущей комнаты. После последней комнаты открывается особый final exit gate, который ведет не в следующую комнату, а в новый процедурно сгенерированный комплекс.

Схема:

```text
[Room 0: Start/Combat] ==wide gate== [Room 1: Combat] ==wide gate== [Room 2: Final Combat/Elite] ==special exit gate== next complex
```

---

## 6. Размеры и форма комнат

### 6.1 MVP room count

Для первого прототипа:

- 3 комнаты;
- линейный путь;
- без ветвлений;
- без side rooms;
- прямоугольные комнаты;
- один старт, две боевые комнаты, один final exit.

### 6.2 Рекомендуемые размеры

Ориентир:

- Room 0: M-size, примерно current medium arena.
- Room 1: M или L.
- Room 2: L или M с более высоким encounter budget.
- Gate width: минимум 2-3 grid cells `ArenaBuilder` (соответствует ширине ~2× игрока + запас, не путать с макро-tile'ами BSP). На glsl-плоскости комнаты это ощутимо широкий arena portal, а не дверной проём.

Комнаты должны оставаться достаточно большими для dash / slide / double jump combat. Если комната ощущается как коридор, layout неверный.

### 6.3 Почему только прямоугольники в MVP

Прямоугольные комнаты в первой версии нужны не потому, что это финальная цель, а потому что они снижают риск:

- проще делать wall openings;
- проще проверять NavMesh;
- проще room-local spawn containment;
- проще не сломать `ArenaBuilder`;
- легче отладить gates и staged clears.

L/T/octagon shapes можно вернуть после того, как базовая связка комнат работает.

---

## 7. Runtime flow

Базовый flow:

1. `ArenaComplexFlowController` получает seed и `complexIndex`.
2. `ArenaComplexGenerator` строит `ArenaComplexData`.
3. `ArenaComplexBuilder` строит все комнаты и ворота под одним `ArenaRoot`.
4. `ArenaNavMeshController` делает один runtime bake на весь комплекс.
5. Игрок телепортируется в стартовую точку Room 0.
6. Стартует encounter Room 0.
7. Gate 0 закрыт, пока Room 0 active.
8. После clear Room 0 открывается Gate 0.
9. Игрок входит в Room 1.
10. Стартует encounter Room 1.
11. После clear Room 1 открывается Gate 1.
12. Игрок входит в Room 2.
13. Стартует encounter Room 2.
14. После clear Room 2 открывается final exit gate.
15. Игрок входит в final exit.
16. Fade out.
17. Старый `ArenaRoot` уничтожается.
18. Генерируется новый complex с `complexIndex + 1`.

---

## 8. Правила room locking

MVP использует staged room locking с обязательным close-behind поведением.

### 8.1 Базовые инварианты

- активна ровно одна комната — `currentRoomId`;
- enemies спавнятся только для current room;
- будущие комнаты не спавнят врагов заранее;
- зачищенные комнаты больше никогда не спавнят врагов;
- игрок не должен проскочить мимо encounter'а.

### 8.2 Состояния комнаты

Каждая `ArenaComplexRoomNode` имеет состояние:

```text
Pending  -> игрок ещё не входил, encounter не запускался
Active   -> идёт encounter, обе двери (back и forward) locked
Cleared  -> encounter завершён, двери в эту комнату навсегда unlocked
```

Переходы только в одну сторону: `Pending -> Active -> Cleared`. Cleared никогда не возвращается в Active. Это критическое правило для backtracking.

### 8.3 Состояния gate

Gate (door link между двумя комнатами) имеет два независимых "замка":

- `lockedByRoomActive` — true, пока хотя бы одна из соединённых комнат в состоянии Active;
- `permanentlyUnlocked` — true, когда обе соединённые комнаты в состоянии Cleared.

Gate проходим только когда `!lockedByRoomActive`. Как только gate стал permanentlyUnlocked, lockedByRoomActive больше не выставляется (комнаты Cleared, Active быть не могут).

Final exit gate имеет отдельную логику (§14): открывается, когда final room Cleared, и не закрывается обратно.

### 8.4 Конкретный flow

1. Игрок появляется в Room 0. Room 0 переходит в Active. Gate 0-1 lockedByRoomActive=true.
2. Игрок зачищает Room 0. Room 0 -> Cleared. Gate 0-1 lockedByRoomActive снимается. Gate 0-1 НЕ становится permanentlyUnlocked, потому что Room 1 ещё Pending.
3. Игрок входит в Room 1 → Room 1 переходит в Active. Gate 0-1 снова lockedByRoomActive=true (закрывается за спиной). Gate 1-2 lockedByRoomActive=true.
4. Игрок зачищает Room 1. Room 1 -> Cleared. Gate 0-1 теперь permanentlyUnlocked (обе комнаты Cleared). Gate 1-2 lockedByRoomActive снимается, но не permanentlyUnlocked.
5. Игрок может свободно вернуться в Room 0 — Gate 0-1 открыт навсегда. В Room 0 врагов нет (Cleared).
6. Игрок входит в Room 2 → Room 2 Active, Gate 1-2 закрывается за спиной.
7. Игрок зачищает Room 2 → Gate 1-2 permanentlyUnlocked, открывается final exit.

### 8.5 Что недопустимо

- двери в уже зачищенную комнату повторно блокироваться не должны;
- зачищенные комнаты не должны повторно спавнить врагов при возврате;
- игрок не должен иметь возможность зайти в Pending-комнату через back-gate (Pending-комната за gate, который lockedByRoomActive — закрыт; permanentlyUnlocked — невозможен, потому что Pending ≠ Cleared).

---

## 9. Архитектура

Добавить новый модуль:

```text
Assets/Scripts/ProceduralArena/Complex/
```

Рекомендуемые файлы:

```text
Assets/Scripts/ProceduralArena/Complex/
  ArenaComplexData.cs
  ArenaComplexRoomNode.cs
  ArenaComplexDoorLink.cs
  ArenaComplexGenerator.cs
  ArenaComplexBuilder.cs
  ArenaComplexFlowController.cs
  ArenaComplexExitTrigger.cs
  ArenaComplexDebugGizmos.cs
```

Важно: не складывать это в deprecated `Layout/` BSP-модули. Arena Complex - новый слой над текущим single-arena pipeline.

### 9.1 `ArenaComplexGenerator`

Ответственность:

- чистая deterministic генерация данных;
- не создает GameObject'ы;
- не трогает сцену;
- принимает `complexSeed`, `complexIndex`, config;
- возвращает `ArenaComplexData`;
- использует только `System.Random`;
- создает room nodes, links, final exit.

### 9.2 `ArenaComplexData`

Содержит результат генерации:

- seed;
- complex index;
- список комнат;
- список внутренних дверей/gates;
- id стартовой комнаты;
- id финальной комнаты;
- позицию player start;
- позицию final exit;
- debug metadata.

### 9.3 `ArenaComplexRoomNode`

Описывает одну комнату комплекса:

- `id`;
- `ArenaCategory`;
- `arenaIndex`;
- `roomSeed`;
- grid/world bounds;
- `ArenaTypeProfile`;
- сгенерированный `ArenaRoomData`;
- room-local spawn points;
- состояние `cleared`.

### 9.4 `ArenaComplexDoorLink`

Описывает связь между двумя соседними комнатами:

- `fromRoomId`;
- `toRoomId`;
- world position;
- direction/forward;
- width;
- locked/open state;
- ссылка на runtime gate object после build.

### 9.5 `ArenaComplexBuilder`

Строит геометрию:

- один `ArenaRoot`;
- `Complex_Room_0`, `Complex_Room_1`, `Complex_Room_2`;
- shell/floor/walls/ceiling/cover/structures для каждой комнаты;
- internal gates;
- final exit gate;
- room entry triggers;
- spawn markers/anchors.

Должен переиспользовать существующие build helpers:

- `BuildUtils.SpawnBox`;
- `ArenaBuildMaterials`;
- `WorldUVScaler`;
- room-level части `ArenaBuilder`, если их безопасно вынести в helper methods.

### 9.6 `ArenaComplexFlowController`

Runtime state machine комплекса:

- хранит текущий `complexIndex`;
- хранит текущий `roomId`;
- запускает генерацию и build;
- запускает NavMesh bake;
- телепортирует игрока;
- стартует encounter текущей комнаты;
- открывает gates после clear;
- обрабатывает final exit;
- запускает следующий complex.

Рекомендация для MVP: сделать отдельный `ArenaComplexFlowController`, а не сразу переписывать `RunController`. Это снижает риск сломать уже рабочий single-arena flow.

### 9.7 `ArenaComplexExitTrigger`

Триггер финальной двери:

- активен только после clear финальной комнаты;
- при входе игрока вызывает `RequestNextComplex()`;
- запускает fade/regenerate flow.

### 9.8 `ArenaComplexDebugGizmos`

Editor/debug визуализация:

- room rectangles/bounds;
- ids комнат;
- gate links;
- spawn points;
- current room;
- final exit.

---

## 10. Черновик data model

Точная реализация может отличаться, но стартовая форма должна быть примерно такой:

```csharp
public sealed class ArenaComplexData
{
    public int complexIndex;
    public int complexSeed;
    public List<ArenaComplexRoomNode> rooms = new();
    public List<ArenaComplexDoorLink> links = new();
    public int startRoomId;
    public int finalRoomId;
    public Vector3 playerStartWorld;
    public Vector3 finalExitWorld;
}
```

```csharp
public sealed class ArenaComplexRoomNode
{
    public int id;
    public ArenaCategory category;
    public int arenaIndex;
    public int roomSeed;
    public Vector2Int gridOrigin;     // источник истины для layout
    public Vector2Int gridSize;       // источник истины для layout
    // worldBounds вычисляется из (gridOrigin, gridSize) и cell size при build,
    // не хранится в data — см. ArenaComplexBuilder.ComputeWorldBounds(node).
    public ArenaTypeProfile typeProfile;
    public ArenaRoomData roomData;
    public List<Vector3> combatSpawnPoints = new();
    public bool cleared;
}
```

```csharp
public sealed class ArenaComplexDoorLink
{
    public int id;
    public int fromRoomId;
    public int toRoomId;
    public Vector3 worldPosition;
    public Vector3 forward;
    public int widthCells;
    public bool startsLocked;
    public bool isOpen;
}
```

Если нужно видеть данные в Inspector/debug, можно использовать `[Serializable]`. Если это runtime-only data, достаточно plain classes.

---

## 11. Правила генерации

### 11.1 Linear placement

Для MVP не нужен сложный packing. Достаточно deterministic linear placement:

```text
Room 0 at grid (0, 0)
Room 1 east of Room 0
Room 2 east of Room 1
```

Псевдоалгоритм:

```text
BuildComplex(seed, complexIndex):
  complexSeed = Mix(seed, complexIndex)
  layoutRng = new System.Random(complexSeed)

  roomCount = 3
  room0 = Start/Combat, size M
  room1 = Combat, size M or L
  room2 = Elite or Combat, size L

  for each room:
    roomSeed = layoutRng.Next()
    generate ArenaRoomData using SingleArenaGenerator-compatible path
    assign world bounds and local spawn points

  create link room0 -> room1
  create link room1 -> room2
  create final exit on far wall of room2
```

### 11.2 Determinism

Требования:

- same run seed + same complex index = same complex layout;
- no `UnityEngine.Random`;
- room seeds derived from complex seed;
- gate positions deterministic;
- final exit deterministic.

Рекомендуемая структура seed'ов:

```text
complexSeed = Hash(runSeed, complexIndex)
layoutSeed = Hash(complexSeed, "layout")
roomSeed = Hash(complexSeed, "room", roomIndex)
gateSeed = Hash(complexSeed, "gate")
```

**Hash utility отсутствует в проекте** (проверено grep'ом по `Assets/Scripts/ProceduralArena`). Добавить как часть PR 3.5.A:

```csharp
// Assets/Scripts/ProceduralArena/Complex/ComplexHash.cs
internal static class ComplexHash
{
    // SplitMix64-style stable int mixer. НЕ использовать string.GetHashCode()
    // (нестабилен между .NET runtime). Литералы вроде "layout"/"room"/"gate"
    // превращать в const int salt'ы.
    public const int SaltLayout = unchecked((int)0x9E3779B1);
    public const int SaltRoom   = unchecked((int)0xC2B2AE35);
    public const int SaltGate   = unchecked((int)0x27D4EB2F);

    public static int Mix(int a, int b)
    {
        unchecked
        {
            uint x = (uint)(a ^ (b * 0x9E3779B1));
            x ^= x >> 16; x *= 0x7FEB352D;
            x ^= x >> 15; x *= 0x846CA68B;
            x ^= x >> 16;
            return (int)x;
        }
    }
    public static int Mix(int a, int b, int c) => Mix(Mix(a, b), c);
}
```

Это блокирующая зависимость для PR 3.5.A — без неё determinism-контракт фиктивный.

### 11.3 Gate placement

Internal gates:

- находятся между соседними комнатами;
- стоят на общей/стыкующейся стене;
- имеют реальное wall opening;
- достаточно широкие для игрока и NavMeshAgent'ов;
- имеют readable frame;
- имеют blocker collider/barrier в locked state;
- визуально меняются в open state.

Минимальный gate MVP:

- frame из кубов через `BuildUtils.SpawnBox`;
- emissive barrier/door panel;
- collider blocker;
- trigger рядом с входом в следующую комнату.

### 11.4 Final exit placement

Final exit:

- только в последней комнате;
- открывается только после clear последней комнаты;
- визуально отличается от internal gates;
- ведет на следующий generated complex;
- не должен выглядеть как обычный проход.

---

## 12. Build rules

### 12.1 Один `ArenaRoot`

Весь комплекс строится под одним корнем:

```text
ArenaRoot
  Complex_Room_0
    Shell
    Cover
    Structures
    Spawns
  Complex_Room_1
    Shell
    Cover
    Structures
    Spawns
  Complex_Room_2
    Shell
    Cover
    Structures
    Spawns
  Gates
    Gate_0_1
    Gate_1_2
    FinalExitGate
```

Это важно для:

- одного NavMesh bake;
- простой очистки через `Destroy(arenaRoot)`;
- понятной hierarchy debug;
- будущего complex-level post-processing/fog/theme.

### 12.2 Reuse `ArenaBuilder`

Нельзя без необходимости копировать весь `ArenaBuilder`.

Предпочтительный путь:

1. Найти части `ArenaBuilder.BuildSingle`, которые можно безопасно вынести в private/internal helper methods.
2. Дать им world offset / parent transform.
3. `ArenaComplexBuilder` вызывает эти helpers для каждой комнаты.
4. `ArenaBuilder.BuildSingle` остается рабочим для single-arena mode.

Если extraction слишком рискованный для первого PR, допустим временный minimal rectangular builder, но он должен:

- использовать `BuildUtils.SpawnBox`;
- использовать `ArenaBuildMaterials`;
- сохранять `WorldUVScaler`;
- не вводить новый параллельный материал pipeline.

### 12.3 Wall openings

Ворота должны быть настоящими проходами, а не glowing panel перед сплошной стеной.

Acceptance:

- игрок проходит через open gate без clipping;
- NavMesh соединяется через gate;
- frame/lintel закрывает визуальные щели;
- blocker действительно блокирует closed gate.

### 12.4 NavMesh

MVP target:

- один runtime NavMesh bake после build всего комплекса;
- без полного rebake на каждое открытие/закрытие gate;
- gates достаточно широкие, чтобы агенты не застревали.

**Reuse существующего `ArenaNavMeshController`.** Контроллер уже использует `CollectObjects.Children` на ArenaRoot и `useGeometry = PhysicsColliders` — это значит, что bake автоматически охватит всю геометрию любого количества Room sub-root'ов под одним ArenaRoot. Новый API не нужен, рефакторинга в PR 3.5.B не требуется. `ArenaComplexFlowController` вызывает `ArenaNavMeshController.BakeAsync(arenaRoot)` ровно один раз после полного build.

**Gate blocking — `NavMeshObstacle`, не plain Collider.**

Plain `Collider` блокирует только физику игрока и hitscan, но `NavMeshAgent` его игнорирует и спокойно пройдёт сквозь "закрытую" дверь. На MVP это формально не проявляется (Pending-комнаты пусты, Cleared-back-gate всегда unlocked), но close-behind в §8 и любой будущий enemy-leash/преследование сломаются молча.

Решение: каждый gate имеет компонент `NavMeshObstacle` с `carve = false`:

- `carve = false` — obstacle блокирует агентов, но НЕ перепекает NavMesh динамически (это дорого и не нужно);
- активный obstacle = closed gate, неактивный = open gate;
- bake выполняется один раз при геометрически открытых проходах (NavMesh соединён через все gates), а блокировка происходит на уровне agent steering, не геометрии.

Включение/выключение obstacle полностью соответствует gate state из §8.3:

```text
gate.obstacle.enabled = lockedByRoomActive && !permanentlyUnlocked
```

**Visual barrier** (emissive panel, frame visuals) — это отдельный GameObject рядом с obstacle. Он отвечает только за визуал и блокировку игрока (Collider на physics layer), не за pathfinding.

**Final exit gate** — особый случай: closed состояние не должно пускать игрока, но врагов в final exit и не должно тянуть, поэтому достаточно visual barrier + player-blocking Collider, NavMeshObstacle опционален.

**Запрет:** `useGeometry = RenderMeshes` в новом коде. Текущий контроллер собирает физические коллайдеры — этой схеме нужно следовать, иначе NavMeshObstacle перестанет вписываться единообразно.

---

## 13. Encounter flow

### 13.1 Room ownership tracking

Trigger volume — основной driver, но не единственный. Полагаться только на `OnTriggerEnter` нельзя: dash/slide/тонкие коллайдеры могут проскользнуть, NavMesh edge cases могут оставить игрока вне zone, а Room 0 вообще не имеет entry-trigger момента (туда телепорт).

Используется двухуровневая схема:

**Primary: per-room trigger volume.** У каждой комнаты есть BoxCollider trigger, заполняющий её footprint (минус gate threshold ~1 ячейка, чтобы не флипать ownership на самой границе двери). При `OnTriggerEnter` с тэгом Player → `FlowController.RequestEnterRoom(roomId)`.

**Fallback: bounds polling.** `ArenaComplexFlowController` каждые ~0.2 сек проверяет, в `worldBounds` какой комнаты находится `player.transform.position` (XZ-plane, Y игнорируется или зажимается). Если игрок физически в комнате, отличной от `currentRoomId`, и эта комната не Pending за locked gate, вызывается `RequestEnterRoom(detectedRoomId)`. Polling — не каждый кадр, чтобы не дёргать state machine, и идемпотентен (повторный вход в текущую комнату — no-op).

**Room 0 startup.** Не зависит от триггера вообще. Сразу после bake/teleport `ArenaComplexFlowController.StartComplex()` явно вызывает `RequestEnterRoom(startRoomId)`, что переводит Room 0 в Active и запускает encounter. Теоретически polling сделает то же самое в первый тик, но явный старт убирает 1-кадровое окно неопределённости.

`RequestEnterRoom(roomId)` идемпотентный и обрабатывает все легитимные переходы:

```text
RequestEnterRoom(roomId):
  if roomId == currentRoomId: return                           // no-op
  if rooms[roomId].state == Cleared: SetCurrentRoom(roomId); return  // backtracking, без encounter
  if rooms[roomId].state == Active: SetCurrentRoom(roomId); return   // recovery, encounter уже идёт
  if rooms[roomId].state == Pending:
    if not CanReach(currentRoomId, roomId): log warning; return      // не должно произойти при правильных gate locks
    EnterPendingRoom(roomId)                                          // переводит в Active, спавнит, закрывает back-gate

EnterPendingRoom(roomId):
  previousRoomId = currentRoomId
  rooms[roomId].state = Active
  currentRoomId = roomId
  CloseBackGate(previousRoomId, roomId)        // §8.4 step 3/6
  ConfigureEncounterForRoom(roomId)
  StartEncounterForRoom(roomId)
```

`ConfigureEncounterForRoom`: каждая комната владеет собственным `EncounterController` (вешается `ArenaComplexBuilder`'ом на свой Room sub-root), со своим списком `spawnPoints`, `arenaIndex`, `spawnProfile`. Никакого `GameManager.SetSpawnPoints` API не нужно — текущий `EncounterController.BeginEncounter()` уже принимает spawn points через self.spawnPoints и пробрасывает их в `GameManager.BeginEncounter(...)`.

`StartEncounterForRoom(roomId)`: вызывает `BeginEncounter()` на EncounterController комнаты. На Cleared callback подписан handler, который выставляет `rooms[roomId].state = Cleared`, снимает lockedByRoomActive с обоих смежных gate, обновляет permanentlyUnlocked, и если это final room — открывает final exit (§14).

### 13.2 Room clear

Когда current room cleared:

1. Комната получает `cleared = true`.
2. Если это не final room, открывается следующий internal gate.
3. Если это final room, открывается final exit gate.
4. Debug overlay/log обновляется.

### 13.3 Room-local spawn points

Обязательное правило:

- `GameManager` получает только spawn points активной комнаты.

Нельзя:

- спавнить врагов в будущей комнате;
- спавнить врагов за locked gate;
- спавнить врагов внутри blocker/gate frame/cover/structure;
- использовать глобальный список всех spawn points комплекса для одного encounter'а.

### 13.4 Difficulty scaling

Для MVP:

```text
baseArenaIndex = complexIndex * 3      // 3 = roomCount, чтобы не было overlap между комплексами
Room 0 arenaIndex = baseArenaIndex
Room 1 arenaIndex = baseArenaIndex + 1
Room 2 arenaIndex = baseArenaIndex + 2
```

Множитель `3` совпадает с `roomCount`, чтобы arenaIndex монотонно рос между комплексами без задвоений на стыке. Если позже roomCount станет переменным, формулу надо переписать как накопительный счётчик, а не `complexIndex * roomCount`.

Можно настроить мягче, если enemy budget растет слишком быстро.

Цель: переиспользовать существующий Phase 3 spawn composition вместо новой системы сложности.

---

## 14. Final exit gate

Final exit gate - ключевой UX-элемент.

Он должен отличаться от internal gates:

- другой материал или более сильный emissive color;
- крупнее frame;
- portal-like panel;
- особая подсветка;
- optional label вроде `Next Complex`;
- placement только в финальной комнате.

Behavior:

- locked до clear финальной комнаты;
- open после clear;
- OnTriggerEnter запускает fade;
- старый complex уничтожается;
- новый complex генерируется;
- player появляется в start room нового комплекса;
- run продолжается.

Важно: internal gate ведет в следующую комнату этого же комплекса. Final gate ведет на следующий комплекс.

---

## 15. Интеграция с текущим run flow

Есть два варианта.

### Option A - отдельный complex mode

Добавить отдельный `ArenaComplexFlowController` в `test.unity`.

Пример:

- `useArenaComplexMode = true`;
- если включено, стартует complex flow;
- если выключено, работает текущий single-arena run graph.

Плюсы:

- минимальный риск;
- можно сравнивать old/new flow;
- текущий `RunController` не ломается.

Минусы:

- временно появляется отдельный owner transition flow.

### Option B - встроить complex в `RunController`

Каждый run graph node может стать не одной ареной, а одним комплексом.

Плюсы:

- архитектурно красивее в финальной версии.

Минусы:

- выше риск для MVP;
- больше касаний уже проверенного transition system.

Рекомендация: **для MVP выбрать Option A**. После acceptance можно решать, нужно ли сливать complex flow с run graph.

### 15.1 Fade и transition ownership

`ArenaFlowController` сейчас владеет fade и single-arena teleport. Чтобы не дублировать логику и не плодить двух владельцев экрана:

- `ArenaComplexFlowController` НЕ переопределяет fade самостоятельно;
- он зовёт публичный API `ArenaFlowController.PlayFadeOut()` / `PlayFadeIn()` (или вынесенный в utility класс хелпер, если эти методы сейчас private — допустимый minor refactor в PR 3.5.C);
- player teleport в Room 0 нового комплекса делает `ArenaComplexFlowController` напрямую (он владеет данными `playerStartWorld`);
- destroy старого `ArenaRoot` — тоже на complex flow controller, после fade-out, до bake нового complex.

Если `ArenaFlowController.PlayFadeOut()` нет в публичном виде — извлечь в `ArenaTransitionUtil` маленьким хелпером. НЕ копировать coroutine.

### 15.2 Состояние сборки между PR-ами

Чтобы не было непонятного "что вообще сейчас работает":

- **После PR 3.5.A:** complex mode выключен в сцене. `useArenaComplexMode = false`. Single-arena flow работает как раньше. Новый код — только data + ContextMenu "Generate & Dump Complex" для проверки detерминизма.
- **После PR 3.5.B:** complex mode можно включить флагом, видна геометрия 3 комнат. Все gates стартуют в open state (`obstacle.enabled = false`), encounters не запускаются — просто можно ходить и смотреть. Single-arena flow всё ещё работает при выключенном флаге.
- **После PR 3.5.C:** complex mode полностью функционален. Single-arena flow — fallback при выключенном флаге.
- **После PR 3.5.D:** visual polish, никаких новых gameplay контрактов.

Это явно фиксирует "вот этот PR оставляет проект в таком-то playable state", чтобы reviewers и пользователь могли тестировать каждый PR независимо.

---

## 16. Debugging и logging

Добавить один короткий summary log на генерацию:

```text
[ArenaComplex] seed=12345 index=2 rooms=3 links=2 finalRoom=2 bakeMs=...
```

Дополнительные room logs только под debug flag:

```text
[ArenaComplex] room=1 category=Combat size=15x15 seed=...
```

Debug gizmos:

- room bounds;
- room id;
- gate links;
- gate open/locked state;
- spawn points;
- current room;
- final exit.

Runtime debug overlay позже может показывать:

```text
Complex 2 / Room 1 of 3 / Combat / Enemies 5 alive / Gate locked
```

Для MVP polished UI не нужен, но gizmos/logs нужны обязательно, иначе feature будет сложно отлаживать.

---

## 17. PR-разбиение

### PR 3.5.A - Data model + linear complex generation

Scope:

- добавить `Assets/Scripts/ProceduralArena/Complex/`;
- добавить `ArenaComplexData`;
- добавить `ArenaComplexRoomNode`;
- добавить `ArenaComplexDoorLink`;
- добавить `ArenaComplexGenerator`;
- генерировать deterministic 3-room linear layout;
- использовать только rectangular rooms;
- создать 2 internal links и 1 final exit;
- добавить `ComplexHash` utility (см. §11.2);
- добавить summary log;
- добавить ContextMenu команду "Arena Complex / Generate & Dump" на любом MonoBehaviour-хосте (например, `ArenaComplexDebugHost`), которая генерирует data и пишет в Console читаемый dump (rooms, links, seeds, bounds);
- добавить debug gizmos для room bounds + links, если успевается.

Acceptance:

- один seed + complexIndex всегда даёт одинаковый dump (проверяется двумя последовательными вызовами ContextMenu);
- другой seed даёт отличающийся layout хотя бы по size/seed/profile;
- 3 rooms, 2 links, 1 final exit присутствуют в data;
- gridOrigin/gridSize пар комнат не пересекаются;
- нет `UnityEngine.Random` в новом procedural code (`rg "UnityEngine.Random" Assets/Scripts/ProceduralArena/Complex` пусто);
- нет `string.GetHashCode()` для seed mixing;
- проект компилируется;
- scene GameObject'ы пока не обязательны, но dump читаем человеком.

### PR 3.5.B - Complex geometry build

Scope:

- добавить `ArenaComplexBuilder`;
- построить 3 комнаты под одним `ArenaRoot`;
- построить internal gate frames;
- построить final exit frame;
- сделать реальные wall openings;
- переиспользовать текущие materials/build helpers;
- выполнить один NavMesh bake на весь complex;
- телепортировать player в Room 0.

Acceptance:

- Play Mode создает видимый 3-room complex;
- player физически стоит в Room 0;
- gates визуально находятся между комнатами;
- open gate проходим;
- closed gate блокирует;
- нет длинных коридоров;
- NavMesh bake успешен;
- single-arena mode не сломан.

### PR 3.5.C - Room flow + staged encounters

Scope:

- добавить `ArenaComplexFlowController`;
- добавить room entry triggers;
- запускать encounter только для current room;
- передавать room-local spawn points;
- открывать next gate после clear;
- открывать final gate после clear финальной комнаты;
- реализовать transition на следующий complex.

Acceptance:

- Room 0 стартует и clear'ится;
- Gate 0 открывается только после Room 0 clear;
- Room 1 стартует только после входа игрока;
- Gate 1 открывается только после Room 1 clear;
- Room 2 clear открывает final exit;
- final exit генерирует следующий complex;
- два комплекса подряд можно пройти без перезапуска Play Mode;
- enemy death count не дублируется;
- HP orb drops работают;
- pooling не течет state;
- нет нормальных `[EnemySpawnComposer] Fallback` warnings при настроенных профилях.

### PR 3.5.D - MVP gate readability pass

Scope:

- сделать internal gates читаемыми;
- сделать final exit gate явно отличающимся;
- добавить labels/icons, если нужно;
- добавить simple SFX/VFX placeholders, если это дешево;
- добавить current complex/room в debug overlay.

Acceptance:

- игрок с расстояния понимает, где следующий проход;
- closed/open state очевиден;
- final gate читается как переход на следующий уровень/комплекс;
- gate visuals не мешают видеть врагов, telegraphs и projectiles;
- нет заметного FPS drop от новых lights/effects.

PR 3.5.D можно отложить, если сначала нужен строго functional MVP. Но до презентации feature как "готовой" этот pass нужен.

---

## 18. Testing checklist

### 18.1 Compile checks

- `dotnet build Assembly-CSharp.csproj` проходит.
- Нет missing namespace/reference errors.
- `rg "UnityEngine.Random" Assets/Scripts/ProceduralArena` не показывает новых procedural usages.

### 18.2 Generation checks

- Seed A всегда строит тот же complex.
- Seed B дает хотя бы частично другой complex.
- 3 rooms существуют.
- 2 internal gates существуют.
- final exit существует только в final room.
- gates совпадают с openings.
- room bounds не накладываются неправильно.

### 18.3 Play Mode checks

В `Assets/test.unity`:

1. Включить Arena Complex mode.
2. Start Play Mode.
3. Проверить spawn игрока в Room 0.
4. Проверить, что Gate 0 закрыт.
5. Зачистить Room 0.
6. Проверить, что Gate 0 открылся.
7. Войти в Room 1.
8. Проверить старт Room 1 encounter.
9. Зачистить Room 1.
10. Проверить, что Gate 1 открылся.
11. Войти в Room 2.
12. Зачистить Room 2.
13. Проверить, что final exit открылся.
14. Войти в final exit.
15. Проверить fade/regenerate into next complex.
16. Пройти минимум 2 комплекса подряд.

### 18.4 Combat regression checks

- Kill count увеличивается один раз на врага.
- HP orbs выпадают после убийств.
- Glory-kill работает.
- Stagger не остается stuck на pooled enemies.
- Spitter projectile trail не тянется из старой позиции.
- Active attack slot limits работают.
- Fair-spawn delay работает при близком spawn.
- Disabled enemies лежат под `EnemyPool`, а не копятся в scene root.
- Disabled projectiles лежат под `EnemyProjectilePool`.

### 18.5 Navigation checks

- Player не цепляется за gate frame.
- Enemy NavMeshAgent доходит до player внутри active room.
- Spawn points доступны на NavMesh.
- Locked/future rooms не получают enemies заранее.
- Нет `SetDestination` errors из-за inactive/unbaked agents.

### 18.6 Visual checks

- Internal gates видны из комнаты.
- Closed gate выглядит закрытым.
- Open gate выглядит проходимым.
- Final exit отличается от internal gates.
- Lights/fog/materials не превращают complex в визуальную кашу.
- Labels, если есть, не перекрывают combat visuals.

---

## 19. Риски и mitigation

### Risk: feature снова разрастется в dungeon generator

Mitigation:

- MVP = 3 rooms, linear, rectangular.
- Branching и special rooms только после acceptance.

### Risk: single-arena flow сломается

Mitigation:

- complex mode как отдельный parallel path;
- не переписывать `RunController` в первом PR;
- `ArenaFlowController.EnterArena(...)` оставить рабочим.

### Risk: NavMesh не соединит комнаты

Mitigation:

- gates широкие;
- bake with openings;
- locked state через blocker/barrier;
- минимум сложной геометрии в thresholds.

### Risk: builder станет дублированным и грязным

Mitigation:

- выносить только маленькие helpers;
- не делать большой refactor `ArenaBuilder` в одном PR;
- temporary rectangular builder допустим только как явно отмеченный MVP compromise.

### Risk: encounters считают врагов между комнатами неправильно

Mitigation:

- spawn only current room;
- room-local spawn point list;
- no pre-spawn future rooms;
- clear callback открывает только следующий gate.

### Risk: performance проседает из-за 3 комнат сразу

Mitigation:

- MVP только 3 rooms;
- использовать существующие instancing/material правила;
- не добавлять много lights;
- profile после functional pass.

---

## 20. Будущие расширения после MVP

После acceptance MVP развивать в таком порядке:

1. Branching:
   - одна развилка;
   - combat vs elite/reward route;
   - readable door labels.
2. Special rooms:
   - reward room;
   - shop room;
   - rest room;
   - challenge room.
3. Better room shapes:
   - L/T/octagon;
   - asymmetric final room;
   - biome-dependent layouts.
4. Complex theme:
   - один biome на complex;
   - controlled biome blend;
   - landmark props.
5. Room-local encounter design:
   - spawn groups;
   - ranged nests;
   - elite ambush room;
   - mini-boss room.
6. Map/route UI:
   - lightweight complex map;
   - special room icons;
   - final gate indicator.
7. Long-term run graph integration:
   - run graph node = one complex;
   - boss node = boss complex or final room.

---

## 21. Definition of Done

Arena Complex MVP считается готовым, когда:

- Play Mode генерирует 3-room connected complex;
- player проходит Room 0 -> Room 1 -> Room 2;
- gates открываются только после clear соответствующей комнаты;
- final special gate появляется после clear финальной комнаты;
- final gate генерирует следующий complex;
- минимум 2 комплекса подряд проходятся без restart Play Mode;
- backtracking в зачищенную комнату работает: двери в неё не блокируются повторно, враги повторно не спавнятся;
- close-behind на входе в Pending-комнату работает (back-gate закрывается, пока активен encounter);
- internal gate vs final exit gate визуально различимы (PR 3.5.D pass пройден);
- single-arena mode остается доступен;
- deterministic rules сохранены;
- Phase 3 combat contracts не регресснули;
- `dotnet build Assembly-CSharp.csproj` проходит;
- Unity Editor playtest подтверждает, что feature playable, а не только code-complete.

---

## 22. Рекомендуемая первая задача

Начать с **PR 3.5.A - Data model + linear complex generation**.

Не начинать с визуалов. Сначала нужно доказать, что data layer понятный и стабильный:

- 3 room nodes;
- deterministic seeds;
- 2 internal links;
- final exit;
- summary log;
- debug gizmos, если быстро.

После этого geometry и flow можно делать без гадания, что именно означает complex layout.

