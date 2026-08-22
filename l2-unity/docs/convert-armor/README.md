# Convert L2 armor textures and meshes (Unity Interlude client)

This folder documents how we import Lineage 2 Interlude **armor textures** and **skeletal meshes** into this Unity client. Use it before converting another race (Fighter, Elf, DarkElf, FMagic, …).

**Proven on:** male human mystic (`MMagic`). Test piece `MMagic_m004_u` (item 351 / 358) matched handmade FBX (verts, bounds, 108 bones including FBX leaf bones).

Do **not** overwrite meshes the user already imported from Blender by hand. Convert only missing pieces. Convert **one test mesh** per race, verify on a live character, then batch.

---

## 1. How the client finds assets

`armorgrp` names look like `Package.Asset`:

| armorgrp string | Unity `Resources.Load` path |
|---|---|
| Mesh `Magic.MMagic_m004_u` | `Data/Animations/Magic/MMagic_m004_u` (**prefab**, not the FBX) |
| Texture `MMagic.MMagic_m004_t40_u` | `Data/SysTextures/MMagic/Materials/MMagic_m004_t40_u` (**Material**) |

Code:

- Mesh: `AbstractCache.LoadArmorModel` → `Data/Animations/{package}/{asset}`
- Material: `AbstractGetCache.LoadArmorMaterial` → `Data/SysTextures/{package}/Materials/{asset}`  
  Also tries dotted filenames `MMagic.MMagic_m004_t40_u` as a fallback. Prefer the **short** material name.

On-disk layout:

```
Assets/Resources/Data/Animations/{Package}/{Race}_{piece}.prefab
Assets/Resources/Data/Animations/{Package}/{Race}/Models/{Race}_{piece}.fbx
Assets/Resources/Data/SysTextures/{Package}/{png files}
Assets/Resources/Data/SysTextures/{Package}/Materials/{material}.mat
```

Example (`MMagic`):

```
Assets/Resources/Data/Animations/Magic/MMagic_m004_u.prefab
Assets/Resources/Data/Animations/Magic/MMagic/Models/MMagic_m004_u.fbx
Assets/Resources/Data/SysTextures/MMagic/MMagic_m004_t40_u.png   (or *_sp.png / *_ori.png)
Assets/Resources/Data/SysTextures/MMagic/Materials/MMagic_m004_t40_u.mat
```

The prefab is a thin wrapper that instances the FBX (same pattern as handmade `MMagic_m005_u.prefab`).

---

## 2. Race / package map

From `Armorgrp_interlude.txt` / `ArmorgrpTable.cs`:

| armorgrp field | Typical mesh package | Texture package | Unity animation folder |
|---|---|---|---|
| `m_HumnFigh` | `Fighter` | `MFighter` (sometimes `mfighter`) | `Data/Animations/Fighter/` |
| `f_HumnFigh` | `Fighter` | `FFighter` | `Data/Animations/Fighter/` |
| `m_DarkElf` | `DarkElf` | `MDarkElf` | `Data/Animations/DarkElf/` |
| `f_DarkElf` | `DarkElf` | `FDarkElf` | `Data/Animations/DarkElf/` |
| `m_Dorf` | `Dwarf` | `MDwarf` | `Data/Animations/Dwarf/` |
| `f_Dorf` | `Dwarf` | `FDwarf` | `Data/Animations/Dwarf/` |
| `m_Elf` | `Elf` | `MElf` | `Data/Animations/Elf/` |
| `f_Elf` | `Elf` | `FElf` | `Data/Animations/Elf/` |
| `m_Magic` | `Magic` | `MMagic` | `Data/Animations/Magic/` |
| `f_Magic` | `Magic` | `FMagic` | `Data/Animations/Magic/` |
| `m_Orc` | `Orc` | `MOrc` | `Data/Animations/Orc/` |
| `f_Orc` | `Orc` | `FOrc` | `Data/Animations/Orc/` |
| `m_Shaman` | `Shaman` | `MShaman` | `Data/Animations/Shaman/` |

Piece suffixes: `_u` chest, `_l` legs, `_g` gloves, `_b` boots, `_f` face, `_m00_ah` / `_m00_bh` hair. Full-body items (`body_part=onepiece`) use two meshes (chest + legs).

---

## 3. Find which item uses a mesh

File: `Assets/StreamingAssets/Data/Meta/Armorgrp_interlude.txt`

Search `Magic.MMagic_m004_u` (or the race you need). Column 2 is `object_id`. Names: `Assets/StreamingAssets/Data/Meta/Itemname-e_interlude.txt`.

Example for `MMagic_m004_u`:

| ID | Name | Texture |
|---|---|---|
| 351 | Blast Plate | `MMagic_m004_t40_u` |
| 358 | Blue Wolf Breastplate | `MMagic_m004_t68_u` |
| 4224 | Dream Armor | `MMagic_m004_t40_u` |

In Play Mode, missing assets log:

```
[GEAR] Missing mesh/texture, reset to naked id=… slot=… race=… mesh=… texture=… meshPath=… materialPath=…
```

Unity Console filter: `Missing mesh/texture`.

---

## 4. Textures → Unity materials

### Source

UModel unpack, typically:

```
…/unpack/textures_{race}/{Package}/Texture/*.png
```

MMagic example:

```
C:\Users\hh-soft\Pictures\test_umodel\unpack\textures_mmagic\MMagic\Texture
```

### Keep / skip

**Keep:** `{Race}_m{nnn}_t{id}_{u|l|g|b}` and hair `{Race}_m{nnn}_t{id}_m00_{ah|bh}`.

