# Shader/VFX Dangerous Functions

This document tracks shader/VFX helpers and material settings that can silently break imported Lineage 2 effects.

## `StartLocationOffset` inside mesh vertex shaders

Known call path:

```hlsl
L2Fx_MeshBuiltin_TransformVertexOS(...)
posOS += L2Fx_MeshBuiltin_StartLocationOffsetM(startLocationOffsetUU);
```

Problem:

- This offsets every vertex inside the shader rather than moving the Unity object transform.
- With converted FBX meshes, this can make the rendered texture appear shifted relative to the mesh silhouette.
- UVs remain correct, but the shader-mutated vertex positions no longer match the imported mesh preview/Lit shader result.
- The issue was confirmed on `vampiric/touch` Fury layers: texture fits correctly when `_StartLocationOffset` is zero, and breaks when restored to UE value `(5, 0, -23)`.

Prefer:

- Keep `_StartLocationOffset` at zero for converted mesh emitters when UV/mesh alignment matters.
- Apply visual placement offsets via prefab `Transform.localPosition`, parent transform, or composite positioning instead.

Current safe test state for `vampiric/touch` Fury mesh layers:

```yaml
_SpinParticles: 1
_UseSizeScale: 1
_StartSize: {r: 1, g: 1, b: 1, a: 0}
_StartLocationOffset: {r: 0, g: 0, b: 0, a: 0}
```

