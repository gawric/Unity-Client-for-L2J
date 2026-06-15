# Lineage 2 UE3 .uc -> Unity URP material mapping guide for AI

This project ports Lineage 2 Interlude particle effects from UE3 UnrealScript `.uc` emitters to Unity URP materials using custom shaders in:

`Assets/Resources/Data/Shaders/Skills/`

Finished effect examples with `.uc` + `.mat` files are in:

`Assets/Resources/Data/Effects/`

Generated from a scan of the current project:

- `.uc` files found: 18
- `.mat` files found: 95
- custom skill shaders found: 18
- direct emitter-name `.uc` -> `.mat` pairs found: 76

Important project rule: C# scripts usually handle particle instances, physics, movement and positioning. Shaders are primarily visual renderers: texture, color, alpha, atlas, size scale, spin, optional shader-side spawn/motion. Do not blindly enable shader-side motion if C# already moves particles.

---

## 1. Matching `.uc` emitters to materials

In `.uc`, each emitter is declared like:

```uc
Begin Object Class=SpriteEmitter Name=SpriteEmitter34
    ... fields ...
End Object
Emitters(1)=SpriteEmitter'SpriteEmitter34'
```

Unity material names normally match the UE emitter name:

- `SpriteEmitter34` -> `SpriteEmitter34.mat`
- `MeshEmitter12` -> `MeshEmitter12.mat`
- Duplicates may use suffixes: `SpriteEmitter8_Left.mat`, `SpriteEmitter8_Right.mat` -> use `SpriteEmitter8` from `.uc`
- Clone materials may use suffixes: `MeshEmitter34_1.mat`, `MeshEmitter34_2.mat` -> use `MeshEmitter34` if exact emitter is absent

When porting a new effect, first parse every `Begin Object` block, then find a material with the same name.

---

## 2. Coordinate and unit conventions

UE3 coordinate axes in `.uc`:

- X = right
- Y = forward
- Z = up

Unity convention used by this project:

- X = right
- Y = up
- Z = forward

Axis conversion:

```text
Unity.x = UE.X
Unity.y = UE.Z
Unity.z = UE.Y
```

Unit conversion:

```text
1 Unreal Unit ~= 0.01 Unity meter
Unity meters = UE value * 0.01
```

Use this conversion for spawn offsets, location ranges and velocities when the material property expects Unity-space values. Some newer shaders expose raw-UE properties plus toggles such as `_ApplySpawnUnitScale`, `_SpawnUnitScale`, `_ApplyUuToStartSize`; in those shaders raw UU may be passed and converted in shader.

Important exception: quad meshes are already authored in Unity scale. Do not scale mesh vertex positions again just because `.uc StartSizeRange` is in UU. `StartSizeRange` may still be useful for shader billboard/particle size properties, but verify the shader path.

---

## 3. High-confidence mappings observed in real materials

These fields were exact or near-exact in finished effects.

### Timing

```text
.uc FadeIn=True/False              -> material _FadeIn = 1/0
.uc FadeInEndTime=0.05             -> material _FadeInEndTime = 0.05
.uc FadeOut=True/False             -> material _Fadeout = 1/0
.uc FadeOutStartTime=0.2           -> material _FadeoutStartTime = 0.2
.uc LifetimeRange=(Min=a,Max=b)    -> material _LifetimeRange = (a,b,0,0)
.uc InitialDelayRange=(Min=a,Max=b)-> material _InitialDelayRange = (a,b,0,0)
```

Scan result:

- `FadeOutStartTime -> _FadeoutStartTime`: 51 exact matches
- `FadeInEndTime -> _FadeInEndTime`: 29 exact matches
- `LifetimeRange -> _LifetimeRange`: median ratio 1.0 in matched materials
- `InitialDelayRange -> _InitialDelayRange`: median ratio 1.0

If the material has `_HasLifetime`, set it to 1 when `.uc` has `LifetimeRange`.

### Atlas / flipbook

```text
.uc TextureUSubdivisions=N         -> _TextureUSubdivisions = N
.uc TextureVSubdivisions=N         -> _TextureVSubdivisions = N
.uc SubdivisionStart=N             -> _SubdivisionStart = N
.uc SubdivisionEnd=N               -> _SubdivisionEnd = N
.uc UseRandomSubdivision=True      -> _UseRandomSubdivision = 1
.uc BlendBetweenSubdivisions=True  -> _BlendBetweenSubdivisions = 1
```

