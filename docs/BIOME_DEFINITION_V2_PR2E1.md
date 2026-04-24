# Void Survivor — BiomeDefinition v2 For PR 2.E.1

**Status:** planning artifact  
**Date:** 2026-04-23  
**Scope:** точная структура будущего `BiomeDefinition` перед началом `PR 2.E.1`

---

## 1. Зачем нужен этот файл

Этот документ фиксирует техническую структуру `BiomeDefinition` до начала кода.

Его задача:

- перевести visual-plan в конкретный data-layer
- заранее решить, какие поля реально нужны в `BiomeDefinition`
- не импровизировать в середине `PR 2.E.1`
- отделить обязательные поля для первого pass от optional-полей для следующих подпунктов `PR 2.E`

Этот файл отвечает на вопрос:

> Что именно должно лежать в `BiomeDefinition`, чтобы builder перестал работать только через цвета и начал работать через реальные biome-driven materials.

---

## 2. Текущее состояние

Сейчас [BiomeDefinition.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Arena/BiomeDefinition.cs) хранит в основном:

- цвет пола
- цвет стен
- цвет потолка
- цвет cover/platform/ramp
- цвета и intensity для start/exit/barrier
- debug tint

Это было достаточно для `PR 2.D`, но недостаточно для `PR 2.E.1`, потому что:

- нет material slots
- нет разделения base/accent/trim
- нет atmosphere-параметров
- нет contamination-настроек для `Alien Nexus`
- нет безопасной связи между curator texture set и runtime builder

---

## 3. Главное проектное решение

### 3.1 Что хранить в `BiomeDefinition`

`BiomeDefinition v2` должен хранить **готовые Material references**, а не отдельные raw texture maps.

То есть в `BiomeDefinition` должны лежать:

- `Material floorPrimary`
- `Material wallPrimary`
- и так далее

А не:

- `Texture2D floorBaseColor`
- `Texture2D floorNormal`
- `Texture2D floorRoughness`
- ...

### 3.2 Почему это лучше

Такой подход:

- проще по коду
- лучше для Unity workflow
- лучше для Inspector
- позволяет смешанный texture workflow скрыть внутри готовых материалов
- избавляет `BiomeDefinition` от десятков texture-полей
- упрощает поддержку `Alien Nexus`, где часть ассетов не полностью metallic-workflow

### 3.3 Что остаётся вне `BiomeDefinition`

Внутрь `BiomeDefinition` не нужно тащить:

- все raw texture maps поштучно
- импорт-настройки normal/roughness
- сложную runtime-сборку материалов из PNG/JPG

Это должно быть решено заранее на уровне Unity material assets.

---

## 4. Цели для v2

`BiomeDefinition v2` должен уметь:

- задавать базовые материалы для основных поверхностей
- задавать accent и trim материалы для следующих подпунктов `PR 2.E`
- задавать emissive language биома
- задавать atmosphere-параметры
- задавать силу заражения для `Alien Nexus`
- сохранять обратную совместимость с текущим builder-first pipeline

---

## 5. Предлагаемая структура класса

Ниже не финальный код, а точная целевая схема.

```csharp
[CreateAssetMenu(fileName = "BiomeDefinition", menuName = "VoidSurvivor/Arena/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string biomeId = "void-station";
    public string displayName = "Void Station";

    [Header("Primary Surface Materials")]
    public Material floorPrimary;
    public Material floorAccent;
    public Material wallPrimary;
    public Material wallTrim;
    public Material ceilingPrimary;

    [Header("Gameplay Geometry Materials")]
    public Material coverMaterial;
    public Material platformMaterial;
    public Material rampMaterial;
    public Material propMaterial;

    [Header("Emissive Accent Materials")]
    public Material emissiveAccent;

    [Header("Biome Palette")]
    public Color floorTint = Color.white;
    public Color wallTint = Color.white;
    public Color ceilingTint = Color.white;
    public Color accentTint = Color.white;
    public Color propTint = Color.white;

    [Header("Marker Emissive")]
    public Color startMarkerColor = new Color(0.3f, 1f, 0.5f);
    public float startMarkerIntensity = 1.5f;
    public Color exitMarkerColor = new Color(1f, 0.3f, 0.3f);
    public float exitMarkerIntensity = 1.8f;
    public Color barrierColor = new Color(1f, 0.75f, 0.15f);
    public float barrierIntensity = 2.2f;
    public Color emissiveAccentColor = Color.cyan;
    public float emissiveAccentIntensity = 1.5f;

    [Header("Atmosphere")]
    public Color fogColor = Color.black;
    public float fogStrength = 0f;
    public Color ambientTint = Color.white;
    public Color debugTint = Color.white;

    [Header("Contamination")]
    public bool useContaminationLayer = false;
    public Material contaminationMaterial;
    [Range(0f, 1f)] public float contaminationStrength = 0f;
    [Range(0f, 1f)] public float perimeterContaminationBias = 0.5f;
    [Range(0f, 1f)] public float centerCleanBias = 1f;
}
```

