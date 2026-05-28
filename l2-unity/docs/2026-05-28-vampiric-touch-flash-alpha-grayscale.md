# Vampiric Touch — SpriteEmitter0 alpha / grayscale заметка

**Дата:** 2026-05-28

Краткая заметка по `VampireFlash` (`SpriteEmitter0`, `fx_m_t0005`) для будущей настройки VFX.

## Симптом

- В Unity видна только яркая бело-голубая сердцевина частицы.
- Внутренняя синяя/фиолетовая "плазма" вокруг ядра почти исчезает.
- При сильном `RGB Boost` плазма начинает проявляться, но выглядит неправильно.
- В preview может казаться, что atlas cell выбран верно, но дизайн частицы беднее, чем в RenderDoc / оригинале.

## Причина

Для `fx_m_t0005` нельзя грубо превращать текстуру в grayscale/маску и затем резать alpha по яркости.

Оригинальная частица содержит цветовую структуру:

- яркое бело-голубое ядро;
- синий glow;
- слабая фиолетово-синяя внешняя плазма.

Если включить grayscale-логику или слишком жесткий `AlphaFromLuma`, слабая цветная плазма воспринимается как "почти черный фон" и вырезается вместе с фоном.

RenderDoc fixed-function fragment для оригинала по сути делает:

```hlsl
color = sample(texture, uv) * vertexColor;
```

То есть RGB из atlas должен сохраняться, а alpha/mask не должен уничтожать слабые цветные пиксели.

## Что делать в Unity

Для `SpriteEmitter0.mat` / `VampiricTouchFlash.shader`:

- `_AlphaFromLuma = 1`
- `_UseSoftLumaAlpha = 1`
- `_LumaAlphaPower = 0.55` примерно
- `_LumaAlphaFloor = 0` или очень маленькое значение
- `_IgnoreMainTexAlpha = 1`, если импортированная alpha текстуры режет плазму
- `_RgbBoost` не выкручивать как основной способ "починить" частицу; сначала починить alpha
- `_AlphaBoost` вернуть в нормальное значение для runtime, если в debug оно было занулено

В shader RGB не должен умножаться на alpha mask:

```hlsl
rgb = tex.rgb * tint.rgb * rgbBoost;
alpha = softMask * alphaBoost * tint.a * opacity * lifeAlpha;
```

Плохо:

```hlsl
rgb = tex.rgb * mask;
```

Такой вариант убивает слабую цветную плазму.

## Texture Import

Для `fx_m_t0005`:

- Не использовать grayscale-конвертацию как финальный источник цвета.
- `sRGBTexture` оставить включенным.
- Если alpha import режет цветной glow, проверять `IgnoreMainTexAlpha` в материале.
- Если мылит, сначала проверить `AtlasCellZoom`; сильный zoom кропает и растягивает atlas cell.

## UV / frame notes

Для текущей Unity-разметки хорошие синие частицы были найдены при:

- `TextureUSubdivisions = 2`
- `TextureVSubdivisions = 2`
- `SubdivisionStart = 2`
- `SubdivisionEnd = 2`
- `UseRandomSubdivision = 0`
- `AtlasCellZoom = 1` для проверки полного кадра

`sprite1.csv` из RenderDoc показывал UV `0.5..1 / 0..0.5`, но это мог быть draw не от нужного `VampireFlash`: он соответствовал области с хвостом/клином, а не синим motes.