Scan result:

- `TextureUSubdivisions -> _TextureUSubdivisions`: 28 exact matches
- `TextureVSubdivisions -> _TextureVSubdivisions`: 28 exact matches

Note: `SubdivisionStart/End` are sometimes manually tuned when texture atlas layout differs or the shader intentionally crops/chooses a different visual cell. Start with exact `.uc` values, then verify visually.

### Alpha / opacity

Common shader property names differ:

```text
.uc Opacity=x -> _Opacity=x if shader has _Opacity
.uc Opacity=x -> _Alpha=x if shader has _Alpha and no _Opacity
```

Observed materials often tune alpha manually. Median ratio for `Opacity -> _Opacity` among non-exact cases was about 1.67. So copy `.uc Opacity` as first approximation, then allow manual brightness/alpha tuning.

Bright additive effects often need:

```text
_RgbBoost / _AlphaBoost / _Brighten / _AlphaFromLuma / _LumaAlphaFloor
```

These are shader-specific artistic controls, not direct `.uc` fields.

### ColorScale

UE3 color uses 0..255 channel values and B,G,R,A order in text:

```uc
ColorScale(0)=(Color=(B=255,G=128,R=64,A=255))
ColorScale(1)=(RelativeTime=1.0,Color=(B=0,G=0,R=255,A=255))
```

Convert to Unity color:

```text
Unity.r = R / 255
Unity.g = G / 255
Unity.b = B / 255
Unity.a = A / 255
```

Possible material property layouts:

Old/common layout:

```text
_ColorScale0Color = color0
_ColorScale0Time  = time0, default 0
_ColorScale1Color = color1
_ColorScale1Time  = time1
...
```

Newer shaders:

```text
_ColorScale0 = color0
_ColorScaleTime1 = time1
_ColorScale1 = color1
_ColorScaleTime2 = time2
_ColorScale2 = color2
_ColorScaleCount = number of keys
_ColorScaleRepeats = .uc ColorScaleRepeats if present
```

Always write only properties that exist on the selected shader/material.

### ColorMultiplierRange

```uc
ColorMultiplierRange=(X=(Min=r0,Max=r1),Y=(Min=g0,Max=g1),Z=(Min=b0,Max=b1))
```

Common mappings:

```text
_ColorMultMin = (r0,g0,b0,1)
_ColorMultMax = (r1,g1,b1,1)
```

Older material layout:

```text
_ColorMultiplierRangeR = (r0,r1,0,0)
_ColorMultiplierRangeG = (g0,g1,0,0)
_ColorMultiplierRangeB = (b0,b1,0,0)
```

If Max is omitted in `.uc`, treat Max = Min or the UE default depending on context. In the examples, missing max often means 0 or same value; verify visually.

### Size

```uc
StartSizeRange=(X=(Min=a,Max=b),Y=(Min=c,Max=d),Z=(Min=e,Max=f))
```

Common mappings:

```text
_SizeRange  = (X.Min, X.Max, 0, 0)       // newer simple shaders
_SizeRangeX = (X.Min, X.Max, 0, 0)       // older axis layout
_SizeRangeY = (Y.Min, Y.Max, 0, 0)
_SizeRangeZ = (Z.Min, Z.Max, 0, 0)
```

For sprite emitters with `UniformSize=True`, X is usually enough.

Do not automatically multiply StartSize by 0.01 unless the shader expects Unity meters. Several shaders have a toggle:

```text
_ApplyUuToStartSize = 0/1
```

Project preference: keep backward-compatible defaults. If a shader already uses authored Unity quad bounds, do not force raw UE size scaling.

### SizeScale

```uc
SizeScale(0)=(RelativeTime=0.07,RelativeSize=1.2)
SizeScale(1)=(RelativeTime=0.11,RelativeSize=1.1)
SizeScale(2)=(RelativeTime=1.0,RelativeSize=1.2)
```

Mapping:

```text
_UseSizeScale = 1 if .uc has UseSizeScale=True or SizeScale keys exist
_SizeScale0 = (RelativeTime, RelativeSize, 0, 0)
_SizeScale1 = (RelativeTime, RelativeSize, 0, 0)
...
_SizeScaleRepeats = .uc SizeScaleRepeats if present
```

If `RelativeTime` is omitted in `SizeScale(0)`, use 0.0. If `RelativeSize` is omitted, use 1.0.

