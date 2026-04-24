# Void Survivor — PR 2.E Visual Style Pass (Техническое задание)

**Status:** COMPLETED (2026-04-24)  
**Date:** 2026-04-22  
**Phase position:** между завершённой Phase 2 и стартом Phase 3  
**Purpose:** быстро поднять визуальное качество игры к предзащите, не превращая задачу в полный art-production pipeline

---

## 1. Контекст

Phase 2 procedural arena pipeline завершён и verified:

- single-arena generator
- run graph
- encounter flow
- verticality / biomes / scaling

Проблема:

- игра уже функциональна, но визуально всё ещё слишком близка к "серому blockout-прототипу"
- к предзащите нужно, чтобы игра выглядела как **осознанно стилизованный playable prototype**, а не как техническая демка из серых кубов

Поэтому вводится отдельный промежуточный PR:

**PR 2.E — Visual Style Pass**

Это **не полная Phase 5**, а ограниченный visual pass поверх уже готовой procedural pipeline.

---

## 2. Главная цель

Сделать так, чтобы:

- арены выглядели заметно более стильными и атмосферными
- два биома визуально читались с первого взгляда
- procedural geometry сохранялась простой и дешёвой
- бой оставался читаемым
- реализация не сломала scope перед стартом Phase 3

Ключевой ориентир:

> Не "делаем финальный арт", а делаем **stylized graybox+**, который хорошо смотрится на предзащите.

---

## 3. Выбранный вариант

Выбран **Вариант B — оптимальный**:

- усиленные биомы
- архитектурные детали в builder
- floor patterns
- rule-based decorative props
- atmosphere / lighting accents
- использование найденных в интернете tileable textures, но только как части единой системы

Не входит в scope:

- полный environment art pass
- большое количество уникальных 3D-моделей
- ручная расстановка декора по каждой арене
- 5+ биомов
- heavy Blender workload

---

## 4. Арт-направление

## 4.1 Void Station

Визуальный образ:

- холодный sci-fi industrial
- тёмный металл
- серо-сине-графитовая база
- голубой / бирюзовый emissive
- техно-линии, warning stripes, панельная структура

Эмоция:

- стерильная, техногенная, опасная станция

## 4.2 Alien Nexus

Визуальный образ:

- тёмно-фиолетовая / бордово-красная база
- чужеродные светящиеся вставки
- более странный, менее индустриальный ритм
- ощущение "инопланетного узла" или corrupted structure

Эмоция:

- чужеродное, тревожное, нестабильное пространство

---

## 5. Основной принцип реализации

Visual pass строится из трёх слоёв:

## 5.1 Form

Архитектурные детали, генерируемые builder'ом:

- wall ribs
- ceiling beams
- door frames
- floor borders
- corner pillars
- edge trims

## 5.2 Material

Система материалов и текстур:

- tileable floor/wall/ceiling textures
- accent materials
- emissive materials
- biome-specific palette

## 5.3 Atmosphere

Общая визуальная подача:

- light accents
- emissive strips
- fog / atmospheric tint
- color grading
- distant background silhouettes / outside void treatment

---

## 6. Работа с интернет-текстурами

## 6.1 Да, использовать можно

Текстуры из интернета **разрешены и рекомендуются**, но только при соблюдении правил ниже.

## 6.2 Обязательные требования к текстурам

Использовать только:

- tileable / seamless textures
- материалы одной стилистики внутри одного биома
- желательно PBR-наборы:
  - BaseColor / Albedo
  - Normal
  - Metallic / Mask / ORM
  - Roughness / Smoothness
  - optional AO / Height

## 6.3 Чего нельзя делать

Нельзя:

- брать случайные несовместимые текстуры
- смешивать radically different art styles
- делать один и тот же объект с разной texel density
- просто "наклеить картинку" без общей material system

## 6.4 Как текстуры должны использоваться

Текстуры должны быть встроены в **material slots** биома:

- `floorPrimary`
- `floorAccent`
- `wallPrimary`
- `wallTrim`
- `ceilingPrimary`
- `coverMaterial`
- `propMaterial`
- `emissiveAccent`

Тогда builder использует не случайные материалы, а структурированную biome-driven систему.

---

## 7. Scope PR 2.E

## 7.1 Биомы сделать реально разными

Усилить уже существующую biome-систему:

- перейти от "разных цветов" к "разным визуальным наборам"
- расширить `BiomeDefinition`
- добавить material slots и visual tuning parameters

Нужно:

- 4-6 основных материалов на биом
- контрастные emissive accents
- согласованная палитра

## 7.2 Добавить архитектурные детали в builder

Нужно процедурно спавнить:

- рёбра на стенах
- потолочные балки
- рамки вокруг дверей
- бордюры по краям пола
- угловые стойки

Ограничение:

- всё должно быть дешёвым по реализации
- базовый вариант допускает использование обычных кубов

## 7.3 Сделать пол читаемым

Пол должен перестать быть визуально "одним серым листом".

Нужно добавить:

- floor panels / sections
- центральный рисунок или зону акцента
- полосы к дверям
- кольца / рамки / дорожки

Желательно различать floor pattern по типам арен:

- Combat
- Elite
- Parkour
- Shop / Rest
- Boss

## 7.4 Rule-based decorative props

Добавить простую систему структурированного декора:

- колонны по краям
- технические блоки в углах
- консоли у стен
- подвесные элементы под потолком
- пилоны у ключевых точек

Ключевой принцип:

- не хаотично
- не в боевом центре
- не ломать проходимость
- использовать зоны комнаты и правила placement

## 7.5 Atmosphere / ambiance

Добавить:

- fog tint по биому
- мягкий bloom
- color grading
- emissive strips / light markers
- визуальный фон за пределами арены

Цель:

- скрыть простоту geometry
- сделать арены более "собранными"

---

## 8. Технический план

## 8.1 Data layer

Расширить `BiomeDefinition`:

- material references
- optional texture-driven look presets
- emissive tuning
- atmosphere tint / fog color

Возможные новые поля:

- `floorPrimary`
- `floorAccent`
- `wallPrimary`
- `wallTrim`
- `ceilingPrimary`
- `coverMaterial`
- `propMaterial`
- `emissiveMaterial`
- `fogColor`
- `ambientTint`

## 8.2 Builder layer

Расширить `ArenaBuilder.BuildSingle(...)` новыми подслоями:

- `ArchitecturalDetails`
- `FloorPatterns`
- `Decor`
- `AtmosphereMarkers`

Желательно держать отдельные child-объекты:

- `Architecture`
- `FloorDetails`
- `Decor`
- `Atmosphere`

Это упростит debugging и последующий refactor.

## 8.3 Placement logic

Добавить rule-based зоны внутри комнаты:

- center
- perimeter
- corners
- near walls
- near exits
- safe combat core

Decor должен использовать именно эти зоны.

## 8.4 Atmosphere layer

Если возможно без лишнего риска:

- biome-specific fog / tint
- light strips / emissive pillars
- simple external silhouette objects outside arena shell

---

## 9. Предлагаемый PR split внутри задачи

Чтобы не делать всё одним большим рисковым коммитом, задачу логично разбить так:

## PR 2.E.1 — Materials + Biome Visual Slots

Scope:

- расширить `BiomeDefinition`
- завести material slots
- подключить текстуры/материалы для 2 биомов
- усилить контраст между Void Station и Alien Nexus

Acceptance:

- два биома визуально различимы с первого взгляда
- цвета и материалы выглядят согласованно

## PR 2.E.2 — Builder Architecture Pass

Scope:

- wall ribs
- door frames
- ceiling beams
- floor borders
- corner pillars

Acceptance:

- арены перестают выглядеть как "пустые коробки"
- детали не блокируют движение

## PR 2.E.3 — Floor Patterns + Rule-Based Decor

Scope:

- floor sections / panels / rings / stripes
- декор по краям / углам / стенам
- 2-3 prop group patterns

Acceptance:

- пол читается как спроектированная поверхность
- декор выглядит структурированно, а не хаотично

## PR 2.E.4 — Atmosphere Pass

Scope:

- fog / tint
- emissive accent lighting
- simple background silhouettes / outer void treatment
- post-processing tuning

Acceptance:

- сцена заметно атмосфернее
- biome mood читается без UI-подсказок

Если времени мало, можно остановиться после PR 2.E.2 или 2.E.3.

---

## 10. Acceptance criteria для всей задачи

PR 2.E считается успешным, если:

- `Void Station` и `Alien Nexus` визуально различаются с первого взгляда
- арены больше не выглядят как "просто серые кубы"
- текстуры не выглядят случайно налепленными
- бой остаётся читаемым
- декор не мешает movement flow
- procedural generation остаётся детерминированной
- новые визуальные слои не ломают encounter pipeline
- игра выглядит заметно лучше к предзащите

---

## 11. Что сознательно вне scope

Не входит в этот PR:

- финальный AAA-level environment art
- уникальные handcrafted room sets
- сложные анимированные декорации
- полноценные проп-паки на десятки моделей
- масштабный sound/UI polish
- full Phase 5 visual production

---

## 12. Практическая рекомендация по asset-подходу

Самый безопасный рабочий подход:

1. Сначала подобрать 2 небольших moodboard-набора текстур
   - Void Station
   - Alien Nexus

2. Из них собрать curated material library

3. Только после этого подключать builder-level decoration

4. Не тащить десятки ассетов сразу

5. Каждую новую visual layer проверять в Play Mode на 5-аренном run

---

## 13. Следующий шаг после утверждения этого TZ

После утверждения данного документа:

1. Собрать референсы и текстуры для 2 биомов
2. Начать с `PR 2.E.1`
3. Только потом переходить к `PR 2.E.2 / 2.E.3 / 2.E.4`
4. После завершения visual pass — стартовать Phase 3

---

## 14. Краткий итог

PR 2.E нужен для того, чтобы:

- не уходя в полный visual production
- быстро и контролируемо поднять качество картинки
- использовать найденные интернет-текстуры правильно
- объединить builder + materials + atmosphere в одну систему
- сделать игру визуально убедительнее к предзащите

Это **не замена Phase 5**, а **ограниченный style pass перед Phase 3**.
