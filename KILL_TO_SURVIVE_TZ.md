# Void Survivor — Система "Kill-to-Survive" (Техническое Задание)

## Цель

Описывает ключевой игровой цикл из GDD: игрок не может отсидеться и восстановиться — он обязан **убивать чтобы выживать**. Реализует четыре тесно связанных подсистемы:

1. **HP-орбы** — враги при смерти могут уронить лечилку; игрок подбирает касанием.
2. **Стаггер-состояние** — враги с низким HP становятся "готовыми к добиванию" и визуально пульсируют.
3. **Glory Kill** — добивание застаггеренного врага через Void Blade лечит игрока на большую сумму.
4. **Kill streak speed boost** — серия убийств за короткий промежуток даёт временный буст скорости.

Плюс одна чистка: **удалить любую пассивную регенерацию HP** (иначе всё выше теряет смысл).

Работа делится на **два последовательных PR** (PR A и PR B), чтобы сцена оставалась играбельной на каждом коммите.

---

## Контекст проекта

Текущее состояние (после Weapon System PR B):

- `Health` компонент в [Assets/test/Health.cs](Assets/test/Health.cs) — имеет `maxHealth`, `currentHealth`, события `onDeath`, `onTakeDamage`, `onHealthChanged`. **Метода `Heal` нет.** Пассивной регенерации в коде на данный момент нет (проверено).
- Враг = prefab с `Health` + `SimpleEnemyAI` + `NavMeshAgent`. Спавнится в `GameManager.SpawnEnemy`; менеджер подписывается на `enemyHealth.onDeath`.
- Player = `Player` GameObject с `Health`, `PlayerController`, `WeaponManager`.
- Void Blade = слот 5, `MeleeArcFireMode`, `WeaponCategory.Melee` в `WeaponDefinition`.
- `PlayerController.moveSpeed` — публичный float, читается напрямую в `HandleMovement`.

Переиспользование вместо редизайна: никаких рефакторов существующего боевого кода сверх минимальных точек интеграции.

---

## Общая цель

- Убийство врагов — единственный способ лечиться (орбы + glory kill).
- Glory kill = **риск за большую награду** — игрок должен сблизиться в меле с низко-HP врагом.
- Буст скорости на стриках поощряет агрессию и комбо с dash/slide.
- Все подсистемы опциональны через компоненты на префабах/игроке, минимум связей.
- **Три точки расширения (seams) заложены сразу** — чтобы будущая система апгрейдов/магазина не требовала переписывания этих компонентов. См. раздел "Точки расширения".

---

## Scope

### В скоупе

- `HealthPickup` компонент + HP-орб prefab.
- `PickupSpawner` — хелпер, спавнит настраиваемый prefab в точке мира (сейчас для орбов, позже для ammo и апгрейд-шардов).
- `EnemyLootTable` — компонент на враге с таблицей дропа (**seam для будущей настройки дропа**).
- `PlayerStats` — централизованный провайдер числовых параметров игрока (**seam для будущих апгрейдов**).
- `EnemyStagger` — компонент на враге, подписан на `Health.onHealthChanged`, экспозит `IsStaggered` + `OnStaggerChanged`.
- Визуал стаггера — простой material flash / emissive pulse (без анимаций и сложных VFX).
- `GloryKillDetector` — на Player'е; проверяет наличие застаггеренной цели перед игроком при ударе Void Blade.
- `IGloryKillPolicy` + `AlwaysAllowPolicy` — **seam для будущих условий glory kill** (серии, заряды и т.д.).
- `KillStreakTracker` — считает смерти врагов в скользящем окне, шлёт событие `OnStreakActive(bool)` + экспозит текущий стрик.
- `PlayerSpeedModifier` — маленький API на `PlayerController` для внешних систем-бустеров.
- `Health.Heal(float amount)` — метод с клэмпом по `maxHealth`.
- Привязка орбов к смерти врага через `EnemyLootTable`.
- Аудит `PlayerController` и всех скриптов на пассивную регенерацию; если найдена — удалить.

### Вне скоупа

