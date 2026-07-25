# SpawnParticleSnapshot RNG log guide

`SpawnParticleSnapshot.log` is emitted by `AutoLoginInterlude` during
`UParticleEmitter::SpawnParticle`. It records the client-side RNG sequence for
one particle spawn and is intended to let tools or AI reconstruct the same
spawn in Unity.

## Capture boundary

Each `SpawnPtvdCapture` block is one `SpawnParticle` call, identified by:

- `emitter`, `slot`, and `slotIndex`;
- emitter fingerprint values such as velocity range, repeats, and fade time;
- `dt`, the spawn delta time;
- PTVD input, normalized direction, pre-PTVD velocity, and final velocity.

For `m_u004_b / SpriteEmitter0`, use this fingerprint:

```text
repeats=9.000000 fadeOutStart=0.954000
StartVelocityRange=X[60,60] Y[60,60] Z[-18,1]
```

## RNG stream schema

```text
rngStream draws=28 scopes=12 truncated=0
  scope[1] StartLocationPolarRange emitter+0x180 vector draws=[3,6)
  draw[3] before=0x........ appRand=..... after=0x........
```

- `draw[N]` is one `appRand` execution.
- `before` is the TLS LCG state before that execution.
- `appRand` is the returned 15-bit value.
- `after` is the LCG state after that execution.
- The LCG transition is:

```text
stateAfter = stateBefore * 214013 + 2531011  (uint32 overflow)
appRand = (stateAfter >> 16) & 0x7FFF
```

- `scope[N] draws=[start,end)` assigns draw indices to one emitter range.
- `vector` scopes are `FRangeVector::GetRand`; their CPU draw order is Z, Y,
  X, while the logged vector value is X, Y, Z.
- `scalar` scopes are `FRange::GetRand`; `range=[min,max]` and `value` are
  recorded.
- `truncated=1` means the bounded draw buffer overflowed; do not treat that
  capture as a complete spawn sequence.

## Known SpriteEmitter0 scopes

| Emitter offset | Scope | Draws |
|---|---|---|
| `+0x3A0` | `StartVelocityRange` | 0–2 |
| `+0x180` | `StartLocationPolarRange` | 3–5 |
| `+0x2CC` | `StartSizeRange` | 19–21 |
| `+0x278` | `StartSpinRange` | 22–24 |
| `+0x260` | `SpinsPerSecondRange` | 25–27 |
| `+0x380` | LifetimeRange | scalar |

`unknown emitter+0x...` is deliberately not assigned a semantic name. It is
still a valid ordered RNG scope. Map it from the emitter layout or live
disassembly before using it as a named Unity parameter.

## SoulShot smog SE325 (shape@+0x174==0)

Fingerprint: `FadeOut≈0.32` + `FadeInEnd≈0.12` + `Opacity≈0.6`
(`kind=SoulShotSmogPolar` / `SpawnSoulShotSmogCapture`).

Live `rngStream draws=28` (2026-07-22) — **no Polar scope**:

| Draws | Scope | Offset |
|---|---|---|
| 0–2 | StartVelocityRange | `+0x3A0` |
| 3–5 | StartLocationRange (zeros) | `+0x158` |
| 6 | Mesh/OtherScalar `[0,1]` | `+0x2FC` |
| 7–9 | UnusedRangeVectorA | `+0x1FC` |
| 10–12 | UnusedRangeVectorB | `+0x214` |
| 13–15 | ColorMultiplierRange | `+0xB8` |
| 16 | LifetimeRange | `+0x380` |
| 17 | InitialDelayRange | `+0x378` |
| 18 | StartVelocityRadialRange | `+0x198` |
| 19–21 | StartSizeRange (vector) | `+0x2CC` |
| 22–24 | StartSpinRange | `+0x278` |
| 25–27 | SpinsPerSecondRange | `+0x260` |

Unity replay: `L2FxSpriteSpawnParticle.hlsl` +
`_SpriteSpinRandStateBits = motionState + 22`.
Do **not** skip LocRange/mid draws even when values are zero — Lifetime/Size
desync and smoke look “wrong-random”.

Polar@+0x180 is present in UC but **not** GetRand’d when shape≠Polar.

## SoulShot Spirit ME225 (MeshEmitter)

