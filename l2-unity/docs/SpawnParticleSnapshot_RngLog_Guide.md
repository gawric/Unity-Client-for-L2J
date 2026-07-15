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
