# Поиск StaticMesh по RenderDoc (PSKX + скрипт)

Когда `.uc` эффекта указывает один mesh (`black_berserker00/01`), а в RenderDoc draw call другой (другое число треугольников, другая форма), имя mesh нужно восстанавливать **по фактам из захвата**, а не по декомпилированному конфигу.

Для Vampiric Touch это сработало так:

| Источник | Triangles | Результат |
|----------|-----------|-----------|
| RenderDoc draw call | **36** (108 indices) | эталон |
| `black_berserker01.pskx` | 28 | не совпадает |
| `black_berserker00` (FBX / отсутствующий pskx) | 60 | не совпадает |
| **`black_vampire01.pskx`, material slot 0** | **36**, UV **12/12** | **нужный mesh** |

Полное имя в клиенте: `StaticMesh'LineageEffectsStaticmeshes.Black.black_vampire01'` (в Unity — submesh / faces только **material 0**).

---

## Что нужно заранее

1. **RenderDoc** — захват кадра с нужным эффектом.
2. **Umodel** — распакованный пакет `LineageEffectsStaticmeshes` в `.pskx` (папка `StaticMesh`).
3. **Python 3.10+** (`py -3` на Windows).
4. Скрипт: [`tools/scan_pskx_36tris.py`](../tools/scan_pskx_36tris.py)

---

## Шаг 1 — Данные из RenderDoc

### Triangle / index count

В Pipeline или Mesh viewer смотри draw call эффекта:

- **Index count = 108** → **36 triangles** (108 ÷ 3).
- Запиши это число — по нему можно фильтровать кандидатов (`--expected-tris 36`).

Не путать с числом строк в CSV (см. ниже).

### Экспорт CSV (VS output)

1. Выбери **тот же** draw call (mesh pass с текстурой эффекта, например `fx_m_t0032`).
2. Открой **Vertex Output** / экспорт вершин после VS.
3. Сохрани CSV с колонками минимум:
   - `VTX`, `IDX`
   - `out_Texcoord0.x`, `out_Texcoord0.y`
4. Положи файл, например: `decompile/vampiric_touch_mesh.csv`

Важно:

- Это **UV после vertex shader**, не «сырой» mesh из umodel один в один, но **набор UV-углов** совпадает с развёрткой StaticMesh — по ним и ищем файл.
- Строк в CSV обычно `triangles × 3` (108 строк ≈ 36 tris).

### Неверный CSV (частая ошибка)

Файл `decompile/sprite1.csv` в репозитории **не подходит** для скрипта, если в нём только:

```text
VTX, IDX, STATUS
---, ---, No geometry and no tessellation shader bound.
```

Это **не** mesh-экспорт Vertex Output. Скрипт ищет колонки **`out_Texcoord0.x`** и **`out_Texcoord0.y`** (пробел в имени колонки допустим, например ` out_Texcoord0.x`).

Нужно:

1. В RenderDoc выбрать **тот же draw call**, где в Mesh/Pipeline **108 indices** (36 triangles).
2. Открыть **Vertex Output** этого draw call (не пустой pass без geometry).
3. Экспортировать CSV с `out_Texcoord0.x` / `out_Texcoord0.y` и сохранить, например, как `decompile/vampiric_touch_mesh.csv`.
4. В команде указать путь к **этому** файлу: `--csv "...\vampiric_touch_mesh.csv"`.

После корректного CSV та же команда из шага 3 должна снова показать:

```text
black_vampire01.pskx
  mat 0: 36 tris, 12/12 UV hits  ← material slot matches expected tris
```

Если скрипт падает с ошибкой про отсутствие колонки `Texcoord0` — CSV переснят с не того draw call или экспорт обрезан.

---

## Шаг 2 — Распаковка mesh (umodel)

Экспорт StaticMesh в **psk/pskx** в одну папку, например:

```text
.../LineageEffectsStaticmeshes/StaticMesh/
  black_vampire01.pskx
  black_berserker01.pskx
  ...
```

Проверь, что нужные имена реально есть: у `black_berserker00` в одной из распаковок был только `.props.txt` без `.pskx` — такой mesh скрипт не увидит.

---

## Шаг 3 — Запуск скрипта

Из корня репозитория `l2-unity`:

```powershell
py -3 tools/scan_pskx_36tris.py `
  --csv "c:\unity\l2 client\decompile\vampiric_touch_mesh.csv" `
  --pskx-dir "C:\Users\hh-soft\Pictures\test_umodel\unpack\LineageEffectsStaticmeshes\LineageEffectsStaticmeshes\StaticMesh" `
  --uc-root "C:\Users\hh-soft\Pictures\test_umodel\unreal script\UnrealScript\UnrealScript" `
  --expected-tris 36 `
  --min-hits 8
