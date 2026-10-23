# Skeletal PSK + PSA -> FBX, same axis setup as the monster meshes.
# blender -b -P psk_psa_to_fbx.py -- <psk> <psa> <fbx> <addon.py> [reference.fbx]
import bpy
import os
import sys
import importlib.util


def _argv_after_dash():
    if "--" not in sys.argv:
        raise SystemExit("usage: blender -b -P psk_psa_to_fbx.py -- <psk> <psa> <fbx> <addon.py> [reference.fbx]")
    return sys.argv[sys.argv.index("--") + 1 :]


def _load_addon(path):
    spec = importlib.util.spec_from_file_location("io_import_scene_unreal_psa_psk_280", path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def _reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _mesh_size():
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        return None
    size = meshes[0].dimensions
    return (size.x, size.y, size.z, meshes[0].name)


def _import_psk(mod, psk_path, scale_down):
    ok = mod.pskimport(
        psk_path,
        context=bpy.context,
        bScaleDown=scale_down,
        bDontInvertRoot=True,
        bReorientBones=False,
    )
    if ok is False:
        raise SystemExit("pskimport failed: " + psk_path)


def main():
    args = _argv_after_dash()
    if len(args) < 4:
        raise SystemExit("need <psk> <psa> <fbx> <addon.py> [reference.fbx]")
    psk_path, psa_path, fbx_path, addon_path = args[0], args[1], args[2], args[3]
    reference = args[4] if len(args) >= 5 else ""
    os.makedirs(os.path.dirname(fbx_path) or ".", exist_ok=True)
    mod = _load_addon(addon_path)

    ref_size = None
    if reference and os.path.isfile(reference):
        _reset()
        bpy.ops.import_scene.fbx(filepath=reference)
        ref_size = _mesh_size()
        print("REF", ref_size)

    _reset()
    _import_psk(mod, psk_path, False)
    raw_size = _mesh_size()
    print("PSK_RAW", raw_size)

    scale_down = False
    if ref_size and raw_size and ref_size[2] > 0.001:
        ratio = raw_size[2] / ref_size[2]
        print("HEIGHT_RATIO", ratio)
        if ratio > 20.0:
            scale_down = True

    if scale_down:
        _reset()
        _import_psk(mod, psk_path, True)
        print("PSK_SCALED", _mesh_size())

    mesh_only = len(args) >= 6 and args[5].lower() == "mesh"
    arm = next((obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"), None)
    if arm is None:
        raise SystemExit("no armature")
    if mesh_only:
        _export(fbx_path, False)
        print("WROTE", fbx_path, "mesh_only", "scale_down", scale_down)
        return

    ok = mod.psaimport(
        psa_path,
        context=bpy.context,
        oArmature=arm,
        bScaleDown=scale_down,
        bDontInvertRoot=True,
    )
    if ok is False:
        raise SystemExit("psaimport failed: " + psa_path)

    for act in bpy.data.actions:
        if "ao_" not in act.name.lower():
            act.name = "ao_" + act.name
        print("ACTION", act.name)

    _export(fbx_path, True)
    print("WROTE", fbx_path, "scale_down", scale_down)


def _export(fbx_path, bake_anim):
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=bake_anim,
        bake_anim_use_all_actions=bake_anim,
        bake_anim_simplify_factor=0.0,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_NONE",
        path_mode="AUTO",
    )


if __name__ == "__main__":
    main()
