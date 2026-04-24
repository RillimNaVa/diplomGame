# СПЕЦИФИКАЦИЯ ТЕКСТУР ДЛЯ PR 2.E

Проект: `Void Survivor`  
Назначение: правила использования texture sets в material system procedural builder-а

---

## 0. Назначение документа

Этот файл задаёт короткие рабочие правила для набора текстур в `docs/textures`.

Подробная схема слотов и распределения по биомам описана в [biome_material_plan.md](C:/Users/assam/DiplomGame/docs/textures/biome_material_plan.md).

Этот файл отвечает на вопрос:

- какие texture sets считаются основными
- какому биому они принадлежат
- какие ограничения на их использование

---

## 1. Общие правила

### 1.1 Минимальный технический стандарт

Использовать по возможности:

- `BaseColor`
- `Normal`
- `Roughness`
- `Metallic` или ручную metallic/smoothness настройку
- `AO`
- `Height` опционально

Запрещено:

- использовать только `BaseColor` без нормали
- тянуть preview-render вместо реальных texture maps

### 1.2 Unity URP mapping

- `Albedo` <- `BaseColor`
- `Normal` <- `Normal`
- `Metallic` <- `Metallic`, если карта есть
- `Smoothness` <- `1 - Roughness`
- `Occlusion` <- `AO`

### 1.3 Важная оговорка по workflow

Набор не полностью единый:

- sci-fi материалы ближе к standard metallic workflow
- alien материалы частично используют `SPEC` вместо полного metallic набора

Следствие:

- `Void Station` можно собирать как чистый metallic sci-fi kit
- `Alien Nexus` должен быть гибридным биомом: sci-fi база + alien contamination

### 1.4 Texel density

Цель:

- одинаково воспринимаемый масштаб материалов на похожих поверхностях

Запрещено:

- один и тот же класс поверхности делать визуально в разных масштабах без причины

---

## 2. Одобренные texture sets

Рабочие наборы:

- `Sci_fi_Metal_Panel_007_SD`
- `Sci_fi_Metal_Panel_009_SD`
- `Sci-fi_Walll_001_SD`
- `Sci-fi_Metal_Walkway_001_SD`
- `Alien_Metal_002_SD`
- `Alien_Muscle_001_SD`
- `Alien_Flesh_001`

Не использовать как runtime textures:

- `Material_01.png`
- `material_1901.png`
- `Material_1019.png`
- `Material_1930a.png`
- `Alien_Flesh_001_render.jpg`

---

## 3. Void Station

### Sci-fi Metal Panel 007

Тип:

- основной структурный материал

Использование:

- `floorPrimary`
- `coverMaterial`
- часть `propMaterial`

Правило:

- это основа biome-а, а не акцент

### Sci-fi Metal Panel 009

Тип:

- accent / trim

Использование:

- `floorAccent`
- `wallTrim`
- `propMaterial`

Ограничение:

- не более `25-30%` видимой площади как акцентный слой

### Sci-fi Metal Walkway

Тип:

- направляющий и emissive-материал

Использование:

- `emissiveAccent`
- дорожки к exit-зонам
- направляющие полосы

Важно:

- направленный рисунок использовать осознанно

### Sci-fi Wall

Тип:

- крупная спокойная фоновая поверхность

Использование:

- `wallPrimary`
- `ceilingPrimary`

---

## 4. Alien Nexus

## 4.1 Главный принцип

`Alien Nexus` не строится как полностью мясной биом.

Он строится как:

- заражённая sci-fi станция
- структура от `Void Station`
- alien-вкрапления как вторичный слой

## 4.2 Alien Metal

Тип:

- infected accent metal

Использование:

- `floorAccent`
- `propMaterial`
- локальные заражённые сектора

Правило:

- можно использовать и как `floorPrimary` в отдельных зонах, но не как единственный материал всего биома

## 4.3 Alien Muscle

Тип:

- contamination trim / overlay

Использование:

- `wallTrim`
- seams
- perimeter overlays
- near-exit corruption details

Запрещено:

- большие сплошные поверхности

## 4.4 Alien Flesh

Тип:

- focal infection material

Использование:

- локальные wall patches
- ceiling growths
- boss/elite contamination emphasis

Запрещено:

- сплошной пол
- полный wallPrimary всей арены

## 4.5 Sci-fi base inside Alien Nexus

Для `Alien Nexus` разрешено и рекомендуется использовать:

- `Sci_fi_Metal_Panel_007_SD`
- `Sci_fi_Metal_Panel_009_SD`
- `Sci-fi_Walll_001_SD`

Но:

- с более тёмным фиолетово-бордовым тоном
- с alien contamination поверх структуры

---

## 5. Слоты

Базовые biome slots:

- `floorPrimary`
- `floorAccent`
- `wallPrimary`
- `wallTrim`
- `ceilingPrimary`
- `coverMaterial`
- `propMaterial`
- `emissiveAccent`

---

## 6. Логика распределения

- `center` -> максимально чисто и читаемо
- `perimeter` -> детали, trims, contamination
- `corners` -> props и заражённые акценты
- `near exits` -> emissive guidance и визуальный фокус

Главное правило:

- бой важнее декоративной плотности

---

## 7. Запреты

Запрещено:

- хаотично смешивать стили
- использовать разные масштабы без системы
- покрывать `Alien Nexus` органикой равномерно по всей комнате
- делать `Alien Flesh` универсальной базой
- смешивать голубой emissive `Void Station` и фиолетовый emissive `Alien Nexus` внутри одного биома

---

## 8. Emissive language

- `Void Station` -> cyan / blue
- `Alien Nexus` -> violet / magenta

---

## 9. Визуальная цель

- `Void Station` -> структура, порядок, инженерный ритм
- `Alien Nexus` -> та же структура, но захваченная и заражённая alien-слоем