```

Параметры:

| Параметр | Назначение |
|----------|------------|
| `--csv` | CSV из RenderDoc (обязательно) |
| `--pskx-dir` | папка с `*.pskx` (обязательно) |
| `--uc-root` | опционально: где искать `.uc`, которые ссылаются на найденное имя |
| `--expected-tris` | подсветить mesh / material slot с этим числом треугольников |
| `--min-hits` | минимум совпавших UV-углов (по умолчанию 8 из ~12 unique UV) |

### Как читать вывод

Пример (Vampiric Touch):

```text
RenderDoc CSV: sprite1.csv
  VS rows: 108  →  ~36 triangles
  Unique UV corners: 12

Matches (>=8/12 UV hits):
  12/12 UV  tris= 104  verts= 103  black_vampire01.pskx
      mat 0: 36 tris, 12/12 UV hits  ← material slot matches expected tris
      mat 1: 16 tris, ...
      mat 2: 52 tris, ...

Best candidate:
  File: black_vampire01.pskx
  UE path: LineageEffectsStaticmeshes.*.black_vampire01
  Use material slot 0 (36 tris) if full mesh has more triangles.
```

Интерпретация:

1. **`12/12 UV`** — полное совпадение «отпечатка» UV из RenderDoc.
2. **`tris=104`** — в файле несколько material groups; draw call мог рисовать **один slot**.
3. Строка **`mat 0: 36 tris, 12/12 UV`** — это и есть искомый submesh.

Если совпадений нет — снизь `--min-hits` до 4–6 или пересними CSV с другого draw call.

---

## Шаг 4 — Blender / Unity

1. Импорт `black_vampire01.pskx` (umodel → Blender).
2. В mesh несколько **material slots** — для эффекта нужен **slot 0** (36 faces).
3. Экспорт FBX только с faces material 0 **или** в Unity два submesh / два материала, активный — первый.
4. Текстура эффекта остаётся из шейдера (`fx_m_t0032_A` и т.д.) — меняется геометрия, не атлас.

Проверка в Blender: Statistics → **Triangles = 36** на выделенных faces material 0.

---

## Логика скрипта (кратко)

1. Читает уникальные пары `(U, V)` из CSV RenderDoc.
2. Для каждого `.pskx` (формат ActorX / umodel):
   - читает wedges (`VTXW0000`) и faces (`FACE0000`);
   - считает совпадения UV с допуском `0.025`;
   - дополнительно разбивает faces по **material index** (часто один draw = один slot).
3. Сортирует по числу совпадений UV; опционально помечает slot с `--expected-tris`.

Фильтр «только mesh с 36 tris в файле целиком» **недостаточен**: правильный asset может иметь 104 tri, а в RenderDoc — только 36 (один material).

---

## Типичные ошибки

| Ошибка | Что делать |
|--------|------------|
| Смотреть только `.uc` | UC может быть старый/неверный; опираться на RenderDoc + PSKX |
| Искать файл ровно с 36 tris | Смотреть **material slots** внутри `black_vampire01` и т.п. |
| Сравнивать post-VS позиции с raw mesh | Сравнивать **UV + triangle count**, не world position из CSV |
| Нет `.pskx` в папке | Переэкспорт из umodel (как с `black_berserker00`) |
| CSV только `VTX, IDX, STATUS` / «No geometry» | Переснять **Vertex Output** draw call с 108 indices; нужны `out_Texcoord0.x/y` |
| Скрипт не находит mesh | Проверить CSV (см. выше), путь `--pskx-dir`, снизить `--min-hits` |

---

## Связанные файлы

| Файл | Роль |
|------|------|
| [`tools/scan_pskx_36tris.py`](../tools/scan_pskx_36tris.py) | скрипт поиска |
| `Assets/Resources/Data/Effects/vampiric/touch/` | Vampiric Touch prefab / shaders |
| `docs/effects-composite-prefab-mechanic.md` | как собирается composite-эффект |

---

## Кейс: Vampiric Touch (skill 1147 / m_u041_vampiric)

- RenderDoc: **36 tris**, 12 unique UV.
- Скрипт: **`black_vampire01.pskx`**, **material 0** = 36 tris, UV 12/12.
- В `m_u041_vampiric.uc` указаны `black_berserker00/01` — для этого клиента **не использовать** как источник mesh без проверки.

После замены mesh в Unity имеет смысл снова сравнить кадр с оригиналом (форма «короны», не широкие крылья `black_berserker01`).