- Полный HUD со стрик-счётчиком и glory-kill промптом. UI-пасс будет отдельной фазой (Phase 4 Polish); сейчас все нужные события экспозятся, подписать HUD позже тривиально.
- Glory kill-анимация (zoom камеры, slow-mo, финишер-клип).
- Сложная физика/дуга орбов (статичный спавн сейчас; bounce/magnet откладывается).
- Damage-over-time, lifesteal, armor (DOOM-style броня — дело будущих фаз).
- Сетевой трекинг убийств.
- Сама система апгрейдов / магазина — её задача только предусмотреть (seams заложены).
- Editor-тулинг сверх стандартного инспектора.

---

## Принципы дизайна

- **Событийный подход.** `Health.onDeath` и `Health.onHealthChanged` уже есть — используем их, не опросы через Update.
- **Никаких новых синглтонов.** `KillStreakTracker`, `PlayerStats` живут на Player'е или `GameManager`, но не становятся `static` god-object'ами.
- **Компоненты опциональны.** Враг без `EnemyStagger` всё ещё нормально умирает. Враг без `EnemyLootTable` просто ничего не роняет.
- **Не трогаем сигнатуры `WeaponBase`.** Glory Kill — сайд-система, наблюдающая за Void Blade, а не модификация `MeleeArcFireMode`.
- **Additive > invasive.** `PlayerSpeedModifier` означает что `moveSpeed` в инспекторе остаётся авторским значением; множитель применяется в момент чтения.
- **Seams — не over-engineering.** Каждый seam закрывает конкретный именованный сценарий будущей фичи (апгрейды, спец-мобы, шоп), а не гипотетический.

---

## Целевая архитектура

### `Health.Heal(float amount)` — добавление в существующий класс

- Добавляет `amount` к `currentHealth`, клэмп в `[0, maxHealth]`.
- Вызывает `onHealthChanged`.
- No-op если `currentHealth <= 0` (хилом не воскрешаем).
- Это **единственное** изменение `Health.cs`.

### `PlayerStats` (новый MonoBehaviour на Player'е) — SEAM #1

Централизованный провайдер численных параметров, которыми управляют апгрейды.

Поля:

- `float orbHealAmount = 5f`
- `float gloryHealAmount = 25f`
- `float gloryBonusDamage = 999f`
- `int streakThreshold = 5`
- `float streakWindowSeconds = 10f`
- `float streakBoostMultiplier = 1.2f`
- `float streakBoostDuration = 5f`

Правила:

- Все компоненты системы (`HealthPickup`, `GloryKillDetector`, `KillStreakTracker`) читают числа **отсюда**, не держат собственные поля-дубликаты.
- Апгрейд в будущем просто пишет в поля: `playerStats.orbHealAmount *= 1.2f;` — никакой другой код не меняется.
- Поля публичные (или публичные properties с setter'ами) — не шифруемся от будущих систем.

Опциональное событие: `event Action OnStatsChanged` — если апгрейд-UI захочет реактивно показывать текущие значения. Подключается когда нужно.

### `HealthPickup` (новый MonoBehaviour)

Ответственность:

- Пикап в пространстве, восстанавливает HP когда игрок входит в триггер.
- При подборе читает `PlayerStats.orbHealAmount` с Player'а, вызывает `Health.Heal(...)`.
- Уничтожается при подборе или через `lifetime` секунд.

Поля:

- `float lifetime = 15f`
- `float magnetRange = 0f` — если > 0, орб притягивается к игроку в радиусе (на этом этапе 0; включится полировкой позже, без правки кода)
- `Rigidbody optionalRigidbody` — опциональный (если присутствует на prefab'е — работает физика bounce при спавне; если нет — статика)
- `AudioClip pickupSfx` (опционально)
- `GameObject pickupVfx` (опционально)

Правила:

- `OnTriggerEnter`: ищет `Health` на вошедшем объекте; лечит только если это объект с тегом `Player`.
- Не лечит мёртвого игрока.
- Хил-значение **не хранится в компоненте** — берётся из `PlayerStats` (seam #1).

### `PickupSpawner` (статический хелпер или MonoBehaviour на GameManager)

Ответственность:

- Одна точка входа: `Spawn(GameObject prefab, Vector3 position, int count = 1)`.
- Небольшой случайный XZ-разброс между несколькими дропами.

Почему не просто `Instantiate` инлайном: в будущих фазах будут падать несколько типов пикапов (ammo, upgrade shards). Прогон через один метод = одно место для добавления вариативности/пулинга позже.

### `EnemyLootTable` (новый MonoBehaviour на prefab'е врага) — SEAM #2

Ответственность:

- Хранит таблицу того, что враг может уронить при смерти, с вероятностями и количествами.
- На `Health.onDeath` своего GameObject'а проходит по таблице и спавнит выигравшие записи через `PickupSpawner`.

Структура:

```csharp
[Serializable]
public class LootEntry
{
    public GameObject prefab;
    [Range(0f, 1f)] public float chance = 1f;
    public int minCount = 1;
    public int maxCount = 1;
}

public class EnemyLootTable : MonoBehaviour
{
    [SerializeField] private LootEntry[] drops;
    // подписывается на Health.onDeath, роллит drops при вызове
}
```

Правила:

- Обычный моб: таблица либо пустая, либо `HP orb @ chance=0.15` (редкий дроп).
- Спец-моб (в будущем): `HP orb @ chance=1.0` + `AmmoOrb @ chance=0.5`.
- `GameManager` **перестаёт знать про орбы** — он просто спавнит врага с loot table, дроп происходит автоматически.

Будущие расширения (без правок `EnemyLootTable`):

- Апгрейд "+10% шанс дропа" — в `EnemyLootTable.Roll()` добавится чтение модификатора из Player'а (через `PlayerStats` или отдельный `LootModifierProvider`). Один if, без затрагивания остального.
- Новые типы пикапов — новый prefab в инспекторе, ноль кода.

### `EnemyStagger` (новый MonoBehaviour на prefab'е врага)

Ответственность:

- Подписан на `Health.onHealthChanged` своего же GameObject'а.
- Становится застаггеренным когда `currentHealth / maxHealth <= staggerThreshold`.
- Остаётся в этом состоянии до смерти (не сбрасывается хилом — враги в Phase 1 не лечатся).
- Гоняет простую визуалку: `Renderer.material.EnableKeyword("_EMISSION")` + пульсация emission color.
- Экспозит `bool IsStaggered` и `event Action<bool> OnStaggerChanged`.

Поля:

- `[Range(0f,1f)] float staggerThreshold = 0.2f`
- `Renderer[] targetRenderers` (авторезолв если пусто — seam для будущей замены на shader graph / частицы)
- `Color staggerEmission = Color.red`
- `float pulseSpeed = 4f`

Будущая полировка (без ада):

- Заменить material flash на shader graph с пульсом — просто другой материал в инспекторе, компонент не трогается.
- Добавить `ParticleSystem[] pulseParticles` — ~5 строк на включение/выключение в `OnStaggerChanged`, контракт `IsStaggered` не меняется.

### `IGloryKillPolicy` + `AlwaysAllowPolicy` (новые классы) — SEAM #3

Решает **когда** glory kill разрешён — чтобы будущие апгрейды могли добавить условия.

```csharp
public struct GloryKillContext
{
    public Health target;
    public WeaponBase weapon;
    public KillStreakTracker streakTracker;
}

public interface IGloryKillPolicy
{
    bool CanGloryKill(GloryKillContext ctx);
    void NotifyGloryKillPerformed(GloryKillContext ctx);
}

public class AlwaysAllowPolicy : MonoBehaviour, IGloryKillPolicy
{
    public bool CanGloryKill(GloryKillContext ctx) => true;
    public void NotifyGloryKillPerformed(GloryKillContext ctx) { }
}
```

Дефолт на Player'е: `AlwaysAllowPolicy` — поведение как сейчас.

Будущие реализации (без правок `GloryKillDetector`):

- `StreakRequiredPolicy` — `CanGloryKill` возвращает `ctx.streakTracker.CurrentStreak >= requiredStreak`, `NotifyGloryKillPerformed` сбрасывает счётчик или отнимает заряд.
- `ChargeLimitPolicy` — отдельный счётчик зарядов, восстанавливается по таймеру.

Апгрейд в будущем = замена компонента политики на Player'е.

### `GloryKillDetector` (новый MonoBehaviour на Player'е)

Ответственность:

- Подписан на `WeaponManager.OnWeaponEquipped` чтобы знать когда активен Void Blade.
- При активном — подписан на `WeaponBase.OnFired` конкретно Void Blade.
- На fire: делает свой `OverlapSphere` (та же математика что и `MeleeArcFireMode`) и ищет первого застаггеренного врага.
- Спрашивает у `IGloryKillPolicy.CanGloryKill(ctx)` — разрешено ли.
- Если разрешено: применяет `PlayerStats.gloryBonusDamage` + вызывает `playerHealth.Heal(PlayerStats.gloryHealAmount)` + уведомляет policy через `NotifyGloryKillPerformed`.
- Лечит **один раз** за взмах (первая цель = glory).

Как определяет что активна именно Void Blade: по `WeaponDefinition.weaponCategory == Melee` **и** по `weaponId == "void_blade"`. (Жёсткая привязка к Void Blade зафиксирована по твоему запросу.)

Поля:

- `LayerMask hitMask`
- `AudioClip gloryKillSfx` (опционально)
- `GameObject gloryKillVfx` (опционально, спавнится в позиции врага)

Почему отдельный компонент, а не внутри `MeleeArcFireMode`:

- MeleeArcFireMode остаётся переиспользуемым для будущих меле-оружий без glory.
- Никаких новых зависимостей в weapon-system сборке.

### `KillStreakTracker` (новый MonoBehaviour на Player'е)

Ответственность:

- Держит очередь таймстемпов убийств.
- На каждую смерть врага пушит `Time.time` в очередь.
- Каждый кадр выкидывает из очереди таймстемпы старше `PlayerStats.streakWindowSeconds`.
- Когда количество в очереди переходит через `PlayerStats.streakThreshold` снизу → активируется буст, запускается таймер `PlayerStats.streakBoostDuration`.
- Ставит `PlayerSpeedModifier` на Player'е в `PlayerStats.streakBoostMultiplier`; сбрасывает в 1f когда таймер истёк.
- Экспозит `int CurrentStreak`, `bool IsBoostActive`, события `OnStreakChanged(int)` и `OnBoostChanged(bool)` — пригодятся для HUD позже.

Подписка на смерти: либо через `GameManager` (лучше, один канал), либо через общий статический `event Action OnAnyEnemyDied` на `Health`. В ТЗ выберем подписку через `GameManager.OnEnemyDied` — он уже умеет считать врагов.

### `PlayerSpeedModifier` — добавление в `PlayerController`

Подход:

- Добавить приватное `float speedMultiplier = 1f;` с публичным сеттером + геттером.
- Заменить чтения `moveSpeed` внутри `HandleMovement` на `moveSpeed * speedMultiplier` там, где это применяется (обычная ходьба, воздух). Dash/slide — независимые скорости, не умножаются.
- `KillStreakTracker` вызывает `player.SetSpeedMultiplier(x)` на включение/выключение буста.

Правила:

- Множитель ровно 1 когда ничего не применено — никакого скрытого роста скорости.
- Только один источник буста пока что; наслоение нескольких (апгрейд + стрик одновременно) — будущее расширение через переход на список множителей. Контракт `SetSpeedMultiplier` останется совместим.

---

## Точки расширения (seams) — сводка

Три шва, заложенные **сразу**, которые закрывают явно названные тобой будущие сценарии. Каждый — один маленький файл в этом PR, но экономит полный рефакторинг позже.

| Будущая фича | Какой seam закрывает | Что меняется при внедрении |
|---|---|---|
| Апгрейд «+X% хила от орбов» | `PlayerStats` | Одна строка в обработчике покупки |
| Апгрейд «+X% шанс дропа» | `EnemyLootTable` (читает модификатор из Player'а) | Один if в `Roll()` |
| Спец-моб с гарантированным дропом HP | `EnemyLootTable` | Только инспектор (другой prefab врага с другой таблицей) |
| Спец-моб с ammo-дропом | `EnemyLootTable` | Только инспектор (новый LootEntry) |
| Glory kill только после серии | `IGloryKillPolicy` (новая реализация) | Новый компонент-политика, свап на Player'е |
| Glory kill с зарядами | `IGloryKillPolicy` (новая реализация) | Новый компонент-политика, свап на Player'е |
| Апгрейд "+X% скорости навсегда" | `PlayerSpeedModifier` (расширение до списка множителей) | Один рефакторинг API, клиенты не меняются |
| Новый тип пикапа (ammo/armor/shard) | `PickupSpawner` + новая `LootEntry` | Новый компонент пикапа, старые не трогаются |
| HUD со стрик-счётчиком | События `KillStreakTracker` / `GloryKillDetector` | Только UI, подписка |

---

## Точки интеграции

### GameManager

- `SpawnEnemy`: после создания врага больше не подписывается на `onDeath` для спавна орба (это переходит в `EnemyLootTable`). Остаётся подписка `enemyHealth.onDeath.AddListener(OnEnemyDied)` для подсчёта волны и для `KillStreakTracker`.
- Добавить публичное событие `event Action OnEnemyKilled` (уже есть `OnEnemyDied` как приватный метод — сделать public event поверх).
- Поле `GameObject hpOrbPrefab` **удаляется** — теперь live-prefab'ы врагов сами знают что ронять через свой `EnemyLootTable`.

### Player

- Добавить `Health` (уже есть), `PlayerStats`, `AlwaysAllowPolicy` (реализующий `IGloryKillPolicy`), `GloryKillDetector`, `KillStreakTracker`.
- Тег `Player` (уже стоит).

### Enemy prefab

- Добавить `EnemyStagger`. Прописать `targetRenderers` если у врага несколько мешей.
- Добавить `EnemyLootTable`. В простейшем случае таблица с одной записью: `HP orb @ chance=0.15`.

---

## Стартовые значения (все в `PlayerStats`)

Базовый тюнинг — балансируется позже в плейтесте, апгрейдами в будущем.

| Параметр | Значение | Обоснование |
|---|---|---|
| `orbHealAmount` | 5 | Маленький — поощряет агрессию |
| `gloryHealAmount` | 25 | 5× обычного орба — оправдывает риск сближения |
| `gloryBonusDamage` | 999 | Гарантированный instakill на stagger-HP |
| `streakThreshold` | 5 убийств | Достижимо в волне, не тривиально |
| `streakWindowSeconds` | 10 | Хватает на несколько врагов |
| `streakBoostMultiplier` | 1.2 | Чувствуется, не ломает движение |
| `streakBoostDuration` | 5 | Ощутимо, но заставляет снова убивать |
| `EnemyLootTable.drops[0].chance` (обычный моб) | 0.15 (15%) | Орб — редкое событие, не постоянный фон |
| `EnemyStagger.staggerThreshold` | 0.2 (20% HP) | Награда за прицельные убийства |
| `HealthPickup.lifetime` | 15 с | Хватает подобрать, не захламляет арену |

---

## Требуемая структура файлов

```text
Assets/
  Scripts/
    Combat/
      Pickups/
        HealthPickup.cs
        PickupSpawner.cs
      Enemies/
        EnemyStagger.cs
        EnemyLootTable.cs
      Player/
        PlayerStats.cs
        GloryKillDetector.cs
        IGloryKillPolicy.cs
        AlwaysAllowPolicy.cs
        KillStreakTracker.cs
    Prefabs/
      HPOrb.prefab          (создаётся в редакторе, не кодом)
```

`PlayerController.cs` получает метод `SetSpeedMultiplier` на месте (новый файл не нужен).
`Health.cs` получает метод `Heal` на месте.

---

## План миграции — два PR

### PR A — HP Orbs + Heal API + Loot Table + PlayerStats (базовый seam #1 и #2)

Цель: убийство врага **иногда** роняет орб, который восстанавливает HP через централизованную stat-систему. Игрок всё ещё может умереть.

Шаги:

1. Добавить `Health.Heal(float amount)`.
2. Аудит `PlayerController` и всех скриптов на пассивную регенерацию — удалить если найдено. (Ожидается: ничего.)
3. Создать `PlayerStats.cs` — пока только поля, относящиеся к орбам (`orbHealAmount`). Остальные добавятся в PR B.
4. Создать папку `Scripts/Combat/Pickups/`.
5. Написать `HealthPickup.cs` (читает `orbHealAmount` из `PlayerStats`) + `PickupSpawner.cs`.
6. Создать папку `Scripts/Combat/Enemies/`.
7. Написать `EnemyLootTable.cs` + `LootEntry`.
8. Собрать `HPOrb.prefab` в редакторе: маленькая сфера, trigger collider, `HealthPickup`, emissive материал. Опциональный `Rigidbody` можно не добавлять сейчас (bounce — полировка).
9. На prefab'е врага добавить `EnemyLootTable` с одной записью: `prefab = HPOrb`, `chance = 0.15`, `min/maxCount = 1`.
10. `GameManager.SpawnEnemy` больше не занимается орбами — вся логика дропа внутри врага.
11. Плейтест: убить врагов (10+) → изредка появляются орбы → подбор → +5 HP → HUD обновляется. Смерть всё ещё перезагружает сцену.

**PR A acceptance:** игрок лечится только через орбы. Дроп работает через таблицу на враге. `PlayerStats` присутствует на Player'е. Никаких других новых систем.

### PR B — Stagger + Glory Kill + Kill Streak + Policy (seam #3)

Цель: полный цикл — слабые враги мигают, Void Blade добивает их на хил, серии дают буст скорости. `IGloryKillPolicy` встроен по умолчанию как `AlwaysAllowPolicy`, готов к замене апгрейдом в будущем.

Шаги:

1. Расширить `PlayerStats` оставшимися полями (`gloryHealAmount`, `gloryBonusDamage`, `streakThreshold`, `streakWindowSeconds`, `streakBoostMultiplier`, `streakBoostDuration`).
2. Создать папку `Scripts/Combat/Player/` (если её ещё нет) — положить `PlayerStats.cs` сюда если был в другом месте.
3. Написать `IGloryKillPolicy.cs` + `AlwaysAllowPolicy.cs` + `GloryKillContext` struct.
4. Написать `GloryKillDetector.cs` — подписка на `WeaponManager.OnWeaponEquipped`, проверка Void Blade, OverlapSphere, policy check, bonus damage + heal через `PlayerStats`.
5. Написать `EnemyStagger.cs` — повесить на prefab врага; проверить emissive pulse на низком HP.
6. Написать `KillStreakTracker.cs` — подписка на `GameManager.OnEnemyKilled` (сделать event public если приватный).
7. Добавить `SetSpeedMultiplier` в `PlayerController`; использовать `moveSpeed * speedMultiplier` в `HandleMovement`.
8. Сцена: на Player'е добавить `PlayerStats`, `AlwaysAllowPolicy`, `GloryKillDetector`, `KillStreakTracker`. На враге: `EnemyStagger`.
9. Плейтест:
   - враги мигают при низком HP
   - удар Void Blade по застаггеренному = instakill + +25 HP
   - удар по нестаггер-врагу = только обычный урон
   - 5 убийств за 10с = 1.2× скорость на 5с
   - меньше убийств = ничего

**PR B acceptance:** все пять пунктов из чек-листа Phase 1 "Kill-to-Survive" в PROGRESS.md отмечены; нет регрессий от PR A; `IGloryKillPolicy` присутствует с дефолтной реализацией.

---

## Критерии приёмки (после PR B)

- Игрок лечится только через орбы или glory kill; никакой пассивной регенерации.
- `Health.Heal` никогда не превышает `maxHealth`.
- Убитый враг дропает HP-орб согласно своей `EnemyLootTable` (по умолчанию 15% шанс).
- Орб лечит на `PlayerStats.orbHealAmount` (=5 по умолчанию); исчезает через `HealthPickup.lifetime` (=15с).
- Враги с HP < `EnemyStagger.staggerThreshold` визуально мигают.
- Удар Void Blade по застаггеренному, при `AlwaysAllowPolicy.CanGloryKill == true` = instakill + +`gloryHealAmount` HP.
- Удар Void Blade по НЕ застаггеренному = только обычный меле-урон.
- 5 убийств за 10 с → `streakBoostMultiplier`× скорость на `streakBoostDuration` секунд.
- Множитель скорости чисто возвращается в 1× после таймера.
- Движение (walk/dash/slide/jump) под бустом ощущается корректно.
- Волны, AI врагов, переключение оружий, перезарядка — без изменений.
- Все числовые параметры подсистем читаются из `PlayerStats` — замена значения в `PlayerStats` меняет поведение без правок других компонентов.

---

## Ограничения для реализующего агента

- Не модифицировать `WeaponBase`, `WeaponManager`, `FireModeBase`, любые `FireMode*` классы. Glory Kill **наблюдает**, а не мутирует.
- Не рефакторить `SimpleEnemyAI`.
- Не добавлять синглтонов сверх существующих (`GameManager.instance` ок).
- Минимум правок в `Health.cs` — только `Heal`.
- Все числовые параметры, которые будет трогать система апгрейдов, живут в `PlayerStats`. Не дублировать их в других компонентах.
- Сохранить совместимость с текущим wiring сцены; новые компоненты добавляются в редакторе на существующие prefab'ы.
- Не реализовывать саму систему апгрейдов / магазина — только seams.
- Не реализовывать HUD для стрика/glory/буста — события экспозятся, UI делается отдельным PR после Phase 2.

---

## Рекомендуемые упрощения (в духе ТЗ оружий)

- Орбы — статичные, без Rigidbody. Bounce-полировка потом через включение Rigidbody в prefab'е, код не трогается.
- Магнит орбов к игроку — отложен. Поле `magnetRange` уже есть в компоненте, включится в инспекторе позже.
- Стаггер-визуал — material emission pulse. Смена на shader graph / частицы — только замена материала / добавление `ParticleSystem[]` в инспекторе без правок контракта.
- `GloryKillDetector` делает свой `OverlapSphere` — да, дублирует логику `MeleeArcFireMode`, но это цена изоляции weapon-system'а от glory-логики. Принято осознанно.
- `KillStreakTracker` использует простой `List<float>` с фильтром по таймстемпам, не ring buffer — для числа <20 элементов это быстрее и читаемее.

---

## Точки расширения (полная таблица)

| Будущая фича | Где подключается | Стоимость |
|---|---|---|
| Апгрейд «+X% хила от орбов» | `PlayerStats.orbHealAmount` — один setter | 1 строка |
| Апгрейд «+X% glory heal» | `PlayerStats.gloryHealAmount` | 1 строка |
| Апгрейд «+X% шанс дропа» | `EnemyLootTable.Roll()` — читать `LootModifierProvider` с Player'а | 1 if + новый компонент-провайдер |
| Апгрейд «стрик-порог уменьшен» | `PlayerStats.streakThreshold` | 1 строка |
| Glory kill только после стрика | Новый `StreakRequiredPolicy : IGloryKillPolicy` | 1 новый файл, свап компонента |
| Glory kill с зарядами | Новый `ChargeLimitPolicy : IGloryKillPolicy` | 1 новый файл, свап компонента |
| Спец-моб с гарантированным HP-дропом | Новый prefab врага с другой `EnemyLootTable` | только инспектор |
| Спец-моб с ammo-дропом | `LootEntry` с ammo prefab в таблице спец-моба | только инспектор |
| Новый тип пикапа | Новый компонент-пикап + `LootEntry` | 1 новый компонент |
| Наслоение бустов скорости (streak + upgrade) | Переход `PlayerSpeedModifier` на список множителей | 1 рефакторинг, клиенты не меняются |
| HUD: стрик-счётчик | `KillStreakTracker.OnStreakChanged` | только UI |
| HUD: glory-prompt | `EnemyStagger.OnStaggerChanged` в радиусе игрока | только UI |
| HUD: индикатор буста | `KillStreakTracker.OnBoostChanged` | только UI |
| Glory kill slow-mo / zoom | Новое событие `GloryKillDetector.OnGloryKill` + cinematic listener | additive, детектор не меняется |
| Частицы стаггера | `EnemyStagger` + `ParticleSystem[]` | ~5 строк |
| Shader graph стаггера | Смена материала в `EnemyStagger.targetRenderers` | только инспектор |

Все эти фичи — **additive**. Ни одна не требует правки уже написанных компонентов.

---

## Итоговый интент

Цикл Kill-to-Survive — **ключевая причина** почему DOOM-style combat ощущается иначе чем cover-shooter: агрессия = выживание. Задача этого ТЗ — сделать цикл механически работающим с минимумом новой сложности **сейчас**, но с правильно заложенными швами для будущей системы апгрейдов/магазина, чтобы Phase 2 (процедурные арены) и Phase 4 (шоп + апгрейды) могли на него опереться без переписывания.

Если нужны компромиссы, приоритеты:

- корректность важнее хитрости
- играбельность важнее чистоты абстракций
- изолированные системы важнее общего клея
- additive расширения важнее спекулятивных интерфейсов
- seams только под названные будущие сценарии, а не гипотетические

---

## Implementation Status

(заполняется реализующим агентом по мере выполнения, как в [WEAPON_SYSTEM_TZ.md](./WEAPON_SYSTEM_TZ.md))

### PR A — HP Orbs + Heal API + Loot Table + PlayerStats

(pending)

### PR B — Stagger + Glory Kill + Kill Streak + Policy

(pending)