**Skip:** cubes (`blue_Cube`, `cube_ideos`, `Vesper_Cube`, …), `gold`, `Mantle_*`, `Phone_Cube`, `pp11`, `ss_t06`, and addon meshes/textures with `_Rra_`, `_Rrm_`, `_Rsm_`, `_hrr_`, `_hra_`, `_Hrm_`.

### Naming

UModel often ships `_sp` / `_ori` / `_sp2` variants.

- Copy PNG **with** the umodel suffix (`MMagic_m005_t02_u_sp.png`).
- Create **one** `.mat` per armorgrp name, **without** `_sp` / `_ori` / `_sp2`.
- Prefer albedo source: exact `{name}.png`, else `{name}_sp.png`, else `{name}_ori.png`.
- Normalize `Mmagic_` → `MMagic_` so it matches `Resources.Load`.
- Do **not** name the material `MMagic.MMagic_m005_t06_u.mat`. The loader looks for `Materials/MMagic_m005_t06_u`.

### Material

Clone an existing race `.mat` (URP Lit, shader guid `933532a4fcc9baf4fa0491de14d08ed7`). Point `_BaseMap` and `_MainTex` at the PNG guid. Write a matching `.png.meta` / `.mat.meta` (or let Unity import the PNG, then create the material).

Do not overwrite materials/pngs that already exist.

After adding files, restart Play so `ModelTable` caches them. Equip can also lazy-load a missing material.

---

## 5. Meshes: PSK → Blender → FBX → prefab

### Tools (working setup)

- Blender **3.6.16** portable:  
  `C:\Users\hh-soft\Pictures\test_umodel\blemder_portable\blender-3.6.16-windows-x64\blender.exe`
- PSK importer (not in Blender’s default addons):  
  `…\blender3d_import_psk_psa-2.8.3\blender3d_import_psk_psa-2.8.3\addons\io_import_scene_unreal_psa_psk_280.py`
- PSK source:  
  `…\unpack\armor_model\{Package}\SkeletalMesh\*.psk`  
  Magic: `…\unpack\armor_model\Magic\SkeletalMesh`

Call `pskimport()` **directly**. `bpy.ops.import_scene.psk` in this addon version may not call import when `files` is empty.

### Import

```python
pskimport(
    filepath,
    context=bpy.context,
    bScaleDown=True,       # * 0.01  (cm → m); matches handmade MMagic FBX bounds
    bDontInvertRoot=True,
    bReorientBones=False,
)
```

### Export FBX

```python
bpy.ops.export_scene.fbx(
    filepath=out_fbx,
    use_selection=False,
    object_types={"ARMATURE", "MESH"},
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=True,          # handmade MMagic pieces have 108 bones (86 + leaf *_end)
    primary_bone_axis="Y",
    secondary_bone_axis="X",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=False,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_NONE",
    path_mode="AUTO",
)
```

Blender may print `NO NODES!` during armature export; the FBX is still valid if size is ~200KB+ and a reimport shows mesh + armature.

### Unity FBX importer (`.fbx.meta`)

Copy from a known-good piece of the **same race** (e.g. `MMagic_m005_u.fbx.meta`):

- `meshes.globalScale: 0.019`
- `humanDescription.globalScale: 0.019`
- `useFileScale: 0`
- `animationType: 2` (Generic)
- `avatarSetup: 0` (no Avatar)
- New `guid` per file

`SkinnedMeshSync` copies `bones` from the character body renderer onto the armor. Bone names must stay `bip01` / `Bip01_*` as in `{Race}_anim`.

### Prefab wrapper

Clone `MMagic_m005_u.prefab` / `MMagic_m004_u.prefab`:

- Outer object name = armorgrp asset (`MMagic_m004_u`)
- Layer 6, tag `User`
- Nested PrefabInstance of the FBX (`fileID: 100100000`, root transform `-8679921383154817045`, root GO `919132149155446097`)
- Unique wrapper fileIDs and unique prefab `.meta` guid
- Place at `Assets/Resources/Data/Animations/{Package}/{Asset}.prefab`

Skip dest names that already have a prefab (user handmade). Skip `_00_*_ALL.psk` and addon psk (`Rra`, `Rrm`, `Hrm`, `Hra`, `Hrr`).

### Verify before a race batch

1. Pick a **missing** body piece (not one the user already made).
2. Convert PSK → FBX with the settings above.
3. Reimport both the new FBX and an existing handmade FBX of that race in Blender; verts / dimensions / bone count must match the handmade one (MMagic chest: 255 verts, dims ≈ `(0.211, 0.0545, 0.1281)`, 108 bones).
4. In Unity, wait for import, equip a real item from `armorgrp` (e.g. 358 for `MMagic_m004_u`).
5. Check bind: mesh sits on the body, not rotated, skin follows walk/cast.
6. Only then batch the rest of that race.

---

## 6. Suggested batch order

1. Textures + materials for the race (so equip does not fall back to naked for missing `.mat`).
2. One test mesh + prefab.
3. Remaining body `u/l/g/b` (and hair if needed).
4. Leave FMagic / other races until that race is requested.
5. Weapons are **not** in the armor `SkeletalMesh` folder; they need a separate unpack + `LoadWeaponModel` path `Data/Animations/{package}/{asset}`.

---

## 7. Do not

- Do not invent race fallbacks (mage wearing fighter mesh). Missing asset → naked slot + `[GEAR]` log.
- Do not put conversion scripts inside `Decompile_Common` or change skill shaders for this work.
- Do not mix PlayerEntity paperdoll (UserInfo) with UserEntity (CharInfo). Mesh import is shared; packet gear is not.
- Do not name materials with the L2 package prefix in the filename (`MMagic.MMagic_….mat`).