---

## 6. Разделы и смысл каждого поля

## 6.1 Identity

- `biomeId`
  - стабильный внутренний id
  - нужен для логов, debug UI, условной логики

- `displayName`
  - человекочитаемое имя в debug overlay и future UI

## 6.2 Primary Surface Materials

Это ключевой слой для `PR 2.E.1`.

- `floorPrimary`
  - основная поверхность пола
  - используется почти всегда

- `floorAccent`
  - акцентные полосы, секции, ring-zones
  - в `PR 2.E.1` может храниться заранее, даже если активно используется позже

- `wallPrimary`
  - основной материал стен

- `wallTrim`
  - рёбра, рамки, trims, edge elements
  - особенно важен для `PR 2.E.2`

- `ceilingPrimary`
  - основной материал потолка

## 6.3 Gameplay Geometry Materials

- `coverMaterial`
  - cover blocks
  - должен быть читаемым и не сливаться со стеной

- `platformMaterial`
  - материал платформ в parkour-аренах
  - нужен отдельно, чтобы не пытаться всегда брать тот же пол

- `rampMaterial`
  - материал рамп
  - должен быть стабильно читаемым как traversable surface

- `propMaterial`
  - будущие builder-props, стойки, консоли, технические блоки

## 6.4 Emissive Accent Materials

- `emissiveAccent`
  - материал для визуальных полос, guidance-элементов, accent panels
  - нужен отдельно от start/exit/barrier, потому что это слой стиля, а не gameplay marker

## 6.5 Biome Palette

Эти поля нужны как безопасный слой поверх материалов.

- `floorTint`
- `wallTint`
- `ceilingTint`
- `accentTint`
- `propTint`

Зачем они нужны:

- мягкий цветовой сдвиг без клонирования всех материалов
- возможность сделать `Alien Nexus` более фиолетовым на той же sci-fi базе
- быстрый art tuning без перекомпоновки всей material library

## 6.6 Marker Emissive

Эти поля остаются из текущей системы, потому что они уже полезны и не должны ломаться:

- `startMarkerColor`
- `startMarkerIntensity`
- `exitMarkerColor`
- `exitMarkerIntensity`
- `barrierColor`
- `barrierIntensity`

Добавляются:

- `emissiveAccentColor`
- `emissiveAccentIntensity`

Они отделяют:

- gameplay markers
- стиль биома

## 6.7 Atmosphere

- `fogColor`
  - цвет atmosphere/fog pass

- `fogStrength`
  - безопасная scalar-настройка силы fog/tint

- `ambientTint`
  - мягкий общий biome-grade

- `debugTint`
  - оставить для overlay и dev-tools

## 6.8 Contamination

Этот блок нужен в основном для `Alien Nexus`.

- `useContaminationLayer`
  - включает hybrid-mode заражения

- `contaminationMaterial`
  - локальный organic/corrupted material для overlay-пятен и trims

- `contaminationStrength`
  - насколько агрессивно биом должен использовать contamination layer

- `perimeterContaminationBias`
  - насколько заражение тяготеет к краям, углам и стыкам

- `centerCleanBias`
  - насколько чистой остаётся боевая центральная зона

Это позволит реализовать правило:

- `Void Station` -> структура
- `Alien Nexus` -> заражённая структура

---

## 7. Что обязательно для PR 2.E.1

Обязательный минимум:

- `biomeId`
- `displayName`
- `floorPrimary`
- `wallPrimary`
- `ceilingPrimary`
- `coverMaterial`
- `platformMaterial`
- `rampMaterial`
- `propMaterial`
- `emissiveAccent`
- `startMarkerColor`
- `startMarkerIntensity`
- `exitMarkerColor`
- `exitMarkerIntensity`
- `barrierColor`
- `barrierIntensity`
- `debugTint`

Желательно добавить сразу, даже если не всё будет использоваться в первом коммите:

- `floorAccent`
- `wallTrim`
- `floorTint`
- `wallTint`
- `ceilingTint`
- `accentTint`
- `emissiveAccentColor`
- `emissiveAccentIntensity`