### Spin

```uc
SpinParticles=True -> _SpinParticles = 1
SpinsPerSecondRange=(X=(Min=a,Max=b),Y=(Min=c,Max=d),Z=(Min=e,Max=f))
StartSpinRange=(X=(Min=a,Max=b),Y=(...),Z=(...))
```

Common mappings:

```text
_SpinsPerSecondRange = (X.Min, X.Max, 0, 0)      // simple shaders
_SpinsPerSecondRangeX = (X.Min, X.Max, 0, 0)
_SpinsPerSecondRangeY = (Y.Min, Y.Max, 0, 0)
_SpinsPerSecondRangeZ = (Z.Min, Z.Max, 0, 0)
_StartSpinRange = (X.Min, X.Max, 0, 0)
_StartSpinRangeX/Y/Z similarly
```

Because C# may drive per-particle rotation, spin may be shader-specific. If the material is invisible or rotates wrongly, disable shader spin first and verify texture/alpha.

### StartLocationOffset

```uc
StartLocationOffset=(X=a,Y=b,Z=c)
```

Unity-space vector mapping:

```text
_StartLocationOffset = (a*0.01, c*0.01, b*0.01, 0)
```

Raw-UE shader mapping for newer shaders:

```text
_StartLocationOffset = (a,b,c,0)
_ApplySpawnUnitScale = 1
_SpawnUnitScale = 0.01
```

Check the shader property label. If it says `(UU)`, pass raw UE and enable scale. If old material values are tiny like 0.03 for UE 3.0, it expects Unity meters.

### StartLocationRange

```uc
StartLocationRange=(X=(Min=a,Max=b),Y=(Min=c,Max=d),Z=(Min=e,Max=f))
```

Old axis layout expects Unity-space after axis conversion:

```text
_StartLocationRangeX = (X.Min*0.01, X.Max*0.01, 0, 0)
_StartLocationRangeY = (Z.Min*0.01, Z.Max*0.01, 0, 0)   // Unity Y = UE Z
_StartLocationRangeZ = (Y.Min*0.01, Y.Max*0.01, 0, 0)   // Unity Z = UE Y
```

Newer VampiricTouchFlash-style layout may use raw UE:

```text
_UseStartLocationRange = 1
_StartLocationRangeXY = (X.Min, X.Max, Y.Min, Y.Max)
_StartLocationRangeZ = (Z.Min, Z.Max, 0, 0)
```

### StartLocationPolarRange

```uc
StartLocationPolarRange=(X=(Min=az0,Max=az1),Y=(Min=pitch0,Max=pitch1),Z=(Min=r0,Max=r1))
```

Common mappings:

```text
_StartLocationPolarRangeX = (az0, az1, 0, 0)
_StartLocationPolarRangeY = (pitch0, pitch1, 0, 0)
_StartLocationPolarRangeZ = (r0, r1, 0, 0)
```

Newer layout:

```text
_PolarAzimuthDeg = (az0, az1, 0, 0)
_PolarPitchDeg   = (pitch0, pitch1, 0, 0)
_PolarRadius     = (r0, r1, 0, 0)
```

In UE3 particle configs, `Y=85..95` often means almost horizontal ring because pitch is measured from +Z. For `PolarPitchDeg = 90`, spawn offset is a horizontal ring; pair with horizontal velocity pattern in section 9.

### StartVelocityRange

```uc
StartVelocityRange=(X=(Min=a,Max=b),Y=(Min=c,Max=d),Z=(Min=e,Max=f))
```

Old material layout usually expects Unity meters/sec after axis conversion:

```text
_StartVelocityRangeX = (X.Min*0.01, X.Max*0.01, 0, 0)
_StartVelocityRangeY = (Z.Min*0.01, Z.Max*0.01, 0, 0)   // Unity Y = UE Z
_StartVelocityRangeZ = (Y.Min*0.01, Y.Max*0.01, 0, 0)   // Unity Z = UE Y
```

Newer shader properties may expect raw UE:

```text
_StartVelocityRange = (X.Min, X.Max, Y.Min, Y.Max)
_RadialSpeed = derived speed range when GetVelocityDirectionFrom is radial/sphere
```

If `.uc` has `GetVelocityDirectionFrom=PTVD_OwnerAndStartPosition` with polar spawn, direction is **not** a raw velocity axis — see section 9. Often set speed on `_VelocityRangeX` only and derive direction from `spawnOfs.xz`.