Fingerprint: `FadeOut≈0.41` + `FadeInEnd≈0.09` + `Opacity≈0.2` live
(UC lists 0.3; live `e_u505_e` probe 2026-07-22 reads **0.2**)
(+ soft cues Repeats=7 / Offset X=-5 / SizeX=0.015)
(`kind=SoulShotSpiritMesh` / `SpawnSoulShotSpiritCapture`).

**LIVE VERIFIED** `SpawnSoulShotSpiritCapture` 2026-07-22:
`rngStream draws=28 scopes=12 truncated=0` — same Wave/smog shape-0 order.

| Draws | Scope | Offset |
|---|---|---|
| 0–2 | StartVelocityRange | `+0x3A0` |
| 3–5 | StartLocationRange (zeros) | `+0x158` |
| 6 | Mesh/OtherScalar `[0,1]` | `+0x2FC` |
| 7–12 | Unused zero vectors | `+0x1FC` / `+0x214` |
| 13–15 | ColorMultiplierRange | `+0xB8` |
| 16 | LifetimeRange | `+0x380` |
| 17 | InitialDelayRange | `+0x378` |
| 18 | trailing scalar | `+0x198` |
| 19–21 | StartSizeRange | `+0x2CC` |
| 22–24 | StartSpinRange | `+0x278` |
| 25–27 | SpinsPerSecondRange | `+0x260` |

Live floats: Vel X=10 YZ±20, Size 0.015/0.1/0.1, ColorMul 0.6,
Life 1–1.5, Offset X=-5 (not RNG), Accel=0, shape=0, ptvd=0.
SpinCCW: 3× appFrand after draw 27 (not in rngStream).

Offset X=-5 is **not** an RNG draw — applied after LocRange.
Unity: `L2FxMeshSpawnParticle_SampleLocVelSize` + spin at draw 22;
mat Opacity **0.2**. DocExtractor: `MeshEmitter225` → Unity_ParticleSnapshot.

## SoulShot ShockWave ME226 (MeshEmitter)

Fingerprint: `FadeOut≈0.0375` + `Lifetime=0.2` + `Opacity≈0.6`
(+ SizeX=0.1 / Vel X=4 / no SPS / MaxParticles=2).
Prefab: `MeshEmitter226` / shader `MeshEmitter226_ShockWave`.

L2 capture: `SpawnSoulShotShockWaveCapture` / `kind=SoulShotShockWaveMesh`
(AutoLogin `SpawnParticleSnapshot.log`).

UC stream same MeshSpawn 28 draws; StartSpin Z[0,1] only; SPS=0.
**StartSpin.Roll** is the ring plane — compare slot0 vs slot1 `|dRoll|`:
small wrap → rings stack and look like one; large → clearly two.

Unity spawn log mirrors `rngStream` + `ringSpinCompare look=...`.
warmup ParticleGroup `_relativeWarmupTime=0.01` (UC RelativeWarmup 0.05×life).

## SoulShot Needle SE326 (SpriteEmitter)

Fingerprint: `FadeOut≈0.084` + `Opacity≈0.6` + `MaxParticles=8` + life `0.3`
(+ Polar θ=90 φ0..360 R10..15, Offset X=2, Size X0.15..0.7 Y18..25,
Vel 150..180, VelLoss 0.5, PTDU_Up, PTVD_OwnerAndStartPosition).
Prefab: `SpriteEmitter326` / shader `SpriteEmitter326_Needle`.
Atlas `fx_m_t0061` 2×2 sub 2..3. Blend One One.

RNG: Polar Upline-order 28 draws (Vel→Polar→mid→ColorMul→Life→SizeXYZ).
Unity: `L2FxSpritePolar` + `L2FxPTVD_OwnerAndStartPosition` + `L2FxPTDU_Up`.

## PTVD interpretation

For verified `m_u004_b / SpriteEmitter0` captures:

```text
velocityBeforePtvd = rawVelocity + acceleration * dt
direction = normalize(ptvdInput)
finalVelocityAfterSpawn = -velocityBeforePtvd * direction
```

The multiplication is component-wise. `PTVD validation ... result=PASS` with
zero error confirms that this spawn followed the formula.

PTVD consumes no separate RNG draw. It uses the position and velocity that
earlier scopes already produced.

## Limitation

The logger captures `appRand` while a hooked `FRange` or `FRangeVector` scope
is active. A direct `appFrand` call outside these helpers is intentionally not
present in `rngStream`. If an effect cannot be reproduced after every logged
scope has been replayed, add a direct-appFrand capture keyed by its caller
return address.