Можно оставить на later pass, если нужно урезать риск:

- `fogColor`
- `fogStrength`
- `ambientTint`
- `useContaminationLayer`
- `contaminationMaterial`
- `contaminationStrength`
- `perimeterContaminationBias`
- `centerCleanBias`

---

## 8. Маппинг на текущий builder

На первом этапе `ArenaBuildMaterials` должен мапить поля так:

- `floor` <- `floorPrimary`
- `wall` <- `wallPrimary`
- `ceiling` <- `ceilingPrimary`
- `cover` <- `coverMaterial`
- `platform` <- `platformMaterial`, иначе fallback в `floorAccent` или `floorPrimary`
- `ramp` <- `rampMaterial`, иначе fallback в `floorPrimary`
- `startMarker` <- procedural emissive material from marker fields
- `exitMarker` <- procedural emissive material from marker fields
- `barrier` <- procedural emissive material from marker fields

Поля:

- `floorAccent`
- `wallTrim`
- `propMaterial`
- `emissiveAccent`

на `PR 2.E.1` могут уже храниться в `BiomeDefinition`, даже если их полноценное builder-использование придёт в `PR 2.E.2` и `PR 2.E.3`.

Это правильно, потому что data model надо заложить сразу.

---

## 9. Конкретное наполнение по двум биомам

## 9.1 Void Station

- `floorPrimary` -> material from `Sci_fi_Metal_Panel_007_SD`
- `floorAccent` -> material from `Sci_fi_Metal_Panel_007_SD` after the 2026-04-24 cyan follow-up
- `wallPrimary` -> material from `Sci-fi_Walll_001_SD`
- `wallTrim` -> material from `Sci_fi_Metal_Panel_007_SD` after the 2026-04-24 cyan follow-up
- `ceilingPrimary` -> material from `Sci-fi_Walll_001_SD`
- `coverMaterial` -> `Sci_fi_Metal_Panel_007_SD`
- `platformMaterial` -> `Sci_fi_Metal_Panel_007_SD`
- `rampMaterial` -> `Sci_fi_Metal_Panel_007_SD`
- `propMaterial` -> `Sci_fi_Metal_Panel_007_SD` after the 2026-04-24 cyan follow-up
- `emissiveAccent` -> walkway emissive material
- `useContaminationLayer` -> `false`

## 9.2 Alien Nexus

- `floorPrimary` -> tinted `Sci_fi_Metal_Panel_007_SD`
- `floorAccent` -> tinted `Sci_fi_Metal_Panel_009_SD`
- `wallPrimary` -> tinted `Sci-fi_Walll_001_SD`
- `wallTrim` -> tinted `Sci_fi_Metal_Panel_009_SD`
- `ceilingPrimary` -> tinted `Sci-fi_Walll_001_SD`
- `coverMaterial` -> tinted `Sci_fi_Metal_Panel_007_SD`
- `platformMaterial` -> tinted `Sci_fi_Metal_Panel_009_SD`
- `rampMaterial` -> tinted `Sci_fi_Metal_Panel_007_SD`
- `propMaterial` -> `Alien_Metal_002_SD`
- `emissiveAccent` -> walkway emissive material recolored to violet/magenta
- `useContaminationLayer` -> `true`
- `contaminationMaterial` -> `Alien_Muscle_001`

---

## 10. Чего не нужно добавлять в v2

Не стоит добавлять:

- по 5-6 `Texture2D` полей на каждый слот
- отдельные texture-поля под каждую PBR-карту
- сложные serialized dictionaries
- biome-specific placement rules прямо в `BiomeDefinition`
- списки из десятков prop references уже на этом этапе

Причина:

- это раздует scope
- ухудшит Inspector usability
- замедлит `PR 2.E.1`

---

## 11. Рекомендуемый порядок внедрения

1. Сначала расширить `BiomeDefinition` material-based полями.
2. Затем обновить `ArenaBuildMaterials.CreateDefaults(...)`, чтобы он мог брать не только цвета, но и material references.
3. После этого перевести два текущих biome assets на новую структуру.
4. Только потом идти в builder details / trims / floor patterns / decor.

---

## 12. Краткий вывод

`BiomeDefinition v2` должен стать:

- не контейнером "просто цветов",
- а компактным biome-style profile,
- который хранит готовые runtime materials,
- palette tuning,
- emissive language,
- и, при необходимости, contamination tuning.

Это даст нам чистую техническую базу для `PR 2.E.1` и не перегрузит архитектуру перед Phase 3.