Scan result for old materials showed velocity ratios around 0.01 to 0.02 for many cases, confirming UU-to-meter conversion plus some manual tuning.

### VelocityLossRange

```uc
VelocityLossRange=(X=(Min=a,Max=b),Y=(...),Z=(...))
```

Newer shaders:

```text
_UseVelocityLoss = 1
_VelocityLossRange = (a,b,0,0)
```

Older layouts may have `_VelocityLossRangeX/Y/Z`.

---

## 4. Shader choice rules

Before converting values, inspect the target material's shader and only set properties that exist.

The scanned shader folder contains these skill shaders:

- `L2/Effects/VampiricTouchFlash` — most feature-complete sprite shader; supports lifetime, delay, fade, opacity, color scale, color multiplier, polar spawn, sphere radius, start location range, owner velocity, velocity loss, atlas, debug preview.
- `L2/Effects/PoisonKirakira` — good for poison spark/sphere effects, supports repeats/loss style features.
- `L2/Effects/VampiricTouchSpark` — `fx_m_t0000` dust/spark tail; polar spawn, horizontal radial velocity from `spawnOfs.xz`, texture dilate/RGB/alpha boost. Reference for `PTVD_OwnerAndStartPosition` style rings.
- `L2/Effects/ShieldTaSprite` — Shield TA sprites (`AngelDust`, flash); uses horizontal radial motion pattern from `VampiricTouchSpark` for polar skirt effects.
- `L2/Effects/MagicCircleAlphaBlend` / `MagicCircleBrighten` — circle/ground/mesh style shaders.
- `L2/Effects/L2IceFrag`, `L2IceApprox`, steam shaders — ice/steam-specific visuals.

If a `.uc` uses a feature the shader does not expose, do not invent a material property. Either choose a more capable shader or add a backward-compatible `[Toggle]` feature defaulted off.

---

## 5. Common UE3 fields and what to do with them

Fields often present but not always material properties:

```text
MaxParticles                  -> C# particle system / prefab, not material
InitialParticlesPerSecond     -> C# spawning, not material
AutomaticInitialSpawning      -> C# spawning, not material
RespawnDeadParticles          -> C# spawning/lifetime, not material
CoordinateSystem              -> C# transform / shader mode depending effect
DrawStyle                     -> shader blend mode choice, not usually a property
Texture                       -> assign _MainTex manually or via asset lookup; script does not auto-resolve package names
StaticMesh                    -> prefab/mesh renderer asset, not material scalar
UseMeshBlendMode              -> shader/blend selection
RenderTwoSided                -> shader Cull Off or material/render state
Name="..."                   -> human label only
```

AI should not try to put these into random material floats unless the shader has a clearly named property.

---

## 6. Recommended AI workflow for porting one new effect

1. Parse the `.uc` file and list all `Begin Object` emitters in order.
2. For each emitter, decide emitter type: `SpriteEmitter`, `MeshEmitter`, `BeamEmitter`.
3. Create/find a Unity material named exactly like the emitter.
4. Choose the closest existing shader by visual family and required properties.
5. Copy high-confidence values: fade, lifetime, delay, atlas, color scale, size scale, spin.
6. Convert spatial values carefully:
   - axis: `(X,Y,Z)_UE -> (X,Z,Y)_Unity`
   - units: multiply by 0.01 only for Unity-space properties
7. Do not enable shader-side velocity/motion if C# already handles it.
8. Test in Play mode. Unity editor `_Time.y` can make shader-side lifetime/motion appear expired or displaced in Scene view.
9. Tune only artistic controls after the mechanical mapping: opacity, alpha boost, RGB boost, luma alpha, atlas crop/zoom.

---

## 7. Unity editor automation

A conservative editor script was added at:

`Assets/Editor/L2UcMaterialAutoMapper.cs`

Usage:

1. In Unity Project window select one `.uc` file.
2. Select one or more `.mat` files from the same effect folder.
3. Run menu:

```text
Tools/L2 Effects/Apply selected UC to selected Materials
```

The script:

- matches materials to `.uc` emitters by name
- supports `_Left`, `_Right`, `_1`, `_2` suffix fallbacks
- writes only properties that exist on the material shader
- supports Undo
- saves assets
- logs matched materials and property write counts to Console

The script intentionally does not:

- assign textures from `Texture'Package.Name'`
- change shader assignment
- change prefabs, meshes, particle counts, spawn rates or C# settings
- overwrite unsupported shader features

This makes it safe as a first-pass mapper, not a final visual replacement for manual review.

---

## 8. Critical pitfalls

- `_Time.y` in Unity editor is editor uptime, not effect start time. Shader-side lifetime/motion can make particles invisible in Scene view. Test in Play mode or set `_StartTime`/disable shader motion.
- If C# handles movement, disable shader-side radial velocity and large spawn offsets unless the shader was designed for that effect.
- If using authored Unity quad bounds, do not apply raw `.uc StartSizeRange` as another huge scale.
- `Opacity` and additive brightness rarely match perfectly. Copy as first pass, then tune.
- `SubdivisionStart/End` can be manually changed in finished materials when the atlas differs or when only certain cells look good.
- UE `ColorScale` text writes channels as B,G,R,A, but Unity Color is R,G,B,A.
- **`_OwnerWorldPos` is not always "player feet in world space".** `ParticleGroup` may write `PlayerEntity.transform.position` or `OwnerTarget.position`. For radial skirt/ring effects on the character, that world Y often differs from the emitter pivot. Do not use `normalize(spawnWS - _OwnerWorldPos)` unless you have verified both points are in the same semantic frame. See section 9.
- **Fallback direction vectors must match the effect plane.** A fallback like `float3(0, 1, 0)` forces upward motion when `length(spawnWS - owner)` is near zero (common at small `PolarRadius`). Use horizontal fallback from polar azimuth / `spawnOfs.xz`, not world up.
- **Keep motion in one coordinate space.** If velocity is integrated as `spawnOfs += disp` in object/local space, direction must also be object-space horizontal (`spawnOfs.xz`). Do not mix world-space `dirWS` with local `spawnOfs += disp`.

---

## 9. Sprite radial motion, `GetVelocityDirectionFrom`, and `_OwnerWorldPos`

This section documents a real production bug from Shield TA `AngelDust` (`m_u008_b` `SpriteEmitter6`, material `SpriteEmitter6.mat`, shader `ShieldTaSprite`). The same particle family already worked in `VampiricTouchSpark` (`m_u003_b` `SpriteEmitter2`, `fx_m_t0000`, subdiv 14..16).

### What UE3 actually means

```uc
GetVelocityDirectionFrom=PTVD_OwnerAndStartPosition
StartLocationPolarRange=(X=(Max=360),Y=(Min=90,Max=90),Z=(Min=15,Max=15))
StartVelocityRange=(X=(Min=25,Max=35),Y=(Min=25,Max=35),Z=(Min=25,Max=35))
```

In UE3 the **owner and emitter live on the same actor**. Direction is:

```text
dir = normalize(startPosition - ownerPosition)
```

Both points are in **emitter-local / actor space**. Owner is the effect pivot, not the global world feet of the character mesh.

Unity port mistake:

```hlsl
// WRONG for compact horizontal skirt on character-attached effect
float3 spawnWS = TransformObjectToWorld(spawnOfs);
float3 dirWS = normalize(spawnWS - _OwnerWorldPos.xyz);
float3 velWS = dirWS * speed;
```

Why it fails:

1. `spawnWS` is on the polar ring around the **effect pivot** (often waist / cast point, higher Y).
2. `_OwnerWorldPos` from `ParticleGroup` is often `PlayerEntity.transform.position` (feet / capsule root, lower Y).
3. `spawnWS - owner` gets a large **vertical** component -> particles fly up.
4. At small `PolarRadius` (15 UU = 0.15 m) the vector length can be ~0 -> fallback `(0,1,0)` makes it worse.

Increasing `PolarRadius` to 50..80 only hides the math bug by making the horizontal part dominate.

### Correct pattern (horizontal radial skirt)

Use the same approach as `VampiricTouchSpark.shader`:

```hlsl
float3 spawnOfs = L2Fx_UeVectorToUnity(posUe) * _SpawnUnitScale * motionComp;

float speed = length(float3(
    L2Fx_RandomRange(_VelocityRangeX.xy, seed, startTime, 101.0),
    L2Fx_RandomRange(_VelocityRangeY.xy, seed, startTime, 103.0),
    L2Fx_RandomRange(_VelocityRangeZ.xy, seed, startTime, 107.0)));

float2 hDir = L2Fx_OutwardDirectionXZ(
    spawnOfs, _PolarAzimuthDeg.xy, seed, startTime, 181.0);
float3 vel = float3(hDir.x, 0.0, hDir.y) * speed * _SpawnUnitScale * motionComp;

float loss = L2Fx_RandomRange(_VelocityLossRange.xy, seed, startTime, 109.0) * _SpawnUnitScale * motionComp;
spawnOfs += L2Fx_DisplacementLinearHorizontalVelocityLoss(vel, float3(0,0,0), loss, age);
float3 centerWS = TransformObjectToWorld(spawnOfs);
```

Key rules:

| Rule | Reason |
|------|--------|
| Direction from `spawnOfs.xz` / `L2Fx_OutwardDirectionXZ` | Matches UE owner=start actor pivot; always horizontal ring |
| `vel.y = 0` | Skirt/cloud stays in XZ plane |
| Integrate in **object/local** space, then `TransformObjectToWorld` | Effect follows character rotation correctly |
| Use `L2Fx_DisplacementLinearHorizontalVelocityLoss` | Particles slow and stop in the ring instead of flying forever |

### Velocity range mapping for radial owner-start effects

When direction comes from spawn offset, UE velocity range mainly supplies **speed magnitude**, not a world-axis vector.

Practical material setup:

```text
_VelocityRangeX = (25, 35, 0, 0)   // copy .uc StartVelocityRange X (or any one axis)
_VelocityRangeY = (0, 0, 0, 0)
_VelocityRangeZ = (0, 0, 0, 0)
_VelocityLossRange = (2.5, 2.5, 0, 0)   // raise to 5..8 if spread too wide
_PolarRadius = (15, 15, 0, 0)           // compact .uc value works once direction is fixed
```

Do not put speed on Y only with the old `length(velUe)` + broken `dirWS` path — that yields near-zero or chaotic speeds.

### When `_OwnerWorldPos` is appropriate

Use world owner offset only when the shader **intentionally** targets another world anchor and both points are in the same coordinate frame, for example:

- projectile trail toward a moving target
- effect detached from the caster pivot
- verified C# override via `SetOwnerWorldPosOverride`

For character-attached polar rings, prefer **emitter pivot + polar offset**. `_OwnerWorldPos` may remain on the material for API compatibility but should not drive skirt direction.

### Fallback direction (never use world up)

```hlsl
// BAD for horizontal ring
dirWS = len > 1e-5 ? dirWS / len : float3(0, 1, 0);

// BETTER: derive from polar azimuth
float2 h = L2Fx_OutwardDirectionXZ(spawnOfs, _PolarAzimuthDeg.xy, seed, startTime, salt);
float3 dirWS = float3(h.x, 0.0, h.y);
```

### AI checklist for new sprite shaders with polar spawn

1. Read `.uc` for `GetVelocityDirectionFrom`, `StartLocationPolarRange`, `StartVelocityRange`, `VelocityLossRange`.
2. Find a **working reference** in the same visual family (`VampiricTouchSpark` for `fx_m_t0000` dust/sparks).
3. Test at **small** `PolarRadius` from `.uc` (15 UU), not only large debug radii.
4. Test in **Play mode** with character rotation — direction must stay horizontal.
5. Do not treat debug atlas preview as proof of motion; it bypasses lifetime/fade.
6. If particles go up: suspect owner/spawn Y mismatch or `(0,1,0)` fallback, not opacity.

### Reference files in this repo

```text
.uc:     Assets/Resources/Data/Effects/shield/wh_shield_ta/m_u008_b.uc (SpriteEmitter6 AngelDust)
.mat:    Assets/Resources/Data/Effects/shield/wh_shield_ta/SpriteEmitter6.mat
shader:  Assets/Resources/Data/Shaders/Skills/Shield/ShieldTaSprite.shader
ref:     Assets/Resources/Data/Shaders/Skills/Vampiric/Touch/VampiricTouchSpark.shader
ref.mat: Assets/Resources/Data/Effects/vampiric/touch/el_vampiric_touch_ta/SpriteEmitter2.mat
helper:  Assets/Resources/Data/Shaders/Skills/Common/L2FxMeshParticleMotion.hlsl (L2Fx_OutwardDirectionXZ)
C#:      Assets/Scripts/Rendering/Effects/ParticleGroup.cs (_OwnerWorldPos write path)
```
