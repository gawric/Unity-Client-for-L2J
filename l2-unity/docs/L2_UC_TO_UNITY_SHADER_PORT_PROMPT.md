# L2 UC To Unity Shader Port Prompt

Use this prompt before porting any Lineage 2 `.uc` emitter into Unity shader/material files.

```text
You are porting a Lineage 2 UnrealScript emitter (`.uc`) to Unity URP.

Before writing or editing any shader/material, gather context:

1. Read the target `.uc` emitter block completely.
2. Read the existing material, prefab, shader, and nearby sibling emitters for this effect.
3. If visual parity is important, inspect RenderDoc exports from the original and Unity:
   - mesh/sprite CSV for radius, height, sprite size, and motion shape
   - XML/pipeline state for blend factors, blend op, render target format, and texture format
4. Check shared L2Fx helpers before inventing new conversion logic:
   - `Assets/Resources/Data/Shaders/Skills/Common/L2FxUcToUnityConvert.hlsl`
   - `Assets/Resources/Data/Shaders/Skills/Common/L2FxMeshAutoScale.hlsl`
   - `Assets/Resources/Data/Shaders/Skills/Common/L2FxSpriteAutoScale.hlsl`
   - `Assets/Resources/Data/Shaders/Skills/Common/L2FxMeshEmitterVertex.hlsl`
   - `Assets/Resources/Data/Shaders/Skills/Common/L2FxSpriteEmitterVertex.hlsl`
   - `Assets/Resources/Data/Shaders/Skills/Common/L2FxMeshBrightenD3d9.hlsl`
   - `Assets/Resources/Data/Shaders/Skills/Common/L2FxPlasmaParticleBlend.hlsl`
   - `docs/L2Fx_Shader_Library_API.md`
   - `docs/metrics/L2_UC_TO_UNITY_AI_GUIDE.md`

Core rules:

- Keep raw `.uc` values in material fields whenever possible.
- Do not manually rewrite UC size/spin/offset values to "look right".
- Convert UC values to Unity through `L2FxUcToUnityConvert.hlsl`.
- If a raw UC value needs per-effect correction, add/reuse explicit `_Uc...Scale` material properties and call `L2Fx_UcToUnityApplyScale*`; do not hide tuned constants inside one-off shader math.
- Use `_L2FxMeshScale` / `_L2FxSpriteScale` for per-effect visual size tuning.
- Use `_L2FxEffectScale` only for runtime target autoscale.
- Use `_L2FxMeshSpinDirection` only when mesh spin direction needs UC-to-Unity conversion.
- Keep GameObject scale at `1` where practical; prefer material/shader conversion coefficients.
- Do not mass-migrate old working effects. Apply the new conversion path only to the current effect or new effects.
- If particles need a character/target focal point, use Composite/ParticleGroup shader target position:
  `passShaderTargetPosition`, `shaderTargetAttachmentPoint`, `shaderTargetPositionOffset`, `_UseExternalTargetPosition`, `_L2FxTargetWorldPos`.
  Do not add one-off target scripts to emitter GameObjects.
- If velocity is `PTVD_StartPositionAndOwner`, check whether Unity should use full 3D spawn-owner direction. Use `_UseFull3DVelocityFromOwner` when UE `PolarPitch`/spawn height contributes to vertical motion.
- If owner position must match focal target, use `_UseOwnerFromShaderTarget` so `_OwnerWorldPos` resolves to the shader target (for example `CasterCenter`).
- Keep material-level blend explicit when different emitters sharing a shader need different behavior. Prefer `_SrcBlend` / `_DstBlend` properties over hard-coding a global shader blend if the shader is shared.

Expected new shader flow:

```text
raw .uc material params
  -> L2FxUcToUnityConvert.hlsl
  -> Unity-ready size/spin/offset values
  -> lower-level vertex/render helpers
```

Blend and color matching:

- `DrawStyle=PTDS_Brighten` often maps to additive or screen-like brighten, but verify in RenderDoc instead of guessing.
- If original pipeline uses `One / OneMinusSrcColor`, set Unity blend to `Blend One OneMinusSrcColor` or material `_SrcBlend=One`, `_DstBlend=OneMinusSrcColor`.
- If global `_RgbBoost` or luma settings make a hot center blow out before soft plasma is visible, include `L2FxPlasmaParticleBlend.hlsl` and add:
  `_PlasmaRgbScale`, `_PlasmaLumaMax`.
- `L2Fx_PlasmaParticle_ApplyLowLumaRgbScale(...)` is color-agnostic: it separates soft plasma by luma, not by blue channel. If plasma and core differ only by hue, use a different mask strategy.

MeshEmitter checklist:

- `StartSizeRange` -> `L2Fx_UcToUnityMeshSize(...)`
- `SizeScale(n)` -> existing SizeScale helper
- `SpinCCWorCW` and `SpinsPerSecondRange` -> keep UC values, then `L2Fx_UcToUnityMeshSpinRate(...)`
- `StartLocationOffset` -> `L2Fx_UcToUnityStartLocationOffset(...)`
- `StartVelocityRange` / `Acceleration` -> use existing UE axis conversion and `_SpawnUnitScale`; add convert helper only if missing
- `StaticMesh` authoring scale may require `_L2FxMeshScale`
- For head+tail ribbon meshes using `fx_m_t0005`-style atlases, prefer `_UseD3d9BrightenFs = 1` and `L2Fx_MeshBrighten_D3d9TexFactor(...)`.

SpriteEmitter checklist:

- `StartSizeRange` stays raw UC in material
- `L2Fx_SpriteAutoScaleStartSize(...)` preserves legacy raw sprite-unit sizing for shaders that already use it (for example `MightTaSprite`)
- Use `L2Fx_UcToUnitySpriteStartSize(...)` only for shaders/materials intentionally retuned to UU-to-Unity size conversion
- Apply `_L2FxSpriteScale` / `_L2FxEffectScale` through sprite autoscale helpers
- Preserve texture atlas fields: `TextureUSubdivisions`, `TextureVSubdivisions`, `SubdivisionStart`, `SubdivisionEnd`, `UseRandomSubdivision`, `BlendBetweenSubdivisions`
- For `SphereRadiusRange`, reuse `L2Fx_SpawnRegionRandomOnSphereUe(...)` and add an explicit scale such as `_UcSphereRadiusScale` when tuning is needed
- Check polar spawn, velocity, and spin against existing sprite helpers before writing custom code
- For funnel/inward particles, prefer external shader target position (`CasterCenter`, `TargetCenter`, etc.) over hard-coded `StartLocationOffset` focal points

If a visual correction is needed:

- First identify the raw UC field.
- Keep that UC field unchanged in the material.
- Add or reuse an explicit `L2Fx` convert/tuning property.
- Document the reason in the effect notes or guide.

After editing:

- Check lints for changed shader/material files.
- Search for all call sites if a shared HLSL function signature changed.
- Update `docs/metrics/L2_UC_TO_UNITY_AI_GUIDE.md` when adding a reusable conversion/tuning rule.
```

